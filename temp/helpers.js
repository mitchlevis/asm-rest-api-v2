import AWS from 'aws-sdk';
import Joi from 'joi';
import Sequelize from 'sequelize';
import crypto from 'crypto';
import { MODELS, MODEL_ASSOCIATIONS} from "./models";
import sequelizeAdapter from "./mssql-db";
import { convertModelToSchema } from './open-api-functions';
const Op = Sequelize.Op;


// Convert user-friendly operators to Sequelize operators
const operatorsAliases = {
  $eq: Op.eq,
  $ne: Op.ne,
  $is: Op.is,
  $not: Op.not,
  $or: Op.or,
  $col: Op.col,
  $gt: Op.gt,
  $gte: Op.gte,
  $lt: Op.lt,
  $lte: Op.lte,
  $between: Op.between,
  $notBetween: Op.notBetween,
  $all: Op.all,
  $in: Op.in,
  $notIn: Op.notIn,
  $like: Op.like,
  $notLike: Op.notLike,
  $startsWith: Op.startsWith,
  $endsWith: Op.endsWith,
  $substring: Op.substring,
  $iLike: Op.iLike,
  $notILike: Op.notILike,
  $regexp: Op.regexp,
  $notRegexp: Op.notRegexp,
  $iRegexp: Op.iRegexp,
  $notIRegexp: Op.notIRegexp,
  $any: Op.any,
  $match: Op.match,
  $like: Op.like
};

export const authenticate = async (_evt, API_KEYS) => {
  if(!_evt.headers['authorization'] || _evt.headers['authorization'] === ''){
    console.log('Missing Authorization Header - Returning 401');
    await throwError(401, 'Unauthorized');
  }
  const authHeader = _evt.headers['authorization'];
  const apiKeys = JSON.parse(API_KEYS);

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
export const authenticateSessionToken = async (_evt) => {
  // Look for x-session-token header and x-username header
  if(!_evt.headers['x-session-token'] || _evt.headers['x-session-token'] === ''){
    console.log('Missing x-session-token Header - Returning 401');
    await throwError(401, 'Unauthorized');
  }
  if(!_evt.headers['x-username'] || _evt.headers['x-username'] === ''){
    console.log('Missing x-username Header - Returning 401');
    await throwError(401, 'Unauthorized');
  }

  // Get session token and username from headers
  const session_token = _evt.headers['x-session-token'];
  const username = _evt.headers['x-username'];

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

// Function to validate parameters (path, query, body) against a Joi schema
export const validateIncomingParameters = async (_evt, parameters) => {
  const { path, query, body } = parameters;
  const validatedParameters = { path: null, query: null, body: null };

  // Validate Path Parameters
  if(path && Object.keys(path).length > 0){
    const pathValidation = Joi.object(path).validate(_evt.pathParameters || {});
    if(pathValidation.error){
      console.error(pathValidation.error);
      return throwError(400, `Invalid Path Parameters - ${pathValidation.error}`);
    }
    validatedParameters.path = pathValidation.value;
  }

  // Validate Query Parameters
  if(query && Object.keys(query).length > 0){
    const queryValidation = Joi.object(query).validate(_evt.queryStringParameters || {});
    if(queryValidation.error){
      console.error(queryValidation.error);
      return throwError(400, `Invalid Query Parameters - ${queryValidation.error}`);
    }
    validatedParameters.query = queryValidation.value;
  }

  // Validate Body Parameters
  if(body && Object.keys(body).length > 0){
    const bodyValidation = Joi.object(body).validate(JSON.parse(_evt.body || '{}'));
    if(bodyValidation.error){
      console.error(bodyValidation.error);
      return throwError(400, `Invalid Body Parameters - ${bodyValidation.error}`);
    }
    validatedParameters.body = bodyValidation.value;
  }

  return { path: validatedParameters.path, query: validatedParameters.query, body: validatedParameters.body }
};

export const validateOutgoingParameters = async (data, responseParameter) => {
  
  // If type is joi, validate the response body
  if(responseParameter.type === 'joi'){
    if(responseParameter.schema && Object.keys(responseParameter.schema).length > 0){
      console.log('Validating Response Body against Joi Schema', responseParameter.schema);
      const responseValidation = Joi.object(responseParameter.schema).validate(data || {});
      if(responseValidation.error){
        console.error(responseValidation.error);
        return Promise.reject({ statusCode: 406, message: `Invalid Response Body - ${responseValidation.error}` });
      }
    }
  }

  // If type is model, validate the response body
  if(responseParameter.type === 'model'){
    const model = await getDbObject(responseParameter.model, false);
    const schema = convertModelToSchema(model);
    console.log('Validating Response Body against Model Schema', schema);
    const responseValidation = Joi.object(schema).validate(data || {});
    if(responseValidation.error){
      console.error(responseValidation.error);
      return Promise.reject({ statusCode: 406, message: `Invalid Response Body - ${responseValidation.error}` });
    }
  }
};

export const parseFilterJSON = (input) => {
  try{
    const output = JSON.parse(input);
    if (typeof output === 'string'){
      return parseFilterJSON(output);
    }
    return output;
  }
  catch(e){
    throw new Error(`Invalid filter JSON - ${input}`);
  }
}

export const getFilterQueryParameter = (queryStringParameters) => {
console.log('queryStringParameters',queryStringParameters);
  if(!queryStringParameters || !queryStringParameters.filter){
    return undefined;
  }

  try{
    const filter = parseFilterJSON(decodeURIComponent(queryStringParameters.filter));
    // const filter = decodeURIComponent(queryStringParameters.filter);
console.log('filter', typeof filter);
    return filter;
  }
  catch(err){
    console.error('Invalid filter query parameter', err);
  }
}

export const throwError = async (statusCode, message) => {
  return Promise.reject({ statusCode, message });
}

export const getHeaders = (origin) => {
  return {
    'Content-Type': 'application/json',
    'Access-Control-Allow-Origin': origin,
    'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type, Authorization, X-Amz-Date, X-Api-Key, X-Amz-Security-Token, x-username, x-session-token',
    'Access-Control-Expose-Headers': 'x-total-count',
    'Access-Control-Allow-Credentials': 'true',
  };
};

export const formatErrorResponse = async (origin, err) => {
  let statusCode = 500;
  let errorObject = { statusCode: 500, message: 'Unknown Server Error' };

  // Close the Sequelize connection before returning the response
  await sequelizeAdapter.closeSequelize();
  
  console.log('formatErrorResponse:', err);

  // Check if error is a custom error object
  if(err.statusCode){
    statusCode = err.statusCode;
    errorObject = err;
  }

  // Check if error is sequelize UniqueConstraintError
  if(err.name === 'SequelizeUniqueConstraintError'){
    statusCode = 400;
    errorObject = { statusCode: 400, message: 'SequelizeUniqueConstraintError - Duplicate Record' };
  }
  // Check if error is sequelize SequelizeDatabaseError
  else if(err.name === 'SequelizeDatabaseError'){
    statusCode = 400;
    errorObject = { statusCode: 400, message: `SequelizeDatabaseError - ${err.parent}` };
  }
  // Check if error is sequelize ValidationError
  else if(err.name === 'SequelizeValidationError'){
    statusCode = 400;
    errorObject = { statusCode: 400, message: `SequelizeValidationError - ${err.message}` };
  }
  // Check if error is sequelize SequelizeEagerLoadingError
  else if(err.name === 'SequelizeEagerLoadingError'){
    statusCode = 400;
    errorObject = { statusCode: 400, message: `SequelizeEagerLoadingError - ${err.message}` };
  }

  console.error(JSON.stringify(err));

  return {
    statusCode: statusCode,
    headers: {...getHeaders(origin), ...{ 'Content-Type': 'application/json' }},
    body: JSON.stringify(errorObject)
  };
};

export const formatSuccessResponse = async (_evt, data, statusCode = 200, parameters = undefined, extraHeaders = {}, keepDBAlive = false) => {console.log('extraHeaders', extraHeaders)
  // Close the Sequelize connection before returning the response
  if(!keepDBAlive){
    await sequelizeAdapter.closeSequelize();
  }

  // Check if we need to validate the response against a Joi/Model schema
  if(parameters?.responses && parameters.responses[statusCode]){
    try{
      await validateOutgoingParameters(data, parameters.responses[statusCode]);
    }
    catch(err){
      return formatErrorResponse(_evt.headers.origin, err);
    }
  }

  return {
    statusCode: statusCode,
    headers: extraHeaders && Object.keys(extraHeaders).length > 0 ? { ...getHeaders(_evt.headers.origin), ...extraHeaders } : getHeaders(_evt.headers.origin),
    body: JSON.stringify(data)
  };
};

export const closeSequelizeConnection = async () => {
  await sequelizeAdapter.closeSequelize();
}

export const getResourceName = async (_evt, MAPPINGS) => {
  const path = _evt.requestContext.http.path;
  const pathParts = path.split('/');
  if(!MAPPINGS[pathParts[2]]) { 
    return Promise.reject({ statusCode: 400, message: 'Invalid Resource. Not Found'});
  }

  const resourceName = MAPPINGS[pathParts[2]];
  return resourceName;
};

export const getSequelizeObject = async () => {
  const sequelize = await sequelizeAdapter.getSequelize();
  return sequelize;
}

export const getSequelizeTransaction = async () => {
  const transaction = await sequelizeAdapter.getSequelizeTransaction();
  return transaction;
};

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
        dbObject.belongsTo(belongsToModel, { foreignKey: belongsTo.foreignKey, targetKey: belongsTo.targetKey, targetKey: belongsTo.targetKey, as: belongsTo.as, scope: belongsTo.scope, constraints: belongsTo.constraints, foreignKeyConstraint: belongsTo.foreignKeyConstraint });
      }
    }
    // Has One
    if(MODEL_ASSOCIATIONS[resourceName]?.hasOne && MODEL_ASSOCIATIONS[resourceName]?.hasOne.length > 0){

      for(const hasOne of MODEL_ASSOCIATIONS[resourceName]?.hasOne){
        const hasOneResourceName = hasOne.modelName;
        const hasOneModelObject = MODELS[hasOneResourceName];
        const hasOneModel = sequelize.define(hasOneResourceName, hasOneModelObject, { tableName: hasOneResourceName, timestamps: false});
        dbObject.hasOne(hasOneModel, { foreignKey: hasOne.foreignKey, sourceKey: hasOne.sourceKey, as: hasOne.as, scope: hasOne.scope, constraints: hasOne.constraints });
      }
    }
    // Has Many
    if(MODEL_ASSOCIATIONS[resourceName]?.hasMany && MODEL_ASSOCIATIONS[resourceName]?.hasMany.length > 0){

      for(const hasMany of MODEL_ASSOCIATIONS[resourceName]?.hasMany){
        const hasManyResourceName = hasMany.modelName;
        const hasManyModelObject = MODELS[hasManyResourceName];
        const hasManyModel = sequelize.define(hasManyResourceName, hasManyModelObject, { tableName: hasManyResourceName, timestamps: false});

        dbObject.hasMany(hasManyModel, { foreignKey: hasMany.foreignKey, sourceKey: hasMany.sourceKey, targetKey: hasMany.targetKey, as: hasMany.as, scope: hasMany.scope, constraints: hasMany.constraints });
      }
    }
  }

  return dbObject;
}

// Function to replace operator aliases in a query object with Sequelize operators
export const replaceOperators = (obj) => {
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
            formattedObj[operatorsAliases[key]] = obj[key];
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
export const getPrimaryKeys = (model) => {
  return Object.entries(model.rawAttributes)
               .filter(([_, attribute]) => attribute.primaryKey)
               .map(([key, _]) => key);
}

export const createWhereClauseForGetOneRecord = async (model, compoundId) => {
  const primaryKeys = getPrimaryKeys(model);
console.log('primaryKeys', primaryKeys)
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

export const parseJSONFields = (obj) => {
  // Check if the input is an object or array, and iterate through its properties or elements.
  if (obj !== null && typeof obj === 'object') {
    Object.keys(obj).forEach(key => {
      console.log('key', key);
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
}

export const parseJSONFieldsInDbResponse = (data) => {
  data.forEach(item => parseJSONFields(item));
}

export const toMSSQLDatetime = (date) => {
  return date.getFullYear() + '-' +
    ('00' + (date.getMonth() + 1)).slice(-2) + '-' +
    ('00' + date.getDate()).slice(-2) + ' ' + 
    ('00' + date.getHours()).slice(-2) + ':' + 
    ('00' + date.getMinutes()).slice(-2) + ':' + 
    ('00' + date.getSeconds()).slice(-2);
}

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

export const arrayToObject = (array, key = 'id') => {
  return array.reduce((obj, item) => {
    obj[item[key]] = item;
    return obj;
  }, {});
};

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

// Generate SQL from sequelize query (for debugging purposes)
export const generateSQL = async (model, options) => {
  const query = model.build();
  const sql = await query.constructor.findAll({
    ...options,
    // Disable raw query to ensure proper model instantiation
    raw: false,
    // Use this option to get the SQL without executing the query
    logging: (sql) => {
      console.log("Generated SQL:", sql);
      return sql;
    },
  });
  return sql;
};

export const invokeLambdaFunction = async (functionName, invocationType = 'RequestResponse', payload) => {
  const lambda = new AWS.Lambda();

  const params = {
    FunctionName: functionName,
    InvocationType: invocationType,
    Payload: JSON.stringify(payload)
  };

  const result = await lambda.invoke(params).promise();

  return result;
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

/**
 * Checks if a processing object exists and if its timestamp is older than the specified threshold
 * @param {Object} s3Service - The S3 service object with getObject and deleteObject methods
 * @param {string} cacheKey - The cache key to check for processing
 * @param {number} thresholdHours - Number of hours after which processing is considered stale (default: 1)
 * @returns {Promise<boolean>} - Returns true if processing should continue, false if still processing
 */
export const checkProcessingStatus = async (s3Service, cacheKey, thresholdHours = 1) => {
  const { getObject, deleteObject } = s3Service;
  const processingKey = `${cacheKey}-processing`;
  
  try {
    const isProcessing = await getObject({ key: processingKey, json: true });
    
    if (!isProcessing) {
      return true; // No processing object, safe to proceed
    }
    
    // Check if timestamp exists and is valid
    if (!isProcessing.timestamp) {
      console.log('Processing object found without timestamp, assuming stale and removing');
      await deleteObject({ key: processingKey }).catch(err => console.error(err));
      return true;
    }
    
    // Check if the processing timestamp is older than threshold
    const processingTimestamp = new Date(isProcessing.timestamp);
    const now = new Date();
    const thresholdTime = new Date(now.getTime() - (thresholdHours * 60 * 60 * 1000));
    
    if (processingTimestamp < thresholdTime) {
      console.log(`Processing object is older than ${thresholdHours} hour(s), assuming responsibility for refresh`);
      await deleteObject({ key: processingKey }).catch(err => console.error(err));
      return true;
    }
    
    return false; // Still processing within threshold
  } catch (error) {
    console.error('Error checking processing status:', error);
    return true; // If there's an error, assume we can proceed
  }
}

/**
 * Creates a processing object with timestamp
 * @param {Object} s3Service - The S3 service object with putObject method
 * @param {string} cacheKey - The cache key to mark as processing
 * @returns {Promise<void>}
 */
export const markAsProcessing = async (s3Service, cacheKey) => {
  const { putObject } = s3Service;
  const processingKey = `${cacheKey}-processing`;
  
  await putObject({ 
    key: processingKey, 
    data: { processing: true, timestamp: new Date().toISOString() }, 
    json: true 
  });
}

// Build contextual facet values (no counts) for one or more fields based on current where/include
// facetSpecs: [{ key, attribute, labelAttribute?, labelConcat?: { parts: string[], separator?: string }, labelCoalesce?: { parts: string[] }, include?, order? }]
export const buildFacetValues = async ({ model, where = {}, include = [], facetSpecs = [], limit = 50, sequelize = undefined }) => {
  const db = sequelize || await getSequelizeObject();

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
    const sanitizedInclude = [
      ...(include || []),
      ...((spec.include || []))
    ].map((inc) => ({ ...inc, attributes: [] }));

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