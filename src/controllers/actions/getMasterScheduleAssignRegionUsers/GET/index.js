import { authenticateSessionToken, validateIncomingParameters, getDbObject, getSequelizeObject, formatSuccessResponse, formatErrorResponse } from '../../../../utils/helpers';
import * as requestValidationSchema from "./request";
import { getQuery, formatResult } from "../../../../db/sql_queries/getMasterScheduleAssignRequests.js";

export default async (request) => {
	try{
		// Authenticate Session Token
		await authenticateSessionToken(request);

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { regionId } = path;

		// Get all regions users
		const regionUserModel = await getDbObject('RegionUser', true, request);
		const regionUsers = await regionUserModel.findAll({
			attributes: ['RegionId', 'Username', 'RealUsername', 'FirstName', 'LastName', 'Rank', 'RankNumber', 'Positions', 'PublicData', 'PrivateData', 'InternalData', 'GlobalAvailabilityData'],
			where: { RegionId: regionId },
			order: [['FirstName', 'ASC']],
			raw: true
		});

		// Get Requests
		const SQL = getQuery(regionId);
		const sequelize = await getSequelizeObject(request);
		const queryResult = await sequelize.query(SQL, { type: sequelize.QueryTypes.SELECT });
		const requests = formatResult(queryResult);

		// Parse Ranks & Positions for region users
		for(let i = 0; i < regionUsers.length; i++){
			let rankData = {};
			let subRankData = {};
			let positionData = [];

			try{
				rankData = JSON.parse(regionUsers[i].Rank);
			}
			catch(err){
				console.log(`Invalid rank JSON for user ${regionUsers[i].Username}: ${regionUsers[i].Rank} - Skipping`);
			}

			try{
				subRankData = JSON.parse(regionUsers[i].RankNumber);
			}
			catch(err){
				console.log(`Invalid rank number JSON for user ${regionUsers[i].Username}: ${regionUsers[i].RankNumber} - Skipping`);
			}

			try{
				positionData = JSON.parse(regionUsers[i].Positions);
			}
			catch(err){
				console.log(`Invalid positions JSON for user ${regionUsers[i].Username}: ${regionUsers[i].Positions} - Skipping`);
			}

			regionUsers[i].Ranks = rankData;
			regionUsers[i].SubRanks = subRankData;
			regionUsers[i].Positions = positionData;
		}

		return formatSuccessResponse(request, { data: { users: regionUsers, requests } });
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
};
