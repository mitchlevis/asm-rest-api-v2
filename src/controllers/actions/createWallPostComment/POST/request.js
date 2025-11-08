import { z } from 'zod';

export const path = {

};

export const query = {

};

export const body = {
	WallPostUserId: z.string(),
	WallPostId: z.coerce.number(),
	Comment: z.string(),
	ParentCommentId: z.coerce.number().optional().default(null),
};

export default {
  path,
  query,
  body
};

