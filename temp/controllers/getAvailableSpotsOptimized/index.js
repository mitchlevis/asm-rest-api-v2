import Sequelize from 'sequelize';
const Op = Sequelize.Op;
const literal = Sequelize.literal;
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, getFilterQueryParameter, throwError, getDbObject, getSequelizeObject, sortArrayByProperty, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { getRankOrder, groupSchedulesByParkId, getLocationType, getPayForPosition } from "@asportsmanager-api/core/business-logic-helpers";
import { getQuery as getAvailableSpotsQuery, formatResult as formatAvailableSpotsResult } from "@asportsmanager-api/core/sql_queries/getAvailableSpotsByRegion";
import { getQuery as getRefereeAvailabilityForScheduleList, formatResult as formatRefereeAvailabilityForScheduleList } from "@asportsmanager-api/core/sql_queries/getRefereeAvailabilityForScheduleList";

// Helper: derive position type
const getPositionType = (positionId) => {
  if (!positionId) return 'official';
  if (positionId.startsWith('scorekeeper')) return 'scorekeeper';
  if (positionId.startsWith('supervisor')) return 'supervisor';
  return 'official';
};

// Helper: rank eligibility check for a single type
const isRankEligible = (type, leagueConstraints, userRankForType, userRankNumForType) => {
  if (!leagueConstraints) return true; // no constraints -> allow
  const minRankAllowed = leagueConstraints.MinRankAllowed && leagueConstraints.MinRankAllowed[type] !== undefined ? leagueConstraints.MinRankAllowed[type] : '';
  const maxRankAllowed = leagueConstraints.MaxRankAllowed && leagueConstraints.MaxRankAllowed[type] !== undefined ? leagueConstraints.MaxRankAllowed[type] : '';
  const minRankNumAllowed = leagueConstraints.MinRankNumberAllowed && leagueConstraints.MinRankNumberAllowed[type] !== undefined ? parseFloat(leagueConstraints.MinRankNumberAllowed[type]) : undefined;
  const maxRankNumAllowed = leagueConstraints.MaxRankNumberAllowed && leagueConstraints.MaxRankNumberAllowed[type] !== undefined ? parseFloat(leagueConstraints.MaxRankNumberAllowed[type]) : undefined;

  const hasAnyConstraint = (minRankAllowed && minRankAllowed !== '') || (maxRankAllowed && maxRankAllowed !== '') ||
    (minRankNumAllowed !== undefined && !Number.isNaN(minRankNumAllowed)) || (maxRankNumAllowed !== undefined && !Number.isNaN(maxRankNumAllowed));
  if (!hasAnyConstraint) return true; // unconstrained => allow

  const rankOrder = getRankOrder();
  if (!userRankForType || userRankForType === '') return false; // constrained but user has no rank

  const userOrd = rankOrder.indexOf((userRankForType || '').toLowerCase());
  if (userOrd === -1) return false;

  let minOrd = undefined;
  let maxOrd = undefined;
  if (minRankAllowed && minRankAllowed !== '') minOrd = rankOrder.indexOf((minRankAllowed || '').toLowerCase());
  if (maxRankAllowed && maxRankAllowed !== '') maxOrd = rankOrder.indexOf((maxRankAllowed || '').toLowerCase());

  const ordOk = (minOrd === undefined || userOrd >= minOrd) && (maxOrd === undefined || userOrd <= maxOrd);

  let numOk = true;
  if (typeof userRankNumForType === 'number' && !Number.isNaN(userRankNumForType)) {
    if (minRankNumAllowed !== undefined && !Number.isNaN(minRankNumAllowed)) {
      numOk = numOk && userRankNumForType >= minRankNumAllowed;
    }
    if (maxRankNumAllowed !== undefined && !Number.isNaN(maxRankNumAllowed)) {
      numOk = numOk && userRankNumForType <= maxRankNumAllowed;
    }
  }

  return ordOk && numOk;
};

// Helper: build VALUES list for MSSQL NVARCHAR identifiers
const toValuesList = (values) => {
  if (!values || values.length === 0) return "(N'')";
  return values.map(v => `(N'${String(v).replace(/'/g, "''")}')`).join(',');
};

// Local copy of the existing formatter to avoid dynamic import issues
const formatRowsForFrontEndOptimized = (rows, currentUserId, currentRegionUsername, userPositions, refereeAvailabilityFormattedResult, sortingArray = false, showAssignees = false) => {
  let formattedResult = [];
  for(const row of rows){
    const obj = { ...row };

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
        if(position.PositionId && position.PositionId.startsWith('scorekeeper')){
          positionType = 'scorekeeper';
        }
        else if(position.PositionId && position.PositionId.startsWith('supervisor')){
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

export const handler = async (_evt) => {
  try{
    const tokenData = await authenticateSessionToken(_evt);
    const realUsername = tokenData.UserName;

    // Validate Parameters
    const { path, query } = await validateIncomingParameters(_evt, parameters);

    const { regionId } = path; // optional
    const filter = getFilterQueryParameter(_evt.queryStringParameters);
    const sort = query.sort.split(',');
    const sortDirection = query.sort_direction;
    const limit = query.limit;
    const offset = query.offset;
    const groupParks = query.group_parks;

    // Load RegionUser scope for this user
    const regionUserModel = await getDbObject('RegionUser');
    const whereRU = regionId ? { RealUsername: realUsername, RegionId: regionId } : { RealUsername: realUsername, IsArchived: 0 };
    const regionUsers = await regionUserModel.findAll({ where: whereRU, raw: true });

    if(!regionUsers || regionUsers.length === 0){
      await throwError(403, `User does not have access to ${regionId ? 'this': 'these'} region${regionId ? '': 's'}`);
    }

    // Build per-region maps and union positions
    const regionIdSet = new Set(regionUsers.map(r => r.RegionId));
    const regionIdList = Array.from(regionIdSet);
    const regionIdValuesList = toValuesList(regionIdList);
    const regionUsernameList = regionUsers.map(r => r.Username).filter(Boolean);
    const regionUsernameValuesList = toValuesList(regionUsernameList);

    const regionUserByRegion = new Map();
    const userPositionsSet = new Set();
    for(const ru of regionUsers){
      regionUserByRegion.set(ru.RegionId, ru);
      try{
        const positions = JSON.parse(ru.Positions || '[]');
        positions.forEach(p => userPositionsSet.add(p));
      } catch(err){ /* ignore */ }
    }
    const userPositions = Array.from(userPositionsSet);
    const hasOfficial = userPositions.includes('official') ? 1 : 0;
    const hasSupervisor = userPositions.includes('supervisor') ? 1 : 0;
    const hasScorekeeper = userPositions.includes('scorekeeper') ? 1 : 0;

    // Build raw ID query (region-scoped, open spots or user request, date window)
    const sequelize = await getSequelizeObject();
    const orderBy = 's.GameDate';
    const direction = sortDirection === 'ASC' ? 'ASC' : 'DESC';

    const idSQL = `
      WITH UserRegions AS (
        SELECT RegionId FROM (VALUES ${regionIdValuesList}) AS UR(RegionId)
      ),
      RegionUsernames AS (
        SELECT Username FROM (VALUES ${regionUsernameValuesList}) AS RU(Username)
      ),
      RankOrder(rank, ord) AS (
        SELECT * FROM (VALUES ${getRankOrder().map((r, i) => `('` + r + `', ${i})`).join(', ')}) AS RO(rank, ord)
      )
      SELECT 
        s.RegionId,
        s.ScheduleId,
        s.GameDate,
        s.LeagueId,
        COUNT(*) OVER() AS TotalCount
      FROM Schedule s
      JOIN UserRegions ur ON ur.RegionId = s.RegionId
      JOIN Region r ON r.RegionID = s.RegionId
      JOIN RegionUser ru ON ru.RegionId = s.RegionId AND ru.RealUsername = :realUsername
      WHERE CAST(s.GameDate AS DATE) BETWEEN CAST(GETDATE() AS DATE) AND COALESCE(r.MaxDateShowAvailableSpots, '9999-12-31')
        AND (
          EXISTS (
            SELECT 1 FROM SchedulePosition sp
            LEFT JOIN RegionLeague rl ON rl.RegionId = s.RegionId AND rl.LeagueId = s.LeagueId
            WHERE sp.RegionId = s.RegionId
              AND sp.ScheduleId = s.ScheduleId
              AND sp.OfficialId = ''
              AND (
                /* Official type */
                (
                  :hasOfficial = 1 AND sp.PositionId NOT LIKE 'scorekeeper%' AND sp.PositionId NOT LIKE 'supervisor%'
                  AND (
                    CASE 
                      WHEN JSON_VALUE(ru.Rank, '$.official') = '' THEN 0
                      WHEN JSON_VALUE(rl.MinRankAllowed, '$.official') = '' THEN 1
                      WHEN (
                        SELECT COUNT(*) FROM RankOrder ro
                        WHERE ro.ord >= (SELECT ord FROM RankOrder WHERE rank = JSON_VALUE(rl.MinRankAllowed, '$.official'))
                          AND ro.ord <= (SELECT ord FROM RankOrder WHERE rank = JSON_VALUE(rl.MaxRankAllowed, '$.official'))
                          AND ro.rank = JSON_VALUE(ru.Rank, '$.official')
                      ) > 0 THEN 1
                      ELSE 0
                    END
                  ) = 1
                  AND CAST(JSON_VALUE(rl.MinRankNumberAllowed, '$.official') AS FLOAT) <= CAST(JSON_VALUE(ru.RankNumber, '$.official') AS FLOAT)
                  AND CAST(JSON_VALUE(rl.MaxRankNumberAllowed, '$.official') AS FLOAT) >= CAST(JSON_VALUE(ru.RankNumber, '$.official') AS FLOAT)
                )
                OR
                /* Supervisor type */
                (
                  :hasSupervisor = 1 AND sp.PositionId LIKE 'supervisor%'
                  AND (
                    CASE 
                      WHEN JSON_VALUE(ru.Rank, '$.supervisor') = '' THEN 0
                      WHEN JSON_VALUE(rl.MinRankAllowed, '$.supervisor') = '' THEN 1
                      WHEN (
                        SELECT COUNT(*) FROM RankOrder ro
                        WHERE ro.ord >= (SELECT ord FROM RankOrder WHERE rank = JSON_VALUE(rl.MinRankAllowed, '$.supervisor'))
                          AND ro.ord <= (SELECT ord FROM RankOrder WHERE rank = JSON_VALUE(rl.MaxRankAllowed, '$.supervisor'))
                          AND ro.rank = JSON_VALUE(ru.Rank, '$.supervisor')
                      ) > 0 THEN 1
                      ELSE 0
                    END
                  ) = 1
                  AND CAST(JSON_VALUE(rl.MinRankNumberAllowed, '$.supervisor') AS FLOAT) <= CAST(JSON_VALUE(ru.RankNumber, '$.supervisor') AS FLOAT)
                  AND CAST(JSON_VALUE(rl.MaxRankNumberAllowed, '$.supervisor') AS FLOAT) >= CAST(JSON_VALUE(ru.RankNumber, '$.supervisor') AS FLOAT)
                )
                OR
                /* Scorekeeper type */
                (
                  :hasScorekeeper = 1 AND sp.PositionId LIKE 'scorekeeper%'
                  AND (
                    CASE 
                      WHEN JSON_VALUE(ru.Rank, '$.scorekeeper') = '' THEN 0
                      WHEN JSON_VALUE(rl.MinRankAllowed, '$.scorekeeper') = '' THEN 1
                      WHEN (
                        SELECT COUNT(*) FROM RankOrder ro
                        WHERE ro.ord >= (SELECT ord FROM RankOrder WHERE rank = JSON_VALUE(rl.MinRankAllowed, '$.scorekeeper'))
                          AND ro.ord <= (SELECT ord FROM RankOrder WHERE rank = JSON_VALUE(rl.MaxRankAllowed, '$.scorekeeper'))
                          AND ro.rank = JSON_VALUE(ru.Rank, '$.scorekeeper')
                      ) > 0 THEN 1
                      ELSE 0
                    END
                  ) = 1
                  AND CAST(JSON_VALUE(rl.MinRankNumberAllowed, '$.scorekeeper') AS FLOAT) <= CAST(JSON_VALUE(ru.RankNumber, '$.scorekeeper') AS FLOAT)
                  AND CAST(JSON_VALUE(rl.MaxRankNumberAllowed, '$.scorekeeper') AS FLOAT) >= CAST(JSON_VALUE(ru.RankNumber, '$.scorekeeper') AS FLOAT)
                )
              )
          )
          OR EXISTS (
            SELECT 1 FROM ScheduleRequest sr
            WHERE sr.RegionId = s.RegionId AND sr.ScheduleId = s.ScheduleId AND sr.OfficialId IN (SELECT Username FROM RegionUsernames)
          )
        )
      ORDER BY ${orderBy} ${direction}
      OFFSET :offset ROWS FETCH NEXT :limit ROWS ONLY;
    `;

    const idRows = await sequelize.query(idSQL, {
      type: Sequelize.QueryTypes.SELECT,
      replacements: { hasOfficial, hasSupervisor, hasScorekeeper, offset, limit, realUsername }
    });

    if(!idRows || idRows.length === 0){
      return formatSuccessResponse(_evt, { events: [], totalCount: 0 }, 200);
    }

    const totalCount = idRows[0]?.TotalCount ?? idRows.length;
    const ids = idRows.map(r => ({ RegionId: r.RegionId, ScheduleId: r.ScheduleId }));
    console.log('[OPT] userPositions', userPositions);
    console.log('[OPT] ID rows count', idRows.length, 'totalCount', totalCount);
    console.log('[OPT] First IDs', ids.slice(0, 5));

    // Fetch details for selected IDs
    const [detailRowsRaw] = await Promise.all([
      (async () => {
        const sql = getAvailableSpotsQuery(ids, null);
        const res = await sequelize.query(sql, { type: sequelize.QueryTypes.SELECT });
        return formatAvailableSpotsResult(res);
      })()
    ]);
    console.log('[OPT] Detail rows count', detailRowsRaw?.length || 0);

    // Fetch availability per region in parallel
    const idsByRegion = ids.reduce((acc, row) => {
      acc[row.RegionId] = acc[row.RegionId] || [];
      acc[row.RegionId].push(row.ScheduleId);
      return acc;
    }, {});

    const availabilityTasks = Object.entries(idsByRegion).map(async ([rid, scheduleIds]) => {
      const ru = regionUserByRegion.get(rid);
      const regionUsername = ru?.Username || '';
      const sql = getRefereeAvailabilityForScheduleList(rid, scheduleIds, regionUsername);
      const res = await sequelize.query(sql, { type: sequelize.QueryTypes.SELECT });
      return formatRefereeAvailabilityForScheduleList(res);
    });
    const availabilityArrays = await Promise.all(availabilityTasks);
    const availabilityFlat = availabilityArrays.flat();
    console.log('[OPT] Availability rows', availabilityFlat.length);

    // JS rank gating not needed if SQL already applied gating; but we still need RL constraints for formatting pay display
    const leaguePairs = new Set();
    for(const row of detailRowsRaw){
      if(row.LeagueId && row.LeagueId !== ''){
        leaguePairs.add(`${row.RegionId}::${row.LeagueId}`);
      }
    }
    const leagueRegionIds = Array.from(new Set(Array.from(leaguePairs).map(k => k.split('::')[0])));
    const leagueIds = Array.from(new Set(Array.from(leaguePairs).map(k => k.split('::')[1])));

    let regionLeagueConstraintsMap = new Map();
    if(leaguePairs.size > 0){
      const regionLeagueModel = await getDbObject('RegionLeague');
      const rlRows = await regionLeagueModel.findAll({
        where: { RegionId: { [Op.in]: leagueRegionIds }, LeagueId: { [Op.in]: leagueIds } },
        attributes: ['RegionId', 'LeagueId', 'MinRankAllowed', 'MaxRankAllowed', 'MinRankNumberAllowed', 'MaxRankNumberAllowed'],
        raw: true
      });
      for(const rl of rlRows){
        let minRankAllowed = {};
        let maxRankAllowed = {};
        let minRankNumberAllowed = {};
        let maxRankNumberAllowed = {};
        try{ minRankAllowed = JSON.parse(rl.MinRankAllowed || '{}'); } catch(e){}
        try{ maxRankAllowed = JSON.parse(rl.MaxRankAllowed || '{}'); } catch(e){}
        try{ minRankNumberAllowed = JSON.parse(rl.MinRankNumberAllowed || '{}'); } catch(e){}
        try{ maxRankNumberAllowed = JSON.parse(rl.MaxRankNumberAllowed || '{}'); } catch(e){}
        regionLeagueConstraintsMap.set(`${rl.RegionId}::${rl.LeagueId}`, {
          MinRankAllowed: minRankAllowed,
          MaxRankAllowed: maxRankAllowed,
          MinRankNumberAllowed: minRankNumberAllowed,
          MaxRankNumberAllowed: maxRankNumberAllowed
        });
      }
    }
    console.log('[OPT] League constraints count', regionLeagueConstraintsMap.size);

    // Since rank gating is applied in SQL, do not gate again in JS
    const filteredDetails = detailRowsRaw;

    const rowsByRegion = filteredDetails.reduce((acc, r) => {
      acc[r.RegionId] = acc[r.RegionId] || [];
      acc[r.RegionId].push(r);
      return acc;
    }, {});

    let finalRows = [];
    for(const [rid, rows] of Object.entries(rowsByRegion)){
      const ru = regionUserByRegion.get(rid);
      const showAssignees = !!(ru?.CanViewMasterSchedule || ru?.IsExecutive);
      let regionRows = formatRowsForFrontEndOptimized(rows, realUsername, ru?.Username || '', userPositions, availabilityFlat, sort && sort.length > 0 ? [[sort[0], sortDirection]] : [], showAssignees);
      if(groupParks){
        regionRows = groupSchedulesByParkId(regionRows);
      }
      finalRows = finalRows.concat(regionRows);
    }

    // Global sorting if requested
    if(sort && sort.length > 0){
      const firstSort = [sort[0], sortDirection];
      finalRows = sortArrayByProperty(finalRows, firstSort[0], firstSort[1]);
    }

    return formatSuccessResponse(_evt, { events: finalRows, totalCount }, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};


