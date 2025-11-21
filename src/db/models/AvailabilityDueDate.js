import { DataTypes } from '@sequelize/core';

const AvailabilityDueDate = {
	RegionId: {
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
	Name: {
		type: DataTypes.STRING(200),
		allowNull: false,
	},
	DueDate: {
		type: DataTypes.STRING, // Storing datetime as string due to the bug
		allowNull: false,
	},
	RemindDaysInAdvance: {
		type: DataTypes.INTEGER,
		allowNull: false,
	},
	CanFillInDaysInAdvance: {
		type: DataTypes.INTEGER,
		allowNull: false,
	},
};

const AvailabilityDueDateAssociations = {
	// Associations removed - using raw SQL queries for complex composite key joins
};

export { AvailabilityDueDate, AvailabilityDueDateAssociations };

