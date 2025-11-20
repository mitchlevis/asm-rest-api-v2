
export const getQuery = (regionId, userId, sortDirection) => {

  return `
SELECT
  CONVERT(DATE, s.GameDate) AS GameDay,
  SUM(rlp.Pay) AS TotalPay
FROM
  [Schedule] s
  INNER JOIN SchedulePosition sp ON sp.RegionId = s.RegionId
  AND sp.ScheduleId = s.ScheduleId
  INNER JOIN RegionUser ru ON ru.RegionId = s.RegionId
  AND ru.Username = sp.OfficialId
  LEFT JOIN RegionLeaguePay rlp ON rlp.RegionId = s.RegionId
  AND rlp.LeagueId = s.LeagueId
  AND rlp.GameStatus = s.GameStatus
  AND rlp.CrewType = CASE
      WHEN sp.PositionId = 'scorekeeper' THEN JSON_VALUE(s.CrewType, '$.scorekeeper')
      WHEN sp.PositionId = 'supervisor' THEN JSON_VALUE(s.CrewType, '$.supervisor')
      ELSE JSON_VALUE(s.CrewType, '$.umpire')
  END
  AND rlp.PositionId = sp.PositionId
WHERE
  ru.RealUsername = '${userId}'
  ${regionId ? `AND s.RegionId = '${regionId}'` : ''}
  AND s.GameStatus = '' -- Game was not Cancelled
  AND s.GameDate >= DATEADD(YEAR, -1, GETDATE())
GROUP BY
  CONVERT(DATE, s.GameDate)
ORDER BY
  GameDay ${sortDirection};
  `;
}

export const formatResult = async (result) => {

};
