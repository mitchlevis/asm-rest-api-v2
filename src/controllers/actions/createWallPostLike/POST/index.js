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

		const { WallPostUserId, WallPostId, IsLiked, CommentId, CommentCommentId } = body;

		// Get the WallPost
		const wallPostModel = await getDbObject('WallPost', true, request);
		const wallPost = await wallPostModel.findOne({ where: { WallPostId: WallPostId, UserId: WallPostUserId }});
		if(!wallPost){
			await throwError(404, `Wall post ${WallPostId} not found`);
		}

		const userModel = await getDbObject('User', true, request);
		// Get the User
		const user = await userModel.findOne({ where: { Username: userId }});
		if(!user){
			await throwError(404, `User ${userId} not found`);
		}

		let returnObject;

		// If there is a CommentCommentId and a CommentId, we are creating a like for a wall post comment comment
		if(CommentCommentId && CommentId){
			// Get the WallPostCommentComment
			const wallPostCommentCommentModel = await getDbObject('WallPostCommentComment', true, request);
			const wallPostCommentComment = await wallPostCommentCommentModel.findOne({ where: { CommentCommentId: CommentCommentId, CommentId: CommentId, WallPostId: WallPostId, UserId: WallPostUserId }});
			if(!wallPostCommentComment){
				await throwError(404, `Wall post comment comment ${CommentCommentId} not found`);
			}

			// If the user is liking the wall post comment comment, we need to create a new like
			if(IsLiked){
				// Create the WallPostCommentCommentLike
				const wallPostCommentCommentLikeModel = await getDbObject('WallPostCommentCommentLike', true, request);
				returnObject = await wallPostCommentCommentLikeModel.create({
					UserId: WallPostUserId,
					WallPostId: WallPostId,
					CommentId: CommentId,
					CommentCommentId: CommentCommentId,
					CommentLikerId: userId,
					LikeType: 0,
					LikeDate: new Date().toISOString()
				});
			}
			else { // If the user is unliking the wall post comment comment, we need to delete the like
				// Delete the WallPostCommentCommentLike
				const wallPostCommentCommentLikeModel = await getDbObject('WallPostCommentCommentLike', true, request);
				await wallPostCommentCommentLikeModel.destroy({ where: { UserId: WallPostUserId, WallPostId: WallPostId, CommentId: CommentId, CommentCommentId: CommentCommentId, CommentLikerId: userId }});
				returnObject = {
					success: true,
					message: `Wall post comment comment ${CommentCommentId} unliked`
				};
			}
		}
		else if(CommentId){ // If there is a CommentId, we are creating a like for a wall post comment
			// Get the WallPostComment
			const wallPostCommentModel = await getDbObject('WallPostComment', true, request);
			const wallPostComment = await wallPostCommentModel.findOne({ where: { CommentId: CommentId, WallPostId: WallPostId, UserId: WallPostUserId }});
			if(!wallPostComment){
				await throwError(404, `Wall post comment ${CommentId} not found`);
			}

			// If the user is liking the wall post comment, we need to create a new like
			if(IsLiked){
				// Create the WallPostCommentLike
				const wallPostCommentLikeModel = await getDbObject('WallPostCommentLike', true, request);
				returnObject = await wallPostCommentLikeModel.create({
					UserId: WallPostUserId,
					WallPostId: WallPostId,
					CommentId: CommentId,
					CommentLikerId: userId,
					LikeType: 0,
					LikeDate: new Date().toISOString()
				});
			}
			else { // If the user is unliking the wall post comment, we need to delete the like
				// Delete the WallPostCommentLike
				const wallPostCommentLikeModel = await getDbObject('WallPostCommentLike', true, request);
				const wallPostCommentLike = await wallPostCommentLikeModel.findOne({ where: { UserId: WallPostUserId, WallPostId: WallPostId, CommentId: CommentId, CommentLikerId: userId }});
				if(wallPostCommentLike){
					await wallPostCommentLikeModel.destroy({ where: { UserId: WallPostUserId, WallPostId: WallPostId, CommentId: CommentId, CommentLikerId: userId }});
				}
				returnObject = {
					success: true,
					message: `Wall post comment ${CommentId} unliked`
				};
			}

		}
		else { // If there is no CommentId, we are creating a like for a wall post
			// If the user is liking the wall post, we need to create a new like
			if(IsLiked){
				// Create the WallPostLike
				const wallPostLikeModel = await getDbObject('WallPostLike', true, request);
				returnObject = await wallPostLikeModel.create({
					UserId: WallPostUserId,
					WallPostId: WallPostId,
					LikerId: userId,
					LikeType: 0,
					LikeDate: new Date().toISOString()
				});
			}
			else { // If the user is unliking the wall post, we need to delete the like
				const wallPostLikeModel = await getDbObject('WallPostLike', true, request);
				const wallPostLike = await wallPostLikeModel.findOne({ where: { UserId: WallPostUserId, WallPostId: WallPostId, LikerId: userId }});
				if(wallPostLike){
					await wallPostLikeModel.destroy({ where: { UserId: WallPostUserId, WallPostId: WallPostId, LikerId: userId }});
				}
				returnObject = {
					success: true,
					message: `Wall post ${WallPostId} unliked`
				};
			}
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
