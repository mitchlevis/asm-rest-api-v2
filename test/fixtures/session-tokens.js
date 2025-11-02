/**
 * Session token fixtures for testing
 */

const now = new Date();
const futureDate = new Date(now);
futureDate.setDate(futureDate.getDate() + 7); // 7 days from now

const pastDate = new Date(now);
pastDate.setDate(pastDate.getDate() - 1); // 1 day ago

export const validSessionToken = {
	Username: 'testuser',
	SessionToken: 'valid-token-123',
	UserName: 'testuser', // Note: different casing in DB
	IssuanceDate: now.toISOString(),
	DurationDays: 7,
};

export const expiredSessionToken = {
	Username: 'testuser',
	SessionToken: 'expired-token-456',
	UserName: 'testuser',
	IssuanceDate: pastDate.toISOString(),
	DurationDays: 1, // Already expired
};

export const invalidSessionToken = {
	Username: 'testuser',
	SessionToken: 'invalid-token-789',
	UserName: 'differentuser', // Mismatched username
	IssuanceDate: now.toISOString(),
	DurationDays: 7,
};

export const sessionTokens = [
	validSessionToken,
	expiredSessionToken,
	{
		Username: 'user2',
		SessionToken: 'token-user2',
		UserName: 'user2',
		IssuanceDate: now.toISOString(),
		DurationDays: 30,
	},
];

export default {
	validSessionToken,
	expiredSessionToken,
	invalidSessionToken,
	sessionTokens,
};

