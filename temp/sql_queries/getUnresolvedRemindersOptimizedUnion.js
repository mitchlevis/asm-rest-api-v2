export const getQuery = (sortDirection, { userId, limit, offset }) => {
  // sortDirection already validated to 'ASC'|'DESC'
  const orderDir = sortDirection === 'DESC' ? 'DESC' : 'ASC';

  const sql = `
WITH DistinctPositions AS (
  SELECT DISTINCT sp.RegionId, sp.ScheduleId, sp.OfficialId, sp.PositionId
  FROM SchedulePosition sp
),
Positions AS (
  SELECT dp.RegionId, dp.ScheduleId, dp.OfficialId,
         STRING_AGG(CAST(dp.PositionId AS varchar(20)), ',') WITHIN GROUP (ORDER BY dp.PositionId) AS Positions
  FROM DistinctPositions dp
  GROUP BY dp.RegionId, dp.ScheduleId, dp.OfficialId
),
DistinctPositionsV AS (
  SELECT DISTINCT spv.RegionId, spv.ScheduleId, spv.OfficialId, spv.PositionId
  FROM SchedulePositionVersion spv
),
PositionsV AS (
  SELECT dpv.RegionId, dpv.ScheduleId, dpv.OfficialId,
         STRING_AGG(CAST(dpv.PositionId AS varchar(20)), ',') WITHIN GROUP (ORDER BY dpv.PositionId) AS Positions
  FROM DistinctPositionsV dpv
  GROUP BY dpv.RegionId, dpv.ScheduleId, dpv.OfficialId
),
LatestVersion AS (
  SELECT RegionId, ScheduleId, VersionId
  FROM (
    SELECT sv.RegionId, sv.ScheduleId, sv.VersionId,
           ROW_NUMBER() OVER (PARTITION BY sv.RegionId, sv.ScheduleId ORDER BY sv.VersionId DESC) AS rn
    FROM ScheduleVersion sv
  ) x
  WHERE rn = 1
),
LastConfirm AS (
  SELECT sc.RegionId, sc.ScheduleId, sc.Username,
         MAX(sc.DateAdded) AS LastConfirmed
  FROM ScheduleConfirm sc
  GROUP BY sc.RegionId, sc.ScheduleId, sc.Username
),
NewNotif AS (
  SELECT DISTINCT
    s.RegionId,
    s.ScheduleId,
    s.VersionId,
    s.GameType,
    s.GameNumber,
    s.LeagueId,
    rl.LeagueName,
    ru.Username AS RegionUsername,
    pAgg.Positions,
    s.GameDate,
    r.TimeZone,
    s.GameStatus,
    r.Sport,
    CASE WHEN p.ParkName IS NOT NULL THEN p.ParkName ELSE s.ParkId END AS ParkName,
    CASE WHEN ht.TeamName IS NOT NULL THEN ht.TeamName ELSE s.HomeTeam END AS HomeTeam,
    CASE WHEN at.TeamName IS NOT NULL THEN at.TeamName ELSE s.AwayTeam END AS AwayTeam,
    'new' AS NotificationType
  FROM [User] u
  INNER JOIN RegionUser ru ON u.Username = ru.RealUsername
  INNER JOIN SchedulePosition sp ON ru.Username = sp.OfficialId AND ru.RegionId = sp.RegionId
  INNER JOIN Schedule s ON sp.ScheduleId = s.ScheduleId AND sp.RegionId = s.RegionId
  INNER JOIN Region r ON s.RegionId = r.RegionId
  LEFT JOIN Positions pAgg ON pAgg.RegionId = s.RegionId AND pAgg.ScheduleId = s.ScheduleId AND pAgg.OfficialId = ru.Username
  LEFT JOIN RegionLeague rl ON rl.RegionId = s.RegionId AND rl.LeagueId = s.LeagueId
  LEFT JOIN ScheduleConfirm sc ON s.ScheduleId = sc.ScheduleId AND s.RegionId = sc.RegionId AND ru.Username = sc.Username
  LEFT JOIN [Park] p ON p.RegionId = s.RegionId AND p.ParkId = s.ParkId
  LEFT JOIN [Team] ht ON s.RegionId = ht.RegionId AND s.HomeTeam = ht.TeamId
  LEFT JOIN [Team] at ON s.RegionId = at.RegionId AND s.AwayTeam = at.TeamId
  WHERE u.Username = :userId
    AND sc.ScheduleId IS NULL
    AND (s.GameStatus IS NULL OR s.GameStatus = '')
    AND s.GameDate > CURRENT_TIMESTAMP
),
ChangedNotif AS (
  SELECT DISTINCT
    sv.RegionId,
    sv.ScheduleId,
    sv.VersionId,
    sv.GameType,
    sv.GameNumber,
    sv.LeagueId,
    rl.LeagueName,
    ru.Username AS RegionUsername,
    pvAgg.Positions,
    sv.GameDate,
    r.TimeZone,
    sv.GameStatus,
    r.Sport,
    CASE WHEN p.ParkName IS NOT NULL THEN p.ParkName ELSE sv.ParkId END AS ParkName,
    CASE WHEN ht.TeamName IS NOT NULL THEN ht.TeamName ELSE sv.HomeTeam END AS HomeTeam,
    CASE WHEN at.TeamName IS NOT NULL THEN at.TeamName ELSE sv.AwayTeam END AS AwayTeam,
    'changed' AS NotificationType
  FROM [User] u
  INNER JOIN RegionUser ru ON u.Username = ru.RealUsername
  INNER JOIN SchedulePositionVersion spv ON ru.RegionId = spv.RegionId AND ru.Username = spv.OfficialId
  INNER JOIN Region r ON spv.RegionId = r.RegionId
  INNER JOIN LatestVersion lsv ON spv.ScheduleId = lsv.ScheduleId AND spv.RegionId = lsv.RegionId
  INNER JOIN ScheduleVersion sv ON lsv.ScheduleId = sv.ScheduleId AND lsv.RegionId = sv.RegionId AND lsv.VersionId = sv.VersionId
  LEFT JOIN RegionLeague rl ON rl.RegionId = sv.RegionId AND rl.LeagueId = sv.LeagueId
  LEFT JOIN ScheduleConfirm sc ON sv.ScheduleId = sc.ScheduleId AND sv.RegionId = sc.RegionId AND sv.VersionId = sc.VersionId AND ru.Username = sc.Username
  LEFT JOIN LastConfirm lc ON lc.RegionId = sv.RegionId AND lc.ScheduleId = sv.ScheduleId AND lc.Username = ru.Username
  LEFT JOIN PositionsV pvAgg ON pvAgg.RegionId = sv.RegionId AND pvAgg.ScheduleId = sv.ScheduleId AND pvAgg.OfficialId = ru.Username
  LEFT JOIN [Park] p ON p.RegionId = sv.RegionId AND p.ParkId = sv.ParkId
  LEFT JOIN [Team] ht ON sv.RegionId = ht.RegionId AND sv.HomeTeam = ht.TeamId
  LEFT JOIN [Team] at ON sv.RegionId = at.RegionId AND sv.AwayTeam = at.TeamId
  WHERE u.Username = :userId
    AND sc.Username IS NULL
    AND (sv.GameStatus IS NULL OR sv.GameStatus = '')
    AND sv.IsDeleted = 0
    AND EXISTS (
      SELECT 1 FROM ScheduleConfirm sc2
      WHERE sc2.RegionId = sv.RegionId AND sc2.ScheduleId = sv.ScheduleId AND sc2.Username = ru.Username
    )
    AND sv.DateAdded > COALESCE(lc.LastConfirmed, '1900-01-01')
    AND sv.GameDate > CURRENT_TIMESTAMP
),
CancelledNotif AS (
  SELECT DISTINCT
    sv.RegionId,
    sv.ScheduleId,
    sv.VersionId,
    sv.GameType,
    sv.GameNumber,
    sv.LeagueId,
    rl.LeagueName,
    ru.Username AS RegionUsername,
    pvAgg.Positions,
    sv.GameDate,
    r.TimeZone,
    sv.GameStatus,
    r.Sport,
    CASE WHEN p.ParkName IS NOT NULL THEN p.ParkName ELSE sv.ParkId END AS ParkName,
    CASE WHEN ht.TeamName IS NOT NULL THEN ht.TeamName ELSE sv.HomeTeam END AS HomeTeam,
    CASE WHEN at.TeamName IS NOT NULL THEN at.TeamName ELSE sv.AwayTeam END AS AwayTeam,
    'cancelled' AS NotificationType
  FROM [User] u
  INNER JOIN RegionUser ru ON u.Username = ru.RealUsername
  INNER JOIN SchedulePositionVersion spv ON ru.RegionId = spv.RegionId AND ru.Username = spv.OfficialId
  INNER JOIN Region r ON spv.RegionId = r.RegionId
  INNER JOIN LatestVersion lsv ON spv.ScheduleId = lsv.ScheduleId AND spv.RegionId = lsv.RegionId
  INNER JOIN ScheduleVersion sv ON lsv.ScheduleId = sv.ScheduleId AND lsv.RegionId = sv.RegionId AND lsv.VersionId = sv.VersionId
  LEFT JOIN RegionLeague rl ON rl.RegionId = sv.RegionId AND rl.LeagueId = sv.LeagueId
  LEFT JOIN ScheduleConfirm sc ON sv.ScheduleId = sc.ScheduleId AND sv.RegionId = sc.RegionId AND sv.VersionId = sc.VersionId AND ru.Username = sc.Username
  LEFT JOIN PositionsV pvAgg ON pvAgg.RegionId = sv.RegionId AND pvAgg.ScheduleId = sv.ScheduleId AND pvAgg.OfficialId = ru.Username
  LEFT JOIN [Park] p ON p.RegionId = sv.RegionId AND p.ParkId = sv.ParkId
  LEFT JOIN [Team] ht ON sv.RegionId = ht.RegionId AND sv.HomeTeam = ht.TeamId
  LEFT JOIN [Team] at ON sv.RegionId = at.RegionId AND sv.AwayTeam = at.TeamId
  WHERE u.Username = :userId
    AND sc.Username IS NULL
    AND (
      (sv.GameStatus IS NOT NULL AND sv.GameStatus <> '')
      OR sv.IsDeleted = 1
    )
    AND sv.GameDate > CURRENT_TIMESTAMP
),
Unioned AS (
  SELECT * FROM NewNotif
  UNION ALL
  SELECT * FROM ChangedNotif
  UNION ALL
  SELECT * FROM CancelledNotif
),
Ranked AS (
  SELECT *,
         ROW_NUMBER() OVER (PARTITION BY NotificationType ORDER BY GameDate ${orderDir}) AS rn
  FROM Unioned
)
SELECT RegionId,
       ScheduleId,
       VersionId,
       GameType,
       GameNumber,
       LeagueId,
       LeagueName,
       RegionUsername,
       Positions,
       GameDate,
       TimeZone,
       GameStatus,
       Sport,
       CASE 
         WHEN LOWER(Sport) IN ('basketball','volleyball') THEN 'court'
         WHEN LOWER(Sport) IN ('hockey') THEN 'arena'
         ELSE 'field'
       END AS LocationType,
       ParkName,
       HomeTeam,
       AwayTeam,
       NotificationType,
       rn
FROM Ranked
WHERE rn BETWEEN (:offset + 1) AND (:offset + :limit)
ORDER BY NotificationType, GameDate ${orderDir};
`;

  const replacements = { userId, limit, offset };
  return { sql, replacements };
};


