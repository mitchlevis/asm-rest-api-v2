import { Availability3, Availability3Associations } from './Availability3';
import { AvailabilityDueDate, AvailabilityDueDateAssociations } from './AvailabilityDueDate';
import { AvailabilityDueDateUser, AvailabilityDueDateUserAssociations } from './AvailabilityDueDateUser';
import { ChatMessage, ChatMessageAssociations } from './ChatMessage';
import { CrawlerSchedule, CrawlerScheduleAssociations } from './CrawlerSchedule';
import { CrawlerScheduleEvent, CrawlerScheduleEventAssociations } from './CrawlerScheduleEvent';
import { FriendNotification, FriendNotificationAssociations } from './FriendNotification';
import { Link, LinkAssociations } from './Link';
import { LinkCategory, LinkCategoryAssociations } from './LinkCategory';
import { Park, ParkAssociations } from './Park';
import { Region, RegionAssociations } from './Region';
import { RegionLeague, RegionLeagueAssociations } from './RegionLeague';
import { RegionLeaguePay, RegionLeaguePayAssociations } from './RegionLeaguePay';
import { RegionUser, RegionUserAssociations } from './RegionUser';
import { Schedule, ScheduleAssociations } from './Schedule';
import { ScheduleBookOff, ScheduleBookOffAssociations } from './ScheduleBookOff';
import { ScheduleConfirm, ScheduleConfirmAssociations } from './ScheduleConfirm';
import { ScheduleFine, ScheduleFineAssociations } from './ScheduleFine';
import { SchedulePosition, SchedulePositionAssociations } from './SchedulePosition';
import { SchedulePositionTemp, SchedulePositionTempAssociations } from './SchedulePositionTemp';
import { SchedulePositionVersion, SchedulePositionVersionAssociations } from './SchedulePositionVersion';
import { ScheduleRequest, ScheduleRequestAssociations } from './ScheduleRequest';
import { ScheduleTemp, ScheduleTempAssociations } from './ScheduleTemp';
import { ScheduleUserComment, ScheduleUserCommentAssociations } from './ScheduleUserComment';
import { ScheduleVersion, ScheduleVersionAssociations } from './ScheduleVersion';
import { SessionToken, SessionTokenAssociations } from './SessionToken';
import { Team, TeamAssociations } from './Team';
import { User, UserAssociations } from './User';
import { UserInvitation, UserInvitationAssociations } from './UserInvitation';
import { UserSubmittedInfo, UserSubmittedInfoAssociations } from './UserSubmittedInfo';
import { UsernameLastClicked, UsernameLastClickedAssociations } from './UsernameLastClicked';
import { WallPost, WallPostAssociations } from './WallPost';
import { WallPostComment, WallPostCommentAssociations } from './WallPostComment';
import { WallPostCommentComment, WallPostCommentCommentAssociations } from './WallPostCommentComment';
import { WallPostCommentCommentLike, WallPostCommentCommentLikeAssociations } from './WallPostCommentCommentLike';
import { WallPostCommentLike, WallPostCommentLikeAssociations } from './WallPostCommentLike';
import { WallPostLike, WallPostLikeAssociations } from './WallPostLike';
import { WallPostTags, WallPostTagsAssociations } from './WallPostTags';

const MODELS = {
 Availability3,
 AvailabilityDueDate,
 AvailabilityDueDateUser,
 ChatMessage,
 CrawlerSchedule,
 CrawlerScheduleEvent,
 FriendNotification,
 Link,
 LinkCategory,
 Park,
 Region,
 RegionLeague,
 RegionLeaguePay,
 RegionUser,
 Schedule,
 ScheduleBookOff,
 ScheduleConfirm,
 ScheduleFine,
 SchedulePosition,
 SchedulePositionTemp,
 SchedulePositionVersion,
 ScheduleRequest,
 ScheduleTemp,
 ScheduleUserComment,
 ScheduleVersion,
 SessionToken,
 Team,
 User,
 UserInvitation,
 UserSubmittedInfo,
 UsernameLastClicked,
 WallPost,
 WallPostComment,
 WallPostCommentComment,
 WallPostCommentCommentLike,
 WallPostCommentLike,
 WallPostLike,
 WallPostTags,
}

const MODEL_ASSOCIATIONS = {
 Availability3: Availability3Associations,
 AvailabilityDueDate: AvailabilityDueDateAssociations,
 AvailabilityDueDateUser: AvailabilityDueDateUserAssociations,
 ChatMessage: ChatMessageAssociations,
 CrawlerSchedule: CrawlerScheduleAssociations,
 CrawlerScheduleEvent: CrawlerScheduleEventAssociations,
 FriendNotification: FriendNotificationAssociations,
 Link: LinkAssociations,
 LinkCategory: LinkCategoryAssociations,
 Park: ParkAssociations,
 Region: RegionAssociations,
 RegionLeague: RegionLeagueAssociations,
 RegionLeaguePay: RegionLeaguePayAssociations,
 RegionUser: RegionUserAssociations,
 Schedule: ScheduleAssociations,
 ScheduleBookOff: ScheduleBookOffAssociations,
 ScheduleConfirm: ScheduleConfirmAssociations,
 ScheduleFine: ScheduleFineAssociations,
 SchedulePosition: SchedulePositionAssociations,
 SchedulePositionTemp: SchedulePositionTempAssociations,
 SchedulePositionVersion: SchedulePositionVersionAssociations,
 ScheduleRequest: ScheduleRequestAssociations,
 ScheduleTemp: ScheduleTempAssociations,
 ScheduleUserComment: ScheduleUserCommentAssociations,
 ScheduleVersion: ScheduleVersionAssociations,
 SessionToken: SessionTokenAssociations,
 Team: TeamAssociations,
 User: UserAssociations,
 UserInvitation: UserInvitationAssociations,
 UserSubmittedInfo: UserSubmittedInfoAssociations,
 UsernameLastClicked: UsernameLastClickedAssociations,
 WallPost: WallPostAssociations,
 WallPostComment: WallPostCommentAssociations,
 WallPostCommentComment: WallPostCommentCommentAssociations,
 WallPostCommentCommentLike: WallPostCommentCommentLikeAssociations,
 WallPostCommentLike: WallPostCommentLikeAssociations,
 WallPostLike: WallPostLikeAssociations,
 WallPostTags: WallPostTagsAssociations,
}

/**
 * Converts a PascalCase model name to plural kebab-case
 * Examples: WallPost -> wall-posts, LinkCategory -> link-categories, Availability3 -> availabilities3
 */
const modelNameToPluralKebabCase = (modelName) => {
	// Check if model name ends with a number (e.g., "Availability3")
	const numberMatch = modelName.match(/^(.+?)(\d+)$/);
	let baseName = modelName;
	let numberSuffix = '';

	if (numberMatch) {
		baseName = numberMatch[1];
		numberSuffix = numberMatch[2];
	}

	// Convert PascalCase to kebab-case
	const kebabCase = baseName
		.replace(/([A-Z])/g, '-$1') // Insert dash before capital letters
		.toLowerCase()
		.replace(/^-/, ''); // Remove leading dash

	// Handle pluralization
	// Simple rule: add 's' for most cases, 'es' for words ending in s/x/z/ch/sh
	// For words ending in 'y' preceded by a consonant, change 'y' to 'ies'
	let pluralized;
	if (kebabCase.endsWith('y') && !/[aeiou]y$/.test(kebabCase)) {
		pluralized = kebabCase.slice(0, -1) + 'ies';
	} else if (kebabCase.match(/([sxz]|ch|sh)$/)) {
		pluralized = kebabCase + 'es';
	} else {
		pluralized = kebabCase + 's';
	}

	// Append number suffix if it exists
	return pluralized + numberSuffix;
};

/**
 * Maps plural kebab-case endpoint names to singular PascalCase model names
 * Auto-generated from MODELS to keep in sync
 */
const PLURAL_TO_SINGULAR_MAP = Object.keys(MODELS).reduce((acc, modelName) => {
	const pluralKebabCase = modelNameToPluralKebabCase(modelName);
	acc[pluralKebabCase] = modelName;
	return acc;
}, {});

/**
 * Resolves a plural kebab-case resource name to its singular PascalCase model name
 * @param {string} pluralKebabCase - The plural kebab-case name (e.g., 'wall-posts')
 * @returns {string|null} - The singular PascalCase model name (e.g., 'WallPost') or null if not found
 */
const resolveModelName = (pluralKebabCase) => {
	return PLURAL_TO_SINGULAR_MAP[pluralKebabCase] || null;
};

export { MODELS, MODEL_ASSOCIATIONS, resolveModelName, modelNameToPluralKebabCase };
