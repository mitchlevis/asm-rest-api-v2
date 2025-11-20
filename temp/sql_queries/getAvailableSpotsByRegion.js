export const getQuery = (scheduleIds, officialId = null) => {
  if(!scheduleIds || scheduleIds.length === 0) {
    return 'SELECT * FROM schedule WHERE 1 = 0;';
  }

  const values = scheduleIds.map((compound, index) => {
    return `('${compound.RegionId}', '${compound.ScheduleId}')`;
  });

  return `
WITH OrderedKeys AS (
  SELECT 
    *,
    ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS 'Order'
  FROM 
      (VALUES 
          ${values}
      ) AS V(RegionId, ScheduleId)
)
SELECT
    s.*,
    sv.IsDeleted,
    rl.LeagueName,
    CASE WHEN rl.LeagueId IS NULL THEN s.LeagueId ELSE rl.LeagueId END AS 'LeagueId',
    reg.RegionName,
    reg.TimeZone,
    reg.sport as 'Sport',
    s.HomeTeam,
    CASE WHEN homeTL.TeamName IS NULL THEN homet.TeamName ELSE homeTL.TeamName END as 'HomeTeamName',
    s.AwayTeam,
    CASE WHEN awayTL.TeamName IS NULL THEN awayt.TeamName ELSE awayTL.TeamName END as 'AwayTeamName',
    p.ParkName,
    p.City,
    Positions = (
      SELECT
        isp.OfficialId,
        isp.PositionId,
        ru.Username,
        ru.RealUsername,
        ru.FirstName,
        ru.LastName,
        rlp.Pay,
        Confirmed = (
          SELECT
            COUNT(*)
          FROM [ScheduleConfirm] sc
          WHERE 
            sc.RegionId = isp.RegionId 
            AND sc.ScheduleId = isp.ScheduleId 
            AND sc.Username = isp.OfficialId
            AND sc.VersionId = s.VersionId
        )
      FROM SchedulePosition isp
      LEFT JOIN [RegionUser] ru ON ru.Username = isp.OfficialId AND ru.RegionId = isp.RegionId
      LEFT JOIN [RegionLeaguePay] rlp ON rlp.RegionId = isp.RegionId AND rlp.LeagueId = s.LeagueId AND rlp.CrewType = s.CrewType AND rlp.PositionId = isp.PositionId
      WHERE isp.RegionId = s.RegionId AND isp.ScheduleId = s.ScheduleId 
      FOR JSON PATH
    ),
    ScheduleConfirm = (
      SELECT
        sc.RegionId,
        sc.ScheduleId,
        sc.Username,
        sc.VersionId,
        sc.DateAdded
      FROM ScheduleConfirm sc
      WHERE sc.RegionId = s.RegionId AND sc.ScheduleId = s.ScheduleId
      FOR JSON PATH
    ),
    Requests = (
      SELECT
        sr.RegionId,
        sr.ScheduleId,
        sr.OfficialId,
        sr.DateAdded
      FROM ScheduleRequest sr
      WHERE
        sr.RegionId = s.RegionId
        AND sr.ScheduleId = s.ScheduleId
        ${officialId ? `AND sr.OfficialId = '${officialId}'` : ''}
      FOR JSON PATH
    )
FROM 
  Schedule s
  INNER JOIN ScheduleVersion sv ON s.RegionId = sv.RegionId AND s.ScheduleId = sv.ScheduleId AND s.VersionId = sv.VersionId
  JOIN OrderedKeys ok ON s.RegionId = ok.RegionId AND s.ScheduleId = ok.ScheduleId
  INNER JOIN [Region] reg ON reg.RegionID = s.RegionId
  LEFT JOIN [RegionLeague] rl ON rl.RegionId = s.RegionId AND rl.LeagueId = s.LeagueId
  LEFT JOIN [Team] homet ON homet.RegionId = s.RegionId AND homet.TeamId = s.HomeTeam 
  LEFT JOIN [Team] homeTL on homeTL.RegionId = rl.RealLeagueId AND homeTL.TeamId = s.HomeTeam
  LEFT JOIN [Team] awayt ON awayt.RegionId = s.RegionId AND awayt.TeamId = s.AwayTeam
  LEFT JOIN [Team] awayTL on awayTL.RegionId = rl.RealLeagueId AND awayTL.TeamId = s.AwayTeam
  LEFT JOIN [Park] p ON p.RegionId = s.RegionId AND p.ParkId = s.ParkId

ORDER BY 
  ok.[Order];
  `;
};

export const formatResult = async (result) => {
  if(!result || result.length === 0){
    return [];
  }
  
  // Process all JSON fields in a single loop
  for(const row of result){
    // Parse CrewType
    try {
      row.CrewType = typeof row.CrewType === 'string' ? JSON.parse(row.CrewType) : null;
    } catch(e) {
      row.CrewType = null;
    }
    
    // Parse Positions
    row.Positions = row.Positions && typeof row.Positions === 'string' 
      ? JSON.parse(row.Positions) 
      : row.Positions;
    
    // Parse ScheduleConfirm
    try {
      row.ScheduleConfirm = typeof row.ScheduleConfirm === 'string' 
        ? JSON.parse(row.ScheduleConfirm) 
        : null;
    } catch(e) {
      row.ScheduleConfirm = null;
    }
    
    // Parse Requests
    try {
      row.Requests = typeof row.Requests === 'string' 
        ? JSON.parse(row.Requests) 
        : null;
    } catch(e) {
      row.Requests = null;
    }
  }

  return result;
};