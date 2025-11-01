import { formatSuccessResponse, formatErrorResponse } from '../utils/helpers.js';

/**
 * Controllers
 */
import getWallPostsForUserController from '../controllers/actions/getWallPostsForUser';

export function setupActionsRoutes(router) {
	// GET /actions/wall-posts-for-user - Get wall posts for user (regionId optional)
	router.get('/actions/wall-posts-for-user/:regionId', getWallPostsForUserController);
	router.get('/actions/wall-posts-for-user', getWallPostsForUserController);
};


