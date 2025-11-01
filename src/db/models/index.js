import { RegionUser, RegionUserAssociations } from './RegionUser';
import { User, UserAssociations } from './User';
import { SessionToken, SessionTokenAssociations } from './SessionToken';

const MODELS = {
 RegionUser,
 User,
 SessionToken,
}

const MODEL_ASSOCIATIONS = {
 RegionUser: RegionUserAssociations,
 User: UserAssociations,
 SessionToken: SessionTokenAssociations,
}

export { MODELS, MODEL_ASSOCIATIONS };
