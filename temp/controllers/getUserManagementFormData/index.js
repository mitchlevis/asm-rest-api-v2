import dayjs from 'dayjs';
import Sequelize from 'sequelize';
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, getFilterQueryParameter, replaceOperators, throwError, getDbObject, getSequelizeObject, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { compileAvailabilityRanges, convertBase64ToAvailability } from "@asportsmanager-api/core/business-logic-helpers";
// import { getQuery, formatResult } from "@asportsmanager-api/core/sql_queries/getGlobalAvailabilityRegionUsers";

export const handler = async (_evt) => {
  try{
    const tokenData = await authenticateSessionToken(_evt);
    const userId = tokenData.UserName;

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);
    
    const { regionId } = path;

    const regionUserModel = await getDbObject('RegionUser');

    // Get region for user
    const regionUser = await regionUserModel.findOne({ where: { RegionId: regionId, RealUsername: userId }});

    // If no region, return 404
    if(!regionUser){
      console.log(`User ${userId} is not a member of region ${regionId}`);
      await throwError(404, `Region User not found`);
    }

    // Permissions
    if(!regionUser.IsExecutive && !regionUser.CanViewMasterSchedule){
      await throwError(403, `User ${userId} does not have permission to view region users`);
    }

    // Region Users
    const regionUsers = await regionUserModel.findAll({
      attributes: [
        'RegionId',
        'Username',
        'RealUsername',
        'FirstName',
        'LastName',
        'Email',
        'PhoneNumbers',
        'Country',
        'City',
        'Address',
        'PostalCode',
        'PreferredLanguage',
        'Positions',
        'AlternateEmails',
        'PublicData',
        'PrivateData',
        'InternalData',
        'GlobalAvailabilityData',
      ],
      where: {
        RegionId: regionId
      }
    });

    // Distinct Values & remove 
    const distinctValues = {
      username: [...new Set(regionUsers.map(user => user.Username).filter(value => value))]
        .sort()
        .map(value => ({ value })),
      realUsername: [...new Set(regionUsers.map(user => user.RealUsername).filter(value => value))]
        .sort()
        .map(value => ({ value })),
      firstName: [...new Set(regionUsers.map(user => user.FirstName).filter(value => value))]
        .sort()
        .map(value => ({ value })),
      lastName: [...new Set(regionUsers.map(user => user.LastName).filter(value => value))]
        .sort()
        .map(value => ({ value })),
      email: [...new Set(regionUsers.map(user => user.Email).filter(value => value))]
        .sort()
        .map(value => ({ value })),
      phoneNumbers: [...new Set(
          regionUsers.flatMap(user => {
            if (!user.PhoneNumbers) return [];
            try {
              const parsedNumbers = JSON.parse(user.PhoneNumbers);
              if (Array.isArray(parsedNumbers)) {
                return parsedNumbers.map(num => num.PhoneNumberNumber).filter(value => value);
              }
              return [];
            } catch (e) {
              console.error(`Failed to parse PhoneNumbers for user ${user.RealUsername || user.Username}:`, user.PhoneNumbers, e);
              return [];
            }
          })
        )]
        .sort()
        .map(value => ({ value })),
      country: [...new Set(regionUsers.map(user => user.Country).filter(value => value))]
        .sort()
        .map(value => ({ value })),
      city: [...new Set(regionUsers.map(user => user.City).filter(value => value))]
        .sort()
        .map(value => ({ value })),
      address: [...new Set(regionUsers.map(user => user.Address).filter(value => value))]
        .sort()
        .map(value => ({ value })),
      postalCode: [...new Set(regionUsers.map(user => user.PostalCode).filter(value => value))]
        .sort()
        .map(value => ({ value })),
      preferredLanguage: [...new Set(regionUsers.map(user => user.PreferredLanguage).filter(value => value))]
        .sort()
        .map(value => ({ value })),
      positions: [...new Set(
          regionUsers.flatMap(user => {
            if (!user.Positions) return [];
            try {
              const parsedPositions = JSON.parse(user.Positions);
              if (Array.isArray(parsedPositions)) {
                return parsedPositions.filter(value => value); // Filter out any potentially falsy values within the array
              }
              return [];
            } catch (e) {
              console.error(`Failed to parse Positions for user ${user.RealUsername || user.Username}:`, user.Positions, e);
              return [];
            }
          })
        )]
        .sort()
        .map(value => ({ value })),
      alternateEmails: [...new Set(
          regionUsers.flatMap(user => {
            if (!user.AlternateEmails) return [];
            try {
              const parsedEmails = JSON.parse(user.AlternateEmails);
              if (Array.isArray(parsedEmails)) {
                return parsedEmails.filter(value => value);
              }
              return [];
            } catch (e) {
              console.error(`Failed to parse AlternateEmails for user ${user.RealUsername || user.Username}:`, user.AlternateEmails, e);
              return [];
            }
          })
        )]
        .sort()
        .map(value => ({ value })),
      publicData: [...new Set(regionUsers.map(user => user.PublicData).filter(value => value))]
        .sort()
        .map(value => ({ value })),
      privateData: [...new Set(regionUsers.map(user => user.PrivateData).filter(value => value))]
        .sort()
        .map(value => ({ value })),
      internalData: [...new Set(regionUsers.map(user => user.InternalData).filter(value => value))]
        .sort()
        .map(value => ({ value })),
      globalAvailabilityData: [...new Set(regionUsers.map(user => user.GlobalAvailabilityData).filter(value => value))]
        .sort()
        .map(value => ({ value })),
    }

    return formatSuccessResponse(_evt, { regionId, distinctValues }, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};

const formatResult = (result) => {

  if(!result || result.length === 0){
    return [];
  }

  // Parse special fields
  for(const row of result){

    // Ranks & Positions
    let rankData = {};
    let subRankData = {};
    let positionData = [];

    try{
      rankData = JSON.parse(row.Rank);
    }
    catch(err){
      console.log(`Invalid rank JSON for user ${row.Username}: ${row.Rank} - Skipping`);
    }

    try{
      subRankData = JSON.parse(row.RankNumber);
    }
    catch(err){
      console.log(`Invalid rank number JSON for user ${row.Username}: ${row.RankNumber} - Skipping`);
    }

    try{
      positionData = JSON.parse(row.Positions);
    }
    catch(err){
      console.log(`Invalid position JSON for user ${row.Username}: ${row.Positions} - Skipping`);
    }

    row.Ranks  = rankData;
    row.SubRanks = subRankData;
    row.Positions = positionData;

    // Availability
    if(row.Availability && row.AvailabilityYear && row.AvailabilityMonth){
      row.Availability = convertBase64ToAvailability(row.AvailabilityYear, row.AvailabilityMonth, row.Availability)
    }

    // Schedules
    try{
      row.SchedulePositions = JSON.parse(row.SchedulePositions);
    }
    catch(e){
      row.SchedulePositions = null;
    }
  }

  return result;
};

const getMonthViewAvailability = (year, month, users, rankType) => {
  const daysInMonth = dayjs(`${year}-${month}-01`).daysInMonth();
  const monthViewAvailability = [];

  // Loop through each day of the month
  for(let i = 1; i <= daysInMonth; i++){
    monthViewAvailability.push({
      date: dayjs(`${year}-${month}-${i}`).format('YYYY-MM-DD'),
      available: 0,
      partiallyAvailable: 0,
      unavailable: 0,
      unfilled: 0,
    })
  }

  // Loop through each user
  for(let i = 0; i < users.length; i++){
    const user = users[i];

    // If null, set to unfilled for every day 
    if(user.Availability === null){
      for(let j = 0; j < daysInMonth; j++){
        monthViewAvailability[j].unfilled++;
      }
    }
    else{
      for(const day of Object.keys(user.Availability)){
        const dayIndex = parseInt(day) - 1;
        const dayAvailability = user.Availability[day];

        if(!dayAvailability || dayAvailability.length === 0){
          monthViewAvailability[dayIndex].unavailable++;
        }
        else{
          if(rankType && rankType !== null){
            const userRanks = user.Ranks;

            // Check is the user has the rank_type in their ranks, if not skip
            if(!Object.keys(userRanks).includes(rankType)){
              console.log(`User ${user.Username} does not have rank type ${rankType}, skipping`);
              continue;
            }
          }

          // We assume 'un-available' and check to see if there is at least 1 hour where user is available
          let isAvailable = false;
          let isPartiallyAvailable = false;

          // Loop through each hour of the day
          for(let j = 0; j < dayAvailability.length; j++){
            const hourAvailability = dayAvailability[j];
            
            switch(hourAvailability){
              // Partially Available 
              case 3:
                isPartiallyAvailable = true;
              // Available for Game OR Both
              case 1:
              case 4:
                isAvailable = true;
                break;
              default:
                break;
            }
          }

          // Determine which counter to increment
          if(isAvailable && !isPartiallyAvailable){
            monthViewAvailability[dayIndex].available++;
          }
          else if(isPartiallyAvailable){
            monthViewAvailability[dayIndex].partiallyAvailable++;
          }
          else{
            monthViewAvailability[dayIndex].unavailable++;
          }
        }
      }
    }
  }
  
  return monthViewAvailability;
}

const getDailyViewAvailability = (year, month, users, showAvailable, showUnavailable, showPartiallyAvailable, showNotFilledIn) => {
  const daysInMonth = dayjs(`${year}-${month}-01`).daysInMonth();
  const dailyViewAvailability = [];

  // Loop through each day of the month and initialize an array of users for that day
  for(let i = 1; i <= daysInMonth; i++){
    // Individual Users will populate this array
    dailyViewAvailability.push([]);
  }

  // Loop through each user
  for(let i = 0; i < users.length; i++){
    const user = users[i];

    // If null, set to unfilled for every day
    if(user.Availability === null){
      // if(showNotFilledIn){
        for(let j = 0; j < daysInMonth; j++){
          dailyViewAvailability[j].push({
            date: dayjs(`${year}-${month}-${j + 1}`).format('YYYY-MM-DD'),
            username: user.Username,
            realUsername: user.RealUsername,
            firstName: user.FirstName,
            lastName: user.LastName,
            ranks: user.Ranks,
            subRanks: user.SubRanks,
            availabilityStatus: 'unfilled'
          })
        // }
      }
    }
    else{
      const userAvailability = user.Availability;

      const availabilityRanges = compileAvailabilityRanges(userAvailability);
      // { day: 1, status: 'available', from: 0, to: 20 },

      // Loop through each day of the month
      for(let j = 1; j <= daysInMonth; j++){
        const rangesForDay = availabilityRanges.filter(range => range.day === j);

        // 1. If no ranges for day, set to unavailable
        if(rangesForDay.length === 0){
          dailyViewAvailability[j - 1].push({
            date: dayjs(`${year}-${month}-${j}`).format('YYYY-MM-DD'),
            username: user.Username,
            realUsername: user.RealUsername,
            firstName: user.FirstName,
            lastName: user.LastName,
            ranks: user.Ranks,
            subRanks: user.SubRanks,
            availabilityStatus: 'unavailable'
          })
        }
        // 2. If some ranges are status=available, set to available
        else if(rangesForDay.some(range => range.status === 'available')){
          dailyViewAvailability[j - 1].push({
            date: dayjs(`${year}-${month}-${j}`).format('YYYY-MM-DD'),
            username: user.Username,
            realUsername: user.RealUsername,
            firstName: user.FirstName,
            lastName: user.LastName,
            ranks: user.Ranks,
            subRanks: user.SubRanks,
            schedulePositions: user.SchedulePositions ? (user.SchedulePositions.filter(sp => sp.GameDay === j).length > 0 ? user.SchedulePositions.filter(sp => sp.GameDay === j) : null) : null,
            availabilityStatus: 'available',
            availabilityRanges: rangesForDay
          })
        }
        // 3. If ranges are all status=unavailable, set to unavailable
        else if(rangesForDay.every(range => range.status === 'unavailable')){
          dailyViewAvailability[j - 1].push({
            date: dayjs(`${year}-${month}-${j}`).format('YYYY-MM-DD'),
            username: user.Username,
            realUsername: user.RealUsername,
            firstName: user.FirstName,
            lastName: user.LastName,
            ranks: user.Ranks,
            subRanks: user.SubRanks,
            availabilityStatus: 'unavailable',
            availabilityRanges: rangesForDay
          })
        }
        // 
        else {
          dailyViewAvailability[j - 1].push({
            date: dayjs(`${year}-${month}-${j}`).format('YYYY-MM-DD'),
            username: user.Username,
            realUsername: user.RealUsername,
            firstName: user.FirstName,
            lastName: user.LastName,
            ranks: user.Ranks,
            subRanks: user.SubRanks,
            availabilityStatus: 'partially_available',
            availabilityRanges: rangesForDay
          })
        }
      }
    }
  }

  return dailyViewAvailability;
}