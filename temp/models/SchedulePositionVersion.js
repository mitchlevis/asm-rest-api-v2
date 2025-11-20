const { DataTypes } = require('sequelize');

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
    // Note: OfficialId is not part of the primary key as per the given constraints
  }
};

const SchedulePositionVersionAssociations = {
  // Define associations here
};

export { SchedulePositionVersion, SchedulePositionVersionAssociations };
