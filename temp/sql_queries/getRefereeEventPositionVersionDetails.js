
export const getQuery = (regionId, scheduleId, versionId) => {
	
  return `
SELECT
    spv.RegionId,
    spv.ScheduleId,
    spv.PositionId,
    spv.OfficialId,
    spv.VersionId,
    ru.Username,
    ru.RealUsername,
    ru.FirstName,
    ru.LastName
FROM SchedulePositionVersion spv
  INNER JOIN RegionUser ru ON spv.RegionId = ru.RegionId AND spv.OfficialId = ru.Username
WHERE
    spv.RegionId = '${regionId}'
    AND spv.ScheduleId = ${scheduleId}
    AND spv.VersionId = ${versionId}
  `;
}

export const formatResult = (result) => {
};
