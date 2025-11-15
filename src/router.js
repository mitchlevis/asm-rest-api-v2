import { Router } from 'itty-router';
import { formatSuccessResponse, formatErrorResponse } from './utils/helpers.js';
import sequelizeAdapter from './db/adapter.js';
import { usePosthog } from './services/posthog.js';
import { useR2Service } from './services/R2.js';
// Import middleware
// import { dbMiddleware } from './middleware/db.js';

// Import route handlers
import { setupHealthRoutes } from './routes/health.js';
import { setupActionsRoutes } from './routes/actions.js';
import { setupS3Routes } from './routes/s3.js';
import { setupEmailRoutes } from './routes/email.js';
import { setupRestRoutes } from './routes/rest.js';
/**
 * Creates and configures the main router with all routes
 * @param {Object} env - Cloudflare environment variables
 * @returns {Router} Configured router instance
 */
export function createRouter(request, env, ctx) {
	// Initialize the database adapter with environment variables
	sequelizeAdapter.initialize(env);

	// Initialize Posthog
	const posthog = usePosthog();
	posthog.initialize(request, env, ctx);

	// Initialize R2 Service
	const r2 = useR2Service();
	r2.initialize(env);

	const router = Router();

	// Apply database middleware to all routes using itty-router's middleware pattern
	// Creates a middleware function that captures env in closure
	// Middleware returns undefined to continue, or Response to stop execution
	// router.all('*', (request) => dbMiddleware(request, env));

	// Setup route handlers for different features
	setupHealthRoutes(router);
	setupActionsRoutes(router);
	setupS3Routes(router);
	setupEmailRoutes(router);
	setupRestRoutes(router);

	// CORS Options handler for preflight requests
	router.options('*', (request) => {
		const headers = { 'Content-Type': 'application/json' };
		// Add CORS Headers - use specific origin when credentials are involved
		const origin = request.headers.get('Origin');
		headers['Access-Control-Allow-Origin'] = origin || '*';
		headers['Access-Control-Allow-Methods'] = 'GET, POST, PUT, DELETE, OPTIONS, PATCH';
		headers['Access-Control-Allow-Headers'] = 'Content-Type, authorization, Authorization, x-session-token, X-Session-Token, x-username, X-Username, x-total-count, X-Total-Count';
		headers['Access-Control-Allow-Credentials'] = 'true';

		return new Response('OK', { status: 200, headers: headers });
	});

	// 404 handler for unmatched routes
	router.all('*', (request) => {
		return formatErrorResponse(request, { statusCode: 404, message: 'Not Found 404-R' });
	});

	return router;
}

