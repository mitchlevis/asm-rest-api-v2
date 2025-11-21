Imports System.Data.SqlClient
Imports Newtonsoft.Json
Imports Microsoft.AspNet.SignalR

Public Class RegionUserFetcher
    Public Property RegionUsers As New List(Of RegionUser)
    Public Property RegionIdHelper As New RegionIdHelper

    Public Function LoadAllInRegionIdsHelper(RegionIds As List(Of String), Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionUser)
        Dim ToFetchRegionIds = RegionIdHelper.DoFetch(RegionIds)

        If ToFetchRegionIds.Count > 0 Then
            RegionUsers.AddRange(UmpireAssignor.RegionUser.LoadAllInRegionIdsHelper(ToFetchRegionIds, Username, SQLConnection, SQLTransaction))
            RegionUsers.Sort(RegionUser.BasicSorter)
        End If

        Return RegionUsers
    End Function
End Class

Public Class RegionIdAndUsername
    Public Property RegionId As String
    Public Property Username As String

    Public Sub New()
    End Sub

    Public Sub New(RegionId As String, Username As String)
        Me.RegionId = RegionId
        Me.Username = Username
    End Sub
End Class

Public Class RegionAndRegionUser
    Public Property Region As Region
    Public Property RegionUser As RegionUser
End Class

Public Class RankAndDate
    Public Property RankDate As Date
    Public Property Rank As Decimal

    Public Shared Sorter As GenericIComparer(Of RankAndDate) = New GenericIComparer(Of RankAndDate)(Function(V1, V2)
                                                                            Dim Comp As Integer = V1.RankDate.CompareTo(V2.RankDate)
                                                                            If Comp = 0 Then Comp = V1.Rank.CompareTo(V2.Rank)
                                                                            Return Comp
                                                                        End Function)

    Public Shared ReadOnly FIRST_RANK_DATE = New Date(1800, 1, 1)

    Public Sub New()
    End Sub

    Public Sub New(RankDate As Date, Rank As Decimal)
        Me.RankDate = RankDate
        Me.Rank = Rank
    End Sub

    Public Function Clone()
        Dim Result As New RankAndDate
        Result.RankDate = RankDate
        Result.Rank = Rank
        Return Result
    End Function
End Class

Public Class RegionUserWithRegion
    Public Property RegionId As String
    Public Property Username As String
    Public Property IsLinked As Boolean
    Public Property AllowInfoLink As Boolean
    Public Property IsActive
    Public Property RealUsername As String
    Public Property FirstName As String
    Public Property LastName As String
    Public Property Email As String
    Public Property Positions As List(Of String)
    Public Property PhoneNumbers As List(Of PhoneNumber)
    Public Property AlternateEmails As List(Of String)
    Public Property Country As String
    Public Property State As String
    Public Property City As String
    Public Property Address As String
    Public Property PostalCode As String
    Public Property PreferredLanguage As String
    Public Property Rank As Dictionary(Of String, String)
    Public Property RankNumber As Dictionary(Of String, Decimal)
    Public Property IsArchived As Boolean
    Public Property CanViewAvailability As Boolean
    Public Property CanViewMasterSchedule As Boolean
    Public Property CanViewSupervisors As Boolean
    Public Property PublicData As String
    Public Property PrivateData As String
    Public Property GlobalAvailabilityData As String
    Public Property RankAndDates As List(Of RankAndDate)
    Public Property InternalData As String
    Public Property StatLinks As String
    Public Property PhotoId As Guid
    Public Property HasPhotoId As Boolean

    Private Sub LoadFromSQLReader(Reader As SqlDataReader)
        Me.RegionId = Reader.GetString(0)
        Me.Username = Reader.GetString(1)
        Me.IsLinked = Reader.GetBoolean(2)
        Me.AllowInfoLink = Reader.GetBoolean(3)
        Me.RealUsername = Reader.GetString(4)
        Me.FirstName = Reader.GetString(5)
        Me.LastName = Reader.GetString(6)
        Me.Email = Reader.GetString(7)
        Me.PhoneNumbers = JsonConvert.DeserializeObject(Of List(Of PhoneNumber))(Reader.GetString(8))
        Me.AlternateEmails = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(9).ToLower)
        Me.Country = Reader.GetString(10)
        Me.State = Reader.GetString(11)
        Me.City = Reader.GetString(12)
        Me.Address = Reader.GetString(13)
        Me.PostalCode = Reader.GetString(14)
        Me.PreferredLanguage = Reader.GetString(15)
        Me.Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(16).ToLower)
        Me.Rank = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString(17).ToLower)
        Me.IsActive = Reader.GetBoolean(18)
        Me.CanViewAvailability = Reader.GetBoolean(19)
        Me.CanViewMasterSchedule = Reader.GetBoolean(20)
        Me.CanViewSupervisors = Reader.GetBoolean(21)
        Me.PublicData = Reader.GetString(22)
        Me.PrivateData = Reader.GetString(23)
        Me.GlobalAvailabilityData = Reader.GetString(24)
        Me.RankAndDates = JsonConvert.DeserializeObject(Of List(Of RankAndDate))(Reader.GetString(25))
        Me.InternalData = Reader.GetString(26)
        Me.StatLinks = Reader.GetString(27)
        Me.PhotoId = Reader.GetGuid(28)
        Me.HasPhotoId = Reader.GetBoolean(29)
    End Sub

    Public Sub New()
    End Sub

    Public Sub New(Reader As SqlDataReader)
        LoadFromSQLReader(Reader)
    End Sub

End Class

Public Class RegionUser
    Implements IComparable(Of RegionUser)

    Public Shared Function RankNumberToRank(RankNumber As Decimal) As String
        If RankNumber < 2.75 Then
            Return "rookie"
        ElseIf RankNumber < 3.25 Then
            Return "novice"
        ElseIf RankNumber < 3.75 Then
            Return "intermediate"
        ElseIf RankNumber < 4.25 Then
            Return "junior"
        End If
        Return "senior"
    End Function

    Public Shared RankOrder as New Dictionary(Of String, Integer) From {{ "senior", 5 }, { "junior", 4 }, { "intermediate", 3 }, { "novice", 2 }, { "rookie", 1 }, { "", 0 }}

    Public Shared RankCode As List(Of String) = {
        "",
        "senior",
        "junior",
        "intermediate",
        "novice",
        "rookie"
    }.ToList()

    Public Property CurrentRank As Decimal
        Get
            If RankNumber Is Nothing Then Return 0
            If Not RankNumber.ContainsKey("official") Then Return 0
            Return RankNumber("official")
        End Get
        Set(value As Decimal)
            If RankNumber Is Nothing Then RankNumber = New Dictionary(Of String, Decimal)
            If Rank Is Nothing Then Rank = New Dictionary(Of String, String)
            If Not RankNumber.ContainsKey("official") Then RankNumber.Add("official", 0)
            If Not Rank.ContainsKey("official") Then Rank.Add("official", "rookie")
            RankNumber("official") = value
            Rank("official") = RegionUser.RankNumberToRank(value)
        End Set
    End Property


    Public Property RegionId As String
    Public Property Username As String
    Public Property IsLinked As Boolean
    Public Property AllowInfoLink As Boolean
    Public Property RealUsername As String
    Public Property FirstName As String
    Public Property LastName As String
    Public Property Email As String
    Public Property Positions As List(Of String)
    Public Property PhoneNumbers As List(Of PhoneNumber)
    Public Property AlternateEmails As List(Of String)
    Public Property Country As String
    Public Property State As String
    Public Property City As String
    Public Property Address As String
    Public Property PostalCode As String
    Public Property PreferredLanguage As String = "en"
    Public Property Rank As Dictionary(Of String, String)
    Public Property IsArchived As Boolean = False
    Public Property IsActive As Boolean = True
    Public Property CanViewAvailability As Boolean = True
    Public Property CanViewMasterSchedule As Boolean = False
    Public Property CanViewSupervisors As Boolean = False
    Public Property PublicData As String = ""
    Public Property PrivateData As String = ""
    Public Property GlobalAvailabilityData As String = ""
    Public Property RankAndDates As List(Of RankAndDate)
    Public Property InternalData As String = ""
    Public Property PhotoId As Guid = Guid.Empty
    Public Property HasPhotoId As Boolean = False
    Public Property StatLinks As String = ""
    Public Property RankNumber As Dictionary(Of String, Decimal)

    Public Shared Function GetItem(RegionUsers As List(Of RegionUser), RegionId As String, Username As String) As RegionUser
        Return PublicCode.BinarySearchItem(of RegionUser)(RegionUsers, New RegionUser With {.RegionId = RegionId, .Username = Username}, RegionUser.BasicSorter)
    End Function

    Public Shared Function GetItem(RegionUsers As List(Of RegionUser), RegionId As String) As RegionUser
        Return RegionUsers.Find(Function(RU) RU.RegionId = RegionId)
    End Function

    Public Shared Function ConvertToDic(RegionIds As List(Of String), RegionUsers As List(Of RegionUser)) As Dictionary(Of String, List(Of RegionUser))
        Dim Result As New Dictionary(Of String, List(Of RegionUser))

        For Each TRegionId In RegionIds
            If Not Result.ContainsKey(TRegionId) Then Result.Add(TRegionId, New List(Of RegionUser))
        Next

        For Each RegionUser In RegionUsers
            If Not Result.ContainsKey(RegionUser.RegionId) Then Result.Add(RegionUser.RegionId, New List(Of RegionUser))
            Result(RegionUser.RegionId).Add(RegionUser)
        Next

        Return Result
    End Function


    Public Shared Function BasicComparer(V1 As RegionUser, V2 As RegionUser)
        Dim Comp As Integer = V1.RegionId.CompareTo(V2.RegionId)
        If Comp = 0 Then Comp = V1.Username.CompareTo(V2.Username)
        Return Comp
    End Function

    Public Shared BasicSorter = New GenericIComparer(Of RegionUser)(AddressOf BasicComparer)

    Public Shared FullNameSorter = New GenericIComparer(Of RegionUser)(Function(V1, V2)
                                                                           Dim Comp As Integer = V1.RealUsername.CompareTo(V2.RealUsername)
                                                                           If Comp = 0 Then Comp = V1.LastName.Trim.ToLower.CompareTo(V2.LastName.Trim.ToLower)
                                                                           If Comp = 0 Then Comp = V1.FirstName.Trim.ToLower.CompareTo(V2.FirstName.Trim.ToLower)
                                                                           Return Comp
                                                                       End Function)

    Public Shared Function FilterByRegionId(RegionUsers As List(Of RegionUser), RegionId As String) As List(Of RegionUser)
        Return RegionUsers.FindAll(Function (RU) RU.RegionId = RegionId)
    End Function

    Public Function GetRankTuple(CrewType As String) As Tuple(Of Integer, Integer)
        If Rank Is Nothing Then Return New Tuple(Of Integer, Integer)(-1, -1)
        If Not Rank.ContainsKey(CrewType) Then Return New Tuple(Of Integer, Integer)(-1, -1)

        Dim TRankNumber As Integer = 0
        If RankNumber IsNot Nothing AndAlso RankNumber.ContainsKey(CrewType) Then
            TRankNumber = RankNumber(CrewType)
        End If

        Dim RankInteger = RankCode.IndexOf(Rank(CrewType).ToLower)
        If RankInteger < 1 Then Return New Tuple(Of Integer, Integer)(-1, -1)

        Return New Tuple(Of Integer, Integer)(RankInteger - 1, TRankNumber)
    End Function

    Public Shared Sub BulkDelete(RegionUsers As List(Of RegionUser), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        If RegionUsers.Count = 0 Then Return

        For N As Integer = 0 To RegionUsers.Count - 1 Step 1000
            Dim ParamsSB As New StringBuilder()
            For I As Integer = N To Math.Min(RegionUsers.Count, N + 1000) - 1
                If I <> N Then ParamsSB.Append(" OR ")
                ParamsSB.Append("(RegionId = @RegionId" & I)
                ParamsSB.Append(" AND ")
                ParamsSB.Append("Username = @Username" & I & ")")
            Next

            Dim CommandText As String = "DELETE FROM [RegionUser] WHERE {0}".Replace("{0}", ParamsSB.ToString())
            Using SqlCommand = New SqlCommand(CommandText, SQLConnection, SQLTransaction)
                For I As Integer = N To Math.Min(RegionUsers.Count, N + 1000) - 1
                    SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionUsers(I).RegionId))
                    SqlCommand.Parameters.Add(New SqlParameter("Username" & I, RegionUsers(I).Username))
                Next
                SqlCommand.ExecuteNonQuery()
            End Using
        Next N
    End Sub

    Public Shared Sub BulkInsert(RegionUsers As List(Of RegionUser), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        If RegionUsers.Count = 0 Then Return

        Dim RegionUserTable As New DataTable("RegionUser")
        RegionUserTable.Columns.Add(New DataColumn("RegionId", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("Username", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("IsLinked", GetType(Boolean)))
        RegionUserTable.Columns.Add(New DataColumn("AllowInfoLink", GetType(Boolean)))
        RegionUserTable.Columns.Add(New DataColumn("IsInfoLinked", GetType(Boolean)))
        RegionUserTable.Columns.Add(New DataColumn("RealUsername", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("FirstName", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("LastName", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("Email", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("PhoneNumbers", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("Country", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("State", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("City", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("Address", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("PostalCode", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("PreferredLanguage", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("Rank", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("Positions", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("IsArchived", GetType(Boolean)))
        RegionUserTable.Columns.Add(New DataColumn("IsActive", GetType(Boolean)))
        RegionUserTable.Columns.Add(New DataColumn("AlternateEmails", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("CanViewAvailability", GetType(Boolean)))
        RegionUserTable.Columns.Add(New DataColumn("CanViewMasterSchedule", GetType(Boolean)))
        RegionUserTable.Columns.Add(New DataColumn("IsExecutive", GetType(Boolean)))
        RegionUserTable.Columns.Add(New DataColumn("PublicData", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("PrivateData", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("GlobalAvailabilityData", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("RankAndDates ", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("InternalData", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("PhotoId", GetType(Guid)))
        RegionUserTable.Columns.Add(New DataColumn("HasPhotoId", GetType(Boolean)))
        RegionUserTable.Columns.Add(New DataColumn("StatLinks", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("RankNumber", GetType(String)))
        RegionUserTable.Columns.Add(New DataColumn("CanViewSupervisors", GetType(Boolean)))

        For Each RegionUser In RegionUsers
            Dim Row = RegionUserTable.NewRow()
            Row("RegionId") = RegionUser.RegionId
            Row("Username") = RegionUser.Username
            Row("IsLinked") = RegionUser.IsLinked
            Row("AllowInfoLink") = RegionUser.AllowInfoLink
            Row("IsInfoLinked") = RegionUser.IsLinked AndAlso RegionUser.AllowInfoLink
            Row("RealUsername") = RegionUser.RealUsername
            Row("FirstName") = RegionUser.FirstName
            Row("LastName") = RegionUser.LastName
            Row("Email") = RegionUser.Email
            Row("PhoneNumbers") = JsonConvert.SerializeObject(RegionUser.PhoneNumbers)
            Row("Country") = RegionUser.Country
            Row("State") = RegionUser.State
            Row("City") = RegionUser.City
            Row("Address") = RegionUser.Address
            Row("PostalCode") = RegionUser.PostalCode
            Row("PreferredLanguage") = RegionUser.PreferredLanguage
            Row("Rank") = JsonConvert.SerializeObject(RegionUser.Rank)
            Row("Positions") = JsonConvert.SerializeObject(RegionUser.Positions)
            Row("IsArchived") = RegionUser.IsArchived
            Row("IsActive") = RegionUser.IsActive
            Row("AlternateEmails") = JsonConvert.SerializeObject(RegionUser.AlternateEmails)
            Row("CanViewAvailability") = RegionUser.CanViewAvailability
            Row("CanViewMasterSchedule") = RegionUser.CanViewMasterSchedule
            Row("IsExecutive") = RegionUser.IsExecutive
            Row("PublicData") = RegionUser.PublicData
            Row("PrivateData") = RegionUser.PrivateData
            Row("GlobalAvailabilityData") = RegionUser.GlobalAvailabilityData
            Row("RankAndDates") = JsonConvert.SerializeObject(RegionUser.RankAndDates)
            Row("InternalData") = RegionUser.InternalData
            Row("PhotoId") = RegionUser.PhotoId
            Row("HasPhotoId") = RegionUser.HasPhotoId
            Row("StatLinks") = RegionUser.StatLinks
            Row("RankNumber") = JsonConvert.SerializeObject(RegionUser.RankNumber)
            Row("CanViewSupervisors") = RegionUser.CanViewSupervisors
            RegionUserTable.Rows.Add(Row)
        Next

        Using BulkCopy As New SqlBulkCopy(SQLConnection, SqlBulkCopyOptions.Default, SQLTransaction)
            BulkCopy.DestinationTableName = "RegionUser"
            BulkCopy.BulkCopyTimeout = 10000
            BulkCopy.WriteToServer(RegionUserTable)
        End Using
    End Sub

    Public Function IsPosition(CrewType as String) as Boolean
        CrewType = RegionLeaguePayContracted.NormalizeCrewType(CrewType)
        If CrewType = "official" Then
            Return IsOfficial
        Else If CrewType = "scorekeeper" then
            Return IsScorekeeper
        ElseIf CrewType = "supervisor" Then
            Return IsSupervisor
        End If
        Return False
    End Function

    Public Function GetRankFromCrewType(CrewType as String) as String
        CrewType = RegionLeaguePayContracted.NormalizeCrewType(CrewType)
        If Rank.ContainsKey(CrewType) Then Return Rank(CrewType)
        Return ""
    End Function

    Public Function GetRankNumberFromCrewType(CrewType as String) as Decimal
        CrewType = RegionLeaguePayContracted.NormalizeCrewType(CrewType)
        If RankNumber.ContainsKey(CrewType) Then Return RankNumber(CrewType)
        Return 0
    End Function


    Public Function CanRequestLeague(Region As RegionProperties, League As RegionLeaguePayContracted) As Boolean
        If League Is Nothing Then Return True
        If Region Is Nothing Then Return True

        For Each CrewType In RegionLeaguePayContracted.CrewTypes
            If Region.HasCrewType(CrewType) AndAlso IsPosition(CrewType) Then
                Dim MinRankAllowed = Math.Max(RegionLeaguePayContracted.GetRankIndex(League.GetMinRankAllowed(CrewType)), 4)
                Dim MinRankNumberAllowed = League.GetMinRankNumberAllowed(CrewType)

                Dim MaxRankAllowed = Math.Max(RegionLeaguePayContracted.GetRankIndex(League.GetMaxRankAllowed(CrewType)), 4)
                Dim MaxRankNumberAllowed = League.GetMaxRankNumberAllowed(CrewType)

                Dim UserRank = Math.Max(RegionLeaguePayContracted.GetRankIndex(GetRankFromCrewType(CrewType)), 4)
                Dim UserRankNumber = GetRankNumberFromCrewType(CrewType)

                If UserRank < MinRankAllowed Then
                    Continue For
                End If

                If UserRank = MinRankAllowed AndAlso UserRankNumber < MinRankNumberAllowed Then
                    Continue For
                End If

                If UserRank > MaxRankAllowed Then
                    Continue For
                End If

                If UserRank = MaxRankAllowed AndAlso UserRankNumber > MaxRankNumberAllowed Then
                    Continue For
                End If

                Return True
            End If
        Next

        Return False
    End Function

    Public Function GetRankText(Region As RegionProperties, PositionId as String, L As String) as String

        PositionId = PositionId.ToLower()
        if PositionId <> "official" AndAlso PositionId <> "scorekeeper" AndAlso PositionId <> "supervisor" Then
            return ""
        End If
        
        If Me.Rank Is Nothing OrElse Not Me.Rank.ContainsKey(PositionId) Then
            return ""
        End If

        Dim Rank = Me.Rank(PositionId)
        If L.ToLower.Contains("en") Then
            If Rank = "senior" Then
                Return Region.SeniorRankNameEnglish
            ElseIf Rank = "junior" Then
                Return Region.JuniorRankNameEnglish
            ElseIf Rank = "intermediate" Then
                Return Region.IntermediateRankNameEnglish
            ElseIf Rank = "novice" Then
                Return Region.NoviceRankNameEnglish
            ElseIf Rank = "rookie" Then
                Return Region.RookieRankNameEnglish
            End If
        Else
            If Rank = "senior" Then
                Return Region.SeniorRankNameFrench
            ElseIf Rank = "junior" Then
                Return Region.JuniorRankNameFrench
            ElseIf Rank = "intermediate" Then
                Return Region.IntermediateRankNameFrench
            ElseIf Rank = "novice" Then
                Return Region.NoviceRankNameFrench
            ElseIf Rank = "rookie" Then
                Return Region.RookieRankNameFrench
            End If
        End If
        Return ""
    End Function

    Public Function GetSubRankText(Region As RegionProperties, PositionId as String) as String

        PositionId = PositionId.ToLower()
        if PositionId <> "official" AndAlso PositionId <> "scorekeeper" AndAlso PositionId <> "supervisor" Then
            return ""
        End If
        
        If Me.RankNumber Is Nothing OrElse Not Me.RankNumber.ContainsKey(PositionId) Then
            return ""
        End If

        Return Me.RankNumber(PositionId)
    End Function

    Public Shared Function IsInRegion(RegionId as String, RegionUsers As List(of RegionUser), RealUsername As string)
        If RealUsername = "" then Return False
        Return RegionUsers.Any(Function(RegionUser) RegionUser.RegionId = RegionId AndAlso RegionUser.RealUsername = RealUsername)
    End Function

    Public Shared Function IsExecutive(RegionId as String, RegionUsers As List(of RegionUser), RealUsername As string)
        If RealUsername = "" then Return False
        Return RealUsername = "superadmin" OrElse RegionUsers.Any(Function(RegionUser) RegionUser.RegionId = RegionId AndAlso RegionUser.RealUsername = RealUsername AndAlso RegionUser.IsExecutive())
    End Function


    public Shared Function ShowAddress(Region As RegionProperties, RegionUsers as List(Of RegionUser), RealUsername As string)
        If RealUsername = "superadmin" Then Return true
        If Region Is Nothing Then
           return False
        End If

        If RegionUser.IsInRegion(Region.RegionId, RegionUsers, RealUsername) Then
            If Region.ShowAddress Then
                Return true
            Else
                If RegionUser.IsExecutive(Region.RegionId, RegionUsers, RealUsername) Then
                    Return True
                End If
            End If
        End If

        Return False
    End Function

    Public Shared Function ShowEmailAndPhoneNumber(Region As RegionProperties, RegionUsers as List(Of RegionUser), RealUsername As string)
        If RealUsername = "superadmin" Then Return true
        If Region Is Nothing Then
            return False
        End If

        Return RegionUser.IsInRegion(Region.RegionId, RegionUsers, RealUsername)
    End Function

    Public Shared Function ShowRank(Region As RegionProperties, RegionUsers as List(Of RegionUser), RealUsername As string)
        If RealUsername = "superadmin" Then Return true
        If Region Is Nothing Then
            return False
        End If


        Dim IsInRegion = RegionUser.IsInRegion(Region.RegionId, RegionUsers, RealUsername)
        Dim IsExecutive = RegionUser.IsExecutive(Region.RegionId, RegionUsers, RealUsername)

        If Region.ShowRankToNonMembers OrElse IsExecutive Then
            return True
        End If

        If IsInRegion Then
            Return Region.ShowRank
        End If
        
        return False
    End Function

    Public Shared Function ShowSubRank(Region As RegionProperties, RegionUsers as List(Of RegionUser), RealUsername As string)
        If Region Is Nothing Then
            return RealUsername = "superadmin"
        End If

        Dim IsInRegion = RegionUser.IsInRegion(Region.RegionId, RegionUsers, RealUsername)
        Dim IsExecutive = RegionUser.IsExecutive(Region.RegionId, RegionUsers, RealUsername)

        If Region.ShowSubRankToNonMembers OrElse IsExecutive Then
            return True
        End If

        If IsInRegion Then
            Return Region.ShowSubRank
        End If
        
        Return False
    End Function


    Public Sub ClearPrivateData(RealUsername As String, Region As RegionProperties, RegionUsers As List(Of RegionUser))
        If Not ShowAddress(Region, RegionUsers, RealUsername) Then
            Country = ""
            State = ""
            City = ""
            Address = ""
            PostalCode = ""
        End If

        If Not ShowEmailAndPhoneNumber(Region, RegionUsers, RealUsername) Then
            Email = ""
            PhoneNumbers = New List(Of PhoneNumber)
            AlternateEmails = New List(Of String)
        End If

        Dim RegionId as String = ""
        If Region IsNot Nothing Then
            RegionId = Region.RegionId
        End If


        Dim IsInRegion = RegionUser.IsInRegion(RegionId, RegionUsers, RealUsername)
        Dim IsExecutive = RegionUser.IsExecutive(RegionId, RegionUsers, RealUsername)

        if Not IsExecutive then
            PrivateData = ""
            GlobalAvailabilityData = ""
        End If

        If Not IsInRegion Then
            InternalData = ""
        End If

        If Not ShowRank(Region, RegionUsers, RealUsername) Then
            Rank = New Dictionary(Of String, String)
        End If

        If Not ShowSubRank(Region, RegionUsers, RealUsername) Then
            RankNumber = New Dictionary(Of String, Decimal)
        End If
    End Sub

    Public Function IsSimilarUser(RegionUser As RegionUser) As Boolean
        If FirstName.Trim().ToLower <> RegionUser.FirstName.Trim().ToLower() OrElse LastName.Trim().ToLower <> RegionUser.LastName.Trim().ToLower() Then Return False
        If RealUsername <> "" AndAlso RegionUser.RealUsername <> "" Then
            Return RealUsername = RegionUser.RealUsername
        End If
        If Email.Trim() <> "" AndAlso Email.Trim().ToLower = RegionUser.Email.Trim.ToLower Then
            Return True
        End If
        Return False
    End Function

    Public Shared Function IsValidPosition(Region As Region, Position As String) As Boolean
        Dim AllowedPositions As New List(Of String)

        If Region.EntityType = "team" Then
            AllowedPositions.Add("player")
            AllowedPositions.Add("callup")
            AllowedPositions.Add("spectator")
            AllowedPositions.Add("manager")
            AllowedPositions.Add("coach")
        Else
            If Region.HasOfficials Then
                AllowedPositions.Add("official")
            End If
            If Region.HasScorekeepers Then
                AllowedPositions.Add("scorekeeper")
            End If
            If Region.HasSupervisors Then
                AllowedPositions.Add("supervisor")
            End If
            AllowedPositions.Add("spectator")
            AllowedPositions.Add("assignor")
            AllowedPositions.Add("chief")
        End If

        Return AllowedPositions.Contains(Position)
    End Function

    Public Function IsValidBasic(Region As Region) As Object
        If Me.Username = "" Then Return New ErrorObject("UsernameIsEmpty")
        If Not PublicCode.IsAlphaNum(Me.Username) Then Return New ErrorObject("UsernameAlphaNumeric")
        If Me.Positions Is Nothing OrElse Me.Positions.Count <> 1 Then Return New ErrorObject("NoPosition")
        Me.Positions(0) = Me.Positions(0).ToLower()
        If Not RegionUser.IsValidPosition(Region, Me.Positions(0)) Then Return New ErrorObject("InvalidPosition")
        Me.Username = Me.Username.ToLower
        Return Nothing
    End Function

    Public Function ShowMasterSchedule() As Boolean
        If RealUsername = "superadmin" Then Return True
        Return Me.IsExecutive OrElse Me.CanViewMasterSchedule
    End Function

    Public Function IsValid(Region As Region) As Object
        Dim IsValidObject = IsValidBasic(Region) : If IsValidObject IsNot Nothing Then Return IsValidObject
        If FirstName.Trim() = "" Then Return New ErrorObject("FirstNameRequired")
        If LastName.Trim() = "" Then Return New ErrorObject("LastNameRequired")
        If Me.Email <> "" AndAlso Not PublicCode.IsEmail(Me.Email) Then Return New ErrorObject("InvalidEmail")
        Me.Email = Me.Email.ToLower
        Me.Username = Me.Username.ToLower
        If AlternateEmails Is Nothing Then AlternateEmails = New List(Of String)
        For I As Integer = 0 To AlternateEmails.Count - 1
            If AlternateEmails(I) <> "" AndAlso Not PublicCode.IsEmail(AlternateEmails(I)) Then Return New ErrorObject("InvalidAlternateEmail")
            AlternateEmails(I) = AlternateEmails(I).ToLower
        Next

        Dim TEmails As New List(Of String)
        If Email <> "" Then TEmails.Add(Email)
        For Each TEmail In AlternateEmails
            If TEmails.Contains(TEmail) Then Return New ErrorObject("DuplicateEmail")
            TEmails.Add(TEmail)
        Next

        Return Nothing
    End Function

    Private Sub LoadFromSQLReader(Reader As SqlDataReader, RegionId As String)
        Me.RegionId = RegionId
        Me.Username = Reader.GetString(0).ToLower
        Me.IsLinked = Reader.GetBoolean(1)
        Me.AllowInfoLink = Reader.GetBoolean(2)
        Me.RealUsername = Reader.GetString(3)
        Me.FirstName = Reader.GetString(4)
        Me.LastName = Reader.GetString(5)
        Me.Email = Reader.GetString(6).ToLower
        Me.PhoneNumbers = JsonConvert.DeserializeObject(Of List(Of PhoneNumber))(Reader.GetString(7))
        Me.AlternateEmails = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(8).ToLower)
        Me.Country = Reader.GetString(9)
        Me.State = Reader.GetString(10)
        Me.City = Reader.GetString(11)
        Me.Address = Reader.GetString(12)
        Me.PostalCode = Reader.GetString(13)
        Me.PreferredLanguage = Reader.GetString(14)
        Me.Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(15).ToLower)
        Me.Rank = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString(16))
        Me.RankNumber = JsonConvert.DeserializeObject(Of Dictionary(Of String, Decimal))(Reader.GetString(17))
        Me.IsActive = Reader.GetBoolean(18)
        Me.CanViewAvailability = Reader.GetBoolean(19)
        Me.CanViewMasterSchedule = Reader.GetBoolean(20)
        Me.CanViewSupervisors = Reader.GetBoolean(21)
        Me.PublicData = Reader.GetString(22)
        Me.PrivateData = Reader.GetString(23)
        Me.GlobalAvailabilityData = Reader.GetString(24)
        Me.RankAndDates = JsonConvert.DeserializeObject(Of List(Of RankAndDate))(Reader.GetString(25))
        Me.InternalData = Reader.GetString(26)
        Me.StatLinks = Reader.GetString(27)
        Me.PhotoId = Reader.GetGuid(28)
        Me.HasPhotoId = Reader.GetBoolean(29)
        Dim TPhotoId = Reader.GetGuid(30)
        Dim THasPhotoId = Reader.GetBoolean(31)

        If Not Me.HasPhotoId AndAlso THasPhotoId Then
            Me.HasPhotoId = THasPhotoId
            Me.PhotoId = TPhotoId
        End If
    End Sub

    Private Sub LoadFromSQLReaderWithRegion(Reader As SqlDataReader)
        Me.RegionId = Reader.GetString(0).ToLower
        Me.Username = Reader.GetString(1).ToLower
        Me.IsLinked = Reader.GetBoolean(2)
        Me.AllowInfoLink = Reader.GetBoolean(3)
        Me.RealUsername = Reader.GetString(4).ToLower
        Me.FirstName = Reader.GetString(5)
        Me.LastName = Reader.GetString(6)
        Me.Email = Reader.GetString(7).ToLower
        Me.PhoneNumbers = JsonConvert.DeserializeObject(Of List(Of PhoneNumber))(Reader.GetString(8))
        Me.AlternateEmails = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(9).ToLower)
        Me.Country = Reader.GetString(10)
        Me.State = Reader.GetString(11)
        Me.City = Reader.GetString(12)
        Me.Address = Reader.GetString(13)
        Me.PostalCode = Reader.GetString(14)
        Me.PreferredLanguage = Reader.GetString(15).ToLower
        Me.Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(16).ToLower)
        Me.Rank = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString(17))
        Me.RankNumber = JsonConvert.DeserializeObject(Of Dictionary(Of String, Decimal))(Reader.GetString(18))
        Me.IsActive = Reader.GetBoolean(19)
        Me.CanViewAvailability = Reader.GetBoolean(20)
        Me.CanViewMasterSchedule = Reader.GetBoolean(21)
        Me.CanViewSupervisors = Reader.GetBoolean(22)
        Me.PublicData = Reader.GetString(23)
        Me.PrivateData = Reader.GetString(24)
        Me.GlobalAvailabilityData = Reader.GetString(25)
        Me.RankAndDates = JsonConvert.DeserializeObject(Of List(Of RankAndDate))(Reader.GetString(26))
        Me.InternalData = Reader.GetString(27)
        Me.StatLinks = Reader.GetString(28)
        Me.PhotoId = Reader.GetGuid(29)
        Me.HasPhotoId = Reader.GetBoolean(30)

        Dim TPhotoId = Reader.GetGuid(31)
        Dim THasPhotoId = Reader.GetBoolean(32)

        If Not Me.HasPhotoId AndAlso THasPhotoId Then
            Me.HasPhotoId = THasPhotoId
            Me.PhotoId = TPhotoId
        End If
    End Sub

    Private Sub LoadFromSQLReaderWithRegion(RegionId As String, Reader As SQLReaderIncrementor)
        Me.RegionId = RegionId
        Me.Username = Reader.GetString().ToLower
        Me.IsLinked = Reader.GetBoolean()
        Me.AllowInfoLink = Reader.GetBoolean()
        Me.RealUsername = Reader.GetString().ToLower
        Me.FirstName = Reader.GetString()
        Me.LastName = Reader.GetString()
        Me.Email = Reader.GetString().ToLower
        Me.PhoneNumbers = JsonConvert.DeserializeObject(Of List(Of PhoneNumber))(Reader.GetString())
        Me.AlternateEmails = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString().ToLower)
        Me.Country = Reader.GetString()
        Me.State = Reader.GetString()
        Me.City = Reader.GetString()
        Me.Address = Reader.GetString()
        Me.PostalCode = Reader.GetString()
        Me.PreferredLanguage = Reader.GetString().ToLower
        Me.Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString().ToLower)
        Me.Rank = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString())
        Me.RankNumber = JsonConvert.DeserializeObject(Of Dictionary(Of String, Decimal))(Reader.GetString())
        Me.IsActive = Reader.GetBoolean()
        Me.CanViewAvailability = Reader.GetBoolean()
        Me.CanViewMasterSchedule = Reader.GetBoolean()
        Me.CanViewSupervisors = Reader.GetBoolean()
        Me.PublicData = Reader.GetString()
        Me.PrivateData = Reader.GetString()
        Me.GlobalAvailabilityData = Reader.GetString()
        Me.RankAndDates = JsonConvert.DeserializeObject(Of List(Of RankAndDate))(Reader.GetString())
        Me.InternalData = Reader.GetString()
        Me.StatLinks = Reader.GetString()
        Me.PhotoId = Reader.GetGuid()
        Me.HasPhotoId = Reader.GetBoolean()
    End Sub

    Public Sub New()

    End Sub

    Public Sub New(RegionId As String, Username As String)
        Me.RegionId = RegionId
        Me.Username = Username
    End Sub

    Public Sub New(Reader As SqlDataReader, RegionId As String)
        LoadFromSQLReader(Reader, RegionId)
    End Sub

    Public Shared Function GetFullNameFromRegionUsers(RegionId As String, Username As String, RegionUsers As List(Of RegionUser))
        Dim RegionUser As RegionUser = PublicCode.BinarySearchItem(RegionUsers, New RegionUser(RegionId, Username), BasicSorter)
        If RegionUser Is Nothing Then Return ""
        Return RegionUser.FirstName & " " & RegionUser.LastName
    End Function

    Public Shared Function LoadBasic(RegionId As String, Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As RegionUser
        Dim Result As RegionUser = Nothing

        Dim CommandText = <SQL>
SELECT
	Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, UPhotoId, UHasPhotoId
FROM (
		SELECT
			Username, IsLinked, AllowInfoLink, IsInfoLinked, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
		FROM
			RegionUser
		WHERE RegionId = @RegionId AND Username = @Username AND IsInfoLinked = 0
	UNION
		SELECT
			RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.IsInfoLinked, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		FROM
			RegionUser As RU, [User] AS U
		WHERE RU.RegionId = @RegionId AND RU.Username = @Username AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
) As _RegionUser
</SQL>.Value()

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Result = New RegionUser
                Result.LoadFromSQLReader(Reader, RegionId)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function LoadBasicFromRealUsername(RegionId As String, RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As RegionUser
        Dim Result As RegionUser = Nothing

        Dim CommandText = <SQL>
SELECT
	Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, UPhotoId, UHasPhotoId
FROM (
		SELECT
			Username, IsLinked, AllowInfoLink, IsInfoLinked, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
		FROM
			RegionUser
		WHERE RegionId = @RegionId AND RealUsername = @RealUsername AND IsInfoLinked = 0
	UNION
		SELECT
			RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.IsInfoLinked, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		FROM
			RegionUser As RU, [User] AS U
		WHERE RU.RegionId = @RegionId AND RU.RealUsername = @RealUsername AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
) As _RegionUser
                          </SQL>.Value()
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Result = New RegionUser
                Result.LoadFromSQLReader(Reader, RegionId)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function LoadBasicFromUsername(RegionId As String, Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As RegionUser
        Dim Result As RegionUser = Nothing

        Dim CommandText = <SQL>
SELECT
	Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, UPhotoId, UHasPhotoId
FROM (
		SELECT
			Username, IsLinked, AllowInfoLink, IsInfoLinked, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
		FROM
			RegionUser
		WHERE RegionId = @RegionId AND Username = @Username AND IsInfoLinked = 0
	UNION
		SELECT
			RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.IsInfoLinked, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		FROM
			RegionUser As RU, [User] AS U
		WHERE RU.RegionId = @RegionId AND RU.Username = @Username AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
) As _RegionUser
                          </SQL>.Value()
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Result = New RegionUser
                Result.LoadFromSQLReader(Reader, RegionId)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function LoadPositionsFromRealUsername(RegionId As String, RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As RegionUser
        Dim Result As RegionUser = Nothing

        Dim CommandText = "SELECT Username, Positions FROM RegionUser Where RegionId = @RegionId AND RealUsername = @RealUsername"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Result = New RegionUser With {
                    .Username = Reader.GetString(0).ToLower,
                    .Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(1).ToLower)
                }
                Result.LoadFromSQLReader(Reader, RegionId)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function RealUserNameExists(RegionId As String, RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As Boolean
        Dim Result As Integer = 0

        Dim CommandText = "SELECT RealUsername FROM RegionUser Where RegionId = @RegionId AND RealUsername = @RealUsername"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Result = 1
            End While
            Reader.Close()
        End Using

        Return Result > 0
    End Function

    Public Shared Function LoadAllFromRealUsername(RealUsername As String, Optional OnlyImExec As Boolean = False) As Object
        Dim RegionUserWithRegions As New List(Of RegionUser)
        Dim RegionProperties As New List(Of RegionProperties)
        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    If OnlyImExec Then
                        Dim UniqueRegionIds = RegionUser.GetAllMyRegionsIds(RealUsername, SqlConnection, SqlTransaction)

                        If UniqueRegionIds.Count > 0 Then
                            Dim RegionUsers = RegionUser.LoadAllInRegionIdsHelper(UniqueRegionIds, RealUsername, SqlConnection, SqlTransaction)
                            RegionProperties = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)

                            For I As Integer = UniqueRegionIds.Count - 1 To 0 Step -1
                                Dim URId = UniqueRegionIds(I)
                                Dim IncludeRegion = RegionProperties.Find(Function(R) R.RegionId = URId).ShowFullScheduleToNonMembers
                                If Not IncludeRegion Then
                                    If RealUsername = "superadmin" Then
                                        IncludeRegion = True
                                    Else
                                        For Each RegionUser In RegionUsers
                                            If RegionUser.RegionId = URId AndAlso RegionUser.RealUsername = RealUsername Then
                                                If RegionUser.IsExecutive OrElse RegionUser.CanViewMasterSchedule Then
                                                    IncludeRegion = True
                                                    Exit For
                                                End If
                                            End If
                                        Next
                                    End If
                                End If

                                If Not IncludeRegion Then
                                    UniqueRegionIds.RemoveAt(I)

                                    Dim TRegionUsers As New List(Of RegionUser)
                                    For Each RegionUser In RegionUsers
                                        If RegionUser.RegionId <> URId Then
                                            TRegionUsers.Add(RegionUser)
                                        End If
                                    Next
                                    RegionUsers = TRegionUsers

                                    Dim TRegionProperties As New List(Of RegionProperties)
                                    For Each RegionProperty In RegionProperties
                                        If RegionProperty.RegionId <> URId Then
                                            TRegionProperties.Add(RegionProperty)
                                        End If
                                    Next
                                    RegionProperties = TRegionProperties
                                End If
                            Next

                            Dim ExecutiveForRegions As New List(Of String)
                            Dim InRegions As New List(Of String)

                            If RealUsername = "superadmin" Then
                                For Each RegionUser In RegionUsers
                                    If RegionUser.RealUsername = "" Then Continue For
                                    If Not InRegions.Contains(RegionUser.RegionId) Then
                                        ExecutiveForRegions.Add(RegionUser.RegionId)
                                    End If
                                    If Not InRegions.Contains(RegionUser.RegionId) Then
                                        InRegions.Add(RegionUser.RegionId)
                                    End If
                                Next
                            Else
                                For Each RegionUser In RegionUsers
                                    If RegionUser.RealUsername = "" Then Continue For
                                    If RegionUser.RealUsername = RealUsername AndAlso RegionUser.IsExecutive AndAlso Not InRegions.Contains(RegionUser.RegionId) Then
                                        ExecutiveForRegions.Add(RegionUser.RegionId)
                                    End If
                                    If RegionUser.RealUsername = RealUsername AndAlso Not InRegions.Contains(RegionUser.RegionId) Then
                                        InRegions.Add(RegionUser.RegionId)
                                    End If
                                Next
                            End If

                            For Each RegionUser In RegionUsers
                                RegionUser.ClearPrivateData(RealUsername, RegionProperties.Find(Function(Region) Region.RegionId = RegionUser.RegionId), RegionUsers)
                            Next

                            RegionUserWithRegions = RegionUsers
                        End If
                    Else
                        RegionUserWithRegions = LoadAllFromRealUsernameHelper(RealUsername, SqlConnection, SqlTransaction)
                    End If
                End Using
            End Using

            CleanRegionUsers(RegionProperties, RegionUserWithRegions, RealUsername)

            Return New With {
                .Success = True,
                .RegionUsers = RegionUserWithRegions,
                .Regions = RegionProperties
            }
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function LoadAllFromRealUsernameHelper(RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionUser)
        Dim Results As New List(Of RegionUser)

        Dim CommandText = <SQL>
        SELECT
			RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, UPhotoId, UHasPhotoId
        FROM (
		    SELECT
			    RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
		    FROM
			    RegionUser
		    WHERE RegionId In(Select RegionId FROM RegionUser WHERE RealUsername = @RealUsername) AND IsInfoLinked = 0
	    UNION
		    SELECT
			    RU.RegionId, RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		    FROM
			    RegionUser As RU, [User] AS U
		    WHERE RU.RegionId In(Select RegionId FROM RegionUser WHERE RealUsername = @RealUsername) AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
        ) As _RegionUser ORDER BY RegionId, Username                              
        </SQL>.Value

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim RegionUser As New RegionUser()
                RegionUser.LoadFromSQLReaderWithRegion(Reader)
                Results.Add(RegionUser)
            End While
            Reader.Close()
        End Using


        Return Results
    End Function

    Public Shared Function LoadAllFromRealUsernameExecHelper(RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionUser)
        Dim Results As New List(Of RegionUser)

        Dim CommandText = <SQL>
        SELECT
			RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, UPhotoId, UHasPhotoId
        FROM (
		    SELECT
			    RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
		    FROM
			    RegionUser
		    WHERE RegionId In(Select RegionId FROM RegionUser WHERE RealUsername = @RealUsername AND IsArchived = 0 AND (Positions LIKE '%chief%' OR Positions LIKE '%assignor%')) AND IsInfoLinked = 0
	    UNION
		    SELECT
			    RU.RegionId, RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		    FROM
			    RegionUser As RU, [User] AS U
		    WHERE RU.RegionId In(Select RegionId FROM RegionUser WHERE RealUsername = @RealUsername AND IsArchived = 0 AND (Positions LIKE '%chief%' OR Positions LIKE '%assignor%')) AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
        ) As _RegionUser ORDER BY Username                              
        </SQL>.Value

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim RegionUser As New RegionUser()
                RegionUser.LoadFromSQLReaderWithRegion(Reader)
                Results.Add(RegionUser)
            End While
            Reader.Close()
        End Using


        Return Results
    End Function

    Public Shared Function LoadAllMyRegionUsersFromRealUsername(RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionUser)
        Dim Results As New List(Of RegionUser)

        Dim CommandText = <SQL>
        SELECT
			RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, UPhotoId, UHasPhotoId
        FROM (
		    SELECT
			    RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
		    FROM
			    RegionUser
		    WHERE RealUsername = @RealUsername AND IsArchived = 0 AND IsInfoLinked = 0
	    UNION
		    SELECT
			    RU.RegionId, RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		    FROM
			    RegionUser As RU, [User] AS U
		    WHERE RU.RealUsername = @RealUsername AND RU.IsArchived = 0 AND RU.IsInfoLinked = 1 AND RU.RealUsername = U.Username
        ) As _RegionUser ORDER BY Username                              
        </SQL>.Value

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim RegionUser As New RegionUser()
                RegionUser.LoadFromSQLReaderWithRegion(Reader)
                Results.Add(RegionUser)
            End While
            Reader.Close()
        End Using


        Return Results
    End Function

    Public Shared Function LoadRegionAndRegionUserFromRealUsernameHelper(RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionAndRegionUser)
        Dim Results As New List(Of RegionAndRegionUser)

        Dim CommandText = <SQL>
		SELECT
			RU.RegionId, RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, R.RegionName, R.Sport, R.EntityType, R.Season, R.DefaultCrew, R.ShowAddress, R.ShowRank, R.ShowSubRank, R.ShowRankToNonMembers, R.ShowSubRankToNonMembers, R.DefaultContactListSort, R.ShowRankInSchedule, R.ShowSubRankInSchedule, R.MaxSubRankRequest, R.SubRankIsNumber, R.ShowTeamsInSchedule, R.SortSubRankDesc, R.SeniorRankNameEnglish, R.SeniorRankNameFrench, R.JuniorRankNameEnglish, R.JuniorRankNameFrench, R.IntermediateRankNameEnglish, R.IntermediateRankNameFrench, R.NoviceRankNameEnglish, R.NoviceRankNameFrench, R.RookieRankNameEnglish, R.RookieRankNameFrench, R.AllowBookOffs, R.BookOffHoursBefore, R.MaxDateShowAvailableSpots, R.EmailAvailableSpots, R.EmailRequestedGames, R.DefaultArriveBeforeMins, R.DefaultMaxGameLengthMins, R.OnlyAllowedToConfirmDaysBefore, R.EmailConfirmDaysBefore, R.HasOfficials, R.HasScorekeepers, R.HasSupervisors, R.IsDemo, R.Country, R.State, R.City, R.Address, R.PostalCode, R.ShowLinksToNonMembers, R.ShowParksToNonMembers, R.ShowLeaguesToNonMembers, R.ShowOfficialRegionsToNonMembers, R.ShowTeamsToNonMembers, R.ShowHolidayListToNonMembers, R.ShowContactListToNonMembers, R.ShowAvailabilityDueDateToNonMembers, R.ShowAvailabilityToNonMembers, R.ShowAvailableSpotsToNonMembers, R.ShowFullScheduleToNonMembers, R.ShowStandingsToNonMembers, R.ShowStatsToNonMembers, R.ShowMainPageToNonMembers, R.HasEnteredMainPage, R.ExtraScoreParameters, R.HomeTeamCanEnterScore, R.AwayTeamCanEnterScore, R.ScorekeeperCanEnterScore, R.TeamCanEnterStats, R.ScorekeeperCanEnterStats, R.RegionIsLadderLeague, R.NumberOfPlayersInLadderLeague, R.HomeTeamCanCancelGameHoursBefore, R.AwayTeamCanCancelGameHoursBefore, R.MinimumValue, R.MinimumPercentage, R.TimeZone, R.AutoSyncCancellationHoursBefore, R.AutoSyncSchedule, R.UniquePlayers, R.StatIndex, R.StandingIndex, R.DefaultArriveBeforeAwayMins, R.DefaultArriveBeforePracticeMins, R.DefaultMaxGameLengthPracticeMins, R.IncludeAfternoonAvailabilityOnWeekdays, R.IncludeMorningAvailabilityOnWeekdays, R.EnableNotFilledInInAvailability, R.EnableDualAvailability, R.IsAvailableText, R.IsAvailableDualText, R.IsAvailableCombinedText, R.ShowOnlyDueDatesRangeForAvailability, R.ShowRankInGlobalAvailability, R.ShowSubRankInGlobalAvailability, R.SortGlobalAvailabilityByRank, R.SortGlobalAvailabilityBySubRank, R.NotifyPartnerOfCancellation, R.ShowPhotosToNonMembers, R.ShowArticlesToNonMembers, R.ShowWallToNonMembers, R.LeagueRankMaxes, R.LinkedRegionIds, R.StatLinks, R.PhotoId, R.HasPhotoId, RU.IsArchived
		FROM
			RegionUser As RU, [User] AS U, Region AS R
		WHERE RU.RealUsername = @RealUsername AND RU.RegionId = R.RegionID AND U.Username = RU.RealUsername
                          </SQL>.Value

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)

            While Reader.Read
                Dim RegionAndRegionUser As New RegionAndRegionUser With {
                    .Region = New Region,
                    .RegionUser = New RegionUser
                }

                Dim RegionId As String = Reader.GetString().ToLower
                RegionAndRegionUser.RegionUser.LoadFromSQLReaderWithRegion(RegionId, Reader)

                RegionAndRegionUser.Region = New Region With {
                    .RegionId = RegionId,
                    .RegionName = Reader.GetString(),
                    .Sport = Reader.GetString().ToLower,
                    .EntityType = Reader.GetString().ToLower,
                    .Season = Reader.GetInt32(),
                    .DefaultCrew = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
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
                    .SortSubRankDesc  = Reader.GetBoolean(),
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
                    .StatLinks = Reader.GetString(),
                    .PhotoId = Reader.GetGuid(),
                    .HasPhotoId = Reader.GetBoolean()
                }

                RegionAndRegionUser.RegionUser.IsArchived = Reader.GetBoolean()

                Results.Add(RegionAndRegionUser)
            End While
            Reader.Close()
        End Using


        Return Results
    End Function

    Public Shared Function LoadAllInRegion(RealUsername As String, RegionId As String) As Object
        Dim RegionUsers As New List(Of RegionUser)
        Dim Official As RegionUser = Nothing
        Dim RegionUserTemps As New Dictionary(Of String, List(Of RegionUser))
        Dim IsExecutive As Boolean = False
        Dim Assignor As RegionUser = Nothing
        Dim Region As RegionProperties = Nothing

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Region = UmpireAssignor.RegionProperties.GetRegionPropertiesHelper(RegionId, SqlConnection, SqlTransaction, True)

                    Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, RealUsername, SqlConnection, SqlTransaction)
                    If Assignor Is Nothing Then
                        Dim MyRegions = UmpireAssignor.Region.GetMyRegionsHelper(RealUsername, SqlConnection, SqlTransaction)
                        Dim MyRegionIds As New List(Of String)
                        For Each MyRegion In MyRegions
                            MyRegionIds.Add(MyRegion.RegionId)
                        Next
                        Dim TLeagues = RegionLeague.GetRegionsLeaguesHelper(MyRegionIds, SqlConnection, SqlTransaction)
                        For Each TLeague In TLeagues
                            If TLeague.RealLeagueId = RegionId AndAlso TLeague.IsLinked Then
                                Assignor = New RegionUser With {
                                    .RegionId = TLeague.RegionId,
                                    .Positions = New List(Of String)
                                }
                                Exit For
                            End If
                        Next
                    End If
                    If Assignor Is Nothing AndAlso Not Region.ShowContactListToNonMembers Then
                        Return New ErrorObject("InvalidPermissions")
                    End If

                    RegionUsers = LoadAllInRegionHelper(RegionId, RealUsername, SqlConnection, SqlTransaction, IsExecutive, Assignor IsNot Nothing)

                    If Assignor IsNot Nothing AndAlso Assignor.IsExecutive() Then
                        Dim CommandText As String = "SELECT UserSubmitPosition FROM RegionUserTempSubmitted WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId"
                        Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                            SqlCommand.Parameters.Add(New SqlParameter("AssignorId", RealUsername))

                            Dim Reader = SqlCommand.ExecuteReader
                            While Reader.Read
                                RegionUserTemps.Add(Reader.GetString(0).ToLower, New List(Of RegionUser))
                            End While
                            Reader.Close()
                        End Using

                        CommandText = "SELECT UserSubmitPosition, Username, AllowInfoLink, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData FROM RegionUserTemp WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId"
                        Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                            SqlCommand.Parameters.Add(New SqlParameter("AssignorId", RealUsername))

                            Dim Reader = SqlCommand.ExecuteReader
                            While Reader.Read
                                Dim UserSubmitPosition As String = Reader.GetString(0).ToLower

                                Dim TRegionUser = New RegionUser With {
                                    .RegionId = RegionId,
                                    .Username = Reader.GetString(1).ToLower,
                                    .AllowInfoLink = Reader.GetBoolean(2),
                                    .FirstName = Reader.GetString(3),
                                    .LastName = Reader.GetString(4),
                                    .Email = Reader.GetString(5).ToLower,
                                    .PhoneNumbers = JsonConvert.DeserializeObject(Of List(Of PhoneNumber))(Reader.GetString(6)),
                                    .AlternateEmails = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(7).ToLower),
                                    .Country = Reader.GetString(8),
                                    .State = Reader.GetString(9),
                                    .City = Reader.GetString(10),
                                    .Address = Reader.GetString(11),
                                    .PostalCode = Reader.GetString(12),
                                    .PreferredLanguage = Reader.GetString(13).ToLower,
                                    .Rank = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString(14).ToLower),
                                    .RankNumber = JsonConvert.DeserializeObject(Of Dictionary(Of String, Decimal))(Reader.GetString(15).ToLower),
                                    .IsActive = Reader.GetBoolean(16),
                                    .CanViewAvailability = Reader.GetBoolean(17),
                                    .CanViewMasterSchedule = Reader.GetBoolean(18),
                                    .CanViewSupervisors = Reader.GetBoolean(19),
                                    .PublicData = Reader.GetString(20),
                                    .PrivateData = Reader.GetString(21),
                                    .GlobalAvailabilityData = Reader.GetString(22),
                                    .RankAndDates = JsonConvert.DeserializeObject(of List(Of RankAndDate))(Reader.GetString(23)),
                                    .InternalData = Reader.GetString(24),
                                    .StatLinks = "",
                                    .PhotoId = Guid.Empty,
                                    .HasPhotoId = False
                                }

                                RegionUserTemps(UserSubmitPosition).Add(TRegionUser)
                            End While
                            Reader.Close()
                        End Using
                    End If

                End Using
            End Using

            For Each RU In RegionUsers
                If RU.RealUsername = RealUsername AndAlso RealUsername <> "" Then
                    If RU.IsExecutive OrElse RealUsername = "superadmin" Then IsExecutive = True
                End If
            Next

            CleanRegionUsers({Region}.ToList(), RegionUsers, RealUsername)

            Return New With {
                .Success = True,
                .IsExecutive = IsExecutive,
                .IsInRegion = Assignor IsNot Nothing,
                .RegionUsers = RegionUsers,
                .Regions = {Region}.ToList(),
                .Official = Official,
                .RegionUserTemps = RegionUserTemps
            }
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function


    Public Shared Function LoadAllInAllRegionsHelper(RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionUser)
        Dim Results As New List(Of RegionUser)

        Dim CommandText = <SQL>                              
        SELECT
			RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, UPhotoId, UHasPhotoId
        FROM (
		    SELECT
			    RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
		    FROM
			    RegionUser 
		    WHERE RegionId IN (SELECT RegionId FROM RegionUser WHERE RealUsername = @RealUsername AND IsArchived = 0) AND IsInfoLinked = 0
	    UNION
		    SELECT
			    RU.RegionId, RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		    FROM
			    RegionUser As RU, [User] AS U
		    WHERE RegionId IN (SELECT RegionId FROM RegionUser WHERE RealUsername = @RealUsername AND IsArchived = 0) AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
        ) As _RegionUser ORDER BY RegionId, Username
                          </SQL>.Value

        Dim Regions = RegionProperties.GetAllMyRegionPropertiesHelper(RealUsername, SQLConnection, SQLTransaction)

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim TRegionUser As New RegionUser()
                TRegionUser.LoadFromSQLReaderWithRegion(Reader)
                Results.Add(TRegionUser)
            End While
            Reader.Close()
        End Using

        For Each RU In Results
            Dim Region = Regions.Find(Function(R) R.RegionId = RU.REgionId)
            RU.ClearPrivateData(RealUsername, Region, Results)
        Next

        Return Results
    End Function

    Public Shared Function LoadAllInRegionHelper(RegionId As String, RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction, Optional ByRef IsExecutive As Boolean = False, Optional IsInRegion As Boolean = True) As List(Of RegionUser)
        Dim Results As New List(Of RegionUser)

        Dim CommandText = <SQL>                              
        SELECT
			Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, UPhotoId, UHasPhotoId
        FROM (
		    SELECT
			    Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
		    FROM
			    RegionUser
		    WHERE RegionId = @RegionId AND IsInfoLinked = 0
	    UNION
		    SELECT
			    RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		    FROM
			    RegionUser As RU, [User] AS U
		    WHERE RU.RegionId = @RegionId AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
        ) As _RegionUser ORDER BY Username
                          </SQL>.Value
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Results.Add(New RegionUser(Reader, RegionId))
            End While
            Reader.Close()
        End Using

        Dim Region = RegionProperties.GetRegionPropertiesHelper(RegionId, SQLConnection, SQLTransaction)

        If RealUsername = "superadmin" Then IsExecutive = True

        If Not IsExecutive Then
            For Each RU In Results
                If RU.RealUsername = RealUsername AndAlso RealUsername <> "" Then
                    If RU.IsExecutive Then IsExecutive = True
                End If
            Next
        End If
        
        For Each RU In Results
            RU.ClearPrivateData(RealUsername, Region, Results)
        Next

        Return Results
    End Function

    Public Shared Function LoadAllInRegionIdsHelper(RegionIds As List(Of String), RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionUser)
        Dim Results As New List(Of RegionUser)

        If RegionIds.Count = 0 Then Return Results

        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim CommandText = <SQL>                              
        SELECT
			RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, UPhotoId, UHasPhotoId
        FROM (
		    SELECT
			    RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
		    FROM
			    RegionUser
		    WHERE RegionId IN ({0}) AND IsInfoLinked = 0
	    UNION
		    SELECT
			    RU.RegionId, RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		    FROM
			    RegionUser As RU, [User] AS U
		    WHERE RU.RegionId IN ({0}) AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
        ) As _RegionUser ORDER BY RegionId, Username
                          </SQL>.Value.Replace("{0}", RegionIdParams.ToString())
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim RU As New RegionUser()
                RU.LoadFromSQLReaderWithRegion(Reader)
                Results.Add(RU)
            End While
            Reader.Close()
        End Using

        Dim IsExecutiveRegions As New Dictionary(Of String, Boolean)
        For Each Region In RegionIds
            IsExecutiveRegions.Add(Region, False)
        Next

        Dim MyRegionProperties = RegionProperties.GetAllMyRegionPropertiesHelper(RealUsername, SQLConnection, SQLTransaction)

        For Each RU In Results
            If RU.RealUsername = RealUsername OrElse RealUsername = "superadmin" Then
                If RU.IsExecutive OrElse RealUsername = "superadmin" Then IsExecutiveRegions(RU.RegionId) = True
            End If
        Next

        For Each RU In Results
            If Not IsExecutiveRegions(RU.RegionId) AndAlso RU.RealUsername <> RealUsername Then
                RU.ClearPrivateData(RealUsername, MyRegionProperties.Find(Function(Region) region.RegionId = RU.regionId), Results)
            End If
        Next

        Return Results
    End Function

    Public Shared Function GetAllRealUsersInRegions(RegionIds As List(Of String), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of ChatContact)
        Dim Results As New List(Of ChatContact)

        If RegionIds.Count = 0 Then Return Results


        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim CommandText As String = "SELECT RU.RegionId, RU.RealUsername, U.FirstName, U.LastName, U.PhotoId, U.HasPhotoId, U.Email FROM RegionUser AS RU, [User] AS U WHERE RU.RegionId IN ({0}) AND RU.RealUsername <> '' AND U.Username = RU.RealUsername ORDER BY RU.RealUsername".Replace("{0}", RegionIdParams.ToString())
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Results.Add(New ChatContact With {
                    .RegionIds = {Reader.GetString()}.ToList(),
                    .Username = Reader.GetString(),
                    .FirstName = Reader.GetString(),
                    .LastName = Reader.GetString(),
                    .PhotoId = Reader.GetGuid(),
                    .HasPhotoId = Reader.GetBoolean(),
                    .RequiresInvite = If(Reader.GetString() = "", True, False),
                    .IsBlocked = False,
                    .IsDeleted = False,
                    .FriendSince = New Date(2016, 4, 2),
                    .OnlineStatus = 0
                })
            End While
            Reader.Close()
        End Using

        For I As Integer = Results.Count - 1 To 1 Step -1
            If Results(I).Username = Results(I - 1).Username Then
                If Not Results(I - 1).RegionIds.Contains(Results(I).RegionIds(0)) Then
                    Results(I - 1).RegionIds.Add(Results(I).RegionIds(0))
                End If
                Results.RemoveAt(I)
            End If
        Next

        Return Results
    End Function

    Public Shared Sub CleanRegionUsers(Regions As List(Of RegionProperties), RegionUsers As List(Of RegionUser), RealUsername As String)
        If RealUsername = "superadmin" Then Exit Sub

        Dim RegionIds As New List(Of String)
        For Each RegionUser In RegionUsers
            If Not RegionIds.Contains(RegionUser.RegionId) Then
                RegionIds.Add(RegionUser.RegionId)
            End If
        Next

        Dim IsExecutiveRegions As New Dictionary(Of String, Boolean)
        Dim IsInRegion As New Dictionary(Of String, Boolean)
        For Each Region In RegionIds
            IsExecutiveRegions.Add(Region, False)
            IsInRegion.Add(Region, False)
        Next

        For Each RU In RegionUsers
            If RU.RealUsername = RealUsername AndAlso RealUsername <> "" Then
                IsInRegion(RU.RegionId) = True
                If RU.IsExecutive OrElse RealUsername = "superadmin" Then IsExecutiveRegions(RU.RegionId) = True
            End If
        Next

        For Each RU In RegionUsers
            Dim TRegion = Regions.Find(Function(R) R.RegionId = RU.RegionId)

            RU.ClearPrivateData(RealUsername, TRegion, RegionUsers)
        Next

    End Sub

    Public Shared Function GetAllRegionUsersHelper(SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionUser)
        Dim Results As New List(Of RegionUser)

        Dim CommandText = <SQL>                              
        SELECT
			RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, UPhotoId, UHasPhotoId
        FROM (
		    SELECT
			    RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
		    FROM
			    RegionUser
		    WHERE IsInfoLinked = 0
	    UNION
		    SELECT
			    RU.RegionId, RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		    FROM
			    RegionUser As RU, [User] AS U
		    WHERE RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
        ) As _RegionUser ORDER BY RegionId, Username
                          </SQL>.Value
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim RU As New RegionUser()
                RU.LoadFromSQLReaderWithRegion(Reader)
                Results.Add(RU)
            End While
            Reader.Close()
        End Using

        Return Results
    End Function

    Public Shared Function LoadAllInRegionSimpleHelper(RegionId As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionUser)
        Dim Results As New List(Of RegionUser)

        Dim CommandText = <SQL>
        SELECT
			Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, UPhotoId, UHasPhotoId
        FROM (
		    SELECT
			    Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
		    FROM
			    RegionUser
		    WHERE RegionId = @RegionId AND IsInfoLinked = 0
	    UNION
		    SELECT
			    RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		    FROM
			    RegionUser As RU, [User] AS U
		    WHERE RU.RegionId = @RegionId AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
        ) As _RegionUser ORDER BY Username
        </SQL>.Value

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Results.Add(New RegionUser(Reader, RegionId))
            End While
            Reader.Close()
        End Using


        Return Results
    End Function

    Public Shared Function LoadAllInRegionsSimpleHelper(RegionIds As List(Of String), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionUser)
        Dim Results As New List(Of RegionUser)

        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim CommandText = <SQL>
        SELECT
			RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, UPhotoId, UHasPhotoId
        FROM (
		    SELECT
			    RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
		    FROM
			    RegionUser
		    WHERE RegionId In ({0}) AND IsInfoLinked = 0
	    UNION
		    SELECT
			    RU.RegionId, RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		    FROM
			    RegionUser As RU, [User] AS U
		    WHERE RU.RegionId In ({0}) AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
        ) As _RegionUser ORDER BY Username
        </SQL>.Value.Replace("{0}", RegionIdParams.ToString())

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim TRegionUser As New RegionUser()
                TRegionUser.LoadFromSQLReaderWithRegion(Reader)
                Results.Add(TRegionUser)
            End While
            Reader.Close()
        End Using

        Return Results
    End Function

    Public Shared Function GetAllRegionIdsFromRealUsernames(RegionUsers As List(Of RegionUser), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of String)
        Dim Result As New List(Of String)

        Dim UniqueRealUsernames As New List(Of String)

        For Each RegionUser In RegionUsers
            If RegionUser.RealUsername <> "" AndAlso Not UniqueRealUsernames.Contains(RegionUser.RealUsername) Then
                UniqueRealUsernames.Add(RegionUser.RealUsername)
            End If
        Next

        If UniqueRealUsernames.Count = 0 Then Return Result

        Dim RealUsernamesParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To UniqueRealUsernames.Count
            If I <> 1 Then RealUsernamesParams.Append(", ")
            RealUsernamesParams.Append("@RealUsername" & I)
        Next

        Dim CommandText As String = "SELECT RegionId FROM RegionUser WHERE RealUsername IN ({0})".Replace("{0}", RealUsernamesParams.ToString())

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To UniqueRealUsernames.Count
                SqlCommand.Parameters.Add(New SqlParameter("RealUsername" & I, UniqueRealUsernames(I - 1)))
            Next

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim RegionId = Reader.GetString(0)
                If Not Result.Contains(RegionId) Then Result.Add(RegionId)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetAllRegionIdsImExecutiveFor(RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of String)
        Dim Result As New List(Of String)
        If RealUsername.Trim = "" Then Return Result

        Dim CommandText As String = "SELECT RegionId, Positions FROM RegionUser WHERE RealUsername = @RealUsername AND IsArchived = 0"

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim RU As New RegionUser()
                RU.RegionId = Reader.GetString(0)
                RU.Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(1))

                If RU.IsExecutive OrElse RealUsername = "superadmin" Then
                    If Not Result.Contains(RU.RegionId) Then Result.Add(RU.RegionId)
                End If
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetAllMyRegionsIds(RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of String)
        Dim Result As New List(Of String)
        If RealUsername.Trim() = "" Then Return Result

        Dim CommandText As String = "SELECT RegionId FROM RegionUser WHERE RealUsername = @RealUsername AND IsArchived = 0"

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim RU As New RegionUser()
                RU.RegionId = Reader.GetString(0)
                If Not Result.Contains(RU.RegionId) Then Result.Add(RU.RegionId)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetAllMyRegionsIdsIncludingArchived(RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of String)
        Dim Result As New List(Of String)
        If RealUsername.Trim() = "" Then Return Result

        Dim CommandText As String = "SELECT RegionId FROM RegionUser WHERE RealUsername = @RealUsername"

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim RU As New RegionUser()
                RU.RegionId = Reader.GetString(0)
                If Not Result.Contains(RU.RegionId) Then Result.Add(RU.RegionId)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetAllUsersRegionsIdsIncludingArchived(Usernames As List(Of String), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of String)
        Dim Result As New List(Of String)

        If Usernames.Count = 0 Then Return Result

        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To Usernames.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@Username" & I)
        Next

        Dim CommandText As String = "SELECT DISTINCT RegionId FROM RegionUser WHERE RealUsername IN ({0}) ORDER BY RegionId".Replace("{0}", RegionIdParams.ToString())

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)

            For I As Integer = 1 To Usernames.Count
                SqlCommand.Parameters.Add(New SqlParameter("Username" & I, Usernames(I - 1)))
            Next

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim RU As New RegionUser()
                RU.RegionId = Reader.GetString(0)
                If Not Result.Contains(RU.RegionId) Then Result.Add(RU.RegionId)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetAllMyRegionsIdExecs(RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of String)
        Dim Result As New List(Of String)

        Dim CommandText As String = "SELECT RegionId, Positions FROM RegionUser WHERE RealUsername = @RealUsername AND IsArchived = 0"

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim RU As New RegionUser()
                RU.RegionId = Reader.GetString(0)
                RU.Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(1))
                If (RU.IsExecutive OrElse RealUsername = "superadmin") AndAlso Not Result.Contains(RU.RegionId) Then Result.Add(RU.RegionId)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function


    Public Shared Function LoadAllInMyRegionsHelper(RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionUser)
        Dim Results As New List(Of RegionUser)
        If RealUsername = "" Then Return Results

        Dim CommandText = <SQL>
		SELECT
			RegionId, Username, IsLinked, AllowInfoLink, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Positions, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId, PhotoId, HasPhotoId
		FROM
			RegionUser
		WHERE RegionId IN (SELECT DISTINCT RegionId FROM RegionUser WHERE RealUsername = @RealUsername) AND IsInfoLinked = 0
                          </SQL>.Value
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim RU As New RegionUser()
                RU.LoadFromSQLReaderWithRegion(Reader)
                Results.Add(RU)
            End While
            Reader.Close()
        End Using


        CommandText = <SQL>
		SELECT
			RU.RegionId, RU.Username, RU.IsLinked, RU.AllowInfoLink, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.PhoneNumbers, U.AlternateEmails, U.Country, U.State, U.City, U.Address, U.PostalCode, U.PreferredLanguage, RU.Positions, RU.Rank, RU.RankNumber, RU.IsActive, RU.CanViewAvailability, RU.CanViewMasterSchedule, RU.CanViewSupervisors, RU.PublicData, RU.PrivateData, RU.GlobalAvailabilityData, RU.RankAndDates, RU.InternalData, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		FROM
			RegionUser As RU, [User] AS U
		WHERE RU.RegionId In (SELECT DISTINCT RegionId FROM RegionUser WHERE RealUsername = @RealUsername) AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
         </SQL>.Value

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim RU As New RegionUser()
                RU.LoadFromSQLReaderWithRegion(Reader)
                Results.Add(RU)
            End While
            Reader.Close()
        End Using

        Results.Sort(New GenericIComparer(Of RegionUser)(Function(V1, V2)
                                                             Dim Comp As Integer = V1.RegionId.CompareTo(V2.RegionId)
                                                             If Comp = 0 Then Comp = V1.Username.CompareTo(V2.Username)
                                                             Return Comp
                                                         End Function))

        Return Results
    End Function

    Public Shared Function GeExecutivesShortFromRegionIdsHelper(RegionIds As List(Of String), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionUser)
        Dim Result As New List(Of RegionUser)

        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim CommandText = <SQL>
SELECT
	RegionId, Username, RealUsername,FirstName, LastName, Email, AlternateEmails, PreferredLanguage, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
FROM (
		SELECT
			RegionId, Username, RealUsername, FirstName, LastName, Email, AlternateEmails, PreferredLanguage, StatLinks, PhotoId, HasPhotoId, PhotoId AS UPhotoId, HasPhotoId AS UHasPhotoId
		FROM
			RegionUser
		WHERE RegionId IN ({0}) AND IsExecutive = 1 AND IsInfoLinked = 0
	UNION
		SELECT
			RU.RegionId, RU.Username, RU.RealUsername, RU.FirstName, RU.LastName, U.Email, U.AlternateEmails, U.PreferredLanguage, RU.StatLinks, RU.PhotoId, RU.HasPhotoId, U.PhotoId, U.HasPhotoId
		FROM
			RegionUser As RU, [User] AS U
		WHERE RU.RegionId IN ({0}) AND RU.IsExecutive = 1 AND RU.RealUsername = U.Username AND RU.IsInfoLinked = 1
) As _RegionUser
         </SQL>.Value.Replace("{0}", RegionIdParams.ToString())

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim TRegionUser = New RegionUser With {
                    .RegionId = Reader.GetString(0),
                    .Username = Reader.GetString(1),
                    .RealUsername = Reader.GetString(2),
                    .FirstName = Reader.GetString(3),
                    .LastName = Reader.GetString(4),
                    .Email = Reader.GetString(5),
                    .AlternateEmails = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(6)),
                    .PreferredLanguage = Reader.GetString(7),
                    .StatLinks = Reader.GetString(8),
                    .PhotoId = Reader.GetGuid(9),
                    .HasPhotoId = Reader.GetBoolean(10)
                }

                Dim PhotoId = Reader.GetGuid(11)
                Dim HasPhotoId = Reader.GetBoolean(12)

                If Not TRegionUser.HasPhotoId AndAlso HasPhotoId Then
                    TRegionUser.HasPhotoId = HasPhotoId
                    TRegionUser.PhotoId = PhotoId
                End If

                Result.Add(TRegionUser)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function UploadToTemp(AssignorId As String, RegionId As String, PositionId As String, RegionUsers As List(Of RegionUser)) As Object
        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim Region = UmpireAssignor.Region.GetRegionHelper(RegionId, SqlConnection, SqlTransaction)

                    If Not IsValidPosition(Region, PositionId) Then Return New ErrorObject("InvalidPosition")

                    Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                    If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                        Return New ErrorObject("InvalidPermissions")
                    End If

                    Dim CommandText As String = "DELETE FROM RegionUserTempSubmitted WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId AND UserSubmitPosition = @PositionId"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                        SqlCommand.Parameters.Add(New SqlParameter("PositionId", PositionId))
                        SqlCommand.ExecuteNonQuery()
                    End Using

                    CommandText = "INSERT INTO RegionUserTempSubmitted (RegionId, UserSubmitId, UserSubmitPosition, IsSubmitted) VALUES (@RegionId, @AssignorId, @PositionId, 1)"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                        SqlCommand.Parameters.Add(New SqlParameter("PositionId", PositionId))
                        SqlCommand.ExecuteNonQuery()
                    End Using

                    CommandText = "DELETE FROM RegionUserTemp WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId AND UserSubmitPosition = @PositionId"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                        SqlCommand.Parameters.Add(New SqlParameter("PositionId", PositionId))
                        SqlCommand.ExecuteNonQuery()
                    End Using


                    CommandText = <SQL>
                                    INSERT INTO
	                                    RegionUserTemp
                                    (RegionId, UserSubmitId, UserSubmitPosition, Username, AllowInfoLink, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData)
	                                    VALUES
                                    (@RegionId, @AssignorId, @PositionId, @Username, @AllowInfoLink, @FirstName, @LastName, @Email, @PhoneNumbers, @AlternateEmails, @Country, @State, @City, @Address, @PostalCode, @PreferredLanguage, @Rank, @RankNumber, @IsActive, @CanViewAvailability, @CanViewMasterSchedule, @CanViewSupervisors, @PublicData, @PrivateData, @GlobalAvailabilityData, @RankAndDates, @InternalData)
                                  </SQL>.Value()

                    For Each RegionUser In RegionUsers
                        Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                            SqlCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                            SqlCommand.Parameters.Add(New SqlParameter("PositionId", PositionId))
                            SqlCommand.Parameters.Add(New SqlParameter("Username", RegionUser.Username))
                            SqlCommand.Parameters.Add(New SqlParameter("AllowInfoLink", RegionUser.AllowInfoLink))
                            SqlCommand.Parameters.Add(New SqlParameter("FirstName", RegionUser.FirstName))
                            SqlCommand.Parameters.Add(New SqlParameter("LastName", RegionUser.LastName))
                            SqlCommand.Parameters.Add(New SqlParameter("Email", RegionUser.Email))
                            SqlCommand.Parameters.Add(New SqlParameter("PhoneNumbers", JsonConvert.SerializeObject(RegionUser.PhoneNumbers)))
                            SqlCommand.Parameters.Add(New SqlParameter("AlternateEmails", JsonConvert.SerializeObject(RegionUser.AlternateEmails)))
                            SqlCommand.Parameters.Add(New SqlParameter("Country", RegionUser.Country))
                            SqlCommand.Parameters.Add(New SqlParameter("State", RegionUser.State))
                            SqlCommand.Parameters.Add(New SqlParameter("City", RegionUser.City))
                            SqlCommand.Parameters.Add(New SqlParameter("Address", RegionUser.Address))
                            SqlCommand.Parameters.Add(New SqlParameter("PostalCode", RegionUser.PostalCode))
                            SqlCommand.Parameters.Add(New SqlParameter("PreferredLanguage", RegionUser.PreferredLanguage))
                            SqlCommand.Parameters.Add(New SqlParameter("Rank", If(PositionId = "official" OrElse PositionId = "scorekeeper" OrElse PositionId = "supervisor", JsonConvert.SerializeObject(RegionUser.Rank), "")))
                            SqlCommand.Parameters.Add(New SqlParameter("RankNumber", If(PositionId = "official" OrElse PositionId = "scorekeeper" OrElse PositionId = "supervisor", JsonConvert.SerializeObject(RegionUser.RankNumber), "")))
                            SqlCommand.Parameters.Add(New SqlParameter("IsActive", RegionUser.IsActive))
                            SqlCommand.Parameters.Add(New SqlParameter("CanViewAvailability", RegionUser.CanViewAvailability))
                            SqlCommand.Parameters.Add(New SqlParameter("CanViewMasterSchedule", RegionUser.CanViewMasterSchedule))
                            SqlCommand.Parameters.Add(New SqlParameter("CanViewSupervisors", RegionUser.CanViewSupervisors))
                            SqlCommand.Parameters.Add(New SqlParameter("PublicData", RegionUser.PublicData))
                            SqlCommand.Parameters.Add(New SqlParameter("PrivateData", RegionUser.PrivateData))
                            SqlCommand.Parameters.Add(New SqlParameter("GlobalAvailabilityData", RegionUser.GlobalAvailabilityData))
                            SqlCommand.Parameters.Add(New SqlParameter("RankAndDates", JsonConvert.SerializeObject(RegionUser.RankAndDates)))
                            SqlCommand.Parameters.Add(New SqlParameter("InternalData", RegionUser.InternalData))
                            SqlCommand.ExecuteNonQuery()
                        End Using
                    Next


                    SqlTransaction.Commit()
                End Using
            End Using
            Return New With {.Success = True}
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function SaveTemp(AssignorId As String, RegionId As String, PositionId As String) As Object
        Dim EmailList As New List(Of EmailTest)

        Dim Region As Region

        Dim RealUsernamesBefore As New List(Of String)
        Dim RealUsernamesAfter As New List(Of String)

        Dim NewRegionUsers As New List(Of RegionUser)
        Dim OldRegionUsers As New List(Of RegionUser)

        Dim ScheduleItems As New Dictionary(Of String, List(Of Schedule))
        Dim GamesWhosScoresNeedUpdating As New Dictionary(Of String, Dictionary(Of Integer, Schedule))

        Dim RegionUsers As new List(Of RegionUser)

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Region = UmpireAssignor.Region.GetRegionHelper(RegionId, SqlConnection, SqlTransaction)

                    If Not IsValidPosition(Region, PositionId) Then Return New ErrorObject("InvalidPosition")

                    Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                    If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                        Return New ErrorObject("InvalidPermissions")
                    End If

                    Dim IsSubmitted As Boolean = False
                    Dim CommandText As String = "SELECT IsSubmitted FROM RegionUserTempSubmitted WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId AND UserSubmitPosition = @PositionId"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                        SqlCommand.Parameters.Add(New SqlParameter("PositionId", PositionId))

                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            IsSubmitted = Reader.GetBoolean(0)
                        End While
                        Reader.Close()
                    End Using

                    If Not IsSubmitted Then
                        Return New ErrorObject("NoCSVFile")
                    End If

                    Dim TempRegionUsers As New List(Of RegionUser)

                    CommandText = "SELECT Username, AllowInfoLink, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Rank, RankNumber, IsActive, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData FROM RegionUserTemp WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId AND UserSubmitPosition = @PositionId"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                        SqlCommand.Parameters.Add(New SqlParameter("PositionId", PositionId))

                        Dim Reader = SqlCommand.ExecuteReader
                        While Reader.Read
                            TempRegionUsers.Add(New RegionUser With {
                                                .Username = Reader.GetString(0).ToLower,
                                                .AllowInfoLink = Reader.GetBoolean(1),
                                                .FirstName = Reader.GetString(2),
                                                .LastName = Reader.GetString(3),
                                                .Email = Reader.GetString(4).ToLower,
                                                .PhoneNumbers = JsonConvert.DeserializeObject(Of List(Of PhoneNumber))(Reader.GetString(5)),
                                                .AlternateEmails = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(6).ToLower),
                                                .Country = Reader.GetString(7),
                                                .State = Reader.GetString(8),
                                                .City = Reader.GetString(9),
                                                .Address = Reader.GetString(10),
                                                .PostalCode = Reader.GetString(11),
                                                .PreferredLanguage = Reader.GetString(12).ToLower,
                                                .Rank = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString(13).ToLower),
                                                .RankNumber = JsonConvert.DeserializeObject(Of Dictionary(Of String, Decimal))(Reader.GetString(14).ToLower),
                                                .IsActive = Reader.GetBoolean(15),
                                                .CanViewAvailability = Reader.GetBoolean(16),
                                                .CanViewMasterSchedule = Reader.GetBoolean(17),
                                                .CanViewSupervisors = Reader.GetBoolean(18),
                                                .PublicData = Reader.GetString(19),
                                                .PrivateData = Reader.GetString(20),
                                                .GlobalAvailabilityData = Reader.GetString(21),
                                                .RankAndDates = JsonConvert.DeserializeObject(Of List(Of RankAndDate))(Reader.GetString(22)),
                                                .InternalData = Reader.GetString(23)
                                            })
                        End While
                        Reader.Close()
                    End Using

                    RegionUsers = UmpireAssignor.RegionUser.LoadAllInRegionHelper(RegionId, AssignorId, SqlConnection, SqlTransaction)


                    TempRegionUsers.Sort(New GenericIComparer(Of RegionUser)(Function(V1, V2) V1.Username.ToLower.CompareTo(V2.Username)))
                    RegionUsers.Sort(New GenericIComparer(Of RegionUser)(Function(V1, V2) V1.Username.CompareTo(V2.Username)))

                    Dim TriedToDeleteAdmin As Boolean = False

                    For Each RegionUser In RegionUsers
                        If RegionUser.RealUsername <> "" AndAlso Not RealUsernamesBefore.Contains(RegionUser.RealUsername) Then
                            RealUsernamesBefore.Add(RegionUser.RealUsername)
                        End If
                    Next

                    For Each RegionUser In TempRegionUsers
                        If RegionUser.RealUsername <> "" AndAlso Not RealUsernamesAfter.Contains(RegionUser.RealUsername) Then
                            RealUsernamesAfter.Add(RegionUser.RealUsername)
                        End If
                    Next

                    OldRegionUsers = RegionUsers

                    PublicCode.ProcessListPairs(RegionUsers, TempRegionUsers, Function(V1, V2) V1.Username.CompareTo(V2.Username),
                                                Sub(RU)
                                                    Try
                                                        If Not DeleteHelper(AssignorId, Region, PositionId, RU, ScheduleItems, GamesWhosScoresNeedUpdating, SqlConnection, SqlTransaction).Success Then TriedToDeleteAdmin = True
                                                    Catch ex As Exception
                                                        PublicCode.SendEmailStandard("Crash", "Cant delete user " & RU.Username, ex.StackTrace, True, False)
                                                    End Try
                                                End Sub,
                                                Sub(TRU)
                                                    Try
                                                        UpsertHelper(Region, PositionId, TRU, Nothing, ScheduleItems, GamesWhosScoresNeedUpdating, EmailList, SqlConnection, SqlTransaction)
                                                    Catch ex As Exception
                                                        PublicCode.SendEmailStandard("Crash", "Cant insert user " & TRU.Username, ex.StackTrace, True, False)
                                                    End Try
                                                End Sub,
                                                Sub(RU, TRU)
                                                    TRU.PhotoId = RU.PhotoId
                                                    TRU.HasPhotoId = RU.HasPhotoId
                                                    TRU.StatLinks = RU.StatLinks
                                                    Try
                                                        UpsertHelper(Region, PositionId, TRU, RU, ScheduleItems, GamesWhosScoresNeedUpdating, EmailList, SqlConnection, SqlTransaction)
                                                    Catch ex As Exception
                                                        PublicCode.SendEmailStandard("Crash", "Cant upsert user " & TRU.Username, ex.StackTrace, True, False)
                                                    End Try
                                                End Sub)

                    If TriedToDeleteAdmin Then Return New ErrorObject("UnAdmin")


                    CommandText = "DELETE FROM RegionUserTempSubmitted WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId AND UserSubmitPosition = @PositionId"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                        SqlCommand.Parameters.Add(New SqlParameter("PositionId", PositionId))
                        SqlCommand.ExecuteNonQuery()
                    End Using

                    CommandText = "DELETE FROM RegionUserTemp WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId AND UserSubmitPosition = @PositionId"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                        SqlCommand.Parameters.Add(New SqlParameter("PositionId", PositionId))
                        SqlCommand.ExecuteNonQuery()
                    End Using

                    NewRegionUsers = UmpireAssignor.RegionUser.LoadAllInRegionHelper(RegionId, AssignorId, SqlConnection, SqlTransaction)

                    If Region.RegionIsLadderLeague Then
                        Dim ScheduleItemsTotal = LadderLeague.ScheduleItemsConversion(ScheduleItems)

                        Dim OldItemsClone As New List(Of Schedule)
                        For Each Item In ScheduleItemsTotal
                            OldItemsClone.Add(Item.CloneItem)
                        Next

                        LadderLeague.UpdateGameRanks(
                            RegionUsers,
                            OldItemsClone,
                            ScheduleItemsTotal,
                            LadderLeague.SortedGamesWhosScoresNeedUpdatingConversion(GamesWhosScoresNeedUpdating),
                            New List(Of GameEmail),
                            SqlConnection,
                            SqlTransaction)
                    End If

                    If Region IsNot Nothing AndAlso Not Region.IsDemo Then
                        For Each EmailListItem In EmailList
                            Try
                                PublicCode.SendEmailStandard(EmailListItem.Email, EmailListItem.Subject, EmailListItem.Body, True, True, False, SqlConnection, SqlTransaction)
                            Catch
                            End Try
                        Next
                    End If

                    SqlTransaction.Commit()
                End Using
            End Using



            Dim OldRegionUsernames As New List(Of String)
            Dim NewRegionUsernames As New List(Of String)

            For Each RU In OldRegionUsers
                If RU.RealUsername <> "" AndAlso Not OldRegionUsernames.Contains(RU.RealUsername) Then
                    OldRegionUsernames.Add(RU.RealUsername)
                End If
            Next

            For Each RU In NewRegionUsers
                If RU.RealUsername <> "" AndAlso Not NewRegionUsernames.Contains(RU.RealUsername) Then
                    NewRegionUsernames.Add(RU.RealUsername)
                End If
            Next

            OldRegionUsernames.Sort()
            NewRegionUsernames.Sort()

            Dim RemovedRegionUsernames As New List(Of String)

            PublicCode.ProcessListPairs(OldRegionUsernames, NewRegionUsernames, Function(RU1, RU2) RU1.CompareTo(RU2),
                                        Sub(RU1)
                                            RemovedRegionUsernames.Add(RU1)
                                        End Sub,
                                        Sub(RU2)
                                        End Sub,
                                        Sub(RU1, RU2)
                                        End Sub)

            Dim Context = GlobalHost.ConnectionManager.GetHubContext(Of WsHandler)()

            For Each TUsername In NewRegionUsernames
                If WsHandler.UserConnectionIds.ContainsKey(TUsername) Then
                    For Each ConnectionId In WsHandler.UserConnectionIds(TUsername)
                        Context.Clients.Client(ConnectionId).removeContacts(RemovedRegionUsernames, RegionId)
                    Next
                End If
            Next

            For Each TUsername In RemovedRegionUsernames
                If WsHandler.UserConnectionIds.ContainsKey(TUsername) Then
                    For Each ConnectionId In WsHandler.UserConnectionIds(TUsername)
                        Context.Clients.Client(ConnectionId).removeContacts(NewRegionUsernames, RegionId)
                    Next
                End If
            Next

            Return New With {.Success = True}
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try

    End Function

    Public Shared Function CancelTemp(AssignorId As String, RegionId As String, PositionId As String) As Object
        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                    If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                        Return New ErrorObject("InvalidPermissions")
                    End If

                    Dim CommandText As String = "DELETE FROM RegionUserTempSubmitted WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId AND UserSubmitPosition = @PositionId"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                        SqlCommand.Parameters.Add(New SqlParameter("PositionId", PositionId))
                        SqlCommand.ExecuteNonQuery()
                    End Using

                    CommandText = "DELETE FROM RegionUserTemp WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId AND UserSubmitPosition = @PositionId"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                        SqlCommand.Parameters.Add(New SqlParameter("PositionId", PositionId))
                        SqlCommand.ExecuteNonQuery()
                    End Using


                    SqlTransaction.Commit()
                End Using
            End Using
            Return New With {.Success = True}
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function ArchiveUser(RealUsername As String, ArchiveRegion As ArchiveRegion) As Object
        RealUsername = RealUsername.ToLower
        Dim EmailList As New List(Of EmailTest)

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(ArchiveRegion.RegionId, RealUsername, SqlConnection, SqlTransaction)
                    If Assignor Is Nothing Then
                        Return New ErrorObject("InvalidPermissions")
                    End If

                    Dim CommandText As String = "UPDATE RegionUser Set IsArchived = @IsArchived WHERE RegionId = @RegionId AND RealUsername = @RealUsername"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", ArchiveRegion.RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))
                        SqlCommand.Parameters.Add(New SqlParameter("IsArchived", ArchiveRegion.IsArchived))
                        SqlCommand.ExecuteNonQuery()
                    End Using

                    SqlTransaction.Commit()
                End Using
            End Using

            Return New With {.Success = True}
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function Upsert(AssignorId As String, RegionId As String, PositionId As String, RegionUser As RegionUser, Optional SendToNewSite As Boolean = False) As Object
        AssignorId = AssignorId.ToLower
        Dim EmailList As New List(Of EmailTest)

        PositionId = PositionId.ToLower()
        RegionUser.Email = RegionUser.Email.ToLower
        RegionUser.Username = RegionUser.Username.ToLower
        For I As Integer = 0 To RegionUser.AlternateEmails.Count - 1
            RegionUser.AlternateEmails(I) = RegionUser.AlternateEmails(I).ToLower
        Next


        'If Not PublicCode.IsAlphaNum(RegionUser.Username) Then Return New ErrorObject("UsernameNotAlphaNumeric")
        If RegionUser.Rank IsNot Nothing AndAlso RegionUser.Rank.ContainsKey(PositionId) AndAlso Not RankCode.Contains(RegionUser.Rank(PositionId)) Then Return New ErrorObject("InvalidRegionUser")
        If RegionUser.RankNumber IsNot Nothing AndAlso RegionUser.RankNumber.ContainsKey(PositionId) AndAlso RegionUser.RankNumber(PositionId) < 0 Then Return New ErrorObject("InvalidRegionUser")

        Dim ScheduleItems As New Dictionary(Of String, List(Of Schedule))
        Dim GamesWhosScoresNeedUpdating As New Dictionary(Of String, Dictionary(Of Integer, Schedule))
        Dim RegionUsers As New List(Of RegionUser)

        Dim Region As Region = Nothing

        'Try
        If RegionUser.PhoneNumbers Is Nothing Then Return New ErrorObject("MissingPhoneNumbers")
        If RegionUser.AlternateEmails Is Nothing Then Return New ErrorObject("MissingAlternateEmails")

        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Region = UmpireAssignor.Region.GetRegionHelper(RegionId, SqlConnection, SqlTransaction)

                If Not IsValidPosition(Region, PositionId) Then Return New ErrorObject("InvalidPosition")

                Dim OldRegionUser = LoadBasic(RegionUser.RegionId, RegionUser.Username, SqlConnection, SqlTransaction)

                Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                    Return New ErrorObject("InvalidPermissions")
                End If

                UpsertHelper(Region, PositionId, RegionUser, OldRegionUser, ScheduleItems, GamesWhosScoresNeedUpdating, EmailList, SqlConnection, SqlTransaction, SendToNewSite)

                RegionUsers = LoadAllInRegionSimpleHelper(RegionId, SqlConnection, SqlTransaction)

                If Region.RegionIsLadderLeague Then
                    Dim ScheduleItemsTotal = LadderLeague.ScheduleItemsConversion(ScheduleItems)

                    ScheduleItemsTotal = ScheduleItemsTotal.FindAll(Function(S) S.GameDate > New Date(2023, 4, 1))

                    Dim OldItemsClone As New List(Of Schedule)
                    For Each Item In ScheduleItemsTotal
                        OldItemsClone.Add(Item.CloneItem)
                    Next

                    LadderLeague.UpdateGameRanks(
                            RegionUsers,
                            OldItemsClone,
                            ScheduleItemsTotal,
                            LadderLeague.SortedGamesWhosScoresNeedUpdatingConversion(GamesWhosScoresNeedUpdating),
                            New List(Of GameEmail),
                            SqlConnection,
                            SqlTransaction)
                End If

                SqlTransaction.Commit()
            End Using
        End Using

        If Region IsNot Nothing AndAlso Not Region.IsDemo Then
            For Each EmailTest In EmailList
                Try
                    PublicCode.SendEmailStandard(EmailTest.Email, EmailTest.Subject, EmailTest.Body, EmailTest.IsHTML)
                Catch
                End Try
            Next
        End If

        Return New With {.Success = True}
        'Catch ex As Exception
        '    Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Shared Sub ClearAllInRegionHelper(RegionId As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        Using SqlCommand As New SqlCommand("DELETE FROM RegionUser WHERE RegionId = @RegionId", SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.ExecuteNonQuery()
        End Using
    End Sub

    Public Shared Sub UpsertSimpleHelper(RegionUser As RegionUser, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction, Optional CommandText As String = "")
        If CommandText = "" Then
            CommandText = <SQL>
                            DELETE FROM RegionUser WHERE RegionId = @RegionId AND Username = @Username; INSERT INTO RegionUser 
	                            (RegionId, Username, IsLinked, AllowInfoLink, IsInfoLinked, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Rank, RankNumber, Positions, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, IsExecutive, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId)
                            VALUES
	                            (@RegionId, @Username, @IsLinked, @AllowInfoLink, @IsInfoLinked, @RealUsername, @FirstName, @LastName, @Email, @PhoneNumbers, @AlternateEmails, @Country, @State, @City, @Address, @PostalCode, @PreferredLanguage, @Rank, @RankNumber, @Positions, @CanViewAvailability, @CanViewMasterSchedule, @CanViewSupervisors, @IsExecutive, @PublicData, @PrivateData, @GlobalAvailabilityData, @RankAndDates, @InternalData, @StatLinks, @PhotoId, @HasPhotoId)
                        </SQL>.Value()
        End If

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionUser.RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("Username", RegionUser.Username))
            SqlCommand.Parameters.Add(New SqlParameter("IsLinked", RegionUser.IsLinked))
            SqlCommand.Parameters.Add(New SqlParameter("AllowInfoLink", RegionUser.AllowInfoLink))
            SqlCommand.Parameters.Add(New SqlParameter("IsInfoLinked", RegionUser.IsLinked And RegionUser.AllowInfoLink))
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RegionUser.RealUsername))
            SqlCommand.Parameters.Add(New SqlParameter("FirstName", RegionUser.FirstName))
            SqlCommand.Parameters.Add(New SqlParameter("LastName", RegionUser.LastName))
            SqlCommand.Parameters.Add(New SqlParameter("Email", RegionUser.Email))
            SqlCommand.Parameters.Add(New SqlParameter("PhoneNumbers", JsonConvert.SerializeObject(RegionUser.PhoneNumbers)))
            SqlCommand.Parameters.Add(New SqlParameter("AlternateEmails", JsonConvert.SerializeObject(RegionUser.AlternateEmails)))
            SqlCommand.Parameters.Add(New SqlParameter("Country", RegionUser.Country))
            SqlCommand.Parameters.Add(New SqlParameter("State", RegionUser.State))
            SqlCommand.Parameters.Add(New SqlParameter("City", RegionUser.City))
            SqlCommand.Parameters.Add(New SqlParameter("Address", RegionUser.Address))
            SqlCommand.Parameters.Add(New SqlParameter("PostalCode", RegionUser.PostalCode))
            SqlCommand.Parameters.Add(New SqlParameter("PreferredLanguage", RegionUser.PreferredLanguage))
            SqlCommand.Parameters.Add(New SqlParameter("Positions", JsonConvert.SerializeObject(RegionUser.Positions)))
            SqlCommand.Parameters.Add(New SqlParameter("Rank", JsonConvert.SerializeObject(RegionUser.Rank)))
            SqlCommand.Parameters.Add(New SqlParameter("RankNumber", JsonConvert.SerializeObject(RegionUser.RankNumber)))
            SqlCommand.Parameters.Add(New SqlParameter("IsActive", RegionUser.IsActive))
            SqlCommand.Parameters.Add(New SqlParameter("CanViewAvailability", RegionUser.CanViewAvailability))
            SqlCommand.Parameters.Add(New SqlParameter("CanViewMasterSchedule", RegionUser.CanViewMasterSchedule))
            SqlCommand.Parameters.Add(New SqlParameter("CanViewSupervisors", RegionUser.CanViewSupervisors))
            SqlCommand.Parameters.Add(New SqlParameter("IsExecutive", RegionUser.IsExecutive))
            SqlCommand.Parameters.Add(New SqlParameter("PublicData", RegionUser.PublicData))
            SqlCommand.Parameters.Add(New SqlParameter("PrivateData", RegionUser.PrivateData))
            SqlCommand.Parameters.Add(New SqlParameter("GlobalAvailabilityData", RegionUser.GlobalAvailabilityData))
            SqlCommand.Parameters.Add(New SqlParameter("RankAndDates", JsonConvert.SerializeObject(RegionUser.RankAndDates)))
            SqlCommand.Parameters.Add(New SqlParameter("InternalData", RegionUser.InternalData))
            SqlCommand.Parameters.Add(New SqlParameter("StatLinks", RegionUser.StatLinks))
            SqlCommand.Parameters.Add(New SqlParameter("PhotoId", RegionUser.PhotoId))
            SqlCommand.Parameters.Add(New SqlParameter("HasPhotoId", RegionUser.HasPhotoId))
            SqlCommand.ExecuteNonQuery()
        End Using
    End Sub

    Public Shared Sub UpsertHelper(Region As Region, PositionId As String, RegionUser As RegionUser, OldRegionUser As RegionUser, ScheduleItems As Dictionary(Of String, List(Of Schedule)), GamesWhosScoresNeedUpdating As Dictionary(Of String, Dictionary(Of Integer, Schedule)), EmailList As List(Of EmailTest), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction, Optional SendToNewSite As Boolean = False)
        Dim RegionId = Region.RegionId.ToLower
        PositionId = PositionId.ToLower

        If RegionUser.RankAndDates IsNot Nothing Then
            RegionUser.RankAndDates.Sort(RankAndDate.Sorter)
        End If

        Dim ShouldSendEmail As Boolean = False
        Dim UserFromEmail As User = Nothing

        Dim CommandText As String = ""


        Dim Official = UmpireAssignor.RegionUser.LoadBasic(RegionId, RegionUser.Username, SQLConnection, SQLTransaction)

        If Official Is Nothing Then
            CommandText =
<SQL>
    INSERT INTO RegionUser 
	    (RegionId, Username, IsLinked, AllowInfoLink, IsInfoLinked, RealUsername, FirstName, LastName, Email, PhoneNumbers, AlternateEmails, Country, State, City, Address, PostalCode, PreferredLanguage, Rank, RankNumber, Positions, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors, IsExecutive, PublicData, PrivateData, GlobalAvailabilityData, RankAndDates, InternalData, StatLinks, PhotoId, HasPhotoId)
    VALUES
	    (@RegionId, @Username, @IsLinked, @AllowInfoLink, @IsInfoLinked, @RealUsername, @FirstName, @LastName, @Email, @PhoneNumbers, @AlternateEmails, @Country, @State, @City, @Address, @PostalCode, @PreferredLanguage, @Rank, @RankNumber, @Positions, @CanViewAvailability, @CanViewMasterSchedule, @CanViewSupervisors, @IsExecutive, @PublicData, @PrivateData, @GlobalAvailabilityData, @RankAndDates, @InternalData, @StatLinks, @PhotoId, @HasPhotoId)
</SQL>.Value()

            RegionUser.Positions = {PositionId}.ToList()
            RegionUser.RealUsername = ""
            RegionUser.IsLinked = False
        Else
            CommandText = <SQL>
                                UPDATE RegionUser SET
	                                IsLInked = @IsLinked,
	                                AllowInfoLink = @AllowInfoLink,
	                                IsInfoLinked = @IsInfoLinked, 
	                                RealUsername = @RealUsername, 
	                                FirstName = @FirstName, 
	                                LastName = @LastName, 
	                                Email = @Email, 
	                                PhoneNumbers = @PhoneNumbers, 
	                                AlternateEmails = @AlternateEmails, 
	                                Country = @Country, 
	                                State = @State, 
	                                City = @City, 
	                                Address = @Address, 
	                                PostalCode = @PostalCode, 
	                                PreferredLanguage = @PreferredLanguage, 
	                                Rank = @Rank, 
                                    RankNumber = @RankNumber,
	                                Positions = @Positions,
                                    IsActive = @IsActive, 
                                    CanViewAvailability = @CanViewAvailability, 
                                    CanViewMasterSchedule = @CanViewMasterSchedule,
                                    CanViewSupervisors = @CanViewSupervisors,
                                    IsExecutive = @IsExecutive,
                                    PublicData = @PublicData,
                                    PrivateData = @PrivateData,
                                    GlobalAvailabilityData = @GlobalAvailabilityData,
                                    RankAndDates = @RankAndDates,
                                    InternalData = @InternalData,
                                    StatLinks = @StatLinks,
                                    PhotoId = @PhotoId,
                                    HasPhotoId = @HasPhotoId
                                WHERE
	                                RegionID = @RegionId AND Username = @Username
                              </SQL>

            RegionUser.StatLinks = Official.StatLinks

            If Not Official.Positions.Contains(PositionId) Then
                Official.Positions.Add(PositionId)
                'This is for the case when a user is already a chief and wants to also be an ump.
                RegionUser.FirstName = If(RegionUser.FirstName = "", Official.FirstName, RegionUser.FirstName)
                RegionUser.LastName = If(RegionUser.LastName = "", Official.LastName, RegionUser.LastName)
                RegionUser.Email = If(RegionUser.Email = "", Official.Email, RegionUser.Email)
                RegionUser.PhoneNumbers = If(RegionUser.PhoneNumbers.Count = 0, Official.PhoneNumbers, RegionUser.PhoneNumbers)
                RegionUser.AlternateEmails = If(RegionUser.AlternateEmails.Count = 0, Official.AlternateEmails, RegionUser.AlternateEmails)
                RegionUser.Country = If(RegionUser.Country = "", Official.Country, RegionUser.Country)
                RegionUser.State = If(RegionUser.State = "", Official.State, RegionUser.State)
                RegionUser.City = If(RegionUser.City = "", Official.City, RegionUser.City)
                RegionUser.Address = If(RegionUser.Address = "", Official.Address, RegionUser.Address)
                RegionUser.PostalCode = If(RegionUser.PostalCode = "", Official.PostalCode, RegionUser.PostalCode)
                RegionUser.PreferredLanguage = If(RegionUser.PreferredLanguage = "", Official.PreferredLanguage, RegionUser.PreferredLanguage)
                RegionUser.CanViewAvailability = If(RegionUser.Email = "", Official.CanViewAvailability, RegionUser.CanViewAvailability)
                RegionUser.CanViewMasterSchedule = If(RegionUser.Email = "", Official.CanViewMasterSchedule, RegionUser.CanViewMasterSchedule)
                RegionUser.CanViewSupervisors = If(RegionUser.Email = "", Official.CanViewSupervisors, RegionUser.CanViewSupervisors)
                RegionUser.PublicData = If(RegionUser.Email = "", Official.PublicData, RegionUser.PublicData)
                RegionUser.PrivateData = If(RegionUser.Email = "", Official.PrivateData, RegionUser.PrivateData)
                RegionUser.GlobalAvailabilityData = If(RegionUser.Email = "", Official.GlobalAvailabilityData, RegionUser.GlobalAvailabilityData)
                RegionUser.RankAndDates = If(RegionUser.Email = "", Official.RankAndDates, RegionUser.RankAndDates)
                RegionUser.InternalData = If(RegionUser.Email = "", Official.InternalData, RegionUser.InternalData)
                RegionUser.PhotoId = If(RegionUser.Email = "", Official.PhotoId, RegionUser.PhotoId)
                RegionUser.HasPhotoId = If(RegionUser.Email = "", Official.HasPhotoId, RegionUser.HasPhotoId)

                If Official.IsLinked AndAlso Official.AllowInfoLink Then
                    RegionUser.PhoneNumbers = Official.PhoneNumbers
                    RegionUser.AlternateEmails = Official.AlternateEmails
                    RegionUser.Country = Official.Country
                    RegionUser.State = Official.State
                    RegionUser.City = Official.City
                    RegionUser.Address = Official.Address
                    RegionUser.PostalCode = Official.PostalCode
                    RegionUser.PreferredLanguage = Official.PreferredLanguage
                End If
            End If

            RegionUser.Positions = Official.Positions
            RegionUser.RealUsername = Official.RealUsername
            RegionUser.IsLinked = Official.IsLinked

            Dim TRank As New Dictionary(Of String, String)
            If Official.Rank Is Nothing Then Official.Rank = New Dictionary(Of String, String)

            For Each TTRank In Official.Rank
                TRank.Add(TTRank.Key, TTRank.Value)
            Next

            If (PositionId = "official" OrElse PositionId = "scorekeeper" OrElse PositionId = "supervisor") Then
                If RegionUser.Rank Is Nothing Then RegionUser.Rank = New Dictionary(Of String, String)
                If Not RegionUser.Rank.ContainsKey(PositionId) Then RegionUser.Rank.Add(PositionId, "")

                If TRank.ContainsKey(PositionId) Then
                    TRank(PositionId) = RegionUser.Rank(PositionId)
                Else
                    TRank.Add(PositionId, RegionUser.Rank(PositionId))
                End If
            End If

            RegionUser.Rank = TRank


            Dim TRankNumber As New Dictionary(Of String, Decimal)
            If Official.RankNumber Is Nothing Then Official.RankNumber = New Dictionary(Of String, Decimal)

            For Each TTRank In Official.RankNumber
                TRankNumber.Add(TTRank.Key, TTRank.Value)
            Next

            If (PositionId = "official" OrElse PositionId = "scorekeeper" OrElse PositionId = "supervisor") Then
                If RegionUser.RankNumber Is Nothing Then RegionUser.RankNumber = New Dictionary(Of String, Decimal)
                If Not RegionUser.RankNumber.ContainsKey(PositionId) Then RegionUser.RankNumber.Add(PositionId, 0)

                If TRankNumber.ContainsKey(PositionId) Then
                    TRankNumber(PositionId) = RegionUser.RankNumber(PositionId)
                Else
                    TRankNumber.Add(PositionId, RegionUser.RankNumber(PositionId))
                End If
            End If

            RegionUser.RankNumber = TRankNumber
        End If

        RegionUser.Positions.Sort()

        RegionUser.RegionId = RegionId

        UpsertSimpleHelper(RegionUser, SQLConnection, SQLTransaction, CommandText)

        Dim UpdateFriendNotification As Boolean = False
        Dim RegionUserPositions As String = JsonConvert.SerializeObject(RegionUser.Positions)
        Dim UpdateFriendNotificationCommandText As String = "INSERT INTO FriendNotification (Username, FriendId, FriendUsername, DateCreated, IsViewed, Positions, Denied) VALUES (@Username, @RegionId, @FriendUsername, @DateCreated, @IsViewed, @Positions, 0)"

        UserFromEmail = User.GetUserInfoFromEmailHelper(RegionUser.Email, SQLConnection, SQLTransaction)

        Dim Positions As List(Of String) = Nothing
        Dim PositionsAsString As String = ""
        Dim Denied As Boolean = False

        If Official IsNot Nothing AndAlso Official.RealUsername = "" AndAlso Official.Email <> RegionUser.Email AndAlso Official.Email <> "" Then
            CommandText = "DELETE FROM FriendNotification WHERE Username IN (SELECT Username FROM [USER] AS U WHERE U.Email = @Email) AND FriendId = @RegionId AND FriendUsername = @FriendUsername And Denied = 0"
            Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
                SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                SqlCommand.Parameters.Add(New SqlParameter("Email", Official.Email))
                SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", RegionUser.Username))
                SqlCommand.ExecuteNonQuery()
            End Using
        End If

        CommandText = "SELECT Username, Denied, Positions, DateCreated FROM FriendNotification WHERE FriendId = @RegionId AND FriendUsername = @FriendUsername"
        Dim DateCreated = DateTime.UtcNow
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", RegionUser.Username))
            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Denied = Denied OrElse Reader.GetBoolean(1)
                PositionsAsString = Reader.GetString(2)
                Positions = JsonConvert.DeserializeObject(Of List(Of String))(PositionsAsString.ToLower)
                DateCreated = Reader.GetDateTime(3)
            End While
            Reader.Close()
        End Using

        If Not Denied Then
            Using SqlCommand As New SqlCommand("DELETE FROM FriendNotification WHERE FriendId = @RegionId AND FriendUsername = @FriendUsername", SQLConnection, SQLTransaction)
                SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", RegionUser.Username))
                SqlCommand.ExecuteNonQuery()
            End Using

            If UserFromEmail IsNot Nothing AndAlso (Official Is Nothing OrElse (Official IsNot Nothing AndAlso Official.IsLinked = False)) Then
                Using SqlCommand As New SqlCommand(UpdateFriendNotificationCommandText, SQLConnection, SQLTransaction)
                    SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                    SqlCommand.Parameters.Add(New SqlParameter("Username", UserFromEmail.Username))
                    SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", RegionUser.Username))
                    SqlCommand.Parameters.Add(New SqlParameter("DateCreated", DateCreated))
                    SqlCommand.Parameters.Add(New SqlParameter("IsViewed", False))
                    SqlCommand.Parameters.Add(New SqlParameter("Positions", RegionUserPositions))
                    SqlCommand.ExecuteNonQuery()
                End Using
            End If
        End If

        If Not Denied Then
            ShouldSendEmail = True
            If Official IsNot Nothing AndAlso Official.IsLinked Then
                ShouldSendEmail = False
            End If
        End If

        If ShouldSendEmail Then
            PublicCode.InitConfig()

            If RegionUser.Email <> "" AndAlso PublicCode.IsEmail(RegionUser.Email) Then
                Dim RegionName = Region.RegionName
                Dim FirstName = RegionUser.FirstName
                Dim Language = RegionUser.PreferredLanguage

                If UserFromEmail IsNot Nothing Then
                    FirstName = UserFromEmail.FirstName
                    Language = UserFromEmail.PreferredLanguage
                End If

                Dim PositionsConcSB As New StringBuilder()
                If Positions Is Nothing Then Positions = RegionUser.Positions
                For I As Integer = 0 To Positions.Count - 1
                    If I = 0 Then
                        PositionsConcSB.Append(Languages.GetText("Generic", Positions(I), "Text", Language))
                    ElseIf I = Positions.Count - 1 Then
                        PositionsConcSB.Append(" " & Languages.GetText("Generic", "And", "Text", Language) & " ")
                        PositionsConcSB.Append(Languages.GetText("Generic", Positions(I), "Text", Language))
                    Else
                        PositionsConcSB.Append(", " & Languages.GetText("Generic", Positions(I), "Text", Language))
                    End If
                Next

                Dim Emails As New List(Of String)
                If RegionUser.Email <> "" AndAlso PublicCode.IsEmail(RegionUser.Email) Then Emails.Add(RegionUser.Email)

                For Each TEmail In Emails
                    Dim InvitiationGuid = Guid.NewGuid

                    If UserFromEmail Is Nothing Then
                        InvitiationGuid = Guid.NewGuid

                        CommandText = "INSERT INTO UserInvitation (InvitationGUID, Username, FirstName, LastName, Email, PhoneNumbers, Country, State, City, Address, PostalCode, PreferredLanguage, AlternateEmails, InvitationDate) VALUES (@InvitationGUID, @Username, @FirstName, @LastName, @Email, @PhoneNumbers, @Country, @State, @City, @Address, @PostalCode, @PreferredLanguage, @AlternateEmails, @InvitationDate)"
                        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("InvitationGUID", InvitiationGuid))
                            SqlCommand.Parameters.Add(New SqlParameter("Username", RegionUser.Username))
                            SqlCommand.Parameters.Add(New SqlParameter("FirstName", RegionUser.FirstName))
                            SqlCommand.Parameters.Add(New SqlParameter("LastName", RegionUser.LastName))
                            SqlCommand.Parameters.Add(New SqlParameter("Email", RegionUser.Email))
                            SqlCommand.Parameters.Add(New SqlParameter("PhoneNumbers", JsonConvert.SerializeObject(RegionUser.PhoneNumbers)))
                            SqlCommand.Parameters.Add(New SqlParameter("Country", RegionUser.Country))
                            SqlCommand.Parameters.Add(New SqlParameter("State", RegionUser.State))
                            SqlCommand.Parameters.Add(New SqlParameter("City", RegionUser.City))
                            SqlCommand.Parameters.Add(New SqlParameter("Address", RegionUser.Address))
                            SqlCommand.Parameters.Add(New SqlParameter("PostalCode", RegionUser.PostalCode))
                            SqlCommand.Parameters.Add(New SqlParameter("PreferredLanguage", RegionUser.PreferredLanguage))
                            SqlCommand.Parameters.Add(New SqlParameter("AlternateEmails", JsonConvert.SerializeObject(RegionUser.AlternateEmails)))
                            SqlCommand.Parameters.Add(New SqlParameter("InvitationDate", DateTime.UtcNow))
                            SqlCommand.ExecuteNonQuery()
                        End Using
                    End If

                    If Region.RegionIsLadderLeague Then
                        ' Old Site Emails
                        If SendToNewSite = False Then
                            EmailList.Add(New EmailTest With {
                                .Email = TEmail,
                                .Subject = Languages.GetText("InviteUserToRegion", "EmailSubjectLadderLeague", "Text", Language).Replace("{0}", RegionName),
                                .Body = Languages.GetText("InviteUserToRegion", IIf(UserFromEmail Is Nothing, "EmailBodyNoAccountLadderLeague", "EmailBody"), "Text", Language).Replace("{0}", HttpUtility.HtmlEncode(FirstName)).Replace("{1}", HttpUtility.HtmlEncode(RegionName)).Replace("{2}", PositionsConcSB.ToString()).Replace("{3}", IIf(UserFromEmail Is Nothing, PublicCode.GetLocalServer() & "#register?invitationguid=" & InvitiationGuid.ToString, PublicCode.GetLocalServer())),
                                .IsHTML = True
                            })
                        Else ' New Site Emails
                            EmailList.Add(New EmailTest With {
                                .Email = TEmail,
                                .Subject = Languages.GetText("InviteUserToRegion", "EmailSubjectLadderLeague", "Text", Language).Replace("{0}", RegionName),
                                .Body = Languages.GetText("InviteUserToRegion", IIf(UserFromEmail Is Nothing, "EmailBodyNoAccountLadderLeague", "EmailBody"), "Text", Language).Replace("{0}", HttpUtility.HtmlEncode(FirstName)).Replace("{1}", HttpUtility.HtmlEncode(RegionName)).Replace("{2}", PositionsConcSB.ToString()).Replace("{3}", IIf(UserFromEmail Is Nothing, IIf(PublicCode.ConfigObject.Environment <> "production", "https://staging.asportsmanager.com/invitation-confirmation?invitationguid=" & InvitiationGuid.ToString & "&email=" & RegionUser.Email, "https://app.asportsmanager.com/invitation-confirmation?invitationguid=" & InvitiationGuid.ToString & "&email=" & RegionUser.Email), IIf(PublicCode.ConfigObject.Environment <> "production", "https://staging.asportsmanager.com", "https://app.asportsmanager.com"))),
                                .IsHTML = True
                            })
                        End If

                    Else
                        ' Old Site Emails
                        If SendToNewSite = False Then
                            EmailList.Add(New EmailTest With {
                            .Email = TEmail,
                            .Subject = Languages.GetText("InviteUserToRegion", "EmailSubject", "Text", Language).Replace("{0}", RegionName),
                            .Body = Languages.GetText("InviteUserToRegion", IIf(UserFromEmail Is Nothing, "EmailBodyNoAccount", "EmailBody"), "Text", Language).Replace("{0}", HttpUtility.HtmlEncode(FirstName)).Replace("{1}", HttpUtility.HtmlEncode(RegionName)).Replace("{2}", PositionsConcSB.ToString()).Replace("{3}", IIf(UserFromEmail Is Nothing, PublicCode.GetLocalServer() & "#register?invitationguid=" & InvitiationGuid.ToString, PublicCode.GetLocalServer())),
                            .IsHTML = True
                        })
                        Else ' New Site Emails
                            EmailList.Add(New EmailTest With {
                            .Email = TEmail,
                            .Subject = Languages.GetText("InviteUserToRegion", "EmailSubject", "Text", Language).Replace("{0}", RegionName),
                            .Body = Languages.GetText("InviteUserToRegion", IIf(UserFromEmail Is Nothing, "EmailBodyNoAccount", "EmailBody"), "Text", Language).Replace("{0}", HttpUtility.HtmlEncode(FirstName)).Replace("{1}", HttpUtility.HtmlEncode(RegionName)).Replace("{2}", PositionsConcSB.ToString()).Replace("{3}", IIf(UserFromEmail Is Nothing, IIf(PublicCode.ConfigObject.Environment <> "production", "https://staging.asportsmanager.com/invitation-confirmation?invitationguid=" & InvitiationGuid.ToString & "&email=" & RegionUser.Email, "https://app.asportsmanager.com/invitation-confirmation?invitationguid=" & InvitiationGuid.ToString & "&email=" & RegionUser.Email), IIf(PublicCode.ConfigObject.Environment <> "production", "https://staging.asportsmanager.com", "https://app.asportsmanager.com"))),
                            .IsHTML = True
                        })
                        End If

                    End If
                Next
            End If
        End If

        If Region.RegionIsLadderLeague Then
            If RegionUser Is Nothing AndAlso OldRegionUser Is Nothing Then
                'Do Nothing
            ElseIf RegionUser IsNot Nothing AndAlso OldRegionUser Is Nothing Then
                GetMasterSchedule(RegionUser.RegionId, ScheduleItems, SQLConnection, SQLTransaction)
                AddGamesToGamesThatNeedScoreUpdated(Schedule.GetOfficialsScheduleFromSchedule(RegionUser.RegionId, RegionUser.Username, ScheduleItems(RegionUser.RegionId)), GamesWhosScoresNeedUpdating)
            ElseIf RegionUser Is Nothing AndAlso OldRegionUser IsNot Nothing Then
                GetMasterSchedule(OldRegionUser.RegionId, ScheduleItems, SQLConnection, SQLTransaction)
                AddGamesToGamesThatNeedScoreUpdated(Schedule.GetOfficialsScheduleFromSchedule(OldRegionUser.RegionId, OldRegionUser.Username, ScheduleItems(OldRegionUser.RegionId)), GamesWhosScoresNeedUpdating)
            ElseIf RegionUser IsNot Nothing AndAlso OldRegionUser IsNot Nothing Then
                GetMasterSchedule(RegionUser.RegionId, ScheduleItems, SQLConnection, SQLTransaction)

                Dim OfficialsSchedule = Schedule.GetOfficialsScheduleFromSchedule(OldRegionUser.RegionId, OldRegionUser.Username, ScheduleItems(OldRegionUser.RegionId))
                OfficialsSchedule.Sort(Schedule.DateSorter)

                PublicCode.ProcessListPairs(
                    OldRegionUser.RankAndDates,
                    RegionUser.RankAndDates,
                    Function(OV, NV)
                        Return OV.RankDate.CompareTo(NV.RankDate)
                    End Function,
                    Sub(OV)
                        Dim ScheduleItemToSearch = New Schedule()
                        ScheduleItemToSearch.RegionId = OldRegionUser.RegionId
                        ScheduleItemToSearch.GameDate = OV.RankDate
                        Dim SortIndex = PublicCode.BinarySearch(OfficialsSchedule, ScheduleItemToSearch, Schedule.DateSorter)
                        If SortIndex < 0 Then SortIndex = -SortIndex - 1
                        If SortIndex < OfficialsSchedule.Count Then
                            AddGamesToGamesThatNeedScoreUpdated({OfficialsSchedule(SortIndex)}.ToList(), GamesWhosScoresNeedUpdating)
                        End If
                    End Sub,
                    Sub(NV)
                        Dim ScheduleItemToSearch = New Schedule()
                        ScheduleItemToSearch.RegionId = OldRegionUser.RegionId
                        ScheduleItemToSearch.GameDate = NV.RankDate
                        Dim SortIndex = PublicCode.BinarySearch(OfficialsSchedule, ScheduleItemToSearch, Schedule.DateSorter)
                        If SortIndex < 0 Then SortIndex = -SortIndex - 1
                        If SortIndex < OfficialsSchedule.Count Then
                            AddGamesToGamesThatNeedScoreUpdated({OfficialsSchedule(SortIndex)}.ToList(), GamesWhosScoresNeedUpdating)
                        End If
                    End Sub,
                    Sub(OV, NV)
                        If (OV.Rank <> NV.Rank) Then
                            Dim ScheduleItemToSearch = New Schedule()
                            ScheduleItemToSearch.RegionId = OldRegionUser.RegionId
                            ScheduleItemToSearch.GameDate = OV.RankDate
                            Dim SortIndex = PublicCode.BinarySearch(OfficialsSchedule, ScheduleItemToSearch, Schedule.DateSorter)
                            If SortIndex < 0 Then SortIndex = -SortIndex - 1
                            If SortIndex < OfficialsSchedule.Count Then
                                AddGamesToGamesThatNeedScoreUpdated({OfficialsSchedule(SortIndex)}.ToList(), GamesWhosScoresNeedUpdating)
                            End If

                            ScheduleItemToSearch.RegionId = OldRegionUser.RegionId
                            ScheduleItemToSearch.GameDate = NV.RankDate
                            SortIndex = PublicCode.BinarySearch(OfficialsSchedule, ScheduleItemToSearch, Schedule.DateSorter)
                            If SortIndex < 0 Then SortIndex = -SortIndex - 1
                            If SortIndex < OfficialsSchedule.Count Then
                                AddGamesToGamesThatNeedScoreUpdated({OfficialsSchedule(SortIndex)}.ToList(), GamesWhosScoresNeedUpdating)
                            End If
                        End If
                    End Sub
                )
            End If
        End If

    End Sub

    Private Shared Sub AddGamesToGamesThatNeedScoreUpdated(ScheduleItems As List(Of Schedule), GamesWhosScoresNeedUpdating As Dictionary(Of String, Dictionary(Of Integer, Schedule)))
        For Each ScheduleItem In ScheduleItems
            If Not GamesWhosScoresNeedUpdating.ContainsKey(ScheduleItem.RegionId) Then
                GamesWhosScoresNeedUpdating.Add(ScheduleItem.RegionId, New Dictionary(Of Integer, Schedule))
            End If
            If Not GamesWhosScoresNeedUpdating(ScheduleItem.RegionId).ContainsKey(ScheduleItem.ScheduleId) Then
                GamesWhosScoresNeedUpdating(ScheduleItem.RegionId).Add(ScheduleItem.ScheduleId, ScheduleItem)
            End If
        Next
    End Sub

    Private Shared Sub GetMasterSchedule(RegionId As String, ScheduleItems As Dictionary(Of String, List(Of Schedule)), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)

        If Not ScheduleItems.ContainsKey(RegionId) Then
            Dim RegionIds As List(Of String) = {RegionId}.ToList
            Dim FullSchedule = Schedule.GetMasterScheduleFromRegionsNonVersionHelper(RegionIds, Date.MinValue, Date.MaxValue, SQLConnection, SQLTransaction)
            SchedulePosition.GetMasterSchedulePositionFromRegionsNonVersionHelper(RegionIds, Date.MinValue, Date.MaxValue, FullSchedule, SQLConnection, SQLTransaction)
            ScheduleFine.GetMasterScheduleFineFromRegionsNonVersionHelper(RegionIds, Date.MinValue, Date.MaxValue, FullSchedule, SQLConnection, SQLTransaction)
            ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsNonVersionHelper(RegionIds, Date.MinValue, Date.MaxValue, FullSchedule, SQLConnection, SQLTransaction)
            ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsNonVersionHelper(RegionIds, Date.MinValue, Date.MaxValue, FullSchedule, SQLConnection, SQLTransaction)

            ScheduleItems.Add(RegionId, FullSchedule)
        End If
    End Sub


    Public Shared Function ResendInvitation(RegionId As String, AssignorId As String) As Object
        Dim EmailList As New List(Of EmailTest)

        Dim Region As RegionProperties

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                    If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                        Return New ErrorObject("InvalidPermissions")
                    End If

                    Region = UmpireAssignor.RegionProperties.GetRegionPropertiesHelper(RegionId, SqlConnection, SqlTransaction)
                    Dim RegionUsers As List(Of RegionUser) = LoadAllInRegionHelper(RegionId, "", SqlConnection, SqlTransaction, True, True)
                    Dim FriendNotifications As List(Of FriendNotification) = FriendNotification.GetFriendNotificationsInRegionHelper(RegionId, SqlConnection, SqlTransaction)

                    For Each RegionUser In RegionUsers
                        Dim TFriendNotifications = FriendNotifications.FindAll(Function(FN) FN.FriendUsername = RegionUser.Username)

                        ResendInvitationHelper(Region, RegionUser, TFriendNotifications, SqlConnection, SqlTransaction, EmailList)
                    Next

                    SqlTransaction.Commit()
                End Using
            End Using

            If Region IsNot Nothing AndAlso Not Region.IsDemo Then
                For Each EmailListItem In EmailList
                    Try
                        PublicCode.SendEmailStandard(EmailListItem.Email, EmailListItem.Subject, EmailListItem.Body, True)
                    Catch
                    End Try
                Next
            End If

            Return New With {.Success = True}
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function ResendInvitation(RegionId As String, AssignorId As String, Username As String) As Object
        Dim EmailList As New List(Of EmailTest)

        Dim Region As RegionProperties

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                    If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                        Return New ErrorObject("InvalidPermissions")
                    End If

                    Region = UmpireAssignor.RegionProperties.GetRegionPropertiesHelper(RegionId, SqlConnection, SqlTransaction)
                    Dim RegionUser As RegionUser = LoadBasicFromUsername(RegionId, Username, SqlConnection, SqlTransaction)
                    Dim FriendNotifications As List(Of FriendNotification) = FriendNotification.GetFriendNotificationsInRegionUserHelper(RegionId, Username, SqlConnection, SqlTransaction)

                    ResendInvitationHelper(Region, RegionUser, FriendNotifications, SqlConnection, SqlTransaction, EmailList)

                    SqlTransaction.Commit()
                End Using
            End Using

            If Region IsNot Nothing AndAlso Not Region.IsDemo Then
                For Each EmailListItem In EmailList
                    Try
                        PublicCode.SendEmailStandard(EmailListItem.Email, EmailListItem.Subject, EmailListItem.Body, True)
                    Catch
                    End Try
                Next
            End If

            Return New With {.Success = True}
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Sub ResendInvitationHelper(Region As RegionProperties, RegionUser As RegionUser, FriendNotification As List(Of FriendNotification), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction, EmailList As List(Of EmailTest))
        If RegionUser.RealUsername <> "" Then Return
        If FriendNotification.Any(Function(FN) FN.Denied) Then Return
        If Not PublicCode.IsEmail(RegionUser.Email.Trim) Then Return

        Dim PositionsConcSB As New StringBuilder()
        Dim Positions = RegionUser.Positions
        Dim CommandText As String = ""


        Dim Language = RegionUser.PreferredLanguage

        For I As Integer = 0 To Positions.Count - 1
            If I = 0 Then
                PositionsConcSB.Append(Languages.GetText("Generic", Positions(I), "Text", Language))
            ElseIf I = Positions.Count - 1 Then
                PositionsConcSB.Append(" " & Languages.GetText("Generic", "And", "Text", Language) & " ")
                PositionsConcSB.Append(Languages.GetText("Generic", Positions(I), "Text", Language))
            Else
                PositionsConcSB.Append(", " & Languages.GetText("Generic", Positions(I), "Text", Language))
            End If
        Next

        Dim User = UmpireAssignor.User.GetUserInfoFromEmailHelper(RegionUser.Email, SQLConnection, SQLTransaction)
        If User Is Nothing Then
            Dim InvitationGuid = GetUserInvitationHelperFromUsername(RegionUser.Username, RegionUser.Email, SQLConnection, SQLTransaction)
            If InvitationGuid Is Nothing Then
                InvitationGuid = Guid.NewGuid

                CommandText = "INSERT INTO UserInvitation (InvitationGUID, Username, FirstName, LastName, Email, PhoneNumbers, Country, State, City, Address, PostalCode, PreferredLanguage, AlternateEmails, InvitationDate) VALUES (@InvitationGUID, @Username, @FirstName, @LastName, @Email, @PhoneNumbers, @Country, @State, @City, @Address, @PostalCode, @PreferredLanguage, @AlternateEmails, @InvitationDate)"
                Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
                    SqlCommand.Parameters.Add(New SqlParameter("InvitationGUID", InvitationGuid))
                    SqlCommand.Parameters.Add(New SqlParameter("Username", RegionUser.Username))
                    SqlCommand.Parameters.Add(New SqlParameter("FirstName", RegionUser.FirstName))
                    SqlCommand.Parameters.Add(New SqlParameter("LastName", RegionUser.LastName))
                    SqlCommand.Parameters.Add(New SqlParameter("Email", RegionUser.Email))
                    SqlCommand.Parameters.Add(New SqlParameter("PhoneNumbers", JsonConvert.SerializeObject(RegionUser.PhoneNumbers)))
                    SqlCommand.Parameters.Add(New SqlParameter("Country", RegionUser.Country))
                    SqlCommand.Parameters.Add(New SqlParameter("State", RegionUser.State))
                    SqlCommand.Parameters.Add(New SqlParameter("City", RegionUser.City))
                    SqlCommand.Parameters.Add(New SqlParameter("Address", RegionUser.Address))
                    SqlCommand.Parameters.Add(New SqlParameter("PostalCode", RegionUser.PostalCode))
                    SqlCommand.Parameters.Add(New SqlParameter("PreferredLanguage", RegionUser.PreferredLanguage))
                    SqlCommand.Parameters.Add(New SqlParameter("AlternateEmails", JsonConvert.SerializeObject(RegionUser.AlternateEmails)))
                    SqlCommand.Parameters.Add(New SqlParameter("InvitationDate", DateTime.UtcNow))
                    SqlCommand.ExecuteNonQuery()
                End Using
            End If
            If Region.RegionIsLadderLeague Then
                EmailList.Add(New EmailTest With {
                  .Email = RegionUser.Email,
                  .Subject = Languages.GetText("InviteUserToRegion", "EmailSubjectLadderLeague", "Text", RegionUser.PreferredLanguage).Replace("{0}", Region.RegionName),
                  .Body = Languages.GetText("InviteUserToRegion", "EmailBodyNoAccountLadderLeague", "Text", RegionUser.PreferredLanguage).Replace("{0}", HttpUtility.HtmlEncode(RegionUser.FirstName)).Replace("{1}", HttpUtility.HtmlEncode(Region.RegionName)).Replace("{2}", PositionsConcSB.ToString()).Replace("{3}", PublicCode.GetLocalServer() & "#register?invitationguid=" & InvitationGuid.ToString),
                  .IsHTML = True
                })
            Else
                EmailList.Add(New EmailTest With {
                  .Email = RegionUser.Email,
                  .Subject = Languages.GetText("InviteUserToRegion", "EmailSubject", "Text", RegionUser.PreferredLanguage).Replace("{0}", Region.RegionName),
                  .Body = Languages.GetText("InviteUserToRegion", "EmailBodyNoAccount", "Text", RegionUser.PreferredLanguage).Replace("{0}", HttpUtility.HtmlEncode(RegionUser.FirstName)).Replace("{1}", HttpUtility.HtmlEncode(Region.RegionName)).Replace("{2}", PositionsConcSB.ToString()).Replace("{3}", PublicCode.GetLocalServer() & "#register?invitationguid=" & InvitationGuid.ToString),
                  .IsHTML = True
                })
            End If

        Else
            Dim MyFriendNotification = UmpireAssignor.FriendNotification.GetFriendNotificationHelper(User.Username, Region.RegionId, RegionUser.Username, SQLConnection, SQLTransaction)

            If MyFriendNotification Is Nothing Then
                Dim UpdateFriendNotificationCommandText As String = "INSERT INTO FriendNotification (Username, FriendId, FriendUsername, DateCreated, IsViewed, Positions, Denied) VALUES (@Username, @RegionId, @FriendUsername, @DateCreated, @IsViewed, @Positions, 0)"
                Using SqlCommand As New SqlCommand(UpdateFriendNotificationCommandText, SQLConnection, SQLTransaction)
                    SqlCommand.Parameters.Add(New SqlParameter("RegionId", Region.RegionId))
                    SqlCommand.Parameters.Add(New SqlParameter("Username", User.Username))
                    SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", RegionUser.Username))
                    SqlCommand.Parameters.Add(New SqlParameter("DateCreated", DateTime.UtcNow))
                    SqlCommand.Parameters.Add(New SqlParameter("IsViewed", False))
                    SqlCommand.Parameters.Add(New SqlParameter("Positions", JsonConvert.SerializeObject(RegionUser.Positions)))
                    SqlCommand.ExecuteNonQuery()
                End Using
            End If

            If MyFriendNotification Is Nothing OrElse Not MyFriendNotification.Denied Then
                If Region.RegionIsLadderLeague Then
                    EmailList.Add(New EmailTest With {
                      .Email = User.Email,
                      .Subject = Languages.GetText("InviteUserToRegion", "EmailSubjectLadderLeague", "Text", Language).Replace("{0}", Region.RegionName),
                      .Body = Languages.GetText("InviteUserToRegion", "EmailBodyLadderLeague", "Text", Language).Replace("{0}", HttpUtility.HtmlEncode(User.FirstName)).Replace("{1}", HttpUtility.HtmlEncode(Region.RegionName)).Replace("{2}", PositionsConcSB.ToString()).Replace("{3}", PublicCode.GetLocalServer()),
                      .IsHTML = True
                    })
                Else
                    EmailList.Add(New EmailTest With {
                      .Email = User.Email,
                      .Subject = Languages.GetText("InviteUserToRegion", "EmailSubject", "Text", Language).Replace("{0}", Region.RegionName),
                      .Body = Languages.GetText("InviteUserToRegion", "EmailBody", "Text", Language).Replace("{0}", HttpUtility.HtmlEncode(User.FirstName)).Replace("{1}", HttpUtility.HtmlEncode(Region.RegionName)).Replace("{2}", PositionsConcSB.ToString()).Replace("{3}", PublicCode.GetLocalServer()),
                      .IsHTML = True
                    })
                End If
            End If
        End If
    End Sub

    Public Shared Function GetUserInvitation(InvitationGUID As Guid?) As Object
        Dim User As User = Nothing

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    User = GetUserInvitationHelper(InvitationGUID, SqlConnection, SqlTransaction)
                    SqlTransaction.Commit()
                End Using
            End Using


            Return New With {
                .Success = True,
                .User = User
            }
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function GetUserInvitationHelper(InvitationGUID As Guid?, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As User
        Dim User As User = Nothing

        Dim CommandText = "SELECT Username, FirstName, LastName, Email, PhoneNumbers, Country, State, City, Address, PostalCode, PreferredLanguage, AlternateEmails, InvitationDate FROM UserInvitation WHERE InvitationGUID = @InvitationGUID"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("InvitationGUID", InvitationGUID))
            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                User = New User With {
                    .Username = Reader.GetString(0),
                    .FirstName = Reader.GetString(1),
                    .LastName = Reader.GetString(2),
                    .Email = Reader.GetString(3),
                    .PhoneNumbers = JsonConvert.DeserializeObject(Of List(Of PhoneNumber))(Reader.GetString(4)),
                    .Country = Reader.GetString(5),
                    .State = Reader.GetString(6),
                    .City = Reader.GetString(7),
                    .Address = Reader.GetString(8),
                    .PostalCode = Reader.GetString(9),
                    .PreferredLanguage = Reader.GetString(10),
                    .AlternateEmails = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(11)),
                    .RegistrationDate = Reader.GetDateTime(12)
                }
            End While
            Reader.Close()
        End Using
        Return User
    End Function

    Public Shared Function GetUserInviteUserHelper(Username As String, Email As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As User
        Dim User As User = Nothing

        Dim CommandText = "SELECT Username, Email, RegistrationToken FROM InviteUser WHERE Username = @Username"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            SqlCommand.Parameters.Add(New SqlParameter("Email", Email))
            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                User = New User With {
                    .Username = Reader.GetString(0),
                    .Email = Reader.GetString(1),
                    .RegistrationToken = Reader.GetGuid(2)
                }
            End While
            Reader.Close()
        End Using
        Return User
    End Function

    Public Shared Function GetUserInvitationHelperFromUsername(Username As String, Email As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As Guid?
        Dim Result As Guid? = Nothing

        Dim CommandText = "SELECT InvitationGUID FROM UserInvitation WHERE Username = @Username AND Email = @Email"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            SqlCommand.Parameters.Add(New SqlParameter("Email", Email))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Result = Reader.GetGuid(0)
            End While
            Reader.Close()
        End Using
        Return Result
    End Function

    Public Shared Function Delete(AssignorId As String, RegionId As String, PositionId As String, RegionUser As RegionUser) As Object
        PositionId = PositionId.ToLower()
        Dim ShouldSendEmail As Boolean = False
        Dim UserFromEmail As User = Nothing

        PositionId = PositionId.ToLower()
        RegionId = RegionId.ToLower()

        Dim OldRegionUsers As New List(Of RegionUser)
        Dim NewRegionUsers As New List(Of RegionUser)

        Dim ScheduleItems As New Dictionary(Of String, List(Of Schedule))
        Dim GamesWhosScoresNeedUpdating As New Dictionary(Of String, Dictionary(Of Integer, Schedule))

        Dim Region As Region

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Region = UmpireAssignor.Region.GetRegionHelper(RegionId, SqlConnection, SqlTransaction)

                    If Not IsValidPosition(Region, PositionId) Then Return New ErrorObject("InvalidPosition")

                    Dim CommandText As String = ""

                    Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                    If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                        Return New ErrorObject("InvalidPermissions")
                    End If

                    OldRegionUsers = LoadAllInRegionHelper(RegionId, AssignorId, SqlConnection, SqlTransaction)

                    If Not DeleteHelper(AssignorId, Region, PositionId, RegionUser, ScheduleItems, GamesWhosScoresNeedUpdating, SqlConnection, SqlTransaction).Success = True Then
                        Return New ErrorObject("UnAdmin")
                    End If

                    NewRegionUsers = LoadAllInRegionHelper(RegionId, AssignorId, SqlConnection, SqlTransaction)

                    If Region.RegionIsLadderLeague Then
                        Try
                            Dim ScheduleItemsTotal = LadderLeague.ScheduleItemsConversion(ScheduleItems)

                            Dim OldItemsClone As New List(Of Schedule)
                            For Each Item In ScheduleItemsTotal
                                OldItemsClone.Add(Item.CloneItem)
                            Next

                            LadderLeague.UpdateGameRanks(
                                NewRegionUsers,
                                OldItemsClone,
                                ScheduleItemsTotal,
                                LadderLeague.SortedGamesWhosScoresNeedUpdatingConversion(GamesWhosScoresNeedUpdating),
                                New List(Of GameEmail),
                                SqlConnection,
                                SqlTransaction
                                )
                        Catch ex As Exception
                            PublicCode.SendEmailStandard("integration@asportsmanager.com", "Error Updating the Ranks", ex.Message & vbCrLf & vbCrLf & ex.StackTrace, True)
                        End Try
                    End If

                    SqlTransaction.Commit()
                End Using
            End Using

            Dim OldRegionUsernames As New List(Of String)
            Dim NewRegionUsernames As New List(Of String)

            For Each RU In OldRegionUsers
                If RU.RealUsername <> "" AndAlso Not OldRegionUsernames.Contains(RU.RealUsername) Then
                    OldRegionUsernames.Add(RU.RealUsername)
                End If
            Next

            For Each RU In NewRegionUsers
                If RU.RealUsername <> "" AndAlso Not NewRegionUsernames.Contains(RU.RealUsername) Then
                    NewRegionUsernames.Add(RU.RealUsername)
                End If
            Next

            OldRegionUsernames.Sort()
            NewRegionUsernames.Sort()

            Dim RemovedRegionUsernames As New List(Of String)

            PublicCode.ProcessListPairs(OldRegionUsernames, NewRegionUsernames, Function(RU1, RU2) RU1.CompareTo(RU2),
                                        Sub(RU1)
                                            RemovedRegionUsernames.Add(RU1)
                                        End Sub,
                                        Sub(RU2)
                                        End Sub,
                                        Sub(RU1, RU2)
                                        End Sub)

            Dim Context = GlobalHost.ConnectionManager.GetHubContext(Of WsHandler)()

            For Each TUsername In NewRegionUsernames
                If WsHandler.UserConnectionIds.ContainsKey(TUsername) Then
                    For Each ConnectionId In WsHandler.UserConnectionIds(TUsername)
                        Context.Clients.Client(ConnectionId).removeContacts(RemovedRegionUsernames, RegionId)
                    Next
                End If
            Next

            For Each TUsername In RemovedRegionUsernames
                If WsHandler.UserConnectionIds.ContainsKey(TUsername) Then
                    For Each ConnectionId In WsHandler.UserConnectionIds(TUsername)
                        Context.Clients.Client(ConnectionId).removeContacts(NewRegionUsernames, RegionId)
                    Next
                End If
            Next

            Return New With {.Success = True}
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Sub FromUser(User As User)
        Me.RealUsername = User.Username
        Me.IsLinked = True
        Me.AllowInfoLink = True
        Me.PreferredLanguage = User.PreferredLanguage
        Me.FirstName = User.FirstName
        Me.LastName = User.LastName
        Me.Email = User.Email
        Me.PhoneNumbers = User.PhoneNumbers
        Me.AlternateEmails = User.AlternateEmails
        Me.Country = User.Country
        Me.State = User.State
        Me.City = User.City
        Me.PostalCode = User.PostalCode
        Me.Address = User.Address
        Me.PhotoId = User.PhotoId
        Me.HasPhotoId = User.HasPhotoId
        Me.IsActive = True
        Me.IsArchived = False
        Me.CanViewMasterSchedule = True
        Me.CanViewAvailability = True
    End Sub

    Public Function ToAddressCode() As String
        If Country = "" Then Return ""
        If State = "" Then Return ""
        If City = "" Then Return ""
        If Address = "" Then Return ""
        Return HttpUtility.UrlEncode(Country.ToLower & " " & State.ToLower & " " & City.ToLower & " " & Address.ToLower)
    End Function

    Public Shared Function DeleteHelper(AssignorId As String, Region As Region, PositionId As String, RegionUser As RegionUser, ScheduleItems As Dictionary(Of String, List(Of Schedule)), GamesWhosScoresNeedUpdating As Dictionary(Of String, Dictionary(Of Integer, Schedule)), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As Object
        Dim RegionId = Region.RegionId
        Dim Official = UmpireAssignor.RegionUser.LoadBasic(RegionId, RegionUser.Username, SQLConnection, SQLTransaction)
        Dim CommandText As String = ""
        Dim ShouldSendEmail As Boolean = False
        Dim UserFromEmail As User = Nothing


        If Official IsNot Nothing AndAlso Official.Positions.Contains(PositionId) Then
            Dim OldIsExecutive As Boolean = Official.IsExecutive()

            Official.Positions.Remove(PositionId)

            If AssignorId = Official.RealUsername AndAlso Not Official.IsExecutive AndAlso OldIsExecutive Then
                Return New ErrorObject("UnAdmin")
            End If


            If Official.Positions.Count = 0 Then
                CommandText = "DELETE FROM RegionUser WHERE RegionId = @RegionId AND Username = @Username"
                Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
                    SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                    SqlCommand.Parameters.Add(New SqlParameter("Username", RegionUser.Username))
                    SqlCommand.ExecuteNonQuery()
                End Using

                If Official.IsLinked Then
                    CommandText = "DELETE FROM FriendNotification WHERE FriendId = @RegionId AND Username = @Username And FriendUsername = @FriendUsername And Denied = 0"
                    Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("Username", Official.RealUsername))
                        SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", RegionUser.Username))
                        SqlCommand.ExecuteNonQuery()
                    End Using
                End If
            Else
                CommandText = "UPDATE RegionUser SET Positions = @Positions, IsExecutive = @IsExecutive WHERE RegionId = @RegionId AND Username = @Username"
                Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
                    SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                    SqlCommand.Parameters.Add(New SqlParameter("Username", RegionUser.Username))
                    SqlCommand.Parameters.Add(New SqlParameter("Positions", JsonConvert.SerializeObject(Official.Positions)))
                    SqlCommand.Parameters.Add(New SqlParameter("IsExecutive", Official.IsExecutive))
                    SqlCommand.ExecuteNonQuery()
                End Using


                Dim UpdateFriendNotification As Boolean = False
                Dim RegionUserPositions As String = JsonConvert.SerializeObject(Official.Positions)
                Dim UpdateFriendNotificationCommandText As String = "INSERT INTO FriendNotification (Username, FriendId, FriendUsername, DateCreated, IsViewed, Positions, Denied) VALUES (@Username, @RegionId, @FriendUsername, @DateCreated, @IsViewed, @Positions, 0)"

                UserFromEmail = User.GetUserInfoFromEmailHelper(RegionUser.Email, SQLConnection, SQLTransaction)

                If UserFromEmail IsNot Nothing Then
                    If Official Is Nothing Then
                        ShouldSendEmail = True
                        UpdateFriendNotification = True
                    Else
                        If Not Official.IsLinked Then
                            ShouldSendEmail = True
                            Dim Positions As List(Of String) = Nothing
                            Dim PositionsAsString As String = ""


                            CommandText = "SELECT Positions FROM FriendNotification WHERE Username = @Username AND FriendId = @RegionId AND FriendUsername = @FriendUsername"
                            Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
                                SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                                SqlCommand.Parameters.Add(New SqlParameter("Username", UserFromEmail.Username))
                                SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", RegionUser.Username))
                                Dim Reader = SqlCommand.ExecuteReader
                                While Reader.Read
                                    PositionsAsString = Reader.GetString(0).ToLower
                                    Positions = JsonConvert.DeserializeObject(Of List(Of String))(PositionsAsString.ToLower)
                                End While
                                Reader.Close()
                            End Using



                            If Positions Is Nothing Then
                                UpdateFriendNotification = True
                            Else
                                If PositionsAsString <> RegionUserPositions Then
                                    UpdateFriendNotification = True
                                    UpdateFriendNotificationCommandText = "UPDATE FriendNotification SET DateCreated = @DateCreated, IsViewed = @IsViewed, Positions = @Positions WHERE Username = @Username AND FriendId = @RegionId AND FriendUsername = @FriendUsername"
                                Else
                                    UpdateFriendNotification = False
                                End If
                            End If
                        Else
                            ShouldSendEmail = False
                            UpdateFriendNotification = False
                        End If
                    End If
                Else
                    ShouldSendEmail = True
                    UpdateFriendNotification = False
                End If

                If UpdateFriendNotification Then
                    Using SqlCommand As New SqlCommand(UpdateFriendNotificationCommandText, SQLConnection, SQLTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("Username", UserFromEmail.Username))
                        SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", RegionUser.Username))
                        SqlCommand.Parameters.Add(New SqlParameter("DateCreated", DateTime.UtcNow))
                        SqlCommand.Parameters.Add(New SqlParameter("IsViewed", False))
                        SqlCommand.Parameters.Add(New SqlParameter("Positions", RegionUserPositions))
                        SqlCommand.ExecuteNonQuery()
                    End Using
                End If
            End If
        End If

        If Region.RegionIsLadderLeague Then
            GetMasterSchedule(RegionUser.RegionId, ScheduleItems, SQLConnection, SQLTransaction)
            AddGamesToGamesThatNeedScoreUpdated(Schedule.GetOfficialsScheduleFromSchedule(RegionUser.RegionId, RegionUser.Username, ScheduleItems(RegionUser.RegionId)), GamesWhosScoresNeedUpdating)
        End If

        Return New With {.Success = True}
    End Function

    Public Shared Function AllowRegion(OfficialId As String, RegionId As String, RegionUsername As String)
        Dim Regions As List(Of RegionAndPositions) = Nothing
        Dim RegionUsers As List(Of RegionUser) = Nothing
        Dim Users As List(Of User) = Nothing
        Dim FriendNotifications As List(Of FriendNotification) = Nothing

        RegionId = RegionId.ToLower()

        Dim DidDelete As Boolean = False

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim CommandText As String = "DELETE FROM FriendNotification WHERE Username = @Username AND FriendId = @RegionId AND FriendUsername = @FriendUsername"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Username", OfficialId))
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", RegionUsername))
                        DidDelete = (SqlCommand.ExecuteNonQuery() = 1)
                    End Using

                    If DidDelete Then
                        CommandText = "UPDATE RegionUser SET RealUsername = @Username, IsLinked = 1, IsInfoLinked = AllowInfoLink WHERE RegionId = @RegionId AND Username = @RegionUsername"
                        Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("Username", OfficialId))
                            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                            SqlCommand.Parameters.Add(New SqlParameter("RegionUsername", RegionUsername))
                            DidDelete = (SqlCommand.ExecuteNonQuery() = 1)
                        End Using

                        RegionUsers = LoadAllInRegionHelper(RegionId, OfficialId, SqlConnection, SqlTransaction)

                        Dim UserIds As New List(Of String)
                        For Each RegionUser In RegionUsers
                            If RegionUser.RealUsername <> "" AndAlso Not UserIds.Contains(RegionUser.RealUsername) Then
                                UserIds.Add(RegionUser.RealUsername)
                            End If
                        Next

                        Users = User.GetBasicUserInfosHelper(UserIds, SqlConnection, SqlTransaction)
                    End If

                    Regions = User.LoadRegionAndPositions(OfficialId, SqlConnection, SqlTransaction)
                    FriendNotifications = FriendNotification.GetTop5FriendNotificationHelper(OfficialId, SqlConnection, SqlTransaction)

                    SqlTransaction.Commit()
                End Using
            End Using


            Return New With {
                .Success = DidDelete,
                .Message = IIf(DidDelete, "", "NoFriendRequestFound"),
                .Regions = Regions,
                .Users = Users,
                .FriendNotifications = FriendNotifications
            }
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try

    End Function

    Public Shared Function RegionUserFromUserFull(RegionId As String, Rank As Decimal, MyUser As User, RegionUsers As List(Of RegionUser), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As RegionUser
        Dim TestOfficialId As String = PublicCode.SanitizeValue(MyUser.FirstName(0) & MyUser.LastName)
        Dim TestRegionUser = RegionUsers.Find(Function(RU) RU.Username = TestOfficialId)
        Dim DidFind As Boolean = False

        If TestRegionUser IsNot Nothing Then
            For I As Integer = 1 To 100
                TestOfficialId = PublicCode.SanitizeValue(MyUser.FirstName(0) & MyUser.LastName) & I.ToString
                If RegionUsers.Find(Function(RU) RU.Username = TestOfficialId) Is Nothing Then
                    DidFind = True
                    Exit For
                End If
            Next
        Else
            DidFind = True
        End If

        If DidFind Then
            Dim NewRegionUser As New RegionUser With {
                .RegionId = RegionId,
                .Username = TestOfficialId,
                .Positions = {"official"}.ToList,
                .CurrentRank = Rank
            }
            NewRegionUser.FromUser(MyUser)
            NewRegionUser.CanViewMasterSchedule = True
            NewRegionUser.CanViewAvailability = True

            NewRegionUser.RankAndDates = New List(Of RankAndDate)
            NewRegionUser.RankAndDates.Add(New RankAndDate(RankAndDate.FIRST_RANK_DATE, NewRegionUser.CurrentRank))

            Return NewRegionUser
        Else
            Return Nothing
        End If
    End Function


    Public Shared Function JoinRegion(OfficialId As String, RegionId As String, Rank As Decimal)
        Dim Regions As List(Of RegionAndPositions) = Nothing
        Dim RegionUsers As List(Of RegionUser) = Nothing
        Dim Users As List(Of User) = Nothing
        Dim FriendNotifications As List(Of FriendNotification) = Nothing

        OfficialId = OfficialId.ToLower
        RegionId = RegionId.ToLower()

        Dim DidDelete As Boolean = False

        Dim FailedMessage As String = ""

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim MyUser = User.GetUserInfoHelper(OfficialId, SqlConnection, SqlTransaction)
                    RegionUsers = LoadAllInRegionHelper(RegionId, "superadmin", SqlConnection, SqlTransaction)

                    Dim MyRegionUser = RegionUsers.Find(Function(RU) RU.RealUsername = OfficialId)

                    Dim CommandText As String = "DELETE FROM FriendNotification WHERE Username = @Username AND FriendId = @RegionId"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("Username", OfficialId))
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        DidDelete = (SqlCommand.ExecuteNonQuery() = 1)
                    End Using

                    If MyRegionUser Is Nothing Then
                        Dim NewRegionUser = RegionUserFromUserFull(RegionId, Rank, MyUser, RegionUsers, SqlConnection, SqlTransaction)

                        If NewRegionUser IsNot Nothing Then
                            UpsertSimpleHelper(NewRegionUser, SqlConnection, SqlTransaction)

                            Dim UserIds As New List(Of String)
                            For Each RegionUser In RegionUsers
                                If RegionUser.RealUsername <> "" AndAlso Not UserIds.Contains(RegionUser.RealUsername) Then
                                    UserIds.Add(RegionUser.RealUsername)
                                End If
                            Next
                            If Not UserIds.Contains(NewRegionUser.RealUsername) Then
                                UserIds.Add(NewRegionUser.RealUsername)
                            End If

                            Users = User.GetBasicUserInfosHelper(UserIds, SqlConnection, SqlTransaction)

                            Regions = User.LoadRegionAndPositions(OfficialId, SqlConnection, SqlTransaction)
                            FriendNotifications = FriendNotification.GetTop5FriendNotificationHelper(OfficialId, SqlConnection, SqlTransaction)
                        Else
                            FailedMessage = "TooManyUsersWithSameName"
                        End If
                    Else
                        FailedMessage = "UserAlreadyJoined"
                    End If

                    SqlTransaction.Commit()
                End Using
            End Using

            Return New With {
                .Success = FailedMessage = "",
                .Message = FailedMessage,
                .Regions = Regions,
                .Users = Users,
                .FriendNotifications = FriendNotifications
            }
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try

    End Function


    Public Shared Function DenyRegion(OfficialId As String, Username As String, RegionId As String, RegionUsername As String)
        Dim Regions As List(Of RegionAndPositions) = Nothing
        Dim FriendNotifications As List(Of FriendNotification) = Nothing

        RegionId = RegionId.ToLower()

        Dim DidDelete As Boolean = False

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    If OfficialId = Username Then
                        Dim CommandText As String = "UPDATE FriendNotification SET Denied = 1 WHERE Username = @Username AND FriendId = @RegionId AND FriendUsername = @FriendUsername"
                        Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
                            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                            SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", RegionUsername))
                            DidDelete = (SqlCommand.ExecuteNonQuery() = 1)
                        End Using
                    Else
                        Dim CommandText As String = "UPDATE FriendNotification SET Denied = 1 WHERE Username = @Username AND FriendId = @RegionId AND FriendUsername = @FriendUsername AND Username IN (SELECT RegionId FROM RegionUser WHERE RegionId = @Username AND RealUsername = @OfficialId AND IsExecutive = 1)"
                        Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
                            SqlCommand.Parameters.Add(New SqlParameter("OfficialId", OfficialId))
                            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                            SqlCommand.Parameters.Add(New SqlParameter("FriendUsername", RegionUsername))
                            DidDelete = (SqlCommand.ExecuteNonQuery() = 1)
                        End Using
                    End If

                    Regions = User.LoadRegionAndPositions(OfficialId, SqlConnection, SqlTransaction)
                    FriendNotifications = FriendNotification.GetTop5FriendNotificationHelper(OfficialId, SqlConnection, SqlTransaction)

                    SqlTransaction.Commit()
                End Using
            End Using

            Return New With {
                .Success = DidDelete,
                .Message = IIf(DidDelete, "", "NoFriendRequestFound"),
                .Regions = Regions,
                .FriendNotifications = FriendNotifications
            }
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Sub Delete(RegionId As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        Dim CommandText = "DELETE FROM RegionUser Where RegionId = @RegionId AND Username = @Username"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            SqlCommand.ExecuteNonQuery()
        End Using
    End Sub

    Public Shared Function GetUsernameFromRealUsername(RegionId As String, RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As String
        Dim Result As String = ""

        Dim CommandText = "SELECT Username FROM RegionUser Where RegionId = @RegionId AND RealUsername = @RealUsername"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Result = Reader.GetString(0).ToLower
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Function IsTeamRegular() As Boolean
        Return Me.Positions.Contains("player") OrElse Me.Positions.Contains("coach")
    End Function

    Public Function IsOnlyCallUp() As Boolean
        Return Me.Positions.Count = 1 AndAlso Me.Positions(0) = "callup"
    End Function

    Public Shared Function GetUsernameAndPositionsFromRealUsername(RegionId As String, RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction, Optional PreferredUsername As String = "") As RegionUser
        Dim Result As RegionUser = Nothing
        If RealUsername = "" Then Return Result

        If RealUsername = "superadmin" Then
            Return New RegionUser With {
                .Username = "superadmin",
                .Positions = {"assignor", "chief", "manager", "coach"}.ToList(),
                .CanViewAvailability = True,
                .CanViewMasterSchedule = True,
                .CanViewSupervisors = True
            }
        End If

        Dim CommandText = "SELECT Username, Positions, CanViewAvailability, CanViewMasterSchedule, CanViewSupervisors FROM RegionUser Where RegionId = @RegionId AND RealUsername = @RealUsername"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                If Result Is Nothing Then
                    Result = New RegionUser With {
                        .Username = Reader.GetString(0).ToLower,
                        .Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(1).ToLower),
                        .CanViewAvailability = Reader.GetBoolean(2),
                        .CanViewMasterSchedule = Reader.GetBoolean(3),
                        .CanViewSupervisors = Reader.GetBoolean(4)
                    }
                Else
                    If PreferredUsername = Reader.GetString(0).ToLower Then
                        Result.Username = PreferredUsername
                    End If

                    Dim TPositions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(1).ToLower)
                    For Each TPosition In TPositions
                        If Not Result.Positions.Contains(TPosition) Then
                            Result.Positions.Add(TPosition)
                        End If
                    Next

                    Result.CanViewAvailability = Result.CanViewAvailability OrElse Reader.GetBoolean(2)
                    Result.CanViewMasterSchedule = Result.CanViewMasterSchedule OrElse Reader.GetBoolean(3)
                    Result.CanViewSupervisors = Result.CanViewSupervisors OrElse Reader.GetBoolean(4)
                End If
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetUniqueRegionAndOfficialIds(Schedule As List(Of Schedule)) As List(Of RegionIdAndUsername)
        Dim UniqueRegionAndOfficialIds As New List(Of RegionIdAndUsername)
        For Each ScheduleGame In Schedule
            Dim FoundURAO As Boolean = False
            For Each URAO In UniqueRegionAndOfficialIds
                If URAO.RegionId = ScheduleGame.RegionId AndAlso URAO.Username = ScheduleGame.OfficialId Then FoundURAO = True
            Next
            If Not FoundURAO Then UniqueRegionAndOfficialIds.Add(New RegionIdAndUsername(ScheduleGame.RegionId, ScheduleGame.OfficialId))
        Next
        Return UniqueRegionAndOfficialIds
    End Function

    Public Shared Function GetUsernamesFromRealUsername(RegionId As String, RealUsername As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionUser)
        Dim Result As New List(Of RegionUser)
        Dim CommandText As String = "SELECT RegionId, Username, RealUsername, Positions FROM RegionUser WHERE RegionId = @RegionId AND RealUsername = @RealUsername"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Result.Add(New RegionUser With {
                    .RegionId = Reader.GetString(0).ToLower,
                    .Username = Reader.GetString(1).ToLower,
                    .RealUsername = Reader.GetString(2).ToLower,
                    .Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(3).ToLower)
                })
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetUsernameAndPositionsFromRealUsernames(RealUsername As String, RegionIds As List(Of String), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionUser)
        Dim Result As New List(Of RegionUser)
        If RegionIds.Count = 0 Then Return Result

        Dim CommandText As String = "SELECT RegionId, Username, RealUsername, Positions FROM RegionUser WHERE RealUsername = @RealUsername AND RegionId In ("
        Dim URAOSB As New StringBuilder
        Dim URAOCount As Integer = 0
        For Each R In RegionIds
            If URAOCount > 0 Then URAOSB.Append(", ")
            URAOSB.Append(String.Format("@RegionId{0}", URAOCount))
            URAOCount += 1
        Next

        Using SqlCommand As New SqlCommand(CommandText & URAOSB.ToString & ")", SQLConnection, SQLTransaction)
            URAOCount = 0
            SqlCommand.Parameters.Add(New SqlParameter("RealUsername", RealUsername))
            For Each R In RegionIds
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & URAOCount, R))
                URAOCount += 1
            Next

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Result.Add(New RegionUser With {
                    .RegionId = Reader.GetString(0).ToLower,
                    .Username = Reader.GetString(1).ToLower,
                    .RealUsername = Reader.GetString(2).ToLower,
                    .Positions = JsonConvert.DeserializeObject(Of List(Of String))(Reader.GetString(3).ToLower)
                })
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetRegionUserFromRegionAndEmailHelper(RegionId As String, Email As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As RegionUser
        Dim Result As RegionUser = Nothing

        Dim CommandText = "SELECT RegionId, Username, IsLinked, AllowInfoLink, IsInfoLinked, RealUsername, FirstName, LastName, Email, PhoneNumbers, Country, State, City, Address, PostalCode, PreferredLanguage, Rank, Positions, IsArchived, IsActive, AlternateEmails, CanViewAvailability, CanViewMasterSchedule, IsExecutive, PublicData, PrivateData, InternalData, PhotoId, HasPhotoId, StatLinks, RankNumber, GlobalAvailabilityData, RankAndDates, CanViewSupervisors FROM [RegionUser] Where RegionId = @RegionId AND Email = @Email"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("Email", Email))

            ' Debug output - print SQL and parameters
            System.Diagnostics.Debug.WriteLine("SQL: " & CommandText)
            System.Diagnostics.Debug.WriteLine("Parameters: RegionId=" & RegionId & ", Email=" & Email)

            ' Use a Using block for the DataReader to ensure proper disposal
            Using Reader As SqlDataReader = SqlCommand.ExecuteReader()
                While Reader.Read
                    Result = New RegionUser With {
                        .RegionId = Reader.GetString(0).ToLower,
                        .Username = Reader.GetString(1).ToLower,
                        .Email = Reader.GetString(8)
                    }
                End While
            End Using ' This automatically closes and disposes the reader
        End Using

        Return Result
    End Function

    Public Function IsAssignor() As Boolean
        Return Positions IsNot Nothing AndAlso Positions.Contains("assignor")
    End Function

    Public Function IsChief() As Boolean
        Return Positions IsNot Nothing AndAlso Positions.Contains("chief")
    End Function

    Public Function IsOfficial() As Boolean
        Return Positions IsNot Nothing AndAlso Positions.Contains("official")
    End Function

    Public Function IsScorekeeper() As Boolean
        Return Positions IsNot Nothing AndAlso Positions.Contains("scorekeeper")
    End Function

    Public Function IsSupervisor() As Boolean
        Return Positions IsNot Nothing AndAlso Positions.Contains("supervisor")
    End Function

    Public Function IsCallup() As Boolean
        Return Positions IsNot Nothing AndAlso Positions.Contains("callup")
    End Function

    Public Function IsPlayer() As Boolean
        Return Positions IsNot Nothing AndAlso Positions.Contains("player")
    End Function

    Public Function IsCoach() As Boolean
        Return Positions IsNot Nothing AndAlso Positions.Contains("coach")
    End Function

    Public Function IsSpectator() As Boolean
        Return Positions IsNot Nothing AndAlso Positions.Contains("spectator")
    End Function

    Public Function IsEmployee() As Boolean
        Return Positions IsNot Nothing AndAlso (Positions.Contains("official") OrElse Positions.Contains("scorekeeper") OrElse Positions.Contains("supervisor") OrElse Positions.Contains("player"))
    End Function

    Public Function IsExecutive() As Boolean
        Return Positions IsNot Nothing AndAlso (Positions.Contains("chief") OrElse Positions.Contains("assignor") OrElse Positions.Contains("manager") OrElse Positions.Contains("coach"))
    End Function

    Public Function PlayerType() As Integer
        If Me.IsPlayer Then Return 0
        If Me.IsCallup Then Return 1
        Return 2
    End Function

    Private Shared Function IsAssignorDemotingHimself(AssignorId As String, Position As String, RegionUsers As List(Of RegionUser), NewRegionUsers As List(Of RegionUser)) As Boolean
        If Position <> "chief" AndAlso Position <> "assignor" Then Return False
        Dim RegionUser = RegionUsers.Find(Function(RU) RU.Username = AssignorId)
        Dim NewRegionUser = RegionUsers.Find(Function(RU) RU.Username = AssignorId)
        If RegionUser Is Nothing AndAlso NewRegionUser Is Nothing Then Return False
        If RegionUser IsNot Nothing AndAlso NewRegionUser Is Nothing Then Return True
        Return True
    End Function

    Public Function CompareTo(other As RegionUser) As Integer Implements IComparable(Of RegionUser).CompareTo
        Return Username.CompareTo(other.Username)
    End Function
End Class
