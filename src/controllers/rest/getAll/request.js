import { z } from 'zod';
import { parseFilterJSON } from '../../../utils/helpers';

export const path = {
	resourceName: z.string(),
};

export const query = {
	limit: z.coerce.number().optional().default(25),
	offset: z.coerce.number().optional().default(0),
	attributes: z.string()
		.optional()
		.transform((val) => {
			if (!val) return undefined;
			return val.split(',').map(attr => attr.trim()).filter(attr => attr.length > 0);
		}),
	filter: z.string().optional().transform((val) => val && val.length > 0 ? parseFilterJSON(decodeURIComponent(val)) : undefined),
	sort: z.string()
		.optional()
		.transform((val) => {
			if (!val) return undefined;
			return val.split(',').map(sort => sort.trim()).filter(sort => sort.length > 0);
		}),
	sort_direction: z.enum(['ASC', 'DESC', 'asc', 'desc']).optional()
		.transform((val) => {
			if (!val) return 'ASC';
			return val.toUpperCase();
		}),
};

export const body = {

};

export default {
  path,
  query,
  body
};
