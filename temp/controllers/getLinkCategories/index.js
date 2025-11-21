import dayjs from 'dayjs';
import parameters from "./parameters";
import { authenticateSessionToken, validateIncomingParameters, throwError, getFilterQueryParameter, replaceOperators, getSequelizeObject, getDbObject, formatSuccessResponse, formatErrorResponse, buildFacetValues } from "@asportsmanager-api/core/helpers";
import buildFacetSpecs from './facets';
export const handler = async (_evt) => {
  try{
    const tokenData = await authenticateSessionToken(_evt);
    const userId = tokenData.UserName;

    // Validate Parameters
    const { path, query, body } = await validateIncomingParameters(_evt, parameters);
    
    const { regionId } = path;
    
    const filter = getFilterQueryParameter(_evt.queryStringParameters)
    const sort = query.sort.split(',');
    const sortDirection = query.sort_direction;
    const limit = query.limit;
    const offset = query.offset;
    const includeFacets = query.include_facets;
    const facetLimit = query.facet_limit;

    // Formatting
    let formattedFilter;
    if(filter){
      formattedFilter = replaceOperators(filter);
    }

    let sortingArray;
    if(sort){
      sortingArray = sort.map((sortField) => [sortField, sortDirection]);
    }

    const regionUserModel = await getDbObject('RegionUser');
    const whereObject = { RealUsername: userId };
    if(regionId){
      whereObject.regionId = regionId;
    }

    // Get all regions for user
    const regionsForUser = await regionUserModel.findAll({ attributes: ['RegionId'], where: whereObject});
    const regionIds = regionsForUser.map((region) => region.RegionId);

    // If no regions, return 403 Forbidden
    if(regionIds.length === 0){
      await throwError(403, `User does not have access to ${regionId ? 'this': 'these'} region${regionId ? '': 's'}`);
    }

    // Get all link categories for regions
    const linkCategoryModel = await getDbObject('LinkCategory');
    const regionModel = await getDbObject('Region');

    const sequelize = await getSequelizeObject();
    const { Op } = sequelize.Sequelize;
    
    const where = { 
      ...formattedFilter,
      RegionId: { [Op.in]: regionIds } 
    };

    const include = [
      {
        model: regionModel,
        as: 'Region',
        required: false,
        attributes: []
      }
    ];

    const result = await linkCategoryModel.findAndCountAll({ 
      attributes: [
        'RegionId',
        [sequelize.Sequelize.literal('[Region].[RegionName]'), 'RegionName'],
        'CategoryId',
        'CategoryName',
        'CategoryDescription',
        'CategoryColor',
        'SortOrder'
      ],
      where,
      include,
      order: sortingArray,
      limit,
      offset,
    });

    // Facets - Contextual Facet Values
    let facets = undefined;
    if(includeFacets){
      const facetSpecs = buildFacetSpecs();
      facets = await buildFacetValues({ model: linkCategoryModel, where, include, facetSpecs, limit: facetLimit, sequelize });
    }

    return formatSuccessResponse(_evt, {totalCount: result.count, linkCategories: result.rows, ...(facets ? { facets } : {})}, 200);
  }
  catch(err){
    console.error(err);
    return formatErrorResponse(_evt.headers.origin, err);
  }
};