import Joi from 'joi';

const path = {
  regionId: Joi.string().optional().allow('').empty('').default(null),
};

const query = {
  year: Joi.number().integer().min(1900).max(2100).optional().default(() => new Date().getFullYear()),
  month: Joi.number().integer().min(1).max(12).optional().default(() => new Date().getMonth() + 1),
};

const body = {

};

export default {
  path,
  query,
  body
};