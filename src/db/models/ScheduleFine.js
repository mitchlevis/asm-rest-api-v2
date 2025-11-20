import { DataTypes } from '@sequelize/core';

const ScheduleFine = {
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
    Amount: {
        type: DataTypes.DECIMAL(18, 2),
        allowNull: false
    },
    Comment: {
        type: DataTypes.STRING(2000),
        allowNull: false
    }
};

const ScheduleFineAssociations = {
  belongsTo: [
    {
      modelName: 'RegionUser',
      foreignKey: 'OfficialId',
      targetKey: 'Username',
    },
  ]
};

export { ScheduleFine, ScheduleFineAssociations };

