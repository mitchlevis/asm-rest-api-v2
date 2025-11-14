import { z } from 'zod';

export const path = {

};

export const query = {

};

export const body = {
	from: z.string().optional().default('integration@asportsmanager.com'),
	to: z.union([z.string(), z.array(z.string())]).transform((val) => Array.isArray(val) ? val : [val]), // Single email address or array of email addresses - normalized to array
	subject: z.string(),
	body: z.string(),
};

export default {
  path,
  query,
  body
};
