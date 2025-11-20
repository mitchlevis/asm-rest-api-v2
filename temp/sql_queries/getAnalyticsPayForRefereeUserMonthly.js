
export const getQuery = (regionId, userId, sortDirection) => {

  return `
  SELECT
  DATEPART(YEAR, s.GameDate) AS GameYear,
  DATEPART(MONTH, s.GameDate) AS GameMonthNumber,
  DATENAME(MONTH, s.GameDate) AS GameMonthNameEn,
  CASE 
      WHEN MONTH(s.GameDate) = 1 THEN 'Janvier'
      WHEN MONTH(s.GameDate) = 2 THEN 'Février'
      WHEN MONTH(s.GameDate) = 3 THEN 'Mars'
      WHEN MONTH(s.GameDate) = 4 THEN 'Avril'
      WHEN MONTH(s.GameDate) = 5 THEN 'Mai'
      WHEN MONTH(s.GameDate) = 6 THEN 'Juin'
      WHEN MONTH(s.GameDate) = 7 THEN 'Juillet'
      WHEN MONTH(s.GameDate) = 8 THEN 'Août'
      WHEN MONTH(s.GameDate) = 9 THEN 'Septembre'
      WHEN MONTH(s.GameDate) = 10 THEN 'Octobre'
      WHEN MONTH(s.GameDate) = 11 THEN 'Novembre'
      WHEN MONTH(s.GameDate) = 12 THEN 'Décembre'
  END AS GameMonthNameFr,
  SUM(rlp.Pay) AS TotalPay
FROM 
  [Schedule] s
  INNER JOIN SchedulePosition sp 
      ON sp.RegionId = s.RegionId 
      AND sp.ScheduleId = s.ScheduleId
  INNER JOIN RegionUser ru 
      ON ru.RegionId = s.RegionId 
      AND ru.Username = sp.OfficialId
  LEFT JOIN RegionLeaguePay rlp 
      ON rlp.RegionId = s.RegionId
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
  AND s.GameStatus = ''
  AND s.GameDate >= DATEADD(YEAR, -1, GETDATE())
GROUP BY 
  DATEPART(YEAR, s.GameDate), 
  DATEPART(MONTH, s.GameDate), 
  DATENAME(MONTH, s.GameDate)
ORDER BY 
  GameYear ${sortDirection}, 
  GameMonthNumber ${sortDirection};
  `;
}

export const formatResult = async (result) => {

};
