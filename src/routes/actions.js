import { formatSuccessResponse, formatErrorResponse } from '../utils/helpers.js';

/**
 * Controllers
 */

// GET
import getWallPostsForUserController from '../controllers/actions/getWallPostsForUser/GET';

// POST
import loginController from '../controllers/actions/login/POST';
import createWallPostController from '../controllers/actions/createWallPost/POST';
import createWallPostCommentController from '../controllers/actions/createWallPostComment/POST';
import createWallPostLikeController from '../controllers/actions/createWallPostLike/POST';
import respondToRegionInvitationController from '../controllers/actions/respondToRegionInvitation/POST';
import validateUserSessionTokenController from '../controllers/actions/validateUserSessionToken/POST';

// PUT
import linkPutController from '../controllers/actions/link/PUT';
import linkCategoryPutController from '../controllers/actions/linkCategory/PUT';

// DELETE
import linkDeleteController from '../controllers/actions/link/DELETE';
import linkCategoryDeleteController from '../controllers/actions/linkCategory/DELETE';
import regionUserDeleteController from '../controllers/actions/regionUser/DELETE';

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

	// POST /actions/create-wall-post - Create a wall post
	router.post('/actions/create-wall-post', createWallPostController);

	// POST /actions/create-wall-post-comment - Create a wall post comment
	router.post('/actions/create-wall-post-comment', createWallPostCommentController);

	// POST /actions/create-wall-post-like - Create a wall post like
	router.post('/actions/create-wall-post-like', createWallPostLikeController);

	// POST /actions/respond-to-region-invitation - Respond to a region invitation
	router.post('/actions/respond-to-region-invitation', respondToRegionInvitationController);

	// POST /actions/validate-user-session-token - Validate a user session token
	router.post('/actions/validate-user-session-token', validateUserSessionTokenController);

	/*
		PUT Endpoints
	*/

	// PUT /actions/link/:regionId - Create or update a link
	router.put('/actions/link/:regionId', linkPutController);

	// PUT /actions/link-category/:regionId - Create or update a link category
	router.put('/actions/link-category/:regionId', linkCategoryPutController);

	/*
		DELETE Endpoints
	*/

	// DELETE /actions/link/:regionId/:linkId - Delete a link
	router.delete('/actions/link/:regionId/:linkId', linkDeleteController);

	// DELETE /actions/link-category/:regionId/:categoryId - Delete a link category
	router.delete('/actions/link-category/:regionId/:categoryId', linkCategoryDeleteController);

	// DELETE /actions/delete-region-user/:regionId/:username - Delete a region user
	router.delete('/actions/region-user/:regionId/:username', regionUserDeleteController);

};


