import { formatSuccessResponse, formatErrorResponse, authenticate, validateIncomingParameters, createWhereClauseForGetOneRecord, getDbObject, throwError } from '../../../utils/helpers';
import { resolveModelName } from '../../../db/models';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		await authenticate(request);

		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { resourceName, id } = path;
		const { attributes } = query;

		// Resolve plural kebab-case resource name to singular PascalCase model name
		const modelName = resolveModelName(resourceName);

		if (!modelName) {
			await throwError(404, `Resource '${resourceName}' not found`);
		}

		// Validate ID is present and not empty
		if(!id || id === '') {
      await throwError(400, 'Invalid Request. ID is required');
    }

		const dbObject = await getDbObject(modelName, false, request);

		const whereClause = await createWhereClauseForGetOneRecord(dbObject, id);
console.log('whereClause', whereClause);
		const record = await dbObject.findOne({ attributes, where: whereClause, raw: true });

		if(!record) {
			await throwError(404, `Record not found for resource '${resourceName}' with ID '${id}'`);
		}

		return formatSuccessResponse(request, {
			data: record,
		});
	} catch (error) {
		console.error(error);
		return formatErrorResponse(request, error);
	}
};
