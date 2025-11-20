/*
  This file is dedicated to functions/helpers that specifically deal with "Business Logic"

  Availability Cheat-Sheet
  ------------------------
  0 = Not Filled In
  1 = Available for Games
  2 = Not available
  3 = Available for Practices
  4 = Available for Both

*/
const dayjs = require('dayjs');

//  Function to get the location type based on sport
export const getLocationType = (sport) => {
  switch(sport.toLowerCase()){
    case 'baseball':
      return 'field';
    case 'basketball':
      return 'court';
    case 'football':
      return 'field';
    case 'hockey':
      return 'arena';
    case 'soccer':
      return 'field';
    case 'softball':
      return 'field';
    case 'volleyball':
      return 'court';
    default:
      return 'field';
  }
}

/*
  By default we assume everyone gets the umpire pay.
  We then check if the position is scorekeeper or supervisor and update the pay accordingly
*/
export const getPayForPosition = (payOptions, positionId, crewType) => {
  if(!payOptions || payOptions.length === 0 || !positionId || !crewType){
    return null;
  }
  let type = 'umpire';

  if(positionId === 'scorekeeper'){
    type = 'scorekeeper';
  }
  else if(positionId === 'supervisor'){
    type = 'supervisor';
  }
  const payRate = crewType[type];
  const payAmount = payOptions.find((pay) => pay.CrewType === payRate && pay.PositionId === positionId)?.Pay;

  return payAmount;
};

//  Function to get the fines for a specific username
export const getFinesForRealUsername = (finesArray, realUsername) => {
console.log('finesArray, realUsername', finesArray, realUsername);
  let fines = [];

  if(finesArray && finesArray.length > 0){
    for(const fine of finesArray){
      if(fine.RealUsername === realUsername){
        fines.push(fine);
      }
    }
  }

  return fines;
};

/*
  Function to get the game fees for a specific row (Schedule)
  Looks for the nested arrays:
  - row.Positions
  - row.Fines
*/
export const getGameFeesForRow = (row) => {
  let total = 0;
  let items = [];

  // Game Pay
  if(row.Positions && row.Positions.length > 0){
    for(const position of row.Positions){

      if(position.positionUsers && position.positionUsers.length > 0){
        for(const positionUser of position.positionUsers){

          if(positionUser.Pay != 0){
            items.push({
              type: 'position_pay',
              position: position.positionName,
              username: positionUser.Username,
              realUsername: positionUser.RealUsername,
              amount: positionUser.Pay
            });

            if(positionUser.Pay){
              total += positionUser.Pay;
            }
          }
        }
      }
    }
  }

  // Fines
  if(row.Fines && row.Fines.length > 0){
    for(const fine of row.Fines){
      items.push({
        type: 'adjustment',
        username: fine.OfficialId,
        realUsername: fine.RealUsername,
        amount: fine.Amount,
        comment: fine.Comment
      });

      total += fine.Amount;
    }
  }

  return { total, items }
}

/*
  This will accepts 2 arrays of schedule and compare the second to the first.
  It will add meta data => { ...array, { meta: { status: 'Added', diff: {} } } }
*/
export const compareScheduleToScheduleTempAndAddMetaData = (schedule, scheduleTemp) => {
  const createUniqueId = (event) => `${event.RegionId}-${event.ScheduleId}`;
  const scheduleMap = new Map(schedule.map(event => [createUniqueId(event), event]));

  const findDifferences = (original, modified) => {
    const changes = {};

    Object.keys(modified).forEach(key => {
      if (original[key] !== modified[key] && key !== 'meta') {
        changes[key] = modified[key];
      }
    });

    return changes;
  };

  // Create new list with added Meta Data
  let combinedList = scheduleTemp.map(event => {
    const uniqueId = createUniqueId(event);
    const originalEvent = scheduleMap.get(uniqueId);
    const eventCopy = { ...event, meta: {} };

    if (originalEvent) {
      // Deleted
      if (event.IsDeleted) {
        eventCopy.meta.status = 'Removed';
        // Recover Data -For some reason, when a Schedule isDeleted in old site, it removes the GameNumber & GameType
        eventCopy.GameNumber = originalEvent.GameNumber
        eventCopy.GameType = originalEvent.GameType
      }
      else {
        const diffs = findDifferences(originalEvent, event);

        // Changed
        if (Object.keys(diffs).length > 0) {
          eventCopy.meta.status = 'Changed';
          eventCopy.meta.diff = diffs;

          // If there are changes in the Positions, we loop through them and annotate
          if(originalEvent.Positions && event.Positions && originalEvent.Positions !==  event.Positions) {
            eventCopy.PositionsModified = true;

            // Parse the JSON strings into objects
            const originalPositions = JSON.parse(originalEvent.Positions);
            const draftPositions = JSON.parse(event.Positions);

            // Create dictionaries to track positions by PositionId
            const originalPositionMap = {};
            const draftPositionMap = {};

            // Fill the dictionaries
            originalPositions.forEach(pos => {
              originalPositionMap[pos.PositionId] = pos;
            });
            draftPositions.forEach(pos => {
              draftPositionMap[pos.PositionId] = pos;
            });

            // Annotate new and modified positions in the draft
            draftPositions.forEach(draftPos => {
              const originalPos = originalPositionMap[draftPos.PositionId];
              if (!originalPos) {
                draftPos.isAdded = true; // Mark as added
              }
              else {
                // Check for modifications
                const isModified = Object.keys(draftPos).some(key => {
                  return key !== 'PositionId' && draftPos[key] !== originalPos[key];
                });
                if (isModified) {
                  draftPos.isModified = true; // Mark as modified
                }
              }
            });

            // Annotate removed positions in the original
            originalPositions.forEach(pos => {
              if (!draftPositionMap[pos.PositionId]) {
                pos.isRemoved = true; // Mark as removed
              }
            });

            // Combine the results
            const combinedResults = [...draftPositions, ...originalPositions.filter(pos => pos.isRemoved)];

            // Stringify the combined results object
            eventCopy.Positions = JSON.stringify(combinedResults);
          }

          // If there are changes in Fines, we merge them
          if(originalEvent.Fines && event.Fines && originalEvent.Fines !==  event.Fines){

            // Parse
            const originalFines = JSON.parse(originalEvent.Fines);
            const draftFines = JSON.parse(event.Fines);

            // Merge
            const mergedFinesArray = draftFines.concat(originalFines).filter((item, index, self) =>
              index === self.findIndex((t) => t.OfficialId === item.OfficialId)
            );

            eventCopy.Fines = JSON.stringify(mergedFinesArray);
          }

          // If there changes in User Comments, we merge them
          if(originalEvent.UserComments && event.UserComments && originalEvent.UserComments !==  event.UserComments){

            // Parse
            const originalUserComments = JSON.parse(originalEvent.UserComments);console.log('originalFines', originalUserComments)
            const draftUserComments = JSON.parse(event.UserComments);console.log('draftFines', draftUserComments);

            // Merge
            const mergedUserCommentsArray = draftUserComments.concat(originalUserComments).filter((item, index, self) =>
              index === self.findIndex((t) => t.OfficialId === item.OfficialId)
            );

            eventCopy.UserComments = JSON.stringify(mergedUserCommentsArray);
          }
        }
        // Unchanged
        else {
          eventCopy.meta.status = 'Unchanged';
        }
      }
    }
    //  New
    else {
      eventCopy.meta.status = 'Added';
    }
    return eventCopy;
  });

  const onlyInSchedule = schedule.filter(event => !scheduleTemp.some(tempEvent => createUniqueId(tempEvent) === createUniqueId(event)))
                                 .map(event => ({ ...event, meta: { diff: {} } }));

  combinedList = combinedList.concat(onlyInSchedule);

  return combinedList;
}

/*
  Decode base64 Availability
*/
export const convertBase64ToAvailability = (year, month, base64String) => {
  const daysInMonth = new Date(year, month, 0).getDate();
  const result = {};
  const buffer = Buffer.from(base64String, 'base64');

  let byteIndex = 0;
  let shift = 0;

  for (let i = 1; i <= daysInMonth; i++) {
      result[i] = [];
      for (let n = 0; n < 24; n++) {
          const value = (buffer[byteIndex] & (15 << shift)) >> shift;
          result[i].push(value);
          shift += 4;
          if (shift === 8) {
              byteIndex++;
              shift = 0;
          }
      }
  }

  return result;
}

/*
  Check the decoded availability data to see if available for a given game type
*/

export const isOfficialAvailableForGameType = (availabilityData, gameType, day, hour) => {
console.log('gameType, day, hour', gameType, day, hour);
  if(availabilityData && availabilityData[day] && availabilityData[day][hour]) {
    console.log(`availabilityData[${day}][${hour}]`, availabilityData[day][hour]);
    switch(gameType){
      case 'practice':
        return availabilityData[day][hour] === 3 || availabilityData[day][hour] === 4
      case 'game':
      case 'playoff':
      case 'exhibition':
        return availabilityData[day][hour] === 1 || availabilityData[day][hour] === 4
    }
  }

  // If the data does not exist for the given day or hour, assume not available
  return false;
}

export const compileAvailabilityRanges = (userAvailability, month = null, year = null) => {
  const statusMap = {
    0: 'unfilled',
    1: 'available',
    2: 'unavailable',
    3: 'partially_available',
    4: 'available'
  };

  const ranges = [];

  for (const day in userAvailability) {
    let currentStatus = null;
    let startHour = 0;

    userAvailability[day].forEach((status, hour) => {
      const newStatus = statusMap[status];

      if (newStatus !== currentStatus) {
        if (currentStatus !== null) {
          const newRange = {
            day: parseInt(day),
            status: currentStatus,
            from: startHour,
            to: hour
          };
          if(month){
            newRange.month = month;
          }
          if(year){
            newRange.year = year;
          }
          ranges.push(newRange);
        }
        currentStatus = newStatus;
        startHour = hour;
      }
    });

    // Add the last range of the day
    if (currentStatus !== null) {
      const newRange = {
        day: parseInt(day),
        status: currentStatus,
        from: startHour,
        to: 24
      };
      if(month){
        newRange.month = month;
      }
      if(year){
        newRange.year = year;
      }
      ranges.push(newRange);
    }
  }

  return ranges;
}

/*
  Groups the events that are on the same day by ParkId
*/
export const groupSchedulesByParkId = (schedules) => {
  const groupedSchedules = {};
console.log('Original: ', schedules)

  for(const schedule of schedules){
    const date = dayjs(schedule.GameDate).format('DD/MM/YYYY');
    const key = `${date}-${schedule.ParkId}`;
    if (!groupedSchedules[key]) {
       groupedSchedules[key] = [];
     }
     groupedSchedules[key].push(schedule);
  };
console.log('Grouped: ', groupedSchedules)
  const reorderedSchedules = Object.values(groupedSchedules).flat();
console.log('Reordered: ', reorderedSchedules)
  return reorderedSchedules;
};

/*
  Get Rank Order (Hardcoded Logic from legacy site)
*/
export const getRankOrder = () => {
  return [
    'rookie',
    'novice',
    'intermediate',
    'junior',
    'senior',
  ]
}
