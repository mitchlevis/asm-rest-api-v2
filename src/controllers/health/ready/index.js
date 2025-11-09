import { formatSuccessResponse, formatErrorResponse, validateIncomingParameters, getSequelizeObject } from '../../../utils/helpers';
import * as requestValidationSchema from "./request";
import * as responseValidationSchema from "./response";

export default async (request) => {
	try{
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const sequelize = await getSequelizeObject(request);

		if (sequelize) {
			try {
				await sequelize.authenticate();
				return formatSuccessResponse(request, {
					data: {
					ready: true,
					database: 'connected',
					timestamp: new Date().toISOString(),
				},
				responseSchema: responseValidationSchema.success,
			});
			} catch (error) {
				return formatErrorResponse(request, error);
			}
		}

		// If no sequelize, return not configured
		return formatSuccessResponse(request, {
			data: {
				ready: true,
				database: 'not configured',
				timestamp: new Date().toISOString(),
			},
			responseSchema: responseValidationSchema.success,
		});
	} catch (error) {
		console.error(error);
		return formatErrorResponse(request, error);
	}
};
