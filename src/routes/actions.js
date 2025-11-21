import { formatSuccessResponse, formatErrorResponse } from '../utils/helpers.js';

/**
 * Controllers
 */

// GET
import getWallPostsForUserController from '../controllers/actions/getWallPostsForUser/GET';
import getMasterScheduleRefereeEventsController from '../controllers/actions/getMasterScheduleRefereeEvents/GET';
import getMyScheduleRefereeEventsController from '../controllers/actions/getMyScheduleRefereeEvents/GET';
import getMasterScheduleFormDataController from '../controllers/actions/getMasterScheduleFormData/GET';
import getMasterScheduleAssignRegionUsersController from '../controllers/actions/getMasterScheduleAssignRegionUsers/GET';
import getMasterScheduleAssignRegionUsersAvailabilityController from '../controllers/actions/getMasterScheduleAssignRegionUsersAvailability/GET';
import getRegionUsersController from '../controllers/actions/getRegionUsers/GET';
import getUserManagementFormDataController from '../controllers/actions/getUserManagementFormData/GET';
import getUserBasicInformationController from '../controllers/actions/getUserBasicInformation/GET';

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

	// GET /actions/master-schedule-referee-events - Get master schedule referee events (regionId optional)
	router.get('/actions/master-schedule-referee-events/:regionId', getMasterScheduleRefereeEventsController);
	router.get('/actions/master-schedule-referee-events', getMasterScheduleRefereeEventsController);

	// GET /actions/my-schedule-referee-events - Get my schedule referee events (regionId optional)
	router.get('/actions/my-schedule-referee-events/:regionId', getMyScheduleRefereeEventsController);
	router.get('/actions/my-schedule-referee-events', getMyScheduleRefereeEventsController);

	// GET /actions/master-schedule-form-data - Get master schedule form data (regionId optional)
	router.get('/actions/master-schedule-form-data/:regionId', getMasterScheduleFormDataController);
	router.get('/actions/master-schedule-form-data', getMasterScheduleFormDataController);

	// GET /actions/master-schedule-assign-region-users - Get region users for assigning to schedule
	router.get('/actions/master-schedule-assign-region-users/:regionId', getMasterScheduleAssignRegionUsersController);

	// GET /actions/master-schedule-assign-region-users-availability - Get region users with availability & conflicts for a specific schedule
	router.get('/actions/master-schedule-assign-region-users-availability/:regionId/:scheduleId', getMasterScheduleAssignRegionUsersAvailabilityController);

	// GET /actions/region-users/:regionId - Get region users for a region (requires executive or canViewMasterSchedule permissions)
	router.get('/actions/region-users/:regionId', getRegionUsersController);

	// GET /actions/user-management-form-data/:regionId - Get form data for the user management page (requires executive or canViewMasterSchedule permissions)
	router.get('/actions/user-management-form-data/:regionId', getUserManagementFormDataController);

	// GET /actions/get-user-basic-information - Get basic information for authenticated user
	router.get('/actions/get-user-basic-information', getUserBasicInformationController);

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


