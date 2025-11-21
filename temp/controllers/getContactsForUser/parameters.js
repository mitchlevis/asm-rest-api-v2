import Joi from 'joi';

const path = {
  userId: Joi.string().required(),
  regionId: Joi.string().optional().allow('').empty('').default(null),
};

const query = {
  sort_direction: Joi.string().optional().valid('ASC', 'DESC').default('ASC'),
  limit: Joi.number().optional().default(-1),
  offset: Joi.number().optional().default(0),
  show_archived: Joi.boolean().optional().default(false)
};

const body = {

};

export default {
  path,
  query,
  body
};