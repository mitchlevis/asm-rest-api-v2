import Sequelize from 'sequelize';
const Op = Sequelize.Op;
const literal = Sequelize.literal;
import dayjs from 'dayjs';
import { Config } from "sst/node/config";
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, getFilterQueryParameter, throwError, getDbObject, replaceOperators, getSequelizeObject, filterUniqueByCompoundKey, sortArrayByProperty, generateSQL, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { getLocationType, getPayForPosition, getFinesForRealUsername, getGameFeesForRow, getRankOrder, groupSchedulesByParkId, isOfficialAvailableForGameType } from "@asportsmanager-api/core/business-logic-helpers";
import { getQuery, formatResult } from "@asportsmanager-api/core/sql_queries/getAvailableSpotsByRegion";
import { getQuery as getRefereeAvailabilityForScheduleList, formatResult as formatRefereeAvailabilityForScheduleList } from "@asportsmanager-api/core/sql_queries/getRefereeAvailabilityForScheduleList";

export const handler = async (_evt) => {
  try{
    const tokenData = await authenticateSessionToken(_evt);
    const userId = tokenData.UserName;
console.log('userId', userId);
    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);

    const { regionId } = path;
    const filter = getFilterQueryParameter(_evt.queryStringParameters)//query.filter ? JSON.parse(JSON.parse(decodeURIComponent(query.filter))) : undefined;
    const sort = query.sort.split(',');
    const sortDirection = query.sort_direction;
    const limit = query.limit;
    const offset = query.offset;
    const groupParks = query.group_parks;

    // Formatting
    let formattedFilter;
    if(filter){
      formattedFilter = replaceOperators(filter);
    }

    let sortingArray;
    if(sort){
      sortingArray = sort.map((sortField) => [sortField, sortDirection]);
    }

    // Get Region User
    const regionUserModel = await getDbObject('RegionUser');
    const regionUser = await regionUserModel.findOne({ where: { RealUsername: userId, RegionId: regionId } });

    if(!regionUser){
      await throwError(403, `User does not have access to ${regionId ? 'this': 'these'} region${regionId ? '': 's'}`);
    }

    // Parse user's rank and rankNumber JSON
    const userRank = JSON.parse(regionUser.Rank || '{}');
    const userRankNumber = JSON.parse(regionUser.RankNumber || '{}');
    const userPositions = JSON.parse(regionUser.Positions || '[]');

    // Add debug column for positions and handle empty array case
    const userPositionsStr = userPositions.length > 0 
      ? userPositions.map(p => `'${p}'`).join(',')
      : "''"; // Default to impossible position if array is empty
    console.log('User Positions:', userPositionsStr);

    // Get Region
    const regionModel = await getDbObject('Region');
    const region = await regionModel.findOne({ where: { RegionId: regionId } });

    // Enforcing Date Filter (From 'now' to Region.MaxDateShowAvailableSpots)
    const maxDateShowAvailableSpots = region.MaxDateShowAvailableSpots;

    // Get Schedules that have open spots
    const scheduleModel = await getDbObject('Schedule');
    const regionLeagueModel = await getDbObject('RegionLeague');
    
    const { count: scheduleIdsTotalCount, rows: scheduleIds } = await scheduleModel.findAndCountAll({
      raw: true,
      attributes: [
        'RegionId',
        'ScheduleId',
        'GameDate',
        'LeagueId',
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
        ],
        // Add debug columns
        [literal(`JSON_VALUE(RegionLeague.MinRankAllowed, '$.official')`), 'Debug_MinRankAllowed'],
        [literal(`'${userRank.official || ''}'`), 'Debug_UserRank'],
        [literal(`(
          SELECT STUFF((
            SELECT ',' + sp.PositionId
            FROM SchedulePosition sp
            WHERE 
              sp.RegionId = Schedule.RegionId AND
              sp.ScheduleId = Schedule.ScheduleId AND
              sp.OfficialId = ''
            FOR XML PATH(''), TYPE
          ).value('.', 'NVARCHAR(MAX)'), 1, 1, '')
        )`), 'Debug_OpenPositions']
      ],
      include: [
        {
          model: regionModel,
          as: 'Region',
          attributes: ['MaxDateShowAvailableSpots']
        },
        {
          model: regionLeagueModel,
          as: 'RegionLeague',
          required: false,
          attributes: [
            'LeagueId',
            'MinRankAllowed',
            'MinRankNumberAllowed',
            'MaxRankAllowed',
            'MaxRankNumberAllowed'
          ],
          where: literal(`
            /* Ensure we're joining with the correct RegionLeague */
            RegionLeague.RegionId = Schedule.RegionId
          `)
        }
      ],
      where: {
        ...formattedFilter,
        GameDate: {
          [Op.between]: [
            literal('CAST(GETDATE() AS DATE)'), 
            literal(`COALESCE(Region.MaxDateShowAvailableSpots, '9999-12-31')`)
          ]
        },
        regionId: regionId, // Only show schedules for this region
        [Op.and]: [
          literal(`(
            /* Original Condition: If Schedule has no LeagueId, include it, otherwise check rank requirements for open spots */
            (
              Schedule.LeagueId IS NULL 
              OR Schedule.LeagueId = ''
              OR (
                /* Check if user has required rank for any position type that exists in SchedulePosition */
                EXISTS (
                  SELECT 1 FROM SchedulePosition
                  LEFT JOIN RegionLeague ON RegionLeague.RegionId = Schedule.RegionId AND RegionLeague.LeagueId = Schedule.LeagueId
                  WHERE 
                    SchedulePosition.RegionId = Schedule.RegionId 
                    AND SchedulePosition.ScheduleId = Schedule.ScheduleId
                    AND SchedulePosition.OfficialId = '' /* Spot must be open */
                    /* Ensure the position is one the user can work */
                    AND (
                      /* If the user has 'official' position, they can work any non-scorekeeper/supervisor position */
                      ('official' IN (${userPositionsStr}) 
                        AND SchedulePosition.PositionId NOT LIKE 'scorekeeper%'
                        AND SchedulePosition.PositionId NOT LIKE 'supervisor%'
                      )
                      /* Or check for exact position matches */
                      OR SchedulePosition.PositionId IN (${userPositionsStr})
                    )
                    AND (
                      /* Official position check - any position that doesn't start with 'scorekeeper' or 'supervisor' */
                      (
                        SchedulePosition.PositionId NOT LIKE 'scorekeeper%'
                        AND SchedulePosition.PositionId NOT LIKE 'supervisor%'
                        AND (
                          SELECT CASE 
                            WHEN '${userRank.official || ''}' = '' THEN 0
                            WHEN JSON_VALUE(RegionLeague.MinRankAllowed, '$.official') = '' THEN 1
                            WHEN (
                              SELECT COUNT(*) 
                              FROM (VALUES ${getRankOrder().map((r, i) => `('${r}', ${i})`).join(', ')}) AS RankOrder(rank, ord)
                              WHERE RankOrder.ord >= (
                                SELECT ord 
                                FROM (VALUES ${getRankOrder().map((r, i) => `('${r}', ${i})`).join(', ')}) AS RO(rank, ord)
                                WHERE RO.rank = JSON_VALUE(RegionLeague.MinRankAllowed, '$.official')
                              )
                              AND RankOrder.ord <= (
                                SELECT ord 
                                FROM (VALUES ${getRankOrder().map((r, i) => `('${r}', ${i})`).join(', ')}) AS RO(rank, ord)
                                WHERE RO.rank = JSON_VALUE(RegionLeague.MaxRankAllowed, '$.official')
                              )
                              AND RankOrder.rank = '${userRank.official || ''}'
                            ) > 0 THEN 1
                            ELSE 0
                          END
                        ) = 1
                        AND CAST(JSON_VALUE(RegionLeague.MinRankNumberAllowed, '$.official') AS FLOAT) <= ${userRankNumber.official || 0}
                        AND CAST(JSON_VALUE(RegionLeague.MaxRankNumberAllowed, '$.official') AS FLOAT) >= ${userRankNumber.official || 0}
                      )
                      OR
                      /* Supervisor position check */
                      (
                        SchedulePosition.PositionId LIKE 'supervisor%'
                        AND (
                          SELECT CASE 
                            WHEN '${userRank.supervisor || ''}' = '' THEN 0
                            WHEN JSON_VALUE(RegionLeague.MinRankAllowed, '$.supervisor') = '' THEN 1
                            WHEN (
                              SELECT COUNT(*) 
                              FROM (VALUES ${getRankOrder().map((r, i) => `('${r}', ${i})`).join(', ')}) AS RankOrder(rank, ord)
                              WHERE RankOrder.ord >= (
                                SELECT ord 
                                FROM (VALUES ${getRankOrder().map((r, i) => `('${r}', ${i})`).join(', ')}) AS RO(rank, ord)
                                WHERE RO.rank = JSON_VALUE(RegionLeague.MinRankAllowed, '$.supervisor')
                              )
                              AND RankOrder.ord <= (
                                SELECT ord 
                                FROM (VALUES ${getRankOrder().map((r, i) => `('${r}', ${i})`).join(', ')}) AS RO(rank, ord)
                                WHERE RO.rank = JSON_VALUE(RegionLeague.MaxRankAllowed, '$.supervisor')
                              )
                              AND RankOrder.rank = '${userRank.supervisor || ''}'
                            ) > 0 THEN 1
                            ELSE 0
                          END
                        ) = 1
                        AND CAST(JSON_VALUE(RegionLeague.MinRankNumberAllowed, '$.supervisor') AS FLOAT) <= ${userRankNumber.supervisor || 0}
                        AND CAST(JSON_VALUE(RegionLeague.MaxRankNumberAllowed, '$.supervisor') AS FLOAT) >= ${userRankNumber.supervisor || 0}
                      )
                      OR
                      /* Scorekeeper position check */
                      (
                        SchedulePosition.PositionId LIKE 'scorekeeper%'
                        AND (
                          SELECT CASE 
                            WHEN '${userRank.scorekeeper || ''}' = '' THEN 0
                            WHEN JSON_VALUE(RegionLeague.MinRankAllowed, '$.scorekeeper') = '' THEN 1
                            WHEN (
                              SELECT COUNT(*) 
                              FROM (VALUES ${getRankOrder().map((r, i) => `('${r}', ${i})`).join(', ')}) AS RankOrder(rank, ord)
                              WHERE RankOrder.ord >= (
                                SELECT ord 
                                FROM (VALUES ${getRankOrder().map((r, i) => `('${r}', ${i})`).join(', ')}) AS RO(rank, ord)
                                WHERE RO.rank = JSON_VALUE(RegionLeague.MinRankAllowed, '$.scorekeeper')
                              )
                              AND RankOrder.ord <= (
                                SELECT ord 
                                FROM (VALUES ${getRankOrder().map((r, i) => `('${r}', ${i})`).join(', ')}) AS RO(rank, ord)
                                WHERE RO.rank = JSON_VALUE(RegionLeague.MaxRankAllowed, '$.scorekeeper')
                              )
                              AND RankOrder.rank = '${userRank.scorekeeper || ''}'
                            ) > 0 THEN 1
                            ELSE 0
                          END
                        ) = 1
                        AND CAST(JSON_VALUE(RegionLeague.MinRankNumberAllowed, '$.scorekeeper') AS FLOAT) <= ${userRankNumber.scorekeeper || 0}
                        AND CAST(JSON_VALUE(RegionLeague.MaxRankNumberAllowed, '$.scorekeeper') AS FLOAT) >= ${userRankNumber.scorekeeper || 0}
                      )
                    )
                )
              )
            )
            OR
            /* New Condition: User has made a request for this schedule in ScheduleRequest table */
            (
              EXISTS (
                SELECT 1
                FROM ScheduleRequest SR
                WHERE SR.RegionId = Schedule.RegionId
                  AND SR.ScheduleId = Schedule.ScheduleId
                  AND SR.OfficialId = '${regionUser.Username}'
              )
            )
          )`)
        ]
      },
      order: sortingArray,
      limit,
      offset,
    });

    // Add debug logging
    console.log('Query Results:', JSON.stringify(scheduleIds, null, 2));
    console.log('User Ranks:', JSON.stringify({
      rank: userRank,
      rankNumber: userRankNumber
    }, null, 2));

    // Make sure there are at least one schedule,
    if(scheduleIds.length === 0){
      return formatSuccessResponse(_evt, { events: [], totalCount: 0 }, 200);
    }
    
    // With the list of schedule ID, we query using the SQL Query
    const SQL = getQuery(scheduleIds, (regionUser.CanViewMasterSchedule || regionUser.IsExecutive) ? null: regionUser.Username); // If the user can view the master schedule, we don't need to filter by username
    const refereeAvailabilitySQL = getRefereeAvailabilityForScheduleList(regionId, scheduleIds.map(s => s.ScheduleId), regionUser.Username);

    const sequelize = await getSequelizeObject();

    const [queryResult, refereeAvailabilityResult] = await Promise.all([
      sequelize.query(SQL, { type: sequelize.QueryTypes.SELECT }),
      sequelize.query(refereeAvailabilitySQL, { type: sequelize.QueryTypes.SELECT })
    ]);

    const formattedResult = await formatResult(queryResult, { groupParks });
    const refereeAvailabilityFormattedResult = formatRefereeAvailabilityForScheduleList(refereeAvailabilityResult);

    // Format into an object the front-end expects
    const showAssignees = regionUser.CanViewMasterSchedule || regionUser.IsExecutive;
console.log('showAssignees', showAssignees);
    let frontEndFormatted = formatRowsForFrontEnd(formattedResult, userId, regionUser.Username, userPositions, refereeAvailabilityFormattedResult, sortingArray, showAssignees);

    // Group Parks?
    if(groupParks){
      frontEndFormatted = groupSchedulesByParkId(frontEndFormatted);
    }

    return formatSuccessResponse(_evt, { events: frontEndFormatted, totalCount: scheduleIdsTotalCount }, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};

// The front-end expects an array of objects with the following properties
const formatRowsForFrontEnd = (rows, currentUserId, currentRegionUsername, userPositions, refereeAvailabilityFormattedResult, sortingArray = false, showAssignees = false) => {

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
        // Check if the position is one the user can work - if not, skip
        let positionType = 'official';
        if(position.PositionId.startsWith('scorekeeper')){
          positionType = 'scorekeeper';
        }
        else if(position.PositionId.startsWith('supervisor')){
          positionType = 'supervisor';
        }
        if(!userPositions.includes(positionType)){
          continue;
        }

        const pay = getPayForPosition(row.Pay, position.PositionId, row.CrewType);
        obj.Positions.push({
          positionName: position.PositionId,
          IsAdded: position.isAdded,
          IsModified: position.isModified,
          IsRemoved: position.isRemoved,
          positionUsers: [
            {
              OfficialId: position.OfficialId,
              FirstName: showAssignees ? position.FirstName: null,
              LastName: showAssignees ? position.LastName: null,
              Username: showAssignees ? position.Username: null,
              RealUsername: showAssignees ? position.RealUsername: null,
              Confirmed: position.Confirmed && position.Confirmed > 0 ? true: false,
              Conflicts: position.Conflicts && position.Conflicts.length > 0 ? position.Conflicts: [],
            }
          ]
        });
        if(position.RealUsername === currentUserId){
          obj.currentUserPay = pay;
        }
      }
    }

    // Check if the current user is available for the game
    const refereeAvailability = refereeAvailabilityFormattedResult.find(r => r.ScheduleId === obj.ScheduleId);
    obj.IsCurrentUserAvailable = false; // Default to false
    if(refereeAvailability){
      obj.IsCurrentUserAvailable = refereeAvailability.IsAvailable === 1; // Convert to boolean
    }

    obj.RegionUsername = currentRegionUsername;

    formattedResult.push(obj);
  }

  if(sortingArray &&sortingArray.length > 0){
    const firstSort = sortingArray[0];
    formattedResult = sortArrayByProperty(formattedResult, firstSort[0], firstSort[1]);
  }

  return formattedResult;
};
