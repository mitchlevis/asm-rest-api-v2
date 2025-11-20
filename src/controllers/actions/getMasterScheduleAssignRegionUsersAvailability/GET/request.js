import { z } from 'zod';

export const path = {
	regionId: z.string(),
	scheduleId: z.coerce.number(),
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

