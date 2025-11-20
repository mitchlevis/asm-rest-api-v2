import { DataTypes } from '@sequelize/core';

const Availability3 = {
	RegionId: {
		type: DataTypes.STRING(100),
		allowNull: false,
		primaryKey: true
	},
	Username: {
		type: DataTypes.STRING(100),
		allowNull: false,
		primaryKey: true
	},
	AvailabilityDate: {
		type: DataTypes.STRING,
		allowNull: false,
		primaryKey: true
	},
	Availability: {
		type: DataTypes.STRING(1000),
		allowNull: false
	}
};

const Availability3Associations = {
	belongsTo: [
		{
			modelName: 'RegionUser',
			foreignKey: 'RegionId',
			targetKey: 'RegionId',
		},
		{
			modelName: 'RegionUser',
			foreignKey: 'Username',
			targetKey: 'Username',
		}
	]
};

export { Availability3, Availability3Associations };

