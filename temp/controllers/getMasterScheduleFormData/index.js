import Sequelize from 'sequelize';
const Op = Sequelize.Op;
import { Config } from "sst/node/config";
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, invokeLambdaFunction, throwError, getDbObject, closeSequelizeConnection, arrayToObject, getHeaders, formatSuccessResponse, formatErrorResponse, checkProcessingStatus, markAsProcessing } from "@asportsmanager-api/core/helpers";
import { isOfficialAvailableForGameType } from "@asportsmanager-api/core/business-logic-helpers";
import { getQuery, formatResult } from "@asportsmanager-api/core/sql_queries/getMasterScheduleAssignRegionUsersAvailability"
import { useS3Service } from "@asportsmanager-api/core/s3-service";

export const handler = async (_evt) => {
  try{
    const sessionToken = await authenticateSessionToken(_evt);
    const RealUsername = sessionToken.UserName;

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);

    const { regionId } = path;
    const showArchived = query.show_archived;
    const forceRefresh = query.force_refresh;

    // Check Cache
    const { getObject, putObject, deleteObject } = useS3Service();
    const cacheKey = `cache/getMasterScheduleFormData-${RealUsername}-${regionId}-${showArchived}.json`;
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

      if(!forceRefresh){
        // Invoke self with force_refresh=true
        const functionName = process.env.AWS_LAMBDA_FUNCTION_NAME;
        const queryStringParameters = _evt.queryStringParameters ? { ..._evt.queryStringParameters, force_refresh: 'true' } : { force_refresh: 'true' };
        const newEvent = {
          ..._evt,
          queryStringParameters
        };
        await invokeLambdaFunction(functionName, 'Event', newEvent);

        return formatSuccessResponse(_evt, {
          status: 'processing',
          message: 'Timeout - Data is being fetched in the background'
        }, 202);
      }

      // Check if the cache is being processed by another lambda invocation
      const s3Service = { getObject, putObject, deleteObject };
      const canProceed = await checkProcessingStatus(s3Service, cacheKey, 1); // 1 hour threshold
      
      if(!canProceed && !forceRefresh){
        return formatSuccessResponse(_evt, {
          status: 'processing',
          message: 'Data is still being fetched in the background'
        }, 202);
      }

      // Fetch Data and mark as processing with timestamp
      await markAsProcessing(s3Service, cacheKey);
      const data = await fetchData(regionId, showArchived, RealUsername);

      await putObject({ key: cacheKey, data: JSON.stringify(data) });
      await deleteObject({ key: `${cacheKey}-processing` }).catch(err => console.error(err)); // silent errors
      
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

const fetchData = async (regionId, showArchived, RealUsername) => {
  console.log('Fetching Data');
  // Get Region Leagues (for current user)
  const regionUserModel = await getDbObject('RegionUser');
  const regionModel = await getDbObject('Region');
  const regionLeagueModel = await getDbObject('RegionLeague');
  const teamModel = await getDbObject('Team');
  const parkModel = await getDbObject('Park');
  const scheduleModel = await getDbObject('Schedule');

  // Region Leagues
  console.log('Fetching Region Leagues');
  const regionLeaguesResult = await regionUserModel.findAll({
    attributes: ['RegionId', 'IsExecutive'],
    where: {
      RealUsername: RealUsername,
      IsArchived: showArchived
    },
    include: [
      {
        model: regionLeagueModel,
        include: [
          {
            model: regionModel,
            as: 'RealLeague',  // This specifies to use the RealLeagueId association
            attributes: ['RegionId'],
            include: [
              teamModel,
              parkModel
            ]
          },
        ]
      },
      {
        model: regionModel,
        attributes: ['RegionId'],
        include: [
          { 
            model: teamModel,
            attributes: ['TeamId', 'TeamName']
          },
          { 
            model: parkModel,
            attributes: ['ParkId', 'ParkName']
          }
        ]
      }
    ],
  });

  // Regions by RegionId
  const regions = {};
  for(const regionUser of regionLeaguesResult){
    regions[regionUser.RegionId] = regionUser.Region;
  }

  // Reduce RegionLeagues to named Properties 
  console.log('Reducing RegionLeagues to named Properties');
  const regionLeagues = regionLeaguesResult.reduce((acc, region) => {
    acc[region.RegionId] = arrayToObject(region.RegionLeagues, 'LeagueId');
    return acc;
  }, {})

  // Distinct Schedule Values (Used in filter)
  const where = {};
  if (regionId){
    where.RegionId = regionId;
  }
  else{
    where.RegionId = {
      [Op.in]: regionLeaguesResult.map(region => region.RegionId)
    }
  };

  const promises = [];
  // Schedules
  console.log('Adding Schedules Promise');
  promises.push(scheduleModel.findAll({
    attributes: [
      [Sequelize.fn('DISTINCT', Sequelize.col('ScheduleId')), 'value'],
    ],
    order: [
      ['ScheduleId', 'ASC']
    ],
    where,
    raw: true
  }));
  // Parks
  console.log('Adding Parks Promise');
  promises.push(scheduleModel.findAll({
    attributes: [
      [Sequelize.fn('DISTINCT', Sequelize.col('Schedule.ParkId')), 'value'],
      [Sequelize.col('Park.ParkName'), 'label']
    ],
    include: [
      {
        model: parkModel,
        attributes: [],
        on: {
          ParkId: { [Op.eq]: Sequelize.col('Schedule.ParkId') }
        },
      }
    ],
    order: [
      [Sequelize.col('Schedule.ParkId'), 'ASC']
    ],
    where,
    raw: true
  }));
  // Leagues
  console.log('Adding Leagues Promise');
  promises.push(regionLeagueModel.findAll({
    attributes: [
      [Sequelize.fn('DISTINCT', Sequelize.col('LeagueId')), 'value'],
      [Sequelize.fn('CONCAT', Sequelize.col('LeagueName'), ' - (', Sequelize.col('LeagueId'), ')'), 'label']
    ],
    order: [
      [Sequelize.col('LeagueId'), 'ASC']
    ],
    where,
    raw: true
  }));
  // Teams - We re-use the earlier results from RegionLeagues
  console.log('Adding Teams Promise');
  let teams = [];
  for(const region of regionLeaguesResult){
    // If regionId is specified, then we only want to return teams for that region
    if(regionId && regionId !== region.RegionId){
      continue;
    }

    for(const league of region.RegionLeagues){
      if(league.RealLeague && league.RealLeague.Teams && league.RealLeague.Teams.length > 0){
        const teamObjects = league.RealLeague.Teams.map(team => {
          return {
            value: team.TeamId,
            label: `${team.TeamName} - (${team.TeamId})`
          };
        });
        
        teams.push(...teamObjects);
      }
    }
  }

  /*
      [Sequelize.fn('DISTINCT', Sequelize.col('LeagueId')), 'LeagueId'],
      [Sequelize.fn('DISTINCT', Sequelize.col('AwayTeam')), 'AwayTeam'],
      [Sequelize.fn('DISTINCT', Sequelize.col('HomeTeam')), 'HomeTeam'],
  */

  console.log('Resolving Promises');
  const [scheduleId, parkId, leagueId, teamId] = await Promise.all(promises);

  console.log('Promises Resolved - Returning Data');
  return {
    regions,
    regionLeagues,
    distinctScheduleValues: { scheduleId, parkId, leagueId, teamId: teams }
  }
}