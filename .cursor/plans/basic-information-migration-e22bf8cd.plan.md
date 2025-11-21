<!-- e22bf8cd-f3d4-4eac-a641-76692e39aa63 7ab5e1d1-8151-4b07-a0dc-dd197071d02b -->
# Migrate GET /actions/get-user-basic-information

## Implementation Strategy

Create a new endpoint that aggregates 9 data components matching the legacy API response structure. Implement simplified game notification filtering for initial release.

## Reference Documents

**IMPORTANT**: This plan should be used as the primary implementation guide. Additional reference materials:

1. **Detailed SQL Queries & Legacy Logic**: `basicinformation-migration.plan.md` (lines 1-541) - Contains complete SQL queries, execution order, parameters, and detailed filtering logic from the legacy API
2. **Legacy VB.NET Code**: `temp/basicinformation-migration-legacy-code/` folder contains the original source code:

   - `User.vb` - GetBasicInformation method (lines 1162-1282), LastClicked usage, UserSubmittedInfo
   - `RegionUser.vb` - LoadRegionAndRegionUser methods
   - `FriendNotification.vb` - Top 5 notification query logic
   - `Schedule.vb` - Game notifications complex filtering (reference only, simplified version here)
   - `AdvancedAvailability.vb` - Availability structures (for context)

Use these files to understand table structures, field types, and business logic when implementing the models and queries.

## Step 1: Create Missing Database Models

The endpoint requires models that don't exist yet:

### Create `src/db/models/UsernameLastClicked.js`

- Primary key: Username
- Fields: LastClickedRegion, LastClickedUsername

### Create `src/db/models/AvailabilityDueDate.js`

- Composite primary key: RegionId, StartDate, EndDate
- Fields: Name, DueDate, RemindDaysInAdvance, CanFillInDaysInAdvance

### Create `src/db/models/AvailabilityDueDateUser.js`

- Composite primary key: RegionId, Username, StartDate, EndDate
- Fields: IsFilledIn
- Foreign keys to RegionUser and AvailabilityDueDate

### Create `src/db/models/UserSubmittedInfo.js`

- Primary key: Username
- Fields: HasSubmittedInfo (boolean)

### Update `src/db/models/index.js`

Add the 4 new models to the exports and associations

## Step 2: Create Endpoint Structure

### Create folder structure:

- `src/controllers/actions/getUserBasicInformation/GET/`

### Create files:

- `index.js` - Main controller logic
- `request.js` - Zod validation schema (no path/query params, body empty)

## Step 3: Implement Data Queries

In `src/controllers/actions/getUserBasicInformation/GET/index.js`:

### Query 1: Get User Info

```javascript
const userModel = await getDbObject('User', true, request);
const user = await userModel.findOne({
  where: { Username: userId },
  raw: true
});
// Parse JSON: PhoneNumbers, AlternateEmails
// Return error if not found
```

### Query 2: Get Last Clicked

```javascript
const usernameLastClickedModel = await getDbObject('UsernameLastClicked', true, request);
const lastClicked = await usernameLastClickedModel.findOne({
  where: { Username: userId },
  raw: true
});
// Default to { RegionId: "Select", Username: "Select" } if null
```

### Query 3: Get Regions with Positions

```javascript
const regionModel = await getDbObject('Region', true, request);
const regionUserModel = await getDbObject('RegionUser', true, request);
// Join Region and RegionUser where RealUsername = userId
// Parse all JSON fields from Region model
// Order by RegionName
```

### Query 4: Get Region with Users

```javascript
// Complex join: RegionUser, User, Region
// Get full profiles for all regions user belongs to
// Parse JSON fields, lowercase emails/usernames
// If RegionUser has no PhotoId, use User's PhotoId
```

### Query 5: Get Friend Notifications (Top 5)

```javascript
// Implement 3-way UNION query:
// 1. User joining region (FriendNotification + Region + User)
// 2. User adding another user (FriendNotification + User + User)
// 3. Region requesting region for executives
// Order by DateCreated DESC, LIMIT 5
// Parse Positions JSON
```

### Query 6: Get Availability Due Date Users

```javascript
// Implement 2-way UNION:
// 1. Non-linked info (RegionUser email)
// 2. Linked info (User table email)
// Join with AvailabilityDueDate and AvailabilityDueDateUser
// Parse AlternateEmails JSON
```

### Query 7: Get HasSubmittedInfo Flag

```javascript
const userSubmittedInfoModel = await getDbObject('UserSubmittedInfo', true, request);
const submittedInfo = await userSubmittedInfoModel.findOne({
  where: { Username: userId },
  raw: true
});
// Default to false if not found
```

### Query 8: Get Game Notifications (Simplified)

**Simplified approach:**

- Get all user's region IDs
- Query schedules from yesterday onwards where user has positions
- Apply basic timezone filtering (filter out past games)
- Return schedules with positions, parks, teams, leagues data
- Skip complex version comparison logic for initial release
```javascript
// Get regions for user
const regionIds = await regionUserModel.findAll({
  where: { RealUsername: userId, IsArchived: false },
  attributes: ['RegionId'],
  raw: true
});

// Query schedules from yesterday onwards
const startDate = new Date();
startDate.setDate(startDate.getDate() - 1);

// Use simplified query joining Schedule, SchedulePosition, Park, Team, RegionLeague
// Filter where user has assignments (current or historical with confirmations)
// Group results by schedule
```


### Query 9: Get Additional Region Properties

```javascript
// Collect all unique RegionIds from Query 3
// For each RegionLeague from Query 8:
//   If RealLeagueId !== "" AND IsLinked === true
//   If this RealLeagueId NOT in user's regions
//   Add to NotYetFetchedRegionIds
// Query Region model with IN clause for missing region IDs
// Hide payment data (HasPaid = false, PaymentData = "")
```

## Step 4: Apply JavaScript Transformations

### Case Normalization

Apply `.toLowerCase()` to:

- All RegionId, Username, RealUsername, Email values
- Sport, EntityType, PreferredLanguage values

### JSON Parsing

Parse JSON string fields using `safeJSONParse` or `JSON.parse`:

- User: PhoneNumbers, AlternateEmails
- Region: All JSON getter fields (already handled by model)
- RegionUser: Positions, Rank, RankNumber, RankAndDates

### Date Handling

- All SQL dates are UTC
- Convert to region timezone for filtering

## Step 5: Assemble Response

Structure response matching legacy format:

```javascript
return formatSuccessResponse(request, {
  data: {
    Success: true,
    User: {...},
    Regions: [...],
    RegionWithUsers: [...],
    RegionUsers: [...],      // From game notifications
    RegionParks: [...],       // From game notifications
    Teams: [...],             // From game notifications
    RegionLeagues: [...],     // From game notifications
    RegionProperties: [...],  // Additional regions from linked leagues
    FriendNotifications: [...],
    AvailabilityDueDateUsers: [...],
    GameNotifications: [...],
    LastClicked: {...},
    HasSubmittedInfo: boolean
  }
});
```

## Step 6: Register Route

In `src/routes/actions.js`:

- Import controller: `getUserBasicInformationController`
- Add route: `router.get('/actions/get-user-basic-information', getUserBasicInformationController);`

## Step 7: Error Handling

Implement appropriate error responses:

- `{Success: false, ErrorCode: "UserDoesNotExist"}` if user not found
- Standard error handling via `formatErrorResponse`

### To-dos

- [ ] Create 4 missing database models (UsernameLastClicked, AvailabilityDueDate, etc)
- [ ] Create endpoint folder structure and request validation
- [ ] Implement queries 1-7 (user info, regions, notifications, etc)
- [ ] Implement simplified game notifications query
- [ ] Implement additional region properties query
- [ ] Apply transformations and assemble final response structure
- [ ] Register route in actions.js
- [ ] Test endpoint with sample requests