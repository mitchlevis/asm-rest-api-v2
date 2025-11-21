import { DataTypes } from '@sequelize/core';

const AvailabilityDueDateUser = {
	RegionId: {
		type: DataTypes.STRING(100),
		allowNull: false,
		primaryKey: true,
	},
	Username: {
		type: DataTypes.STRING(100),
		allowNull: false,
		primaryKey: true,
	},
	StartDate: {
		type: DataTypes.STRING, // Storing datetime as string due to the bug
		allowNull: false,
		primaryKey: true,
	},
	EndDate: {
		type: DataTypes.STRING, // Storing datetime as string due to the bug
		allowNull: false,
		primaryKey: true,
	},
	IsFilledIn: {
		type: DataTypes.BOOLEAN,
		allowNull: false,
	},
};

const AvailabilityDueDateUserAssociations = {
	// Associations removed - using raw SQL queries for complex composite key joins
};

export { AvailabilityDueDateUser, AvailabilityDueDateUserAssociations };

