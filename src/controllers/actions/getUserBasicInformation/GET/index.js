import Sequelize from '@sequelize/core';
const Op = Sequelize.Op;
const literal = Sequelize.literal;
import { authenticateSessionToken, validateIncomingParameters, getDbObject, getSequelizeObject, formatSuccessResponse, formatErrorResponse, throwError, convertPropertiesToCamelCase } from '../../../../utils/helpers';
import { safeJSONParse } from '../../../../db/models/abc';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		// Authenticate Session Token
		const tokenData = await authenticateSessionToken(request);
		const userId = tokenData.UserName;

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		// Get models
		const userModel = await getDbObject('User', true, request);
		const usernameLastClickedModel = await getDbObject('UsernameLastClicked', true, request);
		const regionModel = await getDbObject('Region', true, request);
		const regionUserModel = await getDbObject('RegionUser', true, request);
		const friendNotificationModel = await getDbObject('FriendNotification', true, request);
		const availabilityDueDateModel = await getDbObject('AvailabilityDueDate', true, request);
		const availabilityDueDateUserModel = await getDbObject('AvailabilityDueDateUser', true, request);
		const userSubmittedInfoModel = await getDbObject('UserSubmittedInfo', true, request);
		const scheduleModel = await getDbObject('Schedule', true, request);
		const parkModel = await getDbObject('Park', true, request);
		const teamModel = await getDbObject('Team', true, request);
		const regionLeagueModel = await getDbObject('RegionLeague', true, request);
		const sequelize = await getSequelizeObject(request);

		// Query 1: Get User Info
		const user = await userModel.findOne({
			where: { Username: userId },
			raw: true
		});

		if (!user) {
			return formatSuccessResponse(request, {
				data: {
					Success: false,
					ErrorCode: "UserDoesNotExist"
				}
			});
		}

		// Parse JSON fields
		let userData = { ...user };
		if (userData.PhoneNumbers) {
			try {
				userData.PhoneNumbers = safeJSONParse(userData.PhoneNumbers);
			} catch (e) {
				console.error(`Failed to parse PhoneNumbers for user ${userId}:`, e);
				userData.PhoneNumbers = null;
			}
		}
		if (userData.AlternateEmails) {
			try {
				userData.AlternateEmails = safeJSONParse(userData.AlternateEmails);
			} catch (e) {
				console.error(`Failed to parse AlternateEmails for user ${userId}:`, e);
				userData.AlternateEmails = [];
			}
		}

		// Query 2: Get Last Clicked
		const lastClickedRecord = await usernameLastClickedModel.findOne({
			where: { Username: userId },
			raw: true
		});

		const lastClicked = lastClickedRecord
			? { RegionId: lastClickedRecord.LastClickedRegion, Username: lastClickedRecord.LastClickedUsername }
			: { RegionId: "Select", Username: "Select" };

		// Query 3: Get Regions with Positions
		const regionsWithPositions = await sequelize.query(`
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
			WHERE RU.RealUsername = :username AND R.RegionID = RU.RegionId
			ORDER BY R.RegionName
		`, {
			replacements: { username: userId },
			type: sequelize.QueryTypes.SELECT
		});

		// Parse JSON fields for regions
		const regions = regionsWithPositions.map(region => {
			const parsed = { ...region };
			// Parse JSON fields
			if (parsed.DefaultCrew) parsed.DefaultCrew = safeJSONParse(parsed.DefaultCrew);
			if (parsed.ExtraScoreParameters) parsed.ExtraScoreParameters = safeJSONParse(parsed.ExtraScoreParameters);
			if (parsed.MinimumValue) parsed.MinimumValue = safeJSONParse(parsed.MinimumValue);
			if (parsed.MinimumPercentage) parsed.MinimumPercentage = safeJSONParse(parsed.MinimumPercentage);
			if (parsed.IsAvailableText) parsed.IsAvailableText = safeJSONParse(parsed.IsAvailableText);
			if (parsed.IsAvailableDualText) parsed.IsAvailableDualText = safeJSONParse(parsed.IsAvailableDualText);
			if (parsed.IsAvailableCombinedText) parsed.IsAvailableCombinedText = safeJSONParse(parsed.IsAvailableCombinedText);
			if (parsed.LeagueRankMaxes) parsed.LeagueRankMaxes = safeJSONParse(parsed.LeagueRankMaxes);
			if (parsed.LinkedRegionIds) parsed.LinkedRegionIds = safeJSONParse(parsed.LinkedRegionIds);
			if (parsed.Positions) parsed.Positions = safeJSONParse(parsed.Positions);
			// Lowercase
			parsed.RegionId = parsed.RegionId?.toLowerCase();
			parsed.Sport = parsed.Sport?.toLowerCase();
			parsed.EntityType = parsed.EntityType?.toLowerCase();
			return parsed;
		});

		// Query 4: Get Region with Users
		const regionsWithUsers = await sequelize.query(`
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
			WHERE RU.RealUsername = :realUsername AND RU.RegionId = R.RegionID AND U.Username = RU.RealUsername
		`, {
			replacements: { realUsername: userId },
			type: sequelize.QueryTypes.SELECT
		});

		// Process regions with users
		const regionWithUsers = regionsWithUsers.map(row => {
			const regionUser = {
				RegionId: row.RegionId?.toLowerCase(),
				Username: row.Username?.toLowerCase(),
				IsLinked: row.IsLinked,
				AllowInfoLink: row.AllowInfoLink,
				RealUsername: row.RealUsername?.toLowerCase(),
				FirstName: row.FirstName,
				LastName: row.LastName,
				Email: row.Email?.toLowerCase(),
				PhoneNumbers: safeJSONParse(row.PhoneNumbers),
				AlternateEmails: safeJSONParse(row.AlternateEmails),
				Country: row.Country,
				State: row.State,
				City: row.City,
				Address: row.Address,
				PostalCode: row.PostalCode,
				PreferredLanguage: row.PreferredLanguage?.toLowerCase(),
				Positions: safeJSONParse(row.Positions),
				Rank: safeJSONParse(row.Rank),
				RankNumber: safeJSONParse(row.RankNumber),
				IsActive: row.IsActive,
				CanViewAvailability: row.CanViewAvailability,
				CanViewMasterSchedule: row.CanViewMasterSchedule,
				CanViewSupervisors: row.CanViewSupervisors,
				PublicData: row.PublicData,
				PrivateData: row.PrivateData,
				GlobalAvailabilityData: row.GlobalAvailabilityData,
				RankAndDates: safeJSONParse(row.RankAndDates),
				InternalData: row.InternalData,
				StatLinks: row.StatLinks,
				PhotoId: row.PhotoId || (userData.PhotoId || null),
				HasPhotoId: row.HasPhotoId || (userData.HasPhotoId || false),
				IsArchived: row.IsArchived
			};

			const region = {
				RegionId: row.RegionId?.toLowerCase(),
				RegionName: row.RegionName,
				Sport: row.Sport?.toLowerCase(),
				EntityType: row.EntityType?.toLowerCase(),
				Season: row.Season,
				DefaultCrew: safeJSONParse(row.DefaultCrew),
				ShowAddress: row.ShowAddress,
				ShowRank: row.ShowRank,
				ShowSubRank: row.ShowSubRank,
				ShowRankToNonMembers: row.ShowRankToNonMembers,
				ShowSubRankToNonMembers: row.ShowSubRankToNonMembers,
				DefaultContactListSort: row.DefaultContactListSort,
				ShowRankInSchedule: row.ShowRankInSchedule,
				ShowSubRankInSchedule: row.ShowSubRankInSchedule,
				MaxSubRankRequest: row.MaxSubRankRequest,
				SubRankIsNumber: row.SubRankIsNumber,
				ShowTeamsInSchedule: row.ShowTeamsInSchedule,
				SortSubRankDesc: row.SortSubRankDesc,
				SeniorRankNameEnglish: row.SeniorRankNameEnglish,
				SeniorRankNameFrench: row.SeniorRankNameFrench,
				JuniorRankNameEnglish: row.JuniorRankNameEnglish,
				JuniorRankNameFrench: row.JuniorRankNameFrench,
				IntermediateRankNameEnglish: row.IntermediateRankNameEnglish,
				IntermediateRankNameFrench: row.IntermediateRankNameFrench,
				NoviceRankNameEnglish: row.NoviceRankNameEnglish,
				NoviceRankNameFrench: row.NoviceRankNameFrench,
				RookieRankNameEnglish: row.RookieRankNameEnglish,
				RookieRankNameFrench: row.RookieRankNameFrench,
				AllowBookOffs: row.AllowBookOffs,
				BookOffHoursBefore: row.BookOffHoursBefore,
				MaxDateShowAvailableSpots: row.MaxDateShowAvailableSpots,
				EmailAvailableSpots: row.EmailAvailableSpots,
				EmailRequestedGames: row.EmailRequestedGames,
				DefaultArriveBeforeMins: row.DefaultArriveBeforeMins,
				DefaultMaxGameLengthMins: row.DefaultMaxGameLengthMins,
				OnlyAllowedToConfirmDaysBefore: row.OnlyAllowedToConfirmDaysBefore,
				EmailConfirmDaysBefore: row.EmailConfirmDaysBefore,
				HasOfficials: row.HasOfficials,
				HasScorekeepers: row.HasScorekeepers,
				HasSupervisors: row.HasSupervisors,
				IsDemo: row.IsDemo,
				Country: row.Country,
				State: row.State,
				City: row.City,
				Address: row.Address,
				PostalCode: row.PostalCode,
				ShowLinksToNonMembers: row.ShowLinksToNonMembers,
				ShowParksToNonMembers: row.ShowParksToNonMembers,
				ShowLeaguesToNonMembers: row.ShowLeaguesToNonMembers,
				ShowOfficialRegionsToNonMembers: row.ShowOfficialRegionsToNonMembers,
				ShowTeamsToNonMembers: row.ShowTeamsToNonMembers,
				ShowHolidayListToNonMembers: row.ShowHolidayListToNonMembers,
				ShowContactListToNonMembers: row.ShowContactListToNonMembers,
				ShowAvailabilityDueDateToNonMembers: row.ShowAvailabilityDueDateToNonMembers,
				ShowAvailabilityToNonMembers: row.ShowAvailabilityToNonMembers,
				ShowAvailableSpotsToNonMembers: row.ShowAvailableSpotsToNonMembers,
				ShowFullScheduleToNonMembers: row.ShowFullScheduleToNonMembers,
				ShowStandingsToNonMembers: row.ShowStandingsToNonMembers,
				ShowStatsToNonMembers: row.ShowStatsToNonMembers,
				ShowMainPageToNonMembers: row.ShowMainPageToNonMembers,
				HasEnteredMainPage: row.HasEnteredMainPage,
				ExtraScoreParameters: safeJSONParse(row.ExtraScoreParameters),
				HomeTeamCanEnterScore: row.HomeTeamCanEnterScore,
				AwayTeamCanEnterScore: row.AwayTeamCanEnterScore,
				ScorekeeperCanEnterScore: row.ScorekeeperCanEnterScore,
				TeamCanEnterStats: row.TeamCanEnterStats,
				ScorekeeperCanEnterStats: row.ScorekeeperCanEnterStats,
				RegionIsLadderLeague: row.RegionIsLadderLeague,
				NumberOfPlayersInLadderLeague: row.NumberOfPlayersInLadderLeague,
				HomeTeamCanCancelGameHoursBefore: row.HomeTeamCanCancelGameHoursBefore,
				AwayTeamCanCancelGameHoursBefore: row.AwayTeamCanCancelGameHoursBefore,
				MinimumValue: safeJSONParse(row.MinimumValue),
				MinimumPercentage: safeJSONParse(row.MinimumPercentage),
				TimeZone: row.TimeZone,
				AutoSyncCancellationHoursBefore: row.AutoSyncCancellationHoursBefore,
				AutoSyncSchedule: row.AutoSyncSchedule,
				UniquePlayers: row.UniquePlayers,
				StatIndex: row.StatIndex,
				StandingIndex: row.StandingIndex,
				DefaultArriveBeforeAwayMins: row.DefaultArriveBeforeAwayMins,
				DefaultArriveBeforePracticeMins: row.DefaultArriveBeforePracticeMins,
				DefaultMaxGameLengthPracticeMins: row.DefaultMaxGameLengthPracticeMins,
				IncludeAfternoonAvailabilityOnWeekdays: row.IncludeAfternoonAvailabilityOnWeekdays,
				IncludeMorningAvailabilityOnWeekdays: row.IncludeMorningAvailabilityOnWeekdays,
				EnableNotFilledInInAvailability: row.EnableNotFilledInInAvailability,
				EnableDualAvailability: row.EnableDualAvailability,
				IsAvailableText: safeJSONParse(row.IsAvailableText),
				IsAvailableDualText: safeJSONParse(row.IsAvailableDualText),
				IsAvailableCombinedText: safeJSONParse(row.IsAvailableCombinedText),
				ShowOnlyDueDatesRangeForAvailability: row.ShowOnlyDueDatesRangeForAvailability,
				ShowRankInGlobalAvailability: row.ShowRankInGlobalAvailability,
				ShowSubRankInGlobalAvailability: row.ShowSubRankInGlobalAvailability,
				SortGlobalAvailabilityByRank: row.SortGlobalAvailabilityByRank,
				SortGlobalAvailabilityBySubRank: row.SortGlobalAvailabilityBySubRank,
				NotifyPartnerOfCancellation: row.NotifyPartnerOfCancellation,
				ShowPhotosToNonMembers: row.ShowPhotosToNonMembers,
				ShowArticlesToNonMembers: row.ShowArticlesToNonMembers,
				ShowWallToNonMembers: row.ShowWallToNonMembers,
				LeagueRankMaxes: safeJSONParse(row.LeagueRankMaxes),
				LinkedRegionIds: safeJSONParse(row.LinkedRegionIds),
				StatLinks: row.StatLinks,
				PhotoId: row.PhotoId,
				HasPhotoId: row.HasPhotoId
			};

			return { Region: region, RegionUser: regionUser };
		});

		// Query 5: Get Friend Notifications (Top 5)
		const friendNotifications = await sequelize.query(`
			SELECT TOP 5
				Username, FullUsername, FriendId, FriendUsername, RegionName, Sport,
				CAST(RegionIsLadderLeague AS BIT) AS RegionIsLadderLeague,
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
				WHERE FR.Username = :username AND R.RegionID = FR.FriendId
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
				WHERE FR.Username = :username AND U1.Username = FR.FriendId
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
						WHERE RU.RealUsername = :username AND RU.IsExecutive = 1 AND RU.IsArchived = 0
					)
					AND R.RegionID = FR.FriendId AND FR.Denied = 0 AND FR.Username = R2.RegionID
			) AS _FriendNotification
			ORDER BY DateCreated DESC
		`, {
			replacements: { username: userId },
			type: sequelize.QueryTypes.SELECT
		});

		// Parse Positions JSON and map to legacy property names for friend notifications
		const friendNotificationsParsed = friendNotifications.map(notif => ({
			Username: notif.Username,
			FullUsername: notif.FullUsername,
			FriendId: notif.FriendId,
			FriendUsername: notif.FriendUsername,
			FriendName: notif.RegionName, // SQL column RegionName -> FriendName
			FriendSport: notif.Sport, // SQL column Sport -> FriendSport
			FriendLadderLeague: notif.RegionIsLadderLeague, // SQL column RegionIsLadderLeague -> FriendLadderLeague
			FriendEntityType: notif.EntityType, // SQL column EntityType -> FriendEntityType
			FriendSeason: notif.Season, // SQL column Season -> FriendSeason
			DateCreated: notif.DateCreated,
			IsViewed: notif.IsViewed,
			Positions: safeJSONParse(notif.Positions),
			Denied: notif.Denied,
			FriendCountry: notif.Country, // SQL column Country -> FriendCountry
			FriendState: notif.State, // SQL column State -> FriendState
			FriendCity: notif.City, // SQL column City -> FriendCity
			FriendAddress: notif.Address, // SQL column Address -> FriendAddress
			FriendPostalCode: notif.PostalCode // SQL column PostalCode -> FriendPostalCode
		}));

		// Query 6: Get Availability Due Date Users
		const availabilityDueDateUsers = await sequelize.query(`
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
				WHERE RU.RealUsername = :username AND RU.IsArchived = 0
					AND ADDU.RegionId = RU.RegionId AND ADDU.Username = RU.Username
					AND RU.IsInfoLinked = 0 AND ADDU.RegionId = AvDD.RegionId
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
				WHERE RU.RealUsername = :username AND RU.IsArchived = 0
					AND ADDU.RegionId = RU.RegionId AND ADDU.Username = RU.Username
					AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
					AND ADDU.RegionId = AvDD.RegionId AND ADDU.StartDate = AvDD.StartDate
					AND ADDU.EndDate = AvDD.EndDate
			) As _RegionUser
			ORDER BY RegionId, Username
		`, {
			replacements: { username: userId },
			type: sequelize.QueryTypes.SELECT
		});

		// Parse AlternateEmails JSON
		const availabilityDueDateUsersParsed = availabilityDueDateUsers.map(item => ({
			...item,
			AlternateEmails: safeJSONParse(item.AlternateEmails)
		}));

		// Query 7: Get HasSubmittedInfo Flag
		const submittedInfo = await userSubmittedInfoModel.findOne({
			where: { Username: userId },
			raw: true
		});
		const hasSubmittedInfo = submittedInfo ? submittedInfo.HasSubmittedInfo : false;

		// Query 8: Get Game Notifications (Simplified)
		// Get all user's region IDs
		const userRegions = await regionUserModel.findAll({
			where: { RealUsername: userId, IsArchived: false },
			attributes: ['RegionId'],
			raw: true
		});
		const regionIds = userRegions.map(r => r.RegionId);

		// Calculate start date (yesterday)
		const startDate = new Date();
		startDate.setUTCDate(startDate.getUTCDate() - 1);
		const startDateStr = startDate.toISOString().slice(0, 19).replace('T', ' ');

		// Get schedules where user has positions (current or historical with confirmations)
		let gameNotifications = [];
		let regionUsersFromGames = [];
		let regionParks = [];
		let teams = [];
		let regionLeagues = [];

		if (regionIds.length > 0) {
			// Build IN clause for MSSQL (doesn't support array parameters)
			const regionIdPlaceholders = regionIds.map((_, index) => `:regionId${index}`).join(', ');
			const regionIdReplacements = {};
			regionIds.forEach((id, index) => {
				regionIdReplacements[`regionId${index}`] = id;
			});

			// Get schedules from yesterday onwards where user has assignments
			const schedules = await sequelize.query(`
				SELECT DISTINCT
					s.RegionId, s.ScheduleId, s.GameDate, s.GameNumber, s.GameType, s.LeagueId,
					s.HomeTeam, s.AwayTeam, s.ParkId, s.GameStatus, s.CrewType, s.VersionId
				FROM Schedule s
				WHERE s.RegionId IN (${regionIdPlaceholders})
					AND s.GameDate >= :startDate
					AND (
						-- Current assignments
						EXISTS (
							SELECT 1 FROM SchedulePosition sp
							WHERE sp.RegionId = s.RegionId
								AND sp.ScheduleId = s.ScheduleId
								AND sp.OfficialId = (
									SELECT Username FROM RegionUser
									WHERE RegionId = s.RegionId AND RealUsername = :username
								)
						)
						OR
						-- Historical assignments with confirmations
						EXISTS (
							SELECT 1 FROM SchedulePositionVersion spv
							WHERE spv.RegionId = s.RegionId
								AND spv.ScheduleId = s.ScheduleId
								AND spv.OfficialId = (
									SELECT Username FROM RegionUser
									WHERE RegionId = s.RegionId AND RealUsername = :username
								)
							AND EXISTS (
								SELECT 1 FROM ScheduleConfirm sc
								WHERE sc.RegionId = spv.RegionId
									AND sc.ScheduleId = spv.ScheduleId
									AND sc.Username = spv.OfficialId
							)
						)
					)
				ORDER BY s.GameDate ASC
			`, {
				replacements: {
					...regionIdReplacements,
					startDate: startDateStr,
					username: userId
				},
				type: sequelize.QueryTypes.SELECT
			});

			// Get related data for these schedules
			if (schedules.length > 0) {
				const scheduleRegionIds = [...new Set(schedules.map(s => s.RegionId))];
				const scheduleIds = schedules.map(s => ({ RegionId: s.RegionId, ScheduleId: s.ScheduleId }));

				// Get parks
				const parks = await parkModel.findAll({
					where: { RegionId: { [Op.in]: scheduleRegionIds } },
					raw: true
				});
				regionParks = parks;

				// Get teams
				const teamsList = await teamModel.findAll({
					where: { RegionId: { [Op.in]: scheduleRegionIds } },
					raw: true
				});
				teams = teamsList;

				// Get region leagues
				const leagues = await regionLeagueModel.findAll({
					where: { RegionId: { [Op.in]: scheduleRegionIds } },
					raw: true
				});
				regionLeagues = leagues;

				// Get region users for these regions
				const regionUsersList = await regionUserModel.findAll({
					where: { RegionId: { [Op.in]: scheduleRegionIds } },
					raw: true
				});
				regionUsersFromGames = regionUsersList;

				// Format game notifications (simplified - just return the schedules)
				gameNotifications = schedules.map(schedule => ({
					RegionId: schedule.RegionId?.toLowerCase(),
					ScheduleId: schedule.ScheduleId,
					GameDate: schedule.GameDate,
					GameNumber: schedule.GameNumber,
					GameType: schedule.GameType,
					LeagueId: schedule.LeagueId,
					HomeTeam: schedule.HomeTeam,
					AwayTeam: schedule.AwayTeam,
					ParkId: schedule.ParkId,
					GameStatus: schedule.GameStatus,
					CrewType: schedule.CrewType,
					VersionId: schedule.VersionId
				}));
			}
		}

		// Query 9: Get Additional Region Properties
		// Collect all unique RegionIds from Query 3
		const userRegionIds = new Set(regions.map(r => r.RegionId?.toLowerCase()));

		// For each RegionLeague from game notifications, check for linked leagues
		const notYetFetchedRegionIds = new Set();
		for (const league of regionLeagues) {
			if (league.RealLeagueId && league.RealLeagueId !== "" && league.IsLinked) {
				const realLeagueIdLower = league.RealLeagueId.toLowerCase();
				if (!userRegionIds.has(realLeagueIdLower)) {
					notYetFetchedRegionIds.add(realLeagueIdLower);
				}
			}
		}

		// Fetch additional regions
		let regionProperties = [];
		if (notYetFetchedRegionIds.size > 0) {
			const additionalRegions = await regionModel.findAll({
				where: { RegionId: { [Op.in]: Array.from(notYetFetchedRegionIds) } },
				raw: true
			});

			regionProperties = additionalRegions.map(region => {
				const parsed = { ...region };
				// Hide payment data
				parsed.HasPaid = false;
				parsed.PaymentData = "";
				// Parse JSON fields
				if (parsed.DefaultCrew) parsed.DefaultCrew = safeJSONParse(parsed.DefaultCrew);
				if (parsed.ExtraScoreParameters) parsed.ExtraScoreParameters = safeJSONParse(parsed.ExtraScoreParameters);
				if (parsed.MinimumValue) parsed.MinimumValue = safeJSONParse(parsed.MinimumValue);
				if (parsed.MinimumPercentage) parsed.MinimumPercentage = safeJSONParse(parsed.MinimumPercentage);
				if (parsed.IsAvailableText) parsed.IsAvailableText = safeJSONParse(parsed.IsAvailableText);
				if (parsed.IsAvailableDualText) parsed.IsAvailableDualText = safeJSONParse(parsed.IsAvailableDualText);
				if (parsed.IsAvailableCombinedText) parsed.IsAvailableCombinedText = safeJSONParse(parsed.IsAvailableCombinedText);
				if (parsed.LeagueRankMaxes) parsed.LeagueRankMaxes = safeJSONParse(parsed.LeagueRankMaxes);
				if (parsed.LinkedRegionIds) parsed.LinkedRegionIds = safeJSONParse(parsed.LinkedRegionIds);
				// Lowercase
				parsed.RegionId = parsed.RegionId?.toLowerCase();
				parsed.Sport = parsed.Sport?.toLowerCase();
				parsed.EntityType = parsed.EntityType?.toLowerCase();
				return parsed;
			});
		}

		// Assemble final response
		const responseData = {
			Success: true,
			User: userData,
			Regions: regions,
			RegionWithUsers: regionWithUsers,
			RegionUsers: regionUsersFromGames,
			RegionParks: regionParks,
			Teams: teams,
			RegionLeagues: regionLeagues,
			RegionProperties: regionProperties,
			FriendNotifications: friendNotificationsParsed,
			AvailabilityDueDateUsers: availabilityDueDateUsersParsed,
			GameNotifications: gameNotifications,
			LastClicked: lastClicked,
			HasSubmittedInfo: hasSubmittedInfo
		};

		// Convert all properties to camelCase to match legacy API format
		const camelCaseResponse = convertPropertiesToCamelCase(responseData);

		return formatSuccessResponse(request, {
			data: camelCaseResponse
		});
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
};

