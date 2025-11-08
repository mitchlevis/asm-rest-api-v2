import { z } from 'zod';

export const path = {

};

export const query = {

};

export const body = {
	username: z.string(),
	password: z.string(),
	rememberMe: z.boolean().optional().default(false),
};

export default {
  path,
  query,
  body
};

