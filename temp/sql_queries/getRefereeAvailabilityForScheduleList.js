import { convertBase64ToAvailability } from '../business-logic-helpers';

export const getQuery = (regionId, scheduleIds, username) => {
  // Convert array to comma-separated string for SQL IN clause
  const scheduleIdsStr = scheduleIds.join(',');
  
  return `
  SELECT
    s.ScheduleId,
    s.GameDate,
    s.GameType,
    ru.Username,
    ru.RealUsername,
    ru.FirstName,
    ru.LastName,
    ru.Rank,
    ru.RankNumber,
    ru.Positions,
    ru.PublicData,
    ru.PrivateData,
    ru.InternalData,
    ru.GlobalAvailabilityData,
    av.Availability,
    YEAR(av.AvailabilityDate) as AvailabilityYear,
    MONTH(av.AvailabilityDate) as AvailabilityMonth,
    DATEPART(DAY, s.GameDate) as GameDay,
    DATEPART(HOUR, s.GameDate) as GameHour,
    Conflicts = (
      SELECT 
        'double_booking' AS Type,
        c.RegionId,
        c.ScheduleId,
        c.GameDate,
        c.GameType,
        c.GameNumber,
        CASE WHEN pc.ParkId IS NULL THEN c.ParkId ELSE pc.ParkName END AS ParkName,
        irl.LeagueName,
        CASE WHEN irl.LeagueId IS NULL THEN c.LeagueId ELSE irl.LeagueId END AS LeagueId,
        COALESCE(irl.ArriveBeforeMins + irl.MaxGameLengthMins, r.DefaultArriveBeforeMins + r.DefaultMaxGameLengthMins) AS TimeWindow
      FROM
        Schedule c
        LEFT JOIN RegionLeague irl ON irl.RegionId = c.RegionId AND irl.LeagueId = c.LeagueId
        LEFT JOIN Region r ON r.RegionId = c.RegionId
        LEFT JOIN Park pc ON pc.RegionId = c.RegionId AND pc.ParkId = c.ParkId
      WHERE c.RegionId = ru.RegionId
        AND c.ScheduleId <> s.ScheduleId
        AND CAST(c.GameDate AS DATE) = CAST(s.GameDate AS DATE)
        AND ABS(DATEDIFF(MINUTE, c.GameDate, s.GameDate)) <= 
          COALESCE(irl.ArriveBeforeMins + irl.MaxGameLengthMins, r.DefaultArriveBeforeMins + r.DefaultMaxGameLengthMins)
        AND EXISTS (
          SELECT 1
          FROM SchedulePosition cp
          WHERE
            cp.OfficialId <> ''
            AND cp.RegionId = c.RegionId
            AND cp.ScheduleId = c.ScheduleId
            AND cp.OfficialId = ru.Username
        )
      FOR JSON PATH
    )
  FROM
    RegionUser ru
    INNER JOIN Schedule s ON s.RegionId = ru.RegionId
    LEFT JOIN Availability3 av ON s.RegionId = av.RegionId 
      AND av.Username = ru.Username 
      AND YEAR(av.AvailabilityDate) = YEAR(s.GameDate) 
      AND MONTH(av.AvailabilityDate) = MONTH(s.GameDate)
  WHERE
    ru.Username = '${username}'
    AND ru.RegionId = '${regionId}'
    AND s.ScheduleId IN (${scheduleIdsStr})
  `;
};

export const formatResult = (result) => {
  if(!result || result.length === 0){
    return [];
  }

  // Parse special fields
  for(const row of result){
    // Conflicts
    try{
      row.Conflicts = JSON.parse(row.Conflicts);
      if(!row.Conflicts){
        row.Conflicts = [];
      }
    }
    catch(err){
      row.Conflicts = [];
    }

    // Ranks & Positions
    let rankData = {};
    let subRankData = {};
    let positionData = [];

    try{
      rankData = JSON.parse(row.Rank);
    }
    catch(err){
      console.log(`Invalid rank JSON for user ${row.Username}: ${row.Rank} - Skipping`);
    }

    try{
      subRankData = JSON.parse(row.RankNumber);
    }
    catch(err){
      console.log(`Invalid rank number JSON for user ${row.Username}: ${row.RankNumber} - Skipping`);
    }

    try{
      positionData = JSON.parse(row.Positions);
    }
    catch(err){
      console.log(`Invalid position JSON for user ${row.Username}: ${row.Positions} - Skipping`);
    }

    row.Ranks = rankData;
    row.SubRanks = subRankData;
    row.Positions = positionData;

    // Availability - Process and determine IsAvailable
    let isAvailable = 0;
    if(row.Availability && row.AvailabilityYear && row.AvailabilityMonth){
      const availabilityData = convertBase64ToAvailability(
        row.AvailabilityYear, 
        row.AvailabilityMonth, 
        row.Availability
      );
      
      row.Availability = availabilityData;
      
      // Check if user is available for this specific game
      if (availabilityData[row.GameDay] && availabilityData[row.GameDay][row.GameHour]) {
        const availValue = availabilityData[row.GameDay][row.GameHour];
        
        // Based on the comment in business-logic-helpers.js:
        // 0 = Not Filled In
        // 1 = Available for Games
        // 2 = Not available 
        // 3 = Available for Practices
        // 4 = Available for Both
        
        if (row.GameType && row.GameType.toLowerCase() === 'practice') {
          isAvailable = (availValue === 3 || availValue === 4) ? 1 : 0;
        } else {
          // For games, playoffs, exhibitions
          isAvailable = (availValue === 1 || availValue === 4) ? 1 : 0;
        }
      }
    }
    
    row.IsAvailable = isAvailable;
  }

  return result;
};