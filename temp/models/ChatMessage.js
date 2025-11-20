const { DataTypes } = require('sequelize');

const ChatMessage = {
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
  MessageId: {
    type: DataTypes.INTEGER,
    allowNull: false,
    primaryKey: true
  },
  FriendMessageId: {
    type: DataTypes.INTEGER,
    allowNull: false
  },
  ReceiverId: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  SenderId: {
    type: DataTypes.STRING(100),
    allowNull: false
  },
  DateSent: {
    type: DataTypes.STRING, // Using STRING due to datetime issue with sequelize and mssql
    allowNull: false
  },
  Message: {
    type: DataTypes.STRING(2000),
    allowNull: false
  }
};

const ChatMessageAssociations = {
};

export { ChatMessage, ChatMessageAssociations };