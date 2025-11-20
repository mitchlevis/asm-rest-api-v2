
export const getQuery = (userId, sortDirection) => {
	
  return `
SELECT DISTINCT
  s.RegionId,
  s.ScheduleId,
  s.VersionId,
  s.GameType,
  s.GameNumber,
  s.LeagueId,
  rl.LeagueName,
  ru.Username AS 'RegionUsername',
  Positions = STUFF((SELECT ',' + isp.PositionId FROM SchedulePosition isp WHERE s.RegionId = isp.regionId AND s.scheduleId = isp.scheduleId AND ru.Username = isp.OfficialId FOR XML PATH('')), 1, 1, ''), -- Since a User can have multiple positions, we need to concatenate them into a csv string
  s.GameDate,
  r.TimeZone,
  s.GameStatus,
  r.Sport,
  CASE WHEN p.ParkName IS NOT NULL THEN p.ParkName ELSE s.ParkId END AS ParkName,
  CASE WHEN ht.TeamName IS NOT NULL THEN ht.TeamName ELSE s.HomeTeam END AS HomeTeam,
  CASE WHEN at.TeamName IS NOT NULL THEN at.TeamName ELSE s.AwayTeam END AS AwayTeam,
  'new' AS NotificationType
FROM
  [User] u
  INNER JOIN RegionUser ru ON u.Username = ru.RealUsername
  INNER JOIN SchedulePosition sp ON ru.Username = sp.OfficialId
  INNER JOIN Schedule s ON sp.ScheduleId = s.ScheduleId AND sp.RegionId = s.RegionId
  INNER JOIN Region r ON s.RegionId = r.RegionId
  LEFT JOIN RegionLeague rl ON rl.RegionId = s.RegionId AND rl.LeagueId = s.LeagueId
  LEFT JOIN ScheduleConfirm sc ON s.ScheduleId = sc.ScheduleId AND s.RegionId = sc.RegionId AND ru.Username = sc.Username
  LEFT JOIN [Park] p ON p.RegionId = s.RegionId AND p.ParkId = s.ParkId
  LEFT JOIN [Team] ht ON s.RegionId = ht.RegionId AND s.HomeTeam = ht.TeamId
  LEFT JOIN [Team] at ON s.RegionId = at.RegionId AND s.AwayTeam = at.TeamId
WHERE
  u.Username = '${userId}'
  AND sc.ScheduleId IS NULL -- This ensures that there's no confirmation for this game by the user
  AND (s.GameStatus IS NULL OR s.GameStatus = '') -- Game was not cancelled
  AND s.GameDate > CURRENT_TIMESTAMP -- This ensures only future games are considered
ORDER BY
  s.GameDate ${sortDirection};
  `;
}

export const formatResult = (result) => {
  // Format Positions into an array
  for(const row of result){
    row.Positions = row?.Positions?.split(',') ?? [];
  }

  // Add LocationType to the result
  for(const row of result){
    switch(row.Sport.toLowerCase()){
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
