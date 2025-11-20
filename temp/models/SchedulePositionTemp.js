const { DataTypes } = require('sequelize');

const SchedulePositionTemp = {
    RegionId: {
        type: DataTypes.STRING(100),
        allowNull: false,
        primaryKey: true
    },
    UserSubmitId: {
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

const SchedulePositionTempAssociations = {
  belongsTo: [
      {
        modelName: 'RegionUser',
        foreignKey: 'OfficialId',
        targetKey: 'Username',
      },
    ],
};

export { SchedulePositionTemp, SchedulePositionTempAssociations };