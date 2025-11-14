/**
 * Controllers for S3 routes
 */

import getJsonController from '../controllers/s3/getJson/index.js';
import putJsonController from '../controllers/s3/putJson/index.js';

// Middleware to extract the catch-all key parameter from the URL
function extractKeyParam(request) {
	const url = new URL(request.url);
	const pathname = url.pathname;
	// Extract everything after '/s3/json/'
	let key = pathname.replace(/^\/s3\/json\//, '');
	// Decode URL-encoded characters (e.g., %2F becomes /)
	try {
		key = decodeURIComponent(key);
	} catch (e) {
		// If decoding fails, use the original key (might already be decoded)
		console.warn(`Failed to decode key: ${key}`, e);
	}
	console.log(`Extracted key from pathname "${pathname}": "${key}"`);
	// Add the key to request.params so controllers can access it
	if (!request.params) {
		request.params = {};
	}
	request.params.key = key;
}

export function setupS3Routes(router) {
	// GET /s3/json/* - Get a JSON object from S3 (supports nested paths)
	router.get('/s3/json/*', (request) => {
		extractKeyParam(request);
		return getJsonController(request);
	});

	// PUT /s3/json/* - Put a JSON object into S3 (supports nested paths)
	router.put('/s3/json/*', (request) => {
		extractKeyParam(request);
		return putJsonController(request);
	});
}

