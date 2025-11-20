
export const getQuery = (regionId, scheduleId) => {
	
  return `
SELECT
    sp.RegionId,
    sp.ScheduleId,
    sp.PositionId,
    sp.OfficialId,
    ru.Username,
    ru.RealUsername,
    ru.FirstName,
    ru.LastName
FROM SchedulePosition sp
    INNER JOIN RegionUser ru ON sp.RegionId = ru.RegionId AND sp.OfficialId = ru.Username
WHERE
    sp.RegionId = '${regionId}'
    AND sp.ScheduleId = ${scheduleId}
  `;
}

export const formatResult = (result) => {
};
