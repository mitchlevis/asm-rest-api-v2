import Joi from 'joi';

const path = {
  regionId: Joi.string().optional().allow('').empty('').default(null),
};

const query = {
  year: Joi.number().integer().min(1900).max(2100).optional().default(() => new Date().getFullYear()),
  month: Joi.number().integer().min(1).max(12).optional().default(() => new Date().getMonth() + 1),
  rank_type: Joi.string().optional().default(null),
  filter: Joi.string().optional().allow('').empty('').default(null), 
  show_available: Joi.boolean().optional().default(true),
  show_unavailable: Joi.boolean().optional().default(false),
  show_partially_available: Joi.boolean().optional().default(false),
  show_not_filled_in: Joi.boolean().optional().default(false),
};

const body = {

};

export default {
  path,
  query,
  body
};