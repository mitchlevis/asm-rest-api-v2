import Joi from 'joi';

const path = {
  regionId: Joi.string().optional().allow('').empty('').default(null),
};

const query = {
  show_archived: Joi.boolean().optional().default(false),
  force_refresh: Joi.boolean().optional().default(false),
};

const body = {

};

export default {
  path,
  query,
  body
};