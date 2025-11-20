
export const getQuery = (userId, sortDirection, max, offset) => {
	const SELECT = max !== -1 && offset === 0 ? `SELECT TOP (${max})` : 'SELECT';
  return `
DECLARE @PageSize INT = ${max === -1 ? 2147483647: max }; -- A large number to effectively fetch all remaining rows
  
;WITH RankedMessages AS (
      SELECT
        [ChatMessage].*, 
        [User].[FirstName], 
        [User].[LastName],
             ROW_NUMBER() OVER (
                 PARTITION BY 
                     CASE 
                         WHEN SenderId < ReceiverId THEN SenderId 
                         ELSE ReceiverId 
                     END, 
                     CASE 
                         WHEN SenderId < ReceiverId THEN ReceiverId 
                         ELSE SenderId 
                     END
                 ORDER BY DateSent DESC
             ) AS rn
      FROM ChatMessage
      INNER JOIN [User] ON [User].[Username] = [ChatMessage].[FriendId]
      WHERE [ChatMessage].[Username] = '${userId}'
  )
  
  ${SELECT}
    FirstName, LastName, [RankedMessages].Username, FriendId, MessageId, FriendMessageId, ReceiverId, SenderId, DateSent, Message
  FROM RankedMessages
  WHERE rn = 1
  ORDER BY DateSent ${sortDirection}
  ${offset !== 0 ? `OFFSET ${offset} ROWS`: ''}
  ${offset !== 0 && max !== -1 ? `FETCH NEXT ${max} ROWS ONLY` : ''};
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
