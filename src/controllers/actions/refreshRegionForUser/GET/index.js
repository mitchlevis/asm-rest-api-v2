import { authenticateSessionToken, validateIncomingParameters, getDbObject, formatSuccessResponse, formatErrorResponse, throwError, parseJSONFields, convertPropertiesToCamelCase } from '../../../../utils/helpers';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		// Authenticate Session Token
		const tokenData = await authenticateSessionToken(request);
		const userId = tokenData.UserName;

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { regionId } = path;
		const { camel_case: camelCase } = query;

		// Get RegionUser
		const regionUserModel = await getDbObject('RegionUser', true, request);
		const regionUser = await regionUserModel.findOne({ 
			where: { RegionId: regionId, RealUsername: userId },
			raw: true
		});

		if (!regionUser) {
			await throwError(404, `Region User not found`);
		}

		// Get Region separately (since associations don't work well)
		const regionModel = await getDbObject('Region', true, request);
		const region = await regionModel.findOne({
			where: { RegionID: regionId },
			raw: true
		});

		if (!region) {
			await throwError(404, `Region not found`);
		}

		// Parse JSON fields
		parseJSONFields(region);
		parseJSONFields(regionUser);

		// Optionally convert to camelCase
		const response = {
			region: camelCase ? convertPropertiesToCamelCase(region) : region,
			regionUser: camelCase ? convertPropertiesToCamelCase(regionUser) : regionUser
		};

		return formatSuccessResponse(request, { data: response }, 200);
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
};

