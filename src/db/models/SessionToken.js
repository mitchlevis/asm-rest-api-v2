import { DataTypes } from '@sequelize/core';

const SessionToken = {
  UserName: {
    type: DataTypes.STRING(200),
    allowNull: false,
    primaryKey: true
  },
  SessionToken: {
    type: DataTypes.UUID,
    allowNull: false,
    primaryKey: true
  },
  IssuanceDate: {
    type: DataTypes.STRING,
    allowNull: false
  },
  DurationDays: {
    type: DataTypes.INTEGER,
    allowNull: false
  }
};

const SessionTokenAssociations = {

};

export { SessionToken, SessionTokenAssociations };
