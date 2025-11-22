import { z } from 'zod';
import { parseFilterJSON, parseBooleanQueryParam } from '../../../../utils/helpers.js';

export const path = {
	regionId: z.string().optional().transform((val) => val === '' ? null : val).default(null),
};

export const query = {
	filter: z.string().optional().transform((val) => val && val.length > 0 ? parseFilterJSON(decodeURIComponent(val)) : undefined),
	sort: z.string()
		.optional()
		.transform((val) => {
			if (!val) return ['GameDate'];
			return val.split(',').map(sort => sort.trim()).filter(sort => sort.length > 0);
		}),
	sort_direction: z.enum(['ASC', 'DESC', 'asc', 'desc']).optional()
		.transform((val) => {
			if (!val) return 'DESC';
			return val.toUpperCase();
		}),
	limit: z.coerce.number().optional().default(-1),
	offset: z.coerce.number().optional().default(0),
	show_archived: parseBooleanQueryParam(),
	show_only_open: parseBooleanQueryParam(),
	show_deleted: parseBooleanQueryParam(),
	show_removed: parseBooleanQueryParam(),
	group_parks: parseBooleanQueryParam(),
	include_facets: parseBooleanQueryParam(),
	facet_limit: z.coerce.number().optional().default(50),
};

export const body = {

};

export default {
  path,
  query,
  body
};

