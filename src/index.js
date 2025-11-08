import { formatErrorResponse } from './utils/helpers.js';
/**
 * Main entry point for Cloudflare Worker
 *
 * - Run `npm run dev` in your terminal to start a development server
 * - Open a browser tab at http://localhost:8787/ to see your worker in action
 * - Run `npm run deploy` to publish your worker
 *
 * Learn more at https://developers.cloudflare.com/workers/
 */

import { createRouter } from './router.js';

// Create router instance (will be reused across requests)
let router = null;

export default {
	async fetch(request, env, ctx) {
		// Initialize router if not already created
		if (!router) {
			router = createRouter(request, env, ctx);
		}

		try {
			// Handle the request with the router
			// Database middleware will handle DB connection initialization
			const response = await router.fetch(request, env, ctx);

			// If router returns undefined (no match), return 404
			if (!response) {
				return formatErrorResponse(request, { statusCode: 404, message: 'Not Found' });
			}

			return response;
		} catch (error) {
			// Global error handler
			console.error('Unhandled error:', error);
			return formatErrorResponse(request, { statusCode: 500, message: 'Internal Server Error' });
		}
	},
};
