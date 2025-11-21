import { DataTypes } from '@sequelize/core';

const UsernameLastClicked = {
	Username: {
		type: DataTypes.STRING(100),
		allowNull: false,
		primaryKey: true,
	},
	LastClickedRegion: {
		type: DataTypes.STRING(100),
		allowNull: false,
	},
	LastClickedUsername: {
		type: DataTypes.STRING(100),
		allowNull: false,
	},
};

const UsernameLastClickedAssociations = {

};

export { UsernameLastClicked, UsernameLastClickedAssociations };

