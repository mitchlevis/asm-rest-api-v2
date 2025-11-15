import { z } from 'zod';

export const path = {
	resourceName: z.string(),
	id: z.string(),
};

export const query = {
	attributes: z.string()
		.optional()
		.transform((val) => {
			if (!val) return undefined;
			return val.split(',').map(attr => attr.trim()).filter(attr => attr.length > 0);
		}),
};

export const body = {

};

export default {
  path,
  query,
  body
};
