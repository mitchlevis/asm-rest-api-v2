import Sequelize from 'sequelize';
const Op = Sequelize.Op;
import { Config } from "sst/node/config";
import parameters from './parameters';
import { authenticate, validateIncomingParameters, throwError, getSequelizeObject, getHeaders, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { getQuery } from "@asportsmanager-api/core/sql_queries/getAnalyticsPayForRefereeUserYearly";

export const handler = async (_evt) => {
  try{
    await authenticate(_evt, Config.API_KEYS);

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);

    const { userId, regionId } = path;
    const sortDirection = query.sort_direction;
    
    if(!userId || userId === ''){
      await throwError(400, 'Invalid Request. userId is required');
    }

    const SQL = getQuery(regionId, userId, sortDirection);
    
    const sequelize = await getSequelizeObject();
    const queryResult = await sequelize.query(SQL, { type: sequelize.QueryTypes.SELECT });

    const response = queryResult;

    return formatSuccessResponse(_evt, response, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};