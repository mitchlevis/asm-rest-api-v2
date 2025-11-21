import dayjs from 'dayjs';
import Sequelize from 'sequelize';
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, getFilterQueryParameter, replaceOperators, throwError, getDbObject, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";

export const handler = async (_evt) => {
  try{
    const tokenData = await authenticateSessionToken(_evt);
    const userId = tokenData.UserName;

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);
    
    const { regionId } = path;
    const filter = getFilterQueryParameter(_evt.queryStringParameters);
    const sort = query.sort.split(',');
    const sortDirection = query.sort_direction;
    const limit = query.limit;
    const offset = query.offset;

    // Formatting
    let formattedFilter;
    if(filter){
      formattedFilter = replaceOperators(filter);
    }

    let sortingArray;
    if(sort){
      sortingArray = sort.map((sortField) => [sortField, sortDirection]);
    }

    // Get region for user
    const regionUserModel = await getDbObject('RegionUser');
    const regionUser = await regionUserModel.findOne({ where: { RegionId: regionId, RealUsername: userId }});

    // If no region, return 404
    if(!regionUser){
      console.log(`User ${userId} does not have a region`);
      await throwError(404, `User region not found`);
    }

    // Permissions
    if(!regionUser.IsExecutive && !regionUser.CanViewMasterSchedule){
      await throwError(403, `User ${userId} does not have permission to view region users`);
    }

    /*
      Get Region Users
    */
    const { count: regionUsersTotalCount, rows: regionUsers } = await regionUserModel.findAndCountAll({ 
      where: {
        ...formattedFilter,
        RegionId: regionId
      },
      order: sortingArray,
      limit,
      offset,
    });

    return formatSuccessResponse(_evt, { regionUsers: regionUsers, totalCount: regionUsersTotalCount }, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};