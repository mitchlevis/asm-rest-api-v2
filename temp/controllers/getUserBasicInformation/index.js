import Sequelize from 'sequelize';
const Op = Sequelize.Op;
import { Config } from "sst/node/config";
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, invokeLambdaFunction, throwError, getDbObject, closeSequelizeConnection, arrayToObject, getHeaders, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { isOfficialAvailableForGameType } from "@asportsmanager-api/core/business-logic-helpers";
import { getQuery, formatResult } from "@asportsmanager-api/core/sql_queries/getMasterScheduleAssignRegionUsersAvailability"
import { useS3Service } from "@asportsmanager-api/core/s3-service";
import axios from "axios";

export const handler = async (_evt) => {
  try{
    const sessionToken = await authenticateSessionToken(_evt);
    const RealUsername = sessionToken.UserName;
    const SessionToken = sessionToken.SessionToken;

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);

    const forceRefresh = query.force_refresh;

    // Check Cache
    const { getObject, putObject } = useS3Service();
    const cacheKey = `cache/getUserBasicInformation-${RealUsername}.json`;
    const cache = await getObject({ key: cacheKey, json: true });

    if(cache && !forceRefresh){
      console.log('Cache Hit');
      // Respond immediately if cache exists
      const response = formatSuccessResponse(_evt, cache, 200, undefined, undefined, true);

      // Invoke self with force_refresh=true
      const functionName = process.env.AWS_LAMBDA_FUNCTION_NAME;
      const queryStringParameters = _evt.queryStringParameters ? { ..._evt.queryStringParameters, force_refresh: 'true' } : { force_refresh: 'true' };
      const newEvent = {
        ..._evt,
        queryStringParameters
      };
      
      await invokeLambdaFunction(functionName, 'Event', newEvent);

      return response;
    }
    else{
      console.log(forceRefresh ? 'Force Refreshing Cache' : 'Cache Miss');

      // Proceed with normal execution if cache does not exist or forceRefresh is true
      const startTime = new Date();
      const data = await fetchData(SessionToken, RealUsername);

      await putObject({ key: cacheKey, data: JSON.stringify(data) });
      
      const endTime = new Date();
      const duration = endTime.getTime() - startTime.getTime();
      console.log(`Cache refreshed in ${duration}ms`);

      return formatSuccessResponse(_evt, data, 200);
    }
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};

const fetchData = async (SessionToken, RealUsername) => {
  console.log('Fetching Data from URL: ', Config.ASM_LEGACY_API_URL);
  
  const response = await axios({
    method: 'get',
    url: `${Config.ASM_LEGACY_API_URL}/api/basicInformation`,
    headers: {
      'Cookie': `username=${RealUsername}; sessiontoken=${SessionToken}`
    }
  })
  
  if(response.data?.success === true){
    return response.data;
  }
  else{
    if(response.data?.message === 'InvalidSessionToken'){
      throw {statusCode: 401, message: 'Unauthorized'};
    }
    else{
      throw {statusCode: 500, message: response.data?.message || 'Unknown Server Error'};
    }
  }
}