import { z } from 'zod';

export const path = {
	invitationId: z.string(),
	email: z.string().email(),
};

export const query = {

};

export const body = {

};

export default {
  path,
  query,
  body
};

