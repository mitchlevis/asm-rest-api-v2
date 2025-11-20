
export const getQuery = (regionId, userId, sortDirection) => {

  return `
  SELECT
  DATEPART(YEAR, s.GameDate) AS GameYear,
  SUM(rlp.Pay) AS TotalPay
FROM [Schedule] s
INNER JOIN SchedulePosition sp ON sp.RegionId = s.RegionId AND sp.ScheduleId = s.ScheduleId
INNER JOIN RegionUser ru ON ru.RegionId = s.RegionId AND ru.Username = sp.OfficialId
LEFT JOIN RegionLeaguePay rlp ON rlp.RegionId = s.RegionId
                              AND rlp.LeagueId = s.LeagueId
                              AND rlp.GameStatus = s.GameStatus
                              AND rlp.CrewType = CASE 
                                   WHEN sp.PositionId = 'scorekeeper' AND ISJSON(s.CrewType) = 1 THEN JSON_VALUE(s.CrewType, '$.scorekeeper')
                                   WHEN sp.PositionId = 'supervisor' AND ISJSON(s.CrewType) = 1 THEN JSON_VALUE(s.CrewType, '$.supervisor')
                                   WHEN ISJSON(s.CrewType) = 1 THEN JSON_VALUE(s.CrewType, '$.umpire')
                                   ELSE NULL -- Or your predefined value
                              END
                              AND rlp.PositionId = sp.PositionId
WHERE
  ru.RealUsername = '${userId}'
  ${regionId ? `AND s.RegionId = '${regionId}'` : ''}
  AND s.GameStatus = ''
GROUP BY DATEPART(YEAR, s.GameDate)
HAVING SUM(rlp.Pay) IS NOT NULL
ORDER BY GameYear ${sortDirection};
  `;
}

export const formatResult = async (result) => {

};
