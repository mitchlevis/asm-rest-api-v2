import Joi from 'joi';

const path = {
  regionId: Joi.string().optional().allow('').empty('').default(null),
};

const query = {
  filter: Joi.string().optional().allow('').empty('').default(null),
  sort: Joi.string().optional().allow('').empty('').default('SortOrder'),
  sort_direction: Joi.string().optional().valid('ASC', 'DESC').default('ASC'),
  limit: Joi.number().optional().default(25),
  offset: Joi.number().optional().default(0),
  include_facets: Joi.boolean().optional().default(false),
  facet_limit: Joi.number().optional().default(50),
};

const body = {

};

export default {
  path,
  query,
  body
};