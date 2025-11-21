import { Config } from "sst/node/config";
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, getSequelizeObject, formatSuccessResponse, formatErrorResponse } from "@asportsmanager-api/core/helpers";
import { getQuery as getOptimizedUnionQuery } from "@asportsmanager-api/core/sql_queries/getUnresolvedRemindersOptimizedUnion";

export const handler = async (_evt) => {
  try {
    const tokenData = await authenticateSessionToken(_evt);
    const userId = tokenData.UserName;

    const { query } = await validateIncomingParameters(_evt, parameters);

    const sortDirection = query.sort_direction; // already validated to ASC|DESC with default
    let { limit, offset } = query;

    // Enforce caps/sanity
    if (limit == null || Number.isNaN(limit)) limit = 50;
    if (offset == null || Number.isNaN(offset)) offset = 0;
    if (limit > 200) limit = 200;
    if (offset < 0) offset = 0;

    const { sql, replacements } = getOptimizedUnionQuery(sortDirection, { userId, limit, offset });

    const sequelize = await getSequelizeObject();

    const rows = await sequelize.query(sql, {
      type: sequelize.QueryTypes.SELECT,
      replacements,
    });

    // Split into categories and post-process Positions CSV -> array
    const response = { new: [], changed: [], cancelled: [] };
    for (const row of rows) {
      const item = { ...row };
      // Clean helper fields
      delete item.rn;
      // Normalize Positions to array
      item.Positions = item?.Positions ? String(item.Positions).split(',') : [];
      if (row.NotificationType === 'new') response.new.push(item);
      else if (row.NotificationType === 'changed') response.changed.push(item);
      else if (row.NotificationType === 'cancelled') response.cancelled.push(item);
    }

    return formatSuccessResponse(_evt, response, 200);
  } catch (err) {
    console.error(err);
    return formatErrorResponse(_evt.headers?.origin, err);
  }
};


