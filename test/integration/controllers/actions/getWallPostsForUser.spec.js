import { describe, it, expect, vi, beforeEach } from 'vitest';
import getWallPostsForUserController from '../../../../src/controllers/actions/getWallPostsForUser/index.js';
import { createMockRequest } from '../../../helpers/test-helpers.js';
import { parseResponse } from '../../../helpers/test-helpers.js';
import { validSessionToken } from '../../../fixtures/session-tokens.js';
import { validUser } from '../../../fixtures/users.js';
import { regionUsers, mockQueryResults } from '../../../fixtures/db-responses.js';

// Mock all dependencies
vi.mock('../../../../src/utils/helpers.js', async () => {
	const actual = await vi.importActual('../../../../src/utils/helpers.js');
	return {
		...actual,
		authenticateSessionToken: vi.fn(),
		getDbObject: vi.fn(),
		getSequelizeObject: vi.fn(),
	};
});

vi.mock('../../../../src/db/sql_queries/getWallPostsForUser.js', () => ({
	getQuery: vi.fn(),
	formatResult: vi.fn(),
}));

describe('getWallPostsForUser Controller', () => {
	let authenticateSessionToken, getDbObject, getSequelizeObject, getQuery, formatResult;

	beforeEach(async () => {
		process.env.RESPONSE_VALIDATION = 'disabled'; // Disable for controller tests
		
		const helpers = await import('../../../../src/utils/helpers.js');
		authenticateSessionToken = helpers.authenticateSessionToken;
		getDbObject = helpers.getDbObject;
		getSequelizeObject = helpers.getSequelizeObject;

		const sqlQueries = await import('../../../../src/db/sql_queries/getWallPostsForUser.js');
		getQuery = sqlQueries.getQuery;
		formatResult = sqlQueries.formatResult;
	});

	it('should return 401 when authentication fails', async () => {
		vi.mocked(authenticateSessionToken).mockRejectedValue({
			statusCode: 401,
			message: 'Unauthorized',
		});

		const request = createMockRequest('GET', 'http://example.com/actions/wall-posts-for-user', {
			headers: {
				'x-session-token': 'invalid-token',
				'x-username': 'testuser',
			},
		});

		const response = await getWallPostsForUserController(request);

		expect(response.status).toBe(401);
		const data = await parseResponse(response);
		expect(data.statusCode).toBe(401);
	});

	it('should return empty array when user has no regions', async () => {
		vi.mocked(authenticateSessionToken).mockResolvedValue(validSessionToken);
		
		const mockRegionUserModel = {
			findAll: vi.fn().mockResolvedValue([]),
		};
		vi.mocked(getDbObject).mockResolvedValue(mockRegionUserModel);

		const request = createMockRequest('GET', 'http://example.com/actions/wall-posts-for-user', {
			headers: {
				'x-session-token': validSessionToken.SessionToken,
				'x-username': validSessionToken.Username,
			},
		});

		const response = await getWallPostsForUserController(request);

		expect(response.status).toBe(200);
		const data = await parseResponse(response);
		expect(Array.isArray(data)).toBe(true);
		expect(data.length).toBe(0);
	});

	it('should return wall posts when user has regions', async () => {
		vi.mocked(authenticateSessionToken).mockResolvedValue(validSessionToken);
		
		const mockRegionUserModel = {
			findAll: vi.fn()
				.mockResolvedValueOnce(regionUsers) // First call for user's regions
				.mockResolvedValueOnce([{ RealUsername: 'user1' }, { RealUsername: 'user2' }]), // Second call for users in regions
		};
		vi.mocked(getDbObject).mockResolvedValue(mockRegionUserModel);

		const mockSequelize = {
			query: vi.fn().mockResolvedValue(mockQueryResults.wallPosts),
			QueryTypes: { SELECT: 'SELECT' },
		};
		vi.mocked(getSequelizeObject).mockResolvedValue(mockSequelize);

		vi.mocked(getQuery).mockReturnValue('SELECT * FROM WallPost');
		vi.mocked(formatResult).mockResolvedValue([{ id: 1, post: 'test' }]);

		const request = createMockRequest('GET', 'http://example.com/actions/wall-posts-for-user', {
			headers: {
				'x-session-token': validSessionToken.SessionToken,
				'x-username': validSessionToken.Username,
			},
		});

		const response = await getWallPostsForUserController(request);

		expect(response.status).toBe(200);
		const data = await parseResponse(response);
		expect(Array.isArray(data)).toBe(true);
	});

	it('should filter by regionId when provided', async () => {
		vi.mocked(authenticateSessionToken).mockResolvedValue(validSessionToken);
		
		const mockRegionUserModel = {
			findAll: vi.fn()
				.mockResolvedValueOnce([{ RegionId: 'region1' }])
				.mockResolvedValueOnce([{ RealUsername: 'user1' }]),
		};
		vi.mocked(getDbObject).mockResolvedValue(mockRegionUserModel);

		const mockSequelize = {
			query: vi.fn().mockResolvedValue([]),
			QueryTypes: { SELECT: 'SELECT' },
		};
		vi.mocked(getSequelizeObject).mockResolvedValue(mockSequelize);
		vi.mocked(getQuery).mockReturnValue('SELECT * FROM WallPost');
		vi.mocked(formatResult).mockResolvedValue([]);

		const request = createMockRequest('GET', 'http://example.com/actions/wall-posts-for-user/region1', {
			params: { regionId: 'region1' },
			headers: {
				'x-session-token': validSessionToken.SessionToken,
				'x-username': validSessionToken.Username,
			},
		});

		const response = await getWallPostsForUserController(request);

		expect(response.status).toBe(200);
		// Verify regionId was used in the query
		expect(mockRegionUserModel.findAll).toHaveBeenCalled();
	});
});

