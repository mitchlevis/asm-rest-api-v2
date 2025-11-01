import { formatSuccessResponse, formatErrorResponse } from '../utils/helpers.js';
/**
 * Health check and status routes
 */

export function setupHealthRoutes(router) {
	// GET /health - Basic health check
	router.get('/health', (request) => {
		return formatSuccessResponse(request, {
			data: {
				health: 'ok',
				timestamp: new Date().toISOString(),
			},
			extraHeaders: {
				'Content-Type': 'application/json',
			},
		});
	});

	// GET /health/ready - Readiness check (includes DB check)
	router.get('/health/ready', async (request) => {
		const sequelize = request.db;

		if (sequelize) {
			try {
				await sequelize.authenticate();
				return formatSuccessResponse(request, {
					data: {
					ready: true,
					database: 'connected',
					timestamp: new Date().toISOString(),
				}});
			} catch (error) {
				return formatErrorResponse(request, error);
			}
		}

		return formatSuccessResponse(request, {
			data: {
				ready: true,
				database: 'not configured',
				timestamp: new Date().toISOString(),
			},
		});
	});

	// GET /health/live - Liveness check
	router.get('/health/live', (request) => {
		return formatSuccessResponse(request, {
			data: {
				alive: true,
				timestamp: new Date().toISOString(),
			},
		});
	});
}

