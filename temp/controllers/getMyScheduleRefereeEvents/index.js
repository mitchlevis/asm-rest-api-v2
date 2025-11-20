import Sequelize from 'sequelize';
const Op = Sequelize.Op;
const literal = Sequelize.literal;
import { Config } from "sst/node/config";
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, getFilterQueryParameter, throwError, getDbObject, replaceOperators, getSequelizeObject, filterUniqueByCompoundKey, sortArrayByProperty, generateSQL, formatSuccessResponse, formatErrorResponse, buildFacetValues } from "@asportsmanager-api/core/helpers";
import { getLocationType, getPayForPosition, getFinesForRealUsername, getGameFeesForRow, compareScheduleToScheduleTempAndAddMetaData, groupSchedulesByParkId } from "@asportsmanager-api/core/business-logic-helpers";
import { getQuery, formatResult } from "@asportsmanager-api/core/sql_queries/getMyScheduleEvents";
import buildFacetSpecs from './facets';

export const handler = async (_evt) => {
  try{
    const tokenData = await authenticateSessionToken(_evt);
    const userId = tokenData.UserName;

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);

    const { regionId } = path;
    const filter = getFilterQueryParameter(_evt.queryStringParameters)//query.filter ? JSON.parse(JSON.parse(decodeURIComponent(query.filter))) : undefined;
    const sort = query.sort.split(',');
    const sortDirection = query.sort_direction;
    const limit = query.limit;
    const offset = query.offset;
    const showArchived = query.show_archived;
    const showOnlyOpen = query.show_only_open;
    const showDeleted = query.show_deleted;
    const showRemoved = query.show_removed;
    const groupParks = query.group_parks;
    const includeFacets = query.include_facets;
    const facetLimit = query.facet_limit;

    // Formatting
    let formattedFilter;
    if(filter){
      formattedFilter = replaceOperators(filter);
    }

    let sortingArray;
    if(sort){
      sortingArray = sort.map((sortField) => [sortField, sortDirection]);
    }

    const regionUserModel = await getDbObject('RegionUser');
    const whereObject = { RealUsername: userId }; // IsExecutive is a flag to indicate that the user has "Master Schedule" rights
    if(regionId){
      whereObject.regionId = regionId;
    }

    // Get all regions for user
    const regionsForUser = await regionUserModel.findAll({ attributes: ['RegionId', 'IsArchived'], where: whereObject});
    
    // If no regions, return 403 Forbidden
    if(regionsForUser.length === 0){
      await throwError(403, `User does not have access to ${regionId ? 'this': 'these'} region${regionId ? '': 's'}`);
    }

    // Compile regionIds - filter out archived regions if showArchived is false
    const regionIds = regionsForUser
      .filter((region) => {

        if(showArchived){
          return true;
        }
        return region.IsArchived === false;
      })
      .map((region) => region.RegionId);

    // If no regions, return empty arrays
    if(regionIds.length === 0){
      console.log('No regions found');
      return formatSuccessResponse(_evt, { events: [], eventsTemp: [], eventsEnhanced: [] }, 200);
    }
    
    // Models used for includes and facets
    const scheduleModel = await getDbObject('Schedule');
    const parkModel = await getDbObject('Park');
    const regionLeagueModel = await getDbObject('RegionLeague');
    const teamModel = await getDbObject('Team');

    const { count: scheduleIdsTotalCount, rows: scheduleIds } = await scheduleModel.findAndCountAll({
      raw: true,
      attributes: [
        'RegionId',
        'ScheduleId',
        'GameDate',
        [
          literal(`(
            SELECT COUNT(*)
            FROM SchedulePosition
            WHERE 
              SchedulePosition.RegionId = Schedule.RegionId AND
              SchedulePosition.ScheduleId = Schedule.ScheduleId AND
              SchedulePosition.OfficialId = ''
          )`),
          'OpenPositionsCount'
        ]
      ],
      include: [
        {
          model: regionUserModel,
          attributes: ['RegionId'],
          where: {
            RealUsername: userId
          }
        }
      ],
      where: {
        ...formattedFilter,
        regionId: { [Op.in]: regionIds },
        [Op.or]: [
          { // Current assignments
            [Op.and]: [
              literal(`(
                SELECT COUNT(*)
                FROM SchedulePosition
                WHERE 
                  SchedulePosition.RegionId = Schedule.RegionId AND
                  SchedulePosition.ScheduleId = Schedule.ScheduleId AND
                  SchedulePosition.OfficialId = RegionUser.Username
              ) > 0`)
            ]
          },
          { // Previous assignments with confirmation, excluding confirmed removals
            [Op.and]: [
              literal(`(
                SELECT COUNT(*)
                FROM SchedulePositionVersion
                WHERE 
                  SchedulePositionVersion.RegionId = Schedule.RegionId AND
                  SchedulePositionVersion.ScheduleId = Schedule.ScheduleId AND
                  SchedulePositionVersion.OfficialId = RegionUser.Username
              ) > 0`),
              literal(`(
                SELECT COUNT(*)
                FROM ScheduleConfirm
                WHERE 
                  ScheduleConfirm.RegionId = Schedule.RegionId AND
                  ScheduleConfirm.ScheduleId = Schedule.ScheduleId AND
                  ScheduleConfirm.Username = RegionUser.Username
              ) > 0`),
              ...(!showRemoved ? [literal(`(
                NOT EXISTS (
                  SELECT 1
                  FROM SchedulePositionVersion AS SPV
                  WHERE 
                    SPV.RegionId = Schedule.RegionId AND
                    SPV.ScheduleId = Schedule.ScheduleId AND
                    SPV.OfficialId = RegionUser.Username AND
                    EXISTS (
                      SELECT 1
                      FROM ScheduleConfirm AS SC
                      WHERE 
                        SC.RegionId = SPV.RegionId AND
                        SC.ScheduleId = SPV.ScheduleId AND
                        SC.Username = SPV.OfficialId AND
                        SC.VersionId > SPV.VersionId
                    )
                )
              )`)] : [])
            ]
          }
        ],
        ...(!showDeleted ? {
          [Op.and]: [// Show deleted schedules, but exclude those that have been confirmed
            literal(`(
              NOT EXISTS (
                SELECT 1
                FROM ScheduleVersion AS SV
                WHERE 
                  SV.RegionId = Schedule.RegionId AND
                  SV.ScheduleId = Schedule.ScheduleId AND
                  SV.IsDeleted = 1 AND
                  EXISTS (
                    SELECT 1
                    FROM ScheduleConfirm AS SC
                    WHERE 
                      SC.RegionId = SV.RegionId AND
                      SC.ScheduleId = SV.ScheduleId AND
                      SC.Username = RegionUser.Username AND
                      SC.VersionId = SV.VersionId
                  )
              )
            )`)
          ]
        } : {}),
        ...(showOnlyOpen ? { // Only show Open (Optional)
          [Op.and]: [
            literal(`(
              SELECT COUNT(*)
              FROM SchedulePosition
              WHERE 
                SchedulePosition.RegionId = Schedule.RegionId AND
                SchedulePosition.ScheduleId = Schedule.ScheduleId AND
                SchedulePosition.OfficialId = ''
            ) > 0`)
          ]
        } : {})
      },
      order: sortingArray,
      limit,
      offset,
    });

    // Make sure there are at least one schedule,
    if(scheduleIds.length === 0){
      return formatSuccessResponse(_evt, { events: [], eventsTemp: [], eventsEnhanced: [] }, 200);
    }
    
    // With the list of schedule ID, we query using the SQL Query
    const SQL = getQuery(scheduleIds, userId);

    const sequelize = await getSequelizeObject();

    const queryResult = await sequelize.query(SQL, { type: sequelize.QueryTypes.SELECT });

    const formattedResult = await formatResult(queryResult, { groupParks });

    // Format into an object the front-end expects
    let frontEndFormatted = formatRowsForFrontEnd(formattedResult, userId, sortingArray);

    // Group Parks?
    if(groupParks){
      frontEndFormatted = groupSchedulesByParkId(frontEndFormatted);
    }

    // Facets
    let facets = undefined;
    if(includeFacets){
      const whereForFacets = {
        ...(formattedFilter || {}),
        regionId: { [Op.in]: regionIds },
        [Op.or]: [
          { // Current assignments
            [Op.and]: [
              literal(`(
                SELECT COUNT(*)
                FROM SchedulePosition
                WHERE 
                  SchedulePosition.RegionId = Schedule.RegionId AND
                  SchedulePosition.ScheduleId = Schedule.ScheduleId AND
                  SchedulePosition.OfficialId = RegionUser.Username
              ) > 0`)
            ]
          },
          { // Previous assignments with confirmation, excluding confirmed removals
            [Op.and]: [
              literal(`(
                SELECT COUNT(*)
                FROM SchedulePositionVersion
                WHERE 
                  SchedulePositionVersion.RegionId = Schedule.RegionId AND
                  SchedulePositionVersion.ScheduleId = Schedule.ScheduleId AND
                  SchedulePositionVersion.OfficialId = RegionUser.Username
              ) > 0`),
              literal(`(
                SELECT COUNT(*)
                FROM ScheduleConfirm
                WHERE 
                  ScheduleConfirm.RegionId = Schedule.RegionId AND
                  ScheduleConfirm.ScheduleId = Schedule.ScheduleId AND
                  ScheduleConfirm.Username = RegionUser.Username
              ) > 0`),
              ...(!showRemoved ? [literal(`(
                NOT EXISTS (
                  SELECT 1
                  FROM SchedulePositionVersion AS SPV
                  WHERE 
                    SPV.RegionId = Schedule.RegionId AND
                    SPV.ScheduleId = Schedule.ScheduleId AND
                    SPV.OfficialId = RegionUser.Username AND
                    EXISTS (
                      SELECT 1
                      FROM ScheduleConfirm AS SC
                      WHERE 
                        SC.RegionId = SPV.RegionId AND
                        SC.ScheduleId = SPV.ScheduleId AND
                        SC.Username = SPV.OfficialId AND
                        SC.VersionId > SPV.VersionId
                    )
                )
              )`)] : [])
            ]
          }
        ],
        ...(!showDeleted ? {
          [Op.and]: [
            literal(`(
              NOT EXISTS (
                SELECT 1
                FROM ScheduleVersion AS SV
                WHERE 
                  SV.RegionId = Schedule.RegionId AND
                  SV.ScheduleId = Schedule.ScheduleId AND
                  SV.IsDeleted = 1 AND
                  EXISTS (
                    SELECT 1
                    FROM ScheduleConfirm AS SC
                    WHERE 
                      SC.RegionId = SV.RegionId AND
                      SC.ScheduleId = SV.ScheduleId AND
                      SC.Username = RegionUser.Username AND
                      SC.VersionId = SV.VersionId
                  )
              )
            )`)
          ]
        } : {}),
        ...(showOnlyOpen ? {
          [Op.and]: [
            literal(`(
              SELECT COUNT(*)
              FROM SchedulePosition
              WHERE 
                SchedulePosition.RegionId = Schedule.RegionId AND
                SchedulePosition.ScheduleId = Schedule.ScheduleId AND
                SchedulePosition.OfficialId = ''
            ) > 0`)
          ]
        } : {})
      };
      const includeForFacets = [
        { model: regionUserModel, attributes: ['RegionId'], where: { RealUsername: userId } },
        { model: parkModel, as: 'Park', required: false },
        { model: regionLeagueModel, as: 'RegionLeague', required: false },
        { model: teamModel, as: 'HomeTeamAssociation', required: false },
        { model: teamModel, as: 'AwayTeamAssociation', required: false },
      ];
      const facetSpecs = buildFacetSpecs({ parkModel, regionLeagueModel, teamModel });
      facets = await buildFacetValues({ model: scheduleModel, where: whereForFacets, include: includeForFacets, facetSpecs, limit: facetLimit, sequelize });
    }

    return formatSuccessResponse(_evt, { events: frontEndFormatted, totalCount: scheduleIdsTotalCount, ...(facets ? { facets } : {}) }, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};

// The front-end expects an array of objects with the following properties
const formatRowsForFrontEnd = (rows, currentUserId, sortingArray = false) => {

  let formattedResult = [];
  for(const row of rows){
    const obj = { ...row };
    
    // obj.Positions = row.Positions && row.Positions.length > 0 ? row.Positions: [];
    obj.Location = {
      id: row.ParkName ? `${row.RegionId}-${row.ParkId}`: null,
      type: getLocationType(row.Sport),
      name: row.ParkName ?? row.ParkId,
      city: row.City
    }

    // Positions
    obj.Positions = [];
    if(row.Positions){
      for(const position of row.Positions){
        const pay = getPayForPosition(row.Pay, position.PositionId, row.CrewType);
        obj.Positions.push({
          positionName: position.PositionId,
          IsAdded: position.isAdded,
          IsModified: position.isModified,
          IsRemoved: position.isRemoved,
          positionUsers: [
            {
              OfficialId: position.OfficialId,
              FirstName: position.FirstName,
              LastName: position.LastName,
              Username: position.Username,
              RealUsername: position.RealUsername,
              Confirmed: position.Confirmed && position.Confirmed > 0 ? true: false,
              Conflicts: position.Conflicts && position.Conflicts.length > 0 ? position.Conflicts: [],
              Pay: pay,
              Fines: getFinesForRealUsername(obj.Fines, position.RealUsername)
            }
          ]
        });
        if(position.RealUsername === currentUserId){
          obj.currentUserPay = pay;
        }
      }
    }

    // Calculate Game Fees
    const rowFees = getGameFeesForRow(obj);

    obj.GameFeeTotal = rowFees.total;
    obj.GameFeeItemized = rowFees.items;

    // Format Versions Recursively (if any)
    if(obj.Versions && obj.Versions.length > 0){
      obj.Versions = formatRowsForFrontEnd(obj.Versions, currentUserId);
    }

    formattedResult.push(obj);
  }

  if(sortingArray &&sortingArray.length > 0){
    const firstSort = sortingArray[0];
    formattedResult = sortArrayByProperty(formattedResult, firstSort[0], firstSort[1]);
  }

  return formattedResult;
};
