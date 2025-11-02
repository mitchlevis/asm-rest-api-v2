/**
 * User fixtures for testing
 */

export const validUser = {
	Username: 'testuser',
	FirstName: 'Test',
	LastName: 'User',
	PhotoId: 'photo123',
	Email: 'test@example.com',
};

export const invalidUser = {
	Username: 'invaliduser',
	// Missing required fields
};

export const users = [
	validUser,
	{
		Username: 'user2',
		FirstName: 'User',
		LastName: 'Two',
		PhotoId: null,
		Email: 'user2@example.com',
	},
	{
		Username: 'user3',
		FirstName: 'User',
		LastName: 'Three',
		PhotoId: 'photo456',
		Email: 'user3@example.com',
	},
];

export default {
	validUser,
	invalidUser,
	users,
};

