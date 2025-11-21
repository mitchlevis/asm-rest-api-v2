import { z } from 'zod';
import { parseFilterJSON } from '../../../../utils/helpers.js';

export const path = {
	regionId: z.string(),
};

export const query = {
	filter: z.string().optional().transform((val) => val && val.length > 0 ? parseFilterJSON(decodeURIComponent(val)) : undefined),
	sort: z.string()
		.optional()
		.transform((val) => {
			if (!val) return 'FirstName';
			return val.split(',').map(sort => sort.trim()).filter(sort => sort.length > 0);
		}),
	sort_direction: z.enum(['ASC', 'DESC', 'asc', 'desc']).optional()
		.transform((val) => {
			if (!val) return 'ASC';
			return val.toUpperCase();
		}),
	limit: z.coerce.number().optional().default(25),
	offset: z.coerce.number().optional().default(0),
};

export const body = {

};

export default {
  path,
  query,
  body
};

