import Joi from 'joi';

const path = {
  regionId: Joi.string().required(),
  scheduleId: Joi.string().required(),
};

const query = {

};

const body = {

};

export default {
  path,
  query,
  body
};