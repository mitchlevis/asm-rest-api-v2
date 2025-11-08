import { DataTypes } from '@sequelize/core';

const WallPostLike = {
  UserId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  WallPostId: {
    type: DataTypes.INTEGER,
    allowNull: false,
    primaryKey: true
  },
  LikerId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  LikeType: {
    type: DataTypes.INTEGER,
    allowNull: false
  },
  LikeDate: {
    type: DataTypes.STRING, // Storing datetime as string due to the bug
    allowNull: false
  }
};

const WallPostLikeAssociations = {

};

export { WallPostLike, WallPostLikeAssociations };
