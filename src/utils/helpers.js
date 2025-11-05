import sequelizeAdapter from '../db/adapter.js';
import { MODELS, MODEL_ASSOCIATIONS } from '../db/models';
import { z } from 'zod';
import { v4 as uuidv4 } from 'uuid';
import crypto from 'crypto';

// Format a success response
// Supports two call patterns:
// 1. formatSuccessResponse(request, data, statusCode)
// 2. formatSuccessResponse(request, { data, statusCode, extraHeaders, keepDBAlive, responseSchema })
export const formatSuccessResponse = async (request, dataOrOptions, statusCodeParam = undefined) => {
console.log('formatSuccessResponse', request, dataOrOptions, statusCodeParam);
	const sequelize = await sequelizeAdapter.getSequelize(false);

	// Handle both call patterns
	let data, statusCode, extraHeaders, keepDBAlive, responseSchema;

	if (Array.isArray(dataOrOptions) || (typeof dataOrOptions !== 'object' && dataOrOptions !== undefined)) {
		// Pattern 1: formatSuccessResponse(request, data, statusCode)
		data = dataOrOptions;
		statusCode = statusCodeParam || 200;
		extraHeaders = {};
		keepDBAlive = false;
		responseSchema = undefined;
	} else {
		// Pattern 2: formatSuccessResponse(request, { data, statusCode, ... })
		data = dataOrOptions?.data;
		statusCode = dataOrOptions?.statusCode || 200;
		extraHeaders = dataOrOptions?.extraHeaders || {};
		keepDBAlive = dataOrOptions?.keepDBAlive || false;
		responseSchema = dataOrOptions?.responseSchema;
	}

	// Convert Headers object to plain object and merge with extraHeaders
	const headers = { ...extraHeaders };

	// Add CORS Headers - use specific origin when credentials are involved
	const origin = request.headers.get('Origin');
	headers['Access-Control-Allow-Origin'] = origin || '*';
	headers['Access-Control-Allow-Methods'] = 'GET, POST, PUT, DELETE, OPTIONS, PATCH';
	headers['Access-Control-Allow-Headers'] = 'Content-Type, Authorization';
	headers['Access-Control-Allow-Credentials'] = 'true';

	// Response validation (if schema provided)
	if (responseSchema) {
		const validationMode = process.env.RESPONSE_VALIDATION || 'disabled';

		try {
			responseSchema.parse(data);
		} catch (error) {
			if (error instanceof z.ZodError) {
				const errorMessage = formatZodError(error);
				const validationError = `Response validation failed: ${errorMessage}`;

				if (validationMode === 'strict') {
					// In strict mode (development/test), close connection and throw formatted error
					if(sequelize && !keepDBAlive){
						console.log('Closing Sequelize connection');
						await sequelize.close();
					}
					console.error(validationError);
					console.error('Response data:', JSON.stringify(data, null, 2));
					// Throw error object that formatErrorResponse can handle
					const validationErrorObj = {
						statusCode: 412,
						message: `Response validation failed: The API response does not match the expected schema. ${errorMessage}`,
					};
					return formatErrorResponse(request, validationErrorObj);
				} else if (validationMode === 'log') {
					// In log mode (production), log warning but continue
					console.warn(validationError);
					console.warn('Response data:', JSON.stringify(data, null, 2));
					// Continue with response
				}
				// If disabled, skip validation entirely
			}
		}
	}

	// Close the Sequelize connection before returning the response
	if(sequelize && !keepDBAlive){
		console.log('Closing Sequelize connection');
		await sequelize.close();
	}

	return Response.json(data, {
		status: statusCode,
		headers: headers,
	});
}

// Format an error response
export const formatErrorResponse = async (request, error) => {
console.log('formatErrorResponse', request, error);
	const sequelize = await sequelizeAdapter.getSequelize(false);

	let statusCode = 500;
  let errorObject = { statusCode: 500, message: 'Unknown Server Error' };

	if(sequelize){
		console.log('Closing Sequelize connection');
		await sequelize.close();
	}

	// Check if error is a custom error object
	if(error.statusCode){
		statusCode = error.statusCode;
		errorObject = error;
	}

	// Check if error is sequelize UniqueConstraintError
	if(error.name === 'SequelizeUniqueConstraintError'){
		statusCode = 400;
		errorObject = { statusCode: 400, message: 'SequelizeUniqueConstraintError - Duplicate Record' };
	}

	// Check if error is sequelize SequelizeDatabaseError
	if(error.name === 'SequelizeDatabaseError'){
		statusCode = 400;
		errorObject = { statusCode: 400, message: `SequelizeDatabaseError - ${error.parent}` };
	}

	// Check if error is sequelize ValidationError
	if(error.name === 'SequelizeValidationError'){
		statusCode = 400;
		errorObject = { statusCode: 400, message: `SequelizeValidationError - ${error.message}` };
	}

	// Check if error is sequelize EagerLoadingError
	if(error.name === 'SequelizeEagerLoadingError'){
		statusCode = 400;
		errorObject = { statusCode: 400, message: `SequelizeEagerLoadingError - ${error.message}` };
	}

	const headers = { 'Content-Type': 'application/json' };
	// Add CORS Headers - use specific origin when credentials are involved
	const origin = request.headers.get('Origin');
	headers['Access-Control-Allow-Origin'] = origin || '*';
	headers['Access-Control-Allow-Methods'] = 'GET, POST, PUT, DELETE, OPTIONS, PATCH';
	headers['Access-Control-Allow-Headers'] = 'Content-Type, Authorization';
	headers['Access-Control-Allow-Credentials'] = 'true';

	return Response.json(errorObject, {
		status: statusCode,
		headers: headers,
	});
}

export const throwError = async (statusCode, message) => {
  return Promise.reject({ statusCode, message });
}

export const authenticateSessionToken = async (request) => {

  // Look for x-session-token header and x-username header
  if(!request.headers.get('x-session-token') || request.headers.get('x-session-token') === ''){
    console.log('Missing x-session-token Header - Returning 401');
    await throwError(401, 'Unauthorized');
  }
  if(!request.headers.get('x-username') || request.headers.get('x-username') === ''){
    console.log('Missing x-username Header - Returning 401');
    await throwError(401, 'Unauthorized');
  }

  // Get session token and username from headers
  const session_token = request.headers.get('x-session-token');
  const username = request.headers.get('x-username');

  const userModel = await getDbObject('User');
  const sessionTokenModel = await getDbObject('SessionToken');

  const userPromise = userModel.findOne({ where: { Username: username } })
    .then(async(res) => {
      if(!res){
        console.log('User not found - Returning 401');
        await throwError(401, 'Unauthorized');
      }
      return res;
    })
    .catch(async (err) => {
      console.error(`Error finding user with username ${username}:`, err);
      await throwError(401, 'Unauthorized');
    })
  const sessionTokenPromise = sessionTokenModel.findOne({ where: { Username: username, SessionToken: session_token } })
    .then(async(res) => {
      if(!res){
        console.log('Session Token not found - Returning 401');
        await throwError(401, 'Unauthorized');
      }
      return res;
    })
    .catch(async (err) => {
      console.error(`Error finding token ${session_token}:`, err);
      await throwError(401, 'Unauthorized');
    })

  // Wait for both promises to resolve
  const [user, sessionToken] = await Promise.all([userPromise, sessionTokenPromise]);

  // Check if Username matches
  if(user.Username !== sessionToken.UserName){
    console.error(`Username provided in headers doesn't match the session token's username. Username Provided: ${username}| Session Token Provided: ${session_token} | Username: ${user.Username} | Session Token Username: ${sessionToken.UserName}`);
    await throwError(401, 'Unauthorized');
  }

  // Check if session token is expired - IssuanceDate + DurationDays < Now
  const now = new Date();
  const expirationDate = new Date(sessionToken.IssuanceDate);
  expirationDate.setDate(expirationDate.getDate() + sessionToken.DurationDays);
  if(now > expirationDate){
    await throwError(401, 'Session token expired');
  }

  //
  return sessionToken;
};

// Helper function to preprocess query parameters (handle empty strings, null, and invalid numbers)
const preprocessQueryData = (queryData) => {
	const processed = {};
	for (const [key, value] of Object.entries(queryData || {})) {
		// Convert empty strings and null to undefined so defaults apply
		if (value === '' || value === null) {
			processed[key] = undefined;
		} else if (typeof value === 'string') {
			// Check if this string would produce NaN when coerced to number
			const trimmed = value.trim();
			if (trimmed === '') {
				processed[key] = undefined;
			} else {
				// Test if it's a valid number (for fields that might be numbers)
				const num = Number(trimmed);
				// If it's not a valid number, keep original value (let Zod handle validation)
				// We'll catch NaN after Zod processes it
				processed[key] = value;
			}
		} else {
			processed[key] = value;
		}
	}
	return processed;
};

// Helper function to format Zod errors
const formatZodError = (error) => {
	if (error instanceof z.ZodError && error.errors && error.errors.length > 0) {
		// Extract just the message from each error object
		return error.errors.map(e => {
			const path = e.path && e.path.length > 0 ? `${e.path.join('.')}: ` : '';
			return `${path}${e.message}`;
		}).join(', ');
	}

	// If error.message is a JSON string, try to parse it and extract messages
	if (error.message && typeof error.message === 'string') {
		try {
			const parsed = JSON.parse(error.message);
			if (Array.isArray(parsed)) {
				return parsed.map(e => e.message || 'Validation failed').join(', ');
			}
		} catch {
			// Not JSON, return as-is
		}
	}

	return error.message || 'Validation failed';
};

// Function to validate parameters (path, query, body) against a Zod schema
export const validateIncomingParameters = async (request, parameters) => {
	const { path, query, body } = parameters;

	const validatedParameters = { path: null, query: null, body: null };

	// Validate Path Parameters
	if(path && Object.keys(path).length > 0){
		try {
			const pathSchema = z.object(path);
			const pathData = request.params || {};
			validatedParameters.path = pathSchema.parse(pathData);
		} catch(error){
			if(error instanceof z.ZodError){
				console.error('Path validation error:', error);
				const errorMessage = formatZodError(error);
				await throwError(400, `Invalid Path Parameters - ${errorMessage}`);
			}
			await throwError(400, `Invalid Path Parameters - ${error.message || 'Unknown error'}`);
		}
	}

	// Validate Query Parameters
	if(query && Object.keys(query).length > 0){
		try {
			const querySchema = z.object(query);
			const queryData = preprocessQueryData(request.query);

			// Parse and validate
			const result = querySchema.parse(queryData);

			// Post-process to handle any NaN values that might have slipped through coercion
			const cleanedResult = {};
			for (const [key, value] of Object.entries(result)) {
				if (typeof value === 'number' && isNaN(value)) {
					// NaN detected - get default from schema or use undefined
					const fieldSchema = querySchema.shape[key];
					if (fieldSchema) {
						// Try to get default value by parsing undefined
						try {
							cleanedResult[key] = fieldSchema.parse(undefined);
						} catch {
							// If no default, keep undefined
							cleanedResult[key] = undefined;
						}
					} else {
						cleanedResult[key] = undefined;
					}
				} else {
					cleanedResult[key] = value;
				}
			}

			validatedParameters.query = cleanedResult;
		} catch(error){
			if(error instanceof z.ZodError){
				console.error('Query validation error:', error);
				const errorMessage = formatZodError(error);
				await throwError(400, `Invalid Query Parameters - ${errorMessage}`);
			}
			await throwError(400, `Invalid Query Parameters - ${error.message || 'Unknown error'}`);
		}
	}

	// Validate Body Parameters
	if(body && Object.keys(body).length > 0){
		try {
			const bodySchema = z.object(body);
			let bodyData = await request.json()

			validatedParameters.body = bodySchema.parse(bodyData);
		} catch(error){
			if(error instanceof z.ZodError){
				console.error('Body validation error:', error);
				const errorMessage = formatZodError(error);
				await throwError(400, `Invalid Body Parameters - ${errorMessage}`);
			}
			await throwError(400, `Invalid Body Parameters - ${error.message || 'Unknown error'}`);
		}
	}

	return { path: validatedParameters.path, query: validatedParameters.query, body: validatedParameters.body };
};

// Get a Sequelize object
export const getSequelizeObject = async () => {
  const sequelize = await sequelizeAdapter.getSequelize();
  return sequelize;
}

// Get a Sequelize transaction
export const getSequelizeTransaction = async () => {
  const transaction = await sequelizeAdapter.getSequelizeTransaction();
  return transaction;
};

// Get a Model
export const getDbObject = async (resourceName, includeAssociations = true) => {
  // Dynamically import the model
  const model = MODELS[resourceName];

  const sequelize = await sequelizeAdapter.getSequelize();

  const dbObject = sequelize.define(resourceName, model, { tableName: resourceName, timestamps: false});

  if(includeAssociations === true){
    /*
      Dynamically import the associations
    */

    // Belongs To
    if(MODEL_ASSOCIATIONS[resourceName]?.belongsTo && MODEL_ASSOCIATIONS[resourceName]?.belongsTo.length > 0){

      for(const belongsTo of MODEL_ASSOCIATIONS[resourceName]?.belongsTo){
        const belongsToResourceName = belongsTo.modelName;
        const belongsToModelObject = MODELS[belongsToResourceName];
        const belongsToModel = sequelize.define(belongsToResourceName, belongsToModelObject, { tableName: belongsToResourceName, timestamps: false});
        const belongsToOptions = { foreignKey: belongsTo.foreignKey, targetKey: belongsTo.targetKey, as: belongsTo.as, scope: belongsTo.scope };
        // Map both old option names to the new Sequelize v7 option name
        if (belongsTo.constraints !== undefined) {
          belongsToOptions.foreignKeyConstraints = belongsTo.constraints;
        } else if (belongsTo.foreignKeyConstraint !== undefined) {
          belongsToOptions.foreignKeyConstraints = belongsTo.foreignKeyConstraint;
        }
        dbObject.belongsTo(belongsToModel, belongsToOptions);
      }
    }
    // Has One
    if(MODEL_ASSOCIATIONS[resourceName]?.hasOne && MODEL_ASSOCIATIONS[resourceName]?.hasOne.length > 0){

      for(const hasOne of MODEL_ASSOCIATIONS[resourceName]?.hasOne){
        const hasOneResourceName = hasOne.modelName;
        const hasOneModelObject = MODELS[hasOneResourceName];
        const hasOneModel = sequelize.define(hasOneResourceName, hasOneModelObject, { tableName: hasOneResourceName, timestamps: false});
        const hasOneOptions = { foreignKey: hasOne.foreignKey, sourceKey: hasOne.sourceKey, as: hasOne.as, scope: hasOne.scope };
        if (hasOne.constraints !== undefined) {
          hasOneOptions.foreignKeyConstraints = hasOne.constraints;
        }
        dbObject.hasOne(hasOneModel, hasOneOptions);
      }
    }
    // Has Many
    if(MODEL_ASSOCIATIONS[resourceName]?.hasMany && MODEL_ASSOCIATIONS[resourceName]?.hasMany.length > 0){

      for(const hasMany of MODEL_ASSOCIATIONS[resourceName]?.hasMany){
        const hasManyResourceName = hasMany.modelName;
        const hasManyModelObject = MODELS[hasManyResourceName];
        const hasManyModel = sequelize.define(hasManyResourceName, hasManyModelObject, { tableName: hasManyResourceName, timestamps: false});
        const hasManyOptions = { foreignKey: hasMany.foreignKey, sourceKey: hasMany.sourceKey, targetKey: hasMany.targetKey, as: hasMany.as, scope: hasMany.scope };
        if (hasMany.constraints !== undefined) {
          hasManyOptions.foreignKeyConstraints = hasMany.constraints;
        }
        dbObject.hasMany(hasManyModel, hasManyOptions);
      }
    }
  }

  return dbObject;
}

/**
 * Encrypts a string using SHA256 with salt - equivalent to VB.NET EncryptSHA256Managed function
 * @param {string} SALT - The salt value to use
 * @param {string} clearString - The string to encrypt
 * @returns {string} Base64 encoded SHA256 hash
 */
export const encryptSHA256Managed = (SALT, clearString) => {
  // Create the salted string (Salt + ClearString) - equivalent to VB.NET UnicodeEncoding.GetBytes(Salt & ClearString)
  const saltedString = SALT + clearString;

  // Convert to UTF-16LE encoding (equivalent to VB.NET UnicodeEncoding)
  const utf16Buffer = Buffer.from(saltedString, 'utf16le');

  // Create SHA256 hash - equivalent to VB.NET SHA256Managed.ComputeHash
  const hash = crypto.createHash('sha256');
  hash.update(utf16Buffer);
  const hashBytes = hash.digest();

  // Convert to Base64 string - equivalent to VB.NET Convert.ToBase64String
  return hashBytes.toString('base64');
}

// Convert PascalCase to camelCase (recursively)
export const convertPropertiesToCamelCase = (obj) => {
  // Handle null or undefined
  if (obj === null || obj === undefined) {
    return obj;
  }

  // Handle arrays - recursively convert each element
  if (Array.isArray(obj)) {
    return obj.map(item => convertPropertiesToCamelCase(item));
  }

  // Handle non-object primitives (string, number, boolean, etc.)
  if (typeof obj !== 'object') {
    return obj;
  }

  // Handle Date objects and other special object types
  if (obj instanceof Date || obj instanceof RegExp || obj instanceof Buffer) {
    return obj;
  }

  // Handle plain objects - convert keys and recursively convert values
  const result = {};
  for (const key in obj) {
    if (obj.hasOwnProperty(key)) {
      // Convert PascalCase to camelCase: first letter lowercase, rest stays the same
      const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
      // Recursively convert the value
      result[camelKey] = convertPropertiesToCamelCase(obj[key]);
    }
  }

  return result;
};
