import { z } from 'zod';

export const path = {

};

export const query = {

};

export const body = {
	regionId: z.string(),
	accept: z.boolean(),
};

export default {
  path,
  query,
  body
};

