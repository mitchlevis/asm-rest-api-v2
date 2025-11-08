import { DataTypes } from '@sequelize/core';

const WallPostCommentComment = {
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
  CommenterId: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  Comment: {
    type: DataTypes.STRING(2000),
    allowNull: false
  },
  CommentDate: {
    type: DataTypes.STRING, // Storing datetime as string due to the bug
    allowNull: false
  },
  IsPhoto: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  }
};

const WallPostCommentCommentAssociations = {

};

export { WallPostCommentComment, WallPostCommentCommentAssociations };
