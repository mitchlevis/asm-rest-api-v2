import { authenticate, validateIncomingParameters, formatSuccessResponse, formatErrorResponse, throwError } from '../../../utils/helpers';
import { useSESService } from '../../../services/SES';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		await authenticate(request);

		// Validate Parameters
    const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { from, to, subject, body: textBody } = body;

		const ses = useSESService();

		const result = await ses.sendEmail({ fromEmailAddress: from, toEmailList: to, subject, textBody });

		return formatSuccessResponse(request, {
			data: result,
		});
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
}
