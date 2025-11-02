/**
 * Controllers for health routes
 */

import healthController from '../controllers/health/index/index.js';
import liveController from '../controllers/health/live/index.js';
import readyController from '../controllers/health/ready/index.js';

export function setupHealthRoutes(router) {
	// GET /health - Basic health check
	router.get('/health', healthController);

	// GET /health/ready - Readiness check (includes DB check)
	router.get('/health/ready', readyController);

	// GET /health/live - Liveness check
	router.get('/health/live', liveController);
}

