import { env, createExecutionContext, waitOnExecutionContext, SELF } from 'cloudflare:test';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import worker from '../../../src/index.js';
import { parseResponse } from '../../helpers/test-helpers.js';

describe('Health Routes - Integration Tests', () => {
	beforeEach(() => {
		// Set response validation to strict for tests
		process.env.RESPONSE_VALIDATION = 'strict';
	});

	describe('GET /health', () => {
		it('should return 200 with health status', async () => {
			const response = await SELF.fetch('http://example.com/health');
			
			expect(response.status).toBe(200);
			const data = await parseResponse(response);
			expect(data.health).toBe('ok');
			expect(data).toHaveProperty('timestamp');
			expect(typeof data.timestamp).toBe('string');
		});

		it('should return valid ISO timestamp', async () => {
			const response = await SELF.fetch('http://example.com/health');
			const data = await parseResponse(response);
			
			// Validate ISO timestamp format
			const timestamp = new Date(data.timestamp);
			expect(timestamp.toISOString()).toBe(data.timestamp);
		});
	});

	describe('GET /health/live', () => {
		it('should return 200 with alive status', async () => {
			const response = await SELF.fetch('http://example.com/health/live');
			
			expect(response.status).toBe(200);
			const data = await parseResponse(response);
			expect(data.alive).toBe(true);
			expect(data).toHaveProperty('timestamp');
		});
	});

	describe('GET /health/ready', () => {
		it('should return 200 with ready status', async () => {
			const response = await SELF.fetch('http://example.com/health/ready');
			
			expect(response.status).toBe(200);
			const data = await parseResponse(response);
			expect(data.ready).toBe(true);
			expect(data).toHaveProperty('database');
			expect(['connected', 'not configured']).toContain(data.database);
			expect(data).toHaveProperty('timestamp');
		});

		it('should handle database connection status', async () => {
			// Note: Actual DB connection testing would require proper mocking
			// This test verifies the endpoint responds correctly
			const response = await SELF.fetch('http://example.com/health/ready');
			const data = await parseResponse(response);
			
			expect(data.database).toBeDefined();
			expect(typeof data.ready).toBe('boolean');
		});
	});
});

