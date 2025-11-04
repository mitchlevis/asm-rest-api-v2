import { formatSuccessResponse, formatErrorResponse } from '../utils/helpers.js';

/**
 * Controllers
 */
import getWallPostsForUserController from '../controllers/actions/getWallPostsForUser';
import loginController from '../controllers/actions/login';

export function setupActionsRoutes(router) {
	/*
		GET Endpoints
	*/

	// GET /actions/wall-posts-for-user - Get wall posts for user (regionId optional)
	router.get('/actions/wall-posts-for-user/:regionId', getWallPostsForUserController);
	router.get('/actions/wall-posts-for-user', getWallPostsForUserController);

	/*
		POST Endpoints
	*/

	// POST /actions/login - Login user
	router.post('/actions/login', loginController);
};


