import { DataTypes } from '@sequelize/core';

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

