import { formatSuccessResponse, formatErrorResponse, authenticate, validateIncomingParameters, replaceOperators, getDbObject, throwError } from '../../../utils/helpers';
import { resolveModelName } from '../../../db/models';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		await authenticate(request);

		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { resourceName } = path;
		const { limit, offset, attributes, filter, sort, sort_direction: sortDirection } = query;

		// Reject "resources" as it's a reserved route for getting available resources
		if (resourceName === 'resources') {
			await throwError(404, `Resource '${resourceName}' not found`);
		}

		// Resolve plural kebab-case resource name to singular PascalCase model name
		const modelName = resolveModelName(resourceName);

		if (!modelName) {
			await throwError(404, `Resource '${resourceName}' not found`);
		}

		// Filter
		let formattedFilter;
    if(filter){
      formattedFilter = replaceOperators(filter);
    }

		let sortingArray;
    if(sort){
      sortingArray = sort.map((sortField) => [sortField, sortDirection]);
    }

		const dbObject = await getDbObject(modelName, false, request);

		const {count, rows} = await dbObject.findAndCountAll({ order: sortingArray, attributes, limit, offset, where: formattedFilter });

		return formatSuccessResponse(request, {
			data: rows,
			extraHeaders: { 'x-total-count': count },
		});
	} catch (error) {
		console.error(error);
		return formatErrorResponse(request, error);
	}
};
