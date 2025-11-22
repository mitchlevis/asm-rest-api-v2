import { z } from 'zod';
import { parseBooleanQueryParam } from '../../../../utils/helpers.js';

export const path = {
	regionId: z.string(),
};

export const query = {
	camel_case: parseBooleanQueryParam(),
};

export const body = {

};

export default {
  path,
  query,
  body
};

