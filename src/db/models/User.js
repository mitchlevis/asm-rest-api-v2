import { DataTypes } from '@sequelize/core';

const User = {
	Username: {
		type: DataTypes.STRING(100),
		allowNull: false,
		primaryKey: true,
	},
	FirstName: {
		type: DataTypes.STRING(100),
		allowNull: false,
	},
	LastName: {
		type: DataTypes.STRING(100),
		allowNull: false,
	},
	Email: {
		type: DataTypes.STRING(200),
		allowNull: false,
	},
	Password: {
		type: DataTypes.STRING(256),
		allowNull: false,
	},
	RegistrationDate: {
		type: DataTypes.STRING, // Storing datetime as string due to the bug
		allowNull: false,
	},
	PhoneNumbers: {
		type: DataTypes.STRING(2000),
		allowNull: false,
	},
	Country: {
		type: DataTypes.STRING(200),
		allowNull: false,
	},
	State: {
		type: DataTypes.STRING(200),
		allowNull: false,
	},
	City: {
		type: DataTypes.STRING(200),
		allowNull: false,
	},
	Address: {
		type: DataTypes.STRING(200),
		allowNull: false,
	},
	PostalCode: {
		type: DataTypes.STRING(200),
		allowNull: false,
	},
	PreferredLanguage: {
		type: DataTypes.STRING(50),
		allowNull: false,
	},
	AlternateEmails: {
		type: DataTypes.STRING(2000),
		allowNull: false,
	},
	EmailAvailableGames: {
		type: DataTypes.BOOLEAN,
		allowNull: false,
	},
	EmailAvailabilityReminders: {
		type: DataTypes.BOOLEAN,
		allowNull: false,
	},
	EmailGamesRequiringConfirm: {
		type: DataTypes.BOOLEAN,
		allowNull: false,
	},
	SMSGameReminders: {
		type: DataTypes.BOOLEAN,
		allowNull: false,
	},
	SMSLastMinuteChanges: {
		type: DataTypes.BOOLEAN,
		allowNull: false,
	},
	SMSAvailabilityReminders: {
		type: DataTypes.BOOLEAN,
		allowNull: false,
	},
	TimeZone: {
		type: DataTypes.STRING(200),
		allowNull: false,
	},
	ICSToken: {
		type: DataTypes.UUID, // Representing uniqueidentifier
		allowNull: true,
	},
	PhotoId: {
		type: DataTypes.UUID, // Representing uniqueidentifier
		allowNull: true,
	},
	HasPhotoId: {
		type: DataTypes.BOOLEAN,
		allowNull: false,
	},
	HasBetaAccess: {
		type: DataTypes.BOOLEAN,
		allowNull: false,
	},
	NextLadderLeaguePaymentDue: {
		type: DataTypes.STRING, // Storing datetime as string due to the bug
		allowNull: true,
	},
};

const UserAssociations = {

};

export { User, UserAssociations };
