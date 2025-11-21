import { authenticateSessionToken, validateIncomingParameters, getDbObject, formatSuccessResponse, formatErrorResponse, throwError } from '../../../../utils/helpers';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		// Authenticate Session Token
		const tokenData = await authenticateSessionToken(request);
		const userId = tokenData.UserName;

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { regionId } = path;

		const regionUserModel = await getDbObject('RegionUser', true, request);

		// Get region for user
		const regionUser = await regionUserModel.findOne({
			where: { RegionId: regionId, RealUsername: userId },
			raw: true
		});

		// If no region, return 404
		if(!regionUser){
			console.log(`User ${userId} is not a member of region ${regionId}`);
			await throwError(404, `Region User not found`);
		}

		// Permissions
		if(!regionUser.IsExecutive && !regionUser.CanViewMasterSchedule){
			await throwError(403, `User ${userId} does not have permission to view region users`);
		}

		// Region Users
		const regionUsers = await regionUserModel.findAll({
			attributes: [
				'RegionId',
				'Username',
				'RealUsername',
				'FirstName',
				'LastName',
				'Email',
				'PhoneNumbers',
				'Country',
				'City',
				'Address',
				'PostalCode',
				'PreferredLanguage',
				'Positions',
				'AlternateEmails',
				'PublicData',
				'PrivateData',
				'InternalData',
				'GlobalAvailabilityData',
			],
			where: {
				RegionId: regionId
			},
			raw: true
		});

		// Distinct Values & remove duplicates
		const distinctValues = {
			username: [...new Set(regionUsers.map(user => user.Username).filter(value => value))]
				.sort()
				.map(value => ({ value })),
			realUsername: [...new Set(regionUsers.map(user => user.RealUsername).filter(value => value))]
				.sort()
				.map(value => ({ value })),
			firstName: [...new Set(regionUsers.map(user => user.FirstName).filter(value => value))]
				.sort()
				.map(value => ({ value })),
			lastName: [...new Set(regionUsers.map(user => user.LastName).filter(value => value))]
				.sort()
				.map(value => ({ value })),
			email: [...new Set(regionUsers.map(user => user.Email).filter(value => value))]
				.sort()
				.map(value => ({ value })),
			phoneNumbers: [...new Set(
					regionUsers.flatMap(user => {
						if (!user.PhoneNumbers) return [];
						try {
							const parsedNumbers = JSON.parse(user.PhoneNumbers);
							if (Array.isArray(parsedNumbers)) {
								return parsedNumbers.map(num => num.PhoneNumberNumber).filter(value => value);
							}
							return [];
						} catch (e) {
							console.error(`Failed to parse PhoneNumbers for user ${user.RealUsername || user.Username}:`, user.PhoneNumbers, e);
							return [];
						}
					})
				)]
				.sort()
				.map(value => ({ value })),
			country: [...new Set(regionUsers.map(user => user.Country).filter(value => value))]
				.sort()
				.map(value => ({ value })),
			city: [...new Set(regionUsers.map(user => user.City).filter(value => value))]
				.sort()
				.map(value => ({ value })),
			address: [...new Set(regionUsers.map(user => user.Address).filter(value => value))]
				.sort()
				.map(value => ({ value })),
			postalCode: [...new Set(regionUsers.map(user => user.PostalCode).filter(value => value))]
				.sort()
				.map(value => ({ value })),
			preferredLanguage: [...new Set(regionUsers.map(user => user.PreferredLanguage).filter(value => value))]
				.sort()
				.map(value => ({ value })),
			positions: [...new Set(
					regionUsers.flatMap(user => {
						if (!user.Positions) return [];
						try {
							const parsedPositions = JSON.parse(user.Positions);
							if (Array.isArray(parsedPositions)) {
								return parsedPositions.filter(value => value); // Filter out any potentially falsy values within the array
							}
							return [];
						} catch (e) {
							console.error(`Failed to parse Positions for user ${user.RealUsername || user.Username}:`, user.Positions, e);
							return [];
						}
					})
				)]
				.sort()
				.map(value => ({ value })),
			alternateEmails: [...new Set(
					regionUsers.flatMap(user => {
						if (!user.AlternateEmails) return [];
						try {
							const parsedEmails = JSON.parse(user.AlternateEmails);
							if (Array.isArray(parsedEmails)) {
								return parsedEmails.filter(value => value);
							}
							return [];
						} catch (e) {
							console.error(`Failed to parse AlternateEmails for user ${user.RealUsername || user.Username}:`, user.AlternateEmails, e);
							return [];
						}
					})
				)]
				.sort()
				.map(value => ({ value })),
			publicData: [...new Set(regionUsers.map(user => user.PublicData).filter(value => value))]
				.sort()
				.map(value => ({ value })),
			privateData: [...new Set(regionUsers.map(user => user.PrivateData).filter(value => value))]
				.sort()
				.map(value => ({ value })),
			internalData: [...new Set(regionUsers.map(user => user.InternalData).filter(value => value))]
				.sort()
				.map(value => ({ value })),
			globalAvailabilityData: [...new Set(regionUsers.map(user => user.GlobalAvailabilityData).filter(value => value))]
				.sort()
				.map(value => ({ value })),
		};

		return formatSuccessResponse(request, { data: { regionId, distinctValues } });
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
};

