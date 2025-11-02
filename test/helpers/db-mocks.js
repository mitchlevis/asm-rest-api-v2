/**
 * Database mocking utilities for tests
 * Note: These functions return mock objects that should be used with vi.fn() in tests
 */

/**
 * Create a mock Sequelize instance
 * @param {Object} vi - Vitest vi object (from vitest)
 * @param {Object} options - Mock options
 * @returns {Object} Mock Sequelize instance
 */
export function mockSequelize(vi, options = {}) {
	const mockQueryResults = options.queryResults || [];
	const mockAuthenticate = options.authenticate || (async () => true);
	const shouldFailAuthenticate = options.shouldFailAuthenticate || false;

	return {
		authenticate: vi.fn().mockImplementation(async () => {
			if (shouldFailAuthenticate) {
				throw new Error('Database connection failed');
			}
			return mockAuthenticate();
		}),
		query: vi.fn().mockResolvedValue(mockQueryResults),
		close: vi.fn().mockResolvedValue(undefined),
		define: vi.fn(),
		transaction: vi.fn().mockResolvedValue({
			commit: vi.fn().mockResolvedValue(undefined),
			rollback: vi.fn().mockResolvedValue(undefined),
		}),
		QueryTypes: {
			SELECT: 'SELECT',
		},
	};
}

/**
 * Create a mock Sequelize model
 * @param {Object} vi - Vitest vi object (from vitest)
 * @param {string} modelName - Name of the model
 * @param {Object} methods - Methods to mock (findOne, findAll, etc.)
 * @returns {Object} Mock model
 */
export function mockModel(vi, modelName, methods = {}) {
	const defaultMethods = {
		findOne: vi.fn().mockResolvedValue(null),
		findAll: vi.fn().mockResolvedValue([]),
		create: vi.fn().mockResolvedValue({}),
		update: vi.fn().mockResolvedValue([0]),
		destroy: vi.fn().mockResolvedValue(0),
		...methods,
	};

	return {
		name: modelName,
		...defaultMethods,
	};
}

/**
 * Create mock query results
 * @param {Array} data - Array of result objects
 * @returns {Array} Mock query results
 */
export function mockQueryResults(data = []) {
	return data;
}

/**
 * Reset all database mocks
 * Note: vi is available globally in Vitest tests, so this is just a convenience function
 */
export function resetDbMocks() {
	// This would be called with vi.clearAllMocks() in actual tests
	// Keeping as a placeholder for consistency
}

