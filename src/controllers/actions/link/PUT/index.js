import { v4 as uuidv4 } from 'uuid';
import { authenticateSessionToken, validateIncomingParameters, getDbObject, getSequelizeTransaction, isRegionUserExecutive, formatSuccessResponse, formatErrorResponse, throwError } from '../../../../utils/helpers';
import * as requestValidationSchema from "./request";

export default async (request) => {
	let transaction;
	try{
		// Authenticate Session Token
		const tokenData = await authenticateSessionToken(request);
    const userId = tokenData.UserName;

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { regionId } = path;
		const { LinkId, CategoryId, LinkTitle, LinkAddress, LinkDescription } = body;

		// Verify if the user has access to the region
    const regionUserModel = await getDbObject('RegionUser', true, request);
    const regionUser = await regionUserModel.findOne({ where: { RegionId: regionId, RealUsername: userId } });
    if(!regionUser){
      await throwError(403, 'User does not have access to the region');
    }

    // Verify if the user is executive
    const isExecutive = await isRegionUserExecutive(regionUser);
    if(!isExecutive){
      await throwError(403, 'User does not have the required permissions to create or update links');
    }

    // Determine if we need to create or update the link
    const isCreate = !LinkId;

    // Get the Link model
    const linkModel = await getDbObject('Link', true, request);

    // Start a new transaction
    transaction = await getSequelizeTransaction(request);

    let link = null;
    if(isCreate){ // Create the link
      const generatedLinkId = uuidv4();
      link = await linkModel.create({ RegionId: regionId, LinkId: generatedLinkId, CategoryId, LinkTitle, LinkAddress, LinkDescription }, { transaction });
    }
    else{ // Update the link
      await linkModel.update({ CategoryId, LinkTitle, LinkAddress, LinkDescription }, { where: { RegionId: regionId, LinkId }, transaction });
      link = await linkModel.findOne({ where: { RegionId: regionId, LinkId }, transaction });
    }

    // Commit the transaction
    await transaction.commit();

		return formatSuccessResponse(request, {
			data: { ...link.toJSON() },
		});
	}
	catch(err){
		// Rollback transaction if it was started
		if(transaction){
			try {
				await transaction.rollback();
			} catch (rollbackError) {
				console.error('Error rolling back transaction:', rollbackError);
			}
		}
		console.error(err);
		return formatErrorResponse(request, err);
	}
}
