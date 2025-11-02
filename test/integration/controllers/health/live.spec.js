import { describe, it, expect, beforeEach } from 'vitest';
import liveController from '../../../../src/controllers/health/live/index.js';
import { createMockRequest } from '../../../helpers/test-helpers.js';
import { parseResponse } from '../../../helpers/test-helpers.js';

describe('Health Live Controller - GET /health/live', () => {
	beforeEach(() => {
		process.env.RESPONSE_VALIDATION = 'strict';
	});

	it('should return alive status', async () => {
		const request = createMockRequest('GET', 'http://example.com/health/live');
		
		const response = await liveController(request);
		
		expect(response.status).toBe(200);
		const data = await parseResponse(response);
		expect(data.alive).toBe(true);
		expect(data).toHaveProperty('timestamp');
	});

	it('should return valid response structure', async () => {
		const request = createMockRequest('GET', 'http://example.com/health/live');
		
		const response = await liveController(request);
		const data = await parseResponse(response);
		
		expect(data).toHaveProperty('alive');
		expect(data).toHaveProperty('timestamp');
		expect(typeof data.alive).toBe('boolean');
		expect(typeof data.timestamp).toBe('string');
	});
});

