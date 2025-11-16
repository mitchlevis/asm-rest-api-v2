import { DataTypes } from '@sequelize/core';

const ScheduleConfirm = {
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
  Username: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  VersionId: {
    type: DataTypes.INTEGER,
    allowNull: false
  },
  DateAdded: {
    type: DataTypes.STRING,
    allowNull: false
  }
};

const ScheduleConfirmAssociations = {
};

export { ScheduleConfirm, ScheduleConfirmAssociations };

