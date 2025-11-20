
export const getQuery = (regionId) => {
	
  return `
  SELECT
  sr.RegionId,
  sr.ScheduleId,
  sr.OfficialId,
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
  ru.IsLinked,
  sr.DateAdded
FROM
  [ScheduleRequest] sr
  LEFT JOIN RegionUser ru ON ru.Username = sr.OfficialId AND ru.RegionId = sr.RegionId
WHERE sr.RegionId = '${regionId}'
ORDER BY sr.DateAdded ASC
  `;
}

export const formatResult = (result) => {

  if(!result || result.length === 0){
    return [];
  }

  // Parse Ranks & Positions
  for(const row of result){
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
  }

  return result;
};
