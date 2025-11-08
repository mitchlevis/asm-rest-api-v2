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

		const { WallPostUserId, WallPostId, Comment, ParentCommentId } = body;

		 // Get the WallPost
		 const wallPostModel = await getDbObject('WallPost');
		 const wallPost = await wallPostModel.findOne({ where: { WallPostId: WallPostId, UserId: WallPostUserId }});
		 if(!wallPost){
			 await throwError(404, `Wall post ${WallPostId} not found`);
		 }

		 const userModel = await getDbObject('User');
		 // Get the User
		 const user = await userModel.findOne({ where: { Username: userId }});
		 if(!user){
			 await throwError(404, `User ${userId} not found`);
		 }

		 const wallPostCommentModel = await getDbObject('WallPostComment');

		 // Get the MAX CommentId for the Wall Post
		 const maxCommentId = await wallPostCommentModel.max('CommentId', { where: { UserId: WallPostUserId, WallPostId: WallPostId }});
		 const nextCommentId = maxCommentId ? maxCommentId + 1 : 1;

		 let returnObject;

		 // If there is a parent comment, we need to get the parent comment
		 if(ParentCommentId){
			 // Get the ParentComment
			 const parentCommentModel = await getDbObject('WallPostComment');
			 const parentComment = await parentCommentModel.findOne({ where: { CommentId: ParentCommentId, WallPostId: WallPostId, UserId: WallPostUserId }});
			 if(!parentComment){
				 await throwError(404, `Parent comment ${ParentCommentId} not found`);
			 }

			 // Get the MAX CommentCommentId for WallPostCommentComment
			 const commentCommentModel = await getDbObject('WallPostCommentComment');
			 const maxCommentCommentId = await commentCommentModel.max('CommentCommentId', { where: { CommentId: ParentCommentId, WallPostId: WallPostId, UserId: WallPostUserId }});
			 const nextCommentCommentId = maxCommentCommentId ? maxCommentCommentId + 1 : 1;

			 // Create the WallPostCommentComment
			 returnObject = await commentCommentModel.create({
				 CommentCommentId: nextCommentCommentId,
				 CommentId: ParentCommentId,
				 WallPostId: WallPostId,
				 UserId: WallPostUserId,
				 CommenterId: userId,
				 Comment: Comment,
				 CommentDate: new Date().toISOString(),
				 IsPhoto: false
			 });
		 }
		 else {
			 // Create the WallPostComment
			 returnObject = await wallPostCommentModel.create({
				 CommentId: nextCommentId,
				 WallPostId: WallPostId,
				 UserId: WallPostUserId,
				 CommenterId: userId,
				 Comment: Comment,
				 CommentDate: new Date().toISOString(),
				 IsPhoto: false
			 });
		 }

		return formatSuccessResponse(request, {
			data: returnObject,
		});
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
}
