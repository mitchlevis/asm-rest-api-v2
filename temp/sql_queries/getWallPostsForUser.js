
export const getQuery = (userIdArray, sortDirection, max, offset) => {
	console.log('userIdArray, sortDirection, max',userIdArray, sortDirection, max);
		const SELECT = max !== -1 && offset === 0 ? `SELECT TOP (${max})` : 'SELECT';
    const inClause = userIdArray.map((userId) => `'${userId}'`).join(',');
  return `
  ${ SELECT }
  wp.UserId,
	u.FirstName,
	u.LastName,
  u.PhotoId,
  wp.WallPostId,
  wp.Post,
  wp.Link,
  wp.PostDate,
  wp.PostType,
  Tags = (
    SELECT
      wpt.UserId,
			u.FirstName,
      u.LastName,
      u.PhotoId,
      wpt.WallPostId,
      wpt.TaggedId
    FROM WallPostTags wpt
		JOIN [User] u ON u.Username = wpt.TaggedId
    WHERE wpt.UserId = wp.UserId AND wpt.WallPostId = wp.WallPostId
    FOR JSON PATH
  ),
  Likes = (
    SELECT
      wpl.UserId,
			u.FirstName,
      u.LastName,
      u.PhotoId,
      wpl.WallPostId,
      wpl.LikerId
    FROM WallPostLike wpl
		JOIN [User] u ON u.Username = wpl.LikerId
    WHERE wpl.UserId = wp.UserId AND wpl.WallPostId = wp.WallPostId
    FOR JSON PATH
  ),
  Comments = (
    SELECT
      wpc.UserId,
			u.FirstName,
      u.LastName,
      u.PhotoId,
      wpc.WallPostId,
      wpc.CommentId,
      wpc.Comment,
      wpc.CommenterId,
      wpc.CommentDate,
      Likes = (
        SELECT
          wpcl.UserId,
					u.FirstName,
          u.LastName,
          u.PhotoId,
          wpcl.WallPostId,
          wpcl.CommentId,
          wpcl.CommentLikerId
        FROM WallPostCommentLike wpcl
				JOIN [User] u ON u.Username = wpcl.CommentLikerId
        WHERE wpcl.UserId = wpc.UserId AND wpcl.WallPostId = wpc.WallPostId AND wpcl.CommentId = wpc.CommentId
        FOR JSON PATH
      ),
      Comments = (
        SELECT
          wpcc.UserId,
					u.FirstName,
          u.LastName,
          u.PhotoId,
          wpcc.WallPostId,
          wpcc.CommentId,
          wpcc.CommentCommentId,
          wpcc.Comment,
          wpcc.CommenterId,
          wpcc.CommentDate,
          Likes = (
            SELECT
              wpccl.UserId,
              u.FirstName,
              u.LastName,
              u.PhotoId,
              wpccl.WallPostId,
              wpccl.CommentId,
              wpccl.CommentCommentId,
              wpccl.CommentLikerId
            FROM WallPostCommentCommentLike wpccl
            JOIN [User] u ON u.Username = wpccl.CommentLikerId
            WHERE wpccl.UserId = wpcc.UserId AND wpccl.WallPostId = wpcc.WallPostId AND wpccl.CommentId = wpcc.CommentId AND wpccl.CommentCommentId = wpcc.CommentCommentId
            FOR JSON PATH
          )
        FROM WallPostCommentComment wpcc
				JOIN [User] u ON u.Username = wpcc.CommenterId
        WHERE wpcc.UserId = wpc.UserId AND wpcc.WallPostId = wpc.WallPostId AND wpcc.CommentId = wpc.CommentId
        FOR JSON PATH
      )
    FROM WallPostComment wpc
		JOIN [User] u ON u.Username = wpc.CommenterId
    WHERE wpc.UserId = wp.UserId AND wpc.WallPostId = wp.WallPostId
    FOR JSON PATH
  )
FROM WallPost wp
JOIN [User] u ON u.Username = wp.UserId
WHERE wp.UserId IN (${inClause.toString()})
ORDER BY wp.PostDate ${sortDirection}
${offset !== 0 ? `OFFSET ${offset} ROWS`: ''}
${offset !== 0 && max !== -1 ? `FETCH NEXT ${max} ROWS ONLY` : ''}
FOR JSON PATH;

  `;
}

export const formatResult = async (result) => {
    if(!result || result.length === 0){
			return [];
    }
		
    let jsonString = '';
    for(const row of result){
        jsonString += row[Object.keys(row)[0]];
    }
    return JSON.parse(jsonString);
};
