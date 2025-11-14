import { authenticate, validateIncomingParameters, formatSuccessResponse, formatErrorResponse, throwError } from '../../../utils/helpers';
import { useR2Service } from '../../../services/R2';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		await authenticate(request);

		// Validate Parameters
    const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { key } = path;
		const bucketBindingName = 'API_BUCKET';

		const r2 = useR2Service();

		const result = await r2.getObject({ key, bucketBindingName, json: true });

		if(!result) {
			await throwError(404, `Object not found in ${bucketBindingName} with key ${key}`);
		}

		return formatSuccessResponse(request, {
			data: result,
		});
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
}
