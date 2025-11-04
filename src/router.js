import { Router } from 'itty-router';
import { formatSuccessResponse, formatErrorResponse } from './utils/helpers.js';
import sequelizeAdapter from './db/adapter.js';

// Import middleware
// import { dbMiddleware } from './middleware/db.js';

// Import route handlers
import { setupHealthRoutes } from './routes/health.js';
import { setupActionsRoutes } from './routes/actions.js';

/**
 * Creates and configures the main router with all routes
 * @param {Object} env - Cloudflare environment variables
 * @returns {Router} Configured router instance
 */
export function createRouter(env) {
	// Initialize the database adapter with environment variables
	sequelizeAdapter.initialize(env);

	const router = Router();

	// Apply database middleware to all routes using itty-router's middleware pattern
	// Creates a middleware function that captures env in closure
	// Middleware returns undefined to continue, or Response to stop execution
	// router.all('*', (request) => dbMiddleware(request, env));

	// Setup route handlers for different features
	setupHealthRoutes(router);
	setupActionsRoutes(router);

	// CORS Options TODO
	router.options('*', (request) => {
		return new Response('OK', { status: 200 });
	});

	// 404 handler for unmatched routes
	router.all('*', (request) => {
		return formatErrorResponse(request, { statusCode: 404, message: 'Not Found 404-R' });
	});

	return router;
}

