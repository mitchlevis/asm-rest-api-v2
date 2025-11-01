import { z } from 'zod';

export const path = {
	regionId: z.string().optional().transform((val) => val === '' ? null : val).default(null),
};

export const query = {
	sort_direction: z.enum(['ASC', 'DESC']).optional().default('DESC'),
	limit: z.coerce.number().optional().default(-1),
	offset: z.coerce.number().optional().default(0),
};

export const body = {

};

export default {
  path,
  query,
  body
};
