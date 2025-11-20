import { z } from 'zod';

export const path = {
	regionId: z.string().optional().transform((val) => val === '' ? null : val).default(null),
};

export const query = {
	show_archived: z.coerce.boolean().optional().default(false),
	force_refresh: z.coerce.boolean().optional().default(false),
};

export const body = {

};

export default {
  path,
  query,
  body
};
