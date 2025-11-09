import { z } from 'zod';

export const path = {
	regionId: z.string(),
	categoryId: z.coerce.number(),
};

export const query = {

};

export const body = {

};

export default {
  path,
  query,
  body
};

