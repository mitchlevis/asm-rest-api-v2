import { Sequelize } from '@sequelize/core';
import { MsSqlDialect } from '@sequelize/mssql';
import * as tedious from 'tedious';

let sequelize = null;

/**
 * Load and configure Sequelize instance for MSSQL
 * Adapted from Lambda version for Cloudflare Workers
 * @param {Object} env - Cloudflare environment variables
 * @returns {Promise<Sequelize>} Configured Sequelize instance
 */
const loadSequelize = async (env) => {
	const config = {
		host: env.DATABASE_HOST,
		database: env.DATABASE_NAME,
		user: env.DATABASE_USERNAME,
		password: env.DATABASE_PASSWORD,
		port: env.DATABASE_PORT || 1433, // Default MSSQL port
		encrypt: env.DATABASE_ENCRYPT !== 'false', // Default to true for security
		trustServerCertificate: env.DATABASE_TRUST_CERT === 'true', // For self-signed certs
	};

	console.log(`Connecting to MSSQL server: ${config.host}:${config.port}, database: ${config.database}`);

	const sequelizeInstance = new Sequelize({
		dialect: MsSqlDialect,
		tediousModule: tedious,
		server: config.host,
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

	await sequelizeInstance.authenticate();
	console.log('Database connection established successfully');
	return sequelizeInstance;
};

/**
 * Get or create Sequelize instance
 * Cloudflare Workers can reuse connections across requests
 * @param {Object} env - Cloudflare environment variables
 * @returns {Promise<Sequelize>} Sequelize instance
 */
export const getSequelize = async (createInstanceIfNotExists = true) => {
	// Useful to simply see if the connection is already established or not without creating a new instance
	if(!sequelize && !createInstanceIfNotExists){
		return null;
	}

	if (!sequelize) {
		console.log('Creating new sequelize connection');
		sequelize = await loadSequelize(process.env);
	} else {
		console.log('Reusing existing sequelize connection');
		// Test connection to ensure it's still alive
		try {
			await sequelize.authenticate();
		} catch (error) {
			console.log('Connection lost, recreating...');
			sequelize = await loadSequelize(process.env);
		}
	}
	return sequelize;
};

/**
 * Get a Sequelize transaction
 * @param {Object} env - Cloudflare environment variables
 * @returns {Promise<Transaction>} Sequelize transaction
 */
export const getSequelizeTransaction = async (env) => {
	const sequelizeInstance = await getSequelize(env);
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
	getSequelize,
	getSequelizeTransaction,
	closeSequelize,
};

