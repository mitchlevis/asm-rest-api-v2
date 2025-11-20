import { DataTypes } from '@sequelize/core';
import { safeJSONParse } from './abc';

const Schedule = {
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
    allowNull: false
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
    type: DataTypes.STRING,
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
  GameType: {
    type: DataTypes.STRING(50),
    allowNull: false
  },
  LinkedRegionId: {
    type: DataTypes.STRING(100),
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
  LinkedScheduleId: {
    type: DataTypes.INTEGER,
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
    allowNull: true
  },
  SupervisorRegionId: {
    type: DataTypes.STRING(100),
    allowNull: false
  }
};

const ScheduleAssociations = {
  belongsTo: [
    {
      modelName: 'Region',
      foreignKey: 'RegionId',
      targetKey: 'RegionID',
    },
    {
      modelName: 'RegionUser',
      foreignKey: 'RegionId',
      targetKey: 'RegionId',
      constraints: false,
    },
    {
      modelName: 'RegionLeague',
      foreignKey: 'LeagueId',
      targetKey: 'LeagueId',
      as: 'RegionLeague',
    },
    {
      modelName: 'ScheduleVersion',
      foreignKey: 'VersionId',
      targetKey: 'VersionId',
      constraints: false,
    },
    {
      modelName: 'Team',
      as: 'HomeTeamAssociation',
      foreignKey: 'HomeTeam',
    },
    {
      modelName: 'Team',
      as: 'AwayTeamAssociation',
      foreignKey: 'AwayTeam',
    },
    {
      modelName: 'Park',
      foreignKey: 'ParkId',
      // sourceKey: 'ParkId',
			as: 'ParkAssociation',
    },
  ],
  hasMany: [
    {
      modelName: 'SchedulePosition',
      foreignKey: 'ScheduleId',
      sourceKey: 'ScheduleId',
    },
    {
      modelName: 'ScheduleFine',
      foreignKey: 'ScheduleId',
    },
    {
      modelName: 'ScheduleRequest',
      foreignKey: 'ScheduleId',
    },
    {
      modelName: 'ScheduleBookOff',
      foreignKey: 'ScheduleId',
    },
    {
      modelName: 'ScheduleUserComment',
      foreignKey: 'ScheduleId',
    },
  ],
};

export { Schedule, ScheduleAssociations };

