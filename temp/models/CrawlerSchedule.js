const { DataTypes } = require('sequelize');

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
        type: DataTypes.STRING, // nvarchar with unlimited length in SQL Server maps to STRING in Sequelize
        allowNull: true
    },
    DateCreated: {
        type: DataTypes.DATE, // datetime2 maps to DATE in Sequelize
        allowNull: true
    },
    DateUpdated: {
        type: DataTypes.DATE, // datetime2 maps to DATE in Sequelize
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
        type: DataTypes.DATE,
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