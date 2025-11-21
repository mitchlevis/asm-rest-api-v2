import Sequelize from 'sequelize';
const Op = Sequelize.Op;
import { Config } from "sst/node/config";
import parameters from "./parameters";
import { authenticate, validateIncomingParameters, getDbObject, getSequelizeObject, getHeaders, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { getLocationType, getPayForPosition } from "@asportsmanager-api/core/business-logic-helpers";
import { getQuery, formatResult } from "@asportsmanager-api/core/sql_queries/getRefereeEventsForUser";

export const handler = async (_evt) => {
  try{
    await authenticate(_evt, Config.API_KEYS);

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);

    const { userId, regionId } = path;
    const pastEventsOnly = query.past_events_only;
    const sortDirection = query.sort_direction;
    const limit = query.limit;
    const offset = query.offset;

    const regionUserModel = await getDbObject('RegionUser');
    const whereObject = { RealUsername: userId };
    if(regionId){
      whereObject.regionId = regionId;
    }

    // Get all regions for user
    const regionsForUser = await regionUserModel.findAll({ attributes: ['RegionId'], where: whereObject});
    const regionIds = regionsForUser.map((region) => region.RegionId);

    // If no regions, return empty array
    if(regionIds.length === 0){
      return formatSuccessResponse(_evt, [], 200);
    }

    // Get all events for regions
    const SQL = getQuery(regionIds, userId, pastEventsOnly, sortDirection, limit, offset);
    const sequelize = await getSequelizeObject();

    const queryResult = await sequelize.query(SQL, { type: sequelize.QueryTypes.SELECT });
    const formattedResult = await formatResult(queryResult);

    // Format into an object the front-end expects
    const frontEndFormatted = formatRowsForFrontEnd(formattedResult, userId);

    const response = frontEndFormatted;

    return formatSuccessResponse(_evt, response, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};

// The front-end expects an array of objects with the following properties
const formatRowsForFrontEnd = (rows, currentUserId) => {
  const formattedResult = [];
  for(const row of rows){
    const obj = { };
    obj.type = row.GameType;
    obj.status = row.GameStatus;
    obj.id = `${row.RegionId}-${row.ScheduleId}`;
    // RETARDED LOGIC BELOW - If the LeagueId is null, then the league is the region id (i.e. the region is the league)
    obj.title = row.LeagueId ? row.LeagueName: row.RegionName;
    obj.subTitle = `#${row.GameNumber}`;
    obj.positions = row.Positions && row.Positions.length > 0 ? row.Positions: [];
    console.log('row.GameDate', row.GameDate);
    obj.eventDate = row.GameDate;
    obj.eventTimeZone = row.TimeZone;
    obj.homeTeam = row.HomeTeamName ?? (row.HomeTeam && row.HomeTeam !== '') ? row.HomeTeam: null;
    obj.awayTeam = row.AwayTeamName ?? (row.AwayTeam && row.AwayTeam !== '') ? row.AwayTeam: null;
    obj.location = {
      id: row.ParkName ? `${row.RegionId}-${row.ParkId}`: null,
      type: getLocationType(row.Sport),
      name: row.ParkName ?? row.ParkId,
      city: row.City
    }

    // Positions
    obj.positions = [];
    if(row.Positions){
      for(const position of row.Positions){
        const pay = getPayForPosition(row.Pay, position.PositionId, row.CrewType);
        obj.positions.push({
          positionName: position.PositionId,
          positionUsers: [
            {
              OfficialId: position.OfficialId,
              FirstName: position.FirstName,
              LastName: position.LastName,
              Username: position.Username,
              RealUsername: position.RealUsername,
              Confirmed: position.Confirmed && position.Confirmed > 0 ? true: false,
              Pay: pay
            }
          ]
        });
        if(position.RealUsername === currentUserId){
          obj.currentUserPay = pay;
        }
      }
    }

    formattedResult.push(obj);
  }

  return formattedResult;
};
