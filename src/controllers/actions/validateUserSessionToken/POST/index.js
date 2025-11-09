import { authenticate, validateIncomingParameters, getDbObject, formatSuccessResponse, formatErrorResponse, throwError } from '../../../../utils/helpers';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		await authenticate(request);

		// Validate Parameters
    const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { session_token, username } = body;

		if(!session_token || session_token === ''){
      await throwError(400, 'Invalid Request. Missing session_token');
    }
    if(!username || username === ''){
      await throwError(400, 'Invalid Request. Missing username');
    }

    const userModel = await getDbObject('User', true, request);
    const sessionTokenModel = await getDbObject('SessionToken', true, request);

    // Get user
    const user = await userModel.findOne({ where: { Username: username } });
    if(!user){
      await throwError(404, 'User not found');
    }
    // Get session token
    const sessionToken = await sessionTokenModel.findOne({ where: { Username: user.Username, SessionToken: session_token } });
    if(!sessionToken){
      await throwError(401, 'Invalid session_token');
    }
    // Check if session token is expired - IssuanceDate + DurationDays < Now
    const now = new Date();
    const expirationDate = new Date(sessionToken.IssuanceDate);
    expirationDate.setDate(expirationDate.getDate() + sessionToken.DurationDays);
    if(now > expirationDate){
      await throwError(401, 'Session token expired');
    }

    const response = sessionToken;

		return formatSuccessResponse(request, {
			data: response,
		});
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
}
