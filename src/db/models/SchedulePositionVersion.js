import { DataTypes } from '@sequelize/core';

const SchedulePositionVersion = {
  RegionId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  ScheduleId: {
    type: DataTypes.INTEGER,
    allowNull: false,
    primaryKey: true
  },
  VersionId: {
    type: DataTypes.INTEGER,
    allowNull: false,
    primaryKey: true
  },
  PositionId: {
    type: DataTypes.STRING(50),
    allowNull: false,
    primaryKey: true
  },
  OfficialId: {
    type: DataTypes.STRING(100),
    allowNull: false
  }
};

const SchedulePositionVersionAssociations = {
};

export { SchedulePositionVersion, SchedulePositionVersionAssociations };

