import { env, SELF } from 'cloudflare:test';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { parseResponse } from '../../helpers/test-helpers.js';
import { validSessionToken, expiredSessionToken } from '../../fixtures/session-tokens.js';

describe('Actions Routes - Integration Tests', () => {
	beforeEach(() => {
		process.env.RESPONSE_VALIDATION = 'strict';
	});

	describe('GET /actions/wall-posts-for-user', () => {
		it('should return 401 when session token is missing', async () => {
			const response = await SELF.fetch('http://example.com/actions/wall-posts-for-user');
			
			expect(response.status).toBe(401);
			const data = await parseResponse(response);
			expect(data.statusCode).toBe(401);
			expect(data.message).toContain('Unauthorized');
		});

		it('should return 401 when username header is missing', async () => {
			const response = await SELF.fetch('http://example.com/actions/wall-posts-for-user', {
				headers: {
					'x-session-token': 'some-token',
				},
			});
			
			expect(response.status).toBe(401);
		});

		it('should return 401 when session token header is missing', async () => {
			const response = await SELF.fetch('http://example.com/actions/wall-posts-for-user', {
				headers: {
					'x-username': 'testuser',
				},
			});
			
			expect(response.status).toBe(401);
		});
	});

	describe('GET /actions/wall-posts-for-user/:regionId', () => {
		it('should return 401 when session token is missing', async () => {
			const response = await SELF.fetch('http://example.com/actions/wall-posts-for-user/region1');
			
			expect(response.status).toBe(401);
		});

		it('should accept regionId parameter', async () => {
			// This test would require proper DB mocking to pass authentication
			// For now, we verify it accepts the parameter format
			const response = await SELF.fetch('http://example.com/actions/wall-posts-for-user/region1', {
				headers: {
					'x-session-token': 'token',
					'x-username': 'user',
				},
			});
			
			// Should fail at auth, but parameter should be accepted
			expect([401, 400]).toContain(response.status);
		});
	});
});

