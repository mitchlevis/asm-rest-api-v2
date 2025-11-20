import Sequelize from 'sequelize';
const Op = Sequelize.Op;
import { Config } from "sst/node/config";
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, throwError, getDbObject, getSequelizeObject, getHeaders, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { getQuery, formatResult } from "@asportsmanager-api/core/sql_queries/getMasterScheduleAssignRequests"

export const handler = async (_evt) => {
  try{
    await authenticateSessionToken(_evt);

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);
    
    const { regionId } = path;

    const regionUserModel = await getDbObject('RegionUser');

    // Get all regions users
    const regionUsers = await regionUserModel.findAll({ attributes: ['RegionId', 'Username', 'RealUsername', 'FirstName', 'LastName', 'Rank', 'RankNumber', 'Positions', 'PublicData', 'PrivateData', 'InternalData', 'GlobalAvailabilityData'], where: { RegionId: regionId }, order: [['FirstName', 'ASC']], raw: true });

    // Get Requests
    const SQL = getQuery(regionId);
    const sequelize = await getSequelizeObject();
    const queryResult = await sequelize.query(SQL, { type: sequelize.QueryTypes.SELECT })
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

    return formatSuccessResponse(_evt, { users: regionUsers, requests}, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};