import Joi from 'joi';

const path = {
  userId: Joi.string().required(),
  regionId: Joi.string().required(),
  scheduleId: Joi.string().required()
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