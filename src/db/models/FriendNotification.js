import { DataTypes } from '@sequelize/core';

const FriendNotification = {
  Username: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  FriendId: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  FriendUsername: {
    type: DataTypes.STRING(100),
    allowNull: false,
    primaryKey: true
  },
  DateCreated: {
    type: DataTypes.DATE,
    allowNull: false
  },
  IsViewed: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  },
  Positions: {
    type: DataTypes.STRING(2000),
    allowNull: false,
    get() {
      const rawValue = this.getDataValue('Positions');
      return safeJSONParse(rawValue);
    },
    set(value) {
      try {
        this.setDataValue('Positions', JSON.stringify(value || []));
      } catch (e) {
        console.error('Error stringifying Positions JSON:', e);
        // Handle error appropriately, maybe set to empty array string?
        this.setDataValue('Positions', '[]');
      }
    }
  },
  Denied: {
    type: DataTypes.BOOLEAN,
    allowNull: false
  }
};

const FriendNotificationAssociations = {

};

export { FriendNotification, FriendNotificationAssociations };
