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

		const { regionId, username } = path;

    const regionUserModel = await getDbObject('RegionUser', true, request);
    const regionUser = await regionUserModel.findOne({ where: { RegionId: regionId, RealUsername: userId }});

		// Check if user has access to the region
    if(!regionUser){
      await throwError(403, `User ${userId} does not have access to region ${regionId}`);
    }

		// Verify if the user is executive (checks both IsExecutive flag and positions)
    const isExecutive = await isRegionUserExecutive(regionUser);
    if(!isExecutive){
      await throwError(403, 'User does not have the required permissions to delete region users');
    }

    // Get User to Delete
    transaction = await getSequelizeTransaction(request); // Start a new transaction
    const userToDelete = await regionUserModel.findOne({ where: { RegionId: regionId, Username: username }, transaction });

    if(!userToDelete){
      await throwError(404, `User ${username} not found in region ${regionId}`);
    }

    // Delete User
    await userToDelete.destroy({ transaction });

    // Commit the transaction
    await transaction.commit();

		return formatSuccessResponse(request, {
			data: { message: 'Region user deleted successfully' },
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
