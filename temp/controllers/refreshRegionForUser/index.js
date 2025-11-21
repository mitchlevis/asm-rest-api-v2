import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, parseJSONFields, convertPropertiesToCamelCase, getDbObject, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";

export const handler = async (_evt) => {
  try{
    const tokenData = await authenticateSessionToken(_evt);
    const userId = tokenData.UserName;

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);

    const { regionId } = path;
    const { camel_case: camelCase } = query;

    const regionUserModel = await getDbObject('RegionUser');
    const regionModel = await getDbObject('Region');

    const result = await regionUserModel.findOne({ 
      where: { RegionId: regionId, RealUsername: userId },
      include: [
        {
          model: regionModel,
          as: 'Region'
        }
      ],
      raw: true,
      nest: true
    });

    const region = result.Region;
    const regionUser = result;
    delete regionUser.Region; // Remove the Region object from the regionUser

    parseJSONFields(region);
    parseJSONFields(regionUser);

    const response = {
      region: camelCase ? convertPropertiesToCamelCase(region) : region,
      regionUser: camelCase ? convertPropertiesToCamelCase(regionUser) : regionUser
    };

    return formatSuccessResponse(_evt, response, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};