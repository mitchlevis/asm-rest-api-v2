import { describe, it, expect, vi, beforeEach } from 'vitest';
import healthController from '../../../../src/controllers/health/index/index.js';
import { createMockRequest } from '../../../helpers/test-helpers.js';
import { parseResponse } from '../../../helpers/test-helpers.js';

describe('Health Controller - GET /health', () => {
	beforeEach(() => {
		process.env.RESPONSE_VALIDATION = 'strict';
	});

	it('should return health status', async () => {
		const request = createMockRequest('GET', 'http://example.com/health');
		
		const response = await healthController(request);
		
		expect(response.status).toBe(200);
		const data = await parseResponse(response);
		expect(data.health).toBe('ok');
		expect(data).toHaveProperty('timestamp');
	});

	it('should return valid response structure', async () => {
		const request = createMockRequest('GET', 'http://example.com/health');
		
		const response = await healthController(request);
		const data = await parseResponse(response);
		
		// Validate structure matches response schema
		expect(data).toHaveProperty('health');
		expect(data).toHaveProperty('timestamp');
		expect(typeof data.timestamp).toBe('string');
	});
});

