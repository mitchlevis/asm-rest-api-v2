import { DataTypes } from '@sequelize/core';

const CrawlerSchedule = {
    CrawlerId: {
        type: DataTypes.INTEGER,
        primaryKey: true,
        autoIncrement: true
    },
    Name: {
        type: DataTypes.STRING(100),
        allowNull: true
    },
    Description: {
        type: DataTypes.STRING(500),
        allowNull: true
    },
    URL: {
        type: DataTypes.STRING,
        allowNull: true
    },
    DateCreated: {
        type: DataTypes.STRING,
        allowNull: true
    },
    DateUpdated: {
        type: DataTypes.STRING,
        allowNull: true
    },
    IsActive: {
        type: DataTypes.BOOLEAN,
        allowNull: true
    },
    DryRun: {
        type: DataTypes.BOOLEAN,
        allowNull: true
    },
    Environment: {
        type: DataTypes.STRING(100),
        allowNull: true
    },
    Implementation: {
        type: DataTypes.STRING(100),
        allowNull: true
    },
    IsLastExecutionSuccessful: {
        type: DataTypes.BOOLEAN,
        allowNull: true
    },
    DateLastExecuted: {
        type: DataTypes.STRING,
        allowNull: true
    },
    RegionId: {
        type: DataTypes.STRING(100),
        allowNull: true
    },
    FetchLimitDays: {
        type: DataTypes.INTEGER,
        allowNull: true
    }
};

const CrawlerScheduleAssociations = {
  hasMany: [
      {
        modelName: 'CrawlerScheduleEvent',
        foreignKey: 'CrawlerId',
        sourceKey: 'CrawlerId',
      }
  ]
};

export { CrawlerSchedule, CrawlerScheduleAssociations };

