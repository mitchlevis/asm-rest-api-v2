
export const getQuery = (regionId, userId, sortDirection) => {

  return `
WITH WeekData AS (
    SELECT
        DATEPART(YEAR, s.GameDate) AS GameYear,
        DATEPART(WEEK, s.GameDate) AS GameWeek,
        s.GameDate,
        rlp.Pay,
        DATEADD(DAY, 1 - DATEPART(WEEKDAY, s.GameDate), CONVERT(DATE, s.GameDate)) AS WeekStart,
        DATEADD(DAY, 7 - DATEPART(WEEKDAY, s.GameDate), CONVERT(DATE, s.GameDate)) AS WeekEnd
    FROM [Schedule] s
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
)

SELECT
    GameYear,
    GameWeek,
    MIN(WeekStart) AS WeekStart,
    MAX(WeekEnd) AS WeekEnd,
    FORMAT(MIN(WeekStart), 'MMMM dd') + ' - ' + FORMAT(MAX(WeekEnd), 'MMMM dd') AS WeekRangeEn,
    CASE 
        WHEN MONTH(MIN(WeekStart)) = MONTH(MAX(WeekEnd)) THEN 
            FORMAT(MIN(WeekStart), 'dd') + ' - ' + FORMAT(MAX(WeekEnd), 'dd ') + 
            CASE MONTH(MIN(WeekStart))
                WHEN 1 THEN 'Janvier'
                WHEN 2 THEN 'Février'
                WHEN 3 THEN 'Mars'
                WHEN 4 THEN 'Avril'
                WHEN 5 THEN 'Mai'
                WHEN 6 THEN 'Juin'
                WHEN 7 THEN 'Juillet'
                WHEN 8 THEN 'Août'
                WHEN 9 THEN 'Septembre'
                WHEN 10 THEN 'Octobre'
                WHEN 11 THEN 'Novembre'
                WHEN 12 THEN 'Décembre'
            END
        ELSE 
            FORMAT(MIN(WeekStart), 'dd ') + 
            CASE MONTH(MIN(WeekStart))
                WHEN 1 THEN 'Janvier'
                WHEN 2 THEN 'Février'
                WHEN 3 THEN 'Mars'
                WHEN 4 THEN 'Avril'
                WHEN 5 THEN 'Mai'
                WHEN 6 THEN 'Juin'
                WHEN 7 THEN 'Juillet'
                WHEN 8 THEN 'Août'
                WHEN 9 THEN 'Septembre'
                WHEN 10 THEN 'Octobre'
                WHEN 11 THEN 'Novembre'
                WHEN 12 THEN 'Décembre'
            END +
            ' - ' +
            FORMAT(MAX(WeekEnd), 'dd ') +
            CASE MONTH(MAX(WeekEnd))
                WHEN 1 THEN 'Janvier'
                WHEN 2 THEN 'Février'
                WHEN 3 THEN 'Mars'
                WHEN 4 THEN 'Avril'
                WHEN 5 THEN 'Mai'
                WHEN 6 THEN 'Juin'
                WHEN 7 THEN 'Juillet'
                WHEN 8 THEN 'Août'
                WHEN 9 THEN 'Septembre'
                WHEN 10 THEN 'Octobre'
                WHEN 11 THEN 'Novembre'
                WHEN 12 THEN 'Décembre'
            END
    END AS WeekRangeFr,
    SUM(Pay) AS TotalPay
FROM WeekData
GROUP BY GameYear, GameWeek
ORDER BY GameYear ${sortDirection}, GameWeek ${sortDirection};
  `;
}

export const formatResult = async (result) => {

};
