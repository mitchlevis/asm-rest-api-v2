import { z } from 'zod';

export const path = {
	regionId: z.string(),
};

export const query = {

};

export const body = {
	LinkId: z.string().optional().default(null),
	CategoryId: z.coerce.number().optional().default(null),
	LinkTitle: z.string(),
	LinkAddress: z.string(),
	LinkDescription: z.string(),
};

export default {
  path,
  query,
  body
};

