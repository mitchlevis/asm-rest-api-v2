import Joi from 'joi';

const path = {

};

const query = {
  sort_direction: Joi.string().optional().valid('ASC', 'DESC').default('ASC'),
  limit: Joi.number().integer().min(1).max(200).optional().default(50),
  offset: Joi.number().integer().min(0).optional().default(0),
};

const body = {
};

export default {
  path,
  query,
  body
};


