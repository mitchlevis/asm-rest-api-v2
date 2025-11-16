const { DataTypes } = require('sequelize');

const ScheduleRequest = {
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
  OfficialId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  DateAdded: {
    type: DataTypes.DATE,
    allowNull: false
  }
};

const ScheduleRequestAssociations = {
  belongsTo: [
    {
      modelName: 'RegionUser',
      foreignKey: 'OfficialId',
      targetKey: 'Username',
    },
  ]
};

export { ScheduleRequest, ScheduleRequestAssociations };