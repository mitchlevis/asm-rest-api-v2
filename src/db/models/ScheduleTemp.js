import { DataTypes } from '@sequelize/core';
import { safeJSONParse } from './abc';

const ScheduleTemp = {
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
	GameNumber: {
		type: DataTypes.STRING(50),
		allowNull: false
	},
	GameNumberModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	LeagueId: {
		type: DataTypes.STRING(100),
		allowNull: false
	},
	LeagueIdModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	HomeTeam: {
		type: DataTypes.STRING(100),
		allowNull: false
	},
	HomeTeamModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	HomeTeamScore: {
		type: DataTypes.STRING(50),
		allowNull: false
	},
	HomeTeamScoreModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	AwayTeam: {
		type: DataTypes.STRING(100),
		allowNull: false
	},
	AwayTeamModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	AwayTeamScore: {
		type: DataTypes.STRING(50),
		allowNull: false
	},
	AwayTeamScoreModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	GameDate: {
		type: DataTypes.STRING,
		allowNull: false
	},
	GameDateModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	ParkId: {
		type: DataTypes.STRING(100),
		allowNull: false
	},
	ParkIdModified: {
		type: DataTypes.BOOLEAN,
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
	CrewTypeModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	GameStatus: {
		type: DataTypes.STRING(50),
		allowNull: false
	},
	GameStatusModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	GameComment: {
		type: DataTypes.STRING(2000),
		allowNull: false
	},
	GameCommentModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	IsDeleted: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	OldScheduleId: {
		type: DataTypes.INTEGER,
		allowNull: false
	},
	GameType: {
		type: DataTypes.STRING(50),
		allowNull: false
	},
	GameTypeModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	LinkedRegionId: {
		type: DataTypes.STRING(100),
		allowNull: false
	},
	LinkedRegionIdModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	LinkedScheduleId: {
		type: DataTypes.INTEGER,
		allowNull: false
	},
	LinkedScheduleIdModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	OfficialRegionId: {
		type: DataTypes.STRING(100),
		allowNull: false
	},
	OfficialRegionIdModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	ScorekeeperRegionId: {
		type: DataTypes.STRING(100),
		allowNull: false
	},
	ScorekeeperRegionIdModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	HomeTeamScoreExtra: {
		type: DataTypes.STRING(2000),
		allowNull: false
	},
	HomeTeamScoreExtraModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	AwayTeamScoreExtra: {
		type: DataTypes.STRING(2000),
		allowNull: false
	},
	AwayTeamScoreExtraModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	},
	GameScore: {
		type: DataTypes.STRING(500),
		allowNull: true
	},
	GameScoreModified: {
		type: DataTypes.BOOLEAN,
		allowNull: true
	},
	SupervisorRegionId: {
		type: DataTypes.STRING(100),
		allowNull: false
	},
	SupervisorRegionIdModified: {
		type: DataTypes.BOOLEAN,
		allowNull: false
	}
};

const ScheduleTempAssociations = {
	hasMany: [
		{
			modelName: 'SchedulePositionTemp',
			foreignKey: 'ScheduleId',
			sourceKey: 'ScheduleId',
		},
	]
};

export { ScheduleTemp, ScheduleTempAssociations };

