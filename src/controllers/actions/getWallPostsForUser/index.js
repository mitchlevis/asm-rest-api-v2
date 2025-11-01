import { authenticateSessionToken, validateIncomingParameters, getDbObject, formatSuccessResponse, formatErrorResponse } from '../../../utils/helpers';
import parameters from "./parameters";

export default async (request) => {
	try{
		// Authenticate Session Token
		const tokenData = await authenticateSessionToken(request);
    const userId = tokenData.UserName;

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, parameters);

		const { regionId } = path;
    const sortDirection = query.sort_direction;
    const limit = query.limit;
    const offset = query.offset;

		const regionUserModel = await getDbObject('RegionUser');
    const whereObject = { RealUsername: userId };
    if(regionId){
      whereObject.regionId = regionId;
    }

    // Get all regions for user
    const regionsForUser = await regionUserModel.findAll({ attributes: ['RegionId'], where: whereObject});
    const regionIds = regionsForUser.map((region) => region.RegionId);

		// If no regions, return empty array
    if(regionIds.length === 0){
      return formatSuccessResponse(request, [], 200);
    }

		// Get All Users for regions
    let usersForRegions = await regionUserModel.findAll({ attributes: ['RealUsername'], where: { RegionId: { [Op.in]: regionIds} }, group: 'RealUsername' });
    usersForRegions = [...new Set(usersForRegions)];
    const usersForRegionsArray = usersForRegions.filter((user) => user.RealUsername !==  '' ).map((obj) => obj.RealUsername);


		return formatSuccessResponse(request, {
			data: { usersForRegionsArray, userId, path, query, body },
		});
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
}
