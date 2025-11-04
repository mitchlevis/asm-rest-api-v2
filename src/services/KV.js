/**
 * Cloudflare KV Storage Service
 * Provides a simple interface for interacting with Cloudflare KV Storage
 *
 * @module services/KV
 */

/**
 * Store a value in KV with optional expiration
 * @param {string} key - The key to store the value under
 * @param {string} value - The value to store (will be JSON stringified if object)
 * @param {Object} env - Cloudflare environment with KV binding
 * @param {Object} options - Optional parameters
 * @param {number} options.expirationTtl - Time to live in seconds
 * @returns {Promise<void>}
 */
export async function put(key, value, env, options = {}) {
	if (!env?.API_KV) {
		throw new Error('KV binding API_KV not found in env');
	}

	const valueToStore = typeof value === 'string' ? value : JSON.stringify(value);
	const putOptions = {};

	if (options.expirationTtl) {
		putOptions.expirationTtl = options.expirationTtl;
	}

	await env.API_KV.put(key, valueToStore, putOptions);
}

/**
 * Retrieve a value from KV
 * @param {string} key - The key to retrieve
 * @param {Object} env - Cloudflare environment with KV binding
 * @param {Object} options - Optional parameters
 * @param {string} options.type - Expected type: 'text', 'json', 'arrayBuffer', 'stream'
 * @returns {Promise<string|Object|null>} The stored value or null if not found
 */
export async function get(key, env, options = {}) {
	if (!env?.API_KV) {
		throw new Error('KV binding API_KV not found in env');
	}

	const type = options.type || 'text';
	const value = await env.API_KV.get(key, { type });

	if (value === null) {
		return null;
	}

	// If type is json, KV already parses it, otherwise return as-is
	return value;
}

/**
 * Delete a value from KV
 * @param {string} key - The key to delete
 * @param {Object} env - Cloudflare environment with KV binding
 * @returns {Promise<void>}
 */
export async function del(key, env) {
	if (!env?.API_KV) {
		throw new Error('KV binding API_KV not found in env');
	}

	await env.API_KV.delete(key);
}

/**
 * List all keys in the KV namespace (with optional prefix)
 * @param {Object} env - Cloudflare environment with KV binding
 * @param {Object} options - Optional parameters
 * @param {string} options.prefix - Prefix to filter keys
 * @param {number} options.limit - Maximum number of keys to return
 * @returns {Promise<{keys: Array, list_complete: boolean}>}
 */
export async function list(env, options = {}) {
	if (!env?.API_KV) {
		throw new Error('KV binding API_KV not found in env');
	}

	return await env.API_KV.list(options);
}

export default {
	put,
	get,
	delete: del,
	list,
};

