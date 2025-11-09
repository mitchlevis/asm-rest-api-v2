import { Sequelize } from '@sequelize/core';
import { MsSqlDialect } from '@sequelize/mssql';
import * as tedious from 'tedious';
import KV from '../services/KV.js';

let sequelize = null;
let env = null; // Store env reference for use across adapter functions

const ipv4Regex = /^(?:\d{1,3}\.){3}\d{1,3}$/;

/**
 * Generate KV key for DNS cache entry
 * @param {string} hostname - The hostname to cache
 * @returns {string} KV key
 */
function getDNSCacheKey(hostname) {
	return `dns:${hostname}`;
}

/**
 * Resolve hostname to IPv4 address using Cloudflare DoH
 * Caches results in KV storage with TTL
 * @param {string} hostname - The hostname to resolve
 * @returns {Promise<{address: string, ttl: number}>} Resolved IPv4 address and TTL
 */
async function resolveIPv4Address(hostname) {
	if (!hostname || ipv4Regex.test(hostname)) {
		return { address: hostname, ttl: 0 };
	}

	console.log('Resolving IPv4 address for hostname:', hostname);

	// Check KV cache first
	const cacheKey = getDNSCacheKey(hostname);
	try {
		const cached = await KV.get(cacheKey, env, { type: 'json' });
		if (cached && cached.address) {
			console.log('DNS cache hit for hostname:', hostname, '->', cached.address);
			return { address: cached.address, ttl: cached.ttl || 0 };
		}
	} catch (error) {
		// If KV is not available or error occurs, log and continue to DNS lookup
		console.warn('KV cache lookup failed, falling back to DNS:', error.message);
	}
	console.log('No DNS cache hit for hostname:', hostname, '->', 'Resolving DNS...');

	// Use Cloudflare DoH to resolve A records; Workers can always use fetch
	// Add timeout using AbortController to prevent hanging
	const controller = new AbortController();
	const timeoutId = setTimeout(() => controller.abort(), 10000); // 10 second timeout

	try {
		const url = `https://cloudflare-dns.com/dns-query?name=${encodeURIComponent(hostname)}&type=A`;
		const res = await fetch(url, {
			headers: { 'accept': 'application/dns-json' },
			signal: controller.signal
		});
		clearTimeout(timeoutId);

		if (!res.ok) {
			throw new Error(`DNS query failed for ${hostname}: HTTP ${res.status}`);
		}
		const data = await res.json();
		const answers = Array.isArray(data?.Answer) ? data.Answer : [];
		const firstA = answers.find(a => a.type === 1 && a.data && ipv4Regex.test(a.data));
		if (!firstA) {
			throw new Error(`No IPv4 A record found for ${hostname}`);
		}

		console.log('Resolved IPv4 address for hostname:', hostname, firstA.data, answers[0]?.TTL);

		// Cap TTL to a reasonable upper bound to avoid long stale entries
		const ttlSeconds = Math.max(30, Math.min(answers[0]?.TTL ?? 60, 300));

		// Store in KV cache with expiration
		try {
			console.log('Storing DNS resolution for hostname:', hostname, 'in KV cache with TTL:', ttlSeconds, 'seconds under key:', cacheKey);
			await KV.put(
				cacheKey,
				{ address: firstA.data, resolvedAt: Date.now() },
				env,
				{ expirationTtl: ttlSeconds }
			);
			console.log('Stored DNS resolution in KV cache with TTL:', ttlSeconds, 'seconds');
		} catch (error) {
			// If KV write fails, log warning but continue
			console.warn('Failed to store DNS resolution in KV cache:', error.message);
		}

		return { address: firstA.data, ttl: ttlSeconds };
	} catch (fetchError) {
		clearTimeout(timeoutId);
		if (fetchError.name === 'AbortError' || fetchError.message?.includes('aborted')) {
			throw new Error(`DNS resolution timeout for ${hostname}: Request took longer than 10 seconds`);
		}
		throw new Error(`DNS resolution failed for ${hostname}: ${fetchError.message}`);
	}
}

/**
 * Initialize the adapter with environment variables
 * Must be called before using any adapter functions
 * @param {Object} environment - Cloudflare environment variables
 */
export const initialize = (environment) => {
	env = environment || process.env;
	console.log('Database adapter initialized with environment');
	// Log environment info for debugging (without sensitive data)
	console.log('Environment check:', {
		hasDATABASE_HOST: !!env.DATABASE_HOST,
		hasDATABASE_NAME: !!env.DATABASE_NAME,
		hasDATABASE_USERNAME: !!env.DATABASE_USERNAME,
		hasDATABASE_PASSWORD: !!env.DATABASE_PASSWORD,
		DATABASE_HOST: env.DATABASE_HOST,
		DATABASE_PORT: env.DATABASE_PORT,
		DATABASE_NAME: env.DATABASE_NAME,
		DATABASE_USERNAME: env.DATABASE_USERNAME,
		ENVIRONMENT: env.ENVIRONMENT || 'unknown',
	});
};

/**
 * Load and configure Sequelize instance for MSSQL
 * Adapted from Lambda version for Cloudflare Workers
 * @returns {Promise<Sequelize>} Configured Sequelize instance
 */
const loadSequelize = async () => {
	if (!env) {
		// Fallback to process.env if not initialized
		env = process.env;
		console.warn('Adapter not initialized, using process.env as fallback');
	}

	// Validate required environment variables before attempting connection
	const requiredVars = ['DATABASE_HOST', 'DATABASE_NAME', 'DATABASE_USERNAME', 'DATABASE_PASSWORD'];
	const missingVars = requiredVars.filter(varName => !env[varName]);

	if (missingVars.length > 0) {
		const errorMessage = `Missing required database environment variables: ${missingVars.join(', ')}. ` +
			`For Cloudflare Workers, ensure secrets are configured in the dashboard for the production environment. ` +
			`Set secrets using: wrangler secret put ${missingVars.find(v => v.includes('PASSWORD')) || 'SECRET_NAME'} --env production`;
		console.error(errorMessage);
		throw new Error(errorMessage);
	}

	const config = {
		host: env.DATABASE_HOST,
		database: env.DATABASE_NAME,
		user: env.DATABASE_USERNAME,
		password: env.DATABASE_PASSWORD,
		port: env.DATABASE_PORT || 1433, // Default MSSQL port
		encrypt: env.DATABASE_ENCRYPT !== 'false', // Default to true for security
		trustServerCertificate: env.DATABASE_TRUST_CERT === 'true', // For self-signed certs
	};

	// Resolve hostname to IPv4 explicitly (Workers lack Node dns; also avoid AAAA issues)
	let resolvedAddress;
	let targetServer;
	try {
		const resolution = await resolveIPv4Address(config.host);
		resolvedAddress = resolution.address;
		targetServer = resolvedAddress || config.host;
		console.log(`Connecting to MSSQL server: ${config.host} -> ${targetServer}:${config.port}, database: ${config.database}`);
	} catch (dnsError) {
		console.error(`DNS resolution failed for ${config.host}:`, dnsError.message);
		throw new Error(`Failed to resolve database hostname ${config.host}: ${dnsError.message}`);
	}

	const sequelizeInstance = new Sequelize({
		dialect: MsSqlDialect,
		tediousModule: tedious,
		server: targetServer,
		port: parseInt(config.port),
		database: config.database,
		authentication: {
			type: 'default',
			options: {
				userName: config.user,
				password: config.password,
			},
		},
		encrypt: config.encrypt,
		trustServerCertificate: config.trustServerCertificate,
		connectTimeout: 30000, // 30 seconds connection timeout
		requestTimeout: 300000, // 5 minutes request timeout
		// Note: MSSQL always returns dates as UTC, timezone option is not supported
		pool: {
			// Cloudflare Workers handle concurrency differently than Lambda
			// Workers can process multiple requests concurrently, so adjust max accordingly
			max: 5, // Adjust based on your expected concurrency
			min: 0, // Start with 0 connections (connection on demand)
			acquire: 30000, // Connection acquisition timeout (30 seconds)
			idle: 10000, // Idle connection timeout
		},
		logging: env.NODE_ENV === 'development' ? console.log : false,
		// Default model options - applied to all models unless overridden
		define: {
			timestamps: false, // MSSQL tables typically don't use Sequelize timestamps
			freezeTableName: true, // Prevent Sequelize from pluralizing/changing table names
			schema: 'dbo', // Default schema for all models
		},
	});

	try {
		console.log('Attempting database connection...');
		const startTime = Date.now();
		await sequelizeInstance.authenticate();
		const connectionTime = Date.now() - startTime;
		console.log(`Database connection established successfully in ${connectionTime}ms`);
	} catch (authError) {
		const errorMessage = authError.message || String(authError);
		const isConnectionError = errorMessage.includes('Could not connect') ||
		                          errorMessage.includes('sequence') ||
		                          errorMessage.includes('ECONNREFUSED') ||
		                          errorMessage.includes('ETIMEDOUT');

		console.error('Database connection failed:', {
			host: config.host,
			targetServer,
			port: config.port,
			database: config.database,
			username: config.user,
			hasPassword: !!config.password,
			error: errorMessage,
			errorType: isConnectionError ? 'NETWORK_CONNECTION' : 'AUTHENTICATION',
		});

		// Close the instance if connection fails
		try {
			await sequelizeInstance.close();
		} catch (closeError) {
			// Ignore close errors
		}

		if (isConnectionError) {
			throw new Error(
				`Database connection failed: Unable to establish TCP connection to ${targetServer}:${config.port}. ` +
				`This is likely a network/firewall issue. Possible causes:\n` +
				`1. Database server firewall/IP allowlisting may be blocking Cloudflare Workers IP ranges\n` +
				`2. Network routing differences between manual and automatic deployments\n` +
				`3. Database server may not be accessible from Cloudflare's network\n` +
				`Solution: Check database server firewall rules and ensure Cloudflare IP ranges are allowed. ` +
				`Consider enabling Smart Placement in wrangler.jsonc to route workers closer to your database.`
			);
		} else {
			throw new Error(`Database authentication failed: ${errorMessage}. Check that DATABASE_PASSWORD secret is configured correctly for this environment.`);
		}
	}
	return sequelizeInstance;
};

/**
 * Get or create Sequelize instance
 * Cloudflare Workers can reuse connections across requests
 * @param {boolean} createInstanceIfNotExists - Whether to create instance if it doesn't exist
 * @returns {Promise<Sequelize>} Sequelize instance
 */
export const getSequelize = async (createInstanceIfNotExists = true) => {
	if (!env) {
		// Fallback to process.env if not initialized
		env = process.env;
		console.warn('Adapter not initialized, using process.env as fallback');
	}

	// Useful to simply see if the connection is already established or not without creating a new instance
	if(!sequelize && !createInstanceIfNotExists){
		return null;
	}

	if (!sequelize) {
		console.log('Creating new sequelize connection');
		sequelize = await loadSequelize();
	} else {
		console.log('Reusing existing sequelize connection');
		// Test connection to ensure it's still alive
		try {
			await sequelize.authenticate();
		} catch (error) {
			console.log('Connection lost, recreating...');
			sequelize = await loadSequelize();
		}
	}
	return sequelize;
};

/**
 * Get a Sequelize transaction
 * @returns {Promise<Transaction>} Sequelize transaction
 */
export const getSequelizeTransaction = async () => {
	const sequelizeInstance = await getSequelize();
	console.log('Creating new sequelize transaction');
	return sequelizeInstance.transaction();
};

/**
 * Close Sequelize connection
 * Useful for cleanup or testing
 */
export const closeSequelize = async () => {
	if (sequelize) {
		console.log('Closing sequelize connection');
		await sequelize.close();
		sequelize = null;
	}
};

export default {
	initialize,
	getSequelize,
	getSequelizeTransaction,
	closeSequelize,
};

