import { validateIncomingParameters, getDbObject, formatSuccessResponse, formatErrorResponse, throwError } from '../../../../utils/helpers';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { invitationId, email } = path;

		// Get Invitation
		const invitationModel = await getDbObject('UserInvitation', true, request);
		const invitation = await invitationModel.findOne({
			where: { InvitationGUID: invitationId, Email: email },
			raw: true
		});

		if(!invitation){
			await throwError(404, `User invitation not found`);
		}

		// Try to match the RegionUser to the Invitation
		const regionUserModel = await getDbObject('RegionUser', true, request);
		const regionUser = await regionUserModel.findOne({
			where: {
				Username: invitation.Username,
				Email: email,
				IsInfoLinked: false
			},
			raw: true
		});

		// Get Region information if RegionUser exists
		let regionName = undefined;
		if (regionUser?.RegionId) {
			const regionModel = await getDbObject('Region', true, request);
			const region = await regionModel.findOne({
				where: { RegionID: regionUser.RegionId },
				attributes: ['RegionName'],
				raw: true
			});
			regionName = region?.RegionName || undefined;
		}

		// Does User Exist
		const userModel = await getDbObject('User', true, request);
		const user = await userModel.findOne({
			where: { Email: email },
			raw: true
		});

		const responseObject = {
			userExists: user ? true : false,
			regionId: regionUser?.RegionId || undefined,
			regionName: regionName,
		}

		return formatSuccessResponse(request, { data: responseObject }, 200);
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
};

