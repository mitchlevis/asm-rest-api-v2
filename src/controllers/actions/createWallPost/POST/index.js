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

		const { Post, Link, PostType } = body;

		// There should at least be a Post or a Link
    if(!Post && !Link){
      await throwError(400, "Post or Link is required");
    }

		// Get the User
    const userModel = await getDbObject('User', true, request);
    const user = await userModel.findOne({ where: { Username: userId }});
    if(!user){
      await throwError(404, `User ${userId} not found`);
    }

		// Get the MAX WallPostId for the user (if none, set to 1)
    const wallPostModel = await getDbObject('WallPost', true, request);
    const maxWallPostId = await wallPostModel.max('WallPostId', { where: { UserId: userId }});
    const nextWallPostId = maxWallPostId ? maxWallPostId + 1 : 1;

		// Create the WallPost
    const wallPost = await wallPostModel.create({
      WallPostId: nextWallPostId,
      UserId: userId,
      PosterId: userId,
      Post: Post,
      Link: Link,
      PostType: PostType,
      PostDate: new Date().toISOString()
    });

		return formatSuccessResponse(request, {
			data: wallPost,
		});
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
}
