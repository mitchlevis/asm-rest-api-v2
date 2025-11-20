
export const getQuery = (userId, regionIds, year, month) => {
	const values = regionIds.map((regionId, index) => {
    return `('${regionId}')`;
  });
  return `
SELECT DISTINCT
    s.RegionId,
    s.ScheduleId,
    s.VersionId,
    s.GameNumber,
    s.LeagueId,
    s.GameDate,
    s.GameStatus
FROM
    [User] u
INNER JOIN RegionUser ru ON u.Username = ru.RealUsername
INNER JOIN SchedulePosition sp ON ru.Username = sp.OfficialId
INNER JOIN Schedule s ON sp.ScheduleId = s.ScheduleId AND sp.RegionId = s.RegionId
WHERE
    u.Username = '${userId}'
    AND ru.RegionId IN (${values})
    AND YEAR(s.GameDate) = '${year}'
    AND MONTH(s.GameDate) = '${month}'
    AND (s.GameStatus IS NULL OR s.GameStatus = '') -- Game was not cancelled
    AND s.GameDate > CURRENT_TIMESTAMP -- This ensures only future games are considered
ORDER BY
    s.GameDate ASC
  `;
}

export const formatResult = (result) => {
};
