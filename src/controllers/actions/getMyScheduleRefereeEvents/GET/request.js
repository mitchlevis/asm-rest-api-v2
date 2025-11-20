import { z } from 'zod';
import { parseFilterJSON } from '../../../../utils/helpers.js';

export const path = {
	regionId: z.string().optional().transform((val) => val === '' ? null : val).default(null),
};

export const query = {
	filter: z.string().optional().transform((val) => val && val.length > 0 ? parseFilterJSON(decodeURIComponent(val)) : undefined),
	sort: z.string()
		.optional()
		.transform((val) => {
			if (!val) return 'GameDate';
			return val.split(',').map(sort => sort.trim()).filter(sort => sort.length > 0);
		}),
	sort_direction: z.enum(['ASC', 'DESC', 'asc', 'desc']).optional()
		.transform((val) => {
			if (!val) return 'DESC';
			return val.toUpperCase();
		}),
	limit: z.coerce.number().optional().default(-1),
	offset: z.coerce.number().optional().default(0),
	show_archived: z.coerce.boolean().optional().default(false),
	show_only_open: z.coerce.boolean().optional().default(false),
	show_deleted: z.coerce.boolean().optional().default(false),
	show_removed: z.coerce.boolean().optional().default(false),
	group_parks: z.coerce.boolean().optional().default(false),
	include_facets: z.coerce.boolean().optional().default(false),
	facet_limit: z.coerce.number().optional().default(50),
};

export const body = {

};

export default {
  path,
  query,
  body
};

