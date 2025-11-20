import { convertBase64ToAvailability } from '../business-logic-helpers';

export const getQuery = (regionIdArray, year, month) => {
	const regionInClause = regionIdArray.map((regionId) => `'${regionId}'`).join(',');

  return `
  SELECT
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
  SchedulePositions = (
    SELECT 
      sp.RegionId,
      sp.ScheduleId,
      s.GameDate,
      DAY(s.GameDate) as GameDay
    FROM 
      SchedulePosition sp
      LEFT JOIN Schedule s ON s.ScheduleId = sp.ScheduleId AND s.RegionId = sp.RegionId
      WHERE
        sp.RegionId = ru.RegionId
        AND sp.OfficialId = ru.Username
        AND YEAR(s.GameDate) = ${year}
        AND MONTH(s.GameDate) = ${month}
    FOR JSON PATH
  )
FROM
  RegionUser ru
  LEFT JOIN Availability3 av ON ru.RegionId = av.RegionId AND av.Username = ru.Username AND YEAR(av.AvailabilityDate) = ${year} AND MONTH(av.AvailabilityDate) = ${month}
WHERE
  ru.RegionId IN (${regionInClause})
  `;
}

export const formatResult = (result) => {

  if(!result || result.length === 0){
    return [];
  }

  // Parse special fields
  for(const row of result){

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

    row.Ranks  = rankData;
    row.SubRanks = subRankData;
    row.Positions = positionData;

    // Availability
    if(row.Availability && row.AvailabilityYear && row.AvailabilityMonth){
      row.Availability = convertBase64ToAvailability(row.AvailabilityYear, row.AvailabilityMonth, row.Availability)
    }

    // Schedules
    try{
      row.SchedulePositions = JSON.parse(row.SchedulePositions);
    }
    catch(e){
      row.SchedulePositions = null;
    }
  }

  return result;
};
