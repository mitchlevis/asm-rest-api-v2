import { DataTypes } from '@sequelize/core';

const WallPostCommentCommentLike = {
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
  CommentId: {
    type: DataTypes.INTEGER,
    allowNull: false,
    primaryKey: true
  },
  CommentCommentId: {
    type: DataTypes.INTEGER,
    allowNull: false,
    primaryKey: true
  },
  CommentLikerId: {
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

const WallPostCommentCommentLikeAssociations = {

};

export { WallPostCommentCommentLike, WallPostCommentCommentLikeAssociations };
