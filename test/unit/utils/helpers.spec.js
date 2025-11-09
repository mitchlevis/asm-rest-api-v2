import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { createMockRequest, createMockEnv } from '../../helpers/test-helpers.js';
import { mockModel, mockSequelize } from '../../helpers/db-mocks.js';
import { validUser } from '../../fixtures/users.js';
import { validSessionToken, expiredSessionToken } from '../../fixtures/session-tokens.js';

// Mock the database adapter
vi.mock('../../../src/db/adapter.js', () => {
	const getSequelize = vi.fn();
	const closeSequelize = vi.fn();
	return {
		default: {
			getSequelize,
			closeSequelize,
		},
		getSequelize,
		closeSequelize,
	};
});

// Mock getDbObject to return mock models
vi.mock('../../../src/db/models', () => ({
	MODELS: {
		User: {},
		SessionToken: {},
	},
	MODEL_ASSOCIATIONS: {},
}));

// Import helpers
import {
	formatSuccessResponse,
	formatErrorResponse,
	validateIncomingParameters,
	authenticateSessionToken,
	getDbObject,
} from '../../../src/utils/helpers.js';

describe('formatSuccessResponse', () => {
	let mockSequelizeInstance;
	let sequelizeAdapter;

	beforeEach(async () => {
		mockSequelizeInstance = mockSequelize(vi);
		sequelizeAdapter = await import('../../../src/db/adapter.js');
		// Reset and set up mock to return instance for getSequelize(request, false)
		vi.mocked(sequelizeAdapter.getSequelize).mockImplementation(async (request, createIfNotExists) => {
			return mockSequelizeInstance;
		});
		vi.mocked(sequelizeAdapter.closeSequelize).mockImplementation(async (request) => {
			return mockSequelizeInstance.close();
		});
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('should format a success response with data', async () => {
		const request = createMockRequest();
		const data = { test: 'data' };

		const response = await formatSuccessResponse(request, { data });

		expect(response.status).toBe(200);
		const responseData = await response.json();
		expect(responseData).toEqual(data);
		// Connection should always be closed
		expect(sequelizeAdapter.closeSequelize).toHaveBeenCalledWith(request);
	});

	it('should handle custom status code', async () => {
		const request = createMockRequest();
		const data = { test: 'data' };

		const response = await formatSuccessResponse(request, {
			data,
			statusCode: 201,
		});

		expect(response.status).toBe(201);
	});

	it('should handle extra headers', async () => {
		const request = createMockRequest();
		const data = { test: 'data' };

		const response = await formatSuccessResponse(request, {
			data,
			extraHeaders: { 'X-Custom': 'value' },
		});

		expect(response.headers.get('X-Custom')).toBe('value');
	});

	it('should always close DB connection', async () => {
		const request = createMockRequest();
		const data = { test: 'data' };

		await formatSuccessResponse(request, {
			data,
		});

		// Connection should always be closed for each request
		expect(sequelizeAdapter.closeSequelize).toHaveBeenCalledWith(request);
	});

	it('should validate response when schema provided and validation is strict', async () => {
		process.env.RESPONSE_VALIDATION = 'strict';
		const request = createMockRequest();
		const data = { test: 'data' };
		const schema = { 
			parse: vi.fn().mockReturnValue(data),
			safeParse: vi.fn().mockReturnValue({ success: true, data }),
		};

		const response = await formatSuccessResponse(request, {
			data,
			responseSchema: schema,
		});

		expect(schema.parse).toHaveBeenCalledWith(data);
		expect(response.status).toBe(200);
	});

	it('should return validation error when schema validation fails in strict mode', async () => {
		// Set environment variable before test
		const originalEnv = process.env.RESPONSE_VALIDATION;
		process.env.RESPONSE_VALIDATION = 'strict';
		
		const request = createMockRequest();
		const data = { test: 'data' };
		const { z } = await import('zod');
		
		// Create a schema that will fail validation
		const schema = z.array(z.string()); // Expects array of strings, but data is an object
		
		// Ensure sequelize instance is returned
		vi.mocked(sequelizeAdapter.getSequelize).mockImplementation(async (request, createIfNotExists) => {
			return mockSequelizeInstance;
		});

		const response = await formatSuccessResponse(request, {
			data,
			responseSchema: schema,
		});

		expect(response.status).toBe(412);
		const responseData = await response.json();
		expect(responseData.statusCode).toBe(412);
		expect(responseData.message).toContain('Response validation failed');
		
		// Restore original env
		if (originalEnv) {
			process.env.RESPONSE_VALIDATION = originalEnv;
		} else {
			delete process.env.RESPONSE_VALIDATION;
		}
	});
});

describe('formatErrorResponse', () => {
	let mockSequelizeInstance;
	let sequelizeAdapter;

	beforeEach(async () => {
		mockSequelizeInstance = mockSequelize(vi);
		sequelizeAdapter = await import('../../../src/db/adapter.js');
		vi.mocked(sequelizeAdapter.getSequelize).mockImplementation(async (request, createIfNotExists) => {
			return mockSequelizeInstance;
		});
		vi.mocked(sequelizeAdapter.closeSequelize).mockImplementation(async (request) => {
			return mockSequelizeInstance.close();
		});
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('should format error response with custom status code', async () => {
		const request = createMockRequest();
		const error = { statusCode: 400, message: 'Bad Request' };

		const response = await formatErrorResponse(request, error);

		expect(response.status).toBe(400);
		const responseData = await response.json();
		expect(responseData).toEqual(error);
		// Connection should always be closed
		expect(sequelizeAdapter.closeSequelize).toHaveBeenCalledWith(request);
	});

	it('should default to 500 for unknown errors', async () => {
		const request = createMockRequest();
		const error = new Error('Unknown error');

		const response = await formatErrorResponse(request, error);

		expect(response.status).toBe(500);
		const responseData = await response.json();
		expect(responseData.statusCode).toBe(500);
	});

	it('should handle Sequelize validation errors', async () => {
		const request = createMockRequest();
		const error = {
			name: 'SequelizeValidationError',
			message: 'Validation error',
		};

		const response = await formatErrorResponse(request, error);

		expect(response.status).toBe(400);
		const responseData = await response.json();
		expect(responseData.statusCode).toBe(400);
	});
});

describe('validateIncomingParameters', () => {
	it('should validate path parameters', async () => {
		const { z } = await import('zod');
		const request = createMockRequest('GET', 'http://example.com/users/123', {
			params: { id: '123' },
		});

		const result = await validateIncomingParameters(request, {
			path: { id: z.string() },
			query: {},
			body: {},
		});

		expect(result.path).toBeDefined();
		expect(result.path.id).toBe('123');
	});

	it('should throw error for invalid path parameters', async () => {
		const { z } = await import('zod');
		const request = createMockRequest('GET', 'http://example.com/users/invalid', {
			params: { id: 'invalid' },
		});

		// Test with a schema that requires a number
		await expect(
			validateIncomingParameters(request, {
				path: {
					id: z.coerce.number(),
				},
				query: {},
				body: {},
			})
		).rejects.toBeDefined();
	});
});

describe('authenticateSessionToken', () => {
	let mockUserModel, mockSessionTokenModel;
	let sequelizeAdapter;

	beforeEach(async () => {
		mockUserModel = mockModel(vi, 'User');
		mockSessionTokenModel = mockModel(vi, 'SessionToken');
		
		// Ensure sequelize adapter returns a proper mock with define method
		sequelizeAdapter = await import('../../../src/db/adapter.js');
		const mockSequelizeInstance = mockSequelize(vi);
		// Mock define to return appropriate model based on resourceName
		mockSequelizeInstance.define = vi.fn().mockImplementation((resourceName) => {
			if (resourceName === 'User') return mockUserModel;
			if (resourceName === 'SessionToken') return mockSessionTokenModel;
			return mockModel(vi, resourceName);
		});
		vi.mocked(sequelizeAdapter.getSequelize).mockImplementation(async (request) => {
			return mockSequelizeInstance;
		});
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('should authenticate valid session token', async () => {
		const request = createMockRequest('GET', 'http://example.com', {
			headers: {
				'x-session-token': validSessionToken.SessionToken,
				'x-username': validSessionToken.Username,
			},
		});

		mockUserModel.findOne.mockResolvedValue({ Username: validSessionToken.Username });
		mockSessionTokenModel.findOne.mockResolvedValue(validSessionToken);

		const result = await authenticateSessionToken(request);

		expect(result).toEqual(validSessionToken);
		expect(mockUserModel.findOne).toHaveBeenCalled();
		expect(mockSessionTokenModel.findOne).toHaveBeenCalled();
	});

	it('should throw error for missing session token header', async () => {
		const request = createMockRequest('GET', 'http://example.com', {
			headers: {
				'x-username': 'testuser',
			},
		});

		await expect(authenticateSessionToken(request)).rejects.toBeDefined();
	});

	it('should throw error for expired session token', async () => {
		const request = createMockRequest('GET', 'http://example.com', {
			headers: {
				'x-session-token': expiredSessionToken.SessionToken,
				'x-username': expiredSessionToken.Username,
			},
		});

		mockUserModel.findOne.mockResolvedValue({ Username: expiredSessionToken.Username });
		mockSessionTokenModel.findOne.mockResolvedValue(expiredSessionToken);

		await expect(authenticateSessionToken(request)).rejects.toBeDefined();
	});
});

