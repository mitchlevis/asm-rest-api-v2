import Sequelize from '@sequelize/core';
import sequelizeAdapter from '../db/adapter.js';
import { MODELS, MODEL_ASSOCIATIONS } from '../db/models';
import operatorsAliases from './sequelize-aliases.js';
import { z } from 'zod';
import { v4 as uuidv4 } from 'uuid';
import crypto from 'crypto';
import { usePosthog } from '../services/posthog.js';

// Format a success response
// Supports two call patterns:
// 1. formatSuccessResponse(request, data, statusCode)
// 2. formatSuccessResponse(request, { data, statusCode, extraHeaders, responseSchema })
export const formatSuccessResponse = async (request, dataOrOptions, statusCodeParam = undefined) => {
// console.log('formatSuccessResponse', request, dataOrOptions, statusCodeParam);

	// Handle both call patterns
	let data, statusCode, extraHeaders, responseSchema;

	if (Array.isArray(dataOrOptions) || (typeof dataOrOptions !== 'object' && dataOrOptions !== undefined)) {
		// Pattern 1: formatSuccessResponse(request, data, statusCode)
		data = dataOrOptions;
		statusCode = statusCodeParam || 200;
		extraHeaders = {};
		responseSchema = undefined;
	} else {
		// Pattern 2: formatSuccessResponse(request, { data, statusCode, ... })
		data = dataOrOptions?.data;
		statusCode = dataOrOptions?.statusCode || 200;
		extraHeaders = dataOrOptions?.extraHeaders || {};
		responseSchema = dataOrOptions?.responseSchema;
	}

	// Convert Headers object to plain object and merge with extraHeaders
	const headers = { ...extraHeaders };

	// Add CORS Headers - use specific origin when credentials are involved
	const origin = request.headers.get('Origin');
	headers['Access-Control-Allow-Origin'] = origin || '*';
	headers['Access-Control-Allow-Methods'] = 'GET, POST, PUT, DELETE, OPTIONS, PATCH';
	headers['Access-Control-Allow-Headers'] = 'Content-Type, authorization, Authorization, x-session-token, X-Session-Token, x-username, X-Username, x-total-count, X-Total-Count';
	headers['Access-Control-Allow-Credentials'] = 'true';
console.log('headers', JSON.stringify(headers, null, 2));
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

	// Always close the Sequelize connection before returning the response
	await sequelizeAdapter.closeSequelize(request);

	return Response.json(data, {
		status: statusCode,
		headers: headers,
	});
}

// Format an error response
export const formatErrorResponse = async (request, error) => {
console.log('formatErrorResponse', request, error);

	// Send Error to Posthog
	const posthog = usePosthog();
	await posthog.captureError(request, error);

	// Always close the Sequelize connection before returning the response
	await sequelizeAdapter.closeSequelize(request);

	let statusCode = 500;
  let errorObject = { statusCode: 500, message: 'Unknown Server Error' };

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
	headers['Access-Control-Allow-Headers'] = 'Content-Type, authorization, Authorization, x-session-token, X-Session-Token, x-username, X-Username, x-total-count, X-Total-Count';
	headers['Access-Control-Allow-Credentials'] = 'true';
console.log('headers', JSON.stringify(headers, null, 2));
	return Response.json(errorObject, {
		status: statusCode,
		headers: headers,
	});
}

export const throwError = async (statusCode, message) => {
  return Promise.reject({ statusCode, message });
}

export const authenticate = async (request) => {
	const API_KEYS = process.env.API_KEYS;

  if(!request.headers.get('authorization') || request.headers.get('authorization') === ''){
    console.log('Missing Authorization Header - Returning 401');
    await throwError(401, 'Unauthorized');
  }
  const authHeader = request.headers.get('authorization');
	let apiKeys = [];
	try{
		apiKeys = JSON.parse(API_KEYS);
	} catch(error){
		console.error('Error parsing API Keys:', error);
		await throwError(401, 'Unauthorized IAK');
	}

  let authorized = false;
  for(const entry of apiKeys){
    if(authHeader === entry.key){
      authorized = true;
      break;
    }
  }
  if(!authorized){
    console.log('Invalid API Key - Returning 401');
    await throwError(401, 'Unauthorized');
  }
};

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

  const userModel = await getDbObject('User', true, request);
  const sessionTokenModel = await getDbObject('SessionToken', true, request);

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
	const posthog = usePosthog();
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

	// Capture the API request event
	// posthog.capture(request, 'api_request', {
	// 	path: validatedParameters.path,
	// 	query: validatedParameters.query,
	// 	body: validatedParameters.body,
	// })

	return { path: validatedParameters.path, query: validatedParameters.query, body: validatedParameters.body };
};

// Parse a filter JSON string into a JSON object
export const parseFilterJSON = (input) => {
  try{
    const output = JSON.parse(input);
    if (typeof output === 'string'){
      return parseFilterJSON(output);
    }
    return output;
  }
  catch(e){
    console.error('Error parsing filter JSON:', e);
    return undefined;
  }
}

// Function to replace operator aliases in a query object with Sequelize operators
export const replaceOperators = (obj) => {
  // Handle arrays - preserve them as arrays, don't process recursively
  if (Array.isArray(obj)) {
    return obj.map(item => {
      if (typeof item === 'object' && item !== null && !Array.isArray(item)) {
        return replaceOperators(item);
      }
      return item;
    });
  }

  // Initialize an empty object to store the formatted query
  const formattedObj = {};

  // Iterate over each key in the input object
  for (const key in obj) {
    // Check if the current value is an object (which could contain nested queries)
    if (typeof obj[key] === 'object' && obj[key] !== null) {
      // If the key is an operator alias, replace it with the corresponding Sequelize operator
      if (Object.keys(operatorsAliases).includes(key)) {
        switch (key) {
          case '$like':
          case '$notLike':
            formattedObj[operatorsAliases[key]] = `%${obj[key]}%`;
            break;
          case '$in':
          case '$notIn':
            // Preserve arrays as-is for $in and $notIn
            formattedObj[operatorsAliases[key]] = Array.isArray(obj[key]) ? obj[key] : replaceOperators(obj[key]);
            break;
          case '$between':
          case '$notBetween':
            // Preserve arrays as-is for $between and $notBetween (must be array of 2 values)
            formattedObj[operatorsAliases[key]] = Array.isArray(obj[key]) ? obj[key] : obj[key];
            break;
          default:
            formattedObj[operatorsAliases[key]] = replaceOperators(obj[key]);
        }
      }
      // If the key is not an operator, recursively call the function to handle nested objects
      else {
        formattedObj[key] = replaceOperators(obj[key]);
      }
    }
    // If the current value is not an object
    else {
      // Again, replace operator aliases with Sequelize operators if necessary
      if (Object.keys(operatorsAliases).includes(key)) {
        switch (key) {
          case '$like':
          case '$notLike':
            formattedObj[operatorsAliases[key]] = `%${obj[key]}%`;
            break;
          case '$in':
          case '$notIn':
            formattedObj[operatorsAliases[key]] = obj[key];
            break;
          default:
            formattedObj[operatorsAliases[key]] = obj[key];
        }
      }
      // Otherwise, simply copy the key-value pair as is
      else {
        formattedObj[key] = obj[key];
      }
    }
  }
  // Return the formatted query object
  return formattedObj;
}

// Function returns an array of primary keys for a given model
// Uses Sequelize v7 API: model.modelDefinition.attributes
// Falls back to source MODELS definition if Sequelize v7 structure is not accessible
export const getPrimaryKeys = (model) => {
  const modelDefinition = model.modelDefinition;

  if (!modelDefinition) {
    return [];
  }

  // Try to get primary keys from modelDefinition.primaryKeys if available
  if (modelDefinition.primaryKeys && Array.isArray(modelDefinition.primaryKeys)) {
    return modelDefinition.primaryKeys.map(pk => pk.attributeName || pk.name || pk);
  }

  // Try rawAttributes first (might still work in v7)
  const rawAttributes = modelDefinition.rawAttributes;
  if (rawAttributes && Object.keys(rawAttributes).length > 0) {
    const primaryKeys = Object.entries(rawAttributes)
      .filter(([_, attribute]) => attribute?.primaryKey === true)
      .map(([key, _]) => key);
    if (primaryKeys.length > 0) {
      return primaryKeys;
    }
  }

  // Try attributes
  const attributes = modelDefinition.attributes;
  if (attributes && Object.keys(attributes).length > 0) {
    const primaryKeys = Object.entries(attributes)
      .filter(([_, attribute]) => {
        // Handle both plain objects and Sequelize Attribute instances
        return attribute?.primaryKey === true ||
               attribute?.get?.('primaryKey') === true ||
               (typeof attribute === 'object' && attribute.primaryKey);
      })
      .map(([key, _]) => key);
    if (primaryKeys.length > 0) {
      return primaryKeys;
    }
  }

  // Last resort: check the original model definition from MODELS
  // This accesses the source definition before Sequelize processes it
  const sourceModel = MODELS[model.name];
  if (sourceModel) {
    return Object.entries(sourceModel)
      .filter(([_, attribute]) => attribute?.primaryKey === true)
      .map(([key, _]) => key);
  }

  return [];
}

export const createWhereClauseForGetOneRecord = async (model, compoundId) => {
  const primaryKeys = getPrimaryKeys(model);
  // If the model has a single primary key, return an object with the primary key and compound ID
  if(primaryKeys.length === 1){
    return { [primaryKeys[0]]: compoundId };
  }

  const idParts = compoundId.split('-');
  if (idParts.length !== primaryKeys.length) {
    return Promise.reject({ statusCode: 400, message: 'Invalid ID Format - Supplied ID does not match the number of primary keys for this model' });
  }

  const whereClause = primaryKeys.reduce((where, key, index) => {
    where[key] = idParts[index];
    return where;
  }, {});

  return whereClause;
}

// Get a Sequelize object
export const getSequelizeObject = async (request) => {
  const sequelize = await sequelizeAdapter.getSequelize(request);
  return sequelize;
}

// Get a Sequelize transaction
export const getSequelizeTransaction = async (request) => {
  const transaction = await sequelizeAdapter.getSequelizeTransaction(request);
  return transaction;
};

// Get a Model
export const getDbObject = async (resourceName, includeAssociations = true, request = null) => {
  // Dynamically import the model
  const model = MODELS[resourceName];

  if (!request) {
    throw new Error('getDbObject requires a request parameter');
  }

  const sequelize = await sequelizeAdapter.getSequelize(request);

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

// Parse JSON fields recursively in an object
export const parseJSONFields = (obj) => {
	// Check if the input is an object or array, and iterate through its properties or elements.
	if (obj !== null && typeof obj === 'object') {
		Object.keys(obj).forEach(key => {
			// Attempt to parse each property or element that is a string.
			if (typeof obj[key] === 'string') {
				try {
					obj[key] = JSON.parse(obj[key]);
				} catch (e) {
					// If parsing fails, it's not a JSON string, so do nothing.
				}
			}
			// If the property or element is an object or array, recursively apply this function.
			parseJSONFields(obj[key]);
		});
	}
};

// Check if the region user is executive
export const isRegionUserExecutive = async (regionUser) => {
  let positions = [];
  try{
    positions = JSON.parse(regionUser.Positions || '[]');
  }
  catch(err){
    console.error('Error parsing positions:', err);
    return false;
  }

  // chief, assignor, manager, coach
  return regionUser.IsExecutive || positions.includes('chief') || positions.includes('assignor') || positions.includes('manager') || positions.includes('coach');
}

// Filter unique by compound key
export const filterUniqueByCompoundKey = (results, keyParts = []) => {
  const uniqueResultsMap = new Map();

  results.forEach(item => {
      // const compoundKey = `${item[keyField1]}-${item[keyField2]}`;
      const compoundKey = keyParts.map(key => item[key]).join('-');
      if (!uniqueResultsMap.has(compoundKey)) {
          uniqueResultsMap.set(compoundKey, item);
      }
  });

  return Array.from(uniqueResultsMap.values());
}

// Sort array by property
export const sortArrayByProperty = (array, property, direction = 'ASC') => {
  return array.sort((a, b) => {
      let valueA = a[property];
      let valueB = b[property];

      // For case-insensitive string comparison convert strings to lowercase
      if (typeof valueA === 'string' && typeof valueB === 'string') {
          valueA = valueA.toLowerCase();
          valueB = valueB.toLowerCase();
      }

      if (direction.toUpperCase() === 'ASC') {
          return valueA > valueB ? 1 : (valueA < valueB ? -1 : 0);
      } else if (direction.toUpperCase() === 'DESC') {
          return valueA < valueB ? 1 : (valueA > valueB ? -1 : 0);
      }
  });
}

// Build contextual facet values (no counts) for one or more fields based on current where/include
// facetSpecs: [{ key, attribute, labelAttribute?, labelConcat?: { parts: string[], separator?: string }, labelCoalesce?: { parts: string[] }, include?, order? }]
export const buildFacetValues = async ({ model, where = {}, include = [], facetSpecs = [], limit = 50, sequelize = undefined, request = undefined }) => {
  const db = sequelize || await getSequelizeObject(request);

  const tasks = facetSpecs.map(async (spec) => {
    const attributeCol = Sequelize.col(spec.attribute);

    let labelCol = null;
    if (spec.labelAttribute) {
      labelCol = Sequelize.col(spec.labelAttribute);
    } else if (spec.labelConcat && Array.isArray(spec.labelConcat.parts) && spec.labelConcat.parts.length > 0) {
      const sep = spec.labelConcat.separator ?? ' - ';
      const parts = [];
      spec.labelConcat.parts.forEach((p, idx) => {
        if (idx > 0) {
          // Use Unicode string literal for MSSQL
          parts.push(Sequelize.literal(`N'${sep.replace(/'/g, "''")}'`));
        }
        parts.push(Sequelize.col(p));
      });
      labelCol = Sequelize.fn('CONCAT', ...parts);
    } else if (spec.labelCoalesce && Array.isArray(spec.labelCoalesce.parts) && spec.labelCoalesce.parts.length > 0) {
      const parts = spec.labelCoalesce.parts.map(p => Sequelize.col(p));
      labelCol = Sequelize.fn('COALESCE', ...parts);
    }

    const attributes = [[attributeCol, 'value']];
    if(labelCol){
      attributes.push([labelCol, 'label']);
    }

    const group = [attributeCol];
    if(labelCol){
      group.push(labelCol);
    }

    // Always order by the selected alias to avoid GROUP BY issues in SQL Server
    const order = [[Sequelize.col(labelCol ? 'label' : 'value'), 'ASC']];

    // Ensure includes used for faceting do not select extra columns that would break GROUP BY
    // When using an alias (as), remove the model property to let Sequelize resolve it from the association
    // Deduplicate includes by alias (as) or model to prevent duplicate joins
    const allIncludes = [
      ...(include || []),
      ...((spec.include || []))
    ];

    // Deduplicate includes - use a Map keyed by alias (as) or model name
    const includeMap = new Map();
    for (const inc of allIncludes) {
      const key = inc.as || (inc.model?.name || inc.model) || JSON.stringify(inc);
      if (!includeMap.has(key)) {
        const sanitized = { ...inc, attributes: [] };
        // Remove model property when using an alias - Sequelize will resolve from association
        if (sanitized.as) {
          delete sanitized.model;
        }
        includeMap.set(key, sanitized);
      }
    }
    const sanitizedInclude = Array.from(includeMap.values());

    const rows = await model.findAll({
      where,
      include: sanitizedInclude,
      attributes,
      group,
      order,
      // Intentionally omit limit to prevent MSSQL from appending PKs to ORDER BY in grouped queries
      raw: true,
      subQuery: false,
    });

    // Omit null/undefined facet values
    const filteredRows = rows.filter(r => r.value !== null && r.value !== undefined);

    const valuesAll = filteredRows.map(r => {
      const out = { value: r.value };
      if(r.label !== undefined){
        out.label = r.label;
      }
      return out;
    });

    const values = valuesAll.slice(0, Math.max(0, limit || 0));

    return [spec.key, values];
  });

  const results = await Promise.all(tasks);
  return results.reduce((acc, [key, values]) => {
    acc[key] = values;
    return acc;
  }, {});
};
