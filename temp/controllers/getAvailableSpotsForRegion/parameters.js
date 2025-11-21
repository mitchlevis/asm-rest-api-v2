import Joi from 'joi';

const path = {
  regionId: Joi.string().required(),
};

const query = {
  filter: Joi.string().optional().allow('').empty('').default(null),
  sort: Joi.string().optional().allow('').empty('').default('GameDate'),
  sort_direction: Joi.string().uppercase().regex(/^ASC|DESC$/).optional().valid('ASC', 'DESC').default('DESC'),
  limit: Joi.number().optional().default(25),
  offset: Joi.number().optional().default(0),
  group_parks: Joi.boolean().optional().default(false),
};

const body = {

};

export default {
  path,
  query,
  body
};