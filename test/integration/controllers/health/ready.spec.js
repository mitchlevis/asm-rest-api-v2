import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createMockRequest } from '../../../helpers/test-helpers.js';
import { parseResponse } from '../../../helpers/test-helpers.js';

// Mock Sequelize to avoid dayjs.extend error - must be hoisted
vi.mock('@sequelize/core', () => ({
	default: class MockSequelize {},
	Sequelize: class MockSequelize {},
}));

// Mock database adapter to prevent Sequelize initialization
vi.mock('../../../../src/db/adapter.js', () => ({
	default: {
		getSequelize: vi.fn().mockResolvedValue(null),
	},
	getSequelize: vi.fn().mockResolvedValue(null),
}));

// Mock getSequelizeObject before importing the controller
vi.mock('../../../../src/utils/helpers.js', async () => {
	const actual = await vi.importActual('../../../../src/utils/helpers.js');
	return {
		...actual,
		getSequelizeObject: vi.fn(),
	};
});

describe('Health Ready Controller - GET /health/ready', () => {
	let getSequelizeObject;
	let readyController;

	beforeEach(async () => {
		process.env.RESPONSE_VALIDATION = 'strict';
		// Import helpers in beforeEach to avoid hoisting issues
		const helpers = await import('../../../../src/utils/helpers.js');
		getSequelizeObject = helpers.getSequelizeObject;
		readyController = (await import('../../../../src/controllers/health/ready/index.js')).default;
	});

	it('should return ready status when database is connected', async () => {
		const mockSequelize = {
			authenticate: vi.fn().mockResolvedValue(undefined),
		};
		vi.mocked(getSequelizeObject).mockResolvedValue(mockSequelize);

		const request = createMockRequest('GET', 'http://example.com/health/ready');
		
		const response = await readyController(request);
		
		expect(response.status).toBe(200);
		const data = await parseResponse(response);
		expect(data.ready).toBe(true);
		expect(data.database).toBe('connected');
		expect(data).toHaveProperty('timestamp');
	});

	it('should return ready status when database is not configured', async () => {
		vi.mocked(getSequelizeObject).mockResolvedValue(null);

		const request = createMockRequest('GET', 'http://example.com/health/ready');
		
		const response = await readyController(request);
		
		expect(response.status).toBe(200);
		const data = await parseResponse(response);
		expect(data.ready).toBe(true);
		expect(data.database).toBe('not configured');
		expect(data).toHaveProperty('timestamp');
	});

	it('should handle database connection errors', async () => {
		const mockSequelize = {
			authenticate: vi.fn().mockRejectedValue(new Error('Connection failed')),
		};
		vi.mocked(getSequelizeObject).mockResolvedValue(mockSequelize);

		const request = createMockRequest('GET', 'http://example.com/health/ready');
		
		const response = await readyController(request);
		
		// Should return error response
		expect(response.status).toBeGreaterThanOrEqual(400);
	});
});

