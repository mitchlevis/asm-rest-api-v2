import Sequelize from '@sequelize/core';
const Op = Sequelize.Op;
const literal = Sequelize.literal;
import { authenticateSessionToken, validateIncomingParameters, getDbObject, getSequelizeObject, formatSuccessResponse, formatErrorResponse, throwError, replaceOperators, filterUniqueByCompoundKey, sortArrayByProperty } from '../../../../utils/helpers';
import { getLocationType, getPayForPosition, getFinesForRealUsername, getGameFeesForRow, compareScheduleToScheduleTempAndAddMetaData, groupSchedulesByParkId } from "../../../../utils/business-logic-helpers.js";
import * as requestValidationSchema from "./request";
import { getQuery, formatResult } from "../../../../db/sql_queries/getMasterScheduleEvents.js";

export default async (request) => {
	try{
		// Authenticate Session Token
		const tokenData = await authenticateSessionToken(request);
    const userId = tokenData.UserName;

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { regionId } = path;
		let { filter, sort, sort_direction: sortDirection, limit, offset, show_archived: showArchived, show_only_open: showOnlyOpen, group_parks: groupParks } = query;

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
		const whereObject = { RealUsername: userId, CanViewMasterSchedule: true }; // CanViewMasterSchedule is a flag to indicate that the user has "Master Schedule" rights
    if(regionId){
      whereObject.regionId = regionId;
    }
    if(!showArchived){
      whereObject.IsArchived = false;
    }

		// Get all regions for user
    const regionsForUser = await regionUserModel.findAll({ attributes: ['RegionId'], where: whereObject});
    const regionIds = regionsForUser.map((region) => region.RegionId);

		// If no regions, return 403 Forbidden
    if(regionIds.length === 0){
      await throwError(403, `User does not have admin access to ${regionId ? 'this': 'these'} region${regionId ? '': 's'}`);
    }

		// Get all schedule rows for region(s)
    const scheduleModel = await getDbObject('Schedule', true, request);
    const schedulePositionModel = await getDbObject('SchedulePosition', true, request);
    const scheduleTempModel = await getDbObject('ScheduleTemp', true, request);
    const schedulePositionTemp = await getDbObject('SchedulePositionTemp', true, request);

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
			where: {
				...formattedFilter,
				regionId: { [Op.in]: regionIds },
				...(showOnlyOpen ? { // Only show Open
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

		const scheduleTempQueryOptions = {
			raw: true,
			attributes: [
				'RegionId',
				'ScheduleId',
				'GameDate',
				[
					literal(`(
						SELECT COUNT(*)
						FROM SchedulePositionTemp
						WHERE
							SchedulePositionTemp.RegionId = ScheduleTemp.RegionId AND
							SchedulePositionTemp.ScheduleId = ScheduleTemp.ScheduleId AND
							SchedulePositionTemp.OfficialId = ''
					)`),
					'OpenPositionsCount'
				]
			],
			where: {
				...formattedFilter,
				UserSubmitId: userId,
				regionId: { [Op.in]: regionIds },
				...(showOnlyOpen ? { // Only show Open
					[Op.and]: [
						literal(`(
							SELECT COUNT(*)
							FROM SchedulePositionTemp
							WHERE
								SchedulePositionTemp.RegionId = ScheduleTemp.RegionId AND
								SchedulePositionTemp.ScheduleId = ScheduleTemp.ScheduleId AND
								SchedulePositionTemp.OfficialId = ''
						) > 0`)
					]
				} : {})
			},
			order: sortingArray,
		};
		// Only include limit/offset if they have valid values (not undefined/null)
		// MSSQL requires limit to be > 0 when offset is present
		if (limit !== undefined && limit !== null && limit > 0) {
			scheduleTempQueryOptions.limit = limit;
			if (offset !== undefined && offset !== null && offset >= 0) {
				scheduleTempQueryOptions.offset = offset;
			}
		}

		const [{ count: scheduleIdsTotalCount, rows: scheduleIds }, { count: scheduleTempIdsTotalCount, rows: scheduleTempIds }] = await Promise.all([
			scheduleModel.findAndCountAll(scheduleQueryOptions),
			scheduleTempModel.findAndCountAll(scheduleTempQueryOptions)
		]);

		// Make sure there are at least one schedule, otherwise return empty array
    const combinedScheduleIds = filterUniqueByCompoundKey([...scheduleIds,...scheduleTempIds], ['RegionId', 'ScheduleId'])
    if(combinedScheduleIds && combinedScheduleIds.length === 0){
      return formatSuccessResponse(request, { data: { events: [], eventsTemp: [], eventsEnhanced: [] } });
    }

		// With the list of schedule ID, we query using the SQL Query
    const SQL = getQuery(scheduleIds);
    const SQLTEMP = getQuery(scheduleTempIds, { returnTemp: true });

    const sequelize = await getSequelizeObject(request);

    const [queryResult, queryResultTemp] = await Promise.all([
      sequelize.query(SQL, { type: sequelize.QueryTypes.SELECT }),
      sequelize.query(SQLTEMP, { type: sequelize.QueryTypes.SELECT }),
    ]);

		// Create Enhanced Result with meta data
    const enhancedResult = compareScheduleToScheduleTempAndAddMetaData(queryResult, queryResultTemp)

    const [formattedResult, formattedResultTemp, formattedResultEnhanced] = await Promise.all([
      formatResult(queryResult, { groupParks }),
      formatResult(queryResultTemp, { groupParks }),
      formatResult(enhancedResult, { groupParks }),
    ]);

		// Format into an object the front-end expects
    let frontEndFormatted = formatRowsForFrontEnd(formattedResult, userId, sortingArray);
    let frontEndFormattedTemp = formatRowsForFrontEnd(formattedResultTemp, userId, sortingArray);
    let frontEndFormattedEnhanced = formatRowsForFrontEnd(formattedResultEnhanced, userId, sortingArray);

    // Group Parks?
    if(groupParks){
      frontEndFormatted = groupSchedulesByParkId(frontEndFormatted);
      frontEndFormattedTemp = groupSchedulesByParkId(frontEndFormattedTemp);
      frontEndFormattedEnhanced = groupSchedulesByParkId(frontEndFormattedEnhanced);
    }

		return formatSuccessResponse(request, { data: { events: frontEndFormatted, eventsTemp: frontEndFormattedTemp, eventsEnhanced: frontEndFormattedEnhanced, totalCount: scheduleIdsTotalCount } });
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

    // obj.Positions = row.Positions && row.Positions.length > 0 ? row.Positions: [];
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
        // Determine pay with robust fallbacks for pending-change (enhanced) rows
        let pay = getPayForPosition(row.Pay, position.PositionId, row.CrewType);
        if(pay === null || pay === undefined || Number.isNaN(pay)){
          // Fallback to pay carried on each position (from SQL) if row-level data is missing
          const positionPay = position?.Pay;
          if(positionPay !== undefined && positionPay !== null){
            pay = positionPay;
          }
        }
        // Ensure numeric for downstream fee calculations
        if(typeof pay === 'string'){
          const parsed = parseFloat(pay);
          if(!Number.isNaN(parsed)){
            pay = parsed;
          }
        }
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

    formattedResult.push(obj);
  }

  if(sortingArray &&sortingArray.length > 0){
    const firstSort = sortingArray[0];
    formattedResult = sortArrayByProperty(formattedResult, firstSort[0], firstSort[1]);
  }

  return formattedResult;
};
