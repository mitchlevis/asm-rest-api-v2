import Joi from 'joi';

const path = {

};

const query = {
  force_refresh: Joi.boolean().optional().default(false),
};

const body = {

};

export default {
  path,
  query,
  body
};