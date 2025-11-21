import Sequelize from 'sequelize';
const Op = Sequelize.Op;
import { Config } from "sst/node/config";
import parameters from "./parameters";
import { authenticate, validateIncomingParameters, throwError, getDbObject, getSequelizeObject, parseJSONFieldsInDbResponse, getHeaders, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";

export const handler = async (_evt) => {
  try {
    await authenticate(_evt, Config.API_KEYS);

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);

    const { userId, regionId } = path;
    const sortDirection = query.sort_direction;
    const limit = query.limit;
    const offset = query.offset;
    const showArchived = query.show_archived;

    const regionUserModel = await getDbObject('RegionUser');
    const regionModel = await getDbObject('Region');
    const userModel = await getDbObject('User');
    const whereObject = { RealUsername: userId, isActive: true, isArchived: showArchived };
    if (regionId) {
      whereObject.regionId = regionId;
    }

    // Get all regions for user
    const regionsForUser = await regionUserModel.findAll({ attributes: ['RegionId'], where: whereObject });
    const regionIds = regionsForUser.map((region) => region.RegionId);

    // If no regions, return empty array
    if (regionIds.length === 0) {
      return formatSuccessResponse(_evt, [], 200);
    }

    // Get All Users for regions
    let usersForRegions = await regionUserModel.findAll({
      raw: true,
      attributes: [
        'RealUsername',
        'RegionId',        
        [Sequelize.literal('[Region].[RegionName]'), 'RegionName'],
        [Sequelize.literal('[Region].[RegistrationDate]'), 'RegistrationDate'],
        [Sequelize.literal('[User].[FirstName]'), 'FirstName'],
        [Sequelize.literal('[User].[LastName]'), 'LastName']
      ],
      order: [
        [Sequelize.literal('[User].[LastName]'), sortDirection],
        [Sequelize.literal('[User].[FirstName]'), sortDirection],
      ],
      limit: limit === -1 ? undefined : limit,
      offset,
      include: [
        {
          model: userModel,
          attributes: [],
        },
        {
          model: regionModel,
          attributes: [],
        }
      ],
      where: {
        RegionId: {
          [Op.in]: regionIds
        },
        isArchived: false,
        isActive: true,
        RealUsername: {
          [Op.ne]: ''
        },
      },
    });

    // Adding the Username field to the object
    usersForRegions = usersForRegions.map(user => {
      user.Username = user.RealUsername;
      delete user.RealUsername;
      return user;
    });

    // Create an object to store the count and regions for each user
    let userOccurrences = {};

    // Iterate over the users array
    for (let user of usersForRegions) {
      // Create a unique key for each user
      let userKey = user.Username;

      // If the user doesn't exist in the object, add them
      if (!userOccurrences[userKey]) {
        userOccurrences[userKey] = {
          count: 1,
          regions: [{ RegionId: user.RegionId, RegistrationDate: user.RegistrationDate, RegionName: user.RegionName }]
        };
      } else {
        // If the user does exist, increment the count and add the new region
        userOccurrences[userKey].count++;
        userOccurrences[userKey].regions.push({ RegionId: user.RegionId, RegistrationDate: user.RegistrationDate, RegionName: user.RegionName });
      }
    }

    // Sort regions by RegistrationDate in descending order
    for (let userKey in userOccurrences) {
      userOccurrences[userKey].regions.sort((a, b) => new Date(b.RegistrationDate) - new Date(a.RegistrationDate));
    }

    // Remove duplicates from the users array
    let uniqueUsers = Array.from(new Set(usersForRegions.map(user => user.Username)))
    .map(userKey => {
      return usersForRegions.find(user => user.Username === userKey);
    });

    // Iterate over the unique users array and add the count and regions from the object
    for(let i = 0; i < uniqueUsers.length; i++) {

      let userKey = uniqueUsers[i].Username;
      uniqueUsers[i].Count = userOccurrences[userKey].count;
      uniqueUsers[i].Regions = userOccurrences[userKey].regions;
    }

    return formatSuccessResponse(_evt, uniqueUsers, 200);
  }
  catch (err) {
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};