const { DataTypes } = require('sequelize');

const ScheduleUserComment = {
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
    Comment: {
        type: DataTypes.STRING(2000),
        allowNull: false
    }
};

const ScheduleUserCommentAssociations = {
    belongsTo: [
        {
          modelName: 'RegionUser',
          foreignKey: 'OfficialId',
          targetKey: 'Username',
        },
      ]
};

export { ScheduleUserComment, ScheduleUserCommentAssociations };