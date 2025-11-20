import Sequelize from 'sequelize';
const Op = Sequelize.Op;
const literal = Sequelize.literal;
import { Config } from "sst/node/config";
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, getFilterQueryParameter, throwError, getDbObject, replaceOperators, getSequelizeObject, filterUniqueByCompoundKey, sortArrayByProperty, generateSQL, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { getLocationType, getPayForPosition, getFinesForRealUsername, getGameFeesForRow, compareScheduleToScheduleTempAndAddMetaData, groupSchedulesByParkId } from "@asportsmanager-api/core/business-logic-helpers";
import { getQuery, formatResult } from "@asportsmanager-api/core/sql_queries/getMasterScheduleEvents";

export const handler = async (_evt) => {
  try{
    await authenticateSessionToken(_evt);

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);

    const { userId, regionId } = path;
    const filter = getFilterQueryParameter(_evt.queryStringParameters)//query.filter ? JSON.parse(JSON.parse(decodeURIComponent(query.filter))) : undefined;
    const sort = query.sort.split(',');
    const sortDirection = query.sort_direction;
    const limit = query.limit;
    const offset = query.offset;
    const showArchived = query.show_archived;
    const showOnlyOpen = query.show_only_open;
    const groupParks = query.group_parks;

    // Formatting
    let formattedFilter;
    if(filter){
      formattedFilter = replaceOperators(filter);
    }

    let sortingArray;
    if(sort){
      sortingArray = sort.map((sortField) => [sortField, sortDirection]);
    }

    const regionUserModel = await getDbObject('RegionUser');
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
    const scheduleModel = await getDbObject('Schedule');
    const schedulePositionModel = await getDbObject('SchedulePosition');
    const scheduleTempModel = await getDbObject('ScheduleTemp');
    const schedulePositionTemp = await getDbObject('SchedulePositionTemp');
    
    const [{ count: scheduleIdsTotalCount, rows: scheduleIds }, { count: scheduleTempIdsTotalCount, rows: scheduleTempIds }] = await Promise.all([
        scheduleModel.findAndCountAll({
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
        limit,
        offset,
      }),
      scheduleTempModel.findAndCountAll({
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
        limit,
        offset,
      })
    ]);

    // Make sure there are at least one schedule, otherwise return empty array
    const combinedScheduleIds = filterUniqueByCompoundKey([...scheduleIds,...scheduleTempIds], ['RegionId', 'ScheduleId'])
    if(combinedScheduleIds && combinedScheduleIds.length === 0){
      return formatSuccessResponse(_evt, { events: [], eventsTemp: [], eventsEnhanced: [] }, 200);
    }
    
    // With the list of schedule ID, we query using the SQL Query
    const SQL = getQuery(scheduleIds);
    const SQLTEMP = getQuery(scheduleTempIds, { returnTemp: true });

    const sequelize = await getSequelizeObject();

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

    return formatSuccessResponse(_evt, { events: frontEndFormatted, eventsTemp: frontEndFormattedTemp, eventsEnhanced: frontEndFormattedEnhanced, totalCount: scheduleIdsTotalCount }, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};

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
