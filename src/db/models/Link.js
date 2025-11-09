import { DataTypes } from '@sequelize/core';

const Link = {
  RegionId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  LinkId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  CategoryId: {
    type: DataTypes.INTEGER,
    allowNull: true
  },
  LinkTitle: {
    type: DataTypes.STRING(300),
    allowNull: false
  },
  LinkAddress: {
    type: DataTypes.STRING(300),
    allowNull: false
  },
  LinkDescription: {
    type: DataTypes.STRING(500),
    allowNull: false
  }
};

const LinkAssociations = {

};

export { Link, LinkAssociations };
