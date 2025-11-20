const { DataTypes } = require('sequelize');

const UserInvitation = {
  InvitationGUID: {
    type: DataTypes.UUID,
    allowNull: false,
    primaryKey: true
  },
  Username: {
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
  AlternateEmails: {
    type: DataTypes.STRING(2000),
    allowNull: false
  },
  InvitationDate: {
    type: DataTypes.DATE,
    allowNull: false
  }
};

const UserInvitationAssociations = {
  // Define any associations here, e.g., belongsTo, hasMany, etc.
};

export { UserInvitation, UserInvitationAssociations };
