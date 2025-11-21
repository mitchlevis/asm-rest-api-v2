import dayjs from 'dayjs';
import Sequelize from 'sequelize';
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, getFilterQueryParameter, replaceOperators, throwError, getDbObject, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";

export const handler = async (_evt) => {
  try{
    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);
    
    const { invitationId, email } = path;

    // Get Invitation
    const invitationModel = await getDbObject('UserInvitation');
    const invitation = await invitationModel.findOne({ where: { InvitationGUID: invitationId, Email: email }});
    if(!invitation){
      await throwError(404, `User invitation not found`);
    }

    // Try to match the RegionUser to the Invitation
    const regionModel = await getDbObject('Region');
    const regionUserModel = await getDbObject('RegionUser');
    const regionUser = await regionUserModel.findOne({
      include: [{
        model: regionModel,
        attributes: ['RegionName', 'RegionId']
      }],
      where: { 
        Username: invitation.Username,
        Email: email,
        IsInfoLinked: false
      },
    });


    // Does User Exists
    const userModel = await getDbObject('User');
    const user = await userModel.findOne({ where: { Email: email }});
console.log('regionUser.Region.RegionId', regionUser.Region.RegionId)
    const responseObject = {
      userExists: user ? true : false,
      regionId: regionUser?.RegionId,
      regionName: regionUser?.Region?.RegionName,
    }
    

    return formatSuccessResponse(_evt, responseObject, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};