import { DataTypes } from '@sequelize/core';

const WallPost = {
  UserId: {
    type: DataTypes.STRING,
    allowNull: false,
    primaryKey: true
  },
  WallPostId: {
    type: DataTypes.INTEGER,
    allowNull: false,
    primaryKey: true
  },
  PosterId: {
    type: DataTypes.STRING,
    allowNull: false
  },
  Post: {
    type: DataTypes.TEXT,
    allowNull: false
  },
  Link: {
    type: DataTypes.STRING,
    allowNull: true
  },
  PostDate: {
    type: DataTypes.STRING,
    allowNull: false
  },
  PostType: {
    type: DataTypes.INTEGER,
    allowNull: false
  }
};

const WallPostAssociations = {

};

export { WallPost, WallPostAssociations };
