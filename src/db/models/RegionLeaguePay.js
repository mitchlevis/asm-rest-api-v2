import { DataTypes } from '@sequelize/core';

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
        type: DataTypes.DECIMAL(18, 2),
        allowNull: false
    }
};

const RegionLeaguePayAssociations = {
};

export { RegionLeaguePay, RegionLeaguePayAssociations };

