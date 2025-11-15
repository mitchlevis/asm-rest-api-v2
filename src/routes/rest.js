/**
 * Controllers for REST routes
 */

import getAllController from '../controllers/rest/getAll/index.js';
import getOneController from '../controllers/rest/getOne/index.js';
import createOneController from '../controllers/rest/createOne/index.js';
import updateOneController from '../controllers/rest/updateOne/index.js';
import deleteOneController from '../controllers/rest/deleteOne/index.js';

import getAvailableResourcesController from '../controllers/rest/getAvailableResources/index.js';
import getColumnsController from '../controllers/rest/getColumns/index.js';
import getIdentifiersController from '../controllers/rest/getIdentifiers/index.js';

export function setupRestRoutes(router) {
	// GET /rest/available-resources - Get all available resources
	router.get('/rest/resources', getAvailableResourcesController);

	// GET /rest/:resourceName - Get all records
	router.get('/rest/:resourceName', getAllController);

	// GET /rest/:resourceName/columns - Get all columns for a resource
	router.get('/rest/:resourceName/columns', getColumnsController);

	// GET /rest/:resourceName/identifiers - Get all identifiers for a resource
	router.get('/rest/:resourceName/identifiers', getIdentifiersController);

	// GET /rest/:resourceName/:id - Get one record by ID
	router.get('/rest/:resourceName/:id', getOneController);

	// POST /rest/:resourceName - Create a new record and return the created record
	router.post('/rest/:resourceName', createOneController);

	// PUT /rest/:resourceName/:id - Update a record by ID and return the updated record
	router.put('/rest/:resourceName/:id', updateOneController);

	// DELETE /rest/:resourceName/:id - Delete a record by ID and return the deleted record
	router.delete('/rest/:resourceName/:id', deleteOneController);
}
