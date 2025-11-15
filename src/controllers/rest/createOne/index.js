import { formatSuccessResponse, formatErrorResponse, authenticate, validateIncomingParameters, createWhereClauseForGetOneRecord, getDbObject, throwError } from '../../../utils/helpers';
import { resolveModelName } from '../../../db/models';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		await authenticate(request);

		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { resourceName } = path;
		const data = await request.json();

		// Resolve plural kebab-case resource name to singular PascalCase model name
		const modelName = resolveModelName(resourceName);
		const dbObject = await getDbObject(modelName, false, request);

		if (!modelName) {
			await throwError(404, `Resource '${resourceName}' not found`);
		}

		const newRecord = await dbObject.create(data);

		return formatSuccessResponse(request, {
			data: newRecord,
		});
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
};
