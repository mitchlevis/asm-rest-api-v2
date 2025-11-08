import { FriendNotification, FriendNotificationAssociations } from './FriendNotification';
import { Region, RegionAssociations } from './Region';
import { RegionUser, RegionUserAssociations } from './RegionUser';
import { User, UserAssociations } from './User';
import { SessionToken, SessionTokenAssociations } from './SessionToken';
import { WallPost, WallPostAssociations } from './WallPost';
import { WallPostComment, WallPostCommentAssociations } from './WallPostComment';
import { WallPostCommentComment, WallPostCommentCommentAssociations } from './WallPostCommentComment';
import { WallPostCommentCommentLike, WallPostCommentCommentLikeAssociations } from './WallPostCommentCommentLike';
import { WallPostCommentLike, WallPostCommentLikeAssociations } from './WallPostCommentLike';
import { WallPostLike, WallPostLikeAssociations } from './WallPostLike';

const MODELS = {
 FriendNotification,
 Region,
 RegionUser,
 User,
 SessionToken,
 WallPost,
 WallPostComment,
 WallPostCommentComment,
 WallPostCommentCommentLike,
 WallPostCommentLike,
 WallPostLike,
}

const MODEL_ASSOCIATIONS = {
 FriendNotification: FriendNotificationAssociations,
 Region: RegionAssociations,
 RegionUser: RegionUserAssociations,
 User: UserAssociations,
 SessionToken: SessionTokenAssociations,
 WallPost: WallPostAssociations,
 WallPostComment: WallPostCommentAssociations,
 WallPostCommentComment: WallPostCommentCommentAssociations,
 WallPostCommentCommentLike: WallPostCommentCommentLikeAssociations,
 WallPostCommentLike: WallPostCommentLikeAssociations,
 WallPostLike: WallPostLikeAssociations,
}

export { MODELS, MODEL_ASSOCIATIONS };
