import { Link, LinkAssociations } from './Link';
import { LinkCategory, LinkCategoryAssociations } from './LinkCategory';
import { FriendNotification, FriendNotificationAssociations } from './FriendNotification';
import { Region, RegionAssociations } from './Region';
import { RegionUser, RegionUserAssociations } from './RegionUser';
import { User, UserAssociations } from './User';
import { SessionToken, SessionTokenAssociations } from './SessionToken';
import { WallPost, WallPostAssociations } from './WallPost';
import { WallPostComment, WallPostCommentAssociations } from './WallPostComment';
import { WallPostCommentComment, WallPostCommentCommentAssociations } from './WallPostCommentComment';
import { WallPostCommentCommentLike, WallPostCommentCommentLikeAssociations } from './WallPostCommentCommentLike';
import { WallPostCommentLike, WallPostCommentLikeAssociations } from './WallPostCommentLike';
import { WallPostLike, WallPostLikeAssociations } from './WallPostLike';

const MODELS = {
 Link,
 LinkCategory,
 FriendNotification,
 Region,
 RegionUser,
 User,
 SessionToken,
 WallPost,
 WallPostComment,
 WallPostCommentComment,
 WallPostCommentCommentLike,
 WallPostCommentLike,
 WallPostLike,
}

const MODEL_ASSOCIATIONS = {
 Link: LinkAssociations,
 LinkCategory: LinkCategoryAssociations,
 FriendNotification: FriendNotificationAssociations,
 Region: RegionAssociations,
 RegionUser: RegionUserAssociations,
 User: UserAssociations,
 SessionToken: SessionTokenAssociations,
 WallPost: WallPostAssociations,
 WallPostComment: WallPostCommentAssociations,
 WallPostCommentComment: WallPostCommentCommentAssociations,
 WallPostCommentCommentLike: WallPostCommentCommentLikeAssociations,
 WallPostCommentLike: WallPostCommentLikeAssociations,
 WallPostLike: WallPostLikeAssociations,
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
