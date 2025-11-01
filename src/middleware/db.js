/**
 * Database middleware for Cloudflare Workers
 * Initializes Sequelize connection and attaches it to the request object
 * 
 * Follows itty-router middleware pattern:
 * - Returns undefined to continue execution
 * - Returns Response to stop execution
 */

import { getSequelize } from '../db/adapter.js';

/**
 * Middleware that initializes and attaches Sequelize to the request
 * @param {Request} request - The incoming request
 * @param {Object} env - Cloudflare environment variables
 * @returns {Promise<Response|undefined>} Returns Response if DB connection fails, otherwise undefined
 */
export async function dbMiddleware(request, env) {
	try {
		// Initialize database connection
		const sequelize = await getSequelize(env);
		
		// Attach sequelize to request object for use in route handlers
		request.db = sequelize;
		
		// Return undefined to continue to next middleware/route handler
		// (explicit return undefined is optional, but makes intent clear)
	} catch (error) {
		console.error('Database connection error:', error);
		// Return error response to stop execution (all downstream handlers won't run)
		return new Response(
			JSON.stringify({
				error: 'Database connection failed',
				message: error.message,
			}),
			{
				status: 503,
				headers: { 'Content-Type': 'application/json' },
			}
		);
	}
}

