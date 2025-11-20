import Joi from 'joi';

const path = {
  regionId: Joi.string().optional().allow('').empty('').default(null),
};

const query = {
  filter: Joi.string().optional().allow('').empty('').default(null),
  sort: Joi.string().optional().allow('').empty('').default('GameDate'),
  sort_direction: Joi.string().uppercase().regex(/^ASC|DESC$/).optional().valid('ASC', 'DESC').default('DESC'),
  limit: Joi.number().optional().default(25),
  offset: Joi.number().optional().default(0),
  show_archived: Joi.boolean().optional().default(false),
  show_only_open: Joi.boolean().optional().default(false),
  show_deleted: Joi.boolean().optional().default(false),
  show_removed: Joi.boolean().optional().default(false),
  group_parks: Joi.boolean().optional().default(false),
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