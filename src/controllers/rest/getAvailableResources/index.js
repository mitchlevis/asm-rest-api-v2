import { formatSuccessResponse, formatErrorResponse, authenticate, validateIncomingParameters } from '../../../utils/helpers';
import { MODELS, modelNameToPluralKebabCase } from '../../../db/models';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		await authenticate(request);

		await validateIncomingParameters(request, requestValidationSchema);

		// Get all model names and convert them to the required format
		const resources = Object.keys(MODELS).map((modelName) => {
			const pluralKebabCase = modelNameToPluralKebabCase(modelName);
			const obj = {};
			obj[modelName] = pluralKebabCase;
			return obj;
		});
console.log('resources', resources);
		return formatSuccessResponse(request, {
			data: resources,
		});
	} catch (error) {
		console.error(error);
		return formatErrorResponse(request, error);
	}
};
