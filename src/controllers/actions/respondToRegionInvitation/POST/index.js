import Sequelize from '@sequelize/core';
const Op = Sequelize.Op;
import { authenticateSessionToken, validateIncomingParameters, getDbObject, getSequelizeObject, formatSuccessResponse, formatErrorResponse, throwError } from '../../../../utils/helpers';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		// Authenticate Session Token
		const tokenData = await authenticateSessionToken(request);
    const userId = tokenData.UserName;

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { regionId, accept } = body;

		// Get the User
    const userModel = await getDbObject('User', true, request);
    const user = await userModel.findOne({ where: { Username: userId }});
    if(!user){
      await throwError(404, `User ${userId} not found`);
    }

    // Get Region User by RegionId and Email
    const regionUserModel = await getDbObject('RegionUser', true, request);
    const regionUser = await regionUserModel.findOne({ where: { RegionId: regionId, Email: user.Email }});
    if(!regionUser){
      await throwError(404, `User ${userId} does not belong to region ${regionId}`);
    }

    // At this point, we know the user has a region user, so we update the IsLinked and RealUsername
    if(accept){
      await regionUserModel.update({ IsLinked: true, RealUsername: userId }, { where: { RegionId: regionId, Email: user.Email }});
    }

    // Let's check if there is a Notification from this Region - If so, we update the IsViewed and Denied
    const friendNotificationModel = await getDbObject('FriendNotification', true, request);
    const friendNotification = await friendNotificationModel.findOne({ where: { FriendId: regionId, FriendUsername: regionUser.Username }});
    if(friendNotification){
      await friendNotificationModel.update({ IsViewed: true, Denied: !accept }, { where: { FriendId: regionId, FriendUsername: regionUser.Username }});
    }

		const response = {
      success: true,
      message: `User ${userId} ${accept ? 'accepted' : 'denied'} region invitation for ${regionId}`
    };

		return formatSuccessResponse(request, {
			data: response,
		});
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
}
