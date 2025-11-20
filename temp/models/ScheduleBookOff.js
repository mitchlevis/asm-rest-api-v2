const { DataTypes } = require('sequelize');

const ScheduleBookOff = {
  RegionId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  Username: {
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
  DateAdded: {
    // Due to a bug with sequelize and mssql, datetime types are represented as strings
    type: DataTypes.STRING,
    allowNull: false
  },
  Reason: {
    type: DataTypes.STRING(500),
    allowNull: false
  }
};

const ScheduleBookOffAssociations = {
  belongsTo: [
    {
      modelName: 'RegionUser',
      foreignKey: 'Username',
      targetKey: 'Username',
    },
  ]
};

export { ScheduleBookOff, ScheduleBookOffAssociations };
