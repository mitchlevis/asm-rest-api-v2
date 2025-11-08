import { z } from 'zod';

export const path = {

};

export const query = {

};

export const body = {
	WallPostUserId: z.string(),
	WallPostId: z.coerce.number(),
	IsLiked: z.boolean(),
	CommentId: z.coerce.number().optional().default(null),
	CommentCommentId: z.coerce.number().optional().default(null),
};

export default {
  path,
  query,
  body
};

