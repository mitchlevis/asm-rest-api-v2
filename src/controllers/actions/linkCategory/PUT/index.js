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
    const { CategoryId, CategoryName, CategoryDescription, CategoryColor, SortOrder } = body;

		// Verify if the user has access to the region
    const regionUserModel = await getDbObject('RegionUser', true, request);
    const regionUser = await regionUserModel.findOne({ where: { RegionId: regionId, RealUsername: userId } });
    if(!regionUser){
      await throwError(403, 'User does not have access to the region');
    }

    // Verify if the user is executive
    const isExecutive = await isRegionUserExecutive(regionUser);
    if(!isExecutive){
      await throwError(403, 'User does not have the required permissions to create or update link categories');
    }

    // Determine if we need to create or update the category
    const isCreate = !CategoryId;

    // Get the LinkCategory model
    const linkCategoryModel = await getDbObject('LinkCategory', true, request);

    let category = null;
    if(isCreate){ // Create the category
      // If the SortOrder is not provided, get the max sort order for the region
      let sortOrder = SortOrder;
      if(!sortOrder){
        // Get the max sort order for the region & increment by 1
        const maxSortOrder = await linkCategoryModel.max('SortOrder', { where: { RegionId: regionId } });
        sortOrder = maxSortOrder ? maxSortOrder + 1 : 1;
      }

      transaction = await getSequelizeTransaction(request); // Start a new transaction
      category = await linkCategoryModel.create({ RegionId: regionId, CategoryName, CategoryDescription, CategoryColor, SortOrder: sortOrder }, { transaction });
    }
    else{ // Update the category
      transaction = await getSequelizeTransaction(request); // Start a new transaction
      await linkCategoryModel.update({ CategoryName, CategoryDescription, CategoryColor, SortOrder: SortOrder || undefined }, { where: { RegionId: regionId, CategoryId }, transaction });
    }
    // Commit the transaction
    await transaction.commit();

    // Get the category again to get the updated category
    if(!category){
      category = await linkCategoryModel.findOne({ where: { RegionId: regionId, CategoryId } });
    }

		return formatSuccessResponse(request, {
			data: { ...category.toJSON() },
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
