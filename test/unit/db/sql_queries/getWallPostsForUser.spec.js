import { describe, it, expect } from 'vitest';
import { getQuery, formatResult } from '../../../../src/db/sql_queries/getWallPostsForUser.js';

describe('getWallPostsForUser SQL Query', () => {
	describe('getQuery', () => {
		it('should generate query with user array', () => {
			const userIds = ['user1', 'user2'];
			const query = getQuery(userIds, 'DESC', 10, 0);

			expect(query).toContain('SELECT');
			expect(query).toContain('user1');
			expect(query).toContain('user2');
			expect(query).toContain('DESC');
		});

		it('should use TOP clause when limit is set and offset is 0', () => {
			const userIds = ['user1'];
			const query = getQuery(userIds, 'DESC', 10, 0);

			expect(query).toContain('SELECT TOP (10)');
		});

		it('should use OFFSET and FETCH when offset is greater than 0', () => {
			const userIds = ['user1'];
			const query = getQuery(userIds, 'ASC', 10, 20);

			expect(query).toContain('OFFSET 20 ROWS');
			expect(query).toContain('FETCH NEXT 10 ROWS ONLY');
		});

		it('should handle unlimited results (limit = -1)', () => {
			const userIds = ['user1'];
			const query = getQuery(userIds, 'DESC', -1, 0);

			expect(query).not.toContain('TOP');
			expect(query).not.toContain('FETCH NEXT');
		});

		it('should handle different sort directions', () => {
			const userIds = ['user1'];
			const ascQuery = getQuery(userIds, 'ASC', 10, 0);
			const descQuery = getQuery(userIds, 'DESC', 10, 0);

			expect(ascQuery).toContain('ORDER BY wp.PostDate ASC');
			expect(descQuery).toContain('ORDER BY wp.PostDate DESC');
		});
	});

	describe('formatResult', () => {
		it('should parse JSON result from query', async () => {
			const mockResult = [
				{
					JSON_F52E2B61_18A1_11d1_B105_00805F49916B: JSON.stringify([{ id: 1, name: 'test' }]),
				},
			];

			const result = await formatResult(mockResult);

			expect(result).toEqual([{ id: 1, name: 'test' }]);
		});

		it('should return empty array for empty result', async () => {
			const result = await formatResult([]);

			expect(result).toEqual([]);
		});

		it('should handle null result', async () => {
			const result = await formatResult(null);

			expect(result).toEqual([]);
		});

		it('should handle multiple JSON rows', async () => {
			// formatResult concatenates JSON strings from multiple rows
			// MSSQL FOR JSON PATH returns valid JSON arrays that get concatenated
			const json1 = JSON.stringify([{ id: 1 }]);
			const json2 = JSON.stringify([{ id: 2 }]);
			const mockResult = [
				{
					[Object.keys({})[0] || 'JSON_F52E2B61_18A1_11d1_B105_00805F49916B']: json1,
				},
				{
					[Object.keys({})[0] || 'JSON_F52E2B61_18A1_11d1_B105_00805F49916B']: json2,
				},
			];

			// Note: formatResult concatenates strings, so '[{id:1}][{id:2}]' is invalid JSON
			// In practice, MSSQL returns a single JSON array, but if multiple rows,
			// they would need to be parsed and merged. This test documents current behavior.
			await expect(formatResult(mockResult)).rejects.toThrow();
		});
	});
});

