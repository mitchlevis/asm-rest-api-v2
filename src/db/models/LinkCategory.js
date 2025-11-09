import { DataTypes } from '@sequelize/core';

const LinkCategory = {
  RegionId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  CategoryId: {
    type: DataTypes.INTEGER,
    allowNull: false,
    primaryKey: true,
    autoIncrement: true
  },
  CategoryName: {
    type: DataTypes.STRING(200),
    allowNull: false
  },
  CategoryDescription: {
    type: DataTypes.STRING(500),
    allowNull: true
  },
  CategoryColor: {
    type: DataTypes.STRING(100),
    allowNull: true
  },
  SortOrder: {
    type: DataTypes.INTEGER,
    allowNull: false
  }
};

const LinkCategoryAssociations = {

};

export { LinkCategory, LinkCategoryAssociations };
