import { DataTypes } from '@sequelize/core';
import { safeJSONParse } from './abc';

const RegionLeague = {
  RegionId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  LeagueId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  RealLeagueId: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  LeagueName: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  DefaultCrewType: {
    type: DataTypes.STRING(200),
    allowNull: true,
    get() {
      const rawValue = this.getDataValue('DefaultCrewType');
      return safeJSONParse(rawValue);
    },
    set(value) {
      this.setDataValue('DefaultCrewType', JSON.stringify(value));
    }
  },
  MaxGameLengthMins: {
    type: DataTypes.INTEGER,
    allowNull: false
  },
  ArriveBeforeMins: {
    type: DataTypes.INTEGER,
    allowNull: false
  },
  Rank: {
    type: DataTypes.INTEGER,
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
  AutoLinkSchedule: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  },
  AllowAutoGameCancellationBeforeHours: {
    type: DataTypes.INTEGER,
    allowNull: false
  },
  ArriveBeforeAwayMins: {
    type: DataTypes.INTEGER,
    allowNull: false
  },
  ArriveBeforePracticeMins: {
    type: DataTypes.INTEGER,
    allowNull: false
  },
  MaxGameLengthPracticeMins: {
    type: DataTypes.INTEGER,
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
  MinRankAllowed: {
    type: DataTypes.STRING(1000),
    allowNull: false,
    get() {
      const rawValue = this.getDataValue('MinRankAllowed');
      return safeJSONParse(rawValue);
    },
    set(value) {
      this.setDataValue('MinRankAllowed', JSON.stringify(value));
    }
  },

  MinRankNumberAllowed: {
    type: DataTypes.STRING(1000),
    allowNull: false,
    get() {
      const rawValue = this.getDataValue('MinRankNumberAllowed');
      return safeJSONParse(rawValue);
    },
    set(value) {
      this.setDataValue('MinRankNumberAllowed', JSON.stringify(value));
    }
  },

  MaxRankAllowed: {
    type: DataTypes.STRING(1000),
    allowNull: false,
    get() {
      const rawValue = this.getDataValue('MaxRankAllowed');
      return safeJSONParse(rawValue);
    },
    set(value) {
      this.setDataValue('MaxRankAllowed', JSON.stringify(value));
    }
  },

  MaxRankNumberAllowed: {
    type: DataTypes.STRING(1000),
    allowNull: false,
    get() {
      const rawValue = this.getDataValue('MaxRankNumberAllowed');
      return safeJSONParse(rawValue);
    },
    set(value) {
      this.setDataValue('MaxRankNumberAllowed', JSON.stringify(value));
    }
  }
};

const RegionLeagueAssociations = {
  belongsTo: [
    {
      modelName: 'Region',
      foreignKey: 'RegionId',
      targetKey: 'RegionID',
      as: 'Region'
    },
    {
      modelName: 'Region',
      foreignKey: 'RealLeagueId',
      targetKey: 'RegionID',
      as: 'RealLeague'
    },
  ],
  hasMany: [
    {
      modelName: 'RegionUser',
      foreignKey: 'RegionId',
      sourceKey: 'RegionId',
    },
  ]
};

export { RegionLeague, RegionLeagueAssociations };

