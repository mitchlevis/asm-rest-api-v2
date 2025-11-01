import { env, createExecutionContext, waitOnExecutionContext, SELF } from 'cloudflare:test';
import { describe, it, expect } from 'vitest';
import worker from '../src';

describe('API Routes', () => {
	describe('Health endpoints', () => {
		it('GET /health returns ok status', async () => {
			const request = new Request('http://example.com/health');
			const ctx = createExecutionContext();
			const response = await worker.fetch(request, env, ctx);
			await waitOnExecutionContext(ctx);

			expect(response.status).toBe(200);
			const data = await response.json();
			expect(data.status).toBe('ok');
			expect(data).toHaveProperty('timestamp');
		});

		it('GET /health/ready returns readiness status', async () => {
			const response = await SELF.fetch('http://example.com/health/ready');
			expect(response.status).toBe(200);
			const data = await response.json();
			expect(data.ready).toBe(true);
		});
	});

	describe('User endpoints', () => {
		it('GET /users returns list of users', async () => {
			const request = new Request('http://example.com/users');
			const ctx = createExecutionContext();
			const response = await worker.fetch(request, env, ctx);
			await waitOnExecutionContext(ctx);

			expect(response.status).toBe(200);
			const data = await response.json();
			expect(data).toHaveProperty('users');
		});

		it('GET /users/:id returns specific user', async () => {
			const response = await SELF.fetch('http://example.com/users/123');
			expect(response.status).toBe(200);
			const data = await response.json();
			expect(data.id).toBe('123');
		});

		it('GET /users/:id/teams returns user teams (subroute)', async () => {
			const response = await SELF.fetch('http://example.com/users/123/teams');
			expect(response.status).toBe(200);
			const data = await response.json();
			expect(data.userId).toBe('123');
			expect(data).toHaveProperty('teams');
		});
	});

	describe('Team endpoints', () => {
		it('GET /teams returns list of teams', async () => {
			const response = await SELF.fetch('http://example.com/teams');
			expect(response.status).toBe(200);
			const data = await response.json();
			expect(data).toHaveProperty('teams');
		});

		it('GET /teams/:id/players returns team players (subroute)', async () => {
			const response = await SELF.fetch('http://example.com/teams/456/players');
			expect(response.status).toBe(200);
			const data = await response.json();
			expect(data.teamId).toBe('456');
			expect(data).toHaveProperty('players');
		});
	});

	describe('404 handling', () => {
		it('returns 404 for unknown routes', async () => {
			const response = await SELF.fetch('http://example.com/unknown');
			expect(response.status).toBe(404);
		});
	});
});
