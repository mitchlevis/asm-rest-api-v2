import Sequelize from '@sequelize/core';
const Op = Sequelize.Op;
const literal = Sequelize.literal;
import { authenticateSessionToken, validateIncomingParameters, getDbObject, getSequelizeObject, formatSuccessResponse, formatErrorResponse, throwError, replaceOperators, sortArrayByProperty, parseFilterJSON } from '../../../../utils/helpers';
import { getLocationType, getPayForPosition, getRankOrder, groupSchedulesByParkId } from "../../../../utils/business-logic-helpers.js";
import * as requestValidationSchema from "./request";
import { getQuery, formatResult } from "../../../../db/sql_queries/getAvailableSpotsByRegion.js";
import { getQuery as getRefereeAvailabilityForScheduleList, formatResult as formatRefereeAvailabilityForScheduleList } from "../../../../db/sql_queries/getRefereeAvailabilityForScheduleList.js";

// Helper: build VALUES list for MSSQL NVARCHAR identifiers
const toValuesList = (values) => {
	if (!values || values.length === 0) return "(N'')";
	return values.map(v => `(N'${String(v).replace(/'/g, "''")}')`).join(',');
};

export default async (request) => {
	try{
		// Authenticate Session Token
		const tokenData = await authenticateSessionToken(request);
		const userId = tokenData.UserName;

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { regionId } = path; // optional - if not provided, search all user's regions
		let { filter, sort, sort_direction: sortDirection, limit, offset, group_parks: groupParks } = query;

		// Normalize limit/offset: Sequelize 7 requires valid values for MSSQL
		if (limit === -1) {
			limit = undefined;
		}
		if (offset === 0 && limit === undefined) {
			offset = undefined;
		}

		// Format sortingArray for Sequelize order clause
		let sortingArray;
		if (sort) {
			if (typeof sort === 'string') {
				sortingArray = [[sort, sortDirection]];
			} else if (Array.isArray(sort)) {
				sortingArray = sort.map((sortField) => [sortField, sortDirection]);
			}
		} else {
			sortingArray = [['GameDate', sortDirection]];
		}

		// Formatting filter
		let formattedFilter;
		if(filter){
			formattedFilter = replaceOperators(filter);
		}

		// Load RegionUser scope for this user (all regions if regionId not provided)
		const regionUserModel = await getDbObject('RegionUser', true, request);
		const whereRU = regionId
			? { RealUsername: userId, RegionId: regionId, IsArchived: false }
			: { RealUsername: userId, IsArchived: false };
		const regionUsers = await regionUserModel.findAll({ where: whereRU, raw: true });

		if(!regionUsers || regionUsers.length === 0){
			await throwError(403, `User does not have access to ${regionId ? 'this' : 'these'} region${regionId ? '' : 's'}`);
		}

		// Build per-region maps and union positions
		const regionIdSet = new Set(regionUsers.map(r => r.RegionId));
		const regionIdList = Array.from(regionIdSet);
		const regionIdValuesList = toValuesList(regionIdList);
		const regionUsernameList = regionUsers.map(r => r.Username).filter(Boolean);
		const regionUsernameValuesList = toValuesList(regionUsernameList);

		const regionUserByRegion = new Map();
		const userPositionsSet = new Set();
		for(const ru of regionUsers){
			regionUserByRegion.set(ru.RegionId, ru);
			try{
				const positions = JSON.parse(ru.Positions || '[]');
				positions.forEach(p => userPositionsSet.add(p));
			} catch(err){ /* ignore */ }
		}
		const userPositions = Array.from(userPositionsSet);
		const hasOfficial = userPositions.includes('official') ? 1 : 0;
		const hasSupervisor = userPositions.includes('supervisor') ? 1 : 0;
		const hasScorekeeper = userPositions.includes('scorekeeper') ? 1 : 0;

		// Build optimized SQL query with CTEs
		const sequelize = await getSequelizeObject(request);
		// sort is always an array from validation schema
		const orderBy = sort && sort.length > 0 ? `s.${sort[0]}` : 's.GameDate';
		const direction = sortDirection === 'ASC' ? 'ASC' : 'DESC';
		const rankOrder = getRankOrder();
		const rankOrderValues = rankOrder.map((r, i) => `('${r}', ${i})`).join(', ');

		// Build the optimized ID query
		const idSQL = `
			WITH UserRegions AS (
				SELECT RegionId FROM (VALUES ${regionIdValuesList}) AS UR(RegionId)
			),
			RegionUsernames AS (
				SELECT Username FROM (VALUES ${regionUsernameValuesList}) AS RU(Username)
			),
			RankOrder(rank, ord) AS (
				SELECT * FROM (VALUES ${rankOrderValues}) AS RO(rank, ord)
			)
			SELECT
				s.RegionId,
				s.ScheduleId,
				s.GameDate,
				s.LeagueId,
				COUNT(*) OVER() AS TotalCount
			FROM Schedule s
			JOIN UserRegions ur ON ur.RegionId = s.RegionId
			JOIN Region r ON r.RegionID = s.RegionId
			JOIN RegionUser ru ON ru.RegionId = s.RegionId AND ru.RealUsername = :realUsername
			WHERE CAST(s.GameDate AS DATE) BETWEEN CAST(GETDATE() AS DATE) AND COALESCE(r.MaxDateShowAvailableSpots, '9999-12-31')
				AND (
					EXISTS (
						SELECT 1 FROM SchedulePosition sp
						LEFT JOIN RegionLeague rl ON rl.RegionId = s.RegionId AND rl.LeagueId = s.LeagueId
						WHERE sp.RegionId = s.RegionId
							AND sp.ScheduleId = s.ScheduleId
							AND sp.OfficialId = ''
							AND (
								/* Official type */
								(
									:hasOfficial = 1 AND sp.PositionId NOT LIKE 'scorekeeper%' AND sp.PositionId NOT LIKE 'supervisor%'
									AND (
										CASE
											WHEN JSON_VALUE(ru.Rank, '$.official') = '' THEN 0
											WHEN JSON_VALUE(rl.MinRankAllowed, '$.official') = '' THEN 1
											WHEN (
												SELECT COUNT(*) FROM RankOrder ro
												WHERE ro.ord >= (SELECT ord FROM RankOrder WHERE rank = JSON_VALUE(rl.MinRankAllowed, '$.official'))
													AND ro.ord <= (SELECT ord FROM RankOrder WHERE rank = JSON_VALUE(rl.MaxRankAllowed, '$.official'))
													AND ro.rank = JSON_VALUE(ru.Rank, '$.official')
											) > 0 THEN 1
											ELSE 0
										END
									) = 1
									AND CAST(JSON_VALUE(rl.MinRankNumberAllowed, '$.official') AS FLOAT) <= CAST(JSON_VALUE(ru.RankNumber, '$.official') AS FLOAT)
									AND CAST(JSON_VALUE(rl.MaxRankNumberAllowed, '$.official') AS FLOAT) >= CAST(JSON_VALUE(ru.RankNumber, '$.official') AS FLOAT)
								)
								OR
								/* Supervisor type */
								(
									:hasSupervisor = 1 AND sp.PositionId LIKE 'supervisor%'
									AND (
										CASE
											WHEN JSON_VALUE(ru.Rank, '$.supervisor') = '' THEN 0
											WHEN JSON_VALUE(rl.MinRankAllowed, '$.supervisor') = '' THEN 1
											WHEN (
												SELECT COUNT(*) FROM RankOrder ro
												WHERE ro.ord >= (SELECT ord FROM RankOrder WHERE rank = JSON_VALUE(rl.MinRankAllowed, '$.supervisor'))
													AND ro.ord <= (SELECT ord FROM RankOrder WHERE rank = JSON_VALUE(rl.MaxRankAllowed, '$.supervisor'))
													AND ro.rank = JSON_VALUE(ru.Rank, '$.supervisor')
											) > 0 THEN 1
											ELSE 0
										END
									) = 1
									AND CAST(JSON_VALUE(rl.MinRankNumberAllowed, '$.supervisor') AS FLOAT) <= CAST(JSON_VALUE(ru.RankNumber, '$.supervisor') AS FLOAT)
									AND CAST(JSON_VALUE(rl.MaxRankNumberAllowed, '$.supervisor') AS FLOAT) >= CAST(JSON_VALUE(ru.RankNumber, '$.supervisor') AS FLOAT)
								)
								OR
								/* Scorekeeper type */
								(
									:hasScorekeeper = 1 AND sp.PositionId LIKE 'scorekeeper%'
									AND (
										CASE
											WHEN JSON_VALUE(ru.Rank, '$.scorekeeper') = '' THEN 0
											WHEN JSON_VALUE(rl.MinRankAllowed, '$.scorekeeper') = '' THEN 1
											WHEN (
												SELECT COUNT(*) FROM RankOrder ro
												WHERE ro.ord >= (SELECT ord FROM RankOrder WHERE rank = JSON_VALUE(rl.MinRankAllowed, '$.scorekeeper'))
													AND ro.ord <= (SELECT ord FROM RankOrder WHERE rank = JSON_VALUE(rl.MaxRankAllowed, '$.scorekeeper'))
													AND ro.rank = JSON_VALUE(ru.Rank, '$.scorekeeper')
											) > 0 THEN 1
											ELSE 0
										END
									) = 1
									AND CAST(JSON_VALUE(rl.MinRankNumberAllowed, '$.scorekeeper') AS FLOAT) <= CAST(JSON_VALUE(ru.RankNumber, '$.scorekeeper') AS FLOAT)
									AND CAST(JSON_VALUE(rl.MaxRankNumberAllowed, '$.scorekeeper') AS FLOAT) >= CAST(JSON_VALUE(ru.RankNumber, '$.scorekeeper') AS FLOAT)
								)
							)
					)
					OR EXISTS (
						SELECT 1 FROM ScheduleRequest sr
						WHERE sr.RegionId = s.RegionId AND sr.ScheduleId = s.ScheduleId AND sr.OfficialId IN (SELECT Username FROM RegionUsernames)
					)
				)
			ORDER BY ${orderBy} ${direction}
			${limit !== undefined ? `OFFSET ${offset || 0} ROWS FETCH NEXT ${limit} ROWS ONLY` : ''};
		`;

		const idRows = await sequelize.query(idSQL, {
			type: Sequelize.QueryTypes.SELECT,
			replacements: { hasOfficial, hasSupervisor, hasScorekeeper, offset: offset || 0, limit: limit || 999999, realUsername: userId }
		});

		if(!idRows || idRows.length === 0){
			return formatSuccessResponse(request, { data: { events: [], totalCount: 0 } }, 200);
		}

		const totalCount = idRows[0]?.TotalCount ?? idRows.length;
		const ids = idRows.map(r => ({ RegionId: r.RegionId, ScheduleId: r.ScheduleId }));

		// Fetch details for selected IDs
		const detailRowsRaw = await (async () => {
			const sql = getQuery(ids, null); // Show all assignees
			const res = await sequelize.query(sql, { type: sequelize.QueryTypes.SELECT });
			return await formatResult(res, { groupParks: false });
		})();

		// Fetch availability per region in parallel
		const idsByRegion = ids.reduce((acc, row) => {
			acc[row.RegionId] = acc[row.RegionId] || [];
			acc[row.RegionId].push(row.ScheduleId);
			return acc;
		}, {});

		const availabilityTasks = Object.entries(idsByRegion).map(async ([rid, scheduleIds]) => {
			const ru = regionUserByRegion.get(rid);
			const regionUsername = ru?.Username || '';
			const sql = getRefereeAvailabilityForScheduleList(rid, scheduleIds, regionUsername);
			const res = await sequelize.query(sql, { type: sequelize.QueryTypes.SELECT });
			return formatRefereeAvailabilityForScheduleList(res);
		});
		const availabilityArrays = await Promise.all(availabilityTasks);
		const availabilityFlat = availabilityArrays.flat();

		// Process results per region
		const rowsByRegion = detailRowsRaw.reduce((acc, r) => {
			acc[r.RegionId] = acc[r.RegionId] || [];
			acc[r.RegionId].push(r);
			return acc;
		}, {});

		let finalRows = [];
		for(const [rid, rows] of Object.entries(rowsByRegion)){
			const ru = regionUserByRegion.get(rid);
			const showAssignees = !!(ru?.CanViewMasterSchedule || ru?.IsExecutive);
			let regionRows = formatRowsForFrontEnd(rows, userId, ru?.Username || '', userPositions, availabilityFlat, sortingArray, showAssignees);
			if(groupParks){
				regionRows = groupSchedulesByParkId(regionRows);
			}
			finalRows = finalRows.concat(regionRows);
		}

		// Global sorting if requested
		if(sortingArray && sortingArray.length > 0){
			const firstSort = sortingArray[0];
			finalRows = sortArrayByProperty(finalRows, firstSort[0], firstSort[1]);
		}

		return formatSuccessResponse(request, { data: { events: finalRows, totalCount } }, 200);
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
};

// The front-end expects an array of objects with the following properties
const formatRowsForFrontEnd = (rows, currentUserId, currentRegionUsername, userPositions, refereeAvailabilityFormattedResult, sortingArray = false, showAssignees = false) => {

	let formattedResult = [];
	for(const row of rows){
		const obj = { ...row };

		obj.Location = {
			id: row.ParkName ? `${row.RegionId}-${row.ParkId}`: null,
			type: getLocationType(row.Sport),
			name: row.ParkName ?? row.ParkId,
			city: row.City
		}

		// Positions
		obj.Positions = [];
		if(row.Positions){
			for(const position of row.Positions){
				// Check if the position is one the user can work - if not, skip
				let positionType = 'official';
				if(position.PositionId && position.PositionId.startsWith('scorekeeper')){
					positionType = 'scorekeeper';
				}
				else if(position.PositionId && position.PositionId.startsWith('supervisor')){
					positionType = 'supervisor';
				}
				if(!userPositions.includes(positionType)){
					continue;
				}

				const pay = getPayForPosition(row.Pay, position.PositionId, row.CrewType);
				obj.Positions.push({
					positionName: position.PositionId,
					IsAdded: position.isAdded,
					IsModified: position.isModified,
					IsRemoved: position.isRemoved,
					positionUsers: [
						{
							OfficialId: position.OfficialId,
							FirstName: showAssignees ? position.FirstName: null,
							LastName: showAssignees ? position.LastName: null,
							Username: showAssignees ? position.Username: null,
							RealUsername: showAssignees ? position.RealUsername: null,
							Confirmed: position.Confirmed && position.Confirmed > 0 ? true: false,
							Conflicts: position.Conflicts && position.Conflicts.length > 0 ? position.Conflicts: [],
						}
					]
				});
				if(position.RealUsername === currentUserId){
					obj.currentUserPay = pay;
				}
			}
		}

		// Check if the current user is available for the game
		const refereeAvailability = refereeAvailabilityFormattedResult.find(r => r.ScheduleId === obj.ScheduleId);
		obj.IsCurrentUserAvailable = false; // Default to false
		if(refereeAvailability){
			obj.IsCurrentUserAvailable = refereeAvailability.IsAvailable === 1; // Convert to boolean
		}

		obj.RegionUsername = currentRegionUsername;

		formattedResult.push(obj);
	}

	if(sortingArray && sortingArray.length > 0){
		const firstSort = sortingArray[0];
		formattedResult = sortArrayByProperty(formattedResult, firstSort[0], firstSort[1]);
	}

	return formattedResult;
};

