import { authenticate, validateIncomingParameters, formatSuccessResponse, formatErrorResponse } from '../../../utils/helpers';
import { useR2Service } from '../../../services/R2';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		await authenticate(request);

		// Validate Parameters (only path)
    const { path } = await validateIncomingParameters(request, requestValidationSchema);

		const { key } = path;

		// Parse the body directly since we accept any JSON
		const data = await request.json();
		console.log('Parsed body data:', data);

		const bucketBindingName = 'API_BUCKET';

		const r2 = useR2Service();

		await r2.putObject({ key, data, bucketBindingName, json: true });

		return formatSuccessResponse(request, {
			data: { message: "File stored successfully." },
		});
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
}
