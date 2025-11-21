import dayjs from 'dayjs';
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, throwError, getSequelizeObject, getDbObject, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { getQuery } from "@asportsmanager-api/core/sql_queries/getAssignmentsMonthlyForRegionUser";
export const handler = async (_evt) => {
  try{
    const tokenData = await authenticateSessionToken(_evt);
    const userId = tokenData.UserName;

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);
    
    const { regionId } = path;
    const { year, month } = query;

    // Get region for user
    const regionUserModel = await getDbObject('RegionUser');
    const whereObject = { RealUsername: userId, IsArchived: false };
    if(regionId){
      whereObject.RegionId = regionId;
    }
    // Get all regions for user
    const regionsForUser = await regionUserModel.findAll({ attributes: ['RegionId'], where: whereObject});
    const regionIds = regionsForUser.map((region) => region.RegionId);

    // If no region, return 404
    if(!regionIds || regionIds.length === 0){
      console.log(`User ${userId} does not have any regions`);
      await throwError(404, `User regions not found`);
    }

    /*
      Get all assignments (SchedulePosition) for the month
    */
    const sequelize = await getSequelizeObject();
    const sqlQuery = getQuery(userId, regionIds, year, month);
    const queryResult = await sequelize.query(sqlQuery, { type: sequelize.QueryTypes.SELECT });

    const compiledAssignments = compileMonthAssignments(year, month, queryResult, regionIds);

    return formatSuccessResponse(_evt, compiledAssignments, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};

const compileMonthAssignments = (year, month, assignments, regionIds) => {
  const daysInMonth = dayjs(`${year}-${month}-01`).daysInMonth();
  const compiledAssignments = {};
  
  // Initialize structure for each region
  for(const regionId of regionIds){
    compiledAssignments[regionId] = {};
    // Create an array of days in the month for each region
    for(let i = 1; i <= daysInMonth; i++){
      compiledAssignments[regionId][i] = [];
    }
  }

  // Loop through assignments and add to the correct region and day
  for(const assignment of assignments){
    const regionId = assignment.RegionId;
    const day = dayjs(assignment.GameDate).date();
    compiledAssignments[regionId][day].push(assignment);
  }

  return compiledAssignments;
}