const { DataTypes } = require('sequelize');

const RegionLeaguePay = {
    RegionId: {
        type: DataTypes.STRING(100),
        allowNull: false,
        primaryKey: true
    },
    LeagueId: {
        type: DataTypes.STRING(100),
        allowNull: false,
        primaryKey: true
    },
    GameStatus: {
        type: DataTypes.STRING(50),
        allowNull: false,
        primaryKey: true
    },
    CrewType: {
        type: DataTypes.STRING(50),
        allowNull: false,
        primaryKey: true
    },
    PositionId: {
        type: DataTypes.STRING(50),
        allowNull: false,
        primaryKey: true
    },
    Pay: {
        type: DataTypes.DECIMAL,
        allowNull: false
    }
};

const RegionLeaguePayAssociations = {
};

export { RegionLeaguePay, RegionLeaguePayAssociations };