import { authenticateSessionToken, validateIncomingParameters, getDbObject, formatSuccessResponse, formatErrorResponse, throwError, replaceOperators } from '../../../../utils/helpers';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		// Authenticate Session Token
		const tokenData = await authenticateSessionToken(request);
		const userId = tokenData.UserName;

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { regionId } = path;
		let { filter, sort, sort_direction: sortDirection, limit, offset } = query;

		// Normalize limit/offset: Sequelize 7 requires valid values for MSSQL
		// -1 means "no limit", 0 means "no offset" (when limit is also not set)
		if (limit === -1) {
			limit = undefined;
		}
		if (offset === 0 && limit === undefined) {
			offset = undefined;
		}

		// Formatting
		let formattedFilter;
		if(filter){
			formattedFilter = replaceOperators(filter);
		}

		// Format sortingArray for Sequelize order clause
		// sort can be a string ('FirstName') or an array (['FirstName'])
		// Sequelize expects an array of arrays: [['FirstName', 'ASC']]
		let sortingArray;
		if (sort) {
			if (typeof sort === 'string') {
				sortingArray = [[sort, sortDirection]];
			} else if (Array.isArray(sort)) {
				sortingArray = sort.map((sortField) => [sortField, sortDirection]);
			}
		} else {
			sortingArray = [['FirstName', sortDirection]];
		}

		// Get region for user
		const regionUserModel = await getDbObject('RegionUser', true, request);
		const regionUser = await regionUserModel.findOne({
			where: { RegionId: regionId, RealUsername: userId },
			raw: true
		});

		// If no region, return 404
		if(!regionUser){
			console.log(`User ${userId} does not have a region`);
			await throwError(404, `User region not found`);
		}

		// Permissions
		if(!regionUser.IsExecutive && !regionUser.CanViewMasterSchedule){
			await throwError(403, `User ${userId} does not have permission to view region users`);
		}

		/*
			Get Region Users
		*/
		const queryOptions = {
			where: {
				...formattedFilter,
				RegionId: regionId
			},
			order: sortingArray,
			raw: true
		};

		// Only include limit/offset if they have valid values (not undefined/null)
		// MSSQL requires limit to be > 0 when offset is present
		if (limit !== undefined && limit !== null && limit > 0) {
			queryOptions.limit = limit;
			if (offset !== undefined && offset !== null && offset >= 0) {
				queryOptions.offset = offset;
			}
		}

		const { count: regionUsersTotalCount, rows: regionUsers } = await regionUserModel.findAndCountAll(queryOptions);

		return formatSuccessResponse(request, { data: { regionUsers: regionUsers, totalCount: regionUsersTotalCount } });
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
};
