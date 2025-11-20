
export const getQuery = (userId, sortDirection) => {

  return `
WITH LatestScheduleVersion AS (
    SELECT
        sv.RegionId,
        sv.ScheduleId,
        MAX(sv.VersionId) AS LatestVersionId
    FROM
        ScheduleVersion sv
    GROUP BY
        sv.RegionId, sv.ScheduleId
)
SELECT DISTINCT
    sv.RegionId,
    sv.ScheduleId,
    sv.VersionId,
    sv.GameType,
    sv.GameNumber,
    sv.LeagueId,
    rl.LeagueName,
    ru.Username AS 'RegionUsername',
    Positions = STUFF((SELECT ',' + isp.PositionId FROM SchedulePosition isp WHERE spv.RegionId = isp.regionId AND spv.scheduleId = isp.scheduleId AND ru.Username = isp.OfficialId FOR XML PATH('')), 1, 1, ''), -- Since a User can have multiple positions, we need to concatenate them into a csv string
    sv.GameDate,
    r.TimeZone,
    sv.GameStatus,
    r.Sport,
    CASE WHEN p.ParkName IS NOT NULL THEN p.ParkName ELSE sv.ParkId END AS ParkName,
    CASE WHEN ht.TeamName IS NOT NULL THEN ht.TeamName ELSE sv.HomeTeam END AS HomeTeam,
    CASE WHEN at.TeamName IS NOT NULL THEN at.TeamName ELSE sv.AwayTeam END AS AwayTeam,
    'changed' AS NotificationType
FROM
    [User] U
    INNER JOIN RegionUser ru ON U.Username = ru.RealUsername
    INNER JOIN SchedulePositionVersion spv ON ru.RegionId = spv.RegionId AND ru.Username = spv.OfficialId
    INNER JOIN Region r ON spv.RegionId = r.RegionId
    INNER JOIN LatestScheduleVersion lsv ON spv.ScheduleId = lsv.ScheduleId AND spv.RegionId = lsv.RegionId
    INNER JOIN ScheduleVersion sv ON lsv.ScheduleId = sv.ScheduleId AND lsv.RegionId = sv.RegionId AND lsv.LatestVersionId = sv.VersionId
    LEFT JOIN RegionLeague rl ON rl.RegionId = sv.RegionId AND rl.LeagueId = sv.LeagueId
    LEFT JOIN ScheduleConfirm sc ON sv.ScheduleId = sc.ScheduleId AND sv.RegionId = sc.RegionId AND sv.VersionId = sc.VersionId AND ru.Username = sc.Username
    LEFT JOIN ScheduleBookOff sbo ON sv.ScheduleId = sbo.ScheduleId AND sv.RegionId = sbo.RegionId AND ru.Username = sbo.Username
    LEFT JOIN [Park] p ON p.RegionId = sv.RegionId AND p.ParkId = sv.ParkId
    LEFT JOIN [Team] ht ON sv.RegionId = ht.RegionId AND sv.HomeTeam = ht.TeamId
    LEFT JOIN [Team] at ON sv.RegionId = at.RegionId AND sv.AwayTeam = at.TeamId
WHERE
    U.Username = '${userId}'
    AND sc.Username IS NULL -- Only unconfirmed changes
    AND sbo.Username IS NULL -- Ommit games where the user is booked off
    AND (sv.GameStatus IS NULL OR sv.GameStatus = '') -- Game was not cancelled
    AND sv.IsDeleted = 0 -- Exclude soft-deleted games
    AND EXISTS (
        SELECT 1
        FROM ScheduleConfirm sc2
        WHERE sc2.RegionId = sv.RegionId AND sc2.ScheduleId = sv.ScheduleId AND sc2.Username = RU.Username
    ) -- Ensures there's at least one confirmation for the schedule by the user
    AND sv.DateAdded > COALESCE(
        (SELECT MAX(DateAdded)
         FROM ScheduleConfirm sc3
         WHERE sc3.ScheduleId = sv.ScheduleId AND sc3.Username = RU.Username),
        '1900-01-01' -- Default old date for no prior confirmation
    )
    AND sv.GameDate > CURRENT_TIMESTAMP -- This ensures only future games are considered
ORDER BY
    sv.GameDate ${sortDirection};
  `;
}

export const formatResult = (result) => {
  // Format Positions into an array
  for (const row of result) {
    row.Positions = row?.Positions?.split(',') ?? [];
  }

  // Add LocationType to the result
  for (const row of result) {
    switch (row.Sport.toLowerCase()) {
      case 'baseball':
        row.LocationType = 'field';
        break;
      case 'basketball':
        row.LocationType = 'court';
        break;
      case 'football':
        row.LocationType = 'field';
        break;
      case 'hockey':
        row.LocationType = 'arena';
        break;
      case 'soccer':
        row.LocationType = 'field';
        break;
      case 'softball':
        row.LocationType = 'field';
        break;
      case 'volleyball':
        row.LocationType = 'court';
        break;
      default:
        row.LocationType = 'field';
    }
  }
  return result;
};
