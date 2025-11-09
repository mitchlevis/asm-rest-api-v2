import { z } from 'zod';

export const path = {
	regionId: z.string(),
};

export const query = {

};

export const body = {
	CategoryId: z.coerce.number().optional().default(null),
	CategoryName: z.string(),
	CategoryDescription: z.string().optional().default(null),
	CategoryColor: z.string().optional().default(null),
	SortOrder: z.coerce.number().optional().default(null),
};

export default {
  path,
  query,
  body
};

