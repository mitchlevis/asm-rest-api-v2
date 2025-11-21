import { DataTypes } from '@sequelize/core';

const UserSubmittedInfo = {
	Username: {
		type: DataTypes.STRING(100),
		allowNull: false,
		primaryKey: true,
	},
	HasSubmittedInfo: {
		type: DataTypes.BOOLEAN,
		allowNull: false,
	},
};

const UserSubmittedInfoAssociations = {

};

export { UserSubmittedInfo, UserSubmittedInfoAssociations };

