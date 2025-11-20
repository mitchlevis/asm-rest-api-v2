
export const getQuery = (scheduleIds, username) => {
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
    ru.Username AS 'RegionUsername',
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
    Fines = (
      SELECT
        sf.OfficialId,
        CASE WHEN ru.RealUsername IS NULL OR ru.RealUsername = '' THEN sf.OfficialId ELSE ru.RealUsername END AS RealUsername,
        ru.FirstName,
        ru.LastName,
        sf.Amount,
        sf.Comment
      FROM
        ScheduleFine sf
        LEFT JOIN RegionUser ru ON ru.Username = sf.OfficialId AND ru.RegionId = sf.RegionId
      WHERE sf.RegionId = s.RegionId AND sf.ScheduleId = s.ScheduleId
      ORDER BY sf.Amount DESC
      FOR JSON PATH
    ),
    Pay = (
      SELECT
        rlp.CrewType,
        rlp.PositionId,
        rlp.Pay
      FROM [RegionLeaguePay] rlp
      WHERE rlp.RegionId = s.RegionId AND rlp.LeagueId = s.LeagueId AND rlp.GameStatus = s.GameStatus
      FOR JSON PATH
    ),
    UserComments = (
      SELECT
        scu.OfficialId,
        scu.Comment,
        ru.FirstName,
        ru.LastName
      FROM
        ScheduleUserComment scu
        LEFT JOIN RegionUser ru ON ru.Username = scu.OfficialId AND ru.RegionId = scu.RegionId
      WHERE scu.RegionId = s.RegionId AND scu.ScheduleId = s.ScheduleId
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
    Bookoffs = (
      SELECT
        DISTINCT sbo.Username,
        sbo.Username AS OfficialId,
        ru.FirstName,
        ru.LastName,
        sbo.DateAdded,
        sbo.Reason
      FROM
        ScheduleBookOff sbo
        LEFT JOIN RegionUser ru ON ru.Username = sbo.Username AND ru.RegionId = sbo.RegionId
      WHERE sbo.RegionId = s.RegionId AND sbo.ScheduleId = s.ScheduleId
      ORDER BY sbo.DateAdded ASC
      FOR JSON PATH
    ),
    Versions = (
      SELECT
        svv.*,
        rlv.LeagueName,
        regv.RegionName,
        regv.TimeZone,
        regv.sport as 'Sport',
        CASE WHEN homeTLv.TeamName IS NULL THEN hometv.TeamName ELSE homeTLv.TeamName END as 'HomeTeamName',
        CASE WHEN awayTLv.TeamName IS NULL THEN awaytv.TeamName ELSE awayTLv.TeamName END as 'AwayTeamName',
        pv.ParkName,
        pv.City,
        Positions = (
          SELECT
            ispv.OfficialId,
            ispv.PositionId,
            ruv.Username,
            ruv.RealUsername,
            ruv.FirstName,
            ruv.LastName,
            rlpv.Pay,
            Confirmed = (
              SELECT
                COUNT(*)
              FROM [ScheduleConfirm] sc
              WHERE 
                sc.RegionId = ispv.RegionId 
                AND sc.ScheduleId = ispv.ScheduleId 
                AND sc.Username = ispv.OfficialId
                AND sc.VersionId = svv.VersionId
            )
          FROM SchedulePositionVersion ispv
          LEFT JOIN [RegionUser] ruv ON ruv.Username = ispv.OfficialId AND ruv.RegionId = ispv.RegionId
          LEFT JOIN [RegionLeaguePay] rlpv ON rlpv.RegionId = ispv.RegionId AND rlpv.LeagueId = svv.LeagueId AND rlpv.CrewType = svv.CrewType AND rlpv.PositionId = ispv.PositionId
          WHERE ispv.RegionId = svv.RegionId AND ispv.ScheduleId = svv.ScheduleId AND ispv.VersionId = svv.VersionId
          FOR JSON PATH
        ),
        Fines = (
          SELECT
            sfv.OfficialId,
            CASE WHEN ruv.RealUsername IS NULL OR ruv.RealUsername = '' THEN sfv.OfficialId ELSE ruv.RealUsername END AS RealUsername,
            ruv.FirstName,
            ruv.LastName,
            sfv.Amount,
            sfv.Comment
          FROM
            ScheduleFineVersion sfv
            LEFT JOIN RegionUser ruv ON ruv.Username = sfv.OfficialId AND ruv.RegionId = sfv.RegionId
          WHERE sfv.RegionId = svv.RegionId AND sfv.ScheduleId = svv.ScheduleId AND sfv.VersionId = svv.VersionId
          ORDER BY sfv.Amount DESC
          FOR JSON PATH
        ),
        Pay = (
          SELECT
            rlpv.CrewType,
            rlpv.PositionId,
            rlpv.Pay
          FROM [RegionLeaguePay] rlpv
          WHERE rlpv.RegionId = svv.RegionId AND rlpv.LeagueId = svv.LeagueId AND rlpv.GameStatus = svv.GameStatus
          FOR JSON PATH
        ),
        Bookoffs = (
          SELECT
            DISTINCT sbov.Username,
            sbov.Username AS OfficialId,
            ruv.FirstName,
            ruv.LastName,
            sbov.DateAdded,
            sbov.Reason
          FROM
            ScheduleBookOff sbov
            LEFT JOIN RegionUser ruv ON ruv.Username = sbov.Username AND ruv.RegionId = sbov.RegionId
          WHERE sbov.RegionId = svv.RegionId AND sbov.ScheduleId = svv.ScheduleId
          ORDER BY sbov.DateAdded ASC
          FOR JSON PATH
        ),
        UserComments = (
          SELECT
            scuv.OfficialId,
            scuv.Comment,
            ruv.FirstName,
            ruv.LastName
          FROM
            ScheduleUserCommentVersion scuv
            LEFT JOIN RegionUser ruv ON ruv.Username = scuv.OfficialId AND ruv.RegionId = scuv.RegionId
          WHERE scuv.RegionId = svv.RegionId AND scuv.ScheduleId = svv.ScheduleId AND scuv.VersionId = svv.VersionId
          FOR JSON PATH
        )
      FROM 
        ScheduleVersion svv
        INNER JOIN [Region] regv ON regv.RegionID = svv.RegionId
        LEFT JOIN [RegionLeague] rlv ON rlv.RegionId = svv.RegionId AND rlv.LeagueId = svv.LeagueId
        LEFT JOIN [Team] hometv ON hometv.RegionId = svv.RegionId AND hometv.TeamId = svv.HomeTeam
        LEFT JOIN [Team] homeTLv on homeTLv.RegionId = rlv.RealLeagueId AND homeTLv.TeamId = svv.HomeTeam
        LEFT JOIN [Team] awaytv ON awaytv.RegionId = svv.RegionId AND awaytv.TeamId = svv.AwayTeam
        LEFT JOIN [Team] awayTLv on awayTLv.RegionId = rlv.RealLeagueId AND awayTLv.TeamId = svv.AwayTeam
        LEFT JOIN [Park] pv ON pv.RegionId = svv.RegionId AND pv.ParkId = svv.ParkId
      WHERE svv.RegionId = s.RegionId AND svv.ScheduleId = s.ScheduleId
      ORDER BY svv.VersionId DESC
      FOR JSON PATH
    )
    
FROM 
  Schedule s
  INNER JOIN ScheduleVersion sv ON s.RegionId = sv.RegionId AND s.ScheduleId = sv.ScheduleId AND s.VersionId = sv.VersionId
  JOIN OrderedKeys ok ON s.RegionId = ok.RegionId AND s.ScheduleId = ok.ScheduleId
  INNER JOIN [Region] reg ON reg.RegionID = s.RegionId
  LEFT JOIN [RegionUser] ru ON ru.RealUsername = '${username}' AND ru.RegionId = s.RegionId
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

export const formatResult = async (result, options) => {
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

  // Parse Fines
  for(const row of result){
    try{
      row.Fines = !options.isInner ? JSON.parse(row.Fines) : row.Fines;
      // Reverse the (+/-)
      for(const fine of row.Fines){
        fine.Amount = -fine.Amount;
      }
    }
    catch(e){
      row.Fines = null;
    }
  }

  // Parse Pay
  for(const row of result){
    try{
      row.Pay = !options.isInner ? JSON.parse(row.Pay) : row.Pay;
    }
    catch(e){
      row.Pay = null;
    }
  }
  
  // If Positions is NOT null, we json parse it and add it to the object
  for(const row of result){
    if(row.Positions && typeof row.Positions === 'string'){
      row.Positions = JSON.parse(row.Positions);
    }
  }

  // Parse User Comments
  for(const row of result){
    try{
      row.UserComments = !options.isInner ? JSON.parse(row.UserComments) : row.UserComments;
    }
    catch(e){
      row.UserComments = null;
    }
  }

  // Parse ScheduleConfirm
  for(const row of result){
    try{
      row.ScheduleConfirm = JSON.parse(row.ScheduleConfirm);
    }
    catch(e){
      row.ScheduleConfirm = null;
    }
  }

  // Parse Bookoffs
  for(const row of result){
    try{
      row.Bookoffs = JSON.parse(row.Bookoffs);
    }
    catch(e){
      row.Bookoffs = null;
    }
  }

  // Parse Versions Recursively
  if(!options.skipVersions){
    for(const row of result){
      try{
        row.Versions = JSON.parse(row.Versions);

        // Format Inner Results
        row.Versions = await formatResult(row.Versions, { isInner: true, skipVersions: true });
      }
      catch(e){
        row.Versions = null;
      }
    }
  }

  return result;
};

