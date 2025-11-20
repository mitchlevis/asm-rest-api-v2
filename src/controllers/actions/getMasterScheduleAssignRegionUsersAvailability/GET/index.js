import { authenticateSessionToken, validateIncomingParameters, getDbObject, getSequelizeObject, formatSuccessResponse, formatErrorResponse } from '../../../../utils/helpers';
import { isOfficialAvailableForGameType } from '../../../../utils/business-logic-helpers.js';
import * as requestValidationSchema from "./request";
import { getQuery, formatResult } from "../../../../db/sql_queries/getMasterScheduleAssignRegionUsersAvailability.js";

export default async (request) => {
	try{
		// Authenticate Session Token
		await authenticateSessionToken(request);

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { regionId, scheduleId } = path;

		// Get Region Users with Availability & Conflicts for specified ScheduleId
		const SQL = getQuery(regionId, scheduleId);

		const sequelize = await getSequelizeObject(request);
		const scheduleModel = await getDbObject('Schedule', true, request);
		const scheduleTempModel = await getDbObject('ScheduleTemp', true, request);

		const [queryResult, scheduleResult, scheduleTempResult] = await Promise.all([
			sequelize.query(SQL, { type: sequelize.QueryTypes.SELECT }),
			scheduleModel.findOne({
				attributes: ['GameDate', 'GameType'],
				where: { RegionId: regionId, ScheduleId: scheduleId},
				raw: true
			}),
			scheduleTempModel.findOne({
				attributes: ['GameDate', 'GameType'],
				where: { RegionId: regionId, ScheduleId: scheduleId},
				raw: true
			}),
		]);

		const regionUsers = formatResult(queryResult);
		const schedule = scheduleResult || scheduleTempResult;

		// Check Availability
		if(schedule && schedule.GameDate){
			const parsedGameDate = new Date(schedule.GameDate.toLocaleString('en-US', { timeZone: 'UTC' }));
			const day = parsedGameDate.getDate();
			const hour = parsedGameDate.getHours();

			for(const user of regionUsers) {
				user.IsAvailable = false;

				if(user.Availability && schedule.GameDate){
					// Availability is already converted to object format by formatResult
					user.IsAvailable = isOfficialAvailableForGameType(user.Availability, schedule.GameType, day, hour);
				}
			}
		} else {
			// If no schedule found, set all users as not available
			for(const user of regionUsers) {
				user.IsAvailable = false;
			}
		}

		return formatSuccessResponse(request, { data: { users: regionUsers } });
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
};

