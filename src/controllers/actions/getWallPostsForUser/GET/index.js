import Sequelize from '@sequelize/core';
const Op = Sequelize.Op;
import { authenticateSessionToken, validateIncomingParameters, getDbObject, getSequelizeObject, formatSuccessResponse, formatErrorResponse, throwError } from '../../../../utils/helpers';
import * as requestValidationSchema from "./request";
import { getQuery, formatResult } from '../../../../db/sql_queries/getWallPostsForUser';

export default async (request) => {
	try{
		// Authenticate Session Token
		const tokenData = await authenticateSessionToken(request);
    const userId = tokenData.UserName;

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { regionId } = path;
    const sortDirection = query.sort_direction;
    const limit = query.limit;
    const offset = query.offset;

		const regionUserModel = await getDbObject('RegionUser', true, request);
    const whereObject = { RealUsername: userId };
    if(regionId){
      whereObject.regionId = regionId;
    }

    // Get all regions for user
    const regionsForUser = await regionUserModel.findAll({ attributes: ['RegionId'], where: whereObject});
    const regionIds = regionsForUser.map((region) => region.RegionId);

		// If no regions, return empty array
    if(regionIds.length === 0){
      return formatSuccessResponse(request, {
        data: [],
        statusCode: 200,
      });
    }

		// Get All Users for regions
    let usersForRegions = await regionUserModel.findAll({ attributes: ['RealUsername'], where: { RegionId: { [Op.in]: regionIds} }, group: 'RealUsername' });
    usersForRegions = [...new Set(usersForRegions)];
    const usersForRegionsArray = usersForRegions.filter((user) => user.RealUsername !==  '' ).map((obj) => obj.RealUsername);

		const SQL = getQuery(usersForRegionsArray, sortDirection, limit, offset);

		const sequelize = await getSequelizeObject(request);
    const queryResult = await sequelize.query(SQL, { type: sequelize.QueryTypes.SELECT });

    const response = await formatResult(queryResult).catch((err) => { console.error(err); return throwError(500, 'SQL Error - Could not format response') });

		return formatSuccessResponse(request, {
			data: response,
		});
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
}
