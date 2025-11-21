<!-- 605be49d-eba6-442b-94c6-aa186d024746 aa84dfa1-1948-423e-963c-c7a4e93b1745 -->
# API Migration Analysis: GET api/basicinformation

## Executive Summary

The `api/basicinformation` endpoint aggregates 9 distinct data components into a single response object. This analysis provides SQL queries, execution order, parameters, and JavaScript implementation logic for migrating this endpoint to Node.js.

## Endpoint Flow

**Controller**: `BasicInformationController.GetValues()`

- Validates session token via cookies
- Extracts username from cookies
- Calls `User.GetBasicInformation(username)`

**Authentication**: Session token must be validated before proceeding

## Response Structure

```javascript
{
  Success: true,
  User: {...},                      // User account information
  Regions: [...],                   // Regions with positions
  RegionWithUsers: [...],           // Regions with full user profiles
  RegionUsers: [...],               // From game notifications
  RegionParks: [...],               // From game notifications  
  Teams: [...],                     // From game notifications
  RegionLeagues: [...],             // From game notifications
  RegionProperties: [...],          // Additional region properties
  FriendNotifications: [...],       // Top 5 friend requests
  AvailabilityDueDateUsers: [...],  // Availability due dates
  GameNotifications: [...],         // Filtered schedule items
  LastClicked: {...},               // Last clicked region/username
  HasSubmittedInfo: boolean         // User info submission status
}
```

## SQL Queries (Execution Order)

### 1. Get User Info

**Purpose**: Fetch authenticated user's account details

**Parameters**: `@Username` (string)

```sql
SELECT 
  Username, Firstname, Lastname, Email, PhoneNumbers, AlternateEmails, 
  Country, State, City, Address, PostalCode, PreferredLanguage, 
  EmailAvailableGames, EmailAvailabilityReminders, EmailGamesRequiringConfirm, 
  SMSGameReminders, SMSLastMinuteChanges, SMSAvailabilityReminders, 
  NextLadderLeaguePaymentDue, TimeZone, ICSToken, PhotoId, HasPhotoId, HasBetaAccess 
FROM [User] 
WHERE Username = @Username
```

**JavaScript Mapping**:

- Parse JSON fields: `PhoneNumbers` (array of objects with Number, Extension, Type), `AlternateEmails` (array of strings)
- Return error `{Success: false, ErrorCode: "UserDoesNotExist"}` if no user found

### 2. Get Last Clicked

**Purpose**: Retrieve last clicked region and username for UI state

**Parameters**: `@Username` (string)

```sql
SELECT LastClickedRegion, LastClickedUsername 
FROM UsernameLastClicked 
WHERE Username = @Username
```

**JavaScript Logic**:

- Default to `{RegionId: "Select", Username: "Select"}` if no record found

### 3. Get Regions with Positions

**Purpose**: Load all regions user belongs to with their positions

**Parameters**: `@Username` (string)

```sql
SELECT 
  R.RegionId, R.RegionName, R.Sport, R.EntityType, R.Season, R.RegistrationDate, 
  R.DefaultCrew, R.ShowAddress, R.ShowRank, R.ShowSubRank, R.ShowRankToNonMembers, 
  R.ShowSubRankToNonMembers, R.DefaultContactListSort, R.ShowRankInSchedule, 
  R.ShowSubRankInSchedule, R.MaxSubRankRequest, R.SubRankIsNumber, R.ShowTeamsInSchedule, 
  R.SortSubRankDesc, R.SeniorRankNameEnglish, R.SeniorRankNameFrench, 
  R.JuniorRankNameEnglish, R.JuniorRankNameFrench, R.IntermediateRankNameEnglish, 
  R.IntermediateRankNameFrench, R.NoviceRankNameEnglish, R.NoviceRankNameFrench, 
  R.RookieRankNameEnglish, R.RookieRankNameFrench, R.AllowBookOffs, R.BookOffHoursBefore, 
  R.MaxDateShowAvailableSpots, R.EmailAvailableSpots, R.EmailRequestedGames, 
  R.DefaultArriveBeforeMins, R.DefaultMaxGameLengthMins, R.OnlyAllowedToConfirmDaysBefore, 
  R.EmailConfirmDaysBefore, R.HasOfficials, R.HasScorekeepers, R.HasSupervisors, 
  R.IsDemo, R.Country, R.State, R.City, R.Address, R.PostalCode, R.ShowLinksToNonMembers, 
  R.ShowParksToNonMembers, R.ShowLeaguesToNonMembers, R.ShowOfficialRegionsToNonMembers, 
  R.ShowTeamsToNonMembers, R.ShowHolidayListToNonMembers, R.ShowContactListToNonMembers, 
  R.ShowAvailabilityDueDateToNonMembers, R.ShowAvailabilityToNonMembers, 
  R.ShowAvailableSpotsToNonMembers, R.ShowFullScheduleToNonMembers, R.ShowStandingsToNonMembers, 
  R.ShowStatsToNonMembers, R.ShowMainPageToNonMembers, R.HasEnteredMainPage, 
  R.ExtraScoreParameters, R.HomeTeamCanEnterScore, R.AwayTeamCanEnterScore, 
  R.ScorekeeperCanEnterScore, R.TeamCanEnterStats, R.ScorekeeperCanEnterStats, 
  R.RegionIsLadderLeague, R.NumberOfPlayersInLadderLeague, R.HomeTeamCanCancelGameHoursBefore, 
  R.AwayTeamCanCancelGameHoursBefore, R.MinimumValue, R.MinimumPercentage, R.TimeZone, 
  R.AutoSyncCancellationHoursBefore, R.AutoSyncSchedule, R.UniquePlayers, R.StatIndex, 
  R.StandingIndex, R.DefaultArriveBeforeAwayMins, R.DefaultArriveBeforePracticeMins, 
  R.DefaultMaxGameLengthPracticeMins, R.IncludeAfternoonAvailabilityOnWeekdays, 
  R.IncludeMorningAvailabilityOnWeekdays, R.EnableNotFilledInInAvailability, 
  R.EnableDualAvailability, R.IsAvailableText, R.IsAvailableDualText, 
  R.IsAvailableCombinedText, R.ShowOnlyDueDatesRangeForAvailability, 
  R.ShowRankInGlobalAvailability, R.ShowSubRankInGlobalAvailability, 
  R.SortGlobalAvailabilityByRank, R.SortGlobalAvailabilityBySubRank, 
  R.NotifyPartnerOfCancellation, R.ShowPhotosToNonMembers, R.ShowArticlesToNonMembers, 
  R.ShowWallToNonMembers, R.LeagueRankMaxes, R.LinkedRegionIds, R.PhotoId, R.HasPhotoId, 
  RU.Positions, RU.Username, RU.IsArchived 
FROM Region AS R, RegionUser AS RU 
WHERE RU.RealUsername = @Username AND R.RegionId = RU.RegionId 
ORDER BY R.RegionName
```

**JavaScript Parsing**: Parse JSON fields - `DefaultCrew`, `ExtraScoreParameters`, `MinimumValue`, `MinimumPercentage`, `IsAvailableText`, `IsAvailableDualText`, `IsAvailableCombinedText`, `LeagueRankMaxes`, `LinkedRegionIds`, `Positions`

### 4. Get Region with Users

**Purpose**: Full region and user profile information

**Parameters**: `@RealUsername` (string)

```sql
SELECT
  RU.RegionId, RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.RealUsername, RU.FirstName, 
  RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, 
  U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, 
  RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, 
  RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, 
  RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, R.RegionName, R.Sport, 
  R.EntityType, R.Season, R.DefaultCrew, R.ShowAddress, R.ShowRank, R.ShowSubRank, 
  R.ShowRankToNonMembers, R.ShowSubRankToNonMembers, R.DefaultContactListSort, 
  R.ShowRankInSchedule, R.ShowSubRankInSchedule, R.MaxSubRankRequest, R.SubRankIsNumber, 
  R.ShowTeamsInSchedule, R.SortSubRankDesc, R.SeniorRankNameEnglish, R.SeniorRankNameFrench, 
  R.JuniorRankNameEnglish, R.JuniorRankNameFrench, R.IntermediateRankNameEnglish, 
  R.IntermediateRankNameFrench, R.NoviceRankNameEnglish, R.NoviceRankNameFrench, 
  R.RookieRankNameEnglish, R.RookieRankNameFrench, R.AllowBookOffs, R.BookOffHoursBefore, 
  R.MaxDateShowAvailableSpots, R.EmailAvailableSpots, R.EmailRequestedGames, 
  R.DefaultArriveBeforeMins, R.DefaultMaxGameLengthMins, R.OnlyAllowedToConfirmDaysBefore, 
  R.EmailConfirmDaysBefore, R.HasOfficials, R.HasScorekeepers, R.HasSupervisors, R.IsDemo, 
  R.Country, R.State, R.City, R.Address, R.PostalCode, R.ShowLinksToNonMembers, 
  R.ShowParksToNonMembers, R.ShowLeaguesToNonMembers, R.ShowOfficialRegionsToNonMembers, 
  R.ShowTeamsToNonMembers, R.ShowHolidayListToNonMembers, R.ShowContactListToNonMembers, 
  R.ShowAvailabilityDueDateToNonMembers, R.ShowAvailabilityToNonMembers, 
  R.ShowAvailableSpotsToNonMembers, R.ShowFullScheduleToNonMembers, R.ShowStandingsToNonMembers, 
  R.ShowStatsToNonMembers, R.ShowMainPageToNonMembers, R.HasEnteredMainPage, 
  R.ExtraScoreParameters, R.HomeTeamCanEnterScore, R.AwayTeamCanEnterScore, 
  R.ScorekeeperCanEnterScore, R.TeamCanEnterStats, R.ScorekeeperCanEnterStats, 
  R.RegionIsLadderLeague, R.NumberOfPlayersInLadderLeague, R.HomeTeamCanCancelGameHoursBefore, 
  R.AwayTeamCanCancelGameHoursBefore, R.MinimumValue, R.MinimumPercentage, R.TimeZone, 
  R.AutoSyncCancellationHoursBefore, R.AutoSyncSchedule, R.UniquePlayers, R.StatIndex, 
  R.StandingIndex, R.DefaultArriveBeforeAwayMins, R.DefaultArriveBeforePracticeMins, 
  R.DefaultMaxGameLengthPracticeMins, R.IncludeAfternoonAvailabilityOnWeekdays, 
  R.IncludeMorningAvailabilityOnWeekdays, R.EnableNotFilledInInAvailability, 
  R.EnableDualAvailability, R.IsAvailableText, R.IsAvailableDualText, 
  R.IsAvailableCombinedText, R.ShowOnlyDueDatesRangeForAvailability, 
  R.ShowRankInGlobalAvailability, R.ShowSubRankInGlobalAvailability, 
  R.SortGlobalAvailabilityByRank, R.SortGlobalAvailabilityBySubRank, 
  R.NotifyPartnerOfCancellation, R.ShowPhotosToNonMembers, R.ShowArticlesToNonMembers, 
  R.ShowWallToNonMembers, R.LeagueRankMaxes, R.LinkedRegionIds, R.StatLinks, R.PhotoId, 
  R.HasPhotoId, RU.IsArchived
FROM RegionUser AS RU, [User] AS U, Region AS R
WHERE RU.RealUsername = @RealUsername AND RU.RegionId = R.RegionID AND U.Username = RU.RealUsername
```

**JavaScript Logic**:

- Convert all RegionId, Username, RealUsername, Email, PreferredLanguage to lowercase
- Parse JSON: `PhoneNumbers`, `AlternateEmails`, `Positions`, `Rank`, `RankNumber`, `RankAndDates`, all Region JSON fields
- Structure as `{Region: {...}, RegionUser: {...}}`
- If RegionUser has no PhotoId but User has one, use User's PhotoId

### 5. Get Friend Notifications

**Purpose**: Top 5 pending friend/region join requests (3-way UNION)

**Parameters**: `@Username` (string)

```sql
SELECT TOP 5
  Username, FullUsername, FriendId, FriendUsername, RegionName, Sport, 
  CAST(_FriendNotification.RegionIsLadderLeague AS BIT) AS RegionIsLadderLeague, 
  EntityType, Season, DateCreated, IsViewed, Positions, Denied, Country, State, 
  City, Address, PostalCode
FROM (
  -- User joining region
  SELECT
    FR.Username, (U.FirstName + ' ' + U.Lastname) AS FullUsername, FR.FriendId, 
    FR.FriendUsername, R.RegionName, R.Sport, R.RegionIsLadderLeague, R.EntityType, 
    R.Season, FR.DateCreated, FR.IsViewed, FR.Positions, FR.Denied, R.Country, 
    R.State, R.City, R.Address, R.PostalCode
  FROM FriendNotification AS FR, Region As R, [User] AS U
  WHERE FR.Username = @Username AND R.RegionId = FR.FriendId 
    AND FR.Denied = 0 AND U.Username = FR.Username
  UNION
  -- User adding another user as friend
  SELECT
    FR.Username, (U2.FirstName + ' ' + U2.Lastname) AS FullUsername, FR.FriendId, 
    FR.FriendUsername, (U1.FirstName + ' ' + U1.LastName) AS RegionName, '' AS Sport, 
    CAST(0 AS BIT) AS RegionIsLadderLeague, 'user' AS EntityType, 0 AS Season, 
    FR.DateCreated, FR.IsViewed, FR.Positions, FR.Denied, '' AS Country, '' AS State, 
    '' AS City, '' AS Address, '' AS PostalCode
  FROM FriendNotification AS FR, [User] As U1, [User] AS U2
  WHERE FR.Username = @Username AND U1.Username = FR.FriendId 
    AND FR.Denied = 0 AND U2.Username = FR.Username
  UNION
  -- Region requesting another region (for executives)
  SELECT
    FR.Username, R2.RegionName, FR.FriendId, FR.FriendUsername, R.RegionName, 
    R.Sport, R.RegionIsLadderLeague, R.EntityType, R.Season, FR.DateCreated, 
    FR.IsViewed, FR.Positions, FR.Denied, R.Country, R.State, R.City, R.Address, 
    R.PostalCode
  FROM FriendNotification AS FR, Region As R, Region As R2
  WHERE FR.Username IN (
      SELECT RU.RegionId FROM RegionUser AS RU 
      WHERE RU.RealUsername = @Username AND RU.IsExecutive = 1 AND RU.IsArchived = 0
    ) 
    AND R.RegionId = FR.FriendId AND FR.Denied = 0 AND FR.Username = R2.RegionId
) AS _FriendNotification
ORDER BY DateCreated DESC
```

**JavaScript Parsing**: `Positions` field is JSON array of strings

### 6. Get Availability Due Date Users

**Purpose**: Availability forms user needs to fill in (2-way UNION for linked/non-linked info)

**Parameters**: `@Username` (string)

```sql
SELECT
  RegionId, Username, RealUsername, FirstName, LastName, Email, AlternateEmails, 
  PreferredLanguage, IsActive, StartDate, EndDate, Name, DueDate, RemindDaysInAdvance, 
  CanFillInDaysInAdvance, IsFilledIn
FROM (
  -- Non-linked info
  SELECT
    RU.RegionId, RU.Username, RU.RealUsername, RU.FirstName, RU.LastName, RU.Email, 
    RU.AlternateEmails, RU.PreferredLanguage, RU.IsActive, ADDU.StartDate, ADDU.EndDate, 
    AvDD.Name, AvDD.DueDate, AvDD.RemindDaysInAdvance, AvDD.CanFillInDaysInAdvance, 
    ADDU.IsFilledIn
  FROM AvailabilityDueDateUser AS ADDU, RegionUser AS RU, AvailabilityDueDate AS AvDD
  WHERE RU.RealUsername = @Username AND RU.IsArchived = 0 
    AND ADDU.RegionId = RU.RegionId AND ADDU.Username = RU.Username 
    AND RU.IsInfoLinked = 0 AND ADDU.RegionId = AvDD.RegionID 
    AND ADDU.StartDate = AvDD.StartDate AND ADDU.EndDate = AvDD.EndDate
  UNION
  -- Linked info (use email from User table)
  SELECT
    RU.RegionId, RU.Username, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, 
    U.AlternateEmails, U.PreferredLanguage, RU.IsActive, ADDU.StartDate, ADDU.EndDate, 
    AvDD.Name, AvDD.DueDate, AvDD.RemindDaysInAdvance, AvDD.CanFillInDaysInAdvance, 
    ADDU.IsFilledIn
  FROM RegionUser As RU, [User] AS U, AvailabilityDueDateUser AS ADDU, 
    AvailabilityDueDate AS AvDD
  WHERE RU.RealUsername = @Username AND RU.IsArchived = 0 
    AND ADDU.RegionId = RU.RegionId AND ADDU.Username = RU.Username 
    AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1 
    AND ADDU.RegionId = AvDD.RegionID AND ADDU.StartDate = AvDD.StartDate 
    AND ADDU.EndDate = AvDD.EndDate
) As _RegionUser 
ORDER BY RegionId, Username
```

**JavaScript Parsing**: `AlternateEmails` is JSON array

### 7. Get HasSubmittedInfo Flag

**Purpose**: Check if user has submitted their profile information

**Parameters**: `@Username` (string)

```sql
SELECT HasSubmittedInfo 
FROM UserSubmittedInfo 
WHERE Username = @Username
```

**JavaScript Logic**: Default to `false` if no record found

### 8. Get Game Notifications (Complex)

**Purpose**: Fetch upcoming games requiring user attention

**Sub-component**: This is the most complex query - calls `GetUsersGameNotifications` which internally calls `GetUsersScheduleFromRegionDateRange`

**Parameters**:

- `@Username` (string)
- `@StartDate` = UTC Now - 1 day
- `@EndDate` = Max DateTime (9999-12-31)

**Step 8.1**: Get all user's region IDs

```sql
SELECT RegionId 
FROM RegionUser 
WHERE RealUsername = @Username AND IsArchived = 0
```

**Step 8.2**: This triggers a massive schedule fetching operation - see detailed breakdown in "Game Notifications Logic" section below

**Result Structure**: Returns object with:

- `Schedule`: Array of filtered ScheduleResult items
- `RegionUsers`: List of RegionUser objects
- `RegionParks`: List of Park objects
- `Teams`: List of Team objects
- `RegionLeagues`: List of RegionLeaguePayContracted objects

### 9. Get Additional Region Properties

**Purpose**: Fetch region details for linked leagues not already in user's regions

**Parameters**: Dynamic list of RegionIds gathered from step 3 + game notifications

**JavaScript Logic**:

1. Collect all unique region IDs from step 3 (Regions)
2. For each RegionLeague from game notifications:

   - If `RealLeagueId !== ""` AND `IsLinked === true`
   - If this RealLeagueId is NOT already in user's Regions
   - Add to `NotYetFetchedRegionIds` list

3. If `NotYetFetchedRegionIds.length > 0`, fetch using dynamic parameter query:
```sql
SELECT [all Region columns - see Region.SelectStr]
FROM Region
WHERE RegionId IN (@RegionId1, @RegionId2, ..., @RegionIdN)
ORDER BY RegionId
```


**JavaScript Logic**: Hide payment data - set `HasPaid = false` and `PaymentData = ""`

## Game Notifications Logic (Detailed)

This is the most complex component - filters schedule items to show only those requiring user notification.

### Filtering Flow

**Input**: All schedule items from yesterday onwards for all user regions

**Processing Steps**:

1. **Get Base Schedule Data** via `GetUsersScheduleFromRegionDateRange`:

   - RegionId: `"allregions"`
   - OfficialId: empty
   - StartDate: UTC Now - 1 day
   - EndDate: Max date
   - LoggedInUsername: Same as Username

2. **For Each Schedule Item**: Apply time zone filter
   ```javascript
   const timeZone = getTimeZone(regionProperty.TimeZone); // Use Olson to TZ conversion
   const regionNow = new Date(Date.UTC() + timeZone.offsetHours);
   if (lastScheduleItem.GameDate < regionNow) continue; // Skip past games
   ```

3. **Apply Entity-Type-Specific Filtering**:

#### For Team Entity Type:

```javascript
const versionId = getConfirmVersionId(scheduleTeamConfirm, officialId);
const isDeleted = lastScheduleItem.getIsDeletedTeamItem(regionUser);
const oldIsDeleted = versionId >= 0 
  ? schedule[versionId].getIsDeletedTeamItem(regionUser) 
  : true;

if (isDeleted && oldIsDeleted) {
  // Check if item was added after confirmation
  for (let i = versionId; i < schedule.length; i++) {
    if (!schedule[i].getIsDeletedTeamItem(regionUser)) {
      includeNotification = true;
      break;
    }
  }
} else {
  if (!isDeleted && !oldIsDeleted) {
    // Check for changes requiring notification
    for (let i = versionId; i < schedule.length; i++) {
      if (isDeleted !== schedule[i].getIsDeletedTeamItem(regionUser)) {
        includeNotification = true;
        break;
      }
      if (requiredGameNotification(lastScheduleItem, schedule[i])) {
        includeNotification = true;
        break;
      }
    }
  } else {
    // Deletion status changed
    includeNotification = true;
  }
}
```

#### For Referee Entity Type:

```javascript
const versionId = getConfirmVersionId(scheduleConfirm, officialId);
const isDeleted = lastScheduleItem.getIsDeletedRefereeItem(regionUser);
const oldIsDeleted = versionId >= 0 
  ? schedule[versionId].getIsDeletedRefereeItem(regionUser) 
  : true;

if (isDeleted && oldIsDeleted) {
  // Check if item was added after confirmation
  for (let i = versionId; i < schedule.length; i++) {
    if (!schedule[i].getIsDeletedRefereeItem(regionUser)) {
      includeNotification = true;
      break;
    }
  }
} else {
  if (!isDeleted && !oldIsDeleted) {
    // Check for changes requiring notification
    for (let i = versionId; i < schedule.length; i++) {
      if (isDeleted !== schedule[i].getIsDeletedRefereeItem(regionUser)) {
        includeNotification = true;
        break;
      }
      // Check game details, position, or fine changes
      if (requiredGameNotification(lastScheduleItem, schedule[i]) ||
          requiredGameNotificationPosition(lastScheduleItem, schedule[i], officialId) !== "SamePosition" ||
          requiredGameNotificationFine(lastScheduleItem, schedule[i], officialId)) {
        includeNotification = true;
        break;
      }
    }
  } else {
    // Deletion status changed
    includeNotification = true;
  }
}
```

### Helper Functions for Game Notification Filtering

#### requiredGameNotification(newSchedule, oldSchedule)

Returns `true` if any of these changed:

- GameNumber
- GameType
- LeagueId
- HomeTeam
- AwayTeam
- GameDate
- ParkId
- GameStatus
- CrewType (dictionary comparison)

#### requiredGameNotificationPosition(newSchedule, oldSchedule, officialId)

Returns:

- `"Removed"`: Official was in old position but not in new
- `"Added"`: Official was not in old but is in new
- `"Changed"`: Position changed
- `"SamePosition"`: No change

#### requiredGameNotificationFine(newSchedule, oldSchedule, officialId)

Returns `true` if fine added/removed/changed for this official

### Version ID Calculation

```javascript
function getConfirmVersionId(confirmRecord, officialId) {
  if (!confirmRecord || confirmRecord.Confirmed === 0) return 0;
  if (confirmRecord.Confirmed === 1 || confirmRecord.Confirmed === 2) {
    return Math.min(scheduleLength - 1, confirmRecord.VersionId - 1);
  }
  return 0;
}
```

## Implementation Order

1. Validate session token (prerequisite)
2. Execute queries 1-7 in parallel (independent)
3. Execute query 8 (game notifications) - depends on nothing but is time-intensive
4. Execute query 9 (additional region properties) - depends on results from queries 3 and 8
5. Assemble final response object

## Critical JavaScript Considerations

### JSON Field Parsing

Many fields store JSON as strings - parse them in JavaScript:

- User: `PhoneNumbers`, `AlternateEmails`
- Region: `DefaultCrew`, `ExtraScoreParameters`, `MinimumValue`, `MinimumPercentage`, all `*Text` fields, `LeagueRankMaxes`, `LinkedRegionIds`
- RegionUser: `Positions`, `Rank`, `RankNumber`, `RankAndDates`, `PhoneNumbers`, `AlternateEmails`

### Case Normalization

Apply `.toLowerCase()` to:

- All RegionId values
- All Username/RealUsername values
- All Email values
- All Sport/EntityType values
- PreferredLanguage

### Date Handling

- All dates from SQL Server are UTC
- Apply time zone conversions using region's TimeZone field (Olson format)
- For game notifications, filter uses: `regionTime = UTC + timeZone.offset`

### Error Handling

Return appropriate error objects:

- `{Success: false, ErrorCode: "UserDoesNotExist"}` if user not found
- `{Success: false, ErrorCode: "InvalidSessionToken"}` if auth fails

## Files Referenced

- `/UmpireAssignor/Controllers/BasicInformationController.vb` (line 8-11)
- `/UmpireAssignor/Models/User.vb` (lines 1162-1282, 1409-1532, 1611-1627)
- `/UmpireAssignor/Models/RegionUser.vb` (lines 1196-1336, 1876-1891)
- `/UmpireAssignor/Models/FriendNotification.vb` (lines 47-106)
- `/UmpireAssignor/Models/AvailabilityDueDate.vb` (lines 146-205)
- `/UmpireAssignor/Models/Schedule/Schedule.vb` (lines 2468-2573, 3670-3820, 5981-6110)
- `/UmpireAssignor/Models/RegionProperties.vb` (lines 961-993)