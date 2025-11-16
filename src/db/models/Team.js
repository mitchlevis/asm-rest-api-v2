import { DataTypes } from '@sequelize/core';

const Team = {
  RegionId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  TeamId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  TeamName: {
    type: DataTypes.STRING(200),
    allowNull: false
  },
  RealTeamId: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  Country: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  State: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  City: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  Address: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  PostalCode: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  IsLinked: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  },
  AllowInfoLink: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  },
  IsInfoLinked: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  },
  TeamShorthand: {
    type: DataTypes.STRING(50),
    allowNull: false
  },
  PhotoId: {
    type: DataTypes.UUID,
    allowNull: true
  },
  HasPhotoId: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  },
  StatLinks: {
    type: DataTypes.STRING(1000),
    allowNull: false
  }
};

const TeamAssociations = {
  belongsTo: [
    {
      modelName: 'Region',
      foreignKey: 'RegionId',
      targetKey: 'RegionID',
    },
  ],
  hasMany: [
    {
      modelName: 'Schedule',
      foreignKey: 'HomeTeam',
      sourceKey: 'TeamId',
    },
    {
      modelName: 'Schedule',
      foreignKey: 'AwayTeam',
      sourceKey: 'TeamId',
    },
  ]
};

export { Team, TeamAssociations };

