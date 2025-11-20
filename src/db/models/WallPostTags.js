import { DataTypes } from '@sequelize/core';

const WallPostTags = {
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
  TaggedId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  TaggerId: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  TaggedDate: {
    type: DataTypes.STRING,
    allowNull: false
  }
};

const WallPostTagsAssociations = {
};

export { WallPostTags, WallPostTagsAssociations };

