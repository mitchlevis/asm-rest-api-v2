import { DataTypes } from '@sequelize/core';

const SchedulePosition = {
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

const SchedulePositionAssociations = {
  belongsTo: [
    {
      modelName: 'RegionUser',
      foreignKey: 'OfficialId',
      targetKey: 'Username',
    },
    {
      modelName: 'Schedule',
      foreignKey: 'ScheduleId',
      targetKey: 'ScheduleId',
      as: 'Schedule',
    },
  ],
};

export { SchedulePosition, SchedulePositionAssociations };

