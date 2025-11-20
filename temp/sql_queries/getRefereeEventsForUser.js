export const getQuery = (regionIdArray, userId, pastEventsOnly = false, sortDirection, max, offset) => {
  const regionInClause = regionIdArray.map((regionId) => `'${regionId}'`).join(',');
  const SELECT = max !== -1 && offset === 0 ? `SELECT DISTINCT TOP (${max})` : 'SELECT DISTINCT';

  return `
${SELECT}
  reg.RegionName,
  [Schedule].GameType,
  [Schedule].CrewType,
  [Schedule].GameStatus,
  [Schedule].RegionId,
  [Schedule].ScheduleId,
  [RegionLeague].LeagueName,
  [RegionLeague].LeagueId,
  [Schedule].GameNumber,
  [Schedule].GameDate,
  reg.TimeZone,
  [Schedule].HomeTeam,
  homet.TeamName as 'HomeTeamName',
  [Schedule].AwayTeam,
  awayt.TeamName as 'AwayTeamName',
  [Schedule].ParkId,
  p.ParkName,
  p.City,
  reg.sport as 'Sport',
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
        WHERE sc.RegionId = isp.RegionId AND sc.ScheduleId = isp.ScheduleId AND sc.VersionId = [Schedule].VersionId AND sc.Username = isp.OfficialId
      )
    FROM SchedulePosition isp
    LEFT JOIN [RegionUser] ru ON ru.Username = isp.OfficialId AND ru.RegionId = isp.RegionId
    LEFT JOIN [RegionLeaguePay] rlp ON rlp.RegionId = isp.RegionId AND rlp.LeagueId = [Schedule].LeagueId AND rlp.CrewType = [Schedule].CrewType AND rlp.PositionId = isp.PositionId
    WHERE isp.RegionId = [Schedule].RegionId AND isp.ScheduleId = [Schedule].ScheduleId 
    FOR JSON PATH
  ),
  Pay = (
    SELECT
      rlp.CrewType,
      rlp.PositionId,
      rlp.Pay
    FROM [RegionLeaguePay] rlp
    WHERE rlp.RegionId = [Schedule].RegionId AND rlp.LeagueId = [Schedule].LeagueId AND rlp.GameStatus = [Schedule].GameStatus
    FOR JSON PATH
  )
FROM [Schedule]
  INNER JOIN SchedulePosition sp ON sp.RegionId = [Schedule].RegionId AND sp.ScheduleId = [Schedule].ScheduleId
  INNER JOIN RegionUser ru ON ru.RegionId = [Schedule].RegionId AND ru.Username = sp.OfficialId
	INNER JOIN [Region] reg ON reg.RegionID = [Schedule].RegionId
	LEFT JOIN [RegionLeague] ON [RegionLeague].RegionId = [Schedule].RegionId AND [RegionLeague].LeagueId = [Schedule].LeagueId
	LEFT JOIN [Team] homet ON homet.RegionId = [Schedule].RegionId AND homet.TeamId = [Schedule].HomeTeam 
	LEFT JOIN [Team] awayt ON awayt.RegionId = [Schedule].RegionId AND awayt.TeamId = [Schedule].AwayTeam
	LEFT JOIN [Park] p ON p.RegionId = [Schedule].RegionId AND p.ParkId = [Schedule].ParkId
WHERE
  ru.RealUsername = '${userId}'
  AND reg.EntityType = 'referee'
  AND [Schedule].RegionId IN (${regionInClause})
  AND ([Schedule].GameStatus = '' OR [Schedule].GameStatus IS NULL) -- Discard games that are 'cancelled'/'weather'/etc
  ${pastEventsOnly ? 'AND [Schedule].GameDate < GETDATE()' : 'AND [Schedule].GameDate > GETDATE()'}
ORDER BY 
  [Schedule].GameDate ${sortDirection}
  ${offset !== 0 ? `OFFSET ${offset} ROWS`: ''}
  ${offset !== 0 && max !== -1 ? `FETCH NEXT ${max} ROWS ONLY` : ''};
  `;
};

export const formatResult = async (result) => {
  if(!result || result.length === 0){
    return [];
  }
  
  // Parse CrewType
  for(const row of result){
    try{
      row.CrewType = JSON.parse(row.CrewType);
    }
    catch(e){
      row.CrewType = null;
    }    
  }

  // Parse Pay
  for(const row of result){
    try{
      row.Pay = JSON.parse(row.Pay);
    }
    catch(e){
      row.Pay = null;
    }
  }
  

  // If Positions is NOT null, we json parse it and add it to the object
  for(const row of result){
    if(row.Positions){
      row.Positions = JSON.parse(row.Positions);
    }
  }

  return result;
};