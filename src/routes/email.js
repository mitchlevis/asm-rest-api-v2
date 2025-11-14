/**
 * Controllers for Email routes
 */

import sendTextEmailController from '../controllers/send-email/text/index.js';

export function setupEmailRoutes(router) {
	// POST /email/text - Send a text email
	router.post('/send-email', (request) => {
		return sendTextEmailController(request);
	});
}

