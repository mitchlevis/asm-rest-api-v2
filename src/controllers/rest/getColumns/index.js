import { formatSuccessResponse, formatErrorResponse, authenticate, validateIncomingParameters, createWhereClauseForGetOneRecord, getDbObject, throwError } from '../../../utils/helpers';
import { resolveModelName } from '../../../db/models';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		await authenticate(request);

		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { resourceName } = path;

		const modelName = resolveModelName(resourceName);
		const dbObject = await getDbObject(modelName, false, request);

		const columns = await dbObject.queryInterface.describeTable(modelName);

		return formatSuccessResponse(request, {
			data: columns,
		});
	} catch (error) {
		console.error(error);
		return formatErrorResponse(request, error);
	}
};
