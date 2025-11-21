Imports System.Data.SqlClient
Imports System.Net.Mail
Imports System.Net.Http
Imports System.Net
Imports System.Web.Http
Imports Newtonsoft.Json

Public Class FriendNotification
    Public Property Username As String
    Public Property FullUsername As String
    Public Property FriendId As String
    Public Property FriendUsername As String
    Public Property FriendName As String
    Public Property FriendSport As String
    Public Property FriendLadderLeague As Boolean
    Public Property FriendEntityType As String
    Public Property FriendSeason As Integer
    Public Property DateCreated As DateTime
    Public Property IsViewed As Boolean
    Public Property Positions As List(Of String)
    Public Property Denied As Boolean
    Public Property FriendCountry As String
    Public Property FriendState As String
    Public Property FriendCity As String
    Public Property FriendAddress As String
    Public Property FriendPostalCode As String

    Public Shared Function GetTop5FriendNotification(Username As String) As Object
        Dim FriendNotifications As List(Of FriendNotification) = Nothing

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    FriendNotifications = GetTop5FriendNotificationHelper(Username, SqlConnection, SqlTransaction)
                End Using
            End Using
            Return New With {
                .Success = True,
                .FriendNotifications = FriendNotifications
            }
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function GetTop5FriendNotificationHelper(Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of FriendNotification)
        Dim FriendNotifications As New List(Of FriendNotification)

        Dim CommandText = <SQL>
SELECT TOP 5
    Username, FullUsername, FriendId, FriendUsername, RegionName, Sport, CAST(_FriendNotification.RegionIsLadderLeague AS BIT) AS RegionIsLadderLeague, EntityType, Season, DateCreated, IsViewed, Positions, Denied, Country, State, City, Address, PostalCode
FROM (
    SELECT
	    FR.Username, (U.FirstName + ' ' + U.Lastname) AS FullUsername, FR.FriendId, FR.FriendUsername, R.RegionName, R.Sport, R.RegionIsLadderLeague, R.EntityType, R.Season, FR.DateCreated, FR.IsViewed, FR.Positions, FR.Denied, R.Country, R.State, R.City, R.Address, R.PostalCode
    FROM
	    FriendNotification AS FR, Region As R, [User] AS U
    WHERE 
	    FR.Username = @Username AND R.RegionId = FR.FriendId And FR.Denied = 0 AND U.Username = FR.Username
UNION
    SELECT
	    FR.Username, (U2.FirstName + ' ' + U2.Lastname) AS FullUsername, FR.FriendId, FR.FriendUsername, (U1.FirstName + ' ' + U1.LastName) AS RegionName, '' AS Sport, CAST(0 AS BIT) AS RegionIsLadderLeague, 'user' AS EntityType, 0 AS Season, FR.DateCreated, FR.IsViewed, FR.Positions, FR.Denied, '' AS Country, '' AS State, '' AS City, '' AS Address, '' AS PostalCode
    FROM
	    FriendNotification AS FR, [User] As U1, [User] AS U2
    WHERE 
	    FR.Username = @Username AND U1.Username = FR.FriendId And FR.Denied = 0 AND U2.Username = FR.Username
UNION
    SELECT
	    FR.Username, R2.RegionName, FR.FriendId, FR.FriendUsername, R.RegionName, R.Sport, R.RegionIsLadderLeague, R.EntityType, R.Season, FR.DateCreated, FR.IsViewed, FR.Positions, FR.Denied, R.Country, R.State, R.City, R.Address, R.PostalCode
    FROM
	    FriendNotification AS FR, Region As R, Region As R2
    WHERE 
	    FR.Username IN (SELECT RU.RegionId FROM RegionUser AS RU WHERE RU.RealUsername = @Username AND RU.IsExecutive = 1 AND RU.IsArchived = 0) AND R.RegionId = FR.FriendId And FR.Denied = 0 AND FR.Username = R2.RegionId
) AS _FriendNotification
ORDER BY
	DateCreated DESC
                      </SQL>.Value

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                FriendNotifications.Add(New FriendNotification With {
                    .Username = Reader.GetString(0),
                    .FullUsername = Reader.GetString(1),
                    .FriendId = Reader.GetString(2),
                    .FriendUsername = Reader.GetString(3),
                    .FriendName = Reader.GetString(4),
                    .FriendSport = Reader.GetString(5),
                    .FriendLadderLeague = Reader.GetBoolean(6),
                    .FriendEntityType = Reader.GetString(7),
                    .FriendSeason = Reader.GetInt32(8),
                    .DateCreated = Reader.GetDateTime(9),
                    .IsViewed = Reader.GetBoolean(10),
                    .Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(11)),
                    .Denied = Reader.GetBoolean(12),
                    .FriendCountry = Reader.GetString(13),
                    .FriendState = Reader.GetString(14),
                    .FriendCity = Reader.GetString(15),
                    .FriendAddress = Reader.GetString(16),
                    .FriendPostalCode = Reader.GetString(17)
                })
            End While
            Reader.Close()
        End Using

        Return FriendNotifications
    End Function

    Public Shared Function GetDeniedRegions(Username As String) As Object
        Dim FriendNotifications As List(Of FriendNotification) = Nothing

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    FriendNotifications = GetDeniedRegionsHelper(Username, SqlConnection, SqlTransaction)
                End Using
            End Using
            Return New With {
                .Success = True,
                .FriendNotifications = FriendNotifications
            }
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function GetFriendNotificationHelper(Username As String, FriendId As String, FriendUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As FriendNotification
        Dim Result As FriendNotification = Nothing

        Dim CommandText = <SQL>
                        SELECT
	                        Username, FriendId, FriendUsername, DateCreated, IsViewed, Positions, Denied
                        FROM
	                        FriendNotification
                        WHERE 
	                        Username = @Username AND FriendId = @FriendId AND FriendUsername = @FriendUsername
                      </SQL>.Value

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            SqlCommand.Parameters.Add(New SqlParameter("FriendId", FriendId))
            SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", FriendUsername))


            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read

                Result = New FriendNotification With {
                    .Username = Reader.GetString(0).ToLower,
                    .FriendId = Reader.GetString(1).ToLower,
                    .FriendUsername = Reader.GetString(2).ToLower,
                    .DateCreated = Reader.GetDateTime(3),
                    .IsViewed = Reader.GetBoolean(4),
                    .Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(5)),
                    .Denied = Reader.GetBoolean(6)
                }

            End While
            Reader.Close()
        End Using


        Return Result
    End Function

    Public Shared Function GetFriendNotificationsHelper(Username As String, FriendId As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of FriendNotification)
        Dim Result As New List(Of FriendNotification)

        Dim CommandText = <SQL>
                        SELECT
	                        Username, FriendId, FriendUsername, DateCreated, IsViewed, Positions, Denied
                        FROM
	                        FriendNotification
                        WHERE 
	                        Username = @Username AND FriendId = @FriendId
                      </SQL>.Value

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            SqlCommand.Parameters.Add(New SqlParameter("FriendId", FriendId))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read

                Result.Add(New FriendNotification With {
                    .Username = Reader.GetString(0).ToLower,
                    .FriendId = Reader.GetString(1).ToLower,
                    .FriendUsername = Reader.GetString(2).ToLower,
                    .DateCreated = Reader.GetDateTime(3),
                    .IsViewed = Reader.GetBoolean(4),
                    .Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(5).ToLower),
                    .Denied = Reader.GetBoolean(6)
                })

            End While
            Reader.Close()
        End Using


        Return Result
    End Function

    Public Shared Function GetFriendNotificationsInRegionHelper(RegionId As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of FriendNotification)
        Dim Result As New List(Of FriendNotification)

        Dim CommandText = <SQL>
                        SELECT
	                        Username, FriendId, FriendUsername, DateCreated, IsViewed, Positions, Denied
                        FROM
	                        FriendNotification
                        WHERE 
	                        FriendId = @RegionId
                      </SQL>.Value

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read

                Result.Add(New FriendNotification With {
                    .Username = Reader.GetString(0).ToLower,
                    .FriendId = Reader.GetString(1).ToLower,
                    .FriendUsername = Reader.GetString(2).ToLower,
                    .DateCreated = Reader.GetDateTime(3),
                    .IsViewed = Reader.GetBoolean(4),
                    .Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(5).ToLower),
                    .Denied = Reader.GetBoolean(6)
                })

            End While
            Reader.Close()
        End Using


        Return Result
    End Function

    Public Shared Function GetFriendNotificationsInRegionUserHelper(RegionId As String, Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of FriendNotification)
        Dim Result As New List(Of FriendNotification)

        Dim CommandText = <SQL>
                        SELECT
	                        Username, FriendId, FriendUsername, DateCreated, IsViewed, Positions, Denied
                        FROM
	                        FriendNotification
                        WHERE 
	                        FriendId = @RegionId AND FriendUsername = @Username
                      </SQL>.Value

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read

                Result.Add(New FriendNotification With {
                    .Username = Reader.GetString(0).ToLower,
                    .FriendId = Reader.GetString(1).ToLower,
                    .FriendUsername = Reader.GetString(2).ToLower,
                    .DateCreated = Reader.GetDateTime(3),
                    .IsViewed = Reader.GetBoolean(4),
                    .Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(5).ToLower),
                    .Denied = Reader.GetBoolean(6)
                })

            End While
            Reader.Close()
        End Using


        Return Result
    End Function

    Public Shared Sub CreateFriendNotificationHelper(Username As String, FriendId As String, FriendUsername As String, Positions As List(Of String), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)

        Dim CommandText = <SQL>
                        INSERT INTO FriendNotification
	                        (Username, FriendId, FriendUsername, DateCreated, IsViewed, Positions, Denied)
                        VALUES
	                        (@Username, @FriendId, @FriendUsername, @DateCreated, @IsViewed, @Positions, @Denied)
                      </SQL>.Value

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)


            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            SqlCommand.Parameters.Add(New SqlParameter("FriendId", FriendId))
            SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", FriendUsername))
            SqlCommand.Parameters.Add(New SqlParameter("DateCreated", Date.UtcNow))
            SqlCommand.Parameters.Add(New SqlParameter("IsViewed", False))
            SqlCommand.Parameters.Add(New SqlParameter("Positions", JsonConvert.SerializeObject(Positions)))
            SqlCommand.Parameters.Add(New SqlParameter("Denied", False))

            SqlCommand.ExecuteNonQuery()
        End Using

    End Sub

    Public Shared Sub DeleteFriendNotificationHelper(Username As String, FriendId As String, FriendUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction, Optional IgnoreDenied As Boolean = False)
        Dim CommandText As String = ""
        If IgnoreDenied Then
            CommandText = <SQL>
                        DELETE FROM FriendNotification WHERE Username = @Username AND FriendId = @FriendId AND FriendUsername = @FriendUsername
                      </SQL>.Value
        Else
            CommandText = <SQL>
                        DELETE FROM FriendNotification WHERE Username = @Username AND FriendId = @FriendId AND FriendUsername = @FriendUsername AND Denied = 0
                      </SQL>.Value
        End If

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            SqlCommand.Parameters.Add(New SqlParameter("FriendId", FriendId))
            SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", FriendUsername))

            SqlCommand.ExecuteNonQuery()
        End Using

    End Sub

    Public Shared Sub DeleteFriendNotificationsHelper(Username As String, FriendId As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction, Optional IgnoreDenied As Boolean = False)
        Dim CommandText As String = ""
        If IgnoreDenied Then
            CommandText = <SQL>
                        DELETE FROM FriendNotification WHERE Username = @Username AND FriendId = @FriendId
                      </SQL>.Value
        Else
            CommandText = <SQL>
                        DELETE FROM FriendNotification WHERE Username = @Username AND FriendId = @FriendId AND Denied = 0
                      </SQL>.Value
        End If

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            SqlCommand.Parameters.Add(New SqlParameter("FriendId", FriendId))

            SqlCommand.ExecuteNonQuery()
        End Using

    End Sub

    Public Shared Function GetDeniedRegionsHelper(Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of FriendNotification)
        Dim FriendNotifications As New List(Of FriendNotification)

        Dim CommandText = <SQL>
SELECT
    Username, FullUsername, FriendId, FriendUsername, RegionName, Sport, CAST(_FriendNotification.RegionIsLadderLeague AS BIT) AS RegionIsLadderLeague, EntityType, Season, DateCreated, IsViewed, Positions, Denied, Country, State, City, Address, PostalCode
FROM (
    SELECT
	    FR.Username, (U.FirstName + ' ' + U.Lastname) AS FullUsername, FR.FriendId, FR.FriendUsername, R.RegionName, R.Sport, R.RegionIsLadderLeague, R.EntityType, R.Season, FR.DateCreated, FR.IsViewed, FR.Positions, FR.Denied, R.Country, R.State, R.City, R.Address, R.PostalCode
    FROM
	    FriendNotification AS FR, Region As R, [User] AS U
    WHERE 
	    FR.Username = @Username AND R.RegionId = FR.FriendId And FR.Denied = 1 AND U.Username = FR.Username
UNION
    SELECT
	    FR.Username, (U2.FirstName + ' ' + U2.Lastname) AS FullUsername, FR.FriendId, FR.FriendUsername, (U1.FirstName + ' ' + U1.LastName) AS RegionName, '' AS Sport, CAST(0 AS BIT) AS RegionIsLadderLeague, 'user' AS EntityType, 0 AS Season, FR.DateCreated, FR.IsViewed, FR.Positions, FR.Denied, '' AS Country, '' AS State, '' AS City, '' AS Address, '' AS PostalCode
    FROM
	    FriendNotification AS FR, [User] As U1, [User] AS U2
    WHERE 
	    FR.Username = @Username AND U1.Username = FR.FriendId And FR.Denied = 1 AND U2.Username = FR.Username
UNION
    SELECT
	    FR.Username, R2.RegionName, FR.FriendId, FR.FriendUsername, R.RegionName, R.Sport, R.RegionIsLadderLeague, R.EntityType, R.Season, FR.DateCreated, FR.IsViewed, FR.Positions, FR.Denied, R.Country, R.State, R.City, R.Address, R.PostalCode
    FROM
	    FriendNotification AS FR, Region As R, Region As R2
    WHERE 
	    FR.Username IN (SELECT RU.RegionId FROM RegionUser AS RU WHERE RU.RealUsername = @Username AND RU.IsExecutive = 1 AND RU.IsArchived = 0) AND R.RegionId = FR.FriendId And FR.Denied = 1 AND FR.Username = R2.RegionId
) AS _FriendNotification
ORDER BY
	DateCreated
                      </SQL>.Value

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
              FriendNotifications.Add(New FriendNotification With {
                      .Username = Reader.GetString(0),
                      .FullUsername = Reader.GetString(1),
                      .FriendId = Reader.GetString(2),
                      .FriendUsername = Reader.GetString(3),
                      .FriendName = Reader.GetString(4),
                      .FriendSport = Reader.GetString(5),
                      .FriendLadderLeague = Reader.GetBoolean(6),
                      .FriendEntityType = Reader.GetString(7),
                      .FriendSeason = Reader.GetInt32(8),
                      .DateCreated = Reader.GetDateTime(9),
                      .IsViewed = Reader.GetBoolean(10),
                      .Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(11)),
                      .Denied = Reader.GetBoolean(12),
                      .FriendCountry = Reader.GetString(13),
                      .FriendState = Reader.GetString(14),
                      .FriendCity = Reader.GetString(15),
                      .FriendAddress = Reader.GetString(16),
                      .FriendPostalCode = Reader.GetString(17)
                  })
            End While
            Reader.Close()
        End Using

        For I As Integer = FriendNotifications.Count - 1 To 0 Step -1
            For N As Integer = FriendNotifications.Count - 2 To 0 Step -1
                If I <> N Then
                    If FriendNotifications(I).Username = FriendNotifications(N).Username AndAlso FriendNotifications(I).FriendId = FriendNotifications(N).FriendId Then
                        FriendNotifications.RemoveAt(I + 1)
                        Exit For
                    End If
                End If
            Next
        Next

        Return FriendNotifications
    End Function

    Public Shared Function UnblockRegion(Username As String, UnblockFriends As List(Of UnblockFriend)) As Object
        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    UnblockRegionHelper(Username, UnblockFriends, SqlConnection, SqlTransaction)

                    SqlTransaction.Commit()
                End Using
            End Using

            Return New With {
                .Success = True
            }
        Catch E As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Sub UnblockRegionHelper(Username As String, UnblockFriends As List(Of UnblockFriend), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        If UnblockFriends.Count = 0 Then Return

        For Each UnblockFriend In UnblockFriends
            UnblockSingleRegionHelper(Username, UnblockFriend.Username, UnblockFriend.FriendId, SQLConnection, SQLTransaction)
        Next

    End Sub

    Public Shared Sub UnblockSingleRegionHelper(Username As String, OnBehalfUsername As String, FriendId As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        Username = Username.ToLower
        OnBehalfUsername = OnBehalfUsername.ToLower

        If Username = OnBehalfUsername Then
            Dim CommandText = "UPDATE FriendNotification SET Denied = 0 WHERE Username = @Username AND FriendId = @FriendId And Denied = 1"

            Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
                SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
                SqlCommand.Parameters.Add(New SqlParameter("FriendId", FriendId))
                SqlCommand.ExecuteNonQuery()
            End Using
        Else
            Dim CommandText = "UPDATE FriendNotification SET Denied = 0, DateCreated = @DateCreated WHERE Username = @OnBehalfUsername AND FriendId = @FriendId And Denied = 1 AND Username IN (SELECT RegionId FROM RegionUser WHERE RegionID = @OnBehalfUsername AND RealUsername = @Username AND IsExecutive = 1)"

            Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
                SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
                SqlCommand.Parameters.Add(New SqlParameter("OnBehalfUsername", OnBehalfUsername))
                SqlCommand.Parameters.Add(New SqlParameter("FriendId", FriendId))
                SqlCommand.Parameters.Add(New SqlParameter("DateCreated", Date.UtcNow))
                SqlCommand.ExecuteNonQuery()
            End Using
        End If


    End Sub

End Class
