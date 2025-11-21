import Joi from 'joi';

const path = {
  userId: Joi.string().required(),
  regionId: Joi.string().optional().allow('').empty('').default(null),
};

const query = {
  sort_direction: Joi.string().optional().valid('ASC', 'DESC').default('DESC'),
};

const body = {

};

export default {
  path,
  query,
  body
};