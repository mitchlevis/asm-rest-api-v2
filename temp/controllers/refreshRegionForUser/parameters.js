import Joi from 'joi';

const path = {
  regionId: Joi.string().required(),
};

const query = {
  camel_case: Joi.boolean().optional().default(false),
};

const body = {

};

export default {
  path,
  query,
  body
};