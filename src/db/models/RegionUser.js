import { DataTypes } from '@sequelize/core';

const RegionUser = {
  RegionId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  Username: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
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
  RealUsername: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  FirstName: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  LastName: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  Email: {
    type: DataTypes.STRING(200),
    allowNull: false
  },
  PhoneNumbers: {
    type: DataTypes.STRING(2000),
    allowNull: false
  },
  Country: {
    type: DataTypes.STRING(200),
    allowNull: false
  },
  State: {
    type: DataTypes.STRING(200),
    allowNull: false
  },
  City: {
    type: DataTypes.STRING(200),
    allowNull: false
  },
  Address: {
    type: DataTypes.STRING(200),
    allowNull: false
  },
  PostalCode: {
    type: DataTypes.STRING(200),
    allowNull: false
  },
  PreferredLanguage: {
    type: DataTypes.STRING(50),
    allowNull: false
  },
  Rank: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  Positions: {
    type: DataTypes.STRING(2000),
    allowNull: false
  },
  IsArchived: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  },
  IsActive: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  },
  AlternateEmails: {
    type: DataTypes.STRING(2000),
    allowNull: false
  },
  CanViewAvailability: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  },
  CanViewMasterSchedule: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  },
  IsExecutive: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  },
  PublicData: {
    type: DataTypes.STRING(2000),
    allowNull: false
  },
  PrivateData: {
    type: DataTypes.STRING(2000),
    allowNull: false
  },
  InternalData: {
    type: DataTypes.STRING(2000),
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
  },
  RankNumber: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  GlobalAvailabilityData: {
    type: DataTypes.STRING(250),
    allowNull: false
  },
  RankAndDates: {
    type: DataTypes.STRING(500),
    allowNull: false
  },
  CanViewSupervisors: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  }
}

// Optional Assosiatiations
const RegionUserAssociations = {
  belongsTo: [
    {
      modelName: 'User',
      foreignKey: 'RealUsername',
      targetKey: 'Username',
    },
    {
      modelName: 'Region',
      foreignKey: 'RegionId',
      targetKey: 'RegionID',
    }
  ],
  hasMany:[
    {
      modelName: 'RegionLeague',
      foreignKey: 'RegionId',
      targetKey: 'RegionId',
    },
    {
      modelName: 'Team',
      foreignKey: 'RegionId',
      targetKey: 'RegionId',
    },
    {
      modelName: 'Schedule',
      foreignKey: 'RegionId',
      targetKey: 'RegionId',
    },
    {
      modelName: 'SchedulePosition',
      foreignKey: 'OfficialId',
      sourceKey: 'Username',
      // as: 'SchedulePosition',
    },
    {
      modelName: 'ScheduleRequest',
      foreignKey: 'OfficialId',
      sourceKey: 'Username',
      // as: 'ScheduleRequest',
    },
    {
      modelName: 'Availability3',
      foreignKey: 'RegionId',
      sourceKey: 'RegionId',
      as: 'Availability3',
    },
    {
      modelName: 'Availability3',
      foreignKey: 'Username',
      sourceKey: 'Username',
    }
  ]
}

export { RegionUser, RegionUserAssociations };
