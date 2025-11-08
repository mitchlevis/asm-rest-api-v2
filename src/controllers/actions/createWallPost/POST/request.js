import { z } from 'zod';

export const path = {

};

export const query = {

};

export const body = {
	Post: z.string().optional().default(""),
  Link: z.string().optional().default(""),
  PostType: z.coerce.number().min(0).max(2).default(0), // Can only be 0, 1 or 2 - 0 = text, 1 = youtube link, 2 = image
};

export default {
  path,
  query,
  body
};

