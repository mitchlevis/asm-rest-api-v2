import { DataTypes } from '@sequelize/core';

const CrawlerScheduleEvent = {
    EventId: {
        type: DataTypes.INTEGER,
        allowNull: false,
        primaryKey: true
    },
    CrawlerId: {
        type: DataTypes.INTEGER,
        allowNull: true
    },
    Name: {
        type: DataTypes.STRING(100),
        allowNull: true
    },
    Description: {
        type: DataTypes.STRING(500),
        allowNull: true
    },
    DateCreated: {
        type: DataTypes.STRING,
        allowNull: true
    }
};

const CrawlerScheduleEventAssociations = {
    belongsTo: [
        {
          modelName: 'CrawlerSchedule',
          foreignKey: 'CrawlerId',
          targetKey: 'CrawlerId',
        },
    ],
};

export { CrawlerScheduleEvent, CrawlerScheduleEventAssociations };

