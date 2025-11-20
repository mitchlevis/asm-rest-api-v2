import Sequelize from 'sequelize';
const Op = Sequelize.Op;
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, throwError, getDbObject, getSequelizeObject, getHeaders, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { isOfficialAvailableForGameType } from "@asportsmanager-api/core/business-logic-helpers";
import { getQuery, formatResult } from "@asportsmanager-api/core/sql_queries/getMasterScheduleAssignRegionUsersAvailability"

export const handler = async (_evt) => {
  try{
    await authenticateSessionToken(_evt);

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);
    
    const { regionId, scheduleId } = path;

    // Get Region Users with Availability & Conflicts for specified ScheduleId
    const SQL = getQuery(regionId, scheduleId);
    
    const sequelize = await getSequelizeObject();
    const scheduleModel = await getDbObject('Schedule');
    const scheduleTempModel = await getDbObject('ScheduleTemp');

    const [queryResult, scheduleResult, scheduleTempResult] = await Promise.all([
      sequelize.query(SQL, { type: sequelize.QueryTypes.SELECT }),
      scheduleModel.findOne({ attributes: ['GameDate', 'GameType'], where: { RegionId: regionId, ScheduleId: scheduleId}, raw: true }),
      scheduleTempModel.findOne({ attributes: ['GameDate', 'GameType'], where: { RegionId: regionId, ScheduleId: scheduleId}, raw: true }),
    ]);

    const regionUsers = formatResult(queryResult);
    const schedule = scheduleResult || scheduleTempResult;

    // Check Availability
    const parsedGameDate = new Date(schedule.GameDate.toLocaleString('en-US', { timeZone: 'UTC' }));console.log('parsedGameDate', parsedGameDate);
    const day = parsedGameDate.getDate();
    const hour = parsedGameDate.getHours();
    for(const user of regionUsers) {
      user.IsAvailable = false;

      if(user.Availability && schedule.GameDate){
        
        user.IsAvailable = isOfficialAvailableForGameType(user.Availability, schedule.GameType, day, hour)

      }
    }


    return formatSuccessResponse(_evt, { users: regionUsers}, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};