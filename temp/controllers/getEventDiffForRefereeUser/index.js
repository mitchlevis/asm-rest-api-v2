import Sequelize from 'sequelize';
const Op = Sequelize.Op;
import { Config } from "sst/node/config";
import parameters from "./parameters";
import { authenticate, validateIncomingParameters, throwError, getDbObject, getSequelizeObject, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { getQuery as getQuerySchedule } from "@asportsmanager-api/core/sql_queries/getRefereeEventDetails";
import { getQuery as getQueryScheduleVersion } from "@asportsmanager-api/core/sql_queries/getRefereeEventVersionDetails";
import { getQuery as getQuerySchedulePosition } from "@asportsmanager-api/core/sql_queries/getRefereeEventPositionDetails";
import { getQuery as getQuerySchedulePositionVersion } from "@asportsmanager-api/core/sql_queries/getRefereeEventPositionVersionDetails";

export const handler = async (_evt) => {
  try {
    await authenticate(_evt, Config.API_KEYS);

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);

    const { userId, regionId, scheduleId } = path;

    if(!userId) {
      await throwError(400, 'User ID is required');
    }
    if(!regionId) {
      await throwError(400, 'Region ID is required');
    }
    if(!scheduleId) {
      await throwError(400, 'Schedule ID is required');
    }

    const sequelize = await getSequelizeObject();
    const regionUserModel = await getDbObject('RegionUser');
    const scheduleConfirmModel = await getDbObject('ScheduleConfirm');
    const scheduleModel = await getDbObject('Schedule');
    const scheduleVersionModel = await getDbObject('ScheduleVersion');

    // Get Region User Username
    const { Username } = await regionUserModel.findOne({ attributes: ['Username'], where: { RealUsername: userId, RegionId: regionId } });

    // Get Latest Schedule Confirm
    const { VersionId } = await scheduleConfirmModel.findOne({ attributes: [[Sequelize.fn('max', Sequelize.col('VersionId')), 'VersionId']], where: { RegionId: regionId, ScheduleId: scheduleId, Username: Username } });

    // Get Schedule & Schedule Version
    const querySchedule = getQuerySchedule(regionId, scheduleId);
    const queryScheduleVersion = getQueryScheduleVersion(regionId, scheduleId, VersionId);

    const schedule = await sequelize.query(querySchedule, { type: sequelize.QueryTypes.SELECT }).then((res) => res[0]);
    const scheduleVersion = await sequelize.query(queryScheduleVersion, { type: sequelize.QueryTypes.SELECT }).then((res) => res[0]);

    // const [schedule, scheduleVersion] = await Promise.all([
    //   scheduleModel.findOne({ where: { RegionId: regionId, ScheduleId: scheduleId }, raw: true }),
    //   scheduleVersionModel.findOne({ where: { RegionId: regionId, ScheduleId: scheduleId, VersionId: VersionId }, raw: true })
    // ]);

    if(!schedule || !scheduleVersion) {
      await throwError(404, 'Schedule not found');
    }
    if(schedule.VersionId < 2){
      await throwError(400, 'Schedule has no changes');
    }
    if(schedule.VersionId === scheduleVersion.VersionId){
      await throwError(400, 'Schedule has no changes since last confirmation');
    }

    // Determine differences between schedule and schedule version
    const scheduleDifferences = {};
    for(const columnName of Object.keys(schedule)){
      //Skip VersionId (as it will always be different)
      if(columnName !== 'VersionId') {
        // Hack! - Doing a JSON stringify because some native types are not being compared correctly
        if(JSON.stringify(schedule[columnName]) != JSON.stringify(scheduleVersion[columnName])){
          // CrewType is JSON
          if(columnName === 'CrewType'){
            scheduleDifferences[columnName] = { old: JSON.parse(scheduleVersion[columnName]), new: JSON.parse(schedule[columnName]) };
          }
          else {
            scheduleDifferences[columnName] = { old: scheduleVersion[columnName], new: schedule[columnName] };
          }
        }
      }
    }

    // Get Schedule Positions & Schedule Position Versions
    const querySchedulePosition = getQuerySchedulePosition(regionId, scheduleId);
    const querySchedulePositionVersion = getQuerySchedulePositionVersion(regionId, scheduleId, VersionId);

    const schedulePositions = await sequelize.query(querySchedulePosition, { type: sequelize.QueryTypes.SELECT });
    const schedulePositionVersions = await sequelize.query(querySchedulePositionVersion, { type: sequelize.QueryTypes.SELECT });

    const schedulePositionDifferences = {};
    // Determine differences between schedule positions and schedule position versions
    for(const schedulePositionVersion of schedulePositionVersions){
      const schedulePosition = schedulePositions.find((sp) => sp.PositionId === schedulePositionVersion.PositionId);
      if(!schedulePosition) {
        if(schedulePositionVersion.FirstName !== '' && schedulePositionVersion.LastName !== '')
        {
          schedulePositionDifferences[schedulePositionVersion.PositionId] = { old: `${schedulePositionVersion.FirstName} ${schedulePositionVersion.LastName}`, new: null };
        }
        else{
          schedulePositionDifferences[schedulePositionVersion.PositionId] = { old: schedulePositionVersion.OfficialId, new: null };
        }
      }
      else {
        if(schedulePositionVersion.OfficialId !== schedulePosition.OfficialId){
          if(schedulePositionVersion.FirstName !== '' && schedulePositionVersion.LastName !== '' && schedulePosition.FirstName !== '' && schedulePosition.LastName !== '')
          {
            schedulePositionDifferences[schedulePositionVersion.PositionId] = { old: `${schedulePositionVersion.FirstName} ${schedulePositionVersion.LastName}`, new: `${schedulePosition.FirstName} ${schedulePosition.LastName}` };
          }
          else{
            schedulePositionDifferences[schedulePositionVersion.PositionId] = { old: schedulePositionVersion.OfficialId, new: schedulePosition.OfficialId };
          }
        }
      }
    }

    // Determine differences between schedule positions and schedule position versions
    for(const schedulePosition of schedulePositions){
      const schedulePositionVersion = schedulePositionVersions.find((spv) => spv.PositionId === schedulePosition.PositionId);
      if(!schedulePositionVersion) {
        if(schedulePosition.FirstName !== '' && schedulePosition.LastName !== ''){
          schedulePositionDifferences[schedulePosition.PositionId] = { old: null, new: `${schedulePosition.FirstName} ${schedulePosition.LastName}` };
        }
        else{
          schedulePositionDifferences[schedulePosition.PositionId] = { old: null, new: schedulePosition.OfficialId };
        }
      }
      else {
        if(schedulePosition.OfficialId !== schedulePositionVersion.OfficialId){
          if(schedulePosition.FirstName !== '' && schedulePosition.LastName !== '' && schedulePositionVersion.FirstName !== '' && schedulePositionVersion.LastName !== ''){
            schedulePositionDifferences[schedulePosition.PositionId] = { old: `${schedulePositionVersion.FirstName} ${schedulePositionVersion.LastName}`, new: `${schedulePosition.FirstName} ${schedulePosition.LastName}` };
          }
          else{
            schedulePositionDifferences[schedulePosition.PositionId] = { old: schedulePositionVersion.OfficialId, new: schedulePosition.OfficialId };
          }
        }
      }
    }

    return formatSuccessResponse(_evt, { Schedule: scheduleDifferences, SchedulePosition: schedulePositionDifferences }, 200);
  }
  catch (err) {
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};