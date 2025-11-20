const { DataTypes } = require('sequelize');

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
    // Note: VersionId is not part of the primary key (Old code would update the row when a new confirmation)
  },
  DateAdded: {
    // Due to a bug with sequelize and mssql, datetime types are represented as strings
    type: DataTypes.STRING,
    allowNull: false
  }
};

const ScheduleConfirmAssociations = {
  // Define associations here
};

export { ScheduleConfirm, ScheduleConfirmAssociations };