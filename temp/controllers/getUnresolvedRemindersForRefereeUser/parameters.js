import Joi from 'joi';

const path = {
  userId: Joi.string().required(),
};

const query = {
  sort_direction: Joi.string().optional().valid('ASC', 'DESC').default('ASC'),
};

const body = {

};

export default {
  path,
  query,
  body
};