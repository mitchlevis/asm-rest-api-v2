/**
 * Test helper utilities for creating mock objects and parsing responses
 */

/**
 * Create a mock Request object
 * @param {string} method - HTTP method (GET, POST, etc.)
 * @param {string} path - Request path/URL
 * @param {Object} options - Options for request
 * @param {Object} options.headers - Request headers
 * @param {Object} options.params - URL parameters
 * @param {Object} options.query - Query string parameters
 * @param {Object|string} options.body - Request body (object or JSON string)
 * @returns {Request} Mock Request object
 */
export function createMockRequest(method = 'GET', path = 'http://example.com', options = {}) {
	const url = new URL(path);
	
	// Add query parameters to URL if provided
	if (options.query) {
		Object.entries(options.query).forEach(([key, value]) => {
			if (value !== undefined && value !== null) {
				url.searchParams.append(key, String(value));
			}
		});
	}

	const requestOptions = {
		method,
		headers: options.headers || {},
	};

	// Add body if provided
	if (options.body !== undefined) {
		if (typeof options.body === 'string') {
			requestOptions.body = options.body;
		} else {
			requestOptions.body = JSON.stringify(options.body);
			if (!requestOptions.headers['Content-Type']) {
				requestOptions.headers['Content-Type'] = 'application/json';
			}
		}
	}

	const request = new Request(url.toString(), requestOptions);
	
	// Add params and query to request object (for itty-router compatibility)
	if (options.params) {
		request.params = options.params;
	}
	if (options.query) {
		request.query = options.query;
	}

	return request;
}

/**
 * Create mock Cloudflare environment object
 * @param {Object} overrides - Environment variable overrides
 * @returns {Object} Mock env object
 */
export function createMockEnv(overrides = {}) {
	return {
		DATABASE_HOST: 'test-host',
		DATABASE_PORT: '1433',
		DATABASE_NAME: 'test-db',
		DATABASE_USERNAME: 'test-user',
		DATABASE_PASSWORD: 'test-password',
		DATABASE_ENCRYPT: 'false',
		DATABASE_TRUST_CERT: 'true',
		RESPONSE_VALIDATION: 'strict',
		NODE_ENV: 'test',
		...overrides,
	};
}

/**
 * Create mock headers object
 * @param {Object} headers - Header key-value pairs
 * @returns {Headers} Headers object
 */
export function createMockHeaders(headers = {}) {
	const headersObj = new Headers();
	Object.entries(headers).forEach(([key, value]) => {
		headersObj.set(key, value);
	});
	return headersObj;
}

/**
 * Parse JSON response
 * @param {Response} response - Response object
 * @returns {Promise<Object>} Parsed JSON data
 */
export async function parseResponse(response) {
	const text = await response.text();
	try {
		return JSON.parse(text);
	} catch (error) {
		throw new Error(`Failed to parse response as JSON: ${text}`);
	}
}

/**
 * Create mock execution context (wrapper for cloudflare:test's createExecutionContext)
 * @returns {ExecutionContext} Mock execution context
 */
export function createMockExecutionContext() {
	// This is a wrapper - actual implementation uses cloudflare:test
	// Import and use from cloudflare:test in actual tests
	return null; // Placeholder - will be imported from cloudflare:test in tests
}

