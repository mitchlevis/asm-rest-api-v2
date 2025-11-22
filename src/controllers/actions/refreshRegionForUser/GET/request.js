import { z } from 'zod';

export const path = {
	regionId: z.string(),
};

export const query = {
	camel_case: z.coerce.boolean().optional().default(false),
};

export const body = {

};

export default {
  path,
  query,
  body
};

