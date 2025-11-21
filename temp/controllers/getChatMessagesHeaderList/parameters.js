import Joi from 'joi';

const path = {
  userId: Joi.string().required(),
};

const query = {
  sort_direction: Joi.string().optional().valid('ASC', 'DESC').default('DESC'),
  limit: Joi.number().optional().default(-1),
  offset: Joi.number().optional().default(0),
};

const body = {

};

export default {
  path,
  query,
  body
};