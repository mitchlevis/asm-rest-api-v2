import Sequelize from '@sequelize/core';
const Op = Sequelize.Op;
const literal = Sequelize.literal;
import { authenticateSessionToken, validateIncomingParameters, getDbObject, getSequelizeObject, formatSuccessResponse, formatErrorResponse, throwError, replaceOperators, sortArrayByProperty, buildFacetValues } from '../../../../utils/helpers';
import { getLocationType, getPayForPosition, getFinesForRealUsername, getGameFeesForRow, groupSchedulesByParkId } from "../../../../utils/business-logic-helpers.js";
import * as requestValidationSchema from "./request";
import { getQuery, formatResult } from "../../../../db/sql_queries/getMyScheduleEvents.js";
import buildFacetSpecs from './facets';

export default async (request) => {
	try{
		// Authenticate Session Token
		const tokenData = await authenticateSessionToken(request);
    const userId = tokenData.UserName;

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { regionId } = path;
		let { filter, sort, sort_direction: sortDirection, limit, offset, show_archived: showArchived, show_only_open: showOnlyOpen, show_deleted: showDeleted, show_removed: showRemoved, group_parks: groupParks, include_facets: includeFacets, facet_limit: facetLimit } = query;

		// Normalize limit/offset: Sequelize 7 requires valid values for MSSQL
		// -1 means "no limit", 0 means "no offset" (when limit is also not set)
		if (limit === -1) {
			limit = undefined;
		}
		if (offset === 0 && limit === undefined) {
			offset = undefined;
		}

		// Format sortingArray for Sequelize order clause
		// sort can be a string ('GameDate') or an array (['GameDate'])
		// Sequelize expects an array of arrays: [['GameDate', 'ASC']]
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

		// Formatting
    let formattedFilter;
    if(filter){
      formattedFilter = replaceOperators(filter);
    }

		const regionUserModel = await getDbObject('RegionUser', true, request);
		const whereObject = { RealUsername: userId }; // No CanViewMasterSchedule check - this is for "My Schedule"
    if(regionId){
      whereObject.regionId = regionId;
    }

		// Get all regions for user
    const regionsForUser = await regionUserModel.findAll({ attributes: ['RegionId', 'IsArchived'], where: whereObject});

		// If no regions, return 403 Forbidden
    if(regionsForUser.length === 0){
      await throwError(403, `User does not have access to ${regionId ? 'this': 'these'} region${regionId ? '': 's'}`);
    }

		// Compile regionIds - filter out archived regions if showArchived is false
    const regionIds = regionsForUser
      .filter((region) => {
        if(showArchived){
          return true;
        }
        return region.IsArchived === false;
      })
      .map((region) => region.RegionId);

		// If no regions, return empty arrays
    if(regionIds.length === 0){
      console.log('No regions found');
      return formatSuccessResponse(request, { data: { events: [], totalCount: 0 } });
    }

		// Models used for includes and facets
    const scheduleModel = await getDbObject('Schedule', true, request);
    const parkModel = await getDbObject('Park', true, request);
    const regionLeagueModel = await getDbObject('RegionLeague', true, request);
    const teamModel = await getDbObject('Team', true, request);

		// Build query options with conditional limit/offset (Sequelize 7 requires valid values for MSSQL)
		const scheduleQueryOptions = {
			raw: true,
			attributes: [
				'RegionId',
				'ScheduleId',
				'GameDate',
				[
					literal(`(
						SELECT COUNT(*)
						FROM SchedulePosition
						WHERE
							SchedulePosition.RegionId = Schedule.RegionId AND
							SchedulePosition.ScheduleId = Schedule.ScheduleId AND
							SchedulePosition.OfficialId = ''
					)`),
					'OpenPositionsCount'
				]
			],
			include: [
			],
			where: {
				...formattedFilter,
				regionId: { [Op.in]: regionIds },
				[Op.or]: [
					{ // Current assignments
						[Op.and]: [
							literal(`(
								SELECT COUNT(*)
								FROM SchedulePosition
								WHERE
									SchedulePosition.RegionId = Schedule.RegionId AND
									SchedulePosition.ScheduleId = Schedule.ScheduleId AND
									SchedulePosition.OfficialId = (SELECT Username FROM RegionUser WHERE RegionUser.RegionId = Schedule.RegionId AND RegionUser.RealUsername = '${userId}')
							) > 0`)
						]
					},
					{ // Previous assignments with confirmation, excluding confirmed removals
						[Op.and]: [
							literal(`(
								SELECT COUNT(*)
								FROM SchedulePositionVersion
								WHERE
									SchedulePositionVersion.RegionId = Schedule.RegionId AND
									SchedulePositionVersion.ScheduleId = Schedule.ScheduleId AND
									SchedulePositionVersion.OfficialId = (SELECT Username FROM RegionUser WHERE RegionUser.RegionId = Schedule.RegionId AND RegionUser.RealUsername = '${userId}')
							) > 0`),
							literal(`(
								SELECT COUNT(*)
								FROM ScheduleConfirm
								WHERE
									ScheduleConfirm.RegionId = Schedule.RegionId AND
									ScheduleConfirm.ScheduleId = Schedule.ScheduleId AND
									ScheduleConfirm.Username = (SELECT Username FROM RegionUser WHERE RegionUser.RegionId = Schedule.RegionId AND RegionUser.RealUsername = '${userId}')
							) > 0`),
							...(!showRemoved ? [literal(`(
								NOT EXISTS (
									SELECT 1
									FROM SchedulePositionVersion AS SPV
									WHERE
										SPV.RegionId = Schedule.RegionId AND
										SPV.ScheduleId = Schedule.ScheduleId AND
										SPV.OfficialId = (SELECT Username FROM RegionUser WHERE RegionUser.RegionId = Schedule.RegionId AND RegionUser.RealUsername = '${userId}') AND
										EXISTS (
											SELECT 1
											FROM ScheduleConfirm AS SC
											WHERE
												SC.RegionId = SPV.RegionId AND
												SC.ScheduleId = SPV.ScheduleId AND
												SC.Username = SPV.OfficialId AND
												SC.VersionId > SPV.VersionId
										)
								)
							`)] : [])
						]
					}
				],
				...(!showDeleted ? {
					[Op.and]: [// Show deleted schedules, but exclude those that have been confirmed
						literal(`(
							NOT EXISTS (
								SELECT 1
								FROM ScheduleVersion AS SV
								WHERE
									SV.RegionId = Schedule.RegionId AND
									SV.ScheduleId = Schedule.ScheduleId AND
									SV.IsDeleted = 1 AND
									EXISTS (
										SELECT 1
										FROM ScheduleConfirm AS SC
										WHERE
											SC.RegionId = SV.RegionId AND
											SC.ScheduleId = SV.ScheduleId AND
											SC.Username = (SELECT Username FROM RegionUser WHERE RegionUser.RegionId = Schedule.RegionId AND RegionUser.RealUsername = '${userId}') AND
											SC.VersionId = SV.VersionId
									)
							)
						)`)
					]
				} : {}),
				...(showOnlyOpen ? { // Only show Open (Optional)
					[Op.and]: [
						literal(`(
							SELECT COUNT(*)
							FROM SchedulePosition
							WHERE
								SchedulePosition.RegionId = Schedule.RegionId AND
								SchedulePosition.ScheduleId = Schedule.ScheduleId AND
								SchedulePosition.OfficialId = ''
						) > 0`)
					]
				} : {})
			},
			order: sortingArray,
		};
		// Only include limit/offset if they have valid values (not undefined/null)
		// MSSQL requires limit to be > 0 when offset is present
		if (limit !== undefined && limit !== null && limit > 0) {
			scheduleQueryOptions.limit = limit;
			if (offset !== undefined && offset !== null && offset >= 0) {
				scheduleQueryOptions.offset = offset;
			}
		}

		const { count: scheduleIdsTotalCount, rows: scheduleIds } = await scheduleModel.findAndCountAll(scheduleQueryOptions);

		// Make sure there are at least one schedule, otherwise return empty array
    if(scheduleIds.length === 0){
      return formatSuccessResponse(request, { data: { events: [], totalCount: 0 } });
    }

		// With the list of schedule ID, we query using the SQL Query
    const SQL = getQuery(scheduleIds, userId);

    const sequelize = await getSequelizeObject(request);

    const queryResult = await sequelize.query(SQL, { type: sequelize.QueryTypes.SELECT });

    const formattedResult = await formatResult(queryResult, { groupParks });

		// Format into an object the front-end expects
    let frontEndFormatted = formatRowsForFrontEnd(formattedResult, userId, sortingArray);

    // Group Parks?
    if(groupParks){
      frontEndFormatted = groupSchedulesByParkId(frontEndFormatted);
    }

		// Facets
    let facets = undefined;
    if(includeFacets){
      const whereForFacets = {
        ...(formattedFilter || {}),
        regionId: { [Op.in]: regionIds },
        [Op.or]: [
          { // Current assignments
            [Op.and]: [
              literal(`(
                SELECT COUNT(*)
                FROM SchedulePosition
                WHERE
                  SchedulePosition.RegionId = Schedule.RegionId AND
                  SchedulePosition.ScheduleId = Schedule.ScheduleId AND
                  SchedulePosition.OfficialId = (SELECT Username FROM RegionUser WHERE RegionUser.RegionId = Schedule.RegionId AND RegionUser.RealUsername = '${userId}')
              ) > 0`)
            ]
          },
          { // Previous assignments with confirmation, excluding confirmed removals
            [Op.and]: [
              literal(`(
                SELECT COUNT(*)
                FROM SchedulePositionVersion
                WHERE
                  SchedulePositionVersion.RegionId = Schedule.RegionId AND
                  SchedulePositionVersion.ScheduleId = Schedule.ScheduleId AND
                  SchedulePositionVersion.OfficialId = (SELECT Username FROM RegionUser WHERE RegionUser.RegionId = Schedule.RegionId AND RegionUser.RealUsername = '${userId}')
              ) > 0`),
              literal(`(
                SELECT COUNT(*)
                FROM ScheduleConfirm
                WHERE
                  ScheduleConfirm.RegionId = Schedule.RegionId AND
                  ScheduleConfirm.ScheduleId = Schedule.ScheduleId AND
                  ScheduleConfirm.Username = (SELECT Username FROM RegionUser WHERE RegionUser.RegionId = Schedule.RegionId AND RegionUser.RealUsername = '${userId}')
              ) > 0`),
              ...(!showRemoved ? [literal(`(
                NOT EXISTS (
                  SELECT 1
                  FROM SchedulePositionVersion AS SPV
                  WHERE
                    SPV.RegionId = Schedule.RegionId AND
                    SPV.ScheduleId = Schedule.ScheduleId AND
                    SPV.OfficialId = (SELECT Username FROM RegionUser WHERE RegionUser.RegionId = Schedule.RegionId AND RegionUser.RealUsername = '${userId}') AND
                    EXISTS (
                      SELECT 1
                      FROM ScheduleConfirm AS SC
                      WHERE
                        SC.RegionId = SPV.RegionId AND
                        SC.ScheduleId = SPV.ScheduleId AND
                        SC.Username = SPV.OfficialId AND
                        SC.VersionId > SPV.VersionId
                    )
                )
              )`)] : [])
            ]
          }
        ],
        ...(!showDeleted ? {
          [Op.and]: [
            literal(`(
              NOT EXISTS (
                SELECT 1
                FROM ScheduleVersion AS SV
                WHERE
                  SV.RegionId = Schedule.RegionId AND
                  SV.ScheduleId = Schedule.ScheduleId AND
                  SV.IsDeleted = 1 AND
                  EXISTS (
                    SELECT 1
                    FROM ScheduleConfirm AS SC
                    WHERE
                      SC.RegionId = SV.RegionId AND
                      SC.ScheduleId = SV.ScheduleId AND
                      SC.Username = (SELECT Username FROM RegionUser WHERE RegionUser.RegionId = Schedule.RegionId AND RegionUser.RealUsername = '${userId}') AND
                      SC.VersionId = SV.VersionId
                  )
              )
            )`)
          ]
        } : {}),
        ...(showOnlyOpen ? {
          [Op.and]: [
            literal(`(
              SELECT COUNT(*)
              FROM SchedulePosition
              WHERE
                SchedulePosition.RegionId = Schedule.RegionId AND
                SchedulePosition.ScheduleId = Schedule.ScheduleId AND
                SchedulePosition.OfficialId = ''
            ) > 0`)
          ]
        } : {})
      };
      const includeForFacets = [
        { model: parkModel, as: 'ParkAssociation', required: false },
        { model: regionLeagueModel, as: 'RegionLeague', required: false },
        { model: teamModel, as: 'HomeTeamAssociation', required: false },
        { model: teamModel, as: 'AwayTeamAssociation', required: false },
      ];
      const facetSpecs = buildFacetSpecs({ parkModel, regionLeagueModel, teamModel });
      facets = await buildFacetValues({ model: scheduleModel, where: whereForFacets, include: includeForFacets, facetSpecs, limit: facetLimit, request });
    }

		return formatSuccessResponse(request, { data: { events: frontEndFormatted, totalCount: scheduleIdsTotalCount, ...(facets ? { facets } : {}) } });
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
}

// The front-end expects an array of objects with the following properties
const formatRowsForFrontEnd = (rows, currentUserId, sortingArray = false) => {
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
        const pay = getPayForPosition(row.Pay, position.PositionId, row.CrewType);
        obj.Positions.push({
          positionName: position.PositionId,
          IsAdded: position.isAdded,
          IsModified: position.isModified,
          IsRemoved: position.isRemoved,
          positionUsers: [
            {
              OfficialId: position.OfficialId,
              FirstName: position.FirstName,
              LastName: position.LastName,
              Username: position.Username,
              RealUsername: position.RealUsername,
              Confirmed: position.Confirmed && position.Confirmed > 0 ? true: false,
              Conflicts: position.Conflicts && position.Conflicts.length > 0 ? position.Conflicts: [],
              Pay: pay,
              Fines: getFinesForRealUsername(obj.Fines, position.RealUsername)
            }
          ]
        });
        if(position.RealUsername === currentUserId){
          obj.currentUserPay = pay;
        }
      }
    }

    // Calculate Game Fees
    const rowFees = getGameFeesForRow(obj);

    obj.GameFeeTotal = rowFees.total;
    obj.GameFeeItemized = rowFees.items;

    // Format Versions Recursively (if any)
    if(obj.Versions && obj.Versions.length > 0){
      obj.Versions = formatRowsForFrontEnd(obj.Versions, currentUserId);
    }

    formattedResult.push(obj);
  }

  if(sortingArray && sortingArray.length > 0){
    const firstSort = sortingArray[0];
    formattedResult = sortArrayByProperty(formattedResult, firstSort[0], firstSort[1]);
  }

  return formattedResult;
};

