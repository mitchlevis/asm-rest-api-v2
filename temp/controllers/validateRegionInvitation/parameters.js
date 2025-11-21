import Joi from 'joi';

const path = {
  invitationId: Joi.string().required(),
  email: Joi.string().required(),
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