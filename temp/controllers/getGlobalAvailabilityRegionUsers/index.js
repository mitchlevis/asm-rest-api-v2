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
    const { year, month, rank_type, show_available, show_unavailable, show_partially_available, show_not_filled_in } = query;

    const filter = getFilterQueryParameter(_evt.queryStringParameters)
    // Formatting
    let formattedFilter;
    if(filter){
      formattedFilter = replaceOperators(filter);
    }
    // Get all regions for user
    const regionUserModel = await getDbObject('RegionUser');

    const whereObject = { RealUsername: userId, CanViewAvailability: true }; // CanViewAvailability is a flag to indicate that the user has rights to view availability
    if(regionId){
      whereObject.regionId = regionId;
    }

    const regionsForUser = await regionUserModel.findAll({ attributes: ['RegionId'], where: whereObject});
    const regionIds = regionsForUser.map((region) => region.RegionId);

    // If no regions, return 403 Forbidden
    if(regionIds.length === 0){
      await throwError(403, `User does not have admin access to ${regionId ? 'this': 'these'} region${regionId ? '': 's'}`);
    }
    /*
      TESTING
    */
    const availability3Model = await getDbObject('Availability3');

    const queryObject = {
      raw: true,
      attributes: [
        'Username',
        'RealUsername',
        'FirstName',
        'LastName',
        'Rank',
        'RankNumber',
        'Positions',
        'PublicData',
        'PrivateData',
        'InternalData',
        'GlobalAvailabilityData',
        [Sequelize.col('Availability3.Availability'), 'Availability'],
        [Sequelize.fn('YEAR', Sequelize.col('Availability3.AvailabilityDate')), 'AvailabilityYear'],
        [Sequelize.fn('MONTH', Sequelize.col('Availability3.AvailabilityDate')), 'AvailabilityMonth'],
        [Sequelize.literal(`(
          SELECT sp.RegionId, sp.ScheduleId, s.GameDate, DAY(s.GameDate) as GameDay
          FROM SchedulePosition sp
          LEFT JOIN Schedule s ON s.ScheduleId = sp.ScheduleId AND s.RegionId = sp.RegionId
          WHERE sp.RegionId = RegionUser.RegionId
          AND sp.OfficialId = RegionUser.Username
          AND YEAR(s.GameDate) = ${year}
          AND MONTH(s.GameDate) = ${month}
          FOR JSON PATH
        )`), 'SchedulePositions']
      ],
      include: [
        {
          model: availability3Model,
          as: 'Availability3',
          attributes: [],
          required: false,
          where: {
            [Sequelize.Op.and]: [
              { RegionId: { [Sequelize.Op.eq]: Sequelize.col('RegionUser.RegionId') } },
              { Username: { [Sequelize.Op.eq]: Sequelize.col('RegionUser.Username') } },
              Sequelize.where(Sequelize.fn('YEAR', Sequelize.col('Availability3.AvailabilityDate')), year),
              Sequelize.where(Sequelize.fn('MONTH', Sequelize.col('Availability3.AvailabilityDate')), month)
            ]
          }
        }
      ],
      where: {
        RegionId: {
          [Sequelize.Op.in]: regionIds
        },
        ...formattedFilter
      },
    };
    
    const queryResult = await regionUserModel.findAll(queryObject);



    // Get Availability
    // const SQL = getQuery(regionIds, year, month);

    // const sequelize = await getSequelizeObject();
    // const queryResult = await sequelize.query(SQL, { type: sequelize.QueryTypes.SELECT })

    const regionUserAvailability = formatResult(queryResult);

    // Get Monthly-View Availability
    const monthViewAvailability = getMonthViewAvailability(year, month, regionUserAvailability, rank_type);
    // Get Daily-View Availability
    const dailyViewAvailability = getDailyViewAvailability(year, month, regionUserAvailability, show_available, show_unavailable, show_partially_available, show_not_filled_in);

    return formatSuccessResponse(_evt, { daily: dailyViewAvailability, month: monthViewAvailability }, 200);
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

    // If rankType is provided, check if the user has the rankType in their ranks, if not skip
    if(rankType && rankType !== null){
      const userRanks = user.Ranks;

      // Check is the user has the rank_type in their ranks, if not skip
      if(!Object.keys(userRanks).includes(rankType)){
        console.log(`User ${user.Username} does not have rank type ${rankType}, skipping`);
        continue;
      }
    }

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
            publicData: user.PublicData,
            privateData: user.PrivateData,
            internalData: user.InternalData,
            globalAvailabilityData: user.GlobalAvailabilityData,
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
            publicData: user.PublicData,
            privateData: user.PrivateData,
            internalData: user.InternalData,
            globalAvailabilityData: user.GlobalAvailabilityData,
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
            publicData: user.PublicData,
            privateData: user.PrivateData,
            internalData: user.InternalData,
            globalAvailabilityData: user.GlobalAvailabilityData,
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
            publicData: user.PublicData,
            privateData: user.PrivateData,
            internalData: user.InternalData,
            globalAvailabilityData: user.GlobalAvailabilityData,
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
            publicData: user.PublicData,
            privateData: user.PrivateData,
            internalData: user.InternalData,
            globalAvailabilityData: user.GlobalAvailabilityData,
            availabilityStatus: 'partially_available',
            availabilityRanges: rangesForDay
          })
        }
      }
    }
  }

  return dailyViewAvailability;
}