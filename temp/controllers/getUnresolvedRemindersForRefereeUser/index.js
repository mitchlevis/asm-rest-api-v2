import { Config } from "sst/node/config";
import parameters from "./parameters";
import { authenticate, validateIncomingParameters, throwError, getSequelizeObject, getHeaders, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { getQuery as getQueryNew, formatResult as formatResultNew }  from "@asportsmanager-api/core/sql_queries/getUnresolvedRemindersNew";
import { getQuery as getQueryChanged, formatResult as formatResultChanged }  from "@asportsmanager-api/core/sql_queries/getUnresolvedRemindersChanged";
import { getQuery as getQueryCancelled, formatResult as formatResultCancelled }  from "@asportsmanager-api/core/sql_queries/getUnresolvedRemindersCancelled";

export const handler = async (_evt) => {
  try{
    await authenticate(_evt, Config.API_KEYS);

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);

    const { userId } = path;
    const sortDirection = query.sort_direction;
    
    if(!userId || userId === ''){
      await throwError(400, 'Invalid Request. userId is required');
    }

    const newRemindersQuery = getQueryNew(userId, sortDirection);
    const changedRemindersQuery = getQueryChanged(userId, sortDirection);
    const cancelledRemindersQuery = getQueryCancelled(userId, sortDirection);

    const sequelize = await getSequelizeObject();

    const [
      newRemindersResults,
      changedRemindersResults,
      cancelledRemindersResults
    ] = await Promise.all([
      sequelize.query(newRemindersQuery, { type: sequelize.QueryTypes.SELECT }),
      sequelize.query(changedRemindersQuery, { type: sequelize.QueryTypes.SELECT }),
      sequelize.query(cancelledRemindersQuery, { type: sequelize.QueryTypes.SELECT }),
    ]);

    const response = {
      new: formatResultNew(newRemindersResults),
      changed: formatResultChanged(changedRemindersResults),
      cancelled: formatResultCancelled(cancelledRemindersResults),
    };

    return formatSuccessResponse(_evt, response, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};