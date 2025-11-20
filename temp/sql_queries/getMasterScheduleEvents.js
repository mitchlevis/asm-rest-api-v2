const dayjs = require('dayjs');

export const getQuery = (scheduleIds, options) => {
  if(!scheduleIds || scheduleIds.length === 0) {
    return 'SELECT * FROM schedule WHERE 1 = 0;';
  }

  // Options
  const returnTemp = options?.returnTemp ?? false;

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
    ${returnTemp === false ? 'sv.IsDeleted,':''}
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
            ${returnTemp === true ? '': 'AND sc.VersionId = s.VersionId'} -- ONLY CHECK VERSION IF NOT TEMP
        ),
        Conflicts = (
          SELECT
            'double_booking' AS Type,
            c.RegionId,
            c.ScheduleId,
            c.GameDate,
            c.GameType,
            c.GameNumber,
            CASE WHEN pc.ParkId IS NULL THEN c.ParkId ELSE pc.ParkName END AS 'ParkName',
            irl.LeagueName,
            CASE WHEN irl.LeagueId IS NULL THEN c.LeagueId ELSE irl.LeagueId END AS 'LeagueId',
            COALESCE(irl.ArriveBeforeMins + irl.MaxGameLengthMins, r.DefaultArriveBeforeMins + r.DefaultMaxGameLengthMins) AS 'TimeWindow'
          FROM
          ${returnTemp === true ? 'ScheduleTemp': 'Schedule'} c
            LEFT JOIN [RegionLeague] irl ON irl.RegionId = c.RegionId AND irl.LeagueId = c.LeagueId
            LEFT JOIN [Region] r ON r.RegionId = c.RegionId
            LEFT JOIN [Park] pc ON pc.RegionId = c.RegionId AND pc.ParkId = c.ParkId
          WHERE c.RegionId = s.RegionId
            AND c.ScheduleId <> s.ScheduleId
            AND (c.GameStatus = '' OR c.GameStatus IS NULL) -- Discard games that are 'cancelled'/'weather'/etc
            AND CAST(c.GameDate AS DATE) = CAST(s.GameDate AS DATE)
            AND ABS(DATEDIFF(MINUTE, c.GameDate, s.GameDate)) <= 
              COALESCE(irl.ArriveBeforeMins + irl.MaxGameLengthMins, r.DefaultArriveBeforeMins + r.DefaultMaxGameLengthMins)
            AND c.ParkId <> s.ParkId -- This condition is to exclude games at the same park
            AND EXISTS (
              SELECT 1
              FROM SchedulePosition cp
              WHERE
                cp.OfficialId <> ''
                AND cp.RegionId = c.RegionId
                AND cp.ScheduleId = c.ScheduleId
                AND cp.OfficialId = isp.OfficialId
            )
          FOR JSON PATH
        )
      FROM ${returnTemp === true ? 'SchedulePositionTemp': 'SchedulePosition'} isp
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
        ${returnTemp === true ? 'ScheduleFineTemp': 'ScheduleFine'} sf
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
    Requests = (
      SELECT
        sr.OfficialId,
        ru.FirstName,
        ru.LastName,
        sr.DateAdded
      FROM
        [ScheduleRequest] sr
        LEFT JOIN RegionUser ru ON ru.Username = sr.OfficialId AND ru.RegionId = sr.RegionId
      WHERE sr.RegionId = s.RegionId AND sr.ScheduleId = s.ScheduleId
      ORDER BY sr.DateAdded ASC
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
    UserComments = (
      SELECT
        scu.OfficialId,
        scu.Comment,
        ru.FirstName,
        ru.LastName
      FROM
        ${returnTemp === true ? 'ScheduleUserCommentTemp': 'ScheduleUserComment'} scu
        LEFT JOIN RegionUser ru ON ru.Username = scu.OfficialId AND ru.RegionId = scu.RegionId
      WHERE scu.RegionId = s.RegionId AND scu.ScheduleId = s.ScheduleId ${returnTemp === true ? 'AND scu.IsDeleted = 0': ''}
      FOR JSON PATH
    )
    
FROM 
  ${returnTemp === true ? 'ScheduleTemp': 'Schedule'} s
  ${returnTemp === false ? 'INNER JOIN ScheduleVersion sv ON s.RegionId = sv.RegionId AND s.ScheduleId = sv.ScheduleId AND s.VersionId = sv.VersionId': ''} 
  JOIN OrderedKeys ok ON s.RegionId = ok.RegionId AND s.ScheduleId = ok.ScheduleId
  INNER JOIN [Region] reg ON reg.RegionID = s.RegionId
  LEFT JOIN [RegionLeague] rl ON rl.RegionId = s.RegionId AND rl.LeagueId = s.LeagueId
  LEFT JOIN [Team] homet ON homet.RegionId = s.RegionId AND homet.TeamId = s.HomeTeam 
  LEFT JOIN [Team] homeTL on homeTL.RegionId = rl.RealLeagueId AND homeTL.TeamId = s.HomeTeam
  LEFT JOIN [Team] awayt ON awayt.RegionId = s.RegionId AND awayt.TeamId = s.AwayTeam
  LEFT JOIN [Team] awayTL on awayTL.RegionId = rl.RealLeagueId AND awayTL.TeamId = s.AwayTeam
  LEFT JOIN [Park] p ON p.RegionId = s.RegionId AND p.ParkId = s.ParkId
    
${returnTemp === false ? 'WHERE sv.IsDeleted = 0': ''}
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
      row.Fines = JSON.parse(row.Fines);
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

  // Parse Schedule Requests
  for(const row of result){
    try{
      row.Requests = JSON.parse(row.Requests);
    }
    catch(e){
      row.Requests = null;
    }
  }

  // Parse Schedule Bookoffs
  for(const row of result){
    try{
      row.Bookoffs = JSON.parse(row.Bookoffs);
    }
    catch(e){
      row.Bookoffs = null;
    }
  }

  // Parse User Comments
  for(const row of result){
    try{
      row.UserComments = JSON.parse(row.UserComments);
    }
    catch(e){
      row.UserComments = null;
    }
  }

  return result;
};