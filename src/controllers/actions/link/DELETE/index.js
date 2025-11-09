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

		const { regionId, linkId } = path;

		// Verify if the user has access to the region
    const regionUserModel = await getDbObject('RegionUser', true, request);
    const regionUser = await regionUserModel.findOne({ where: { RegionId: regionId, RealUsername: userId } });
    if(!regionUser){
      await throwError(403, 'User does not have access to the region');
    }

    // Verify if the user is executive
    const isExecutive = await isRegionUserExecutive(regionUser);
    if(!isExecutive){
      await throwError(403, 'User does not have the required permissions to delete links');
    }

    // Get the Link model
    transaction = await getSequelizeTransaction(request); // Start a new transaction
    const linkModel = await getDbObject('Link', true, request);
    const link = await linkModel.findOne({ where: { RegionId: regionId, LinkId: linkId }, transaction });
    if(!link){
      await throwError(404, 'Link not found');
    }

    // Delete the link
    await link.destroy({ transaction });

    // Commit the transaction
    await transaction.commit();

		return formatSuccessResponse(request, {
			data: { message: 'Link deleted successfully' },
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
