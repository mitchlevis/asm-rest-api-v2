import { z } from 'zod';

export const path = {

};

export const query = {

};

export const body = {
	session_token: z.string(),
	username: z.string(),
};

export default {
  path,
  query,
  body
};

