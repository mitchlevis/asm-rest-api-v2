import dayjs from 'dayjs';
import Sequelize from 'sequelize';
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, throwError, getDbObject, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { compileAvailabilityRanges, convertBase64ToAvailability } from "@asportsmanager-api/core/business-logic-helpers";

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
    const regionUser = await regionUserModel.findOne({ attributes: ['RegionId', 'Username'], where: { RegionId: regionId, RealUsername: userId }});

    // If no region, return 404
    if(!regionUser){
      console.log(`User ${userId} does not have a region`);
      await throwError(404, `User region not found`);
    }

    /*
      
    */
    const availability3Model = await getDbObject('Availability3');

    const queryObject = {
      raw: true,
      attributes: [
        'Username',
        'RealUsername',
        'FirstName',
        'LastName',
        'Rank',
        'RankNumber',
        'Positions',
        'PublicData',
        'PrivateData',
        'InternalData',
        'GlobalAvailabilityData',
        [Sequelize.col('Availability3.Availability'), 'Availability'],
        [Sequelize.fn('YEAR', Sequelize.col('Availability3.AvailabilityDate')), 'AvailabilityYear'],
        [Sequelize.fn('MONTH', Sequelize.col('Availability3.AvailabilityDate')), 'AvailabilityMonth'],
      ],
      include: [
        {
          model: availability3Model,
          as: 'Availability3',
          attributes: [],
          required: false,
          where: {
            [Sequelize.Op.and]: [
              { RegionId: { [Sequelize.Op.eq]: Sequelize.col('RegionUser.RegionId') } },
              { Username: { [Sequelize.Op.eq]: Sequelize.col('RegionUser.Username') } },
              Sequelize.where(Sequelize.fn('YEAR', Sequelize.col('Availability3.AvailabilityDate')), year),
              Sequelize.where(Sequelize.fn('MONTH', Sequelize.col('Availability3.AvailabilityDate')), month)
            ]
          }
        }
      ],
      where: {
        RegionId: regionUser.RegionId,
        Username: regionUser.Username
      },
    };
    
    const queryResult = await regionUserModel.findAll(queryObject);
    if(!queryResult || queryResult.length === 0){
      await throwError(404, `No availability found for user ${userId} in region ${regionId}`);
    }
    
    const regionUserAvailability = formatResult(queryResult);

    const formattedUserAvailability = regionUserAvailability[0];
    if(!formattedUserAvailability.Availability){
      formattedUserAvailability.Availability = createUnfilledAvailability(year, month);
      formattedUserAvailability.AvailabilityYear = year;
      formattedUserAvailability.AvailabilityMonth = month;
    }

    return formatSuccessResponse(_evt, formattedUserAvailability, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};

const formatResult = (result) => {

  if(!result || result.length === 0){
    return [];
  }

  // Parse special fields
  for(const row of result){

    // Ranks & Positions
    let rankData = {};
    let subRankData = {};
    let positionData = [];

    try{
      rankData = JSON.parse(row.Rank);
    }
    catch(err){
      console.log(`Invalid rank JSON for user ${row.Username}: ${row.Rank} - Skipping`);
    }

    try{
      subRankData = JSON.parse(row.RankNumber);
    }
    catch(err){
      console.log(`Invalid rank number JSON for user ${row.Username}: ${row.RankNumber} - Skipping`);
    }

    try{
      positionData = JSON.parse(row.Positions);
    }
    catch(err){
      console.log(`Invalid position JSON for user ${row.Username}: ${row.Positions} - Skipping`);
    }

    row.Ranks  = rankData;
    row.SubRanks = subRankData;
    row.Positions = positionData;

    // Availability
    if(row.Availability && row.AvailabilityYear && row.AvailabilityMonth){
      row.Availability = convertBase64ToAvailability(row.AvailabilityYear, row.AvailabilityMonth, row.Availability)
    }

    // Schedules
    try{
      row.SchedulePositions = JSON.parse(row.SchedulePositions);
    }
    catch(e){
      row.SchedulePositions = null;
    }
  }

  return result;
};

const createUnfilledAvailability = (year, month) => {
 const daysInMonth = dayjs(`${year}-${month}-01`).daysInMonth();
 const unfilledAvailability = {};

 for(let j = 1; j <= daysInMonth; j++){
  unfilledAvailability[j] = [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]; // 24 hours set to 0
 }

 return unfilledAvailability;
}