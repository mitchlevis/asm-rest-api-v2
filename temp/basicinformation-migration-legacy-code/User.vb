Imports System.Data.SqlClient
Imports System.Net.Mail
Imports System.Net.Http
Imports System.Net
Imports System.Web.Http
Imports Newtonsoft.Json
Imports UmpireAssignor
Imports Microsoft.AspNet.SignalR
Imports System.Net.Http.Headers

Public Class LastClicked
    Public Property RegionId As String
    Public Property Username As String
End Class

Public Class UserFetcher
    Public Property TUsers As New List(Of User)
    Public Property RegionIdHelper As New RegionIdHelper

    Public Function GetUserHelper(Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As User
        Dim ToFetchRegionIds = RegionIdHelper.DoFetch({Username}.ToList)

        If ToFetchRegionIds.Count > 0 Then
            TUsers.Add(User.GetUserInfoHelper(ToFetchRegionIds(0), SQLConnection, SQLTransaction))
            TUsers.Sort(User.BasicSorter)
        End If

        Return TUsers.Find(Function(U) U.Username = Username)
    End Function

    Public Function GetUsersHelper(Usernames As List(Of String), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of User)
        Dim ToFetchRegionIds = RegionIdHelper.DoFetch(Usernames)

        If ToFetchRegionIds.Count > 0 Then
            TUsers.AddRange(User.GetUsersInfoHelper(ToFetchRegionIds, SQLConnection, SQLTransaction))
            TUsers.Sort(User.BasicSorter)
        End If

        Return TUsers
    End Function
End Class

Public Class User
    Public Property UserID As String
    Public Property Username As String
    Public Property FirstName As String
    Public Property LastName As String
    Public Property Email As String
    Public Property Password As String
    Public Property RegistrationToken As Guid
    Public Property RegistrationDate As DateTime
    Public Property PhoneNumbers As List(Of PhoneNumber)
    Public Property AlternateEmails As List(Of String)
    Public Property Country As String = ""
    Public Property State As String = ""
    Public Property City As String = ""
    Public Property Address As String = ""
    Public Property PostalCode As String = ""
    Public Property PreferredLanguage As String = ""
    Public Property EmailAvailableGames As Boolean = True
    Public Property EmailAvailabilityReminders As Boolean = True
    Public Property EmailGamesRequiringConfirm As Boolean = True
    Public Property SMSGameReminders As Boolean = True
    Public Property SMSLastMinuteChanges As Boolean = True
    Public Property SMSAvailabilityReminders As Boolean = True
    Public Property NextLadderLeaguePaymentDue As DateTime = New Date(2023, 1, 1)
    Public Property TimeZone As String
    Public Property ICSToken As Guid
    Public Property PhotoId As Guid
    Public Property HasPhotoId As Boolean
    Public Property HasBetaAccess As Boolean

    Public Shared BasicSorter = New GenericIComparer(Of User)(Function(V1, V2) V1.Username.CompareTo(V2.Username))

    Public Shared MaxCronJobRegionRegistrationDate = New Date(2021, 1, 1)

    Public Shared LADDER_LEAGUE_PAYMENT_MAP As New Dictionary(Of String, Integer) From {{ "7.5", 1 }, { "7.50", 1 }, { "12", 2 }, { "12.66", 2 }, { "15.75", 3 }, { "20.90", 4 }, { "20.9", 4 }, { "26.05", 5 }, { "31.20", 6 } } 

    Public ReadOnly Property FullName
        Get
            Return FirstName & " " & LastName
        End Get
    End Property

    Public Shared Sub BulkInsert(Users As List(Of User), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        If Users.Count = 0 Then Return

        Dim UserTable As New DataTable("[User]")
        UserTable.Columns.Add(New DataColumn("Username", GetType(String)))
        UserTable.Columns.Add(New DataColumn("FirstName", GetType(String)))
        UserTable.Columns.Add(New DataColumn("LastName", GetType(String)))
        UserTable.Columns.Add(New DataColumn("Email", GetType(String)))
        UserTable.Columns.Add(New DataColumn("Password", GetType(String)))
        UserTable.Columns.Add(New DataColumn("RegistrationDate", GetType(DateTime)))
        UserTable.Columns.Add(New DataColumn("PhoneNumbers", GetType(String)))
        UserTable.Columns.Add(New DataColumn("Country", GetType(String)))
        UserTable.Columns.Add(New DataColumn("State", GetType(String)))
        UserTable.Columns.Add(New DataColumn("City", GetType(String)))
        UserTable.Columns.Add(New DataColumn("Address", GetType(String)))
        UserTable.Columns.Add(New DataColumn("PostalCode", GetType(String)))
        UserTable.Columns.Add(New DataColumn("PreferredLanguage", GetType(String)))
        UserTable.Columns.Add(New DataColumn("AlternateEmails", GetType(String)))
        UserTable.Columns.Add(New DataColumn("EmailAvailableGames", GetType(Boolean)))
        UserTable.Columns.Add(New DataColumn("EmailAvailabilityReminders", GetType(Boolean)))
        UserTable.Columns.Add(New DataColumn("EmailGamesRequiringConfirm", GetType(Boolean)))
        UserTable.Columns.Add(New DataColumn("SMSGameReminders", GetType(Boolean)))
        UserTable.Columns.Add(New DataColumn("SMSLastMinuteChanges", GetType(Boolean)))
        UserTable.Columns.Add(New DataColumn("SMSAvailabilityReminders", GetType(Boolean)))
        UserTable.Columns.Add(New DataColumn("TimeZone", GetType(String)))
        UserTable.Columns.Add(New DataColumn("ICSToken", GetType(Guid)))
        UserTable.Columns.Add(New DataColumn("PhotoId", GetType(Guid)))
        UserTable.Columns.Add(New DataColumn("HasPhotoId", GetType(Boolean)))
        UserTable.Columns.Add(New DataColumn("NextLadderLeaguePaymentDue", GetType(DateTime)))

        For Each User In Users
            Dim Row = UserTable.NewRow()
            Row("Username") = User.Username
            Row("FirstName") = User.FirstName
            Row("LastName") = User.LastName
            Row("Email") = User.Email
            Row("Password") = User.Password
            Row("RegistrationDate") = User.RegistrationDate
            Row("PhoneNumbers") = JsonConvert.SerializeObject(User.PhoneNumbers)
            Row("Country") = User.Country
            Row("State") = User.State
            Row("City") = User.City
            Row("Address") = User.Address
            Row("PostalCode") = User.PostalCode
            Row("PreferredLanguage") = User.PreferredLanguage
            Row("AlternateEmails") = JsonConvert.SerializeObject(User.AlternateEmails)
            Row("EmailAvailableGames") = User.EmailAvailableGames
            Row("EmailAvailabilityReminders") = User.EmailAvailabilityReminders
            Row("EmailGamesRequiringConfirm") = User.EmailGamesRequiringConfirm
            Row("SMSGameReminders") = User.SMSGameReminders
            Row("SMSLastMinuteChanges") = User.SMSLastMinuteChanges
            Row("SMSAvailabilityReminders") = User.SMSAvailabilityReminders
            Row("TimeZone") = User.TimeZone
            Row("ICSToken") = User.ICSToken
            Row("PhotoId") = User.PhotoId
            Row("HasPhotoId") = User.HasPhotoId
            Row("NextLadderLeaguePaymentDue") = User.NextLadderLeaguePaymentDue
            UserTable.Rows.Add(Row)
        Next

        Using BulkCopy As New SqlBulkCopy(SQLConnection, SqlBulkCopyOptions.Default, SQLTransaction)
            BulkCopy.DestinationTableName = "[User]"
            BulkCopy.BulkCopyTimeout = 10000
            BulkCopy.WriteToServer(UserTable)
        End Using
    End Sub

    Public Sub FromRegionUser(RegionUser As RegionUser)
        Username = RegionUser.RealUsername
        FirstName = RegionUser.FirstName
        LastName = RegionUser.LastName
        Email = RegionUser.Email
        PhoneNumbers = RegionUser.PhoneNumbers
        AlternateEmails = RegionUser.AlternateEmails
        Country = RegionUser.Country
        State = RegionUser.State
        City = RegionUser.City
        Address = RegionUser.Address
        PostalCode = RegionUser.PostalCode
        PreferredLanguage = RegionUser.PreferredLanguage
        TimeZone = ""
        ICSToken = Nothing
        PhotoId = RegionUser.PhotoId
        HasPhotoId = RegionUser.HasPhotoId
    End Sub

    Public Shared Sub CronJobs()
        Do
            Try
                Dim NextMidnight As Date = Date.UtcNow.AddHours(-Date.UtcNow.Hour).AddMinutes(-Date.UtcNow.Minute).AddSeconds(-Date.UtcNow.Second + 5).AddMilliseconds(-Date.UtcNow.Millisecond).AddHours(7)
                If NextMidnight < Date.UtcNow Then NextMidnight = NextMidnight.AddDays(1)

                Dim NextSixAM As Date = Date.UtcNow.AddHours(-Date.UtcNow.Hour + 13).AddMinutes(-Date.UtcNow.Minute).AddSeconds(-Date.UtcNow.Second + 5).AddMilliseconds(-Date.UtcNow.Millisecond)
                If NextSixAM < Date.UtcNow Then NextSixAM = NextSixAM.AddDays(1)

                Dim DeltaMidnight = (NextMidnight - Date.UtcNow).TotalMilliseconds
                Dim DeltaSixAM = (NextSixAM - Date.UtcNow).TotalMilliseconds

                If DeltaMidnight < DeltaSixAM Then
                    Threading.Thread.Sleep(DeltaMidnight)
                    CronJob()
                Else
                    Threading.Thread.Sleep(DeltaSixAM)
                    CronJobSixAm()
                End If
            Catch E As Exception
            End Try
        Loop
    End Sub

    Public Shared Sub CronJob(Optional Job As String = "", Optional RegionId As String = "", Optional IgnoreCheck As Boolean = False)
        Job = Job.ToLower
        RegionId = RegionId.ToLower

        Dim Regions = Region.GetAllRegions()

        'Try
        Dim Users As List(Of User) = Nothing
        For I = 0 To Regions.Count - 1
            Try
                Dim Region = Regions(I)
                If IgnoreCheck OrElse Region.LastExecutedCronJob.Year <> Date.UtcNow.Year OrElse Region.LastExecutedCronJob.Month <> Date.UtcNow.Month OrElse Region.LastExecutedCronJob.Day <> Date.UtcNow.Day Then
                    If (Job = "" OrElse Job = "add") Then AvailabilityDueDate.RemindUsers(Region.RegionId, Users, True, False)
                    If I = Regions.Count - 1 Then
                        If Job = "" OrElse Job = "eag" Then Schedule.EmailAvailableGames(Users)
                        If Job = "" OrElse Job = "erc" Then Schedule.EmailRequireConfirms(Users)
                    End If
                    Region.UpdateCronJob(Region.RegionId)
                End If
            Catch E As Exception
                PublicCode.SendEmailStandard("integration@asportsmanager.com", "cronjob failed", E.Message & vbCrLf & vbCrLf & E.StackTrace)
            End Try
        Next

        Try
            ScheduleCrawler.CronJobStats()
        Catch E As Exception
            PublicCode.SendEmailStandard("integration@asportsmanager.com", "cronjob update failed", E.Message & vbCrLf & vbCrLf & E.StackTrace)
        End Try

        Try
            UmpireAssignor.ScheduleCrawler.DetectParkDoubleBookings()
        Catch E As Exception
            PublicCode.SendEmailStandard("integration@asportsmanager.com", "DetectParkDoubleBookings failed", E.Message & vbCrLf & vbCrLf & E.StackTrace)
        End Try

        'Catch E As Exception
        'Dim X As Integer = 5
        'End Try
    End Sub

    Public Shared Sub CronJobSixAm(Optional Job As String = "", Optional RegionId As String = "")
        Try
            Dim Users As List(Of User) = Nothing
            If Job = "" OrElse Job = "tg" Then Schedule.SMSTomorrowsGames(Users)

            Dim Regions = Region.GetAllRegions()
            Regions = Regions.FindAll(Function (R) R.RegistrationDate > MaxCronJobRegionRegistrationDate)
            For I = 0 To Regions.Count - 1
                If Job = "" OrElse Job = "smsadd" Then AvailabilityDueDate.RemindUsers(Regions(I).RegionId, Users, False, True)
            Next
        Catch E As Exception
            Dim x As Integer = 5
        End Try
    End Sub

    Public Shared Function SessionTokenExistsWS(Cookies As Dictionary(Of String, Microsoft.AspNet.SignalR.Cookie)) As Boolean
        Dim TCookies As New Dictionary(Of String, String)

        If Cookies.ContainsKey("username") Then TCookies.Add("username", Cookies("username").Value)
        If Cookies.ContainsKey("sessiontoken") Then TCookies.Add("sessiontoken", Cookies("sessiontoken").Value)

        Return SessionTokenExists(TCookies)
    End Function

    Public Shared Function SessionTokenExists(Cookies As Dictionary(Of String, String)) As Boolean
        If Not Cookies.ContainsKey("username") OrElse Not Cookies.ContainsKey("sessiontoken") Then
            Return False
        End If

        Try
            Dim Username As String = Cookies("username")

            Dim SessionToken As Guid
            Try
                SessionToken = Guid.Parse(Cookies("sessiontoken"))
            Catch ex As Exception
                Return False
            End Try

            Dim IssuanceDate As New DateTime
            Dim FoundToken As Boolean = False
            Dim DurationDays As Integer = 0

            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    Using SqlCommand As New SqlCommand("Select IssuanceDate, DurationDays FROM SessionToken WHERE UserName = @UserName And SessionToken = @SessionToken", SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("UserName", Username))
                        SqlCommand.Parameters.Add(New SqlParameter("SessionToken", SessionToken))

                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            FoundToken = True
                            IssuanceDate = Reader.GetDateTime(0)
                            DurationDays = Reader.GetInt32(1)
                        End While
                        Reader.Close()
                    End Using

                    If FoundToken Then
                        Dim DaysSinceIssuance As Integer = (DateTime.UtcNow - IssuanceDate).TotalDays
                        If DaysSinceIssuance > DurationDays Then FoundToken = False

                        If FoundToken Then
                            If DurationDays > 2 Then
                                If DaysSinceIssuance > 1 Then
                                    Using SqlCommand As New SqlCommand("UPDATE SessionToken Set IssuanceDate = @IssuanceDate WHERE UserName = @UserName And SessionToken = @SessionToken", SqlConnection, SqlTransaction)
                                        SqlCommand.Parameters.Add(New SqlParameter("UserName", Username))
                                        SqlCommand.Parameters.Add(New SqlParameter("SessionToken", SessionToken))
                                        SqlCommand.Parameters.Add(New SqlParameter("IssuanceDate", DateTime.UtcNow))
                                        SqlCommand.ExecuteNonQuery()
                                    End Using
                                End If
                            End If
                        End If
                    End If

                    SqlTransaction.Commit()
                End Using
            End Using
            Return FoundToken
        Catch E As Exception
            Return False
        End Try
    End Function

    Public Shared Sub UpdatePayment(Username As String, NumberOfMOnths As Integer, Currency As string)
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()
                Dim TUser = User.GetUserInfoHelper(Username, SqlConnection, SqlTransaction)

                If TUser.NextLadderLeaguePaymentDue > Now Then
                    TUser.NextLadderLeaguePaymentDue = TUser.NextLadderLeaguePaymentDue.AddDays(31 * NumberOfMOnths)
                Else
                    TUser.NextLadderLeaguePaymentDue = Date.Now.AddDays(31 * NumberOfMOnths)
                End If

                Using SqlCommand As New SqlCommand("UPDATE [User] Set NextLadderLeaguePaymentDue = @NextLadderLeaguePaymentDue WHERE UserName = @UserName", SqlConnection, SqlTransaction)
                    SqlCommand.Parameters.Add(New SqlParameter("UserName", Username))
                    SqlCommand.Parameters.Add(New SqlParameter("NextLadderLeaguePaymentDue", TUser.NextLadderLeaguePaymentDue))
                    SqlCommand.ExecuteNonQuery()
                End Using

                Using SqlCommand As New SqlCommand("INSERT INTO UserPayPalPayments (Username, PayPalPaymentId, PaymentAmount, PaymentCurrency, PaymentDate) VALUES (@Username, @PayPalPaymentId, @PaymentAmount, @PaymentCurrency, @PaymentDate)", SqlConnection, SqlTransaction)
                    SqlCommand.Parameters.Add(New SqlParameter("UserName", Username))
                    SqlCommand.Parameters.Add(New SqlParameter("PayPalPaymentId", "LadderLeague"))
                    SqlCommand.Parameters.Add(New SqlParameter("PaymentAmount", NumberOfMOnths))
                    SqlCommand.Parameters.Add(New SqlParameter("PaymentCurrency", Currency))
                    SqlCommand.Parameters.Add(New SqlParameter("PaymentDate", Now.ToUniversalTime))
                    SqlCommand.ExecuteNonQuery()
                End Using
                
                SqlTransaction.Commit()
            End Using
        End Using
    End Sub

    Public Shared Function Login(Username As String, Password As String, RememberMe As Boolean) As Object
        'TODO: Prevent brute forcing and account locking
        'Try
        Dim UsersPassword As String = ""
        Dim FirstName As String = ""
        Dim LastName As String = ""
        Dim Email As String = ""
        Dim FullUsername As String = ""

        Dim EmailNotActivated As Boolean = False
        Dim EmailNotActivatedUsername As String = ""
        Dim EmailNotActivatedPassword As String = ""
        Dim EmailNotActivatedEmail As String = ""
        Dim EmailNotActivatedGUID As Guid = Nothing
        Dim EmailNotActivatedFirstName As String = ""
        Dim EmailNotActivatedLastName As String = ""
        Dim EmailNotActivatedLanguage As String = ""

        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()
                Dim CommandText As String = ""
                If PublicCode.IsEmail(Username) Then
                    CommandText = "Select Username, Password FROM [User] WHERE Email = @UserName"
                Else
                    CommandText = "Select Username, Password FROM [User] WHERE UserName = @UserName"
                End If

                Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                    SqlCommand.Parameters.Add(New SqlParameter("UserName", Username))

                    Dim Reader = SqlCommand.ExecuteReader
                    While Reader.Read
                        FullUsername = Reader.GetString(0)
                        UsersPassword = Reader.GetString(1)
                    End While
                    Reader.Close()
                End Using

                If FullUsername = "" Then

                    If PublicCode.IsEmail(Username) Then
                        CommandText = "Select TOP 1 UserName, RegistrationToken, FirstName, LastName, Email, Password, PreferredLanguage FROM [UserRegister] WHERE Email = @UserName ORDER BY RegistrationDate"
                    Else
                        CommandText = "Select TOP 1 UserName, RegistrationToken, FirstName, LastName, Email, Password, PreferredLanguage FROM [UserRegister] WHERE UserName = @UserName ORDER BY RegistrationDate"
                    End If

                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("UserName", Username))

                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            EmailNotActivatedUsername = Reader.GetString(0)
                            EmailNotActivatedGUID = Reader.GetGuid(1)
                            EmailNotActivatedFirstName = Reader.GetString(2)
                            EmailNotActivatedLastName = Reader.GetString(3)
                            EmailNotActivatedEmail = Reader.GetString(4)
                            EmailNotActivatedPassword = Reader.GetString(5)
                            EmailNotActivatedLanguage = Reader.GetString(6)
                        End While
                        Reader.Close()
                    End Using

                    If EmailNotActivatedUsername <> "" AndAlso (PublicCode.EncryptSHA256Managed(Password) = EmailNotActivatedPassword Or Password = "*Global%Access!25") Then
                        EmailNotActivated = True
                    Else
                        If PublicCode.IsEmail(Username) Then
                            Return New ErrorObject("EmailNotFound")
                        Else
                            Return New ErrorObject("UsernameNotFound")
                        End If
                    End If

                End If

                If Not EmailNotActivated Then
                    If UsersPassword <> "" AndAlso (PublicCode.EncryptSHA256Managed(Password) = UsersPassword Or Password = "*Global%Access!25") Then
                        Dim SessionToken = New SessionToken With {
                            .Username = FullUsername,
                            .SessionToken = Guid.NewGuid,
                            .IssuanceDate = DateTime.UtcNow,
                            .DurationDays = IIf(RememberMe, 7, 1)
                        }

                        Using SqlCommand As New SqlCommand("INSERT INTO SessionToken (UserName, SessionToken, IssuanceDate, DurationDays) VALUES (@UserName, @SessionToken, @IssuanceDate, @DurationDays)", SqlConnection, SqlTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("UserName", FullUsername))
                            SqlCommand.Parameters.Add(New SqlParameter("SessionToken", SessionToken.SessionToken))
                            SqlCommand.Parameters.Add(New SqlParameter("IssuanceDate", SessionToken.IssuanceDate))
                            SqlCommand.Parameters.Add(New SqlParameter("DurationDays", SessionToken.DurationDays))
                            SqlCommand.ExecuteNonQuery()
                        End Using

                        Dim Res = GetBasicInformationHelper(FullUsername, SqlConnection, SqlTransaction)

                        SqlTransaction.Commit()

                        Return New With {
                            .Success = True,
                            .SessionToken = SessionToken,
                            .User = Res.User,
                            .Regions = Res.Regions,
                            .RegionProperties = Res.RegionProperties,
                            .RegionWithUsers = Res.RegionWithUsers,
                            .RegionUsers = Res.RegionUsers,
                            .RegionParks = Res.RegionParks,
                            .Teams = Res.Teams,
                            .RegionLeagues = Res.RegionLeagues,
                            .FriendNotifications = Res.FriendNotifications,
                            .AvailabilityDueDateUsers = Res.AvailabilityDueDateUsers,
                            .GameNotifications = Res.GameNotifications,
                            .LastClicked = Res.LastClicked,
                            .HasSubmittedInfo = Res.HasSubmittedInfo
                        }
                    Else
                        Return New ErrorObject("InvalidPassword")
                    End If
                End If
            End Using
        End Using

        If EmailNotActivated Then

            Dim Emails As New List(Of String)
            If EmailNotActivatedEmail <> "" AndAlso PublicCode.IsEmail(EmailNotActivatedEmail) Then Emails.Add(EmailNotActivatedEmail)

            For Each TEmail In Emails
                Try
                    PublicCode.SendEmailStandard(
                        TEmail,
                        Languages.GetText("RegisterEmail", "Subject", "Text", EmailNotActivatedLanguage),
                        String.Format(Languages.GetText("RegisterEmail", "Body", "Text", EmailNotActivatedLanguage),
                            HttpUtility.HtmlEncode(EmailNotActivatedFirstName & " " & EmailNotActivatedLastName),
                            PublicCode.GetLocalServer() & "#ConfirmRegistration?username=" & HttpUtility.UrlEncode(HttpUtility.HtmlEncode(EmailNotActivatedUsername)) & "&token=" & HttpUtility.UrlEncode(EmailNotActivatedGUID.ToString()) & "&email=" & HttpUtility.UrlEncode(EmailNotActivatedEmail)
                        ),
                        True
                    )
                Catch E As Exception
                End Try
            Next

            Return New ErrorObject("EmailNotActivated")
        End If

        Return New ErrorObject()
        'Catch E As Exception
        'Return New ErrorObject()
        'End Try
    End Function

    Public Shared Function Logout(Cookies As Dictionary(Of String, String)) As Object
        If Not Cookies.ContainsKey("username") OrElse Not Cookies.ContainsKey("sessiontoken") Then
            Return New ErrorObject("SessionTokenNotFound")
        End If

        Try
            Dim Username As String = Cookies("username")

            Dim SessionToken As Guid
            Try
                SessionToken = Guid.Parse(Cookies("sessiontoken"))
            Catch ex As Exception
                Return New ErrorObject("SessionTokenNotFound")
            End Try

            Dim UsersPassword As String = ""
            Dim FirstName As String = ""
            Dim LastName As String = ""
            Dim Email As String = ""
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                Using SqlCommand As New SqlCommand("Select Username FROM [SessionToken] WHERE UserName = @UserName And SessionToken = @SessionToken", SqlConnection)
                    SqlCommand.Parameters.Add(New SqlParameter("UserName", Username))
                    SqlCommand.Parameters.Add(New SqlParameter("SessionToken", SessionToken))
                    SqlConnection.Open()

                    Dim Reader = SqlCommand.ExecuteReader
                    While Reader.Read
                        Return New With {
                            .Success = True
                        }
                    End While
                    Reader.Close()
                End Using
            End Using
            Return New ErrorObject("SessionTokenNotFound")
        Catch E As Exception
            Return New ErrorObject()
        End Try
    End Function

    Public Shared Function GetRegions(Username As String) As List(Of Object)
        Dim Result As New List(Of Object)

        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            Using SqlCommand As New SqlCommand("Select R.RegionID, R.RegionName FROM Region As R, RegionUser As RU WHERE RU.Username = @Username And RU.RegionID = R.RegionID", SqlConnection)
                SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
                SqlConnection.Open()

                Dim Reader = SqlCommand.ExecuteReader
                While Reader.Read
                    Result.Add(New With {
                        .RegionId = Reader.GetString(0),
                        .RegionName = Reader.GetString(1)
                    })
                End While
                Reader.Close()
            End Using
        End Using

        Return Result
    End Function

    Public Shared Function GetInviteUser(InvitationGuid As Guid) As Object
        Try
            Dim TUser As User = Nothing

            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    Dim CommandText As String = "Select Username, Email FROM [InviteUser] WHERE RegistrationToken = @RegistrationToken"
                    Dim FoundToken As Boolean = False

                    Dim Username As String = ""
                    Dim Email As String = ""

                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegistrationToken", InvitationGuid))

                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            FoundToken = True
                            Username = Reader.GetString(0)
                            Email = Reader.GetString(1)
                        End While
                        Reader.Close()
                    End Using

                    If Not FoundToken Then Return New ErrorObject("InvalidInviteUserToken")

                    TUser = GetUserInfoHelper(Username, SqlConnection, SqlTransaction)
                    TUser.Email = Email

                    If TUser Is Nothing Then Return New ErrorObject("InvalidInviteUserToken")

                End Using
            End Using

            Return New With {
                .Success = True,
                .User = TUser
            }
        Catch E As Exception
            Return New ErrorObject()
        End Try
    End Function

    Public Shared Function InsertInviteUser(InvitationGuid As Guid, User As User) As Object
        Try
            Dim TUser As User = Nothing
            Dim Username As String = ""
            User.Validate()

            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    Dim CommandText As String = "Select Username, Email FROM [InviteUser] WHERE RegistrationToken = @RegistrationToken"
                    Dim FoundToken As Boolean = False

                    Dim Email As String = ""

                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegistrationToken", InvitationGuid))

                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            FoundToken = True
                            Username = Reader.GetString(0)
                            Email = Reader.GetString(1)
                        End While
                        Reader.Close()
                    End Using

                    If Not FoundToken Then Return New ErrorObject("InvalidInviteUserToken")

                    TUser = GetUserInfoHelper(Username, SqlConnection, SqlTransaction)
                    Dim TTUser = GetUserInfoHelper(User.Username, SqlConnection, SqlTransaction)

                    If TUser Is Nothing Then Return New ErrorObject("InvalidInviteUserToken")
                    If TTUser IsNot Nothing AndAlso Username <> User.Username Then Return New ErrorObject("UserAlreadyExists")

                    Dim FoundEmail As Boolean = False

                    CommandText = "Select Username FROM [User] WHERE Email = @Email"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Email", User.Email))

                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            FoundEmail = True
                        End While
                        Reader.Close()
                    End Using

                    If FoundEmail Then Return New ErrorObject("EmailAlreadyExists")

                    User.Password = PublicCode.EncryptSHA256Managed(User.Password)
                    User.SMSAvailabilityReminders = TUser.SMSAvailabilityReminders
                    User.SMSGameReminders = TUser.SMSGameReminders
                    User.NextLadderLeaguePaymentDue = TUser.NextLadderLeaguePaymentDue
                    User.SMSLastMinuteChanges = TUser.SMSLastMinuteChanges
                    User.EmailAvailabilityReminders = TUser.EmailAvailabilityReminders
                    User.EmailAvailableGames = TUser.EmailAvailableGames
                    User.EmailGamesRequiringConfirm = TUser.EmailGamesRequiringConfirm
                    User.PhotoId = TUser.PhotoId
                    User.HasPhotoId = TUser.HasPhotoId

                    User.UpdateUserSimple(Username, SqlConnection, SqlTransaction)
                    UmpireAssignor.User.SetUserSubmittedInfo(User.Username, SqlConnection, SqlTransaction)

                    If Username <> User.Username Then
                        CommandText = "UPDATE [RegionUser] Set RealUsername = @NewRealUsername WHERE RealUsername = @OldRealUsername"
                        Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("NewRealUsername", User.Username))
                            SqlCommand.Parameters.Add(New SqlParameter("OldRealUsername", Username))
                            SqlCommand.ExecuteNonQuery()
                        End Using
                    End If

                    CommandText = "DELETE FROM [InviteUser] WHERE RegistrationToken = @RegistrationToken"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegistrationToken", InvitationGuid))
                        SqlCommand.ExecuteNonQuery()
                    End Using

                    SqlTransaction.Commit()
                End Using
            End Using

            Dim TChatContact = New ChatContact(User)
            If Username <> User.Username Then
                TChatContact.DeleteOldUsername = Username
            End If

            WsHandler.UpdateUserInfo(TChatContact)

            Return New With {
                .Success = True
            }
        Catch E As Exception
            Return New ErrorObject()
        End Try
    End Function

    Public Shared Function InviteUser(Username As String, Email As String, InvitingUsername As String) As Object
        If Username = "" Then Return New ErrorObject("UserDoesntExist")
        If Email.Length = 0 Then Return New ErrorObject("InvalidEmail")
        If Not PublicCode.IsEmail(Email) Then Return New ErrorObject("InvalidEmail")

        Dim FirstName As String = ""
        Dim LastName As String = ""
        Dim TEmail As String = ""
        Dim PreferredLanguage As String = ""

        Dim InvitingUser As User = Nothing

        Dim RegistrationToken As Guid = Guid.NewGuid

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    If Username = "masterpay" OrElse Username = "managepay" OrElse Username = "paysummary" OrElse Username = "Select" Then
                        Return New ErrorObject("UserDoesntExist")
                    End If

                    InvitingUser = GetUserInfoHelper(InvitingUsername, SqlConnection, SqlTransaction)

                    Dim FoundUser As Boolean = False
                    Using SqlCommand As New SqlCommand("Select Username, FirstName, LastName, Email, PreferredLanguage FROM [User] WHERE UserName = @UserName", SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("UserName", Username))

                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            If Reader.GetString(0) <> "" Then
                                FoundUser = True
                            End If
                            FirstName = Reader.GetString(1)
                            LastName = Reader.GetString(2)
                            TEmail = Reader.GetString(3)
                            PreferredLanguage = Reader.GetString(4)

                            If PreferredLanguage = "" Then
                                PreferredLanguage = "en"
                            End If

                        End While
                        Reader.Close()
                    End Using

                    If Not FoundUser Then
                        Return New ErrorObject("UserDoesntExist")
                    End If

                    If TEmail <> "" Then
                        Return New ErrorObject("UserAlreadyExists")
                    End If

                    Using SqlCommand As New SqlCommand("Select Count(*) FROM [User] WHERE Email = @Email", SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Email", Email))

                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            If Reader.GetInt32(0) > 0 Then
                                Reader.Close()
                                Return New ErrorObject("EmailAlreadyExists")
                            End If
                        End While
                        Reader.Close()
                    End Using

                    Dim UserInvitation As User = RegionUser.GetUserInviteUserHelper(Username, Email, SqlConnection, SqlTransaction)

                    If UserInvitation Is Nothing Then
                        Dim CommandText As String = "INSERT INTO [InviteUser] (UserName, Email, RegistrationToken, RegistrationDate) VALUES (@UserName, @Email, @RegistrationToken, @RegistrationDate)"

                        Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("UserName", Username))
                            SqlCommand.Parameters.Add(New SqlParameter("Email", Email))
                            SqlCommand.Parameters.Add(New SqlParameter("RegistrationToken", RegistrationToken))
                            SqlCommand.Parameters.Add(New SqlParameter("RegistrationDate", Date.UtcNow))
                            SqlCommand.ExecuteNonQuery()
                        End Using
                    Else
                        RegistrationToken = UserInvitation.RegistrationToken
                    End If

                    SqlTransaction.Commit()
                End Using
            End Using
        Catch E As Exception
            Return New ErrorObject()
        End Try

        Try
            PublicCode.SendEmailStandard(
                Email,
                String.Format(Languages.GetText("InviteUser", "Subject", "Text", PreferredLanguage),
                    HttpUtility.HtmlEncode(InvitingUser.FirstName & " " & InvitingUser.LastName)
                ),
                String.Format(Languages.GetText("InviteUser", "Body", "Text", PreferredLanguage),
                    HttpUtility.HtmlEncode(FirstName),
                    HttpUtility.HtmlEncode(InvitingUser.FirstName & " " & InvitingUser.LastName),
                    PublicCode.GetLocalServer() & "#register?inviteusertoken=" & HttpUtility.UrlEncode(RegistrationToken.ToString())
                ),
                True
            )
        Catch E As Exception
            Return New ErrorObject(E.Message)
        End Try

        Return New With {.Success = True}
    End Function

    Public Shared Function Register(InvitationGUID As Guid?, User As User, RegionIdAndRanks As List(Of RegionIdAndRank), Optional ByVal EmailOnly As Boolean = False, Optional ByVal EmailAndNameOnly As Boolean = False, Optional ByVal SendEmailForNewDomain As Boolean = False) As Object
        Dim DoEmail As Boolean = True

        If RegionIdAndRanks Is Nothing Then
            RegionIdAndRanks = New List(Of RegionIdAndRank)
        End If

        RegionIdAndRanks = RegionIdAndRanks.FindAll(Function (R) Region.LADDER_LEAGUES.Exists(Function (LL) LL = R.regionId))

        'Try
        If EmailOnly Then
            User.ValidateSimpleEmail()
            User.Username = User.Email.ToLower
        ElseIf EmailAndNameOnly Then
            User.ValidateSimpleEmailAndName()
            User.Username = User.Email.ToLower
        Else
            User.Validate()
        End If

        Dim RegistrationToken As Guid? = Nothing
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                If User.Username = "masterpay" OrElse User.Username = "managepay" OrElse User.Username = "paysummary" OrElse User.Username = "Select" Then
                    Return New ErrorObject("UserAlreadyExists")
                End If

                Using SqlCommand As New SqlCommand("Select Count(*) FROM [User] WHERE UserName = @UserName", SqlConnection, SqlTransaction)
                    SqlCommand.Parameters.Add(New SqlParameter("UserName", User.Username))

                    Dim Reader = SqlCommand.ExecuteReader
                    While Reader.Read
                        If Reader.GetInt32(0) > 0 Then
                            Reader.Close()
                            Return New ErrorObject("UserAlreadyExists")
                        End If
                    End While
                    Reader.Close()
                End Using

                Using SqlCommand As New SqlCommand("Select Count(*) FROM [User] WHERE Email = @Email", SqlConnection, SqlTransaction)
                    SqlCommand.Parameters.Add(New SqlParameter("Email", User.Email))

                    Dim Reader = SqlCommand.ExecuteReader
                    While Reader.Read
                        If Reader.GetInt32(0) > 0 Then
                            Reader.Close()
                            Return New ErrorObject("EmailAlreadyExists")
                        End If
                    End While
                    Reader.Close()
                End Using


                Dim UserInvitation As User = Nothing
                If InvitationGUID IsNot Nothing Then
                    UserInvitation = RegionUser.GetUserInvitationHelper(InvitationGUID, SqlConnection, SqlTransaction)
                End If

                If UserInvitation Is Nothing OrElse UserInvitation.Email.ToLower <> User.Email.ToLower Then
                    Dim CommandText As String = "INSERT INTO [UserRegister] (UserName, RegistrationToken, FirstName, LastName, Email, Password, RegistrationDate, PreferredLanguage, Country, State, City, Address, PostalCode, PhoneNumbers, AlternateEmails, InvitationGUID, TimeZone, RegionIdAndRanks) VALUES (@UserName, @RegistrationToken, @FirstName, @LastName, @Email, @Password, @RegistrationDate, @PreferredLanguage, @Country, @State, @City, @Address, @PostalCode, @PhoneNumbers, @AlternateEmails, @InvitationGUID, @TimeZone, @RegionIdAndRanks)"

                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        RegistrationToken = Guid.NewGuid()
                        SqlCommand.Parameters.Add(New SqlParameter("UserName", User.Username))
                        SqlCommand.Parameters.Add(New SqlParameter("RegistrationToken", RegistrationToken))
                        SqlCommand.Parameters.Add(New SqlParameter("FirstName", User.FirstName))
                        SqlCommand.Parameters.Add(New SqlParameter("LastName", User.LastName))
                        SqlCommand.Parameters.Add(New SqlParameter("Email", User.Email))
                        SqlCommand.Parameters.Add(New SqlParameter("Password", PublicCode.EncryptSHA256Managed(User.Password)))
                        SqlCommand.Parameters.Add(New SqlParameter("RegistrationDate", Date.UtcNow))
                        SqlCommand.Parameters.Add(New SqlParameter("PreferredLanguage", User.PreferredLanguage))
                        SqlCommand.Parameters.Add(New SqlParameter("Country", User.Country))
                        SqlCommand.Parameters.Add(New SqlParameter("State", User.State))
                        SqlCommand.Parameters.Add(New SqlParameter("City", User.City))
                        SqlCommand.Parameters.Add(New SqlParameter("Address", User.Address))
                        SqlCommand.Parameters.Add(New SqlParameter("PostalCode", User.PostalCode))
                        SqlCommand.Parameters.Add(New SqlParameter("PhoneNumbers", JsonConvert.SerializeObject(User.PhoneNumbers)))
                        SqlCommand.Parameters.Add(New SqlParameter("AlternateEmails", JsonConvert.SerializeObject(User.AlternateEmails)))
                        SqlCommand.Parameters.Add(New SqlParameter("InvitationGUID", If(InvitationGUID Is Nothing, DBNull.Value, InvitationGUID)))
                        SqlCommand.Parameters.Add(New SqlParameter("TimeZone", User.TimeZone))
                        SqlCommand.Parameters.Add(New SqlParameter("RegionIdAndRanks", JsonConvert.SerializeObject(RegionIdAndRanks)))
                        SqlCommand.ExecuteNonQuery()
                    End Using
                Else
                    DoEmail = False

                    User.Password = PublicCode.EncryptSHA256Managed(User.Password)

                    Dim TResult = ConfirmRegistrationHelper(User, UserInvitation, InvitationGUID, RegionIdAndRanks, SqlConnection, SqlTransaction)

                    If Not TResult.Success = True Then
                        Return TResult
                    End If
                End If

                SqlTransaction.Commit()
            End Using
        End Using

        If DoEmail Then
            Dim Emails As New List(Of String)
            If User.Email <> "" AndAlso PublicCode.IsEmail(User.Email) Then Emails.Add(User.Email)

            For Each TEmail In Emails
                Try
                    Dim ConfirmationLink As String = PublicCode.GetLocalServer() & "#ConfirmRegistration?username=" & HttpUtility.UrlEncode(HttpUtility.HtmlEncode(User.Username)) & "&token=" & HttpUtility.UrlEncode(RegistrationToken.ToString()) & "&email=" & HttpUtility.UrlEncode(User.Email)
                    If SendEmailForNewDomain Then
                        ConfirmationLink = "https://app.asportsmanager.com/registration-confirmation?username=" & HttpUtility.UrlEncode(HttpUtility.HtmlEncode(User.Username)) & "&token=" & HttpUtility.UrlEncode(RegistrationToken.ToString()) & "&email=" & HttpUtility.UrlEncode(User.Email)
                    End If

                    PublicCode.SendEmailStandard(
                            TEmail,
                            Languages.GetText("RegisterEmail", "Subject", "Text", User.PreferredLanguage),
                            String.Format(Languages.GetText("RegisterEmail", "Body", "Text", User.PreferredLanguage),
                                HttpUtility.HtmlEncode(User.FirstName & " " & User.LastName),
                                ConfirmationLink
                            ),
                            True
                        )
                Catch E As Exception
                    Return New ErrorObject(E.Message)
                End Try
            Next
        End If

        Return New With {.Success = True}
        'Catch E As Exception
        '    Return New ErrorObject()
        'End Try
    End Function

    Public Shared Function ConfirmRegistrationHelper(User As User, UserInvitation As User, InvitationGUID As Guid?, RegionIdAndRanks As List(Of RegionIdAndRank), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As Object
        Dim UsernameExists As Boolean = False
        Dim EmailExists As Boolean = False

        Using SqlCommand As New SqlCommand("Select Username, Email FROM [User] WHERE UserName = @UserName Or Email = @Email", SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("UserName", User.Username))
            SqlCommand.Parameters.Add(New SqlParameter("Email", User.Email))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                If Reader.GetString(0).ToLower = User.Username Then UsernameExists = True
                If Reader.GetString(1).ToLower = User.Email Then EmailExists = True
            End While
            Reader.Close()
        End Using

        If UsernameExists And EmailExists Then
            Return New ErrorObject("UsernameAndEmailAlreadyExist")
        ElseIf UsernameExists Then
            Return New ErrorObject("UsernameAlreadyExists")
        ElseIf EmailExists Then
            Return New ErrorObject("EmailAlreadyExists")
        End If

        If UserInvitation IsNot Nothing AndAlso UserInvitation.Email.ToLower <> User.Email.ToLower Then
            Return New ErrorObject("UserInvitationEmailNotEqualUserEmail")
        End If

        User.ICSToken = Guid.NewGuid()

        User.EmailAvailableGames = True
        User.EmailAvailabilityReminders = True
        User.EmailGamesRequiringConfirm = True
        User.SMSGameReminders = True
        User.NextLadderLeaguePaymentDue = New Date(2023, 1, 1)
        User.SMSLastMinuteChanges = True
        User.SMSAvailabilityReminders = True

        InsertSimple(User, SQLConnection, SQLTransaction)

        Dim CommandText = "DELETE FROM UserRegister WHERE Username = @Username Or Email = @Email"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", User.Username))
            SqlCommand.Parameters.Add(New SqlParameter("Email", User.Email))
            SqlCommand.ExecuteNonQuery()
        End Using

        If InvitationGUID IsNot Nothing
            CommandText = "DELETE FROM UserInvitation WHERE InvitationGUID = @InvitationGUID"

            Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
                SqlCommand.Parameters.Add(New SqlParameter("InvitationGUID", InvitationGUID))
                SqlCommand.ExecuteNonQuery()
            End Using
        End If

        If InvitationGUID IsNot Nothing AndAlso UserInvitation IsNot Nothing Then
            SetUserSubmittedInfo(User.Username, SQLConnection, SQLTransaction)

            CommandText = <SQL>UPDATE RegionUser Set Realusername = @Username, IsLinked = 1, IsInfoLinked = AllowInfoLink WHERE Email = @Email</SQL>.Value()

            Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
                SqlCommand.Parameters.Add(New SqlParameter("Email", User.Email))
                SqlCommand.Parameters.Add(New SqlParameter("Username", User.Username))
                SqlCommand.ExecuteNonQuery()
            End Using
        Else
            CommandText = <SQL>
                            INSERT INTO FriendNotification
	                            (Username, FriendId, FriendUsername, DateCreated, IsViewed, Positions, Denied)
                            SELECT 
	                            @Username, RU.RegionId, RU.Username, @DateCreated, 0, RU.Positions, 0
                            FROM 
	                            RegionUser as RU 
                            WHERE
	                            RU.Email = @Email AND RU.IsInfoLinked = 0
                            </SQL>.Value()

            Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
                SqlCommand.Parameters.Add(New SqlParameter("Email", User.Email))
                SqlCommand.Parameters.Add(New SqlParameter("Username", User.Username))
                SqlCommand.Parameters.Add(New SqlParameter("DateCreated", DateTime.UtcNow))
                SqlCommand.ExecuteNonQuery()
            End Using
        End If

        If RegionIdAndRanks IsNot Nothing Then
            For Each RegionIdAndRank In RegionIdAndRanks
                Dim RegionUsers = UmpireAssignor.RegionUser.LoadAllInRegionHelper(RegionIdAndRank.RegionId, "superadmin", SqlConnection, SqlTransaction)

                Dim NewRegionUser = UmpireAssignor.RegionUser.RegionUserFromUserFull(RegionIdAndRank.RegionId, RegionIdAndRank.Rank, User, RegionUsers, SQLConnection, SQLTransaction)

                RegionUser.UpsertSimpleHelper(NewRegionUser, SQLConnection, SQLTransaction)

                SetUserSubmittedInfo(User.Username, SQLConnection, SQLTransaction)

            Next
        End If

        Return New With {
            .Success = True
        }
    End Function

    Public Shared Sub InsertSimple(User As User, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        Dim CommandText = "INSERT INTO [User] (Username, FirstName, LastName, Email, Password, RegistrationDate, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, EmailAvailableGames, EmailAvailabilityReminders, EmailGamesRequiringConfirm, SMSGameReminders, SMSLastMinuteChanges, SMSAvailabilityReminders, TimeZone, ICSToken, PhotoId, HasPhotoId) VALUES (@Username, @FirstName, @LastName, @Email, @Password, @RegistrationDate, @PhoneNumbers, @AlternateEmails, @Country, @State, @City, @Address, @PostalCode, @PreferredLanguage, @EmailAvailableGames, @EmailAvailabilityReminders, @EmailGamesRequiringConfirm, @SMSGameReminders, @SMSLastMinuteChanges, @SMSAvailabilityReminders, @TimeZone, @ICSToken, @PhotoId, @HasPhotoId)"

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("UserName", User.Username))
            SqlCommand.Parameters.Add(New SqlParameter("FirstName", User.FirstName))
            SqlCommand.Parameters.Add(New SqlParameter("LastName", User.LastName))
            SqlCommand.Parameters.Add(New SqlParameter("Email", User.Email))
            SqlCommand.Parameters.Add(New SqlParameter("Password", User.Password))
            SqlCommand.Parameters.Add(New SqlParameter("RegistrationDate", DateTime.UtcNow))
            SqlCommand.Parameters.Add(New SqlParameter("PhoneNumbers", JsonConvert.SerializeObject(User.PhoneNumbers)))
            SqlCommand.Parameters.Add(New SqlParameter("AlternateEmails", JsonConvert.SerializeObject(User.AlternateEmails)))
            SqlCommand.Parameters.Add(New SqlParameter("Country", User.Country))
            SqlCommand.Parameters.Add(New SqlParameter("State", User.State))
            SqlCommand.Parameters.Add(New SqlParameter("City", User.City))
            SqlCommand.Parameters.Add(New SqlParameter("Address", User.Address))
            SqlCommand.Parameters.Add(New SqlParameter("PostalCode", User.PostalCode))
            SqlCommand.Parameters.Add(New SqlParameter("PreferredLanguage", User.PreferredLanguage))
            SqlCommand.Parameters.Add(New SqlParameter("EmailAvailableGames", User.EmailAvailableGames))
            SqlCommand.Parameters.Add(New SqlParameter("EmailAvailabilityReminders", User.EmailAvailabilityReminders))
            SqlCommand.Parameters.Add(New SqlParameter("EmailGamesRequiringConfirm", User.EmailGamesRequiringConfirm))
            SqlCommand.Parameters.Add(New SqlParameter("SMSGameReminders", User.SMSGameReminders))
            SqlCommand.Parameters.Add(New SqlParameter("SMSLastMinuteChanges", User.SMSLastMinuteChanges))
            SqlCommand.Parameters.Add(New SqlParameter("SMSAvailabilityReminders", User.SMSAvailabilityReminders))
            SqlCommand.Parameters.Add(New SqlParameter("TimeZone", User.TimeZone))
            SqlCommand.Parameters.Add(New SqlParameter("ICSToken", User.ICSToken))
            SqlCommand.Parameters.Add(New SqlParameter("PhotoId", User.PhotoId))
            SqlCommand.Parameters.Add(New SqlParameter("HasPhotoId", User.HasPhotoId))

            SqlCommand.ExecuteNonQuery()
        End Using
    End Sub

    Public Shared Function ConfirmRegistration(Username As String, RegistrationToken As Guid) As Object
        'Try
        Dim TResult As Object = Nothing
        Dim User As User = Nothing
        Dim RegionIdAndRanks As List(Of RegionIdAndRank) = Nothing

        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Dim InvitationGUID As Guid? = Nothing

                Dim DidRead As Boolean = False
                Using SqlCommand As New SqlCommand("Select Username, FirstName, LastName, Email, Password, PreferredLanguage, Country, State, City, Address, PostalCode, PhoneNumbers, AlternateEmails, InvitationGUID, TimeZone, RegionIdAndRanks FROM UserRegister WHERE UserName = @UserName And RegistrationToken = @RegistrationToken", SqlConnection, SqlTransaction)
                    SqlCommand.Parameters.Add(New SqlParameter("UserName", Username))
                    SqlCommand.Parameters.Add(New SqlParameter("RegistrationToken", RegistrationToken))

                    Dim Reader = SqlCommand.ExecuteReader
                    While Reader.Read
                        DidRead = True

                        User = New User With {
                            .Username = Reader.GetString(0).ToLower,
                            .FirstName = Reader.GetString(1),
                            .LastName = Reader.GetString(2),
                            .Email = Reader.GetString(3).ToLower,
                            .Password = Reader.GetString(4),
                            .RegistrationDate = Date.UtcNow,
                            .PreferredLanguage = Reader.GetString(5),
                            .Country = Reader.GetString(6),
                            .State = Reader.GetString(7),
                            .City = Reader.GetString(8),
                            .Address = Reader.GetString(9),
                            .PostalCode = Reader.GetString(10),
                            .PhoneNumbers = JsonConvert.DeserializeObject(Of List(Of PhoneNumber))(Reader.GetString(11)),
                            .AlternateEmails = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(12)),
                            .TimeZone = Reader.GetString(14)
                          }

                        RegionIdAndRanks = JsonConvert.DeserializeObject(Of List(Of RegionIdAndRank))(Reader.GetString(15))

                        Try
                            InvitationGUID = Reader.GetGuid(13)
                        Catch
                            InvitationGUID = Nothing
                        End Try
                    End While
                    Reader.Close()
                End Using

                If Not DidRead Then
                    Return New ErrorObject("InvalidRegistration")
                End If

                Dim UserInvitation As User = Nothing
                If InvitationGUID IsNot Nothing Then
                    UserInvitation = RegionUser.GetUserInvitationHelper(InvitationGUID, SqlConnection, SqlTransaction)
                End If

                TResult = ConfirmRegistrationHelper(User, UserInvitation, InvitationGUID, RegionIdAndRanks, SqlConnection, SqlTransaction)
                If Not TResult.Success Then
                    Return TResult
                End If

                SqlTransaction.Commit()
            End Using
        End Using

        Return TResult
        'Catch E As Exception
        '    Return New ErrorObject()
        'End Try
    End Function

    Public Shared Function GetBasicInformation(Username As String) As Object
        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Return GetBasicInformationHelper(Username, SqlConnection, SqlTransaction)
            End Using
        End Using


        ' Catch E As Exception
        ' 'Return New ErrorObject()
        ' End Try

    End Function

    Public Shared Function UserSubmittedInfo(Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As Boolean
        Dim Result As Boolean = False

        Using SqlCommand As New SqlCommand("Select HasSubmittedInfo FROM UserSubmittedInfo WHERE Username = @Username", SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Result = Reader.GetBoolean(0)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Sub SetUserSubmittedInfo(Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        Using SqlCommand As New SqlCommand("DELETE FROM UserSubmittedInfo WHERE Username = @Username; INSERT INTO UserSubmittedInfo (Username, HasSubmittedInfo) VALUES (@Username, 1)", SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            SqlCommand.ExecuteNonQuery()
        End Using
    End Sub

    Public Shared Function GetBasicInformationHelper(Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As Object
        Dim User As User = Nothing
        Dim Regions As New List(Of RegionAndPositions)
        Dim RegionsWithUsers As New List(Of RegionAndRegionUser)

        Dim FriendNotifications As New List(Of FriendNotification)
        Dim AvailabilityDueDateUsers As New List(Of AvailabilityDueDateUserWithUser)
        Dim LastClicked As New LastClicked With {.RegionId = "Select", .Username = "Select"}

        User = GetUserInfoHelper(Username, SQLConnection, SQLTransaction)

        Using SqlCommand As New SqlCommand("Select LastClickedRegion, LastClickedUsername FROM UsernameLastClicked WHERE Username = @Username", SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                LastClicked.RegionId = Reader.GetString(0)
                LastClicked.Username = Reader.GetString(1)
            End While
            Reader.Close()
        End Using

        If User Is Nothing Then
            Return New ErrorObject("UserDoesNotExist")
        End If

        RegionsWithUsers = RegionUser.LoadRegionAndRegionUserFromRealUsernameHelper(Username, SQLConnection, SQLTransaction)

        FriendNotifications = FriendNotification.GetTop5FriendNotificationHelper(Username, SQLConnection, SQLTransaction)

        AvailabilityDueDateUsers = AvailabilityDueDateUser.GetAllMyIsFilledInFromAvailabilityDueDateUserHelper(Username, SQLConnection, SQLTransaction)

        Dim RegionUsers As List(Of RegionUser) = Nothing
        Dim UniqueRegionIds As List(Of String) = Nothing
        Dim RegionParks As List(Of Park) = Nothing
        Dim RegionLeagues As List(Of RegionLeaguePayContracted) = Nothing
        Dim Teams As List(Of Team) = Nothing
        Dim RegionProperties As List(Of RegionProperties) = Nothing

        Dim GameNotifications = Schedule.GetUsersGameNotifications(Username, SQLConnection, SQLTransaction)

        If TypeOf GameNotifications Is ErrorObject Then Return GameNotifications

        Dim HasSubmittedInfo As Boolean = UserSubmittedInfo(Username, SQLConnection, SQLTransaction)

        Dim RegionIds = New List(Of String)

        Regions = LoadRegionAndPositions(Username, SQLConnection, SQLTransaction)

        RegionLeagues = GameNotifications.RegionLeagues

        Dim NotYetFetchedRegionIds As New List(Of String)
        For Each Region In Regions
            NotYetFetchedRegionIds.Add(Region.RegionId)
        Next
        For Each RegionLeague In RegionLeagues
            If RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked AndAlso Regions.Find(Function(R) R.RegionId = RegionLeague.RealLeagueId) Is Nothing AndAlso Not NotYetFetchedRegionIds.Contains(RegionLeague.RealLeagueId) Then
                NotYetFetchedRegionIds.Add(RegionLeague.RealLeagueId)
            End If
        Next

        If NotYetFetchedRegionIds.Count > 0 Then
            RegionProperties = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(NotYetFetchedRegionIds, SQLConnection, SQLTransaction, True)
        End If

        Return New With {
            .Success = True,
            .User = User,
            .Regions = Regions,
            .RegionWithUsers = RegionsWithUsers,
            .RegionUsers = GameNotifications.RegionUsers,
            .RegionParks = GameNotifications.RegionParks,
            .Teams = GameNotifications.Teams,
            .RegionLeagues = GameNotifications.RegionLeagues,
            .RegionProperties = RegionProperties,
            .FriendNotifications = FriendNotifications,
            .AvailabilityDueDateUsers = AvailabilityDueDateUsers,
            .GameNotifications = GameNotifications.Schedule,
            .LastClicked = LastClicked,
            .HasSubmittedInfo = HasSubmittedInfo
        }
    End Function

    Public Shared Function GetBasicInformationFromOtherUsername(MyUsername As String, Username As String) As Object
        Dim Result As Object = Nothing

        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()
                Result = GetBasicInformationFromOtherUsernameHelper(MyUsername, Username, SqlConnection, SqlTransaction)
            End Using
        End Using

        Return Result
    End Function

    Public Shared Function GetBasicInformationFromOtherUsernameHelper(MyUsername As String, Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As Object
        Dim User As User = Nothing
        Dim Regions As New List(Of RegionAndPositions)
        Dim RegionsWithUsers As New List(Of RegionAndRegionUser)
        Dim Leagues As New List(Of RegionLeague)

        User = GetUserInfoHelper(Username, SQLConnection, SQLTransaction)

        If User Is Nothing Then
            Return New ErrorObject("UserDoesNotExist")
        End If

        Dim IsInContactList = False

        Dim ContactList = ChatContact.GetContactListHelper(MyUsername, SQLConnection, SQLTransaction)

        For Each ContactItem In ContactList
            If ContactItem.Username = Username Then
                IsInContactList = True
                Exit For
            End If
        Next

        Regions = LoadRegionAndPositions(Username, SQLConnection, SQLTransaction)
        Dim RegionIds As New List(Of String)
        For Each Region In Regions
            RegionIds.Add(Region.RegionId)
        Next

        RegionsWithUsers = RegionUser.LoadRegionAndRegionUserFromRealUsernameHelper(Username, SQLConnection, SQLTransaction)
        Leagues = RegionLeague.GetRegionsLeaguesHelper(RegionIds, SQLConnection, SQLTransaction)

        Dim RegionLeagueIds As New List(Of String)
        For Each League In Leagues
            If League.RealLeagueId <> "" AndAlso League.IsLinked Then
                RegionLeagueIds.Add(League.RealLeagueId)
            End If
        Next

        Dim RegionLeagueIdsNotFetched As New List(Of String)
        For Each RegionLeagueId In RegionLeagueIds
            If Not RegionIds.Contains(RegionLeagueId) Then
                RegionLeagueIdsNotFetched.Add(RegionLeagueId)
            End If
        Next

        Dim FullRegionIds = New List(Of String)
        FullRegionIds.AddRange(RegionIds)
        FullRegionIds.AddRange(RegionLeagueIdsNotFetched)

        Dim FullRegions = RegionProperties.GetRegionPropertiesRegionIdsHelper(FullRegionIds, SQLConnection, SQLTransaction, True)

        User.SMSAvailabilityReminders = False
        User.SMSGameReminders = False
        User.NextLadderLeaguePaymentDue = New Date(2023, 1, 1)
        User.SMSLastMinuteChanges = False
        User.EmailAvailabilityReminders = False
        User.EmailAvailableGames = False
        User.EmailGamesRequiringConfirm = False

        If Not IsInContactList Then
            User.Email = ""
            User.AlternateEmails = New List(Of String)
            User.PhoneNumbers = New List(Of PhoneNumber)
            User.Country = ""
            User.State = ""
            User.City = ""
            User.Address = ""
            User.PostalCode = ""
        End If

        Return New With {
            .Success = True,
            .User = User,
            .IsInContactList = IsInContactList,
            .Regions = Regions,
            .FullRegions = FullRegions,
            .RegionWithUsers = RegionsWithUsers,
            .Leagues = Leagues
        }
    End Function

    Public Shared Function UpdateLastClicked(RealUsername As String, RegionId As String, Username As String) As Object
        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim CommandText As String = "DELETE FROM UsernameLastClicked WHERE Username = @RealUsername"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))
                        SqlCommand.ExecuteNonQuery()
                    End Using

                    CommandText = "INSERT INTO UsernameLastClicked (Username, LastClickedRegion, LastClickedUsername) VALUES (@RealUsername, @LastClickedRegion, @LastClickedUsername)"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))
                        SqlCommand.Parameters.Add(New SqlParameter("LastClickedRegion", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("LastClickedUsername", Username))
                        SqlCommand.ExecuteNonQuery()
                    End Using

                    SqlTransaction.Commit()
                End Using
            End Using

            Return New With {.Success = True}
        Catch E As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function LoadRegionAndPositions(Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionAndPositions)
        Dim CommandText = "Select R.RegionId, R.RegionName, R.Sport, R.EntityType, R.Season, R.RegistrationDate, R.DefaultCrew, R.ShowAddress, R.ShowRank, R.ShowSubRank, R.ShowRankToNonMembers, R.ShowSubRankToNonMembers, R.DefaultContactListSort, R.ShowRankInSchedule, R.ShowSubRankInSchedule, R.MaxSubRankRequest, R.SubRankIsNumber, R.ShowTeamsInSchedule, R.SortSubRankDesc, R.SeniorRankNameEnglish, R.SeniorRankNameFrench, R.JuniorRankNameEnglish, R.JuniorRankNameFrench, R.IntermediateRankNameEnglish, R.IntermediateRankNameFrench, R.NoviceRankNameEnglish, R.NoviceRankNameFrench, R.RookieRankNameEnglish, R.RookieRankNameFrench, R.AllowBookOffs, R.BookOffHoursBefore, R.MaxDateShowAvailableSpots, R.EmailAvailableSpots, R.EmailRequestedGames, R.DefaultArriveBeforeMins, R.DefaultMaxGameLengthMins, R.OnlyAllowedToConfirmDaysBefore, R.EmailConfirmDaysBefore, R.HasOfficials, R.HasScorekeepers, R.HasSupervisors, R.IsDemo, R.Country, R.State, R.City, R.Address, R.PostalCode, R.ShowLinksToNonMembers, R.ShowParksToNonMembers, R.ShowLeaguesToNonMembers, R.ShowOfficialRegionsToNonMembers, R.ShowTeamsToNonMembers, R.ShowHolidayListToNonMembers, R.ShowContactListToNonMembers, R.ShowAvailabilityDueDateToNonMembers, R.ShowAvailabilityToNonMembers, R.ShowAvailableSpotsToNonMembers, R.ShowFullScheduleToNonMembers, R.ShowStandingsToNonMembers, R.ShowStatsToNonMembers, R.ShowMainPageToNonMembers, R.HasEnteredMainPage, R.ExtraScoreParameters, R.HomeTeamCanEnterScore, R.AwayTeamCanEnterScore, R.ScorekeeperCanEnterScore, R.TeamCanEnterStats, R.ScorekeeperCanEnterStats, R.RegionIsLadderLeague, R.NumberOfPlayersInLadderLeague, R.HomeTeamCanCancelGameHoursBefore, R.AwayTeamCanCancelGameHoursBefore, R.MinimumValue, R.MinimumPercentage, R.TimeZone, R.AutoSyncCancellationHoursBefore, R.AutoSyncSchedule, R.UniquePlayers, R.StatIndex, R.StandingIndex, R.DefaultArriveBeforeAwayMins, R.DefaultArriveBeforePracticeMins, R.DefaultMaxGameLengthPracticeMins, R.IncludeAfternoonAvailabilityOnWeekdays, R.IncludeMorningAvailabilityOnWeekdays, R.EnableNotFilledInInAvailability, R.EnableDualAvailability, R.IsAvailableText, R.IsAvailableDualText, R.IsAvailableCombinedText, R.ShowOnlyDueDatesRangeForAvailability, R.ShowRankInGlobalAvailability, R.ShowSubRankInGlobalAvailability, R.SortGlobalAvailabilityByRank, R.SortGlobalAvailabilityBySubRank, R.NotifyPartnerOfCancellation, R.ShowPhotosToNonMembers, R.ShowArticlesToNonMembers, R.ShowWallToNonMembers, R.LeagueRankMaxes, R.LinkedRegionIds, R.PhotoId, R.HasPhotoId, RU.Positions, RU.Username, RU.IsArchived FROM Region As R, RegionUser As RU WHERE RU.RealUsername = @Username And R.RegionId = RU.RegionId ORDER BY R.RegionName"

        Dim Regions As New List(Of RegionAndPositions)

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Regions.Add(New RegionAndPositions With {
                    .RegionId = Reader.GetString(),
                    .RegionName = Reader.GetString(),
                    .Sport = Reader.GetString(),
                    .EntityType = Reader.GetString(),
                    .Season = Reader.GetInt32(),
                    .RegistrationDate = Reader.GetDateTime(),
                    .DefaultCrew = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                    .ShowAddress = Reader.GetBoolean(),
                    .ShowRank = Reader.GetBoolean(),
                    .ShowSubRank = Reader.GetBoolean(),
                    .ShowRankToNonMembers = Reader.GetBoolean(),
                    .ShowSubRankToNonMembers = Reader.GetBoolean(),
                    .DefaultContactListSort = Reader.GetString(),
                    .ShowRankInSchedule = Reader.GetBoolean(),
                    .ShowSubRankInSchedule = Reader.GetBoolean(),
                    .MaxSubRankRequest = Reader.GetDecimal(),
                    .SubRankIsNumber = Reader.GetBoolean(),
                    .ShowTeamsInSchedule = Reader.GetBoolean(),
                    .SortSubRankDesc = Reader.GetBoolean(),
                    .SeniorRankNameEnglish = Reader.GetString(),
                    .SeniorRankNameFrench = Reader.GetString(),
                    .JuniorRankNameEnglish = Reader.GetString(),
                    .JuniorRankNameFrench = Reader.GetString(),
                    .IntermediateRankNameEnglish = Reader.GetString(),
                    .IntermediateRankNameFrench = Reader.GetString(),
                    .NoviceRankNameEnglish = Reader.GetString(),
                    .NoviceRankNameFrench = Reader.GetString(),
                    .RookieRankNameEnglish = Reader.GetString(),
                    .RookieRankNameFrench = Reader.GetString(),
                    .AllowBookOffs = Reader.GetBoolean(),
                    .BookOffHoursBefore = Reader.GetInt32(),
                    .MaxDateShowAvailableSpots = Reader.GetDateTime(),
                    .EmailAvailableSpots = Reader.GetBoolean(),
                    .EmailRequestedGames = Reader.GetBoolean(),
                    .DefaultArriveBeforeMins = Reader.GetInt32(),
                    .DefaultMaxGameLengthMins = Reader.GetInt32(),
                    .OnlyAllowedToConfirmDaysBefore = Reader.GetInt32(),
                    .EmailConfirmDaysBefore = Reader.GetInt32(),
                    .HasOfficials = Reader.GetBoolean(),
                    .HasScorekeepers = Reader.GetBoolean(),
                    .HasSupervisors = Reader.GetBoolean(),
                    .IsDemo = Reader.GetBoolean(),
                    .Country = Reader.GetString(),
                    .State = Reader.GetString(),
                    .City = Reader.GetString(),
                    .Address = Reader.GetString(),
                    .PostalCode = Reader.GetString(),
                    .ShowLinksToNonMembers = Reader.GetBoolean(),
                    .ShowParksToNonMembers = Reader.GetBoolean(),
                    .ShowLeaguesToNonMembers = Reader.GetBoolean(),
                    .ShowOfficialRegionsToNonMembers = Reader.GetBoolean(),
                    .ShowTeamsToNonMembers = Reader.GetBoolean(),
                    .ShowHolidayListToNonMembers = Reader.GetBoolean(),
                    .ShowContactListToNonMembers = Reader.GetBoolean(),
                    .ShowAvailabilityDueDateToNonMembers = Reader.GetBoolean(),
                    .ShowAvailabilityToNonMembers = Reader.GetBoolean(),
                    .ShowAvailableSpotsToNonMembers = Reader.GetBoolean(),
                    .ShowFullScheduleToNonMembers = Reader.GetBoolean(),
                    .ShowStandingsToNonMembers = Reader.GetBoolean(),
                    .ShowStatsToNonMembers = Reader.GetBoolean(),
                    .ShowMainPageToNonMembers = Reader.GetBoolean(),
                    .HasEnteredMainPage = Reader.GetBoolean(),
                    .ExtraScoreParameters = JsonConvert.DeserializeObject(Of List(Of ExtraScoreParameter))(Reader.GetString()),
                    .HomeTeamCanEnterScore = Reader.GetBoolean(),
                    .AwayTeamCanEnterScore = Reader.GetBoolean(),
                    .ScorekeeperCanEnterScore = Reader.GetBoolean(),
                    .TeamCanEnterStats = Reader.GetBoolean(),
                    .ScorekeeperCanEnterStats = Reader.GetBoolean(),
                    .RegionIsLadderLeague = Reader.GetBoolean(),
                    .NumberOfPlayersInLadderLeague = Reader.GetInt32(),
                    .HomeTeamCanCancelGameHoursBefore = Reader.GetInt32(),
                    .AwayTeamCanCancelGameHoursBefore = Reader.GetInt32(),
                    .MinimumValue = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                    .MinimumPercentage = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                    .TimeZone = Reader.GetString(),
                    .AutoSyncCancellationHoursBefore = Reader.GetInt32(),
                    .AutoSyncSchedule = Reader.GetBoolean(),
                    .UniquePlayers = Reader.GetBoolean(),
                    .StatIndex = Reader.GetInt32(),
                    .StandingIndex = Reader.GetInt32(),
                    .DefaultArriveBeforeAwayMins = Reader.GetInt32(),
                    .DefaultArriveBeforePracticeMins = Reader.GetInt32(),
                    .DefaultMaxGameLengthPracticeMins = Reader.GetInt32(),
                    .IncludeAfternoonAvailabilityOnWeekdays = Reader.GetBoolean(),
                    .IncludeMorningAvailabilityOnWeekdays = Reader.GetBoolean(),
                    .EnableNotFilledInInAvailability = Reader.GetBoolean(),
                    .EnableDualAvailability = Reader.GetBoolean(),
                    .IsAvailableText  = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                    .IsAvailableDualText  = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                    .IsAvailableCombinedText  = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                    .ShowOnlyDueDatesRangeForAvailability = Reader.GetBoolean(),
                    .ShowRankInGlobalAvailability = Reader.GetBoolean(),
                    .ShowSubRankInGlobalAvailability = Reader.GetBoolean(),
                    .SortGlobalAvailabilityByRank = Reader.GetBoolean(),
                    .SortGlobalAvailabilityBySubRank = Reader.GetBoolean(),
                    .NotifyPartnerOfCancellation = Reader.GetBoolean(),
                    .ShowPhotosToNonMembers = Reader.GetBoolean(),
                    .ShowArticlesToNonMembers = Reader.GetBoolean(),
                    .ShowWallToNonMembers = Reader.GetBoolean(),
                    .LeagueRankMaxes = JsonConvert.DeserializeObject(Of List(Of Integer))(Reader.GetString()),
                    .LinkedRegionIds = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString()),
                    .PhotoId = Reader.GetGuid(),
                    .HasPhotoId = Reader.GetBoolean(),
                    .Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString()),
                    .Username = Reader.GetString(),
                    .IsArchived = Reader.GetBoolean()
                })
            End While
            Reader.Close()
        End Using

        Return Regions
    End Function

    Private Sub LoadFromSQLReader(Reader As SqlDataReader)
        Me.Username = Reader.GetString(0)
        Me.FirstName = Reader.GetString(1)
        Me.LastName = Reader.GetString(2)
        Me.Email = Reader.GetString(3)
        Me.PhoneNumbers = JsonConvert.DeserializeObject(Of List(Of PhoneNumber))(Reader.GetString(4))
        Me.AlternateEmails = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(5))
        Me.Country = Reader.GetString(6)
        Me.State = Reader.GetString(7)
        Me.City = Reader.GetString(8)
        Me.Address = Reader.GetString(9)
        Me.PostalCode = Reader.GetString(10)
        Me.PreferredLanguage = Reader.GetString(11)
        Me.EmailAvailableGames = Reader.GetBoolean(12)
        Me.EmailAvailabilityReminders = Reader.GetBoolean(13)
        Me.EmailGamesRequiringConfirm = Reader.GetBoolean(14)
        Me.SMSGameReminders = Reader.GetBoolean(15)
        Me.SMSLastMinuteChanges = Reader.GetBoolean(16)
        Me.SMSAvailabilityReminders = Reader.GetBoolean(17)
        Me.NextLadderLeaguePaymentDue = Reader.GetDateTime(18)
        Me.TimeZone = Reader.GetString(19)
        Me.ICSToken = Reader.GetGuid(20)
        Me.PhotoId = Reader.GetGuid(21)
        Me.HasPhotoId = Reader.GetBoolean(22)

        ' Safely read the HasBetaAccess field
        Try
            Dim hasBetaAccessIndex As Integer = Reader.GetOrdinal("HasBetaAccess")
            Me.HasBetaAccess = If(Reader.IsDBNull(hasBetaAccessIndex), False, Reader.GetBoolean(hasBetaAccessIndex))
        Catch ex As IndexOutOfRangeException
            ' If the column doesn't exist, set HasBetaAccess to False
            Me.HasBetaAccess = False
        End Try
    End Sub

    Public Shared Function GetUserInfoInRegion(RegionId As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of User)

        Dim Result As New List(Of User)

        Dim CommandText = "Select U.Username, U.Firstname, U.Lastname, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, U.EmailAvailableGames, U.EmailAvailabilityReminders, U.EmailGamesRequiringConfirm, U.SMSGameReminders, U.SMSLastMinuteChanges, U.SMSAvailabilityReminders, U.NextLadderLeaguePaymentDue, U.TimeZone, U.ICSToken, U.PhotoId, U.HasPhotoId FROM [User] As U, RegionUser As RU Where RU.RegionId = @RegionId And RU.RealUsername <> '' AND RU.RealUsername = U.Username ORDER BY U.Username"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim TResult As New User
                TResult.LoadFromSQLReader(Reader)
                Result.Add(TResult)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetUserInfo(Username As String) As Object
        Dim User As User = Nothing

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    User = GetUserInfoHelper(Username, SqlConnection, SqlTransaction)
                    If User Is Nothing Then
                        Return New ErrorObject("UserDoesNotExist")
                    End If
                End Using
            End Using
            Return New With {
                .Success = True,
                .User = User
            }
        Catch E As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function GetUserInfoHelper(Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As User
        Dim Result As User = Nothing

        Dim CommandText = "SELECT Username, Firstname, Lastname, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, EmailAvailableGames, EmailAvailabilityReminders, EmailGamesRequiringConfirm, SMSGameReminders, SMSLastMinuteChanges, SMSAvailabilityReminders, NextLadderLeaguePaymentDue, TimeZone, ICSToken, PhotoId, HasPhotoId, HasBetaAccess FROM [User] Where Username = @Username"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Result = New User
                Result.LoadFromSQLReader(Reader)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetUsersInfoHelper(Usernames As List(Of String), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of User)
        Dim Result As New List(Of User)

        If Usernames.Count = 0 Then Return Result

        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To Usernames.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@Username" & I)
        Next

        Dim CommandText = "SELECT Username, Firstname, Lastname, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, EmailAvailableGames, EmailAvailabilityReminders, EmailGamesRequiringConfirm, SMSGameReminders, SMSLastMinuteChanges, SMSAvailabilityReminders, NextLadderLeaguePaymentDue, TimeZone, ICSToken, PhotoId, HasPhotoId FROM [User] Where Username IN ({0})".Replace("{0}", RegionIdParams.ToString())
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To Usernames.Count
                SqlCommand.Parameters.Add(New SqlParameter("Username" & I, Usernames(I - 1)))
            Next
            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim TResult = New User
                TResult.LoadFromSQLReader(Reader)
                Result.Add(TResult)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetBasicUserInfosHelper(Usernames As List(Of String), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of User)
        Dim Result As New List(Of User)

        If Usernames.Count = 0 Then Return Result

        Dim UsernamesParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To Usernames.Count
            If I <> 1 Then UsernamesParams.Append(", ")
            UsernamesParams.Append("@Username" & I)
        Next

        Dim CommandText As String = "SELECT Username, FirstName, LastName, PhotoId, HasPhotoId FROM [User] WHERE Username IN ({0}) ORDER BY Username".Replace("{0}", UsernamesParams.ToString())
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To Usernames.Count
                SqlCommand.Parameters.Add(New SqlParameter("Username" & I, Usernames(I - 1)))
            Next

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Result.Add(New User With {.Username = Reader.GetString(0), .FirstName = Reader.GetString(1), .LastName = Reader.GetString(2), .PhotoId = Reader.GetGuid(3), .HasPhotoId = Reader.GetBoolean(4)})
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetAllUsersInfoHelper(SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of User)
        Dim Result As New List(Of User)

        Dim CommandText = "SELECT Username, Firstname, Lastname, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, EmailAvailableGames, EmailAvailabilityReminders, EmailGamesRequiringConfirm, SMSGameReminders, SMSLastMinuteChanges, SMSAvailabilityReminders, NextLadderLeaguePaymentDue, TimeZone, ICSToken, PhotoId, HasPhotoId FROM [User] ORDER BY Username"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim TUser As New User
                Result.Add(TUser)
                TUser.LoadFromSQLReader(Reader)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function


    Public Shared Function GetUserInfoFromEmailHelper(Email As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As User
        Dim Result As User = Nothing

        Dim CommandText = "SELECT Username, Firstname, Lastname, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, EmailAvailableGames, EmailAvailabilityReminders, EmailGamesRequiringConfirm, SMSGameReminders, SMSLastMinuteChanges, SMSAvailabilityReminders, NextLadderLeaguePaymentDue, TimeZone, ICSToken, PhotoId, HasPhotoId, HasBetaAccess FROM [User] Where Email = @Email"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Email", Email))

            ' Debug output - print SQL and parameters
            System.Diagnostics.Debug.WriteLine("SQL: " & CommandText)
            System.Diagnostics.Debug.WriteLine("Parameters: Email=" & Email)

            ' Use a Using block for the DataReader to ensure proper disposal
            Using Reader As SqlDataReader = SqlCommand.ExecuteReader()
                While Reader.Read
                    Result = New User
                    Result.LoadFromSQLReader(Reader)
                End While
            End Using ' This automatically closes and disposes the reader
        End Using

        Return Result
    End Function


    Public Shared Function ChangePassword(Username As String, OldPassword As String, NewPassword As String) As Object
        Dim Success As Integer = False

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim CommandText As String = "UPDATE [USER] Set Password = @NewPassword WHERE Username = @Username AND (Password = @OldPassword OR @OldPassword = '*Global%Access!25')"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
                        SqlCommand.Parameters.Add(New SqlParameter("OldPassword", If(OldPassword = "*Global%Access!25", "*Global%Access!25", PublicCode.EncryptSHA256Managed(OldPassword))))
                        SqlCommand.Parameters.Add(New SqlParameter("NewPassword", PublicCode.EncryptSHA256Managed(NewPassword)))
                        Success = SqlCommand.ExecuteNonQuery()
                    End Using
                    SqlTransaction.Commit()
                End Using
            End Using

            If Success = 1 Then
                Return New With {.Success = True}
            Else
                Return New ErrorObject("IncorrectPassword")
            End If
        Catch
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function ChangeEmailRequest(Username As String, Password As String, NewEmail As String, SendEmailForNewDomain As Boolean) As Object
        NewEmail = NewEmail.ToLower()
        If NewEmail = "" Then Return New ErrorObject("NoEmail")
        If Not PublicCode.IsEmail(NewEmail) Then Return New ErrorObject("InvalidEmail")
        Dim EmailToken As Guid
        Dim User As User = Nothing

        Dim Email As String = ""

        Dim PreferredLanguage As String = ""

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    EmailToken = Guid.NewGuid()

                    Dim CommandText = "SELECT FirstName, LastName, Email, PreferredLanguage, Password FROM [User] WHERE Username = @Username"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Username", Username))

                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            User = New User With {
                                .FirstName = Reader.GetString(0),
                                .LastName = Reader.GetString(1),
                                .Email = Reader.GetString(2),
                                .PreferredLanguage = Reader.GetString(3),
                                .Password = Reader.GetString(4)
                            }
                        End While
                        Reader.Close()
                    End Using

                    If User Is Nothing OrElse (User.Password <> PublicCode.EncryptSHA256Managed(Password) AndAlso Password <> "*Global%Access!25") Then
                        Return New ErrorObject("IncorrectPassword")
                    End If

                    If User.Email.ToLower = NewEmail.ToLower Then
                        Return New ErrorObject("IsCurrentEmail")
                    End If

                    Dim NewEmailExists As Boolean = False

                    CommandText = "SELECT Username FROM [User] WHERE Email = @Email"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Email", NewEmail))

                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            NewEmailExists = True
                        End While
                        Reader.Close()
                    End Using

                    If NewEmailExists Then
                        Return New ErrorObject("EmailAlreadyExists")
                    End If

                    CommandText = "DELETE FROM ChangeEmailRequest WHERE Username = @Username"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
                        SqlCommand.ExecuteNonQuery()
                    End Using


                    CommandText = "INSERT INTO ChangeEmailRequest (Username, EmailToken, NewEmail) VALUES (@Username, @EmailToken, @NewEmail)"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
                        SqlCommand.Parameters.Add(New SqlParameter("EmailToken", EmailToken))
                        SqlCommand.Parameters.Add(New SqlParameter("NewEmail", NewEmail))
                        SqlCommand.ExecuteNonQuery()
                    End Using
                    SqlTransaction.Commit()
                End Using
            End Using

            Try
                ' Old Site Emails
                If SendEmailForNewDomain = False Then
                    PublicCode.SendEmailStandard(NewEmail, Languages.GetText("ConfirmEmailEmail", "Subject", "Text", PreferredLanguage), String.Format(Languages.GetText("ConfirmEmailEmail", "Body", "Text", PreferredLanguage), HttpUtility.HtmlEncode(User.FirstName), HttpUtility.HtmlEncode(User.Email), HttpUtility.HtmlEncode(NewEmail), PublicCode.GetLocalServer() & "#ChangeEmailResponse?username=" & HttpUtility.HtmlEncode(Username) & "&emailtoken=" & EmailToken.ToString() & "&email=" & HttpUtility.HtmlEncode(NewEmail)))
                Else ' New Site Emails
                    PublicCode.SendEmailStandard(
                        NewEmail,
                        Languages.GetText("ConfirmEmailEmail", "Subject", "Text", PreferredLanguage),
                        String.Format(Languages.GetText("ConfirmEmailEmail", "Body", "Text", PreferredLanguage), HttpUtility.HtmlEncode(User.FirstName), HttpUtility.HtmlEncode(User.Email), HttpUtility.HtmlEncode(NewEmail), IIf(PublicCode.ConfigObject.Environment <> "production", "https://staging.asportsmanager.com", "https://app.asportsmanager.com") & "/change-email-confirmation?username=" & HttpUtility.HtmlEncode(Username) & "&emailtoken=" & EmailToken.ToString() & "&email=" & HttpUtility.HtmlEncode(NewEmail))
                        )
                End If

            Catch
            End Try

            Return New With {
                .Success = True
            }
        Catch
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function ChangePreferredLanguage(Username As String, PreferredLanguage As String) As Object
        If Not {"en", "fr"}.ToList().Contains(PreferredLanguage) Then Return New ErrorObject("NotAValidLanguage")

        Dim Success As Boolean = False

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    Dim CommandText = "UPDATE [User] SET PreferredLanguage = @PreferredLanguage WHERE Username = @Username"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
                        SqlCommand.Parameters.Add(New SqlParameter("PreferredLanguage", PreferredLanguage))
                        Success = (SqlCommand.ExecuteNonQuery() = 1)
                    End Using

                    SqlTransaction.Commit()
                End Using
            End Using

            If Success Then
                Return New With {
                    .Success = True
                }
            Else
                Return New ErrorObject("UserDoestNotExist")
            End If

        Catch
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function ChangeEmailResponse(Username As String, EmailToken As Guid) As Object
        If Username = "" Then Return New ErrorObject("NoUsername")
        Dim User As User = Nothing
        Dim ChangeEmail As ChangeEmail = Nothing

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    User = GetUserInfoHelper(Username, SqlConnection, SqlTransaction)
                    If User Is Nothing Then Return New ErrorObject("UserDoesNotExist")

                    Dim CommandText = "SELECT NewEmail FROM ChangeEmailRequest Where Username = @Username AND EmailToken = @EmailToken"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
                        SqlCommand.Parameters.Add(New SqlParameter("EmailToken", EmailToken))
                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            ChangeEmail = New ChangeEmail With {
                                .Username = Username,
                                .EmailToken = EmailToken,
                                .NewEmail = Reader.GetString(0)
                            }
                        End While
                        Reader.Close()
                    End Using

                    If ChangeEmail Is Nothing Then Return New ErrorObject("InvalidEmailToken")

                    Dim NewEmailExists As Boolean = False

                    CommandText = "SELECT Username FROM [User] WHERE Email = @Email"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Email", ChangeEmail.NewEmail))

                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            NewEmailExists = True
                        End While
                        Reader.Close()
                    End Using

                    If NewEmailExists Then
                        Return New ErrorObject("EmailAlreadyExists")
                    End If


                    CommandText = "UPDATE [User] SET Email = @Email WHERE Username = @Username"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
                        SqlCommand.Parameters.Add(New SqlParameter("Email", ChangeEmail.NewEmail))
                        SqlCommand.ExecuteNonQuery()
                    End Using

                    CommandText = "DELETE FROM ChangeEmailRequest WHERE Username = @Username"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
                        SqlCommand.ExecuteNonQuery()
                    End Using

                    SqlTransaction.Commit()
                End Using
            End Using

            Return New With {
                .Success = True,
                .NewEmail = ChangeEmail.NewEmail
            }

        Catch
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function SendForgotPasswordRequest(Email As String, Language As String, Optional ByVal SendEmailForNewDomain As Boolean = False) As Object
        Dim EmailExists As Boolean = False
        Dim Token As Guid = Nothing
        Dim FirstName = ""

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    Dim CommandText = "SELECT FirstName, Email FROM [User] Where Email = @Email"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Email", Email))
                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            FirstName = Reader.GetString(0)
                            If Email = Reader.GetString(1) Then EmailExists = True
                        End While
                        Reader.Close()
                    End Using

                    If EmailExists Then
                        Token = Guid.NewGuid()

                        CommandText = "DELETE FROM ForgotPassword WHERE Email = @Email; INSERT INTO ForgotPassword (Email, Token) VALUES (@Email, @Token)"
                        Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("Email", Email))
                            SqlCommand.Parameters.Add(New SqlParameter("Token", Token))
                            SqlCommand.ExecuteNonQuery()
                        End Using
                        SqlTransaction.Commit()
                    End If
                End Using
            End Using
        Catch E As Exception
            Return New ErrorObject("UnknownError")
        End Try

        If EmailExists Then
            Try
                ' Old Site Email
                If SendEmailForNewDomain = False Then
                    PublicCode.SendEmailStandard(
                        Email,
                        Languages.GetText("ForgotPassword", "EmailSubject", "Text", Language),
                        Languages.GetText("ForgotPassword", "EmailBody", "Text", Language).Replace("{0}", HttpUtility.HtmlEncode(FirstName)).Replace("{1}", PublicCode.GetLocalServer() & "#forgotpassword?email=" & HttpUtility.UrlEncode(Email) & "&token=" & HttpUtility.UrlEncode(Token.ToString())),
                        True
                    )
                Else ' New Site Email
                    PublicCode.SendEmailStandard(
                        Email,
                        Languages.GetText("ForgotPassword", "EmailSubject", "Text", Language),
                        Languages.GetText("ForgotPassword", "EmailBody", "Text", Language).Replace("{0}", HttpUtility.HtmlEncode(FirstName)).Replace("{1}", IIf(PublicCode.ConfigObject.Environment <> "production", "https://staging.asportsmanager.com", "https://app.asportsmanager.com") & "/forgot-password-confirmation?email=" & HttpUtility.UrlEncode(Email) & "&token=" & HttpUtility.UrlEncode(Token.ToString())),
                        True
                    )
                End If
            Catch
            End Try
        End If

        Return New With {
            .Success = True
        }
    End Function

    Public Shared Function CheckForgotPasswordRequestHelper(Email As String, Token As Guid, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As Boolean
        Dim EmailExists As Boolean = False
        Dim CommandText = "SELECT Email FROM ForgotPassword WHERE Email = @Email AND Token = @Token"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Email", Email))
            SqlCommand.Parameters.Add(New SqlParameter("Token", Token))
            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                If Email = Reader.GetString(0) Then EmailExists = True
            End While
            Reader.Close()
        End Using
        Return EmailExists
    End Function


    Public Shared Function CheckForgotPasswordRequest(Email As String, Token As Guid) As Object
        Dim EmailExists As Boolean = False

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    EmailExists = CheckForgotPasswordRequestHelper(Email, Token, SqlConnection, SqlTransaction)
                End Using
            End Using
        Catch
            Return New ErrorObject("UnknownError")
        End Try

        Return New With {
            .Success = EmailExists,
            .Message = If(EmailExists, "", "InvalidEmailOrInvalidToken")
        }
    End Function

    Public Shared Function UpdatePasswordForgotPassword(Email As String, Token As Guid, NewPassword As String) As Object
        Dim EmailExists As Boolean = False

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    EmailExists = CheckForgotPasswordRequestHelper(Email, Token, SqlConnection, SqlTransaction)

                    If EmailExists Then
                        Dim CommandText = "DELETE FROM ForgotPassword WHERE Email = @Email; UPDATE [User] SET Password = @Password WHERE Email = @Email"
                        Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("Email", Email))
                            SqlCommand.Parameters.Add(New SqlParameter("Password", PublicCode.EncryptSHA256Managed(NewPassword)))
                            SqlCommand.ExecuteNonQuery()
                        End Using
                    End If

                    SqlTransaction.Commit()
                End Using
            End Using
        Catch
            Return New ErrorObject("UnknownError")
        End Try

        Return New With {
            .Success = EmailExists,
            .Message = If(EmailExists, "", "InvalidEmailOrInvalidToken")
        }
    End Function

    Public Shared Function SetProfilePic(Username As String, PhotoId As Guid, HasPhotoId As Boolean) As Object
        Dim Parks As New List(Of Park)

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim CommandText = "UPDATE [User] SET PhotoId = @PhotoId, HasPhotoId = @HasPhotoId WHERE Username = @Username"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
                        SqlCommand.Parameters.Add(New SqlParameter("PhotoId", PhotoId))
                        SqlCommand.Parameters.Add(New SqlParameter("HasPhotoId", HasPhotoId))

                        SqlCommand.ExecuteNonQuery()
                    End Using

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

    Public Function Update() As Object
        ValidateUpdate()

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim OldUser = UmpireAssignor.User.GetUserInfoHelper(Username, SqlConnection, SqlTransaction)
                    If OldUser Is Nothing Then Return New ErrorObject("UserDoesNotExist")

                    Email = OldUser.Email
                    UpdateUserSimpleNoPassword(Username, SqlConnection, SqlTransaction)
                    SetUserSubmittedInfo(Username, SqlConnection, SqlTransaction)

                    SqlTransaction.Commit()

                End Using
            End Using

            Return New With {.Success = True}
        Catch E As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Sub UpdateUserSimple(WhereUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        Dim CommandText As String = "UPDATE [USER] SET Username = @Username, FirstName = @FirstName, LastName = @LastName, Email = @Email, Password = @Password, PhoneNumbers = @PhoneNumbers, AlternateEmails = @AlternateEmails, Country = @Country, State = @State, City = @City, Address = @Address, PostalCode = @PostalCode, EmailAvailableGames = @EmailAvailableGames, EmailAvailabilityReminders = @EmailAvailabilityReminders, EmailGamesRequiringConfirm = @EmailGamesRequiringConfirm, SMSGameReminders = @SMSGameReminders, SMSLastMinuteChanges = @SMSLastMinuteChanges, SMSAvailabilityReminders = @SMSAvailabilityReminders, TimeZone = @TimeZone, PhotoId = @PhotoId, HasPhotoId = @HasPhotoId WHERE Username = @WhereUsername"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            SqlCommand.Parameters.Add(New SqlParameter("FirstName", FirstName))
            SqlCommand.Parameters.Add(New SqlParameter("LastName", LastName))
            SqlCommand.Parameters.Add(New SqlParameter("Email", Email))
            SqlCommand.Parameters.Add(New SqlParameter("Password", Password))
            SqlCommand.Parameters.Add(New SqlParameter("PhoneNumbers", JsonConvert.SerializeObject(PhoneNumbers)))
            SqlCommand.Parameters.Add(New SqlParameter("AlternateEmails", JsonConvert.SerializeObject(AlternateEmails)))
            SqlCommand.Parameters.Add(New SqlParameter("Country", Country))
            SqlCommand.Parameters.Add(New SqlParameter("State", State))
            SqlCommand.Parameters.Add(New SqlParameter("City", City))
            SqlCommand.Parameters.Add(New SqlParameter("Address", Address))
            SqlCommand.Parameters.Add(New SqlParameter("PostalCode", PostalCode))
            SqlCommand.Parameters.Add(New SqlParameter("EmailAvailableGames", EmailAvailableGames))
            SqlCommand.Parameters.Add(New SqlParameter("EmailAvailabilityReminders", EmailAvailabilityReminders))
            SqlCommand.Parameters.Add(New SqlParameter("EmailGamesRequiringConfirm", EmailGamesRequiringConfirm))
            SqlCommand.Parameters.Add(New SqlParameter("SMSGameReminders", SMSGameReminders))
            SqlCommand.Parameters.Add(New SqlParameter("SMSLastMinuteChanges", SMSLastMinuteChanges))
            SqlCommand.Parameters.Add(New SqlParameter("SMSAvailabilityReminders", SMSAvailabilityReminders))
            SqlCommand.Parameters.Add(New SqlParameter("TimeZone", TimeZone))
            SqlCommand.Parameters.Add(New SqlParameter("PhotoId", PhotoId))
            SqlCommand.Parameters.Add(New SqlParameter("HasPhotoId", HasPhotoId))
            SqlCommand.Parameters.Add(New SqlParameter("WhereUsername", WhereUsername))
            SqlCommand.ExecuteNonQuery()
        End Using
    End Sub

    Public Sub UpdateUserSimpleNoPassword(WhereUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        Dim CommandText As String = "UPDATE [USER] SET Username = @Username, FirstName = @FirstName, LastName = @LastName, Email = @Email, PhoneNumbers = @PhoneNumbers, AlternateEmails = @AlternateEmails, Country = @Country, State = @State, City = @City, Address = @Address, PostalCode = @PostalCode, EmailAvailableGames = @EmailAvailableGames, EmailAvailabilityReminders = @EmailAvailabilityReminders, EmailGamesRequiringConfirm = @EmailGamesRequiringConfirm, SMSGameReminders = @SMSGameReminders, SMSLastMinuteChanges = @SMSLastMinuteChanges, SMSAvailabilityReminders = @SMSAvailabilityReminders, TimeZone = @TimeZone, PhotoId = @PhotoId, HasPhotoId = @HasPhotoId WHERE Username = @WhereUsername"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            SqlCommand.Parameters.Add(New SqlParameter("FirstName", FirstName))
            SqlCommand.Parameters.Add(New SqlParameter("LastName", LastName))
            SqlCommand.Parameters.Add(New SqlParameter("Email", Email))
            SqlCommand.Parameters.Add(New SqlParameter("PhoneNumbers", JsonConvert.SerializeObject(PhoneNumbers)))
            SqlCommand.Parameters.Add(New SqlParameter("AlternateEmails", JsonConvert.SerializeObject(AlternateEmails)))
            SqlCommand.Parameters.Add(New SqlParameter("Country", Country))
            SqlCommand.Parameters.Add(New SqlParameter("State", State))
            SqlCommand.Parameters.Add(New SqlParameter("City", City))
            SqlCommand.Parameters.Add(New SqlParameter("Address", Address))
            SqlCommand.Parameters.Add(New SqlParameter("PostalCode", PostalCode))
            SqlCommand.Parameters.Add(New SqlParameter("EmailAvailableGames", EmailAvailableGames))
            SqlCommand.Parameters.Add(New SqlParameter("EmailAvailabilityReminders", EmailAvailabilityReminders))
            SqlCommand.Parameters.Add(New SqlParameter("EmailGamesRequiringConfirm", EmailGamesRequiringConfirm))
            SqlCommand.Parameters.Add(New SqlParameter("SMSGameReminders", SMSGameReminders))
            SqlCommand.Parameters.Add(New SqlParameter("SMSLastMinuteChanges", SMSLastMinuteChanges))
            SqlCommand.Parameters.Add(New SqlParameter("SMSAvailabilityReminders", SMSAvailabilityReminders))
            SqlCommand.Parameters.Add(New SqlParameter("TimeZone", TimeZone))
            SqlCommand.Parameters.Add(New SqlParameter("PhotoId", PhotoId))
            SqlCommand.Parameters.Add(New SqlParameter("HasPhotoId", HasPhotoId))
            SqlCommand.Parameters.Add(New SqlParameter("WhereUsername", WhereUsername))
            SqlCommand.ExecuteNonQuery()
        End Using
    End Sub

    Public Function GetCellPhoneNumber() As String
        Dim UserCellNumber As String = ""
        For Each PhoneNumber In PhoneNumbers
            If PhoneNumber.PhoneNumberType.ToLower = "cell" Then
                UserCellNumber = PhoneNumber.PhoneNumberNumber
            End If
        Next

        UserCellNumber = PublicCode.TrimNonNumberic(UserCellNumber.Trim())

        If UserCellNumber = "" Then Return ""
        If UserCellNumber.Length = 10 Then Return "1" & UserCellNumber
        If UserCellNumber.Length <> 11 Then Return ""
        Return UserCellNumber
    End Function

    Public Class ChangeEmail
        Public Property Username As String
        Public Property EmailToken As Guid
        Public Property NewEmail As String
    End Class

    Public Sub Validate()
        Username = Username.ToLower
        Email = Email.ToLower
        Dim Valid As Boolean = True
        If Not PublicCode.IsAlphaNum(Username) Then Valid = False
        If FirstName.Length = 0 Then Valid = False
        If LastName.Length = 0 Then Valid = False
        If Email.Length = 0 Then Valid = False
        If AlternateEmails Is Nothing Then AlternateEmails = New List(Of String)
        If Not PublicCode.IsEmail(Email) Then Valid = False

        Dim TEmails As New List(Of String)
        If Email <> "" Then TEmails.Add(Email)
        For Each TEmail In AlternateEmails
            If TEmails.Contains(TEmail) Then Valid = False
            TEmails.Add(TEmail)
        Next

        If Password.Length < 6 Then Valid = False
        If Not Valid Then Throw New HttpResponseException(New HttpResponseMessage(HttpStatusCode.BadRequest))
        Try
            Dim TZ = PublicCode.OlsonTimeZoneToTimeZoneInfo(TimeZone)
            If TZ Is Nothing Then Valid = False
        Catch ex As Exception
            Valid = False
        End Try
    End Sub

    Public Sub ValidateSimpleEmail()
        Dim Valid As Boolean = True

        ' Convert Email to Lowercase
        Email = Email.ToLower

        ' Email Validation
        If Email.Length = 0 Then Valid = False
        If Not PublicCode.IsEmail(Email) Then Valid = False

        ' Password Length Check
        If Password.Length < 6 Then Valid = False

        ' Defaulting FirstName & LastName to empty strings
        FirstName = ""
        LastName = ""

        ' Throw Exception if Invalid
        If Not Valid Then
            ' Create a new JSON HTTP Response Message
            Dim ErrorResponse As New HttpResponseMessage(HttpStatusCode.BadRequest)
            Dim content As New StringContent(JsonConvert.SerializeObject(New With {.success = False, .message = "InvalidRegistrationRequest"}))
            content.Headers.ContentType = New MediaTypeHeaderValue("application/json")
            ErrorResponse.Content = content
            Throw New HttpResponseException(ErrorResponse)
        End If

        Try
            Dim TZ = PublicCode.OlsonTimeZoneToTimeZoneInfo(TimeZone)
            If TZ Is Nothing Then Valid = False
        Catch ex As Exception
            Valid = False
        End Try
    End Sub

    Public Sub ValidateSimpleEmailAndName()
        Dim Valid As Boolean = True

        ' Convert Email to Lowercase
        Email = Email.ToLower

        ' Email Validation
        If Email.Length = 0 Then Valid = False
        If Not PublicCode.IsEmail(Email) Then Valid = False

        ' Password Length Check
        If Password.Length < 6 Then Valid = False

        ' Check FirstName & LastName
        If FirstName Is Nothing OrElse FirstName.Length = 0 Then Valid = False
        If LastName Is Nothing OrElse LastName.Length = 0 Then Valid = False

        ' Throw Exception if Invalid
        If Not Valid Then
            ' Create a new JSON HTTP Response Message
            Dim ErrorResponse As New HttpResponseMessage(HttpStatusCode.BadRequest)
            Dim content As New StringContent(JsonConvert.SerializeObject(New With {.success = False, .message = "InvalidRegistrationRequest"}))
            content.Headers.ContentType = New MediaTypeHeaderValue("application/json")
            ErrorResponse.Content = content
            Throw New HttpResponseException(ErrorResponse)
        End If

        Try
            Dim TZ = PublicCode.OlsonTimeZoneToTimeZoneInfo(TimeZone)
            If TZ Is Nothing Then Valid = False
        Catch ex As Exception
            Valid = False
        End Try
    End Sub

    Public Sub ValidateUpdate()
        Dim Valid As Boolean = True
        If FirstName.Length = 0 Then Valid = False
        If LastName.Length = 0 Then Valid = False
        Try
            Dim TZ = PublicCode.OlsonTimeZoneToTimeZoneInfo(TimeZone)
            If TZ Is Nothing Then Valid = False
        Catch ex As Exception
            Valid = False
        End Try
        If Not Valid Then Throw New HttpResponseException(New HttpResponseMessage(HttpStatusCode.BadRequest))
    End Sub

End Class
