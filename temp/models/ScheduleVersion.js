const { DataTypes } = require('sequelize');

const ScheduleVersion = {
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
  VersionId: {
    type: DataTypes.INTEGER,
    allowNull: false,
    primaryKey: true
  },
  GameNumber: {
    type: DataTypes.STRING(50),
    allowNull: false
  },
  LeagueId: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  HomeTeam: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  HomeTeamScore: {
    type: DataTypes.STRING(50),
    allowNull: false
  },
  AwayTeam: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  AwayTeamScore: {
    type: DataTypes.STRING(50),
    allowNull: false
  },
  GameDate: {
    // Due to a bug with sequelize and mssql, datetime types are represented as strings
    type: DataTypes.DATE,
    allowNull: false
  },
  ParkId: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  CrewType: {
    type: DataTypes.STRING(100),
    allowNull: true,
    get() {
      return safeJSONParse(this.getDataValue('CrewType'));
    },
    set(value) {
      this.setDataValue('CrewType', JSON.stringify(value));
    }
  },
  GameStatus: {
    type: DataTypes.STRING(50),
    allowNull: false
  },
  GameComment: {
    type: DataTypes.STRING(2000),
    allowNull: false
  },
  IsDeleted: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  },
  DateAdded: {
    type: DataTypes.STRING, // Representing datetime as string
    allowNull: false
  },
  GameType: {
    type: DataTypes.STRING(50),
    allowNull: false
  },
  LinkedRegionId: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  LinkedScheduleId: {
    type: DataTypes.INTEGER,
    allowNull: false
  },
  OfficialRegionId: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  ScorekeeperRegionId: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  HomeTeamScoreExtra: {
    type: DataTypes.STRING(2000),
    allowNull: false
  },
  AwayTeamScoreExtra: {
    type: DataTypes.STRING(2000),
    allowNull: false
  },
  StatLinks: {
    type: DataTypes.STRING(1000),
    allowNull: false
  },
  GameScore: {
    type: DataTypes.STRING(500),
    allowNull: true // This field is nullable based on your instructions
  },
  SupervisorRegionId: {
    type: DataTypes.STRING(100),
    allowNull: false
  }
};

const ScheduleVersionAssociations = {
  hasMany: {
    modelName: 'Schedule',
    foreignKey: 'VersionId',
    sourceKey: 'VersionId',
    constraints: false,
  }
};

export { ScheduleVersion, ScheduleVersionAssociations };
