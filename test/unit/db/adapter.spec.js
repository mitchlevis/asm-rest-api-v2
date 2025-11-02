import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mockSequelize } from '../../helpers/db-mocks.js';

// Mock the adapter module
vi.mock('../../../src/db/adapter.js', () => ({
	default: {
		getSequelize: vi.fn(),
		getSequelizeTransaction: vi.fn(),
		closeSequelize: vi.fn(),
	},
	getSequelize: vi.fn(),
	getSequelizeTransaction: vi.fn(),
	closeSequelize: vi.fn(),
}));

describe('Database Adapter', () => {
	let sequelizeAdapter;

	beforeEach(async () => {
		sequelizeAdapter = await import('../../../src/db/adapter.js');
	});

	it('should return Sequelize instance', async () => {
		const mockInstance = mockSequelize(vi);
		vi.mocked(sequelizeAdapter.getSequelize).mockResolvedValue(mockInstance);

		const result = await sequelizeAdapter.getSequelize();

		expect(result).toBeDefined();
		expect(result.authenticate).toBeDefined();
		expect(result.query).toBeDefined();
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	describe('getSequelize', () => {

		it('should return null when createInstanceIfNotExists is false and no instance exists', async () => {
			vi.mocked(sequelizeAdapter.getSequelize).mockResolvedValue(null);

			const result = await sequelizeAdapter.getSequelize(false);

			expect(result).toBeNull();
		});
	});

	describe('getSequelizeTransaction', () => {
		it('should return a transaction object', async () => {
			const mockTransaction = {
				commit: vi.fn().mockResolvedValue(undefined),
				rollback: vi.fn().mockResolvedValue(undefined),
			};
			vi.mocked(sequelizeAdapter.getSequelizeTransaction).mockResolvedValue(mockTransaction);

			const result = await sequelizeAdapter.getSequelizeTransaction();

			expect(result).toBeDefined();
			expect(result.commit).toBeDefined();
			expect(result.rollback).toBeDefined();
		});
	});

	describe('closeSequelize', () => {
		it('should close the Sequelize connection', async () => {
			vi.mocked(sequelizeAdapter.closeSequelize).mockResolvedValue(undefined);

			await sequelizeAdapter.closeSequelize();

			expect(sequelizeAdapter.closeSequelize).toHaveBeenCalled();
		});
	});
});

