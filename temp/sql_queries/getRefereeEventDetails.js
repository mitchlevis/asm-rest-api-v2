
export const getQuery = (regionId, scheduleId) => {
	
  return `
SELECT TOP 1
    s.*,
    reg.RegionName,
    rl.LeagueName,
    homet.TeamName as 'HomeTeamName',
    awayt.TeamName as 'AwayTeamName',
    p.ParkName
    
FROM Schedule s
  INNER JOIN [Region] reg ON reg.RegionID = s.RegionId
  LEFT JOIN [RegionLeague] rl ON rl.RegionId = s.RegionId AND rl.LeagueId = s.LeagueId
  LEFT JOIN [Team] homet ON homet.RegionId = s.RegionId AND homet.TeamId = s.HomeTeam
	LEFT JOIN [Team] awayt ON awayt.RegionId = s.RegionId AND awayt.TeamId = s.AwayTeam
  LEFT JOIN [Park] p ON p.RegionId = s.RegionId AND p.ParkId = s.ParkId
WHERE
    s.RegionId = '${regionId}'
    AND s.ScheduleId = ${scheduleId}
  `;
}

export const formatResult = (result) => {
};
