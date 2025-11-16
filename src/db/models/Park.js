import { DataTypes } from '@sequelize/core';

const Park = {
  RegionId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  ParkId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  ParkName: {
    type: DataTypes.STRING(200),
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
  Longitude: {
    type: DataTypes.DECIMAL,
    allowNull: false
  },
  Latitude: {
    type: DataTypes.DECIMAL,
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

const ParkAssociations = {
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
      foreignKey: 'ParkId',
      sourceKey: 'ParkId',
    },
  ],
};

export { Park, ParkAssociations };

