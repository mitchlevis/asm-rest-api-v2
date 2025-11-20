
export const getQuery = (regionId, scheduleId, versionId) => {
	
  return `
SELECT TOP 1
    sv.*,
    reg.RegionName,
    rl.LeagueName,
    homet.TeamName as 'HomeTeamName',
    awayt.TeamName as 'AwayTeamName',
    p.ParkName
FROM ScheduleVersion sv
  INNER JOIN [Region] reg ON reg.RegionID = sv.RegionId
  LEFT JOIN [RegionLeague] rl ON rl.RegionId = sv.RegionId AND rl.LeagueId = sv.LeagueId
  LEFT JOIN [Team] homet ON homet.RegionId = sv.RegionId AND homet.TeamId = sv.HomeTeam
	LEFT JOIN [Team] awayt ON awayt.RegionId = sv.RegionId AND awayt.TeamId = sv.AwayTeam
  LEFT JOIN [Park] p ON p.RegionId = sv.RegionId AND p.ParkId = sv.ParkId
WHERE
    sv.RegionId = '${regionId}'
    AND sv.ScheduleId = ${scheduleId}
    AND sv.VersionId = ${versionId}
  `;
}

export const formatResult = (result) => {
};
