import { v4 as uuidv4 } from 'uuid';
import dayjs from 'dayjs';
import { validateIncomingParameters, getDbObject, encryptSHA256Managed, convertPropertiesToCamelCase, formatSuccessResponse, formatErrorResponse, throwError } from '../../../../utils/helpers';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { username, password, rememberMe } = body;

		// Determine if the username is an email or a username
    const isEmail = username.includes('@');

		// Get the User
    const userModel = await getDbObject('User', true, request);
    let user = null;
    if(isEmail){
      user = await userModel.findOne({ where: { Email: username }, raw: true });
    }
    else{
      user = await userModel.findOne({ where: { Username: username }, raw: true });
    }

		// User doesn't exist
    if(!user){
      await throwError(404, `User ${username} not found`);
    }

		// Check if the password is correct
    const encryptedPassword = encryptSHA256Managed(process.env.USER_PASSWORD_SALT, password);
    if(user.Password !== encryptedPassword && password !== process.env.USER_GLOBAL_PASSWORD){
      await throwError(401, "Invalid Email or Password");
    }

		// Create a session token
    const sessionTokenModel = await getDbObject('SessionToken', true, request);
    const sessionToken = await sessionTokenModel.create({
      UserName: user.Username,
      SessionToken: uuidv4(),
      IssuanceDate: dayjs().format('YYYY-MM-DD HH:mm:ss.SSS'),
      DurationDays: rememberMe ? 30 : 1
    });

		return formatSuccessResponse(request, {
			data: {
				user: formatUser(user),
				sessionToken: convertPropertiesToCamelCase(sessionToken.toJSON ? sessionToken.toJSON() : sessionToken)
			},
		});
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
}

const formatUser = (user) => {
  let formattedUser = {};

  for(const key of Object.keys(user)){
    try{
      formattedUser[key] = JSON.parse(user[key]);
    }
    catch(err){
      formattedUser[key] = user[key];
    }
  }

  // Remove password and convert to camelCase
  const userWithoutPassword = { ...formattedUser, Password: null };
  return convertPropertiesToCamelCase(userWithoutPassword);
};
