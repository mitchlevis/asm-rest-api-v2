const { DataTypes } = require('sequelize');

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
		type: DataTypes.DATE,
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
			foreignKey: {
				name: 'RegionId',
				field: 'RegionId'
			},
			targetKey: 'RegionId',
			foreignKeyConstraint: true,
			as: 'Availability3',
		},
		{
			modelName: 'RegionUser',
			foreignKey: {
				name: 'Username',
				field: 'Username'
			},
			targetKey: 'Username',
			foreignKeyConstraint: true,
		}
	]
};

export { Availability3, Availability3Associations };