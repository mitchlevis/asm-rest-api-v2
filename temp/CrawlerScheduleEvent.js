const { DataTypes } = require('sequelize');

const CrawlerScheduleEvent = {
    EventId: {
        type: DataTypes.INTEGER,
        allowNull: false,
        primaryKey: true
    },
    CrawlerId: {
        type: DataTypes.INTEGER,
        allowNull: true,
        // references: {
        //     model: 'CrawlerSchedule', // This is the model name that Sequelize has mapped for the table
        //     key: 'CrawlerId'
        // }
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
        type: DataTypes.DATE, // datetime maps to DATE in Sequelize
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