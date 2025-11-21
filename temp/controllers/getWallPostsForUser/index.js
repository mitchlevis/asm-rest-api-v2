import Sequelize from 'sequelize';
const Op = Sequelize.Op;
import { Config } from "sst/node/config";
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, throwError, getDbObject, getSequelizeObject, getHeaders, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { getQuery, formatResult } from "@asportsmanager-api/core/sql_queries/getWallPostsForUser";

export const handler = async (_evt) => {
  try{
    // await authenticate(_evt, Config.API_KEYS);
    const tokenData = await authenticateSessionToken(_evt);
    const userId = tokenData.UserName;

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);
    
    const { regionId } = path;
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

    // Get All Users for regions
    let usersForRegions = await regionUserModel.findAll({ attributes: ['RealUsername'], where: { RegionId: { [Op.in]: regionIds} }, group: 'RealUsername' });
    usersForRegions = [...new Set(usersForRegions)];
    const usersForRegionsArray = usersForRegions.filter((user) => user.RealUsername !==  '' ).map((obj) => obj.RealUsername);
    
    const SQL = getQuery(usersForRegionsArray, sortDirection, limit, offset);
    
    // await throwError(400, 'Invalid Request. ID is required');
    const sequelize = await getSequelizeObject();
    const queryResult = await sequelize.query(SQL, { type: sequelize.QueryTypes.SELECT });

    const response = await formatResult(queryResult).catch((err) => { console.error(err); return throwError(500, 'SQL Error - Could not format response') });

    return formatSuccessResponse(_evt, response, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};