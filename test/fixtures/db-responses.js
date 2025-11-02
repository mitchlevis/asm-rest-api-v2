/**
 * Database response fixtures for testing
 */

import { users } from './users.js';
import { sessionTokens } from './session-tokens.js';

// RegionUser fixtures
export const regionUsers = [
	{
		RegionId: 'region1',
		RealUsername: 'testuser',
	},
	{
		RegionId: 'region1',
		RealUsername: 'user2',
	},
	{
		RegionId: 'region2',
		RealUsername: 'testuser',
	},
];

// Wall post fixtures
export const wallPosts = [
	{
		UserId: 'testuser',
		FirstName: 'Test',
		LastName: 'User',
		PhotoId: 'photo123',
		WallPostId: 'post1',
		Post: 'This is a test post',
		Link: null,
		PostDate: new Date().toISOString(),
		PostType: 'text',
		Tags: [
			{
				UserId: 'testuser',
				FirstName: 'Test',
				LastName: 'User',
				PhotoId: 'photo123',
				WallPostId: 'post1',
				TaggedId: 'user2',
			},
		],
		Likes: [
			{
				UserId: 'testuser',
				FirstName: 'Test',
				LastName: 'User',
				PhotoId: 'photo123',
				WallPostId: 'post1',
				LikerId: 'user2',
			},
		],
		Comments: [
			{
				UserId: 'testuser',
				FirstName: 'Test',
				LastName: 'User',
				PhotoId: 'photo123',
				WallPostId: 'post1',
				CommentId: 'comment1',
				Comment: 'This is a comment',
				CommenterId: 'user2',
				CommentDate: new Date().toISOString(),
				Likes: [],
				Comments: [],
			},
		],
	},
];

// Mock query results (as returned by Sequelize query)
export const mockQueryResults = {
	wallPosts: [
		{
			[Object.keys({})[0] || 'JSON_F52E2B61-18A1-11d1-B105-00805F49916B']: JSON.stringify(wallPosts),
		},
	],
	empty: [],
	regionUsers: regionUsers.map((ru) => ({ RegionId: ru.RegionId })),
};

export default {
	regionUsers,
	wallPosts,
	mockQueryResults,
	users,
	sessionTokens,
};

