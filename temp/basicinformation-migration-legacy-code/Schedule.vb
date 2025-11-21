Imports System.Data.SqlClient
Imports System.Net.Mail
Imports System.Net.Http
Imports System.Net
Imports System.Web.Http
Imports Newtonsoft.Json
Imports UmpireAssignor
Imports System.Web

Public Class Schedule
    Public Property RegionId As String
    Public Property ScheduleId As Integer
    Public Property VersionId As Integer
    Public Property LinkedRegionId As String = ""
    Public Property LinkedScheduleId As Integer
    Public Property GameNumber As String
    Public Property GameType As String
    Public Property LeagueId As String
    Public Property HomeTeam As String
    Public Property HomeTeamScore As String
    Public Property HomeTeamScoreExtra As Dictionary(Of String, String)
    Public Property AwayTeam As String
    Public Property AwayTeamScore As String
    Public Property AwayTeamScoreExtra As Dictionary(Of String, String)
    Public Property GameDate As DateTime
    Public Property CrewType As Dictionary(Of String, String)
    Public Property ParkId As String
    Public Property GameStatus As String
    Public Property GameComment As String
    Public Property GameScore As GameScore
    Public Property OfficialRegionId As String
    Public Property ScorekeeperRegionId As String
    Public Property SupervisorRegionId As String = ""
    Public Property IsDeleted As Boolean
    Public Property OfficialId As String = ""
    Public Property DateNotified As DateTime
    Public Property DateAdded As DateTime

    Public Property SchedulePositions As List(Of SchedulePosition)
    Public Property ScheduleFines As List(Of ScheduleFine)
    Public Property ScheduleUserComments As List(Of ScheduleUserComment)
    Public Property ScheduleUserCommentTeams As List(Of ScheduleUserCommentTeam)
    Public Property ScheduleCommentTeams As List(Of ScheduleCommentTeam)
    Public Property ScheduleCallUps As List(Of ScheduleCallup)
    Public Property StatLinks As String

    Public Function GetUniqueOfficialsOnGame() As List(Of String)
        Dim Result As New List(Of String)
        For Each SchedulePosition In SchedulePositions
            Dim officialId = SchedulePosition.OfficialId.ToLower
            If officialId.ToLower = "" Then Continue For
            If Not Result.Contains(officialId) Then
                Result.Add(officialId)
            End If
        Next
        Return Result
    End Function

    Public Function GetOfficialsPositionIndex(OfficialId As String) As Integer
        If SchedulePositions Is Nothing Then Return -1
        For I As Integer = 0 To SchedulePositions.Count - 1
            If SchedulePositions(I).OfficialId.ToLower = OfficialId Then Return I
        Next
        Return -1
    End Function

    Public Shared Function GetOfficialsScheduleFromSchedule(RegionId As String, OfficialId As String, FullSchedule As List(Of Schedule)) As List(Of Schedule)
        Dim Result As New List(Of Schedule)
        OfficialId = OfficialId.Trim.ToLower

        For Each ScheduleItem In FullSchedule
            For Each SchedulePosition In ScheduleItem.SchedulePositions
                If SchedulePosition.OfficialId.Trim.ToLower = OfficialId Then
                    Result.Add(ScheduleItem)
                    Exit For
                End If
            Next
        Next

        Return Result
    End Function

    Public Shared Function FilterByRegionId(ScheduleItems As List(Of Schedule), RegionId As String) As List(Of Schedule)
        Return ScheduleItems.FindAll(Function (SI) SI.RegionId = RegionId)
    End Function

    Public Shared Function FilterByRegionId(GamesToUpdate As SortedList(Of Date, SortedList(Of Integer, Schedule)), RegionId As String) As SortedList(Of Date, SortedList(Of Integer, Schedule))
        Dim Result As New SortedList(Of Date, SortedList(Of Integer, Schedule))
        For Each Item In GamesToUpdate
            For Each Item2 In Item.Value
                If Item2.Value.RegionId = RegionId Then
                    If Not Result.ContainsKey(Item.Key) Then
                        Result.Add(Item.Key, New SortedList(Of Integer, Schedule))
                    End If
                    Result(Item.Key).Add(Item2.Key, Item2.Value)
                End If
            Next
        Next
        Return Result
    End Function

    Public Shared Sub BulkDelete(DeleteSchedules As List(Of Schedule), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        If DeleteSchedules.Count = 0 Then Exit Sub

        Dim CommandTextSB As New StringBuilder()
        CommandTextSB.Append("DELETE FROM Schedule WHERE {0}; ")
        CommandTextSB.Append("DELETE FROM SchedulePosition WHERE {0}; ")
        CommandTextSB.Append("DELETE FROM ScheduleFine WHERE {0}; ")
        CommandTextSB.Append("DELETE FROM ScheduleUserComment WHERE {0}; ")
        CommandTextSB.Append("DELETE FROM ScheduleUserCommentTeam WHERE {0}; ")
        CommandTextSB.Append("DELETE FROM ScheduleCommentTeam WHERE {0}; ")
        CommandTextSB.Append("DELETE FROM ScheduleCallup WHERE {0}; ")
        Dim CommandText As String = CommandTextSB.ToString()

        For N As Integer = 0 To DeleteSchedules.Count - 1 Step 1000
            Dim ParamsSB As New StringBuilder()
            For I As Integer = N To Math.Min(DeleteSchedules.Count, N + 1000) - 1
                If I <> N Then ParamsSB.Append(" OR ")
                ParamsSB.Append("(RegionId = @RegionId" & I)
                ParamsSB.Append(" AND ")
                ParamsSB.Append("ScheduleId = @ScheduleId" & I & ")")
            Next

            Using SqlCommand = New SqlCommand(CommandText.Replace("{0}", ParamsSB.ToString()), SQLConnection, SQLTransaction)
                For I As Integer = N To Math.Min(DeleteSchedules.Count, N + 1000) - 1
                    SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, DeleteSchedules(I).RegionId))
                    SqlCommand.Parameters.Add(New SqlParameter("ScheduleId" & I, DeleteSchedules(I).ScheduleId))
                Next
                SqlCommand.ExecuteNonQuery()
            End Using
        Next N
    End Sub

    Public Shared Sub BulkInsert(InsertSchedules As List(Of Schedule), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        If InsertSchedules.Count = 0 Then Return

        Dim ScheduleTable As New DataTable("Schedule")
        ScheduleTable.Columns.Add(New DataColumn("RegionId", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("ScheduleId", GetType(Integer)))
        ScheduleTable.Columns.Add(New DataColumn("VersionId", GetType(Integer)))
        ScheduleTable.Columns.Add(New DataColumn("GameNumber", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("LeagueId", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("HomeTeam", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("HomeTeamScore", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("AwayTeam", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("AwayTeamScore", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("GameDate", GetType(DateTime)))
        ScheduleTable.Columns.Add(New DataColumn("ParkId", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("CrewType", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("GameStatus", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("GameComment", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("GameType", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("LinkedRegionId", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("OfficialRegionId", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("ScorekeeperRegionId", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("LinkedScheduleId", GetType(Int32)))
        ScheduleTable.Columns.Add(New DataColumn("HomeTeamScoreExtra", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("AwayTeamScoreExtra", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("StatLinks", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("GameScore", GetType(String)))
        ScheduleTable.Columns.Add(New DataColumn("SupervisorRegionId", GetType(String)))

        For Each Schedule In InsertSchedules
            Dim Row = ScheduleTable.NewRow()
            Row("RegionId") = Schedule.RegionId
            Row("ScheduleId") = Schedule.ScheduleId
            Row("VersionId") = Schedule.VersionId
            Row("GameNumber") = Schedule.GameNumber
            Row("LeagueId") = Schedule.LeagueId
            Row("HomeTeam") = Schedule.HomeTeam
            Row("HomeTeamScore") = Schedule.HomeTeamScore
            Row("AwayTeam") = Schedule.AwayTeam
            Row("AwayTeamScore") = Schedule.AwayTeamScore
            Row("GameDate") = Schedule.GameDate
            Row("ParkId") = Schedule.ParkId
            Row("CrewType") = JsonConvert.SerializeObject(Schedule.CrewType)
            Row("GameStatus") = Schedule.GameStatus
            Row("GameComment") = Schedule.GameComment
            Row("GameType") = Schedule.GameType
            Row("LinkedRegionId") = Schedule.LinkedRegionId
            Row("OfficialRegionId") = Schedule.OfficialRegionId
            Row("ScorekeeperRegionId") = Schedule.ScorekeeperRegionId
            Row("LinkedScheduleId") = Schedule.LinkedScheduleId
            Row("HomeTeamScoreExtra") = JsonConvert.SerializeObject(Schedule.HomeTeamScoreExtra)
            Row("AwayTeamScoreExtra") = JsonConvert.SerializeObject(Schedule.AwayTeamScoreExtra)
            Row("StatLinks") = Schedule.StatLinks
            Row("GameScore") = JsonConvert.SerializeObject(Schedule.GameScore)
            Row("SupervisorRegionId") = Schedule.SupervisorRegionId

            ScheduleTable.Rows.Add(Row)
        Next

        Using BulkCopy As New SqlBulkCopy(SQLConnection, SqlBulkCopyOptions.Default, SQLTransaction)
            BulkCopy.DestinationTableName = "Schedule"
            BulkCopy.BulkCopyTimeout = 10000
            BulkCopy.WriteToServer(ScheduleTable)
        End Using
    End Sub

    Public Shared Sub BulkInsertVersions(InsertSchedules As List(Of Schedule), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        If InsertSchedules.Count = 0 Then Return

        Dim ScheduleVersionTable As New DataTable("ScheduleVersion")
        ScheduleVersionTable.Columns.Add(New DataColumn("RegionId", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("ScheduleId", GetType(Int32)))
        ScheduleVersionTable.Columns.Add(New DataColumn("VersionId", GetType(Int32)))
        ScheduleVersionTable.Columns.Add(New DataColumn("GameNumber", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("LeagueId", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("HomeTeam", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("HomeTeamScore", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("AwayTeam", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("AwayTeamScore", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("GameDate", GetType(DateTime)))
        ScheduleVersionTable.Columns.Add(New DataColumn("ParkId", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("CrewType", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("GameStatus", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("GameComment", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("IsDeleted", GetType(Boolean)))
        ScheduleVersionTable.Columns.Add(New DataColumn("DateAdded", GetType(DateTime)))
        ScheduleVersionTable.Columns.Add(New DataColumn("GameType", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("LinkedRegionId", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("LinkedScheduleId", GetType(Int32)))
        ScheduleVersionTable.Columns.Add(New DataColumn("OfficialRegionId", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("ScorekeeperRegionId", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("HomeTeamScoreExtra", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("AwayTeamScoreExtra", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("StatLinks", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("GameScore", GetType(String)))
        ScheduleVersionTable.Columns.Add(New DataColumn("SupervisorRegionId", GetType(String)))

        For Each Schedule In InsertSchedules
            Dim Row = ScheduleVersionTable.NewRow()
            Row("RegionId") = Schedule.RegionId
            Row("ScheduleId") = Schedule.ScheduleId
            Row("VersionId") = Schedule.VersionId
            Row("GameNumber") = Schedule.GameNumber
            Row("LeagueId") = Schedule.LeagueId
            Row("HomeTeam") = Schedule.HomeTeam
            Row("HomeTeamScore") = Schedule.HomeTeamScore
            Row("AwayTeam") = Schedule.AwayTeam
            Row("AwayTeamScore") = Schedule.AwayTeamScore
            Row("GameDate") = Schedule.GameDate
            Row("ParkId") = Schedule.ParkId
            Row("CrewType") = JsonConvert.SerializeObject(Schedule.CrewType)
            Row("GameStatus") = Schedule.GameStatus
            Row("GameComment") = Schedule.GameComment
            Row("IsDeleted") = Schedule.IsDeleted
            Row("DateAdded") = Schedule.DateAdded
            Row("GameType") = Schedule.GameType
            Row("LinkedRegionId") = Schedule.LinkedRegionId
            Row("LinkedScheduleId") = Schedule.LinkedScheduleId
            Row("OfficialRegionId") = Schedule.OfficialRegionId
            Row("ScorekeeperRegionId") = Schedule.ScorekeeperRegionId
            Row("HomeTeamScoreExtra") = JsonConvert.SerializeObject(Schedule.HomeTeamScoreExtra)
            Row("AwayTeamScoreExtra") = JsonConvert.SerializeObject(Schedule.AwayTeamScoreExtra)
            Row("StatLinks") = Schedule.StatLinks
            Row("GameScore") = JsonConvert.SerializeObject(Schedule.GameScore)
            Row("SupervisorRegionId") = Schedule.SupervisorRegionId
            ScheduleVersionTable.Rows.Add(Row)
        Next

        Using BulkCopy As New SqlBulkCopy(SQLConnection, SqlBulkCopyOptions.Default, SQLTransaction)
            BulkCopy.DestinationTableName = "ScheduleVersion"
            BulkCopy.BulkCopyTimeout = 10000
            BulkCopy.WriteToServer(ScheduleVersionTable)
        End Using
    End Sub


    Public Shared Function VersionComparer(V1 As Schedule, V2 As Schedule) As Integer
        Dim Comp As Integer = V1.RegionId.CompareTo(V2.RegionId)
        If Comp = 0 Then Comp = V1.ScheduleId.CompareTo(V2.ScheduleId)
        If Comp = 0 Then Comp = V1.VersionId.CompareTo(V2.VersionId)
        If Comp = 0 Then Comp = V1.OfficialId.CompareTo(V2.OfficialId)
        Return Comp
    End Function

    Public Shared Function VersionComparerSimple(V1 As Schedule, V2 As Schedule) As Integer
        Dim Comp As Integer = V1.RegionId.CompareTo(V2.RegionId)
        If Comp = 0 Then Comp = V1.ScheduleId.CompareTo(V2.ScheduleId)
        If Comp = 0 Then Comp = V1.VersionId.CompareTo(V2.VersionId)
        Return Comp
    End Function

    Public Shared Function BasicComparer(V1 As Schedule, V2 As Schedule) As Integer
        Dim Comp As Integer = V1.RegionId.CompareTo(V2.RegionId)
        If Comp = 0 Then Comp = V1.ScheduleId.CompareTo(V2.ScheduleId)
        If Comp = 0 Then Comp = V1.OfficialId.CompareTo(V2.OfficialId)
        If Comp = 0 Then Comp = V1.LinkedRegionId.CompareTo(V2.LinkedRegionId)
        Return Comp
    End Function

    Public Shared Function BasicComparerSimple(V1 As Schedule, V2 As Schedule) As Integer
        Dim Comp As Integer = V1.RegionId.CompareTo(V2.RegionId)
        If Comp = 0 Then Comp = V1.ScheduleId.CompareTo(V2.ScheduleId)
        Return Comp
    End Function

    Public Shared Function BasicComparerSimple2(V1 As Schedule, V2 As Schedule) As Integer
        Dim Comp As Integer = V1.RegionId.CompareTo(V2.RegionId)
        If Comp = 0 Then Comp = V1.ScheduleId.CompareTo(V2.ScheduleId)
        If Comp = 0 Then Comp = V1.OfficialId.CompareTo(V2.OfficialId)
        Return Comp
    End Function

    Public Shared Function DateComparer(V1 As Schedule, V2 As Schedule) As Integer
        Dim Comp As Integer = V1.GameDate.CompareTo(V2.GameDate)
        If Comp = 0 Then V1.ParkId.CompareTo(V2.ParkId)
        If Comp = 0 Then V1.RegionId.CompareTo(V2.RegionId)
        If Comp = 0 Then Comp = V1.ScheduleId.CompareTo(V2.ScheduleId)
        If Comp = 0 Then Comp = V1.OfficialId.CompareTo(V2.OfficialId)
        If Comp = 0 Then Comp = V1.LinkedRegionId.CompareTo(V2.LinkedRegionId)
        Return Comp
    End Function

    Public Shared Function LinkComparer(S1 As Schedule, S2 As Schedule) As Integer
        Dim Comp As Integer = S1.LinkedRegionId.CompareTo(S2.LinkedRegionId)
        If Comp = 0 Then Comp = S1.LinkedScheduleId.CompareTo(S2.LinkedScheduleId)
        Return Comp
    End Function

    Public Shared Function TempComparer(S1 As ScheduleTemp, S2 As Schedule)
        Dim Comp As Integer = S1.RegionId.CompareTo(S2.RegionId)
        If Comp = 0 Then Comp = S1.ScheduleId.CompareTo(S2.ScheduleId)
        If Comp = 0 Then Comp = S1.LinkedRegionId.CompareTo(S2.LinkedRegionId)
        Return Comp
    End Function

    Public Shared BasicSorter As New GenericIComparer(Of Schedule)(AddressOf BasicComparer)
    Public Shared BasicSorterSimple As New GenericIComparer(Of Schedule)(AddressOf BasicComparerSimple)
    Public Shared BasicSorterSimple2 As New GenericIComparer(Of Schedule)(AddressOf BasicComparerSimple2)
    Public Shared DateSorter As New GenericIComparer(Of Schedule)(AddressOf DateComparer)
    Public Shared LinkSorter = New GenericIComparer(Of Schedule)(AddressOf LinkComparer)
    Public Shared VersionSorter = New GenericIComparer(Of Schedule)(AddressOf VersionComparer)
    Public Shared VersionSorterSimple = New GenericIComparer(Of Schedule)(AddressOf VersionComparerSimple)

    Public Shared Sub ClearScheduleHelper(RegionId As String, Optional TSQLConnection As SqlConnection = Nothing, Optional TSQLTransaction As SqlTransaction = Nothing)

        Dim CommandText As String = <SQL>
DELETE FROM Schedule WHERE RegionId = @RegionId;
DELETE FROM ScheduleVersion WHERE RegionId = @RegionId;
                                    </SQL>.Value.ToString()

        If TSQLConnection Is Nothing Then
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.ExecuteNonQuery()
                    End Using
                End Using
            End Using
        Else
            Using SqlCommand As New SqlCommand(CommandText, TSQLConnection, TSQLTransaction)
                SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                SqlCommand.ExecuteNonQuery()
            End Using
        End If
    End Sub

    Public Sub MergeTeamData(RegionId As String, Optional KeepScheduleUserCommentTeam As Boolean = False)
        For Each ScheduleUserComment In ScheduleUserComments
            ScheduleUserComment.LinkedRegionId = ScheduleUserComment.RegionId
        Next

        For Each ScheduleCallUp In ScheduleCallUps
            ScheduleCallUp.LinkedRegionId = ScheduleCallUp.RegionId
        Next

        If KeepScheduleUserCommentTeam Then
            For I As Integer = ScheduleUserCommentTeams.Count - 1 To 0 Step -1
                If Not RegionId = ScheduleUserCommentTeams(I).LinkedRegionId Then
                    ScheduleUserCommentTeams.RemoveAt(I)
                End If
            Next
            For I As Integer = ScheduleCommentTeams.Count - 1 To 0 Step -1
                If Not RegionId = ScheduleCommentTeams(I).LinkedRegionId Then
                    ScheduleCommentTeams.RemoveAt(I)
                End If
            Next
        Else
            For Each ScheduleUserCommentTeam In ScheduleUserCommentTeams
                If RegionId = ScheduleUserCommentTeam.LinkedRegionId Then
                    ScheduleUserComments.Add(New ScheduleUserComment With {
                            .LinkedRegionId = ScheduleUserCommentTeam.LinkedRegionId,
                            .OfficialId = ScheduleUserCommentTeam.OfficialId,
                            .Comment = ScheduleUserCommentTeam.Comment
                        })
                End If
            Next
            For I As Integer = ScheduleCommentTeams.Count - 1 To 0 Step -1
                If Not RegionId = ScheduleCommentTeams(I).LinkedRegionId Then
                    ScheduleCommentTeams.RemoveAt(I)
                End If
            Next
            ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam)
        End If

        For I As Integer = ScheduleCallUps.Count - 1 To 0 Step -1
            If Not RegionId = ScheduleCallUps(I).LinkedRegionId Then
                ScheduleCallUps.RemoveAt(I)
            End If
        Next

        If Not KeepScheduleUserCommentTeam Then ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam)
    End Sub

    Public Shared Sub MergeTeamData(Schedule As List(Of ScheduleResult))

        For Each SR In Schedule
            Dim NewScheduleTeamConfirms As New List(Of ScheduleTeamConfirm)
            For Each ScheduleTeamConfirm In SR.ScheduleTeamConfirms
                If ScheduleTeamConfirm.LinkedRegionId = SR.Schedule(0).RegionId Then
                    NewScheduleTeamConfirms.Add(ScheduleTeamConfirm)
                End If
            Next
            SR.ScheduleTeamConfirms = NewScheduleTeamConfirms

            For Each S In SR.Schedule
                For I As Integer = S.ScheduleUserCommentTeams.Count - 1 To 0 Step -1
                    If S.ScheduleUserCommentTeams(I).LinkedRegionId <> S.RegionId Then S.ScheduleUserCommentTeams.RemoveAt(I)
                Next

                For I As Integer = S.ScheduleCommentTeams.Count - 1 To 0 Step -1
                    If S.ScheduleCommentTeams(I).LinkedRegionId <> S.RegionId Then S.ScheduleCommentTeams.RemoveAt(I)
                Next


                For I As Integer = S.ScheduleCallUps.Count - 1 To 0 Step -1
                    If S.ScheduleCallUps(I).LinkedRegionId <> S.RegionId Then S.ScheduleCallUps.RemoveAt(I)
                Next
            Next

            For I As Integer = 0 To SR.Schedule.Count - 2
                If I = SR.Schedule.Count - 1 Then Exit For
                Dim S1 = SR.Schedule(I)
                Dim S2 = SR.Schedule(I + 1)

                'If Not S1.IsDifferent(S2) Then
                '    SR.Schedule.RemoveAt(I + 1)
                '    I = -1
                'End If
            Next

            For I As Integer = 0 To SR.Schedule.Count - 1
                SR.Schedule(I).VersionId = I + 1

                For Each ScheduleCallup In SR.Schedule(I).ScheduleCallUps
                    ScheduleCallup.LinkedRegionId = ScheduleCallup.RegionId
                Next

                MergeUserCommentData(SR.Schedule(I).ScheduleUserComments, SR.Schedule(I).ScheduleUserCommentTeams)
            Next
        Next
    End Sub

    Public Shared Sub MergeUserCommentData(ByRef ScheduleUserComments As List(Of ScheduleUserComment), ByRef ScheduleUserCommentTeams As List(Of ScheduleUserCommentTeam))
        For Each ScheduleUserComment In ScheduleUserComments
            ScheduleUserComment.LinkedRegionId = ScheduleUserComment.RegionId
        Next

        For Each ScheduleUserCommentTeam In ScheduleUserCommentTeams
            ScheduleUserComments.Add(New ScheduleUserComment With {
                .LinkedRegionId = ScheduleUserCommentTeam.LinkedRegionId,
                .OfficialId = ScheduleUserCommentTeam.OfficialId,
                .Comment = ScheduleUserCommentTeam.Comment,
                .IsDeleted = ScheduleUserCommentTeam.IsDeleted
            })
        Next

        ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam)
    End Sub

    Public Function GetPayForUser(Region As RegionProperties, RegionLeagues As List(Of RegionLeaguePayContracted), OfficialId As String) As Decimal
        Dim Result As Decimal = 0

        OfficialId = OfficialId.ToLower
        Dim GameStatus = Me.GameStatus
        If GameStatus = "normal" Then GameStatus = ""

        Dim Positions = RegionLeaguePayContracted.GetSportPositionOrder()(Region.Sport)
        Dim PositionsScorekeeper = RegionLeaguePayContracted.GetSportPositionOrderScorekeeper()(Region.Sport)
        Dim PositionsSupervisor = RegionLeaguePayContracted.GetSportPositionOrderSupervisor()(Region.Sport)

        Dim RegionLeagueIndex = RegionLeagues.BinarySearch(New RegionLeaguePayContracted With {.RegionId = Me.RegionId, .LeagueId = Me.LeagueId.ToLower}, RegionLeaguePayContracted.BasicSorter)
        Dim RegionLeague As RegionLeaguePayContracted = If(RegionLeagueIndex >= 0, RegionLeagues(RegionLeagueIndex), Nothing)

        If Me.CrewType.ContainsKey("umpire") Then
            Dim CrewType = Me.CrewType("umpire").ToLower

            For Each SchedulePosition In SchedulePositions
                If SchedulePosition.OfficialId.ToLower = OfficialId Then
                    Dim PositionId = SchedulePosition.PositionId.ToLower

                    If Positions.Contains(PositionId) Then
                        If RegionLeague IsNot Nothing Then
                            If RegionLeague.Pay.ContainsKey(GameStatus) Then
                                Dim GameStatusPay = RegionLeague.Pay(GameStatus)
                                If GameStatusPay.ContainsKey(CrewType) Then
                                    Dim CrewTypePay = GameStatusPay(CrewType)
                                    If CrewTypePay.ContainsKey(PositionId) Then
                                        Result += CrewTypePay(PositionId)
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If
            Next
        End If

        If Me.CrewType.ContainsKey("scorekeeper") Then
            Dim CrewType = Me.CrewType("scorekeeper")

            For Each SchedulePosition In SchedulePositions
                If SchedulePosition.OfficialId.ToLower = OfficialId Then
                    Dim PositionId = SchedulePosition.PositionId.ToLower

                    If PositionsScorekeeper.Contains(PositionId) Then
                        If RegionLeague IsNot Nothing Then
                            If RegionLeague.Pay.ContainsKey(GameStatus) Then
                                Dim GameStatusPay = RegionLeague.Pay(GameStatus)
                                If GameStatusPay.ContainsKey(CrewType) Then
                                    Dim CrewTypePay = GameStatusPay(CrewType)
                                    If CrewTypePay.ContainsKey(PositionId) Then
                                        Result += CrewTypePay(PositionId)
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If
            Next
        End If

        If Me.CrewType.ContainsKey("supervisor") Then
            Dim CrewType = Me.CrewType("supervisor")

            For Each SchedulePosition In SchedulePositions
                If SchedulePosition.OfficialId.ToLower = OfficialId Then
                    Dim PositionId = SchedulePosition.PositionId.ToLower

                    If PositionsSupervisor.Contains(PositionId) Then
                        If RegionLeague IsNot Nothing Then
                            If RegionLeague.Pay.ContainsKey(GameStatus) Then
                                Dim GameStatusPay = RegionLeague.Pay(GameStatus)
                                If GameStatusPay.ContainsKey(CrewType) Then
                                    Dim CrewTypePay = GameStatusPay(CrewType)
                                    If CrewTypePay.ContainsKey(PositionId) Then
                                        Result += CrewTypePay(PositionId)
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If
            Next
        End If

        For Each ScheduleFine In ScheduleFines
            If ScheduleFine.OfficialId.ToLower = OfficialId Then
                Result -= ScheduleFine.Amount
            End If
        Next

        Return Result
    End Function

    Public Function ContainsUsername(Username As String, RegionUsers As List(Of RegionUser)) As Boolean
        If Username = "" Then Return true
        For Each SchedulePosition In SchedulePositions
            For Each RegionUser In RegionUsers
                If RegionUser.RegionId = RegionId AndAlso RegionUser.RealUsername = Username AndAlso SchedulePosition.OfficialId.ToLower = RegionUser.Username.ToLower Then Return True
            Next
        Next

        For Each ScheduleFine In ScheduleFines
            For Each RegionUser In RegionUsers
                If RegionUser.RegionId = RegionId AndAlso RegionUser.RealUsername = Username AndAlso ScheduleFine.OfficialId.ToLower = RegionUser.Username.ToLower Then Return True
            Next
        Next
        Return False
    End Function

    Public Function ContainsOfficialId(OfficialId As String) As Boolean
        For Each SchedulePosition In SchedulePositions
            If SchedulePosition.OfficialId.Trim.ToLower = OfficialId Then Return True
        Next

        For Each ScheduleFine In ScheduleFines
            If ScheduleFine.OfficialId.Trim.ToLower = OfficialId Then Return True
        Next

        Return False
    End Function

    Public Function ContainsOfficialIdOnPosition(OfficialId As String) As Boolean
        For Each SchedulePosition In SchedulePositions
            If SchedulePosition.OfficialId.Trim.ToLower = OfficialId Then Return True
        Next

        Return False
    End Function

    Public Sub ClearPostionsAndFines()
        For Each SchedulePosition In SchedulePositions
            SchedulePosition.OfficialId = ""
        Next
        For Each ScheduleFine In ScheduleFines
            ScheduleFine.OfficialId = ""
        Next
    End Sub

    Public Shared Function FilterScheduleBasedOnLeagueList(Schedule As List(Of Schedule), RegionLeagues As List(Of List(Of String))) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        For Each ScheduleItem In Schedule
            Dim IncludeGame As Boolean = False
            If RegionLeagues.Count = 0 Then
                IncludeGame = True
            Else
                For Each RegionLeague In RegionLeagues
                    If ScheduleItem.RegionId.ToLower = RegionLeague(0) AndAlso ScheduleItem.LeagueId.ToLower = RegionLeague(1) Then
                        IncludeGame = True
                    End If
                Next
            End If
            If IncludeGame Then
                Result.Add(ScheduleItem)
            End If
        Next

        Return Result
    End Function

    Public Shared Function FilterScheduleBasedOnRegionIdOfficialId(Schedule As List(Of Schedule), RegionUsers As List(Of RegionUser), Username As String, RegionId As String, OfficialId As String) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        For Each ScheduleItem In Schedule
            Dim ShouldAdd As Boolean = True
            If RegionId <> "allregions" Then
                If ScheduleItem.RegionId <> RegionId Then
                    ShouldAdd = False
                End If
            End If


            If OfficialId <> "allusers" Then
                Dim RegionUser = RegionUsers.Find(Function(V1)
                                                      Return V1.RegionId = ScheduleItem.RegionId AndAlso ScheduleItem.OfficialId = V1.Username AndAlso (V1.LastName + ", " + V1.FirstName).ToLower = OfficialId AndAlso V1.RealUsername = Username
                                                  End Function)

                If RegionUser Is Nothing Then
                    ShouldAdd = False
                End If
            End If

            If ShouldAdd Then
                Result.Add(ScheduleItem)
            End If
        Next

        Return Result
    End Function

    Public Shared Function GetUniqueOfficialIdsFromScheduleSingle(ScheduleItem As Schedule) As List(Of String)
        Dim UniqueIdList As New List(Of String)

        For Each SchedulePosition In ScheduleItem.SchedulePositions
            Dim OL = SchedulePosition.OfficialId.ToLower.Trim
            If OL <> "" Then
                If Not UniqueIdList.Contains(OL) Then
                    UniqueIdList.Add(OL)
                End If
            End If
        Next

        For Each ScheduleFine In ScheduleItem.ScheduleFines
            Dim OL = ScheduleFine.OfficialId.ToLower.Trim
            If OL <> "" Then
                If Not UniqueIdList.Contains(OL) Then
                    UniqueIdList.Add(OL)
                End If
            End If
        Next

        For Each ScheduleUserComment In ScheduleItem.ScheduleUserComments
            Dim OL = ScheduleUserComment.OfficialId.ToLower.Trim
            If OL <> "" Then
                If Not UniqueIdList.Contains(OL) Then
                    UniqueIdList.Add(OL)
                End If
            End If
        Next

        Return UniqueIdList
    End Function

    Public Shared Function GetUniqueOfficialIdsFromSchedule(Schedule As List(Of Schedule), RegionUsers As List(Of RegionUser)) As List(Of String)
        Dim UniqueIdList As New List(Of String)

        For Each ScheduleItem In Schedule
            For Each SchedulePosition In ScheduleItem.SchedulePositions
                Dim OL = SchedulePosition.OfficialId.ToLower.Trim
                If OL <> "" Then
                    If Not UniqueIdList.Contains(OL) Then
                        UniqueIdList.Add(OL)
                    End If
                End If
            Next

            For Each ScheduleFine In ScheduleItem.ScheduleFines
                Dim OL = ScheduleFine.OfficialId.ToLower.Trim
                If OL <> "" Then
                    If Not UniqueIdList.Contains(OL) Then
                        UniqueIdList.Add(OL)
                    End If
                End If
            Next

            For Each ScheduleUserComment In ScheduleItem.ScheduleUserComments
                Dim OL = ScheduleUserComment.OfficialId.ToLower.Trim
                If OL <> "" Then
                    If Not UniqueIdList.Contains(OL) Then
                        UniqueIdList.Add(OL)
                    End If
                End If
            Next
        Next

        Return UniqueIdList
    End Function

    Public Shared Function GetUniqueOfficialIdsFromScheduleTeam(Schedule As List(Of Schedule), RegionUsers As List(Of RegionUser)) As List(Of String)
        Dim UniqueIdList As New List(Of String)

        Dim RegionId = Schedule(0).RegionId

        For Each RegionUser In RegionUsers
            If RegionUser.RegionId = RegionId AndAlso (RegionUser.IsCoach OrElse RegionUser.IsPlayer) AndAlso Not UniqueIdList.Contains(RegionUser.Username) Then
                UniqueIdList.Add(RegionUser.Username)
            End If
        Next

        For Each ScheduleItem In Schedule
            For Each ScheduleCallup In ScheduleItem.ScheduleCallUps
                If Not UniqueIdList.Contains(ScheduleCallup.Username.ToLower) Then
                    UniqueIdList.Add(ScheduleCallup.Username.ToLower)
                End If
            Next
        Next

        Return UniqueIdList
    End Function

    Public Class ScheduleAndPay
        Public Property Schedule As Schedule
        Public Property Pay As Decimal

        Public Shared BasicSorter As New GenericIComparer(Of ScheduleAndPay)(Function(V1, V2)
                                                                                 Dim Comp As Integer = V1.Schedule.RegionId.CompareTo(V2.Schedule.RegionId)
                                                                                 If Comp = 0 Then Comp = V1.Schedule.ScheduleId.CompareTo(V2.Schedule.ScheduleId)
                                                                                 If Comp = 0 Then Comp = V1.Schedule.OfficialId.CompareTo(V2.Schedule.OfficialId)
                                                                                 If Comp = 0 Then Comp = V1.Schedule.LinkedRegionId.CompareTo(V2.Schedule.LinkedRegionId)
                                                                                 Return Comp
                                                                             End Function)

        Public Shared DateSorter As New GenericIComparer(Of ScheduleAndPay)(Function(V1, V2)
                                                                                Dim Comp As Integer = V1.Schedule.GameDate.CompareTo(V2.Schedule.GameDate)
                                                                                If Comp = 0 Then V1.Schedule.ParkId.CompareTo(V2.Schedule.ParkId)
                                                                                If Comp = 0 Then V1.Schedule.RegionId.CompareTo(V2.Schedule.RegionId)
                                                                                If Comp = 0 Then Comp = V1.Schedule.ScheduleId.CompareTo(V2.Schedule.ScheduleId)
                                                                                If Comp = 0 Then Comp = V1.Schedule.OfficialId.CompareTo(V2.Schedule.OfficialId)
                                                                                If Comp = 0 Then Comp = V1.Schedule.LinkedRegionId.CompareTo(V2.Schedule.LinkedRegionId)
                                                                                Return Comp
                                                                            End Function)
    End Class

    Public Class UniqueUserSchedule
        Public Property Schedule As New List(Of ScheduleAndPay)
        Public Property RegionUsers As New List(Of RegionUser)
        Public Property TotalPay As Decimal = 0
        Public Property GameCount As Integer = 0

        Public Shared NameSorter = New GenericIComparer(Of UniqueUserSchedule)(Function(V1, V2)
                                                                                   Dim Comp As Integer = V1.RegionUsers(0).LastName.ToLower.CompareTo(V2.RegionUsers(0).LastName.ToLower)
                                                                                   If Comp = 0 Then Comp = V1.RegionUsers(0).FirstName.ToLower.CompareTo(V2.RegionUsers(0).FirstName.ToLower)
                                                                                   Return Comp
                                                                               End Function)
    End Class

    Public Class GetUniqueRegionOfficialsFromScheduleResult
        Public Property UniqueRegionUsers As Dictionary(Of String, Dictionary(Of String, RegionUser))
        Public Property UniqueUsersSchedule As List(Of UniqueUserSchedule)
        Public Property PlaceHoldersSchedule As List(Of PlaceholderAndSchedule)
    End Class

    Public Class PlaceholderAndSchedule
        Public Property RegionId As String
        Public Property OfficialId As String
        Public Property Schedule As List(Of ScheduleAndPay)
        Public Property TotalPay As Decimal = 0
        Public Property GameCount As Integer = 0

        Public Shared BasicSorter = New GenericIComparer(Of PlaceholderAndSchedule)(Function(V1, V2)
                                                                                        Dim Comp As Integer = V1.OfficialId.CompareTo(V2.OfficialId)
                                                                                        If Comp = 0 Then Comp = V1.RegionId.CompareTo(V2.RegionId)
                                                                                        Return Comp
                                                                                    End Function)
    End Class

    Public Shared Function GetUniqueRegionOfficialsFromSchedule(Schedule As List(Of Schedule), RegionUsers As List(Of RegionUser), Regions As List(Of RegionProperties), RegionLeagues As List(Of RegionLeaguePayContracted)) As GetUniqueRegionOfficialsFromScheduleResult
        Dim UniqueRegionUsers As New Dictionary(Of String, Dictionary(Of String, RegionUser))
        Dim UniqueUsersSchedule As New Dictionary(Of String, List(Of UniqueUserSchedule))
        Dim PlaceHoldersSchedule As New Dictionary(Of String, PlaceholderAndSchedule)

        Dim UniqueUsersSchededuleList As New List(Of UniqueUserSchedule)
        Dim PlaceholdersScheduleList As New List(Of PlaceholderAndSchedule)

        For Each ScheduleItem In Schedule
            Dim UniqueOfficialIds = GetUniqueOfficialIdsFromScheduleSingle(ScheduleItem)

            Dim RegionIndex = Regions.BinarySearch(New RegionProperties With {.RegionId = ScheduleItem.RegionId}, RegionProperties.BasicSorter)

            Dim Region As RegionProperties = If(RegionIndex >= 0, Regions(RegionIndex), Nothing)


            For Each UniqueOfficialId In UniqueOfficialIds
                Dim RegionUser = RegionUsers.Find(Function(V1) V1.RegionId = ScheduleItem.RegionId AndAlso V1.Username = UniqueOfficialId)

                Dim HasItem As Boolean = True
                If Not UniqueRegionUsers.ContainsKey(ScheduleItem.RegionId) Then
                    HasItem = False
                    UniqueRegionUsers.Add(ScheduleItem.RegionId, New Dictionary(Of String, RegionUser))
                End If
                If Not UniqueRegionUsers(ScheduleItem.RegionId).ContainsKey(UniqueOfficialId) Then
                    HasItem = False
                    UniqueRegionUsers(ScheduleItem.RegionId).Add(UniqueOfficialId, RegionUser)
                End If

                Dim Pay = ScheduleItem.GetPayForUser(Region, RegionLeagues, UniqueOfficialId)

                Dim DoGameCount = (ScheduleItem.GameStatus = "" OrElse ScheduleItem.GameStatus = "normal") AndAlso ScheduleItem.SchedulePositions.Find(Function(SP) SP.OfficialId.Trim().ToLower = UniqueOfficialId) IsNot Nothing

                If RegionUser Is Nothing Then
                    Dim Index As String = ScheduleItem.RegionId & ", " & UniqueOfficialId
                    If Not PlaceHoldersSchedule.ContainsKey(Index) Then
                        PlaceHoldersSchedule.Add(Index, New PlaceholderAndSchedule With {.RegionId = ScheduleItem.RegionId, .OfficialId = UniqueOfficialId, .Schedule = New List(Of ScheduleAndPay)})
                    End If
                    PlaceHoldersSchedule(Index).Schedule.Add(New ScheduleAndPay With {.Schedule = ScheduleItem, .Pay = Pay})
                    PlaceHoldersSchedule(Index).TotalPay += Pay
                    If DoGameCount Then PlaceHoldersSchedule(Index).GameCount += 1
                Else
                    Dim Index As String = RegionUser.LastName.Trim.ToLower() & ", " & RegionUser.FirstName.Trim.ToLower()
                    If Not UniqueUsersSchedule.ContainsKey(Index) Then
                        UniqueUsersSchedule.Add(Index, New List(Of UniqueUserSchedule))
                    End If

                    Dim ResultItem = UniqueUsersSchedule(Index)
                    If HasItem Then
                        For I As Integer = 0 To ResultItem.Count - 1
                            Dim FoundItem As Boolean = False
                            For Each RU In ResultItem(I).RegionUsers
                                If RU.RegionId = RegionUser.RegionId AndAlso RU.Username = RegionUser.Username Then
                                    ResultItem(I).Schedule.Add(New ScheduleAndPay With {.Schedule = ScheduleItem, .Pay = Pay})
                                    ResultItem(I).TotalPay += Pay
                                    If DoGameCount Then ResultItem(I).GameCount += 1
                                    FoundItem = True
                                    Exit For
                                End If
                            Next
                            If FoundItem Then Exit For
                        Next
                    Else
                        Dim MatchIndex As New List(Of Integer)
                        For I As Integer = 0 To ResultItem.Count - 1
                            For Each RU In ResultItem(I).RegionUsers
                                If RU.RealUsername = RegionUser.RealUsername OrElse ((RU.RealUsername = "" OrElse RegionUser.RealUsername = "") AndAlso RU.Email.ToLower = RegionUser.Email.ToLower) Then
                                    MatchIndex.Add(I)
                                    Exit For
                                End If
                            Next
                        Next
                        If MatchIndex.Count = 0 Then
                            ResultItem.Add(New UniqueUserSchedule)
                            ResultItem.Last().Schedule.Add(New ScheduleAndPay With {.Schedule = ScheduleItem, .Pay = Pay})
                            ResultItem.Last().RegionUsers.Add(RegionUser)
                            ResultItem.Last().TotalPay += Pay
                            If DoGameCount Then ResultItem.Last().GameCount += 1
                        Else
                            ResultItem(MatchIndex(0)).Schedule.Add(New ScheduleAndPay With {.Schedule = ScheduleItem, .Pay = Pay})
                            ResultItem(MatchIndex(0)).RegionUsers.Add(RegionUser)
                            ResultItem(MatchIndex(0)).TotalPay += Pay
                            If DoGameCount Then ResultItem(MatchIndex(0)).GameCount += 1
                            For I As Integer = MatchIndex.Count - 1 To 1 Step -1
                                ResultItem(MatchIndex(I - 1)).Schedule.AddRange(ResultItem(MatchIndex(I)).Schedule)
                                ResultItem(MatchIndex(I - 1)).RegionUsers.AddRange(ResultItem(MatchIndex(I)).RegionUsers)
                                ResultItem.RemoveAt(MatchIndex(I))
                            Next
                        End If
                    End If

                End If
            Next
        Next

        For Each UniqueUsersScheduleItem In UniqueUsersSchedule
            For Each UniqueUsersScheduleUser In UniqueUsersScheduleItem.Value
                UniqueUsersScheduleUser.Schedule.Sort(ScheduleAndPay.DateSorter)
                UniqueUsersSchededuleList.Add(UniqueUsersScheduleUser)
            Next
        Next

        For Each PlaceHoldersScheduleItem In PlaceHoldersSchedule
            PlaceHoldersScheduleItem.Value.Schedule.Sort(ScheduleAndPay.DateSorter)
            PlaceholdersScheduleList.Add(PlaceHoldersScheduleItem.Value)
        Next

        UniqueUsersSchededuleList.Sort(UniqueUserSchedule.NameSorter)

        PlaceholdersScheduleList.Sort(PlaceholderAndSchedule.BasicSorter)

        Return New GetUniqueRegionOfficialsFromScheduleResult With {
            .UniqueRegionUsers = UniqueRegionUsers,
            .UniqueUsersSchedule = UniqueUsersSchededuleList,
            .PlaceHoldersSchedule = PlaceholdersScheduleList
        }
    End Function

    Public Shared Function AnyOfRegionIdsHaveTemp(Username As String, RegionIds As List(Of String), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As Boolean
        Dim Result As Boolean = False

        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim CommandText As String = "Select IsSubmitted FROM ScheduleTempSubmitted WHERE RegionId In ({0}) And UserSubmitId = @Username".Replace("{0}", RegionIdParams.ToString)

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Result = Result Or Reader.GetBoolean(0)
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Function IsCancelled() As Boolean
        Return GameStatus = "cancelled" OrElse GameStatus = "weather"
    End Function

    Public Shared Function GetMasterScheduleFromRegionHelper(FilterAdmin As Boolean, Username As String, RegionId As String, StartDate As Date, EndDate As Date, GetGlobalAvail As Boolean, IsAvailableSpots As Boolean, IsSimple As Boolean, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction, ByRef UniqueRegionIdAdmins As List(Of String), ByRef Teams As List(Of Team), ByRef RegionUsers As List(Of RegionUser), ByRef ScheduleTemps As List(Of ScheduleTemp), ByRef ScheduleRequests As List(Of ScheduleRequest), ByRef ScheduleBookOffs As List(Of ScheduleBookOff), ByRef RegionUsersAdmin As List(Of RegionUser), ByRef RegionProperties As List(Of RegionProperties), ByRef RegionParks As List(Of Park), ByRef RegionLeagues As List(Of RegionLeaguePayContracted), ByRef TeamRegionIds As List(Of String), ByRef RefereeRegionIds As List(Of String), ByRef RefereeRegionIdAdmins As List(Of String), ByRef OfficialRegions As List(Of OfficialRegion), ByRef DisplayingScheduleRegionIds As List(Of String), ByRef IgnoredSchedule As List(Of ScheduleIgnore), ByRef LeagueRegionIds As List(Of String), ByRef LinkedLeagueIds As List(Of String), ByRef Schedule As List(Of ScheduleResult), ByRef Holidays As List(Of Holiday), ByRef Availabilities As List(Of UserAvailability), ByRef StatGames As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, Dictionary(Of Integer, List(Of StatGame)))))), ByRef FullSchedule As List(Of Schedule)) As ErrorObject
        Dim UniqueRegionIds As New List(Of String)
        Dim Assignor As RegionUser = Nothing

        If IsAvailableSpots Then
            UniqueRegionIds = RegionUser.GetAllMyRegionsIds(Username, SQLConnection, SQLTransaction)

            If RegionId <> "allregions" AndAlso RegionId <> "alleditableregions" AndAlso Not RegionId.Contains(",") Then
                If Not UniqueRegionIds.Contains(RegionId) Then
                    UniqueRegionIds.Add(RegionId)
                End If
            End If
        Else
            If RegionId = "allregions" OrElse RegionId.Contains(",") Then
                UniqueRegionIds = RegionUser.GetAllMyRegionsIds(Username, SQLConnection, SQLTransaction)
                If RegionId.Contains(",") Then
                    Dim RegionIdSplit = RegionId.ToLower().Split(",")

                    Dim TUniqueRegionIds As New List(Of String)
                    For Each UniqueRegionId In UniqueRegionIds
                        If RegionIdSplit.Contains(UniqueRegionId) Then
                            TUniqueRegionIds.Add(UniqueRegionId)
                        End If
                    Next
                    UniqueRegionIds = TUniqueRegionIds
                End If
            ElseIf RegionId = "alleditableregions" Then
                UniqueRegionIds = RegionUser.GetAllRegionIdsImExecutiveFor(Username, SQLConnection, SQLTransaction)
            Else
                UniqueRegionIds = {RegionId}.ToList()
            End If
        End If

        If UniqueRegionIds.Count = 0 Then Return Nothing

        Dim SeasonIds As New List(Of String)
        For Each UniqueRegionId In UniqueRegionIds
            Dim SeasonId = Right(UniqueRegionId, 4)
            If Not SeasonIds.Contains(SeasonId) Then SeasonIds.Add(SeasonId)
        Next

        RegionUsers = RegionUser.LoadAllInRegionIdsHelper(UniqueRegionIds, Username, SQLConnection, SQLTransaction)
        RegionProperties = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)

        If IsAvailableSpots Then
            Dim UserInRegion As Boolean = False
            For Each RU In RegionUsers
                If RU.RealUsername = Username AndAlso Username <> "" Then
                    UserInRegion = True
                End If
            Next
            If Username = "superadmin" Then UserInRegion = True

            If Not UserInRegion Then
                Dim TRegion = RegionProperties.Find(Function(RP) RP.RegionId = RegionId)
                If TRegion Is Nothing OrElse Not TRegion.ShowAvailableSpotsToNonMembers Then
                    Return New ErrorObject("InvalidPermissions")
                End If
            End If
        End If

        If FilterAdmin Then
            For Each UniqueRegionId In UniqueRegionIds
                If Username = "superadmin" Then
                    UniqueRegionIdAdmins.Add(UniqueRegionId)
                Else
                    For Each RegionUser In RegionUsers
                        If RegionUser.RegionId = UniqueRegionId And RegionUser.RealUsername = Username AndAlso (RegionUser.IsExecutive) Then
                            UniqueRegionIdAdmins.Add(UniqueRegionId)
                            Exit For
                        End If
                    Next
                End If
            Next
        Else
            UniqueRegionIdAdmins.AddRange(UniqueRegionIds)
        End If

        For Each RegionUser In RegionUsers
            If UniqueRegionIdAdmins.Contains(RegionUser.RegionId) Then
                RegionUsersAdmin.Add(RegionUser)
            End If
        Next

        For Each UniqueRegionIdAdmin In UniqueRegionIdAdmins
            Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = UniqueRegionIdAdmin)
            If RegionProperty.EntityType = "referee" Then
                RefereeRegionIdAdmins.Add(UniqueRegionIdAdmin)
            End If
        Next

        If Not IsAvailableSpots AndAlso RegionId <> "alleditableregions" AndAlso FilterAdmin Then
            Dim NewUniqueRegionIds As New List(Of String)
            For Each UniqueRegionId In UniqueRegionIds
                Dim DidAddUniqueRegionId As Boolean = False
                Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = UniqueRegionId)
                If RegionProperty IsNot Nothing Then
                    If RegionProperty.ShowFullScheduleToNonMembers Then
                        NewUniqueRegionIds.Add(UniqueRegionId)
                        DidAddUniqueRegionId = True
                    Else
                        If Username = "superadmin" Then
                            NewUniqueRegionIds.Add(UniqueRegionId)
                            DidAddUniqueRegionId = True
                        Else
                            For Each RegionUser In RegionUsers
                                If RegionUser.RegionId = UniqueRegionId AndAlso RegionUser.RealUsername = Username AndAlso (RegionUser.IsExecutive OrElse RegionUser.CanViewMasterSchedule) Then
                                    NewUniqueRegionIds.Add(UniqueRegionId)
                                    DidAddUniqueRegionId = True
                                    Exit For
                                End If
                            Next
                        End If
                    End If
                End If

                If Not DidAddUniqueRegionId Then
                    Dim NewRegionUsers As New List(Of RegionUser)
                    For Each RegionUser In RegionUsers
                        If RegionUser.RegionId <> UniqueRegionId Then NewRegionUsers.Add(RegionUser)
                    Next
                    RegionUsers = NewRegionUsers

                    Dim NewRegionProperties As New List(Of RegionProperties)
                    For Each RegionProperty In RegionProperties
                        If RegionProperty.RegionId <> UniqueRegionId Then NewRegionProperties.Add(RegionProperty)
                    Next
                    RegionProperties = NewRegionProperties
                End If
            Next
            UniqueRegionIds = NewUniqueRegionIds
        End If

        If IsAvailableSpots Then
            If RegionId = "allregions" Then
                For Each RegionProperty In RegionProperties
                    If RegionProperty.EntityType = "referee" Then
                        DisplayingScheduleRegionIds.Add(RegionProperty.RegionId)
                    End If
                Next
            ElseIf RegionId = "alleditableregions" Then
                For Each RegionProperty In RegionProperties
                    If RegionProperty.EntityType = "referee" Then
                        If Username = "superadmin" Then
                            DisplayingScheduleRegionIds.Add(RegionProperty.RegionId)
                        Else
                            For Each RegionUser In RegionUsers
                                If RegionUser.RegionId = RegionProperty.RegionId AndAlso RegionUser.RealUsername = Username AndAlso RegionUser.IsExecutive Then
                                    DisplayingScheduleRegionIds.Add(RegionProperty.RegionId)
                                    Exit For
                                End If
                            Next
                        End If
                    End If
                Next
            Else
                Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = RegionId)
                If RegionProperty IsNot Nothing AndAlso RegionProperty.EntityType = "referee" Then
                    DisplayingScheduleRegionIds.Add(RegionId)
                End If
            End If
        Else
            DisplayingScheduleRegionIds.AddRange(UniqueRegionIds)
        End If

        'No Need to send myschedule because user can't add games if he is not admin anyways
        If UniqueRegionIds.Count = 0 Then Return Nothing

        Dim LastFetchedRegionProperties As New List(Of String)
        LastFetchedRegionProperties.AddRange(UniqueRegionIds)

        For Each RegionProperty In RegionProperties
            If RegionProperty.EntityType = "team" Then TeamRegionIds.Add(RegionProperty.RegionId)
            If RegionProperty.EntityType = "referee" Then RefereeRegionIds.Add(RegionProperty.RegionId)
            If RegionProperty.EntityType = "league" Then LeagueRegionIds.Add(RegionProperty.RegionId)
        Next

        Dim LastFetchedTeamAndOfficialIds As New List(Of String)
        LastFetchedTeamAndOfficialIds.AddRange(UniqueRegionIds)

        OfficialRegions = OfficialRegion.GetOfficialRegionsInRegionsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)
        Teams = Team.GetTeamsInRegionsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)

        If Not IsAvailableSpots AndAlso Not IsSimple Then
            'Get Linked Team and Official Region Ids
            For Each OfficialRegion In OfficialRegions
                If OfficialRegion.RealOfficialRegionId <> "" AndAlso LeagueRegionIds.Contains(OfficialRegion.RegionId) AndAlso Not UniqueRegionIds.Contains(OfficialRegion.RealOfficialRegionId) Then
                    UniqueRegionIds.Add(OfficialRegion.RealOfficialRegionId)
                End If
            Next

            For Each Team In Teams
                If Team.RealTeamId <> "" AndAlso LeagueRegionIds.Contains(Team.RegionId) AndAlso Not UniqueRegionIds.Contains(Team.RealTeamId) Then
                    UniqueRegionIds.Add(Team.RealTeamId)
                End If
            Next
        End If

        'Get OthersRegionIds

        Dim OtherRegionIds As New List(Of String)
        If Not IsAvailableSpots AndAlso RegionUsersAdmin.Count > 0 AndAlso Not IsSimple Then
            OtherRegionIds = RegionUser.GetAllRegionIdsFromRealUsernames(RegionUsersAdmin, SQLConnection, SQLTransaction)
            For I As Integer = OtherRegionIds.Count - 1 To 0 Step -1
                Dim SeasonId = Right(OtherRegionIds(I), 4)
                If Not SeasonIds.Contains(SeasonId) Then
                    OtherRegionIds.RemoveAt(I)
                End If
            Next
        End If

        For Each OtherRegionId In OtherRegionIds
            If Not UniqueRegionIds.Contains(OtherRegionId) Then
                UniqueRegionIds.Add(OtherRegionId)
            End If
        Next

        Dim LastFetchedRegionLeagueIds As New List(Of String)
        LastFetchedRegionLeagueIds.AddRange(UniqueRegionIds)

        If UniqueRegionIds.Count > 0 Then
            RegionLeagues = RegionLeague.GetRegionLeaguesPayContractedRegionIdsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)
        End If

        LinkedLeagueIds = New List(Of String)
        For Each RegionLeague In RegionLeagues
            If RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked AndAlso (UniqueRegionIds.Contains(RegionLeague.RegionId) OrElse UniqueRegionIds.Contains(RegionLeague.RegionId)) AndAlso Not LinkedLeagueIds.Contains(RegionLeague.RealLeagueId) Then
                LinkedLeagueIds.Add(RegionLeague.RealLeagueId)
            End If
        Next

        For Each LinkedLeagueId In LinkedLeagueIds
            If Not UniqueRegionIds.Contains(LinkedLeagueId) Then
                UniqueRegionIds.Add(LinkedLeagueId)
            End If
        Next

        Dim NotYetFetchedRegionLeagueIds As New List(Of String)
        For Each UniqueRegionId In UniqueRegionIds
            If Not LastFetchedRegionLeagueIds.Contains(UniqueRegionId) Then
                NotYetFetchedRegionLeagueIds.Add(UniqueRegionId)
            End If
        Next

        If UniqueRegionIds.Count > 0 Then
            RegionParks = Park.GetParksInRegionsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)
        End If

        RegionParks.Sort(Park.BasicSorter)

        Dim NotYetFetchedTeamAndOfficialIds As New List(Of String)
        For Each UniqueRegionId In UniqueRegionIds
            If Not LastFetchedTeamAndOfficialIds.Contains(UniqueRegionId) Then
                NotYetFetchedTeamAndOfficialIds.Add(UniqueRegionId)
            End If
        Next

        Dim NotYetFetchedRegionProperties As New List(Of String)
        For Each UniqueRegionId In UniqueRegionIds
            If Not LastFetchedRegionProperties.Contains(UniqueRegionId) Then
                NotYetFetchedRegionProperties.Add(UniqueRegionId)
            End If
        Next

        If NotYetFetchedRegionLeagueIds.Count > 0 Then
            RegionLeagues.AddRange(UmpireAssignor.RegionLeague.GetRegionLeaguesPayContractedRegionIdsHelper(NotYetFetchedRegionLeagueIds, SQLConnection, SQLTransaction))
            RegionLeagues.Sort(RegionLeaguePayContracted.BasicSorter)
        End If

        If NotYetFetchedTeamAndOfficialIds.Count > 0 Then
            Teams.AddRange(Team.GetTeamsInRegionsHelper(NotYetFetchedTeamAndOfficialIds, SQLConnection, SQLTransaction))
            Teams.Sort(Team.BasicSorter)

            OfficialRegions.AddRange(OfficialRegion.GetOfficialRegionsInRegionsHelper(NotYetFetchedTeamAndOfficialIds, SQLConnection, SQLTransaction))
            OfficialRegions.Sort(OfficialRegion.BasicSorter)
        End If


        If NotYetFetchedRegionProperties.Count > 0 Then
            RegionProperties.AddRange(UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(NotYetFetchedRegionProperties, SQLConnection, SQLTransaction))
            RegionProperties.Sort(UmpireAssignor.RegionProperties.BasicSorter)

            RegionUsers.AddRange(UmpireAssignor.RegionUser.LoadAllInRegionIdsHelper(NotYetFetchedRegionProperties, Username, SQLConnection, SQLTransaction))
            RegionUsers.Sort(RegionUser.BasicSorter)
        End If

        Dim ScheduleVersions As New List(Of Schedule)
        Dim ScheduleConfirms = New List(Of ScheduleConfirm)
        Dim ScheduleTeamConfirms = New List(Of ScheduleTeamConfirm)


        Dim NewTeamRegionIds As New List(Of String)
        For Each Team In Teams
            If Team.RealTeamId <> "" AndAlso Not NewTeamRegionIds.Contains(Team.RealTeamId) Then
                NewTeamRegionIds.Add(Team.RealTeamId)
            End If
        Next

        Dim RegionUserFetcher As New RegionUserFetcher
        RegionUserFetcher.RegionUsers = RegionUsers
        For Each RU In RegionUsers
            If Not RegionUserFetcher.RegionIdHelper.RegionIdsFetched.Contains(RU.RegionId) Then
                RegionUserFetcher.RegionIdHelper.RegionIdsFetched.Add(RU.RegionId)
            End If
        Next

        Dim RegionPropertiesFetcher As New RegionPropertiesFetcher
        RegionPropertiesFetcher.TRegionProperties = RegionProperties
        For Each RP In RegionProperties
            If Not RegionPropertiesFetcher.RegionIdHelper.RegionIdsFetched.Contains(RP.RegionId) Then
                RegionPropertiesFetcher.RegionIdHelper.RegionIdsFetched.Add(RP.RegionId)
            End If
        Next

        RegionProperties = RegionPropertiesFetcher.GetRegionPropertiesRegionIdsHelper(NewTeamRegionIds, SQLConnection, SQLTransaction)
        RegionUsers = RegionUserFetcher.LoadAllInRegionIdsHelper(NewTeamRegionIds, Username, SQLConnection, SQLTransaction)

        Dim RealRegionLeagueIds As New List(Of String)
        For Each RegionLeague In RegionLeagues
            If RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked AndAlso Not RealRegionLeagueIds.Contains(RegionLeague.RealLeagueId) Then
                RealRegionLeagueIds.Add(RegionLeague.RealLeagueId)
            End If
        Next

        Dim OfficialRegionFetcher As New OfficialRegionFetcher
        OfficialRegionFetcher.OfficialRegions = OfficialRegions
        For Each OfficialRegion In OfficialRegions
            If Not OfficialRegionFetcher.RegionIdHelper.RegionIdsFetched.Contains(OfficialRegion.RegionId) Then
                OfficialRegionFetcher.RegionIdHelper.RegionIdsFetched.Add(OfficialRegion.RegionId)
            End If
        Next

        OfficialRegions = OfficialRegionFetcher.GetOfficialRegionsInRegionsHelper(RealRegionLeagueIds, SQLConnection, SQLTransaction)

        If UniqueRegionIds.Count > 0 Then
            ScheduleVersions = GetMasterScheduleFromRegionsHelper(UniqueRegionIds, StartDate, EndDate, SQLConnection, SQLTransaction)

            SchedulePosition.GetMasterSchedulePositionFromRegionsHelper(UniqueRegionIds, StartDate, EndDate, ScheduleVersions, SQLConnection, SQLTransaction)
            ScheduleFine.GetMasterScheduleFineFromRegionsHelper(UniqueRegionIds, StartDate, EndDate, ScheduleVersions, SQLConnection, SQLTransaction)
            ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsHelper(UniqueRegionIds, StartDate, EndDate, ScheduleVersions, SQLConnection, SQLTransaction)

            ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsHelper(UniqueRegionIds, StartDate, EndDate, ScheduleVersions, SQLConnection, SQLTransaction)
            ScheduleUserCommentTeam.GetMasterScheduleUserCommentTeamFromRegionsHelper(UniqueRegionIds, StartDate, EndDate, ScheduleVersions, SQLConnection, SQLTransaction)
            ScheduleCallup.GetMasterScheduleCallUpFromRegionsHelper(UniqueRegionIds, StartDate, EndDate, ScheduleVersions, SQLConnection, SQLTransaction)

            ScheduleConfirms = ScheduleConfirm.GetMasterScheduleConfirmFromRegions(UniqueRegionIds, StartDate, EndDate, SQLConnection, SQLTransaction)
            ScheduleTeamConfirms = ScheduleTeamConfirm.GetMasterScheduleTeamConfirmFromRegions(UniqueRegionIds, StartDate, EndDate, SQLConnection, SQLTransaction)

            If Not IsSimple Then
                Dim LeagueAndTeamRegionIds As New List(Of String)
                Dim LeagueAndTeamRegions As New List(Of RegionProperties)
                For Each RegionPropertiesItem In RegionProperties
                    If RegionPropertiesItem.EntityType = "league" OrElse RegionPropertiesItem.EntityType = "team" Then
                        LeagueAndTeamRegions.Add(RegionPropertiesItem)
                        LeagueAndTeamRegionIds.Add(RegionPropertiesItem.RegionId)
                    End If
                Next

                Dim DisplayableLeagueAndTeamRegions As New List(Of RegionProperties)
                For Each DisplayingScheduleRegionId In DisplayingScheduleRegionIds
                    Dim DisplayingScheduleRegion As RegionProperties = PublicCode.BinarySearchItem(RegionProperties, New RegionProperties With {.RegionId = DisplayingScheduleRegionId}, UmpireAssignor.RegionProperties.BasicSorter)
                    If DisplayingScheduleRegion IsNot Nothing Then
                        If DisplayingScheduleRegion.EntityType = "league" Then
                            If DisplayableLeagueAndTeamRegions.Find(Function(R) R.RegionId = DisplayingScheduleRegionId) Is Nothing Then
                                DisplayableLeagueAndTeamRegions.Add(DisplayingScheduleRegion)
                            End If
                        ElseIf DisplayingScheduleRegion.EntityType = "team" Then
                            If DisplayableLeagueAndTeamRegions.Find(Function(R) R.RegionId = DisplayingScheduleRegionId) Is Nothing Then
                                DisplayableLeagueAndTeamRegions.Add(DisplayingScheduleRegion)
                            End If
                            For Each RegionLeague In RegionLeagues
                                If RegionLeague.RegionId = DisplayingScheduleRegionId AndAlso RegionLeague.RealLeagueId <> "" AndAlso DisplayableLeagueAndTeamRegions.Find(Function(R) R.RegionId = RegionLeague.RealLeagueId) Is Nothing Then
                                    Dim DisplayingScheduleRegion2 As RegionProperties = PublicCode.BinarySearchItem(RegionProperties, New RegionProperties With {.RegionId = RegionLeague.RealLeagueId}, UmpireAssignor.RegionProperties.BasicSorter)
                                    If DisplayingScheduleRegion2 IsNot Nothing Then
                                        DisplayableLeagueAndTeamRegions.Add(DisplayingScheduleRegion2)
                                    End If
                                End If
                            Next
                        ElseIf DisplayingScheduleRegion.EntityType = "referee" Then
                            For Each RegionLeague In RegionLeagues
                                If RegionLeague.RegionId = DisplayingScheduleRegionId AndAlso RegionLeague.RealLeagueId <> "" AndAlso DisplayableLeagueAndTeamRegions.Find(Function(R) R.RegionId = RegionLeague.RealLeagueId) Is Nothing Then
                                    Dim DisplayingScheduleRegion2 As RegionProperties = PublicCode.BinarySearchItem(RegionProperties, New RegionProperties With {.RegionId = RegionLeague.RealLeagueId}, UmpireAssignor.RegionProperties.BasicSorter)
                                    If DisplayingScheduleRegion2 IsNot Nothing Then
                                        DisplayableLeagueAndTeamRegions.Add(DisplayingScheduleRegion2)
                                    End If
                                End If
                            Next
                        End If
                    End If
                Next

                Dim DisplayableLeagueAndTeamRegionIds As New List(Of String)
                For Each DisplayableLeagueAndTeamRegion In DisplayableLeagueAndTeamRegions
                    DisplayableLeagueAndTeamRegionIds.Add(DisplayableLeagueAndTeamRegion.RegionId)
                Next
            End If
        End If

        Schedule = ScheduleResult.ConvertIntoScheduleResultNoOfficial(ScheduleVersions, ScheduleConfirms, ScheduleTeamConfirms)

        Dim HasTemp As Boolean = False

        If Not IsAvailableSpots AndAlso UniqueRegionIdAdmins.Count > 0 Then
            HasTemp = AnyOfRegionIdsHaveTemp(Username, UniqueRegionIdAdmins, SQLConnection, SQLTransaction)

            If HasTemp Then
                ScheduleTemps = GetMasterScheduleFromRegionsTempHelper(UniqueRegionIdAdmins, Username, StartDate, EndDate, SQLConnection, SQLTransaction)
                SchedulePosition.GetMasterSchedulePositionFromRegionsTempHelper(UniqueRegionIdAdmins, Username, StartDate, EndDate, ScheduleTemps, SQLConnection, SQLTransaction)
                ScheduleFine.GetMasterScheduleFineFromRegionsTempHelper(UniqueRegionIdAdmins, Username, StartDate, EndDate, ScheduleTemps, SQLConnection, SQLTransaction)
                ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsTempHelper(UniqueRegionIdAdmins, Username, StartDate, EndDate, ScheduleTemps, SQLConnection, SQLTransaction)
                ScheduleUserCommentTeam.GetMasterScheduleUserCommentTeamFromRegionsTempHelper(UniqueRegionIdAdmins, Username, StartDate, EndDate, ScheduleTemps, SQLConnection, SQLTransaction)
                ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsTempHelper(UniqueRegionIdAdmins, Username, StartDate, EndDate, ScheduleTemps, SQLConnection, SQLTransaction)
                ScheduleCallup.GetMasterScheduleCallUpFromRegionsTempHelper(UniqueRegionIdAdmins, Username, StartDate, EndDate, ScheduleTemps, SQLConnection, SQLTransaction)
            End If
        End If

        If Not IsSimple Then
            If IsAvailableSpots Then
                ScheduleRequests = ScheduleRequest.GetMyScheduleRequestFromAssignorRegionIdsRangeHelper(DisplayingScheduleRegionIds, Username, StartDate, EndDate, SQLConnection, SQLTransaction)
                ScheduleBookOffs = ScheduleBookOff.GetMyScheduleBookOffFromAssignorRegionIdsRangeHelper(DisplayingScheduleRegionIds, Username, StartDate, EndDate, SQLConnection, SQLTransaction)
            Else
                ScheduleRequests = ScheduleRequest.GetAllScheduleRequestsRegionIdsHelper(UniqueRegionIdAdmins, StartDate, EndDate, SQLConnection, SQLTransaction)
                ScheduleBookOffs = ScheduleBookOff.GetAllScheduleBookOffsRegionIdsHelper(UniqueRegionIdAdmins, StartDate, EndDate, SQLConnection, SQLTransaction)
            End If

            If GetGlobalAvail Then
                If Not IsAvailableSpots Then
                    Availabilities = UserAvailability.GetGlobalAvailabilityFromRegionIdsHelper(UniqueRegionIdAdmins, StartDate, EndDate, SQLConnection, SQLTransaction, RegionUsers)
                    Holidays = UserAvailability.GetHolidaysFromRegionIdsHelper(UniqueRegionIdAdmins, StartDate, EndDate, SQLConnection, SQLTransaction)
                Else
                    Availabilities = UserAvailability.GetUsersAvailabilityFromRegionIdsUsernameHelper(DisplayingScheduleRegionIds, Username, StartDate, EndDate, SQLConnection, SQLTransaction)
                    Holidays = UserAvailability.GetHolidaysFromRegionIdsHelper(DisplayingScheduleRegionIds, StartDate, EndDate, SQLConnection, SQLTransaction)
                End If
            End If


            If Not IsAvailableSpots Then
                IgnoredSchedule = GetIgnoredMasterScheduleFromRegionIdsNonVersionHelper(UniqueRegionIdAdmins, StartDate, EndDate, SQLConnection, SQLTransaction)
            End If
        End If

        If IsSimple AndAlso Not IsAvailableSpots AndAlso UniqueRegionIdAdmins.Count > 0 Then
            For Each TRegionId In UniqueRegionIdAdmins
                Using SQLCommand As New SqlCommand("DELETE FROM ScheduleDownloadDate WHERE RegionId = @RegionId And UserId = @UserId; INSERT INTO ScheduleDownloadDate (RegionId, UserId, DownloadedDate) VALUES (@RegionId, @UserId, @DownloadedDate)", SQLConnection, SQLTransaction)
                    SQLCommand.Parameters.Add(New SqlParameter("RegionId", TRegionId))
                    SQLCommand.Parameters.Add(New SqlParameter("UserId", Username))
                    SQLCommand.Parameters.Add(New SqlParameter("DownloadedDate", DateTime.UtcNow))
                    SQLCommand.ExecuteNonQuery()
                End Using
            Next
        End If

        Return Nothing
    End Function

    Public Shared Function GetScheduleVersionsFromScheduleItemsHelper(ScheduleItems As List(Of Schedule), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        If ScheduleItems.Count = 0 Then Return New List(Of Schedule)

        Dim Result As New List(Of Schedule)

        Dim ScheduleItemsWhere As New StringBuilder()
        Dim N As Integer = 0
        For Each ScheduleItem In ScheduleItems
            If N <> 0 Then ScheduleItemsWhere.Append(" Or ")
            ScheduleItemsWhere.Append("(")
            ScheduleItemsWhere.Append("RegionId = @RegionId" & N)
            ScheduleItemsWhere.Append(" AND ")
            ScheduleItemsWhere.Append("ScheduleId = @ScheduleId" & N)
            ScheduleItemsWhere.Append(")")
            N += 1
        Next

        Dim CommandText = "SELECT RegionId, ScheduleId, VersionId, LinkedRegionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, IsDeleted, DateAdded, StatLinks FROM ScheduleVersion WHERE " & ScheduleItemsWhere.ToString & " ORDER BY RegionId, ScheduleId, VersionId"
        Using SQLCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            Dim I As Integer = 0
            For Each ScheduleItem In ScheduleItems
                SQLCommand.Parameters.Add(New SqlParameter("RegionId" & I, ScheduleItem.RegionId))
                SQLCommand.Parameters.Add(New SqlParameter("ScheduleId" & I, ScheduleItem.ScheduleId))
                I += 1
            Next

            Dim Reader = New SQLReaderIncrementor(SQLCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                   .RegionId = Reader.GetString(),
                   .ScheduleId = Reader.GetInt32(),
                   .VersionId = Reader.GetInt32(),
                   .LinkedRegionId = Reader.GetString(),
                   .LinkedScheduleId = Reader.GetInt32(),
                   .GameNumber = Reader.GetString(),
                   .GameType = Reader.GetString(),
                   .LeagueId = Reader.GetString(),
                   .HomeTeam = Reader.GetString(),
                   .HomeTeamScore = Reader.GetString(),
                   .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                   .AwayTeam = Reader.GetString(),
                   .AwayTeamScore = Reader.GetString(),
                   .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                   .GameDate = Reader.GetDateTime(),
                   .ParkId = Reader.GetString(),
                   .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                   .GameStatus = Reader.GetString(),
                   .GameComment = Reader.GetString(),
                   .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                   .OfficialRegionId = Reader.GetString(),
                   .ScorekeeperRegionId = Reader.GetString(),
                   .SupervisorRegionId = Reader.GetString(),
                   .IsDeleted = Reader.GetBoolean(),
                   .SchedulePositions = New List(Of SchedulePosition),
                   .ScheduleFines = New List(Of ScheduleFine),
                   .ScheduleUserComments = New List(Of ScheduleUserComment),
                   .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                   .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                   .ScheduleCallUps = New List(Of ScheduleCallup),
                   .DateAdded = Reader.GetDateTime(),
                   .StatLinks = Reader.GetString()
                })
            End While
            Reader.Close()
        End Using
        Return Result
    End Function

    Public Shared Function GetMasterScheduleFromRegion(FilterAdmin As Boolean, Username As String, RegionId As String, StartDate As Date, EndDate As Date, Optional GetGlobalAvail As Boolean = False, Optional IsAvailableSpots As Boolean = False, Optional IsSimple As Boolean = False, Optional OldSQLConnection As SqlConnection = Nothing, Optional OldSQLTransaction As SqlTransaction = Nothing) As Object
        Dim Schedule As New List(Of ScheduleResult)
        Dim StatGames As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, Dictionary(Of Integer, List(Of StatGame))))))
        Dim FullSchedule As New List(Of Schedule)
        Dim LinkedSchedule As New List(Of Schedule)
        Dim IgnoredSchedule As New List(Of ScheduleIgnore)
        Dim LinkedScheduleConfirm As New List(Of Schedule)
        Dim MySchedule As New List(Of Schedule)
        Dim ScheduleTemps As List(Of ScheduleTemp) = Nothing
        Dim RegionUsers = New List(Of RegionUser)
        Dim RegionParks = New List(Of Park)
        Dim RegionLeagues = New List(Of RegionLeaguePayContracted)
        Dim Teams = New List(Of Team)
        Dim OfficialRegions As New List(Of OfficialRegion)
        Dim ScheduleRequests As New List(Of ScheduleRequest)
        Dim ScheduleBookOffs As New List(Of ScheduleBookOff)
        Dim RegionProperties = New List(Of RegionProperties)
        Dim Region As Region = Nothing
        Dim Availabilities As List(Of UserAvailability) = Nothing
        Dim Holidays As List(Of Holiday) = Nothing

        Dim LinkedLeagueIds As New List(Of String)
        Dim LinkedLeagueIdsAdmin As New List(Of String)

        Dim LinkedLeagueIdsAdminConfirm As New List(Of String)

        Dim UniqueRegionIdAdmins = New List(Of String)
        Dim RegionUsersAdmin As New List(Of RegionUser)
        Dim DisplayingScheduleRegionIds As New List(Of String)
        Dim RefereeRegionIdAdmins = New List(Of String)

        Dim TeamRegionIds As New List(Of String)
        Dim RefereeRegionIds As New List(Of String)
        Dim LeagueRegionIds As New List(Of String)

        Username = Username.ToLower

        Dim IsExecutive As Boolean = False

        'Try
        Dim GetMasterScheduleFromRegionHelperResult As ErrorObject = Nothing

        If OldSQLConnection IsNot Nothing Then
            GetMasterScheduleFromRegionHelperResult = GetMasterScheduleFromRegionHelper(FilterAdmin, Username, RegionId, StartDate, EndDate, GetGlobalAvail, IsAvailableSpots, IsSimple, OldSQLConnection, OldSQLTransaction, UniqueRegionIdAdmins, Teams, RegionUsers, ScheduleTemps, ScheduleRequests, ScheduleBookOffs, RegionUsersAdmin, RegionProperties, RegionParks, RegionLeagues, TeamRegionIds, RefereeRegionIds, RefereeRegionIdAdmins, OfficialRegions, DisplayingScheduleRegionIds, IgnoredSchedule, LeagueRegionIds, LinkedLeagueIds, Schedule, Holidays, Availabilities, StatGames, FullSchedule)
        Else
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    GetMasterScheduleFromRegionHelperResult = GetMasterScheduleFromRegionHelper(FilterAdmin, Username, RegionId, StartDate, EndDate, GetGlobalAvail, IsAvailableSpots, IsSimple, SqlConnection, SqlTransaction, UniqueRegionIdAdmins, Teams, RegionUsers, ScheduleTemps, ScheduleRequests, ScheduleBookOffs, RegionUsersAdmin, RegionProperties, RegionParks, RegionLeagues, TeamRegionIds, RefereeRegionIds, RefereeRegionIdAdmins, OfficialRegions, DisplayingScheduleRegionIds, IgnoredSchedule, LeagueRegionIds, LinkedLeagueIds, Schedule, Holidays, Availabilities, StatGames, FullSchedule)
                    SqlTransaction.Commit()
                End Using
            End Using
        End If

        If GetMasterScheduleFromRegionHelperResult IsNot Nothing Then
            Return GetMasterScheduleFromRegionHelperResult
        End If

        If ScheduleTemps IsNot Nothing Then
            For Each ScheduleTemp In ScheduleTemps
                MergeUserCommentData(ScheduleTemp.ScheduleUserComments, ScheduleTemp.ScheduleUserCommentTeams)
            Next
        End If

        Dim UniqueRegionIds As New List(Of String)
        For Each RegionPropertiesItem In RegionProperties
            UniqueRegionIds.Add(RegionPropertiesItem.RegionId)
        Next

        TeamRegionIds = New List(Of String)
        RefereeRegionIds = New List(Of String)
        For Each RegionId In UniqueRegionIds
            Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = RegionId)
            If RegionProperty.EntityType = "team" Then TeamRegionIds.Add(RegionProperty.RegionId)
            If RegionProperty.EntityType = "referee" Then RefereeRegionIds.Add(RegionProperty.RegionId)
        Next

        Dim NewSchedule As New List(Of ScheduleResult)
        For Each ScheduleResultItem In Schedule
            Dim TRegionId = ScheduleResultItem.Schedule(0).RegionId
            Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)

            If RegionProperty.EntityType = "league" Then
                Dim FoundTeamIds As New List(Of String)
                For Each ScheduleItem In ScheduleResultItem.Schedule
                    Dim HomeTeam = Teams.Find(Function(T) T.RealTeamName <> "" AndAlso T.RegionId = TRegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.Trim().ToLower AndAlso TeamRegionIds.Contains(T.RealTeamId))
                    Dim AwayTeam = Teams.Find(Function(T) T.RealTeamName <> "" AndAlso T.RegionId = TRegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.Trim().ToLower AndAlso TeamRegionIds.Contains(T.RealTeamId))
                    If HomeTeam IsNot Nothing AndAlso Not FoundTeamIds.Contains(HomeTeam.RealTeamId) Then FoundTeamIds.Add(HomeTeam.RealTeamId)
                    If AwayTeam IsNot Nothing AndAlso Not FoundTeamIds.Contains(AwayTeam.RealTeamId) Then FoundTeamIds.Add(AwayTeam.RealTeamId)
                Next

                For Each FoundTeamId In FoundTeamIds
                    Dim NewScheduleResultItem As New ScheduleResult
                    NewScheduleResultItem.ScheduleTeamConfirms = ScheduleResultItem.ScheduleTeamConfirms

                    Dim FoundRealTeamIndex = -1
                    Dim I As Integer = 0
                    For Each ScheduleItem In ScheduleResultItem.Schedule
                        Dim HomeTeam = Teams.Find(Function(T) T.RealTeamName <> "" AndAlso T.RegionId = TRegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.Trim().ToLower AndAlso T.RealTeamId = FoundTeamId)
                        Dim AwayTeam = Teams.Find(Function(T) T.RealTeamName <> "" AndAlso T.RegionId = TRegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.Trim().ToLower AndAlso T.RealTeamId = FoundTeamId)

                        If HomeTeam IsNot Nothing OrElse AwayTeam IsNot Nothing Then FoundRealTeamIndex = I

                        NewScheduleResultItem.Schedule.Add(ConvertToLinkScheduleItem(ScheduleItem, FoundTeamId, RegionLeagues))
                        I += 1
                    Next

                    If FoundRealTeamIndex <> ScheduleResultItem.Schedule.Count - 1 Then
                        NewScheduleResultItem.Schedule.RemoveRange(FoundRealTeamIndex + 1, ScheduleResultItem.Schedule.Count - 1 - FoundRealTeamIndex)
                        Dim ClonedItem = NewScheduleResultItem.Schedule.Last().CloneItem()
                        ClonedItem.IsDeleted = True
                        NewScheduleResultItem.Schedule.Add(ClonedItem)
                    End If

                    NewSchedule.Add(NewScheduleResultItem)
                Next
            End If
        Next
        Schedule.AddRange(NewSchedule)

        Schedule.Sort(ScheduleResult.BasicSorter)

        MergeTeamData(Schedule)

        Dim MyRegionUsers As New Dictionary(Of String, List(Of RegionUser))
        For Each RegionUser In RegionUsers
            If RegionUser.RealUsername = Username Then
                If Not MyRegionUsers.ContainsKey(RegionUser.RegionId) Then MyRegionUsers.Add(RegionUser.RegionId, New List(Of RegionUser))
                MyRegionUsers(RegionUser.RegionId).Add(RegionUser)
            End If
        Next

        If IsAvailableSpots Then
            'Build MySchedule
            MySchedule = New List(Of Schedule)
            If Not IsSimple Then
                For Each ScheduleResultItem In Schedule
                    If ScheduleResultItem.Schedule.Last().IsCancelled OrElse ScheduleResultItem.Schedule.Last().IsDeleted Then Continue For

                    Dim TRegionId As String = ScheduleResultItem.Schedule(0).RegionId
                    Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)
                    If RegionProperty.EntityType = "team" Then
                        Dim TMyRegionUsers = New List(Of RegionUser)
                        If MyRegionUsers.ContainsKey(TRegionId) Then
                            TMyRegionUsers = MyRegionUsers(TRegionId)
                        End If

                        For Each RegionUser In TMyRegionUsers
                            Dim ScheduleTeamConfirm = ScheduleResultItem.ScheduleTeamConfirms.Find(Function(SC) SC.Username = RegionUser.Username)
                            If ScheduleTeamConfirm IsNot Nothing AndAlso ScheduleTeamConfirm.VersionId = ScheduleResultItem.Schedule.Last().VersionId AndAlso ScheduleTeamConfirm.Confirmed = 2 Then Continue For

                            Dim FoundItem As Boolean = False
                            If RegionUser.IsPlayer OrElse RegionUser.IsCoach Then
                                FoundItem = True
                            ElseIf RegionUser.IsCallup Then
                                Dim ScheduleCallup = ScheduleResultItem.Schedule.Last().ScheduleCallUps.Find(Function(SC) SC.Username = RegionUser.Username)
                                If ScheduleCallup IsNot Nothing Then
                                    FoundItem = True
                                End If
                            End If
                            If FoundItem Then
                                Dim NewScheduleItem = ScheduleResultItem.Schedule.Last().CloneItem()
                                NewScheduleItem.OfficialId = RegionUser.Username
                                MySchedule.Add(NewScheduleItem)
                            End If
                        Next
                    ElseIf RegionProperty.EntityType = "referee" Then
                        Dim TMyRegionUsers = New List(Of RegionUser)
                        If MyRegionUsers.ContainsKey(TRegionId) Then
                            TMyRegionUsers = MyRegionUsers(TRegionId)
                        End If

                        For Each RegionUser In TMyRegionUsers
                            If ScheduleResultItem.Schedule.Last().ContainsOfficialIdOnPosition(RegionUser.Username) Then
                                Dim NewScheduleItem = ScheduleResultItem.Schedule.Last().CloneItem()
                                NewScheduleItem.OfficialId = RegionUser.Username
                                MySchedule.Add(NewScheduleItem)
                            End If
                        Next
                    End If
                Next
            End If

            'Filter Schedule
            Dim TNewSchedule As New List(Of ScheduleResult)
            For Each ScheduleResultItem In Schedule
                Dim TRegionId As String = ScheduleResultItem.Schedule(0).RegionId

                If DisplayingScheduleRegionIds.Contains(TRegionId) Then
                    Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)

                    Dim TMyRegionUsers = New List(Of RegionUser)
                    If MyRegionUsers.ContainsKey(TRegionId) Then
                        TMyRegionUsers = MyRegionUsers(TRegionId)
                    End If

                    Dim CanViewPositions As Boolean = RegionProperty.ShowFullScheduleToNonMembers OrElse TMyRegionUsers.Any(Function(RU) RU.CanViewMasterSchedule OrElse RU.IsExecutive)
                    If Not CanViewPositions Then
                        For Each SC In ScheduleResultItem.Schedule
                            If SC.GameDate < DateTime.UtcNow.AddDays(-1) Then
                                For Each SP In SC.SchedulePositions
                                    SP.OfficialId = ""
                                Next
                            Else
                                For Each SP In SC.SchedulePositions
                                    If SP.OfficialId = "" Then
                                        SP.OfficialId = "open"
                                    Else
                                        SP.OfficialId = "taken"
                                    End If
                                Next
                            End If
                            SC.ScheduleFines = SC.ScheduleFines.FindAll(Function(SF) TMyRegionUsers.Exists(Function(RU) RU.RealUsername = Username AndAlso SF.OfficialId = RU.Username))
                            SC.ScheduleUserComments = SC.ScheduleUserComments.FindAll(Function(SU) TMyRegionUsers.Exists(Function(RU) RU.RealUsername = Username AndAlso SU.OfficialId = RU.Username))
                        Next
                    End If
                    ScheduleResultItem.ScheduleConfirms = New List(Of ScheduleConfirm)
                    ScheduleResultItem.ScheduleTeamConfirms = New List(Of ScheduleTeamConfirm)

                    TNewSchedule.Add(ScheduleResultItem)
                End If
            Next
            Schedule = TNewSchedule
        Else
            'Build LinkedSchedule
            If Not IsSimple Then
                Dim TTNewSchedule As New List(Of ScheduleResult)


                For Each ScheduleResultItem In Schedule
                    Dim TRegionId = ScheduleResultItem.Schedule(0).RegionId
                    Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)

                    If TRegionId <> "allregions" AndAlso Not TRegionId <> "alleditableregions" Then
                        DisplayingScheduleRegionIds.Contains(TRegionId)
                    End If

                    If RegionProperty.EntityType = "league" Then
                        Dim FoundOfficialRegionIds As New List(Of String)
                        For Each ScheduleItem In ScheduleResultItem.Schedule
                            Dim OfficialRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.OfficialRegionId.ToLower AndAlso RefereeRegionIdAdmins.Contains(O.RealOfficialRegionId) AndAlso DisplayingScheduleRegionIds.Contains(O.RealOfficialRegionId))
                            Dim ScorekeeperRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.ScorekeeperRegionId.ToLower AndAlso RefereeRegionIdAdmins.Contains(O.RealOfficialRegionId) AndAlso DisplayingScheduleRegionIds.Contains(O.RealOfficialRegionId))
                            Dim SupervisorRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.SupervisorRegionId.ToLower AndAlso RefereeRegionIdAdmins.Contains(O.RealOfficialRegionId) AndAlso DisplayingScheduleRegionIds.Contains(O.RealOfficialRegionId))

                            If OfficialRegion IsNot Nothing AndAlso Not FoundOfficialRegionIds.Contains(OfficialRegion.RealOfficialRegionId) Then FoundOfficialRegionIds.Add(OfficialRegion.RealOfficialRegionId)
                            If ScorekeeperRegion IsNot Nothing AndAlso Not FoundOfficialRegionIds.Contains(ScorekeeperRegion.RealOfficialRegionId) Then FoundOfficialRegionIds.Add(ScorekeeperRegion.RealOfficialRegionId)
                            If SupervisorRegion IsNot Nothing AndAlso Not FoundOfficialRegionIds.Contains(SupervisorRegion.RealOfficialRegionId) Then FoundOfficialRegionIds.Add(SupervisorRegion.RealOfficialRegionId)
                        Next

                        For Each FoundOfficialRegionId In FoundOfficialRegionIds
                            Dim NewScheduleResultItem As New ScheduleResult
                            NewScheduleResultItem.ScheduleTeamConfirms = ScheduleResultItem.ScheduleTeamConfirms

                            Dim I As Integer = 0
                            Dim LastFoundIndex As Integer = -1
                            For Each ScheduleItem In ScheduleResultItem.Schedule
                                Dim OfficialRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.OfficialRegionId.ToLower AndAlso O.RealOfficialRegionId = FoundOfficialRegionId)
                                Dim ScorekeeperRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.ScorekeeperRegionId.ToLower AndAlso O.RealOfficialRegionId = FoundOfficialRegionId)
                                Dim SupvervisorRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.SupervisorRegionId.ToLower AndAlso O.RealOfficialRegionId = FoundOfficialRegionId)

                                If OfficialRegion IsNot Nothing OrElse ScorekeeperRegion IsNot Nothing OrElse SupvervisorRegion IsNot Nothing Then LastFoundIndex = I

                                Dim LinkedScheduleItem = ConvertToLinkScheduleItem(ScheduleItem, FoundOfficialRegionId, RegionLeagues)

                                Dim CrewType As New Dictionary(Of String, String)

                                If LinkedScheduleItem.CrewType IsNot Nothing AndAlso LinkedScheduleItem.CrewType.ContainsKey("umpire") Then
                                    If OfficialRegion IsNot Nothing Then
                                        CrewType.Add("umpire", LinkedScheduleItem.CrewType("umpire"))
                                    Else
                                        CrewType.Add("umpire", "0-man")
                                    End If
                                End If

                                If LinkedScheduleItem.CrewType IsNot Nothing AndAlso LinkedScheduleItem.CrewType.ContainsKey("scorekeeper") Then
                                    If ScorekeeperRegion IsNot Nothing Then
                                        CrewType.Add("scorekeeper", LinkedScheduleItem.CrewType("scorekeeper"))
                                    Else
                                        CrewType.Add("scorekeeper", "0-man")
                                    End If
                                End If


                                If LinkedScheduleItem.CrewType IsNot Nothing AndAlso LinkedScheduleItem.CrewType.ContainsKey("supervisor") Then
                                    If ScorekeeperRegion IsNot Nothing Then
                                        CrewType.Add("supervisor", LinkedScheduleItem.CrewType("supervisor"))
                                    Else
                                        CrewType.Add("supervisor", "0-man")
                                    End If
                                End If

                                LinkedScheduleItem.CrewType = CrewType

                                NewScheduleResultItem.Schedule.Add(LinkedScheduleItem)
                                I += 1
                            Next
                            If LastFoundIndex <> ScheduleResultItem.Schedule.Count - 1 Then
                                NewScheduleResultItem.Schedule.RemoveRange(LastFoundIndex, ScheduleResultItem.Schedule.Count - 1 - LastFoundIndex)
                                Dim ClonedItem = NewScheduleResultItem.Schedule.Last().CloneItem
                                ClonedItem.IsDeleted = True
                                NewScheduleResultItem.Schedule.Add(ClonedItem)
                            End If

                            If Not NewScheduleResultItem.Schedule.Last().IsDeleted Then
                                LinkedSchedule.Add(NewScheduleResultItem.Schedule.Last())
                            End If
                        Next
                    End If
                Next

                'Build LeagueConfirms
                For Each ScheduleResultItem In Schedule
                    Dim TRegionId = ScheduleResultItem.Schedule(0).RegionId
                    Dim TScheduleId = ScheduleResultItem.Schedule(0).ScheduleId
                    Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)

                    If RegionProperty.EntityType = "league" Then
                        'Find Linked Regions
                        Dim FoundTeamRegionIds As New List(Of String)
                        Dim FoundOfficialRegionIds As New List(Of String)

                        For Each ScheduleItem In ScheduleResultItem.Schedule
                            Dim HomeTeam = Teams.Find(Function(T) T.RealTeamId <> "" AndAlso T.RegionId = TRegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.ToLower)
                            Dim AwayTeam = Teams.Find(Function(T) T.RealTeamId <> "" AndAlso T.RegionId = TRegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.ToLower)
                            Dim OfficialRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.OfficialRegionId.ToLower)
                            Dim ScorekeeperRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.ScorekeeperRegionId.ToLower)
                            Dim SupervisorRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.SupervisorRegionId.ToLower)

                            If HomeTeam IsNot Nothing AndAlso Not FoundTeamRegionIds.Contains(HomeTeam.RealTeamId) Then FoundTeamRegionIds.Add(HomeTeam.RealTeamId)
                            If AwayTeam IsNot Nothing AndAlso Not FoundTeamRegionIds.Contains(AwayTeam.RealTeamId) Then FoundTeamRegionIds.Add(AwayTeam.RealTeamId)
                            If OfficialRegion IsNot Nothing AndAlso Not FoundOfficialRegionIds.Contains(OfficialRegion.RealOfficialRegionId) Then FoundOfficialRegionIds.Add(OfficialRegion.RealOfficialRegionId)
                            If ScorekeeperRegion IsNot Nothing AndAlso Not FoundOfficialRegionIds.Contains(ScorekeeperRegion.RealOfficialRegionId) Then FoundOfficialRegionIds.Add(ScorekeeperRegion.RealOfficialRegionId)
                            If SupervisorRegion IsNot Nothing AndAlso Not FoundOfficialRegionIds.Contains(SupervisorRegion.RealOfficialRegionId) Then FoundOfficialRegionIds.Add(SupervisorRegion.RealOfficialRegionId)

                        Next

                        For Each FoundTeamRegionId In FoundTeamRegionIds
                            Dim LinkedScheduleResultItem = Schedule.Find(Function(S) S.Schedule(0).RegionId = FoundTeamRegionId AndAlso S.Schedule(0).LinkedRegionId = TRegionId AndAlso S.Schedule(0).ScheduleId = TScheduleId)
                            If LinkedScheduleResultItem IsNot Nothing Then
                                Dim ScheduleConfirms = LinkedScheduleResultItem.ScheduleTeamConfirms.FindAll(Function(SC) RegionUsers.Any(Function(RU) RU.RegionId = FoundTeamRegionId AndAlso RU.IsCoach AndAlso RU.Username = SC.Username))
                                If ScheduleConfirms.Count > 0 Then
                                    Dim MaxVersionId = ScheduleConfirms.Max(Function(SC) SC.VersionId)
                                    Dim NewScheduleItem = LinkedScheduleResultItem.Schedule(MaxVersionId - 1).CloneItem()
                                    NewScheduleItem.ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam)
                                    NewScheduleItem.ScheduleCommentTeams = New List(Of ScheduleCommentTeam)
                                    NewScheduleItem.ScheduleCallUps = New List(Of ScheduleCallup)
                                    ScheduleResultItem.RegionConfirms.Add(NewScheduleItem)
                                End If
                            End If
                        Next

                        For Each FoundOfficialRegionId In FoundOfficialRegionIds
                            Dim LinkedScheduleResultItem = Schedule.Find(Function(S) S.Schedule(0).RegionId = FoundOfficialRegionId AndAlso S.Schedule(0).LinkedRegionId = TRegionId AndAlso S.Schedule(0).LinkedScheduleId = TScheduleId)
                            If LinkedScheduleResultItem IsNot Nothing Then
                                Dim NewScheduleItem = LinkedScheduleResultItem.Schedule.Last().CloneItem()
                                NewScheduleItem.ScheduleUserComments = New List(Of ScheduleUserComment)
                                NewScheduleItem.ScheduleCommentTeams = New List(Of ScheduleCommentTeam)
                                NewScheduleItem.ScheduleFines = New List(Of ScheduleFine)
                                NewScheduleItem.SchedulePositions = New List(Of SchedulePosition)
                                ScheduleResultItem.RegionConfirms.Add(NewScheduleItem)
                            End If
                        Next
                    End If
                Next
            End If

            'Filter Schedule
            Dim TNewSchedule As New List(Of ScheduleResult)
            For Each ScheduleResultItem In Schedule
                Dim TRegionId As String = ScheduleResultItem.Schedule(0).RegionId
                Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)

                If DisplayingScheduleRegionIds.Contains(TRegionId) Then
                    TNewSchedule.Add(ScheduleResultItem)
                Else
                    Dim LastItem = ScheduleResultItem.Schedule.Last()
                    If Not LastItem.IsDeleted Then
                        If RegionProperty.EntityType = "referee" Then
                            If Not LastItem.IsCancelled AndAlso Not LastItem.IsDeleted Then
                                For Each SP In LastItem.SchedulePositions
                                    Dim SPL = SP.OfficialId.ToLower.Trim()
                                    If SPL <> "" Then
                                        Dim RegionUser = PublicCode.BinarySearchItem(RegionUsers, New RegionUser With {.RegionId = TRegionId, .Username = SPL}, UmpireAssignor.RegionUser.BasicSorter)
                                        If RegionUser IsNot Nothing Then
                                            If RegionUsersAdmin.Any(Function(RU) RU.IsSimilarUser(RegionUser)) Then
                                                Dim NewScheduleResultItem = New ScheduleResult()

                                                LastItem = LastItem.CloneItem()
                                                LastItem.ScheduleFines = New List(Of ScheduleFine)
                                                LastItem.ScheduleUserComments = New List(Of ScheduleUserComment)
                                                LastItem.ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam)
                                                LastItem.ScheduleCommentTeams = New List(Of ScheduleCommentTeam)

                                                NewScheduleResultItem.Schedule.Add(LastItem)
                                                TNewSchedule.Add(NewScheduleResultItem)
                                                Exit For
                                            End If
                                        End If
                                    End If
                                Next
                            End If
                        ElseIf RegionProperty.EntityType = "team" Then
                            If Not LastItem.IsCancelled Then
                                For Each RegionUser In RegionUsers
                                    If RegionUser.RegionId = TRegionId AndAlso (RegionUser.IsPlayer OrElse RegionUser.IsCoach OrElse (RegionUser.IsCallup AndAlso LastItem.ScheduleCallUps.Any(Function(SC) SC.Username = RegionUser.Username))) Then
                                        Dim ScheduleTeamConfirm = ScheduleResultItem.ScheduleTeamConfirms.Find(Function(SC) SC.Username = RegionUser.Username)
                                        If ScheduleTeamConfirm Is Nothing OrElse ScheduleTeamConfirm.Confirmed <> 2 Then
                                            If RegionUsersAdmin.Any(Function(RU) RU.IsSimilarUser(RegionUser)) Then
                                                Dim NewScheduleResultItem = New ScheduleResult()

                                                LastItem = LastItem.CloneItem()
                                                LastItem.ScheduleFines = New List(Of ScheduleFine)
                                                LastItem.ScheduleUserComments = New List(Of ScheduleUserComment)
                                                LastItem.ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam)
                                                LastItem.ScheduleCommentTeams = New List(Of ScheduleCommentTeam)

                                                NewScheduleResultItem.Schedule.Add(LastItem)
                                                NewScheduleResultItem.ScheduleTeamConfirms = ScheduleResultItem.ScheduleTeamConfirms
                                                TNewSchedule.Add(NewScheduleResultItem)
                                                Exit For
                                            End If
                                        End If
                                    End If
                                Next
                            End If
                        End If
                    End If
                End If
            Next
            Schedule = TNewSchedule
        End If

        RegionUser.CleanRegionUsers(RegionProperties, RegionUsers, Username)

        For I As Integer = 0 To Schedule.Count - 1
            For N As Integer = 0 To Schedule(I).Schedule.Count - 1
                Schedule(I).Schedule(N) = TrimHiddenScheduleData(Schedule(I).Schedule(N), RegionProperties, RegionUsers, Username)
            Next
        Next

        For I As Integer = 0 To FullSchedule.Count - 1
            FullSchedule(I) = TrimHiddenScheduleData(FullSchedule(I), RegionProperties, RegionUsers, Username)
        Next

        If GetGlobalAvail Then
            Return New With {
                .Success = True,
                .IsExecutive = IsExecutive,
                .Schedule = Schedule,
                .StatGames = StatGames,
                .FullSchedule = FullSchedule,
                .LinkedSchedule = LinkedSchedule,
                .IgnoredSchedule = IgnoredSchedule,
                .ScheduleTemps = ScheduleTemps,
                .ScheduleRequests = ScheduleRequests,
                .ScheduleBookOffs = ScheduleBookOffs,
                .RegionUsers = RegionUsers,
                .RegionParks = RegionParks,
                .RegionLeagues = RegionLeagues,
                .Teams = Teams,
                .OfficialRegions = OfficialRegions,
                .RegionProperties = RegionProperties,
                .Holidays = Holidays,
                .Availabilities = Availabilities,
                .MySchedule = MySchedule,
                .DisplayingScheduleRegionIds = DisplayingScheduleRegionIds
            }
        Else
            Return New With {
                .Success = True,
                .Schedule = Schedule,
                .StatGames = StatGames,
                .FullSchedule = FullSchedule,
                .LinkedSchedule = LinkedSchedule,
                .IgnoredSchedule = IgnoredSchedule,
                .ScheduleTemps = ScheduleTemps,
                .ScheduleRequests = ScheduleRequests,
                .ScheduleBookOffs = ScheduleBookOffs,
                .RegionUsers = RegionUsers,
                .RegionParks = RegionParks,
                .RegionLeagues = RegionLeagues,
                .Teams = Teams,
                .OfficialRegions = OfficialRegions,
                .RegionProperties = RegionProperties,
                .DisplayingScheduleRegionIds = DisplayingScheduleRegionIds
            }
        End If
        'Catch E As Exception
        'Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Shared Function GetMasterScheduleSingle(FilterAdmin As Boolean, Username As String, RegionId As String, ScheduleId As Integer, StartDate As Date, EndDate As Date, Optional GetGlobalAvail As Boolean = False, Optional IsAvailableSpots As Boolean = False, Optional IsSimple As Boolean = False, Optional OldSQLConnection As SqlConnection = Nothing, Optional OldSQLTransaction As SqlTransaction = Nothing) As Object
        Dim Schedule As New List(Of ScheduleResult)
        Dim StatGames As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, Dictionary(Of Integer, List(Of StatGame))))))
        Dim FullSchedule As New List(Of Schedule)
        Dim LinkedSchedule As New List(Of Schedule)
        Dim IgnoredSchedule As New List(Of ScheduleIgnore)
        Dim LinkedScheduleConfirm As New List(Of Schedule)
        Dim MySchedule As New List(Of Schedule)
        Dim ScheduleTemps As List(Of ScheduleTemp) = Nothing
        Dim RegionUsers = New List(Of RegionUser)
        Dim RegionParks = New List(Of Park)
        Dim RegionLeagues = New List(Of RegionLeaguePayContracted)
        Dim Teams = New List(Of Team)
        Dim OfficialRegions As New List(Of OfficialRegion)
        Dim ScheduleRequests As New List(Of ScheduleRequest)
        Dim ScheduleBookOffs As New List(Of ScheduleBookOff)
        Dim RegionProperties = New List(Of RegionProperties)
        Dim Region As Region = Nothing
        Dim Availabilities As List(Of UserAvailability) = Nothing
        Dim Holidays As List(Of Holiday) = Nothing

        Dim LinkedLeagueIds As New List(Of String)
        Dim LinkedLeagueIdsAdmin As New List(Of String)

        Dim LinkedLeagueIdsAdminConfirm As New List(Of String)

        Dim UniqueRegionIdAdmins = New List(Of String)
        Dim RegionUsersAdmin As New List(Of RegionUser)
        Dim DisplayingScheduleRegionIds As New List(Of String)
        Dim RefereeRegionIdAdmins = New List(Of String)

        Dim TeamRegionIds As New List(Of String)
        Dim RefereeRegionIds As New List(Of String)
        Dim LeagueRegionIds As New List(Of String)

        Username = Username.ToLower

        Dim IsExecutive As Boolean = False

        'Try
        Dim GetMasterScheduleFromRegionHelperResult As ErrorObject = Nothing

        If OldSQLConnection IsNot Nothing Then
            GetMasterScheduleFromRegionHelperResult = GetMasterScheduleFromRegionHelper(FilterAdmin, Username, RegionId, StartDate, EndDate, GetGlobalAvail, IsAvailableSpots, IsSimple, OldSQLConnection, OldSQLTransaction, UniqueRegionIdAdmins, Teams, RegionUsers, ScheduleTemps, ScheduleRequests, ScheduleBookOffs, RegionUsersAdmin, RegionProperties, RegionParks, RegionLeagues, TeamRegionIds, RefereeRegionIds, RefereeRegionIdAdmins, OfficialRegions, DisplayingScheduleRegionIds, IgnoredSchedule, LeagueRegionIds, LinkedLeagueIds, Schedule, Holidays, Availabilities, StatGames, FullSchedule)
        Else
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    GetMasterScheduleFromRegionHelperResult = GetMasterScheduleFromRegionHelper(FilterAdmin, Username, RegionId, StartDate, EndDate, GetGlobalAvail, IsAvailableSpots, IsSimple, SqlConnection, SqlTransaction, UniqueRegionIdAdmins, Teams, RegionUsers, ScheduleTemps, ScheduleRequests, ScheduleBookOffs, RegionUsersAdmin, RegionProperties, RegionParks, RegionLeagues, TeamRegionIds, RefereeRegionIds, RefereeRegionIdAdmins, OfficialRegions, DisplayingScheduleRegionIds, IgnoredSchedule, LeagueRegionIds, LinkedLeagueIds, Schedule, Holidays, Availabilities, StatGames, FullSchedule)
                    SqlTransaction.Commit()
                End Using
            End Using
        End If

        If GetMasterScheduleFromRegionHelperResult IsNot Nothing Then
            Return GetMasterScheduleFromRegionHelperResult
        End If

        If ScheduleTemps IsNot Nothing Then
            For Each ScheduleTemp In ScheduleTemps
                MergeUserCommentData(ScheduleTemp.ScheduleUserComments, ScheduleTemp.ScheduleUserCommentTeams)
            Next
        End If

        Dim UniqueRegionIds As New List(Of String)
        For Each RegionPropertiesItem In RegionProperties
            UniqueRegionIds.Add(RegionPropertiesItem.RegionId)
        Next

        TeamRegionIds = New List(Of String)
        RefereeRegionIds = New List(Of String)
        For Each RegionId In UniqueRegionIds
            Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = RegionId)
            If RegionProperty.EntityType = "team" Then TeamRegionIds.Add(RegionProperty.RegionId)
            If RegionProperty.EntityType = "referee" Then RefereeRegionIds.Add(RegionProperty.RegionId)
        Next

        Dim NewSchedule As New List(Of ScheduleResult)
        For Each ScheduleResultItem In Schedule
            Dim TRegionId = ScheduleResultItem.Schedule(0).RegionId
            Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)

            If RegionProperty.EntityType = "league" Then
                Dim FoundTeamIds As New List(Of String)
                For Each ScheduleItem In ScheduleResultItem.Schedule
                    Dim HomeTeam = Teams.Find(Function(T) T.RealTeamName <> "" AndAlso T.RegionId = TRegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.Trim().ToLower AndAlso TeamRegionIds.Contains(T.RealTeamId))
                    Dim AwayTeam = Teams.Find(Function(T) T.RealTeamName <> "" AndAlso T.RegionId = TRegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.Trim().ToLower AndAlso TeamRegionIds.Contains(T.RealTeamId))
                    If HomeTeam IsNot Nothing AndAlso Not FoundTeamIds.Contains(HomeTeam.RealTeamId) Then FoundTeamIds.Add(HomeTeam.RealTeamId)
                    If AwayTeam IsNot Nothing AndAlso Not FoundTeamIds.Contains(AwayTeam.RealTeamId) Then FoundTeamIds.Add(AwayTeam.RealTeamId)
                Next

                For Each FoundTeamId In FoundTeamIds
                    Dim NewScheduleResultItem As New ScheduleResult
                    NewScheduleResultItem.ScheduleTeamConfirms = ScheduleResultItem.ScheduleTeamConfirms

                    Dim FoundRealTeamIndex = -1
                    Dim I As Integer = 0
                    For Each ScheduleItem In ScheduleResultItem.Schedule
                        Dim HomeTeam = Teams.Find(Function(T) T.RealTeamName <> "" AndAlso T.RegionId = TRegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.Trim().ToLower AndAlso T.RealTeamId = FoundTeamId)
                        Dim AwayTeam = Teams.Find(Function(T) T.RealTeamName <> "" AndAlso T.RegionId = TRegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.Trim().ToLower AndAlso T.RealTeamId = FoundTeamId)

                        If HomeTeam IsNot Nothing OrElse AwayTeam IsNot Nothing Then FoundRealTeamIndex = I

                        NewScheduleResultItem.Schedule.Add(ConvertToLinkScheduleItem(ScheduleItem, FoundTeamId, RegionLeagues))
                        I += 1
                    Next

                    If FoundRealTeamIndex <> ScheduleResultItem.Schedule.Count - 1 Then
                        NewScheduleResultItem.Schedule.RemoveRange(FoundRealTeamIndex + 1, ScheduleResultItem.Schedule.Count - 1 - FoundRealTeamIndex)
                        Dim ClonedItem = NewScheduleResultItem.Schedule.Last().CloneItem()
                        ClonedItem.IsDeleted = True
                        NewScheduleResultItem.Schedule.Add(ClonedItem)
                    End If

                    NewSchedule.Add(NewScheduleResultItem)
                Next
            End If
        Next
        Schedule.AddRange(NewSchedule)

        Schedule.Sort(ScheduleResult.BasicSorter)

        MergeTeamData(Schedule)

        Dim MyRegionUsers As New Dictionary(Of String, List(Of RegionUser))
        For Each RegionUser In RegionUsers
            If RegionUser.RealUsername = Username Then
                If Not MyRegionUsers.ContainsKey(RegionUser.RegionId) Then MyRegionUsers.Add(RegionUser.RegionId, New List(Of RegionUser))
                MyRegionUsers(RegionUser.RegionId).Add(RegionUser)
            End If
        Next

        If IsAvailableSpots Then
            'Build MySchedule
            MySchedule = New List(Of Schedule)
            If Not IsSimple Then
                For Each ScheduleResultItem In Schedule
                    If ScheduleResultItem.Schedule.Last().IsCancelled OrElse ScheduleResultItem.Schedule.Last().IsDeleted Then Continue For

                    Dim TRegionId As String = ScheduleResultItem.Schedule(0).RegionId
                    Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)
                    If RegionProperty.EntityType = "team" Then
                        Dim TMyRegionUsers = New List(Of RegionUser)
                        If MyRegionUsers.ContainsKey(TRegionId) Then
                            TMyRegionUsers = MyRegionUsers(TRegionId)
                        End If

                        For Each RegionUser In TMyRegionUsers
                            Dim ScheduleTeamConfirm = ScheduleResultItem.ScheduleTeamConfirms.Find(Function(SC) SC.Username = RegionUser.Username)
                            If ScheduleTeamConfirm IsNot Nothing AndAlso ScheduleTeamConfirm.VersionId = ScheduleResultItem.Schedule.Last().VersionId AndAlso ScheduleTeamConfirm.Confirmed = 2 Then Continue For

                            Dim FoundItem As Boolean = False
                            If RegionUser.IsPlayer OrElse RegionUser.IsCoach Then
                                FoundItem = True
                            ElseIf RegionUser.IsCallup Then
                                Dim ScheduleCallup = ScheduleResultItem.Schedule.Last().ScheduleCallUps.Find(Function(SC) SC.Username = RegionUser.Username)
                                If ScheduleCallup IsNot Nothing Then
                                    FoundItem = True
                                End If
                            End If
                            If FoundItem Then
                                Dim NewScheduleItem = ScheduleResultItem.Schedule.Last().CloneItem()
                                NewScheduleItem.OfficialId = RegionUser.Username
                                MySchedule.Add(NewScheduleItem)
                            End If
                        Next
                    ElseIf RegionProperty.EntityType = "referee" Then
                        Dim TMyRegionUsers = New List(Of RegionUser)
                        If MyRegionUsers.ContainsKey(TRegionId) Then
                            TMyRegionUsers = MyRegionUsers(TRegionId)
                        End If

                        For Each RegionUser In TMyRegionUsers
                            If ScheduleResultItem.Schedule.Last().ContainsOfficialIdOnPosition(RegionUser.Username) Then
                                Dim NewScheduleItem = ScheduleResultItem.Schedule.Last().CloneItem()
                                NewScheduleItem.OfficialId = RegionUser.Username
                                MySchedule.Add(NewScheduleItem)
                            End If
                        Next
                    End If
                Next
            End If

            'Filter Schedule
            Dim TNewSchedule As New List(Of ScheduleResult)
            For Each ScheduleResultItem In Schedule
                Dim TRegionId As String = ScheduleResultItem.Schedule(0).RegionId

                If DisplayingScheduleRegionIds.Contains(TRegionId) Then
                    Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)

                    Dim TMyRegionUsers = New List(Of RegionUser)
                    If MyRegionUsers.ContainsKey(TRegionId) Then
                        TMyRegionUsers = MyRegionUsers(TRegionId)
                    End If

                    Dim CanViewPositions As Boolean = RegionProperty.ShowFullScheduleToNonMembers OrElse TMyRegionUsers.Any(Function(RU) RU.CanViewMasterSchedule OrElse RU.IsExecutive)
                    If Not CanViewPositions Then
                        For Each SC In ScheduleResultItem.Schedule
                            If SC.GameDate < DateTime.UtcNow.AddDays(-1) Then
                                For Each SP In SC.SchedulePositions
                                    SP.OfficialId = ""
                                Next
                            Else
                                For Each SP In SC.SchedulePositions
                                    If SP.OfficialId = "" Then
                                        SP.OfficialId = "open"
                                    Else
                                        SP.OfficialId = "taken"
                                    End If
                                Next
                            End If
                            SC.ScheduleFines = SC.ScheduleFines.FindAll(Function(SF) TMyRegionUsers.Exists(Function(RU) RU.RealUsername = Username AndAlso SF.OfficialId = RU.Username))
                            SC.ScheduleUserComments = SC.ScheduleUserComments.FindAll(Function(SU) TMyRegionUsers.Exists(Function(RU) RU.RealUsername = Username AndAlso SU.OfficialId = RU.Username))
                        Next
                    End If
                    ScheduleResultItem.ScheduleConfirms = New List(Of ScheduleConfirm)
                    ScheduleResultItem.ScheduleTeamConfirms = New List(Of ScheduleTeamConfirm)

                    TNewSchedule.Add(ScheduleResultItem)
                End If
            Next
            Schedule = TNewSchedule
        Else
            'Build LinkedSchedule
            If Not IsSimple Then
                Dim TTNewSchedule As New List(Of ScheduleResult)


                For Each ScheduleResultItem In Schedule
                    Dim TRegionId = ScheduleResultItem.Schedule(0).RegionId
                    Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)

                    If TRegionId <> "allregions" AndAlso Not TRegionId <> "alleditableregions" Then
                        DisplayingScheduleRegionIds.Contains(TRegionId)
                    End If

                    If RegionProperty.EntityType = "league" Then
                        Dim FoundOfficialRegionIds As New List(Of String)
                        For Each ScheduleItem In ScheduleResultItem.Schedule
                            Dim OfficialRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.OfficialRegionId.ToLower AndAlso RefereeRegionIdAdmins.Contains(O.RealOfficialRegionId) AndAlso DisplayingScheduleRegionIds.Contains(O.RealOfficialRegionId))
                            Dim ScorekeeperRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.ScorekeeperRegionId.ToLower AndAlso RefereeRegionIdAdmins.Contains(O.RealOfficialRegionId) AndAlso DisplayingScheduleRegionIds.Contains(O.RealOfficialRegionId))
                            Dim SupervisorRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.SupervisorRegionId.ToLower AndAlso RefereeRegionIdAdmins.Contains(O.RealOfficialRegionId) AndAlso DisplayingScheduleRegionIds.Contains(O.RealOfficialRegionId))

                            If OfficialRegion IsNot Nothing AndAlso Not FoundOfficialRegionIds.Contains(OfficialRegion.RealOfficialRegionId) Then FoundOfficialRegionIds.Add(OfficialRegion.RealOfficialRegionId)
                            If ScorekeeperRegion IsNot Nothing AndAlso Not FoundOfficialRegionIds.Contains(ScorekeeperRegion.RealOfficialRegionId) Then FoundOfficialRegionIds.Add(ScorekeeperRegion.RealOfficialRegionId)
                            If SupervisorRegion IsNot Nothing AndAlso Not FoundOfficialRegionIds.Contains(SupervisorRegion.RealOfficialRegionId) Then FoundOfficialRegionIds.Add(SupervisorRegion.RealOfficialRegionId)
                        Next

                        For Each FoundOfficialRegionId In FoundOfficialRegionIds
                            Dim NewScheduleResultItem As New ScheduleResult
                            NewScheduleResultItem.ScheduleTeamConfirms = ScheduleResultItem.ScheduleTeamConfirms

                            Dim I As Integer = 0
                            Dim LastFoundIndex As Integer = -1
                            For Each ScheduleItem In ScheduleResultItem.Schedule
                                Dim OfficialRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.OfficialRegionId.ToLower AndAlso O.RealOfficialRegionId = FoundOfficialRegionId)
                                Dim ScorekeeperRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.ScorekeeperRegionId.ToLower AndAlso O.RealOfficialRegionId = FoundOfficialRegionId)
                                Dim SupvervisorRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.SupervisorRegionId.ToLower AndAlso O.RealOfficialRegionId = FoundOfficialRegionId)

                                If OfficialRegion IsNot Nothing OrElse ScorekeeperRegion IsNot Nothing OrElse SupvervisorRegion IsNot Nothing Then LastFoundIndex = I

                                Dim LinkedScheduleItem = ConvertToLinkScheduleItem(ScheduleItem, FoundOfficialRegionId, RegionLeagues)

                                Dim CrewType As New Dictionary(Of String, String)

                                If LinkedScheduleItem.CrewType IsNot Nothing AndAlso LinkedScheduleItem.CrewType.ContainsKey("umpire") Then
                                    If OfficialRegion IsNot Nothing Then
                                        CrewType.Add("umpire", LinkedScheduleItem.CrewType("umpire"))
                                    Else
                                        CrewType.Add("umpire", "0-man")
                                    End If
                                End If

                                If LinkedScheduleItem.CrewType IsNot Nothing AndAlso LinkedScheduleItem.CrewType.ContainsKey("scorekeeper") Then
                                    If ScorekeeperRegion IsNot Nothing Then
                                        CrewType.Add("scorekeeper", LinkedScheduleItem.CrewType("scorekeeper"))
                                    Else
                                        CrewType.Add("scorekeeper", "0-man")
                                    End If
                                End If


                                If LinkedScheduleItem.CrewType IsNot Nothing AndAlso LinkedScheduleItem.CrewType.ContainsKey("supervisor") Then
                                    If ScorekeeperRegion IsNot Nothing Then
                                        CrewType.Add("supervisor", LinkedScheduleItem.CrewType("supervisor"))
                                    Else
                                        CrewType.Add("supervisor", "0-man")
                                    End If
                                End If

                                LinkedScheduleItem.CrewType = CrewType

                                NewScheduleResultItem.Schedule.Add(LinkedScheduleItem)
                                I += 1
                            Next
                            If LastFoundIndex <> ScheduleResultItem.Schedule.Count - 1 Then
                                NewScheduleResultItem.Schedule.RemoveRange(LastFoundIndex, ScheduleResultItem.Schedule.Count - 1 - LastFoundIndex)
                                Dim ClonedItem = NewScheduleResultItem.Schedule.Last().CloneItem
                                ClonedItem.IsDeleted = True
                                NewScheduleResultItem.Schedule.Add(ClonedItem)
                            End If

                            If Not NewScheduleResultItem.Schedule.Last().IsDeleted Then
                                LinkedSchedule.Add(NewScheduleResultItem.Schedule.Last())
                            End If
                        Next
                    End If
                Next

                'Build LeagueConfirms
                For Each ScheduleResultItem In Schedule
                    Dim TRegionId = ScheduleResultItem.Schedule(0).RegionId
                    Dim TScheduleId = ScheduleResultItem.Schedule(0).ScheduleId
                    Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)

                    If RegionProperty.EntityType = "league" Then
                        'Find Linked Regions
                        Dim FoundTeamRegionIds As New List(Of String)
                        Dim FoundOfficialRegionIds As New List(Of String)

                        For Each ScheduleItem In ScheduleResultItem.Schedule
                            Dim HomeTeam = Teams.Find(Function(T) T.RealTeamId <> "" AndAlso T.RegionId = TRegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.ToLower)
                            Dim AwayTeam = Teams.Find(Function(T) T.RealTeamId <> "" AndAlso T.RegionId = TRegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.ToLower)
                            Dim OfficialRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.OfficialRegionId.ToLower)
                            Dim ScorekeeperRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.ScorekeeperRegionId.ToLower)
                            Dim SupervisorRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = TRegionId AndAlso O.OfficialRegionId = ScheduleItem.SupervisorRegionId.ToLower)

                            If HomeTeam IsNot Nothing AndAlso Not FoundTeamRegionIds.Contains(HomeTeam.RealTeamId) Then FoundTeamRegionIds.Add(HomeTeam.RealTeamId)
                            If AwayTeam IsNot Nothing AndAlso Not FoundTeamRegionIds.Contains(AwayTeam.RealTeamId) Then FoundTeamRegionIds.Add(AwayTeam.RealTeamId)
                            If OfficialRegion IsNot Nothing AndAlso Not FoundOfficialRegionIds.Contains(OfficialRegion.RealOfficialRegionId) Then FoundOfficialRegionIds.Add(OfficialRegion.RealOfficialRegionId)
                            If ScorekeeperRegion IsNot Nothing AndAlso Not FoundOfficialRegionIds.Contains(ScorekeeperRegion.RealOfficialRegionId) Then FoundOfficialRegionIds.Add(ScorekeeperRegion.RealOfficialRegionId)
                            If SupervisorRegion IsNot Nothing AndAlso Not FoundOfficialRegionIds.Contains(SupervisorRegion.RealOfficialRegionId) Then FoundOfficialRegionIds.Add(SupervisorRegion.RealOfficialRegionId)

                        Next

                        For Each FoundTeamRegionId In FoundTeamRegionIds
                            Dim LinkedScheduleResultItem = Schedule.Find(Function(S) S.Schedule(0).RegionId = FoundTeamRegionId AndAlso S.Schedule(0).LinkedRegionId = TRegionId AndAlso S.Schedule(0).ScheduleId = TScheduleId)
                            If LinkedScheduleResultItem IsNot Nothing Then
                                Dim ScheduleConfirms = LinkedScheduleResultItem.ScheduleTeamConfirms.FindAll(Function(SC) RegionUsers.Any(Function(RU) RU.RegionId = FoundTeamRegionId AndAlso RU.IsCoach AndAlso RU.Username = SC.Username))
                                If ScheduleConfirms.Count > 0 Then
                                    Dim MaxVersionId = ScheduleConfirms.Max(Function(SC) SC.VersionId)
                                    Dim NewScheduleItem = LinkedScheduleResultItem.Schedule(MaxVersionId - 1).CloneItem()
                                    NewScheduleItem.ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam)
                                    NewScheduleItem.ScheduleCommentTeams = New List(Of ScheduleCommentTeam)
                                    NewScheduleItem.ScheduleCallUps = New List(Of ScheduleCallup)
                                    ScheduleResultItem.RegionConfirms.Add(NewScheduleItem)
                                End If
                            End If
                        Next

                        For Each FoundOfficialRegionId In FoundOfficialRegionIds
                            Dim LinkedScheduleResultItem = Schedule.Find(Function(S) S.Schedule(0).RegionId = FoundOfficialRegionId AndAlso S.Schedule(0).LinkedRegionId = TRegionId AndAlso S.Schedule(0).LinkedScheduleId = TScheduleId)
                            If LinkedScheduleResultItem IsNot Nothing Then
                                Dim NewScheduleItem = LinkedScheduleResultItem.Schedule.Last().CloneItem()
                                NewScheduleItem.ScheduleUserComments = New List(Of ScheduleUserComment)
                                NewScheduleItem.ScheduleCommentTeams = New List(Of ScheduleCommentTeam)
                                NewScheduleItem.ScheduleFines = New List(Of ScheduleFine)
                                NewScheduleItem.SchedulePositions = New List(Of SchedulePosition)
                                ScheduleResultItem.RegionConfirms.Add(NewScheduleItem)
                            End If
                        Next
                    End If
                Next
            End If

            'Filter Schedule
            Dim TNewSchedule As New List(Of ScheduleResult)
            For Each ScheduleResultItem In Schedule
                Dim TRegionId As String = ScheduleResultItem.Schedule(0).RegionId
                Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)

                If DisplayingScheduleRegionIds.Contains(TRegionId) Then
                    TNewSchedule.Add(ScheduleResultItem)
                Else
                    Dim LastItem = ScheduleResultItem.Schedule.Last()
                    If Not LastItem.IsDeleted Then
                        If RegionProperty.EntityType = "referee" Then
                            If Not LastItem.IsCancelled AndAlso Not LastItem.IsDeleted Then
                                For Each SP In LastItem.SchedulePositions
                                    Dim SPL = SP.OfficialId.ToLower.Trim()
                                    If SPL <> "" Then
                                        Dim RegionUser = PublicCode.BinarySearchItem(RegionUsers, New RegionUser With {.RegionId = TRegionId, .Username = SPL}, UmpireAssignor.RegionUser.BasicSorter)
                                        If RegionUser IsNot Nothing Then
                                            If RegionUsersAdmin.Any(Function(RU) RU.IsSimilarUser(RegionUser)) Then
                                                Dim NewScheduleResultItem = New ScheduleResult()

                                                LastItem = LastItem.CloneItem()
                                                LastItem.ScheduleFines = New List(Of ScheduleFine)
                                                LastItem.ScheduleUserComments = New List(Of ScheduleUserComment)
                                                LastItem.ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam)
                                                LastItem.ScheduleCommentTeams = New List(Of ScheduleCommentTeam)

                                                NewScheduleResultItem.Schedule.Add(LastItem)
                                                TNewSchedule.Add(NewScheduleResultItem)
                                                Exit For
                                            End If
                                        End If
                                    End If
                                Next
                            End If
                        ElseIf RegionProperty.EntityType = "team" Then
                            If Not LastItem.IsCancelled Then
                                For Each RegionUser In RegionUsers
                                    If RegionUser.RegionId = TRegionId AndAlso (RegionUser.IsPlayer OrElse RegionUser.IsCoach OrElse (RegionUser.IsCallup AndAlso LastItem.ScheduleCallUps.Any(Function(SC) SC.Username = RegionUser.Username))) Then
                                        Dim ScheduleTeamConfirm = ScheduleResultItem.ScheduleTeamConfirms.Find(Function(SC) SC.Username = RegionUser.Username)
                                        If ScheduleTeamConfirm Is Nothing OrElse ScheduleTeamConfirm.Confirmed <> 2 Then
                                            If RegionUsersAdmin.Any(Function(RU) RU.IsSimilarUser(RegionUser)) Then
                                                Dim NewScheduleResultItem = New ScheduleResult()

                                                LastItem = LastItem.CloneItem()
                                                LastItem.ScheduleFines = New List(Of ScheduleFine)
                                                LastItem.ScheduleUserComments = New List(Of ScheduleUserComment)
                                                LastItem.ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam)
                                                LastItem.ScheduleCommentTeams = New List(Of ScheduleCommentTeam)

                                                NewScheduleResultItem.Schedule.Add(LastItem)
                                                NewScheduleResultItem.ScheduleTeamConfirms = ScheduleResultItem.ScheduleTeamConfirms
                                                TNewSchedule.Add(NewScheduleResultItem)
                                                Exit For
                                            End If
                                        End If
                                    End If
                                Next
                            End If
                        End If
                    End If
                End If
            Next
            Schedule = TNewSchedule
        End If

        RegionUser.CleanRegionUsers(RegionProperties, RegionUsers, Username)

        For I As Integer = 0 To Schedule.Count - 1
            For N As Integer = 0 To Schedule(I).Schedule.Count - 1
                Schedule(I).Schedule(N) = TrimHiddenScheduleData(Schedule(I).Schedule(N), RegionProperties, RegionUsers, Username)
            Next
        Next

        For I As Integer = 0 To FullSchedule.Count - 1
            FullSchedule(I) = TrimHiddenScheduleData(FullSchedule(I), RegionProperties, RegionUsers, Username)
        Next

        Dim SingleSchedule As Schedule = Nothing
        For Each ScheduleGroup In Schedule
            For Each ScheduleItem In ScheduleGroup.Schedule
                If ScheduleItem.ScheduleId = ScheduleId Then
                    SingleSchedule = ScheduleItem.CloneItem
                End If
            Next
        Next

        Return SingleSchedule
    End Function

    Public Shared Function ConvertScheduleResultsToSchedule(FullSchedule As List(Of ScheduleResult)) As List(Of Schedule)
        Dim FullScheduleSimple As New List(Of Schedule)

        For Each FullScheduleItem In FullSchedule
            Dim LastItem = FullScheduleItem.Schedule.Last()
            If LastItem.IsDeleted Then Continue For
            FullScheduleSimple.Add(LastItem)
        Next

        Return FullScheduleSimple
    End Function

    Public Shared Function GetMergedSchedule(Regions As List(Of RegionProperties), FullSchedule As List(Of ScheduleResult), ScheduleTemps As List(Of ScheduleTemp)) As Dictionary(Of String, List(Of Schedule))
        Dim FullScheduleSimple = ConvertScheduleResultsToSchedule(FullSchedule)
        Dim MergedSchedule = GetMergedSchedule(Regions, ScheduleTemps, FullScheduleSimple)
        Return ConvertScheduleToScheduleDic(MergedSchedule)
    End Function

    Public Shared Function ConvertScheduleToScheduleDic(Schedule As List(Of Schedule)) As Dictionary(Of String, List(Of Schedule))
        Dim Result As New Dictionary(Of String, List(Of Schedule))
        For Each MergedScheduleItem In Schedule
            If Not Result.ContainsKey(MergedScheduleItem.RegionId) Then Result.Add(MergedScheduleItem.RegionId, New List(Of Schedule))
            Result(MergedScheduleItem.RegionId).Add(MergedScheduleItem)
        Next
        Return Result
    End Function

    Public Function GetIsDeletedTeamItem(RegionUser As RegionUser)
        Return IsDeleted OrElse IsCancelled() OrElse (RegionUser IsNot Nothing AndAlso RegionUser.IsOnlyCallUp AndAlso Not ScheduleCallUps.Any(Function(SC) SC.Username = RegionUser.Username))
    End Function

    Public Function GetIsDeletedRefereeItem(RegionUser As RegionUser)
        Return IsDeleted OrElse (Not SchedulePositions.Any(Function(SC) SC.OfficialId = RegionUser.Username) AndAlso Not ScheduleFines.Any(Function(SF) SF.OfficialId = RegionUser.Username))
    End Function

    Public Shared Function GetUsersGameNotifications(Username As String, Optional SQLConnection As SqlConnection = Nothing, Optional SQLTransaction As SqlTransaction = Nothing) As Object
        Dim Result = GetUsersScheduleFromRegionDateRange(Username, "allregions", "", Date.UtcNow.AddDays(-1), Data.SqlTypes.SqlDateTime.MaxValue, Username, False, "", "", "", "", "", "", SQLConnection, SQLTransaction)

        If Not TypeOf Result Is ErrorObject Then
            Dim Schedule As List(Of ScheduleResult) = Result.Schedule
            Dim RegionProperties As List(Of RegionProperties) = Result.RegionProperties
            Dim RegionUsers As List(Of RegionUser) = Result.RegionUsers
            Dim NewSchedule As New List(Of ScheduleResult)

            For Each ScheduleResultItem In Schedule
                Dim TRegionId As String = ScheduleResultItem.Schedule(0).RegionId
                Dim OfficialId As String = ScheduleResultItem.Schedule(0).OfficialId
                Dim LastScheduleItem = ScheduleResultItem.Schedule.Last()

                Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)
                Dim RegionUser = RegionUsers.Find(Function(RU) RU.RegionId = TRegionId AndAlso RU.Username = OfficialId)

                Dim TimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(RegionProperty.TimeZone)

                If LastScheduleItem.GameDate < Date.UtcNow.AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours) Then Continue For

                If RegionProperty.EntityType = "team" Then
                    Dim VersionId As Integer = 0
                    Dim ScheduleTeamConfirm = ScheduleResultItem.ScheduleTeamConfirms.Find(Function(SC) SC.Username = OfficialId)
                    Dim IsDeleted As Boolean = LastScheduleItem.GetIsDeletedTeamItem(RegionUser)
                    If ScheduleTeamConfirm IsNot Nothing AndAlso (ScheduleTeamConfirm.Confirmed = 1 OrElse ScheduleTeamConfirm.Confirmed = 2) Then
                        VersionId = Math.Min(ScheduleResultItem.Schedule.Count - 1, ScheduleTeamConfirm.VersionId - 1)
                        If (VersionId < 0) Then VersionId = 0
                    Else
                        ScheduleTeamConfirm = Nothing
                    End If
                    Dim OldIsDeleted As Boolean = True
                    If ScheduleTeamConfirm IsNot Nothing Then
                        OldIsDeleted = ScheduleResultItem.Schedule(VersionId).GetIsDeletedTeamItem(RegionUser)
                    End If

                    If IsDeleted AndAlso OldIsDeleted Then
                        For I As Integer = VersionId To ScheduleResultItem.Schedule.Count - 1
                            Dim ScheduleItem = ScheduleResultItem.Schedule(I)
                            If Not ScheduleItem.GetIsDeletedTeamItem(RegionUser) Then
                                NewSchedule.Add(ScheduleResultItem)
                                Exit For
                            End If
                        Next
                    Else
                        If Not IsDeleted AndAlso Not OldIsDeleted Then
                            For I As Integer = VersionId To ScheduleResultItem.Schedule.Count - 1
                                Dim ScheduleItem = ScheduleResultItem.Schedule(I)
                                If IsDeleted <> ScheduleItem.GetIsDeletedTeamItem(RegionUser) Then
                                    NewSchedule.Add(ScheduleResultItem)
                                    Exit For
                                End If
                                If RequiredGameNotification(LastScheduleItem, ScheduleItem) Then
                                    NewSchedule.Add(ScheduleResultItem)
                                    Exit For
                                End If
                            Next
                        Else
                            NewSchedule.Add(ScheduleResultItem)
                        End If
                    End If
                ElseIf RegionProperty.EntityType = "referee" Then
                    Dim VersionId As Integer = 0
                    Dim ScheduleConfirm = ScheduleResultItem.ScheduleConfirms.Find(Function(SC) SC.Username = OfficialId)
                    Dim IsDeleted As Boolean = LastScheduleItem.GetIsDeletedRefereeItem(RegionUser)
                    If ScheduleConfirm IsNot Nothing Then
                        VersionId = Math.Min(ScheduleResultItem.Schedule.Count - 1, ScheduleConfirm.VersionId - 1)
                        If (VersionId < 0) Then VersionId = 0
                    End If
                    Dim OldIsDeleted As Boolean = True
                    If ScheduleConfirm IsNot Nothing Then
                        OldIsDeleted = ScheduleResultItem.Schedule(VersionId).GetIsDeletedRefereeItem(RegionUser)
                    End If

                    If IsDeleted AndAlso OldIsDeleted Then
                        For I As Integer = VersionId To ScheduleResultItem.Schedule.Count - 1
                            Dim ScheduleItem = ScheduleResultItem.Schedule(I)
                            If Not ScheduleItem.GetIsDeletedRefereeItem(RegionUser) Then
                                NewSchedule.Add(ScheduleResultItem)
                                Exit For
                            End If
                        Next
                    Else
                        If Not IsDeleted AndAlso Not OldIsDeleted Then
                            For I As Integer = VersionId To ScheduleResultItem.Schedule.Count - 1
                                Dim ScheduleItem = ScheduleResultItem.Schedule(I)
                                If IsDeleted <> ScheduleItem.GetIsDeletedRefereeItem(RegionUser) Then
                                    NewSchedule.Add(ScheduleResultItem)
                                    Exit For
                                End If
                                If RequiredGameNotification(LastScheduleItem, ScheduleItem) OrElse RequiredGameNotificationPosition(LastScheduleItem, ScheduleItem, OfficialId) <> "SamePosition" OrElse RequiredGameNotificationFine(LastScheduleItem, ScheduleItem, OfficialId) Then
                                    NewSchedule.Add(ScheduleResultItem)
                                    Exit For
                                End If
                            Next
                        Else
                            NewSchedule.Add(ScheduleResultItem)
                        End If
                    End If
                End If
            Next
            Result.Schedule = NewSchedule
        End If

        Return Result
    End Function

    Public Shared Function GetAllLinkedLeagueIds(RegionProperties As List(Of RegionProperties), RegionLeagues As List(Of RegionLeaguePayContracted)) As List(Of String)
        Dim LinkedLeagueIds = New List(Of String)
        For Each RegionLeague In RegionLeagues
            Dim TRegion = RegionProperties.Find(Function(R) R.RegionId = RegionLeague.RegionId)

            If TRegion.EntityType = "team" Then
                If RegionLeague.RealLeagueId <> "" Then
                    If Not LinkedLeagueIds.Contains(RegionLeague.RealLeagueId) Then
                        LinkedLeagueIds.Add(RegionLeague.RealLeagueId)
                    End If
                End If
            End If
        Next
        Return LinkedLeagueIds
    End Function

    Public Shared Sub MergeScheduleResult(MergedScheduleResultTeam As List(Of ScheduleResult), ScheduleResultTeamItem As ScheduleResult, RegionUser As RegionUser)
        If RegionUser.IsTeamRegular() Then
            MergedScheduleResultTeam.Add(ScheduleResultTeamItem)
        ElseIf RegionUser.IsOnlyCallUp() Then
            Dim LastGameIndex As Integer = -1
            For I As Integer = 0 To ScheduleResultTeamItem.Schedule.Count - 1
                Dim ScheduleItem = ScheduleResultTeamItem.Schedule(I)

                Dim ScheduleCallup = ScheduleItem.ScheduleCallUps.Find(Function(SC) SC.LinkedRegionId = RegionUser.RegionId AndAlso SC.Username = RegionUser.Username)
                If ScheduleCallup IsNot Nothing Then
                    LastGameIndex = I
                End If
            Next
            If LastGameIndex <> -1 AndAlso LastGameIndex <> ScheduleResultTeamItem.Schedule.Count - 1 Then
                Dim NewScheduleResultTeamItem As New ScheduleResult
                NewScheduleResultTeamItem.ScheduleTeamConfirms = ScheduleResultTeamItem.ScheduleTeamConfirms
                For I As Integer = 0 To LastGameIndex
                    NewScheduleResultTeamItem.Schedule.Add(ScheduleResultTeamItem.Schedule(I))
                Next
                If Not ScheduleResultTeamItem.Schedule(LastGameIndex).IsDeleted Then
                    Dim ClonedItem = ScheduleResultTeamItem.Schedule(LastGameIndex).CloneItem()
                    ClonedItem.IsDeleted = True
                    ClonedItem.VersionId += 1
                    NewScheduleResultTeamItem.Schedule.Add(ClonedItem)
                End If
                MergedScheduleResultTeam.Add(NewScheduleResultTeamItem)
            ElseIf LastGameIndex <> -1 Then
                MergedScheduleResultTeam.Add(ScheduleResultTeamItem)
            End If
        End If
    End Sub

    Public Shared Function ConvertScheduleResultsToSchedule(ScheduleResults As List(Of ScheduleResult), RegionUsers As List(Of RegionUser), RegionProperties As List(Of RegionProperties)) As List(Of Schedule)
        Dim Result As New List(Of Schedule)
        For Each ScheduleResult In ScheduleResults
            Dim ScheduleItem = ScheduleResult.Schedule.Last()
            If Not ScheduleItem.IsDeleted Then
                Dim TRegionId = ScheduleItem.RegionId
                Dim RegionProperty = RegionProperties.Find(Function(R) R.RegionId = TRegionId)
                If RegionProperty.EntityType = "team" Then
                    If ScheduleItem.OfficialId = "" Then
                        Result.Add(ScheduleItem)
                    Else
                        Dim RegionUser = RegionUsers.Find(Function(RU) RU.RegionId = TRegionId AndAlso RU.Username = ScheduleItem.OfficialId)
                        If RegionUser.IsPlayer OrElse RegionUser.IsCoach Then
                            Result.Add(ScheduleItem)
                        ElseIf RegionUser.IsCallup Then
                            If ScheduleItem.ScheduleCallUps.Any(Function(SC) SC.Username = ScheduleItem.OfficialId) Then
                                Result.Add(ScheduleItem)
                            End If
                        End If
                    End If
                Else
                    Result.Add(ScheduleItem)
                End If
            End If
        Next
        Return Result
    End Function

    Public Shared Function GetUsersScheduleFromRegionDateRangeInnerHelper(Username As String, RegionId As String, OfficialId As String, StartDate As Date, EndDate As Date, IsSingleSchedule As Boolean, RealUsername As String, Email As String, UserRegionId As String, UserOfficialId As String, LastName As String, FirstName As String, LoggedInUsername As String, ByRef UniqueRegionIds As List(Of String), ByRef RegionProperties As List(Of RegionProperties), ByRef RegionUsers As List(Of RegionUser), ByRef RegionParks As List(Of Park), ByRef RegionLeagues As List(Of RegionLeaguePayContracted), ByRef Teams As List(Of Team), ByRef OfficialRegions As List(Of OfficialRegion), ByRef LinkedLeagueIds As List(Of String), ByRef Schedule As List(Of ScheduleResult), ByRef StatGames As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, Dictionary(Of Integer, List(Of StatGame)))))), ByRef FullSchedule As List(Of Schedule), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As Object
        Dim QueryRegionUser = New RegionUser With {
            .RegionId = UserRegionId,
            .Username = UserOfficialId,
            .FirstName = FirstName,
            .LastName = LastName,
            .Email = Email,
            .RealUsername = RealUsername
        }

        Dim RegionPropertiesFetcher As New RegionPropertiesFetcher
        Dim RegionUserFetcher As New RegionUserFetcher
        Dim TeamFetcher As New TeamFetcher
        Dim ParkFetcher As New ParkFetcher
        Dim RegionLeagueFetcher As New RegionLeaguePayContractedFetcher
        Dim OfficialRegionFetcher As New OfficialRegionFetcher

        If RegionId = "allregions" Then
            UniqueRegionIds = RegionUser.GetAllMyRegionsIds(Username, SQLConnection, SQLTransaction)
            If IsSingleSchedule Then
                Dim OtherScheduleResultReferee As New List(Of ScheduleResult)
                Dim OtherMergedScheduleResultTeam As New List(Of ScheduleResult)

                If UniqueRegionIds.Count > 0 Then
                    RegionLeagues = RegionLeagueFetcher.GetRegionLeaguesPayContractedRegionIdsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)
                    RegionProperties = RegionPropertiesFetcher.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)

                    Dim LinkedAndMyTeamRegionIds = New List(Of String)
                    LinkedAndMyTeamRegionIds.AddRange(UniqueRegionIds)
                    For Each RegionLeague In RegionLeagues
                        Dim TTRegion As RegionProperties = PublicCode.BinarySearchItem(RegionProperties, New RegionProperties With {.RegionId = RegionLeague.RegionId}, UmpireAssignor.RegionProperties.BasicSorter)

                        If TTRegion IsNot Nothing AndAlso TTRegion.EntityType = "team" Then
                            If RegionLeague.RealLeagueId <> "" AndAlso Not LinkedAndMyTeamRegionIds.Contains(RegionLeague.RealLeagueId) Then
                                LinkedAndMyTeamRegionIds.Add(RegionLeague.RealLeagueId)
                            End If
                        End If
                    Next

                    Teams = TeamFetcher.GetTeamsInRegionsHelper(LinkedAndMyTeamRegionIds, SQLConnection, SQLTransaction)

                    For Each Team In Teams
                        If Team.RealTeamId <> "" AndAlso Not LinkedAndMyTeamRegionIds.Contains(Team.RealTeamId) Then
                            LinkedAndMyTeamRegionIds.Add(Team.RealTeamId)
                        End If
                    Next

                    RegionLeagues = RegionLeagueFetcher.GetRegionLeaguesPayContractedRegionIdsHelper(LinkedAndMyTeamRegionIds, SQLConnection, SQLTransaction)
                    RegionUsers = RegionUserFetcher.LoadAllInRegionIdsHelper(LinkedAndMyTeamRegionIds, Username, SQLConnection, SQLTransaction)
                    RegionProperties = RegionPropertiesFetcher.GetRegionPropertiesRegionIdsHelper(LinkedAndMyTeamRegionIds, SQLConnection, SQLTransaction)
                    RegionParks = ParkFetcher.GetParksInRegionsHelper(LinkedAndMyTeamRegionIds, SQLConnection, SQLTransaction)

                    Dim RegionIdsImExecutiveFor As New List(Of String)
                    Dim RegionIdsICanViewMasterSchedule As New List(Of String)

                    'Get Loggedin Users permissions to see the schedules of regionusers
                    For Each RegionUser In RegionUsers
                        If RegionUser.RealUsername = Username Then
                            If RegionUser.IsExecutive Then
                                If Not RegionIdsImExecutiveFor.Contains(RegionUser.RegionId) Then
                                    RegionIdsImExecutiveFor.Add(RegionUser.RegionId)
                                End If
                                If Not RegionIdsICanViewMasterSchedule.Contains(RegionUser.RegionId) Then
                                    RegionIdsICanViewMasterSchedule.Add(RegionUser.RegionId)
                                End If
                            Else
                                Dim TRegion = RegionProperties.Find(Function(R) R.RegionId = RegionUser.RegionId)
                                If RegionUser.CanViewMasterSchedule OrElse TRegion.ShowFullScheduleToNonMembers Then
                                    If Not RegionIdsICanViewMasterSchedule.Contains(RegionUser.RegionId) Then
                                        RegionIdsICanViewMasterSchedule.Add(RegionUser.RegionId)
                                    End If
                                End If
                            End If
                        End If
                    Next

                    'If Email matches email of another regionuser with realusername in another region you are executive for, then you have permission to see entire schedule of the user.
                    If RealUsername = "" And Email <> "" Then
                        For Each RegionUser In RegionUsers
                            If RegionUser.Email.ToLower = Email AndAlso RegionUser.RealUsername <> "" AndAlso RegionIdsImExecutiveFor.Contains(RegionUser.RegionId) Then
                                RealUsername = RegionUser.RealUsername
                            End If
                        Next
                    End If

                    'Find searched for users inside of logged in users region
                    Dim OverlappedRegionIds As New List(Of String)
                    Dim OverlappedRegionUsers As New List(Of RegionUser)
                    For Each RegionUser In RegionUsers
                        Dim FoundUser As Boolean = False
                        If OfficialId = "allusers" Then
                            FoundUser = RegionUser.RealUsername <> "" AndAlso RegionUser.RealUsername = RealUsername
                        Else
                            FoundUser = ((RegionUser.RealUsername <> "" AndAlso RegionUser.RealUsername = RealUsername) OrElse (RegionUser.Email <> "" AndAlso RegionUser.Email.ToLower = Email) OrElse (RegionUser.RegionId = UserRegionId AndAlso RegionUser.Username = UserOfficialId)) AndAlso RegionUser.FirstName.ToLower.Trim() = FirstName AndAlso RegionUser.LastName.ToLower.Trim() = LastName
                        End If

                        If FoundUser Then
                            If Not OverlappedRegionIds.Contains(RegionUser.RegionId) Then OverlappedRegionIds.Add(RegionUser.RegionId)
                            OverlappedRegionUsers.Add(RegionUser)
                        End If
                    Next


                    'Check if logged in user has permssion to see searched users full schedule or just partial schedule or not at all.
                    Dim CanViewUsersFullSchedule = False
                    Dim CanViewPartialSchedule = False
                    Dim PartialScheduleRegionIds As New List(Of String)

                    For Each OverlappedRegionId In OverlappedRegionIds
                        Dim TRegion = RegionProperties.Find(Function(R) R.RegionId = OverlappedRegionId)
                        If RegionIdsImExecutiveFor.Contains(OverlappedRegionId) Then
                            CanViewUsersFullSchedule = True
                            CanViewPartialSchedule = True
                            PartialScheduleRegionIds.Add(OverlappedRegionId)
                        ElseIf RegionIdsICanViewMasterSchedule.Contains(OverlappedRegionId) Then
                            CanViewPartialSchedule = True
                            PartialScheduleRegionIds.Add(OverlappedRegionId)
                        ElseIf TRegion.ShowFullScheduleToNonMembers Then
                            CanViewPartialSchedule = True
                            PartialScheduleRegionIds.Add(OverlappedRegionId)
                        End If
                    Next

                    Dim OverlappedRefereeRegionIds As New List(Of String)
                    Dim OverlappedTeamRegionIds As New List(Of String)
                    For Each OverlappedRegionId In OverlappedRegionIds
                        Dim TRegion = RegionProperties.Find(Function(R) R.RegionId = OverlappedRegionId)
                        If TRegion.EntityType = "referee" Then
                            OverlappedRefereeRegionIds.Add(OverlappedRegionId)
                        ElseIf TRegion.EntityType = "team" Then
                            OverlappedTeamRegionIds.Add(OverlappedRegionId)
                        End If
                    Next

                    If Not CanViewPartialSchedule Then
                        Return New ErrorObject("NoPermissions")
                    End If


                    Dim DisplayableRefereeRegionIds As New List(Of String)
                    Dim DisplayedTeamRegionIds As New List(Of String)
                    If CanViewUsersFullSchedule Then
                        DisplayableRefereeRegionIds = OverlappedRefereeRegionIds
                        DisplayedTeamRegionIds = OverlappedTeamRegionIds
                    End If


                    'Get Overlapped Referee games that logged in user has access to
                    Dim ScheduleVersionsReferee As List(Of Schedule) = New List(Of Schedule)
                    Dim ScheduleConfirmReferee As List(Of ScheduleConfirm) = New List(Of ScheduleConfirm)

                    If RealUsername <> "" OrElse Email <> "" Then
                        If DisplayableRefereeRegionIds.Count > 0 Then
                            ScheduleVersionsReferee = GetUsersScheduleFromRegionsRangeEmailHelper(DisplayableRefereeRegionIds, RealUsername, Email, LastName, FirstName, StartDate, EndDate, SQLConnection, SQLTransaction)
                            SchedulePosition.GetUsersSchedulePositionFromRegionsRangeEmailHelper(DisplayableRefereeRegionIds, RealUsername, Email, LastName, FirstName, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                            ScheduleFine.GetUsersScheduleFineFromRegionsRangeEmailHelper(DisplayableRefereeRegionIds, RealUsername, Email, LastName, FirstName, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                            ScheduleUserComment.GetUsersScheduleUserCommentFromRegionsRangeEmailHelper(DisplayableRefereeRegionIds, RealUsername, Email, LastName, FirstName, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                            ScheduleCommentTeam.GetUsersScheduleCommentTeamFromRegionsRangeEmailHelper(DisplayableRefereeRegionIds, RealUsername, Email, LastName, FirstName, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)

                            ScheduleConfirmReferee = ScheduleConfirm.GetUsersScheduleConfirmFromRegionsEmailRange(DisplayableRefereeRegionIds, RealUsername, Email, LastName, FirstName, StartDate, EndDate, SQLConnection, SQLTransaction)
                        End If
                    Else
                        If UserRegionId = "" OrElse UserOfficialId = "" Then Return New ErrorObject("InvalidCriteria")
                        If DisplayableRefereeRegionIds.Contains(UserRegionId) Then
                            ScheduleVersionsReferee = GetUsersScheduleFromRegionRangeHelper(UserRegionId, UserOfficialId, StartDate, EndDate, SQLConnection, SQLTransaction)
                            SchedulePosition.GetUsersSchedulePositionFromRegionRangeHelper(UserRegionId, UserOfficialId, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                            ScheduleFine.GetUsersScheduleFineFromRegionRangeHelper(UserRegionId, UserOfficialId, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                            ScheduleUserComment.GetUsersScheduleUserCommentFromRegionRangeHelper(UserRegionId, UserOfficialId, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                            ScheduleCommentTeam.GetUsersScheduleCommentTeamFromRegionRangeHelper(UserRegionId, UserOfficialId, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)


                            ScheduleConfirmReferee = ScheduleConfirm.GetUsersScheduleConfirmFromRegionRange(UserRegionId, UserOfficialId, StartDate, EndDate, SQLConnection, SQLTransaction)
                        End If
                    End If

                    Dim LinkedAndMyTeamRegionIdsAgain = New List(Of String)
                    LinkedAndMyTeamRegionIdsAgain.AddRange(UniqueRegionIds)

                    For Each RegionLeague In RegionLeagues
                        If RegionLeague.RealLeagueId <> "" AndAlso Not LinkedAndMyTeamRegionIdsAgain.Contains(RegionLeague.RealLeagueId) Then
                            LinkedAndMyTeamRegionIdsAgain.Add(RegionLeague.RealLeagueId)
                        End If
                    Next

                    Teams = TeamFetcher.GetTeamsInRegionsHelper(LinkedAndMyTeamRegionIdsAgain, SQLConnection, SQLTransaction)
                    RegionParks = ParkFetcher.GetParksInRegionsHelper(LinkedAndMyTeamRegionIdsAgain, SQLConnection, SQLTransaction)

                    Dim ScheduleResultReferee = ScheduleResult.ConvertIntoScheduleResult(ScheduleVersionsReferee, ScheduleConfirmReferee, New List(Of ScheduleTeamConfirm))

                    Dim DisplayedTeamAndLeagueRegionIds As New List(Of String)
                    LinkedLeagueIds = New List(Of String)

                    DisplayedTeamAndLeagueRegionIds.AddRange(DisplayedTeamRegionIds)
                    For Each RegionLeague In RegionLeagues
                        If DisplayedTeamRegionIds.Contains(RegionLeague.RegionId) Then
                            If RegionLeague.RealLeagueId <> "" AndAlso Not DisplayedTeamAndLeagueRegionIds.Contains(RegionLeague.RealLeagueId) Then
                                DisplayedTeamAndLeagueRegionIds.Add(RegionLeague.RealLeagueId)
                                LinkedLeagueIds.Add(RegionLeague.RealLeagueId)
                            End If
                        End If
                    Next

                    Dim GameLinkedRegionIds As New List(Of String)
                    For Each ScheduleResultRefereeItem In ScheduleResultReferee
                        For Each ScheduleItem In ScheduleResultRefereeItem.Schedule
                            If ScheduleItem.LinkedRegionId <> "" AndAlso Not GameLinkedRegionIds.Contains(ScheduleItem.LinkedRegionId) Then
                                GameLinkedRegionIds.Add(ScheduleItem.LinkedRegionId)
                            End If
                        Next
                    Next

                    RegionProperties = RegionPropertiesFetcher.GetRegionPropertiesRegionIdsHelper(GameLinkedRegionIds, SQLConnection, SQLTransaction)

                    Dim DisplayedTeamAndLeagueRegions As New List(Of RegionProperties)
                    For Each DisplayedTeamAndLeagueRegionId In DisplayedTeamAndLeagueRegionIds
                        DisplayedTeamAndLeagueRegions.Add(RegionProperties.Find(Function(RP) RP.RegionId = DisplayedTeamAndLeagueRegionId))
                    Next

                    'Get Overlapped Team games that logged in user has access to
                    Dim ScheduleVersionsTeam = New List(Of Schedule)
                    Dim ScheduleConfirmTeam As New List(Of ScheduleTeamConfirm)

                    Dim LinkedLeagueIdsExcludingUniqueRegionIds As New List(Of String)
                    For Each LinkedLeagueId In LinkedLeagueIds
                        If Not UniqueRegionIds.Contains(LinkedLeagueId) Then
                            LinkedLeagueIdsExcludingUniqueRegionIds.Add(LinkedLeagueId)
                        End If
                    Next

                    Dim FetchedRegionIds As New List(Of String)
                    FetchedRegionIds.AddRange(UniqueRegionIds)
                    FetchedRegionIds.AddRange(LinkedLeagueIdsExcludingUniqueRegionIds)

                    Dim LinkedLeagueIdsExcludingUniqueRegionIdTeams As New List(Of String)
                    For Each LinkedLeagueIdsExcludingUniqueRegionId In LinkedLeagueIdsExcludingUniqueRegionIds
                        If Not LinkedAndMyTeamRegionIds.Contains(LinkedLeagueIdsExcludingUniqueRegionId) Then
                            LinkedLeagueIdsExcludingUniqueRegionIdTeams.Add(LinkedLeagueIdsExcludingUniqueRegionId)
                        End If
                    Next

                    If LinkedLeagueIdsExcludingUniqueRegionIdTeams.Count > 0 Then
                        Teams.AddRange(UmpireAssignor.Team.GetTeamsInRegionsHelper(LinkedLeagueIdsExcludingUniqueRegionIdTeams, SQLConnection, SQLTransaction))
                        Teams.Sort(Team.BasicSorter)

                        RegionProperties.AddRange(UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(LinkedLeagueIdsExcludingUniqueRegionIds, SQLConnection, SQLTransaction))
                        RegionParks.AddRange(UmpireAssignor.Park.GetParksInRegionsHelper(LinkedLeagueIdsExcludingUniqueRegionIds, SQLConnection, SQLTransaction))

                        RegionProperties.Sort(UmpireAssignor.RegionProperties.BasicSorter)
                        RegionParks.Sort(Park.BasicSorter)
                    End If

                    If DisplayedTeamAndLeagueRegionIds.Count > 0 Then
                        ScheduleVersionsTeam = GetMasterScheduleFromRegionsHelper(DisplayedTeamAndLeagueRegionIds, StartDate, EndDate, SQLConnection, SQLTransaction)
                        ScheduleUserCommentTeam.GetMasterScheduleUserCommentTeamFromRegionsHelper(DisplayedTeamAndLeagueRegionIds, StartDate, EndDate, ScheduleVersionsTeam, SQLConnection, SQLTransaction)
                        ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsHelper(DisplayedTeamAndLeagueRegionIds, StartDate, EndDate, ScheduleVersionsTeam, SQLConnection, SQLTransaction)
                        ScheduleCallup.GetMasterScheduleCallUpFromRegionsHelper(DisplayedTeamAndLeagueRegionIds, StartDate, EndDate, ScheduleVersionsTeam, SQLConnection, SQLTransaction)

                        ScheduleConfirmTeam = UmpireAssignor.ScheduleTeamConfirm.GetMasterScheduleTeamConfirmFromRegions(DisplayedTeamAndLeagueRegionIds, StartDate, EndDate, SQLConnection, SQLTransaction)

                        'StatGames = StatGame.GetStatGamesInRegionsHelper(DisplayedTeamAndLeagueRegions, RegionUsers, Teams, SQLConnection, SQLTransaction)
                        'FullSchedule = GetMasterScheduleFromRegionsNonVersionHelper(DisplayedTeamAndLeagueRegionIds, Date.MinValue, Date.MaxValue, SQLConnection, SQLTransaction)
                    End If

                    Dim ScheduleResultTeam = ScheduleResult.ConvertIntoScheduleResult(ScheduleVersionsTeam, New List(Of ScheduleConfirm), ScheduleConfirmTeam)

                    Dim MergedScheduleResultTeam As New List(Of ScheduleResult)
                    For Each ScheduleResultTeamItem In ScheduleResultTeam
                        Dim TRegionId As String = ScheduleResultTeamItem.Schedule(0).RegionId

                        If DisplayedTeamRegionIds.Contains(TRegionId) Then
                            Dim OverlappedRegionUser = OverlappedRegionUsers.Find(Function(RU)
                                                                                      If OfficialId = "allusers" Then
                                                                                          Return RU.RegionId = TRegionId AndAlso RU.RealUsername <> "" AndAlso RU.RealUsername = RealUsername
                                                                                      End If
                                                                                      Return RU.RegionId = TRegionId AndAlso ((RU.RealUsername <> "" AndAlso RU.RealUsername = RealUsername) OrElse (RU.Email <> "" AndAlso RU.Email.ToLower = Email) OrElse (RU.RegionId = UserRegionId AndAlso RU.Username = UserOfficialId)) AndAlso RU.FirstName.ToLower = FirstName AndAlso RU.LastName.ToLower = LastName
                                                                                  End Function)
                            If OverlappedRegionUser IsNot Nothing Then
                                For Each ScheduleItem In ScheduleResultTeamItem.Schedule
                                    ScheduleItem.OfficialId = OverlappedRegionUser.Username
                                Next

                                MergeScheduleResult(MergedScheduleResultTeam, ScheduleResultTeamItem, OverlappedRegionUser)
                            End If
                        Else
                            Dim RealTeamIds As New List(Of String)
                            For Each ScheduleItem In ScheduleResultTeamItem.Schedule
                                Dim HomeTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso DisplayedTeamRegionIds.Contains(T.RealTeamId))
                                Dim AwayTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso DisplayedTeamRegionIds.Contains(T.RealTeamId))

                                If HomeTeam IsNot Nothing AndAlso Not RealTeamIds.Contains(HomeTeam.RealTeamId) Then RealTeamIds.Add(HomeTeam.RealTeamId)
                                If AwayTeam IsNot Nothing AndAlso Not RealTeamIds.Contains(AwayTeam.RealTeamId) Then RealTeamIds.Add(AwayTeam.RealTeamId)
                            Next

                            For Each RealTeamId In RealTeamIds
                                Dim OverlappedRegionUser = OverlappedRegionUsers.Find(Function(RU)
                                                                                          If OfficialId = "allusers" Then
                                                                                              Return RU.RegionId = RealTeamId AndAlso RU.RealUsername <> "" AndAlso RU.RealUsername = RealUsername
                                                                                          End If
                                                                                          Return RU.RegionId = RealTeamId AndAlso ((RU.RealUsername <> "" AndAlso RU.RealUsername = RealUsername) OrElse (RU.Email <> "" AndAlso RU.Email.ToLower = Email) OrElse (RU.RegionId = UserRegionId AndAlso RU.Username = UserOfficialId)) AndAlso RU.FirstName.ToLower = FirstName AndAlso RU.LastName.ToLower = LastName
                                                                                      End Function)
                                If OverlappedRegionUser IsNot Nothing Then

                                    Dim ClonedScheduleResultTeam = New ScheduleResult
                                    ClonedScheduleResultTeam.ScheduleTeamConfirms.AddRange(ScheduleResultTeamItem.ScheduleTeamConfirms)

                                    Dim I As Integer = 0
                                    Dim FoundIndex As Integer = -1
                                    For Each ScheduleItem In ScheduleResultTeamItem.Schedule
                                        Dim HomeTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso T.RealTeamId = RealTeamId)
                                        Dim AwayTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso T.RealTeamId = RealTeamId)

                                        If HomeTeam IsNot Nothing OrElse AwayTeam IsNot Nothing Then FoundIndex = I

                                        Dim ClonedScheduleItem = ConvertToLinkScheduleItem(ScheduleItem, RealTeamId, RegionLeagues)
                                        ClonedScheduleItem.OfficialId = OverlappedRegionUser.Username
                                        ClonedScheduleResultTeam.Schedule.Add(ClonedScheduleItem)

                                        I += 1
                                    Next

                                    If FoundIndex <> ScheduleResultTeamItem.Schedule.Count - 1 Then
                                        ClonedScheduleResultTeam.Schedule.RemoveRange(FoundIndex + 1, ScheduleResultTeamItem.Schedule.Count - 1 - FoundIndex)
                                        Dim ClonedScheduleItem = ClonedScheduleResultTeam.Schedule.Last().CloneItem()
                                        ClonedScheduleItem.IsDeleted = True
                                        ClonedScheduleResultTeam.Schedule.Add(ClonedScheduleItem)
                                    End If

                                    MergeScheduleResult(MergedScheduleResultTeam, ClonedScheduleResultTeam, OverlappedRegionUser)
                                End If
                            Next
                        End If
                    Next

                    'Get Other Games
                    Dim OtherRegionIds As New List(Of String)

                    Dim RealUsersRegionIds = RegionUser.GetAllMyRegionsIds(RealUsername, SQLConnection, SQLTransaction)
                    For Each RealUsersRegionId In RealUsersRegionIds
                        If Not UniqueRegionIds.Contains(RealUsersRegionId) Then
                            OtherRegionIds.Add(RealUsersRegionId)
                        End If
                    Next

                    If OtherRegionIds.Count > 0 Then
                        Dim OtherViewableRegionIds As New List(Of String)

                        If CanViewUsersFullSchedule Then
                            OtherViewableRegionIds = OtherRegionIds
                        Else
                            OtherViewableRegionIds.AddRange(PartialScheduleRegionIds)
                            Dim TempOtherRegionProperties = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(OtherRegionIds, SQLConnection, SQLTransaction)

                            For Each TRegion In TempOtherRegionProperties
                                If TRegion.ShowFullScheduleToNonMembers Then
                                    OtherViewableRegionIds.Add(TRegion.RegionId)
                                End If
                            Next
                        End If

                        If OtherViewableRegionIds.Count > 0 Then
                            Dim OtherViewableRegionIdsNotFetched As New List(Of String)
                            For Each OtherViewableRegionId In OtherViewableRegionIds
                                If Not FetchedRegionIds.Contains(OtherViewableRegionId) Then OtherViewableRegionIdsNotFetched.Add(OtherViewableRegionId)
                            Next

                            If OtherViewableRegionIdsNotFetched.Count > 0 Then
                                FetchedRegionIds.AddRange(OtherViewableRegionIdsNotFetched)

                                RegionUsers = RegionUserFetcher.LoadAllInRegionIdsHelper(OtherViewableRegionIdsNotFetched, Username, SQLConnection, SQLTransaction)
                                RegionLeagues = RegionLeagueFetcher.GetRegionLeaguesPayContractedRegionIdsHelper(OtherViewableRegionIdsNotFetched, SQLConnection, SQLTransaction)

                                Dim OtherViewableRegionIdsNotFetchedWithLeagues As New List(Of String)
                                OtherViewableRegionIdsNotFetchedWithLeagues.AddRange(OtherViewableRegionIdsNotFetched)
                                For Each RegionLeague In RegionLeagues
                                    If RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked AndAlso Not FetchedRegionIds.Contains(RegionLeague.RealLeagueId) AndAlso Not OtherViewableRegionIdsNotFetchedWithLeagues.Contains(RegionLeague.RealLeagueId) Then
                                        OtherViewableRegionIdsNotFetchedWithLeagues.Add(RegionLeague.RealLeagueId)
                                    End If
                                Next

                                RegionParks = ParkFetcher.GetParksInRegionsHelper(OtherViewableRegionIdsNotFetchedWithLeagues, SQLConnection, SQLTransaction)
                                RegionProperties = RegionPropertiesFetcher.GetRegionPropertiesRegionIdsHelper(OtherViewableRegionIdsNotFetchedWithLeagues, SQLConnection, SQLTransaction)
                                Teams = TeamFetcher.GetTeamsInRegionsHelper(OtherViewableRegionIdsNotFetchedWithLeagues, SQLConnection, SQLTransaction)
                            End If


                            Dim OtherViewableRegionRefereeIds As New List(Of String)
                            Dim OtherViewableRegionTeamIds As New List(Of String)
                            For Each TRegion In RegionProperties
                                If OtherViewableRegionIds.Contains(TRegion.RegionId) Then
                                    If TRegion.EntityType = "referee" Then
                                        OtherViewableRegionRefereeIds.Add(TRegion.RegionId)
                                    ElseIf TRegion.EntityType = "team" Then
                                        OtherViewableRegionTeamIds.Add(TRegion.RegionId)
                                    End If
                                End If
                            Next

                            Dim OtherScheduleVersionsReferee As New List(Of Schedule)
                            Dim OtherScheduleConfirmReferee As New List(Of ScheduleConfirm)

                            If RealUsername <> "" OrElse Email <> "" Then
                                If OtherViewableRegionRefereeIds.Count > 0 Then
                                    OtherScheduleVersionsReferee = GetUsersScheduleFromRegionsNonVersionEmailHelper(OtherViewableRegionRefereeIds, RealUsername, Email, LastName, FirstName, StartDate, EndDate, SQLConnection, SQLTransaction)
                                    SchedulePosition.GetUsersSchedulePositionFromRegionsNonVersionEmailHelper(OtherViewableRegionRefereeIds, RealUsername, Email, LastName, FirstName, StartDate, EndDate, OtherScheduleVersionsReferee, SQLConnection, SQLTransaction)
                                    ScheduleFine.GetUsersScheduleFineFromRegionsNonVersionEmailHelper(OtherViewableRegionRefereeIds, RealUsername, Email, LastName, FirstName, StartDate, EndDate, OtherScheduleVersionsReferee, SQLConnection, SQLTransaction)
                                    ScheduleUserComment.GetUsersScheduleUserCommentFromRegionsNonVersionEmailHelper(OtherViewableRegionRefereeIds, RealUsername, Email, LastName, FirstName, StartDate, EndDate, OtherScheduleVersionsReferee, SQLConnection, SQLTransaction)
                                    ScheduleCommentTeam.GetUsersScheduleCommentTeamFromRegionsNonVersionEmailHelper(OtherViewableRegionRefereeIds, RealUsername, Email, LastName, FirstName, StartDate, EndDate, OtherScheduleVersionsReferee, SQLConnection, SQLTransaction)
                                End If
                            End If

                            For Each OtherScheduleVersionsRefereeItem In OtherScheduleVersionsReferee
                                OtherScheduleConfirmReferee.Add(New ScheduleConfirm With {.RegionId = OtherScheduleVersionsRefereeItem.RegionId, .ScheduleId = OtherScheduleVersionsRefereeItem.ScheduleId, .Username = OtherScheduleVersionsRefereeItem.OfficialId, .DateAdded = Date.UtcNow.AddDays(-1), .VersionId = OtherScheduleVersionsRefereeItem.VersionId})
                            Next

                            OtherScheduleResultReferee = ScheduleResult.ConvertIntoScheduleResult(OtherScheduleVersionsReferee, OtherScheduleConfirmReferee, New List(Of ScheduleTeamConfirm))


                            Dim OtherLinkedLeagueIds As New List(Of String)
                            Dim OtherDisplayedTeamAndLeagueRegionIds As New List(Of String)

                            OtherDisplayedTeamAndLeagueRegionIds.AddRange(OtherViewableRegionTeamIds)
                            For Each RegionLeague In RegionLeagues
                                If OtherViewableRegionIds.Contains(RegionLeague.RegionId) Then
                                    If OtherViewableRegionTeamIds.Contains(RegionLeague.RegionId) Then
                                        If RegionLeague.RealLeagueId <> "" AndAlso Not OtherDisplayedTeamAndLeagueRegionIds.Contains(RegionLeague.RealLeagueId) Then
                                            OtherDisplayedTeamAndLeagueRegionIds.Add(RegionLeague.RealLeagueId)
                                            OtherLinkedLeagueIds.Add(RegionLeague.RealLeagueId)
                                        End If
                                    End If
                                End If
                            Next

                            Dim OtherLinkedLeagueIdsExcludingFetchedRegionIds As New List(Of String)
                            For Each FetchedRegionId In FetchedRegionIds
                                If Not OtherLinkedLeagueIdsExcludingFetchedRegionIds.Contains(FetchedRegionId) Then
                                    OtherLinkedLeagueIdsExcludingFetchedRegionIds.Add(FetchedRegionId)
                                End If
                            Next

                            FetchedRegionIds.AddRange(OtherLinkedLeagueIdsExcludingFetchedRegionIds)

                            Dim OtherScheduleVersionsTeam As New List(Of Schedule)

                            Dim OtherScheduleTeamConfirms As New List(Of ScheduleTeamConfirm)

                            If OtherDisplayedTeamAndLeagueRegionIds.Count > 0 Then
                                OtherScheduleVersionsTeam = GetMasterScheduleFromRegionsHelper(OtherDisplayedTeamAndLeagueRegionIds, StartDate, EndDate, SQLConnection, SQLTransaction)
                                ScheduleUserCommentTeam.GetMasterScheduleUserCommentTeamFromRegionsHelper(OtherDisplayedTeamAndLeagueRegionIds, StartDate, EndDate, OtherScheduleVersionsTeam, SQLConnection, SQLTransaction)
                                ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsHelper(OtherDisplayedTeamAndLeagueRegionIds, StartDate, EndDate, OtherScheduleVersionsTeam, SQLConnection, SQLTransaction)
                                ScheduleCallup.GetMasterScheduleCallUpFromRegionsHelper(OtherDisplayedTeamAndLeagueRegionIds, StartDate, EndDate, OtherScheduleVersionsTeam, SQLConnection, SQLTransaction)

                                OtherScheduleTeamConfirms = ScheduleTeamConfirm.GetMasterScheduleTeamConfirmFromRegions(OtherDisplayedTeamAndLeagueRegionIds, StartDate, EndDate, SQLConnection, SQLTransaction)
                            End If

                            Dim OtherScheduleResultTeam = ScheduleResult.ConvertIntoScheduleResult(OtherScheduleVersionsTeam, New List(Of ScheduleConfirm), OtherScheduleTeamConfirms)

                            OtherMergedScheduleResultTeam = New List(Of ScheduleResult)
                            For Each ScheduleResultTeamItem In OtherScheduleResultTeam
                                Dim TRegionId As String = ScheduleResultTeamItem.Schedule(0).RegionId

                                If OtherViewableRegionTeamIds.Contains(TRegionId) Then
                                    Dim OverlappedRegionUser = RegionUsers.Find(Function(RU)
                                                                                    If OfficialId = "allusers" Then
                                                                                        Return RU.RegionId = TRegionId AndAlso RU.RealUsername <> "" AndAlso RU.RealUsername = RealUsername
                                                                                    End If
                                                                                    Return RU.RegionId = TRegionId AndAlso ((RU.RealUsername <> "" AndAlso RU.RealUsername = RealUsername) OrElse (RU.Email <> "" AndAlso RU.Email.ToLower = Email) OrElse (RU.RegionId = UserRegionId AndAlso RU.Username = UserOfficialId)) AndAlso RU.FirstName.ToLower = FirstName AndAlso RU.LastName.ToLower = LastName
                                                                                End Function)
                                    If OverlappedRegionUser IsNot Nothing Then
                                        For Each ScheduleItem In ScheduleResultTeamItem.Schedule
                                            ScheduleItem.OfficialId = OverlappedRegionUser.Username
                                        Next

                                        MergeScheduleResult(OtherMergedScheduleResultTeam, ScheduleResultTeamItem, OverlappedRegionUser)
                                    End If
                                Else
                                    Dim RealTeamIds As New List(Of String)
                                    For Each ScheduleItem In ScheduleResultTeamItem.Schedule
                                        Dim HomeTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso OtherViewableRegionTeamIds.Contains(T.RealTeamId))
                                        Dim AwayTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso OtherViewableRegionTeamIds.Contains(T.RealTeamId))

                                        If HomeTeam IsNot Nothing AndAlso Not RealTeamIds.Contains(HomeTeam.RealTeamId) Then RealTeamIds.Add(HomeTeam.RealTeamId)
                                        If AwayTeam IsNot Nothing AndAlso Not RealTeamIds.Contains(AwayTeam.RealTeamId) Then RealTeamIds.Add(AwayTeam.RealTeamId)
                                    Next

                                    For Each RealTeamId In RealTeamIds
                                        Dim OverlappedRegionUser = RegionUsers.Find(Function(RU)
                                                                                        If OfficialId = "allusers" Then
                                                                                            Return RU.RegionId = TRegionId AndAlso RU.RealUsername <> "" AndAlso RU.RealUsername = RealUsername
                                                                                        End If
                                                                                        Return RU.RegionId = RealTeamId AndAlso ((RU.RealUsername <> "" AndAlso RU.RealUsername = RealUsername) OrElse (RU.Email <> "" AndAlso RU.Email.ToLower = Email) OrElse (RU.RegionId = UserRegionId AndAlso RU.Username = UserOfficialId)) AndAlso RU.FirstName.ToLower = FirstName AndAlso RU.LastName.ToLower = LastName
                                                                                    End Function)
                                        If OverlappedRegionUser IsNot Nothing Then

                                            Dim ClonedScheduleResultTeam = New ScheduleResult
                                            ClonedScheduleResultTeam.ScheduleTeamConfirms.AddRange(ScheduleResultTeamItem.ScheduleTeamConfirms)

                                            Dim I As Integer = 0
                                            Dim FoundIndex As Integer = -1
                                            For Each ScheduleItem In ScheduleResultTeamItem.Schedule
                                                Dim HomeTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso T.RealTeamId = RealTeamId)
                                                Dim AwayTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso T.RealTeamId = RealTeamId)

                                                If HomeTeam IsNot Nothing OrElse AwayTeam IsNot Nothing Then FoundIndex = I

                                                Dim ClonedScheduleItem = ConvertToLinkScheduleItem(ScheduleItem, RealTeamId, RegionLeagues)
                                                ClonedScheduleItem.OfficialId = OverlappedRegionUser.Username
                                                ClonedScheduleResultTeam.Schedule.Add(ClonedScheduleItem)

                                                I += 1
                                            Next

                                            If FoundIndex <> ScheduleResultTeamItem.Schedule.Count - 1 Then
                                                ClonedScheduleResultTeam.Schedule.RemoveRange(FoundIndex + 1, ScheduleResultTeamItem.Schedule.Count - 1 - FoundIndex)
                                                Dim ClonedScheduleItem = ClonedScheduleResultTeam.Schedule.Last().CloneItem()
                                                ClonedScheduleItem.IsDeleted = True
                                                ClonedScheduleResultTeam.Schedule.Add(ClonedScheduleItem)
                                            End If

                                            MergeScheduleResult(OtherMergedScheduleResultTeam, ClonedScheduleResultTeam, OverlappedRegionUser)
                                        End If
                                    Next
                                End If
                            Next


                        End If
                    End If

                    Schedule = New List(Of ScheduleResult)
                    Schedule.AddRange(ScheduleResultReferee)
                    Schedule.AddRange(MergedScheduleResultTeam)
                    Schedule.AddRange(OtherScheduleResultReferee)
                    Schedule.AddRange(OtherMergedScheduleResultTeam)

                    Schedule.Sort(ScheduleResult.BasicSorter)
                End If
            Else
                RealUsername = Username

                RegionProperties = UmpireAssignor.RegionProperties.GetAllMyRegionPropertiesHelper(Username, SQLConnection, SQLTransaction)

                UniqueRegionIds = New List(Of String)
                For Each TRegionProperties In RegionProperties
                    UniqueRegionIds.Add(TRegionProperties.RegionId)
                Next

                RegionPropertiesFetcher.TRegionProperties = RegionProperties
                RegionPropertiesFetcher.RegionIdHelper.RegionIdsFetched.AddRange(UniqueRegionIds)

                RegionUsers = RegionUserFetcher.LoadAllInRegionIdsHelper(UniqueRegionIds, Username, SQLConnection, SQLTransaction)

                RegionLeagues = RegionLeagueFetcher.GetRegionLeaguesPayContractedRegionIdsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)
                For Each RegionLeague In RegionLeagues
                    If RegionLeague.RealLeagueId <> "" AndAlso Not UniqueRegionIds.Contains(RegionLeague.RealLeagueId) Then
                        UniqueRegionIds.Add(RegionLeague.RealLeagueId)
                    End If
                Next

                RegionParks = ParkFetcher.GetParksInRegionsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)

                Teams = TeamFetcher.GetTeamsInRegionsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)

                For Each Team In Teams
                    If Team.RealTeamId <> "" AndAlso Not UniqueRegionIds.Contains(Team.RealTeamId) Then
                        UniqueRegionIds.Add(Team.RealTeamId)
                    End If
                Next

                RegionLeagues = RegionLeagueFetcher.GetRegionLeaguesPayContractedRegionIdsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)

                RegionProperties = RegionPropertiesFetcher.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)

                Dim MyRegionUsers As New List(Of RegionUser)
                For Each RegionUser In RegionUsers
                    If LastName = "" AndAlso FirstName = "" Then
                        If RegionUser.RealUsername = Username Then
                            MyRegionUsers.Add(RegionUser)
                        End If
                    Else
                        If RegionUser.IsSimilarUser(QueryRegionUser) Then
                            MyRegionUsers.Add(RegionUser)
                        End If
                    End If
                Next

                Dim RefereeRegionIds As New List(Of String)
                Dim TeamRegionIds As New List(Of String)

                For Each TRegion In RegionProperties
                    If TRegion.EntityType = "referee" Then
                        RefereeRegionIds.Add(TRegion.RegionId)
                    ElseIf TRegion.EntityType = "team" Then
                        TeamRegionIds.Add(TRegion.RegionId)
                    End If
                Next

                Dim ScheduleVersionsReferee As New List(Of Schedule)
                Dim ScheduleConfirms As New List(Of ScheduleConfirm)

                If FirstName = "" AndAlso LastName = "" Then
                    ScheduleVersionsReferee = GetUsersScheduleFromRegionsRangeHelper(Username, StartDate, EndDate, SQLConnection, SQLTransaction)
                    SchedulePosition.GetUsersSchedulePositionFromRegionsRangeHelper(Username, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                    ScheduleFine.GetUsersScheduleFineFromRegionsRangeHelper(Username, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                    ScheduleUserComment.GetUsersScheduleUserCommentFromRegionsRangeHelper(Username, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                    ScheduleCommentTeam.GetUsersScheduleCommentTeamFromRegionsRangeHelper(Username, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)

                    ScheduleConfirms = ScheduleConfirm.GetMasterScheduleConfirmFromRegions(RefereeRegionIds, StartDate, EndDate, SQLConnection, SQLTransaction)
                Else
                    ScheduleVersionsReferee = GetUsersScheduleFromRegionsRangeEmailHelper(UniqueRegionIds, RealUsername, Email, FirstName, LastName, StartDate, EndDate, SQLConnection, SQLTransaction)
                    SchedulePosition.GetUsersSchedulePositionFromRegionsRangeEmailHelper(UniqueRegionIds, RealUsername, Email, FirstName, LastName, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                    ScheduleFine.GetUsersScheduleFineFromRegionsRangeEmailHelper(UniqueRegionIds, RealUsername, Email, FirstName, LastName, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                    ScheduleUserComment.GetUsersScheduleUserCommentFromRegionsRangeEmailHelper(UniqueRegionIds, RealUsername, Email, FirstName, LastName, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                    ScheduleCommentTeam.GetUsersScheduleCommentTeamFromRegionsRangeEmailHelper(UniqueRegionIds, RealUsername, Email, FirstName, LastName, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)

                    ScheduleConfirms = ScheduleConfirm.GetMasterScheduleConfirmFromRegions(RefereeRegionIds, StartDate, EndDate, SQLConnection, SQLTransaction)
                End If


                Dim ScheduleResultsReferee = ScheduleResult.ConvertIntoScheduleResult(ScheduleVersionsReferee, ScheduleConfirms, New List(Of ScheduleTeamConfirm))

                LinkedLeagueIds = New List(Of String)
                For Each RegionLeague In RegionLeagues
                    If RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked AndAlso Not LinkedLeagueIds.Contains(RegionLeague.RealLeagueId) Then
                        LinkedLeagueIds.Add(RegionLeague.RealLeagueId)
                    End If
                Next

                Dim LinkedLeagueIdsNotFetched As New List(Of String)
                For Each LinkedLeagueId In LinkedLeagueIds
                    If Not UniqueRegionIds.Contains(LinkedLeagueId) Then
                        LinkedLeagueIdsNotFetched.Add(LinkedLeagueId)
                    End If
                Next

                If LinkedLeagueIdsNotFetched.Count > 0 Then
                    RegionProperties = RegionPropertiesFetcher.GetRegionPropertiesRegionIdsHelper(LinkedLeagueIdsNotFetched, SQLConnection, SQLTransaction)

                    Teams = TeamFetcher.GetTeamsInRegionsHelper(LinkedLeagueIdsNotFetched, SQLConnection, SQLTransaction)
                    RegionParks = ParkFetcher.GetParksInRegionsHelper(LinkedLeagueIdsNotFetched, SQLConnection, SQLTransaction)
                End If

                Dim LinkedTeamAndLeagueIds As New List(Of String)
                LinkedTeamAndLeagueIds.AddRange(TeamRegionIds)
                LinkedTeamAndLeagueIds.AddRange(LinkedLeagueIds)

                Dim MergedScheduleResultsTeam = New List(Of ScheduleResult)

                If LinkedTeamAndLeagueIds.Count > 0 Then
                    Dim ScheduleVersionsTeam = GetMasterScheduleFromRegionsHelper(LinkedTeamAndLeagueIds, StartDate, EndDate, SQLConnection, SQLTransaction)
                    ScheduleUserCommentTeam.GetMasterScheduleUserCommentTeamFromRegionsHelper(LinkedTeamAndLeagueIds, StartDate, EndDate, ScheduleVersionsTeam, SQLConnection, SQLTransaction)
                    ScheduleCallup.GetMasterScheduleCallUpFromRegionsHelper(LinkedTeamAndLeagueIds, StartDate, EndDate, ScheduleVersionsTeam, SQLConnection, SQLTransaction)
                    ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsHelper(LinkedTeamAndLeagueIds, StartDate, EndDate, ScheduleVersionsTeam, SQLConnection, SQLTransaction)

                    Dim ScheduleTeamConfirms = ScheduleTeamConfirm.GetMasterScheduleTeamConfirmFromRegions(LinkedTeamAndLeagueIds, StartDate, EndDate, SQLConnection, SQLTransaction)

                    Dim LinkedTeamAndLeagues As New List(Of RegionProperties)
                    For Each LinkedTeamAndLeagueId In LinkedTeamAndLeagueIds
                        LinkedTeamAndLeagues.Add(RegionProperties.Find(Function(RP) RP.RegionId = LinkedTeamAndLeagueId))
                    Next



                    'StatGames = StatGame.GetStatGamesInRegionsHelper(LinkedTeamAndLeagues, RegionUsers, Teams, SQLConnection, SQLTransaction)
                    'FullSchedule = GetMasterScheduleFromRegionsNonVersionHelper(LinkedTeamAndLeagueIds, Date.MinValue, Date.MaxValue, SQLConnection, SQLTransaction)

                    Dim ScheduleResultsTeam = ScheduleResult.ConvertIntoScheduleResultNoOfficial(ScheduleVersionsTeam, New List(Of ScheduleConfirm), ScheduleTeamConfirms)

                    Dim ScheduleResultTeam = ScheduleResult.ConvertIntoScheduleResultNoOfficial(ScheduleVersionsTeam, New List(Of ScheduleConfirm), ScheduleTeamConfirms)

                    For Each ScheduleResultTeamItem In ScheduleResultsTeam
                        Dim TRegionId As String = ScheduleResultTeamItem.Schedule(0).RegionId

                        If TeamRegionIds.Contains(TRegionId) Then
                            For Each RegionUser In MyRegionUsers
                                If RegionUser.RegionId = TRegionId Then

                                    Dim NewScheduleResultTeamItem As New ScheduleResult
                                    NewScheduleResultTeamItem.ScheduleTeamConfirms = ScheduleResultTeamItem.ScheduleTeamConfirms
                                    For Each ScheduleItem In ScheduleResultTeamItem.Schedule
                                        Dim NewScheduleItem = ScheduleItem.CloneItem()

                                        NewScheduleItem.OfficialId = RegionUser.Username
                                        NewScheduleResultTeamItem.Schedule.Add(NewScheduleItem)
                                    Next

                                    MergeScheduleResult(MergedScheduleResultsTeam, NewScheduleResultTeamItem, RegionUser)
                                End If
                            Next
                        Else
                            Dim RealTeamIds As New List(Of String)
                            For Each ScheduleItem In ScheduleResultTeamItem.Schedule
                                Dim HomeTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso TeamRegionIds.Contains(T.RealTeamId))
                                Dim AwayTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso TeamRegionIds.Contains(T.RealTeamId))

                                If HomeTeam IsNot Nothing AndAlso Not RealTeamIds.Contains(HomeTeam.RealTeamId) Then RealTeamIds.Add(HomeTeam.RealTeamId)
                                If AwayTeam IsNot Nothing AndAlso Not RealTeamIds.Contains(AwayTeam.RealTeamId) Then RealTeamIds.Add(AwayTeam.RealTeamId)
                            Next

                            For Each RealTeamId In RealTeamIds
                                For Each RegionUser In MyRegionUsers
                                    If RegionUser.RegionId = RealTeamId Then

                                        Dim ClonedScheduleResultTeam = New ScheduleResult
                                        ClonedScheduleResultTeam.ScheduleTeamConfirms.AddRange(ScheduleResultTeamItem.ScheduleTeamConfirms)

                                        Dim LastFoundIndex As Integer = -1
                                        Dim I As Integer = 0
                                        For Each ScheduleItem In ScheduleResultTeamItem.Schedule
                                            Dim HomeTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso T.RealTeamId = RealTeamId)
                                            Dim AwayTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso T.RealTeamId = RealTeamId)

                                            If HomeTeam IsNot Nothing OrElse AwayTeam IsNot Nothing Then LastFoundIndex = I

                                            Dim ClonedScheduleItem = ConvertToLinkScheduleItem(ScheduleItem, RealTeamId, RegionLeagues)
                                            ClonedScheduleItem.OfficialId = RegionUser.Username
                                            ClonedScheduleResultTeam.Schedule.Add(ClonedScheduleItem)
                                            I += 1
                                        Next

                                        If LastFoundIndex <> ScheduleResultTeamItem.Schedule.Count - 1 Then
                                            ClonedScheduleResultTeam.Schedule.RemoveRange(LastFoundIndex + 1, ScheduleResultTeamItem.Schedule.Count - 1 - LastFoundIndex)
                                            Dim ClonedItem = ClonedScheduleResultTeam.Schedule.Last().CloneItem()
                                            ClonedItem.IsDeleted = True
                                            ClonedScheduleResultTeam.Schedule.Add(ClonedItem)
                                        End If

                                        MergeScheduleResult(MergedScheduleResultsTeam, ClonedScheduleResultTeam, RegionUser)
                                    End If
                                Next
                            Next
                        End If
                    Next
                End If

                Schedule = New List(Of ScheduleResult)
                Schedule.AddRange(ScheduleResultsReferee)
                Schedule.AddRange(MergedScheduleResultsTeam)

                Schedule.Sort(ScheduleResult.BasicSorter)
            End If
        Else
            UniqueRegionIds = {RegionId}.ToList()

            Dim TUniqueRegionIds = {RegionId}.ToList()

            RegionLeagues = RegionLeagueFetcher.GetRegionLeaguesPayContractedRegionIdsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)
            RegionProperties = RegionPropertiesFetcher.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SQLConnection, SQLTransaction)

            For Each RegionLeague In RegionLeagues
                Dim TTRegion As RegionProperties = PublicCode.BinarySearchItem(RegionProperties, New RegionProperties With {.RegionId = RegionLeague.RegionId}, UmpireAssignor.RegionProperties.BasicSorter)
                If TTRegion IsNot Nothing AndAlso TTRegion.EntityType = "team" Then
                    If RegionLeague.RealLeagueId <> "" AndAlso Not TUniqueRegionIds.Contains(RegionLeague.RealLeagueId) Then
                        TUniqueRegionIds.Add(RegionLeague.RealLeagueId)
                    End If
                End If
            Next

            Teams = TeamFetcher.GetTeamsInRegionsHelper(TUniqueRegionIds, SQLConnection, SQLTransaction)

            For Each Team In Teams
                If Team.RealTeamId <> "" AndAlso Not TUniqueRegionIds.Contains(Team.RealTeamId) Then
                    TUniqueRegionIds.Add(Team.RealTeamId)
                End If
            Next

            RegionUsers = RegionUserFetcher.LoadAllInRegionIdsHelper(TUniqueRegionIds, RealUsername, SQLConnection, SQLTransaction)
            RegionParks = ParkFetcher.GetParksInRegionsHelper(TUniqueRegionIds, SQLConnection, SQLTransaction)

            Dim MyRegionUsers As New List(Of RegionUser)
            If IsSingleSchedule Then
                For Each RegionUser In RegionUsers
                    If RegionUser.Username = OfficialId Then
                        MyRegionUsers.Add(RegionUser)
                    End If
                Next
            Else
                For Each RegionUser In RegionUsers
                    If OfficialId = "allusers" Then
                        If RegionUser.RealUsername = Username Then
                            MyRegionUsers.Add(RegionUser)
                        End If
                    Else
                        If RegionUser.Username = OfficialId Then
                            MyRegionUsers.Add(RegionUser)
                        End If
                    End If
                Next
            End If

            Dim RefereeRegionIds As New List(Of String)
            Dim TeamRegionIds As New List(Of String)

            For Each TRegion In RegionProperties
                If TRegion.EntityType = "referee" Then
                    RefereeRegionIds.Add(TRegion.RegionId)
                ElseIf TRegion.EntityType = "team" Then
                    TeamRegionIds.Add(TRegion.RegionId)
                End If
            Next

            Dim ScheduleResultsReferee As New List(Of ScheduleResult)

            If RefereeRegionIds.Count > 0 Then
                Dim ScheduleVersionsReferee As New List(Of Schedule)
                Dim ScheduleConfirms As New List(Of ScheduleConfirm)

                If MyRegionUsers.Count = 1 Then
                    Dim TUsername As String = MyRegionUsers(0).Username
                    ScheduleVersionsReferee = GetUsersScheduleFromRegionRangeHelper(RegionId, TUsername, StartDate, EndDate, SQLConnection, SQLTransaction)
                    SchedulePosition.GetUsersSchedulePositionFromRegionRangeHelper(RegionId, TUsername, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                    ScheduleFine.GetUsersScheduleFineFromRegionRangeHelper(RegionId, TUsername, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                    ScheduleUserComment.GetUsersScheduleUserCommentFromRegionRangeHelper(RegionId, TUsername, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                    ScheduleCommentTeam.GetUsersScheduleCommentTeamFromRegionRangeHelper(RegionId, TUsername, StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)

                    ScheduleConfirms = ScheduleConfirm.GetUsersScheduleConfirmFromRegionRange(RegionId, TUsername, StartDate, EndDate, SQLConnection, SQLTransaction)

                ElseIf MyRegionUsers.Count > 1 Then
                    ScheduleVersionsReferee = GetUsersScheduleFromRegionsRangeEmailHelper(UniqueRegionIds, RealUsername, "", "", "", StartDate, EndDate, SQLConnection, SQLTransaction)
                    SchedulePosition.GetUsersSchedulePositionFromRegionsRangeEmailHelper(UniqueRegionIds, RealUsername, "", "", "", StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                    ScheduleFine.GetUsersScheduleFineFromRegionsRangeEmailHelper(UniqueRegionIds, RealUsername, "", "", "", StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                    ScheduleUserComment.GetUsersScheduleUserCommentFromRegionsRangeEmailHelper(UniqueRegionIds, RealUsername, "", "", "", StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)
                    ScheduleCommentTeam.GetUsersScheduleCommentTeamFromRegionsRangeEmailHelper(UniqueRegionIds, RealUsername, "", "", "", StartDate, EndDate, ScheduleVersionsReferee, SQLConnection, SQLTransaction)

                    ScheduleConfirms = ScheduleConfirm.GetUsersScheduleConfirmFromRegionsEmailRange(UniqueRegionIds, RealUsername, "", "", "", StartDate, EndDate, SQLConnection, SQLTransaction)
                End If
                ScheduleResultsReferee = ScheduleResult.ConvertIntoScheduleResult(ScheduleVersionsReferee, ScheduleConfirms, New List(Of ScheduleTeamConfirm))
            End If

            Dim FullRealLeagueIds As New List(Of String)
            For Each RegionLeague In RegionLeagues
                If RegionLeague.RealLeagueId <> "" AndAlso Not FullRealLeagueIds.Contains(RegionLeague.RealLeagueId) Then
                    FullRealLeagueIds.Add(RegionLeague.RealLeagueId)
                End If
            Next

            Teams = TeamFetcher.GetTeamsInRegionsHelper(FullRealLeagueIds, SQLConnection, SQLTransaction)
            RegionParks = ParkFetcher.GetParksInRegionsHelper(FullRealLeagueIds, SQLConnection, SQLTransaction)

            Dim MergedScheduleResultTeam = New List(Of ScheduleResult)
            If TeamRegionIds.Count > 0 Then
                LinkedLeagueIds = New List(Of String)
                For Each RegionLeague In RegionLeagues
                    If RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked AndAlso Not LinkedLeagueIds.Contains(RegionLeague.RealLeagueId) Then
                        LinkedLeagueIds.Add(RegionLeague.RealLeagueId)
                    End If
                Next

                Dim LinkedLeagueIdsNotFetched As New List(Of String)
                For Each LinkedLeagueId In LinkedLeagueIds
                    If Not UniqueRegionIds.Contains(LinkedLeagueId) Then
                        LinkedLeagueIdsNotFetched.Add(LinkedLeagueId)
                    End If
                Next

                If LinkedLeagueIdsNotFetched.Count > 0 Then
                    RegionProperties = RegionPropertiesFetcher.GetRegionPropertiesRegionIdsHelper(LinkedLeagueIdsNotFetched, SQLConnection, SQLTransaction)
                    Teams = TeamFetcher.GetTeamsInRegionsHelper(LinkedLeagueIdsNotFetched, SQLConnection, SQLTransaction)
                    RegionParks = ParkFetcher.GetParksInRegionsHelper(LinkedLeagueIdsNotFetched, SQLConnection, SQLTransaction)
                End If

                Dim LinkedTeamAndLeagueIds As New List(Of String)
                LinkedTeamAndLeagueIds.AddRange(TeamRegionIds)
                LinkedTeamAndLeagueIds.AddRange(LinkedLeagueIds)

                If LinkedTeamAndLeagueIds.Count > 0 Then
                    Dim ScheduleVersionsTeam = GetMasterScheduleFromRegionsHelper(LinkedTeamAndLeagueIds, StartDate, EndDate, SQLConnection, SQLTransaction)
                    ScheduleUserCommentTeam.GetMasterScheduleUserCommentTeamFromRegionsHelper(LinkedTeamAndLeagueIds, StartDate, EndDate, ScheduleVersionsTeam, SQLConnection, SQLTransaction)
                    ScheduleCallup.GetMasterScheduleCallUpFromRegionsHelper(LinkedTeamAndLeagueIds, StartDate, EndDate, ScheduleVersionsTeam, SQLConnection, SQLTransaction)
                    ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsHelper(LinkedTeamAndLeagueIds, StartDate, EndDate, ScheduleVersionsTeam, SQLConnection, SQLTransaction)

                    Dim ScheduleTeamConfirms = ScheduleTeamConfirm.GetMasterScheduleTeamConfirmFromRegions(LinkedTeamAndLeagueIds, StartDate, EndDate, SQLConnection, SQLTransaction)

                    Dim LinkedTeamAndLeagues As New List(Of RegionProperties)
                    For Each LinkedTeamAndLeagueId In LinkedTeamAndLeagueIds
                        LinkedTeamAndLeagues.Add(RegionProperties.Find(Function(RP) RP.RegionId = LinkedTeamAndLeagueId))
                    Next

                    'StatGames = StatGame.GetStatGamesInRegionsHelper(LinkedTeamAndLeagues, RegionUsers, Teams, SQLConnection, SQLTransaction)
                    'FullSchedule = GetMasterScheduleFromRegionsNonVersionHelper(LinkedTeamAndLeagueIds, Date.MinValue, Date.MaxValue, SQLConnection, SQLTransaction)

                    Dim ScheduleResultsTeam = ScheduleResult.ConvertIntoScheduleResultNoOfficial(ScheduleVersionsTeam, New List(Of ScheduleConfirm), ScheduleTeamConfirms)

                    Dim ScheduleResultTeam = ScheduleResult.ConvertIntoScheduleResult(ScheduleVersionsTeam, New List(Of ScheduleConfirm), ScheduleTeamConfirms)

                    For Each ScheduleResultTeamItem In ScheduleResultsTeam
                        Dim TRegionId As String = ScheduleResultTeamItem.Schedule(0).RegionId

                        If TeamRegionIds.Contains(TRegionId) Then
                            For Each RegionUser In MyRegionUsers
                                If RegionUser.RegionId = TRegionId Then

                                    Dim NewScheduleResultTeamItem As New ScheduleResult
                                    NewScheduleResultTeamItem.ScheduleTeamConfirms = ScheduleResultTeamItem.ScheduleTeamConfirms
                                    For Each ScheduleItem In ScheduleResultTeamItem.Schedule
                                        Dim NewScheduleItem = ScheduleItem.CloneItem()

                                        NewScheduleItem.OfficialId = RegionUser.Username
                                        NewScheduleResultTeamItem.Schedule.Add(NewScheduleItem)
                                    Next

                                    MergeScheduleResult(MergedScheduleResultTeam, NewScheduleResultTeamItem, RegionUser)
                                End If
                            Next
                        Else
                            Dim RealTeamIds As New List(Of String)
                            For Each ScheduleItem In ScheduleResultTeamItem.Schedule
                                Dim HomeTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso TeamRegionIds.Contains(T.RealTeamId))
                                Dim AwayTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso TeamRegionIds.Contains(T.RealTeamId))

                                If HomeTeam IsNot Nothing AndAlso Not RealTeamIds.Contains(HomeTeam.RealTeamId) Then RealTeamIds.Add(HomeTeam.RealTeamId)
                                If AwayTeam IsNot Nothing AndAlso Not RealTeamIds.Contains(AwayTeam.RealTeamId) Then RealTeamIds.Add(AwayTeam.RealTeamId)
                            Next

                            For Each RealTeamId In RealTeamIds
                                For Each RegionUser In MyRegionUsers
                                    If RegionUser.RegionId = RealTeamId Then

                                        Dim ClonedScheduleResultTeam = New ScheduleResult
                                        ClonedScheduleResultTeam.ScheduleTeamConfirms.AddRange(ClonedScheduleResultTeam.ScheduleTeamConfirms)

                                        Dim FoundIndex As Integer = -1
                                        Dim I As Integer = 0
                                        For Each ScheduleItem In ScheduleResultTeamItem.Schedule
                                            Dim HomeTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso T.RealTeamId = RealTeamId)
                                            Dim AwayTeam = Teams.Find(Function(T) T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.Trim().ToLower AndAlso T.RealTeamId <> "" AndAlso T.RealTeamId = RealTeamId)

                                            If HomeTeam IsNot Nothing OrElse AwayTeam IsNot Nothing Then FoundIndex = I

                                            Dim ClonedScheduleItem = ConvertToLinkScheduleItem(ScheduleItem, RealTeamId, RegionLeagues)
                                            ClonedScheduleItem.OfficialId = RegionUser.Username
                                            ClonedScheduleResultTeam.Schedule.Add(ClonedScheduleItem)
                                            I += 1
                                        Next

                                        If FoundIndex <> ScheduleResultTeamItem.Schedule.Count - 1 Then
                                            ClonedScheduleResultTeam.Schedule.RemoveRange(FoundIndex + 1, ScheduleResultTeamItem.Schedule.Count - 1 - FoundIndex)
                                            Dim ClonedScheduleItem = ClonedScheduleResultTeam.Schedule.Last().CloneItem()
                                            ClonedScheduleItem.IsDeleted = True
                                            ClonedScheduleResultTeam.Schedule.Add(ClonedScheduleItem)
                                        End If

                                        MergeScheduleResult(MergedScheduleResultTeam, ClonedScheduleResultTeam, RegionUser)
                                    End If
                                Next
                            Next
                        End If
                    Next
                End If
            End If

            Schedule = New List(Of ScheduleResult)
            Schedule.AddRange(ScheduleResultsReferee)
            Schedule.AddRange(MergedScheduleResultTeam)

            Schedule.Sort(ScheduleResult.BasicSorter)

        End If

        Dim RealRegionLeagueIds As New List(Of String)
        For Each RegionLeague In RegionLeagues
            If RegionLeague.RealLeagueId <> "" AndAlso Not RealRegionLeagueIds.Contains(RegionLeague.RealLeagueId) Then
                RealRegionLeagueIds.Add(RegionLeague.RealLeagueId)
            End If
        Next

        Dim OfficialRegionFetcher2 As New OfficialRegionFetcher
        OfficialRegionFetcher2.OfficialRegions = OfficialRegions
        For Each OfficialRegion In OfficialRegions
            If Not OfficialRegionFetcher2.RegionIdHelper.RegionIdsFetched.Contains(OfficialRegion.RegionId) Then
                OfficialRegionFetcher2.RegionIdHelper.RegionIdsFetched.Add(OfficialRegion.RegionId)
            End If
        Next

        OfficialRegions = OfficialRegionFetcher2.GetOfficialRegionsInRegionsHelper(RealRegionLeagueIds, SQLConnection, SQLTransaction)

        If Username <> LoggedInUsername Then
            Dim MyRegions = Region.GetMyRegionsHelper(LoggedInUsername, SQLConnection, SQLTransaction)
            Dim MyRegionUsers = RegionUser.LoadAllInMyRegionsHelper(LoggedInUsername, SQLConnection, SQLTransaction)
            Dim MySimilarLeagueIds As New List(Of String)

            For I As Integer = Schedule.Count - 1 To 0 Step -1
                Dim ScheduleItem = Schedule(I).Schedule(0)

                Dim RP = RegionProperties.Find(Function(RPP) RPP.RegionId = ScheduleItem.RegionId)
                Dim CanSeeFullSchedule As Boolean = False
                If RP.ShowFullScheduleToNonMembers Then Continue For
                For Each MyRegionUser In MyRegionUsers
                    If MyRegionUser.RegionId = ScheduleItem.RegionId Then
                        If RP.EntityType = "team" Then
                            CanSeeFullSchedule = True
                        End If
                        If MyRegionUser.IsExecutive Then
                            CanSeeFullSchedule = True
                        End If
                        If MyRegionUser.CanViewMasterSchedule Then
                            CanSeeFullSchedule = True
                        End If
                    End If
                Next
                If Not CanSeeFullSchedule Then
                    If RP.EntityType = "league" Then
                        For Each Team In Teams
                            If Team.IsLinked AndAlso Team.RegionId = ScheduleItem.LinkedRegionId Then
                                If MyRegions.Find(Function(RI) RI.RegionId = Team.RealTeamId) IsNot Nothing Then
                                    CanSeeFullSchedule = True
                                End If
                            End If
                        Next
                    End If
                End If
                If Not CanSeeFullSchedule Then
                    Schedule.RemoveAt(I)
                End If
            Next
        End If

        Return Nothing
    End Function

    Public Shared Function GetUsersScheduleFromRegionDateRange(Username As String, RegionId As String, OfficialId As String, StartDate As Date, EndDate As Date, LoggedInUsername As String, Optional IsSingleSchedule As Boolean = False, Optional RealUsername As String = "", Optional Email As String = "", Optional UserRegionId As String = "", Optional UserOfficialId As String = "", Optional LastName As String = "", Optional FirstName As String = "", Optional OldSQLConnection As SqlConnection = Nothing, Optional OldSQLTransaction As SqlTransaction = Nothing) As Object
        Dim Schedule As New List(Of ScheduleResult)
        Dim StatGames = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Dictionary(Of Integer, Dictionary(Of Integer, List(Of StatGame))))))
        Dim FullSchedule = New List(Of Schedule)
        Dim Region As Region = Nothing
        Dim RegionUsers = New List(Of RegionUser)
        Dim RegionParks = New List(Of Park)
        Dim RegionLeagues As New List(Of RegionLeaguePayContracted)
        Dim OfficialRegions As New List(Of OfficialRegion)
        Dim Teams As New List(Of Team)
        Dim RegionProperties As New List(Of RegionProperties)
        Dim ScheduleRequests As New List(Of ScheduleRequest)

        Dim UniqueRegionIds As New List(Of String)

        Dim ExecutiveForRegions As List(Of String) = Nothing
        Dim InRegions As List(Of String) = Nothing

        Dim LinkedLeagueIds As New List(Of String)

        Dim DisplayingScheduleRegionIds As New List(Of String)

        Username = Username.ToLower.Trim()
        RegionId = RegionId.ToLower.Trim()

        FirstName = (FirstName & "").ToLower.Trim()
        LastName = (LastName & "").ToLower.Trim()
        Email = (Email & "").ToLower.Trim()
        RealUsername = (RealUsername & "").ToLower.Trim()
        UserRegionId = (UserRegionId & "").ToLower.Trim()

        'Try

        If OldSQLConnection Is Nothing Then
            Using TSqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                TSqlConnection.Open()
                Using TSqlTransaction = TSqlConnection.BeginTransaction()
                    Dim TResult = GetUsersScheduleFromRegionDateRangeInnerHelper(Username, RegionId, OfficialId, StartDate, EndDate, IsSingleSchedule, RealUsername, Email, UserRegionId, UserOfficialId, LastName, FirstName, LoggedInUsername, UniqueRegionIds, RegionProperties, RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, LinkedLeagueIds, Schedule, StatGames, FullSchedule, TSqlConnection, TSqlTransaction)
                    If TResult IsNot Nothing Then Return TResult
                End Using
            End Using
        Else
            Dim TResult = GetUsersScheduleFromRegionDateRangeInnerHelper(Username, RegionId, OfficialId, StartDate, EndDate, IsSingleSchedule, RealUsername, Email, UserRegionId, UserOfficialId, LastName, FirstName, LoggedInUsername, UniqueRegionIds, RegionProperties, RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, LinkedLeagueIds, Schedule, StatGames, FullSchedule, OldSQLConnection, OldSQLTransaction)
            If TResult IsNot Nothing Then Return TResult
        End If


        MergeTeamData(Schedule)

        RegionUser.CleanRegionUsers(RegionProperties, RegionUsers, LoggedInUsername)

        RegionProperties = RegionProperties.FindAll(Function(RP)
                                                        If RP.RegionIsLadderLeague Then
                                                            Return True
                                                        End If
                                                        For Each SG In Schedule
                                                            If SG.Schedule.Count > 0 Then
                                                                If SG.Schedule(0).RegionId = RP.RegionId Then Return True
                                                                If SG.Schedule(0).LinkedRegionId = RP.RegionId Then Return True
                                                            End If
                                                        Next
                                                        Return False
                                                    End Function)

        RegionParks = RegionParks.FindAll(Function(P)
                                              Dim TRegion = RegionProperties.Find(Function (R) R.RegionId = P.RegionId)
                                              If TRegion IsNot Nothing AndAlso TRegion.RegionIsLadderLeague Then
                                                  Return True
                                              End If

                                              For Each SG In Schedule
                                                  If SG.Schedule.Count = 0 Then Continue For

                                                  If SG.Schedule(0).LinkedRegionId = "" Then
                                                      If SG.Schedule(0).RegionId = P.RegionId Then
                                                          For Each SG2 In SG.Schedule
                                                              If SG2.ParkId.Trim.ToLower = P.ParkId Then Return True
                                                          Next
                                                      End If
                                                  Else
                                                      If SG.Schedule(0).LinkedRegionId = P.RegionId Then
                                                          For Each SG2 In SG.Schedule
                                                              If SG2.ParkId.Trim.ToLower = P.ParkId Then Return True
                                                          Next
                                                      End If
                                                  End If
                                              Next
                                              Return False
                                          End Function)

        RegionLeagues = RegionLeagues.FindAll(Function(RL)
                                                  For Each SG In Schedule
                                                      If SG.Schedule.Count = 0 Then Continue For

                                                      If SG.Schedule(0).RegionId = RL.RegionId Then
                                                          For Each SG2 In SG.Schedule
                                                              If SG2.LeagueId.Trim.ToLower = RL.LeagueId Then Return True
                                                          Next
                                                      End If
                                                  Next
                                                  Return False
                                              End Function)

        Teams = Teams.FindAll(Function(T)
                                  For Each SG In Schedule
                                      If SG.Schedule.Count = 0 Then Continue For

                                      If SG.Schedule(0).LinkedRegionId = "" Then
                                          If SG.Schedule(0).RegionId = T.RegionId Then
                                              For Each SG2 In SG.Schedule
                                                  If SG2.HomeTeam.Trim.ToLower = T.TeamId Then Return True
                                                  If SG2.AwayTeam.Trim.ToLower = T.TeamId Then Return True
                                              Next
                                          End If
                                      Else
                                          If SG.Schedule(0).LinkedRegionId = T.RegionId Then
                                              For Each SG2 In SG.Schedule
                                                  If SG2.HomeTeam.Trim.ToLower = T.TeamId Then Return True
                                                  If SG2.AwayTeam.Trim.ToLower = T.TeamId Then Return True
                                              Next
                                          End If
                                      End If
                                  Next
                                  Return False
                              End Function)

        RegionUsers = RegionUsers.FindAll(Function(RU)
                                                Dim TRegion = RegionProperties.Find(Function(RP) RP.RegionId = RU.RegionId)
                                                If TRegion IsNot Nothing AndAlso TRegion.RegionIsLadderLeague Then
                                                    Return True
                                                End If
                                                If TRegion IsNot Nothing AndAlso TRegion.RegionIsLadderLeague Then
                                                      Return True
                                              End If
                                              For Each SG In Schedule
                                                  If SG.Schedule.Count = 0 Then Continue For
                                                  If RU.RegionId <> SG.Schedule(0).RegionId Then Continue For
                                                  
                                                  If TRegion IsNot Nothing AndAlso TRegion.EntityType = "team" Then
                                                      If RU.RegionId = TRegion.RegionId Then Return True
                                                  ElseIf TRegion IsNot Nothing AndAlso TRegion.EntityType = "referee" Then
                                                      If RU.RegionId = TRegion.RegionId Then
                                                          For Each SG2 In SG.Schedule
                                                              For Each SP In SG2.SchedulePositions
                                                                  If SP.OfficialId.Trim.ToLower = RU.Username Then Return True
                                                              Next
                                                              For Each SF In SG2.ScheduleFines
                                                                  If SF.OfficialId.Trim.ToLower = RU.Username Then Return True
                                                              Next
                                                              For Each SU In SG2.ScheduleUserComments
                                                                  If SU.OfficialId.Trim.ToLower = RU.Username Then Return True
                                                              Next
                                                          Next
                                                      End If
                                                  End If
                                              Next
                                              Return False
                                          End Function)

        
        For Each ScheduleResultItem In Schedule
            For I As Integer = 0 To ScheduleResultItem.Schedule.Count - 1
                If Not DisplayingScheduleRegionIds.Contains(ScheduleResultItem.Schedule(I).RegionId) Then
                    DisplayingScheduleRegionIds.Add(ScheduleResultItem.Schedule(I).RegionId)
                End If
                If ScheduleResultItem.Schedule(I) IsNot Nothing Then
                    ScheduleResultItem.Schedule(I) = UmpireAssignor.Schedule.TrimHiddenScheduleData(ScheduleResultItem.Schedule(I), RegionProperties, RegionUsers, Username)
                End If
            Next
        Next
       
        For I As Integer = 0 To FullSchedule.Count - 1
            FullSchedule(I) = UmpireAssignor.Schedule.TrimHiddenScheduleData(FullSchedule(I), RegionProperties, RegionUsers, Username)
        Next
         
        Return New With {
            .Success = True,
            .Schedule = Schedule,
            .StatGames = StatGames,
            .FullSchedule = FullSchedule,
            .ScheduleRequests = ScheduleRequests,
            .RegionUsers = RegionUsers,
            .RegionParks = RegionParks,
            .RegionLeagues = RegionLeagues,
            .Teams = Teams,
            .OfficialRegions = New List(Of OfficialRegion),
            .RegionProperties = RegionProperties,
            .DisplayingScheduleRegionIds = DisplayingScheduleRegionIds
        }
        'Catch E As Exception
        'Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Shared Function GetMasterScheduleFromRegionNonVersionWithJoins(Username As String, RegionId As String, MinDate As DateTime, MaxDate As DateTime) As Object
        Dim Schedule As New List(Of Schedule)
        Dim Regions As New List(Of Region)
        Dim RegionUsers = New List(Of RegionUser)
        Dim RegionParks = New List(Of Park)
        Dim RegionLeagues As New List(Of RegionLeaguePayContracted)
        Dim RegionProperties As New List(Of RegionProperties)
        Dim Teams As New List(Of Team)

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    Dim UniqueRegionIds As New List(Of String)
                    If RegionId = "allregions" Then
                        UniqueRegionIds = RegionUser.GetAllMyRegionsIdExecs(Username, SqlConnection, SqlTransaction)
                        Regions = UmpireAssignor.Region.GetRegionFromRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)
                    Else
                        Regions = {UmpireAssignor.Region.GetRegionHelper(RegionId, SqlConnection, SqlTransaction, True)}.ToList()
                        UniqueRegionIds.Add(RegionId)
                        Dim Assignor As RegionUser = Nothing
                        If Regions.Count = 1 AndAlso Not Regions(0).ShowFullScheduleToNonMembers Then
                            Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, Username, SqlConnection, SqlTransaction)
                            If Assignor Is Nothing Then
                                Return New ErrorObject("InvalidPermissions")
                            End If
                        End If
                    End If

                    Schedule = GetMasterScheduleFromRegionsNonVersionHelper(UniqueRegionIds, MinDate, MaxDate, SqlConnection, SqlTransaction)
                    SchedulePosition.GetMasterSchedulePositionFromRegionsNonVersionHelper(UniqueRegionIds, MinDate, MaxDate, Schedule, SqlConnection, SqlTransaction)
                    ScheduleFine.GetMasterScheduleFineFromRegionsNonVersionHelper(UniqueRegionIds, MinDate, MaxDate, Schedule, SqlConnection, SqlTransaction)
                    ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsNonVersionHelper(UniqueRegionIds, MinDate, MaxDate, Schedule, SqlConnection, SqlTransaction)
                    ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsNonVersionHelper(UniqueRegionIds, MinDate, MaxDate, Schedule, SqlConnection, SqlTransaction)


                    RegionUsers = RegionUser.LoadAllInRegionIdsHelper(UniqueRegionIds, Username, SqlConnection, SqlTransaction)
                    RegionLeagues = RegionLeague.GetRegionLeaguesPayContractedRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)

                    Dim NewUniqueRegionIds As New List(Of String)
                    NewUniqueRegionIds.AddRange(UniqueRegionIds)
                    For Each RegionLeague In RegionLeagues
                        If RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked AndAlso Not NewUniqueRegionIds.Contains(RegionLeague.RealLeagueId) Then
                            NewUniqueRegionIds.Add(RegionLeague.RealLeagueId)
                        End If
                    Next

                    RegionParks = Park.GetParksInRegionsHelper(NewUniqueRegionIds, SqlConnection, SqlTransaction)
                    RegionProperties = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(NewUniqueRegionIds, SqlConnection, SqlTransaction)
                    Teams = UmpireAssignor.Team.GetTeamsInRegionsHelper(NewUniqueRegionIds, SqlConnection, SqlTransaction)

                    SqlTransaction.Commit()
                End Using
            End Using

            RegionUser.CleanRegionUsers(RegionProperties, RegionUsers, Username)

            Return New With {
                .Success = True,
                .Schedule = Schedule,
                .Regions = Regions,
                .RegionUsers = RegionUsers,
                .RegionParks = RegionParks,
                .RegionLeagues = RegionLeagues,
                .Teams = Teams,
                .RegionProperties = RegionProperties
            }
        Catch E As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function GetUsersScheduleFromRegionNonVersion(Username As String, RegionId As String, OfficialId As String, StartDate As Date, EndDate As Date, Optional IsSingleSchedule As Boolean = False, Optional RealUsername As String = "", Optional Email As String = "", Optional UserRegionId As String = "", Optional UserOfficialId As String = "", Optional LastName As String = "", Optional FirstName As String = "") As Object
        Dim Schedule As New List(Of Schedule)
        Dim RegionLeagues As New List(Of RegionLeaguePayContracted)
        Dim RegionUsers As New List(Of RegionUser)
        Dim Region As Region = Nothing
        Dim RegionParks = New List(Of Park)
        Dim RegionProperties As New List(Of RegionProperties)
        Dim UniqueRegionIds As New List(Of String)
        Dim Regions As New List(Of Region)
        Dim ExecutiveForRegions As List(Of String) = Nothing
        Dim InRegions As List(Of String) = Nothing

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    If RegionId = "allregions" Then
                        If IsSingleSchedule Then


                            UniqueRegionIds = RegionUser.GetAllMyRegionsIds(Username, SqlConnection, SqlTransaction)

                            If UniqueRegionIds.Count > 0 Then
                                RegionUsers = RegionUser.LoadAllInRegionIdsHelper(UniqueRegionIds, Username, SqlConnection, SqlTransaction)
                                RegionProperties = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)

                                For I As Integer = UniqueRegionIds.Count - 1 To 0 Step -1
                                    Dim URId = UniqueRegionIds(I)
                                    Dim IncludeRegion = RegionProperties.Find(Function(R) R.RegionId = URId).ShowFullScheduleToNonMembers
                                    If Not IncludeRegion Then
                                        For Each RegionUser In RegionUsers
                                            If RegionUser.RegionId = URId AndAlso RegionUser.RealUsername = Username Then
                                                If RegionUser.IsExecutive OrElse RegionUser.CanViewMasterSchedule Then
                                                    IncludeRegion = True
                                                    Exit For
                                                End If
                                            End If
                                        Next
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

                                If UniqueRegionIds.Count > 0 Then
                                    If Email <> "" OrElse RealUsername <> "" Then
                                        Schedule = GetUsersScheduleFromRegionsNonVersionEmailHelper(UniqueRegionIds, RealUsername, Email, LastName, FirstName, Date.MinValue, Date.MaxValue, SqlConnection, SqlTransaction)
                                        SchedulePosition.GetUsersSchedulePositionFromRegionsNonVersionEmailHelper(UniqueRegionIds, RealUsername, Email, LastName, FirstName, Date.MinValue, Date.MaxValue, Schedule, SqlConnection, SqlTransaction)
                                        ScheduleFine.GetUsersScheduleFineFromRegionsNonVersionEmailHelper(UniqueRegionIds, RealUsername, Email, LastName, FirstName, Date.MinValue, Date.MaxValue, Schedule, SqlConnection, SqlTransaction)
                                        ScheduleUserComment.GetUsersScheduleUserCommentFromRegionsNonVersionEmailHelper(UniqueRegionIds, RealUsername, Email, LastName, FirstName, Date.MinValue, Date.MaxValue, Schedule, SqlConnection, SqlTransaction)
                                        ScheduleCommentTeam.GetUsersScheduleCommentTeamFromRegionsNonVersionEmailHelper(UniqueRegionIds, RealUsername, Email, LastName, FirstName, Date.MinValue, Date.MaxValue, Schedule, SqlConnection, SqlTransaction)


                                        RegionUsers = RegionUser.LoadAllInRegionIdsHelper(UniqueRegionIds, Username, SqlConnection, SqlTransaction)
                                        RegionLeagues = RegionLeague.GetRegionLeaguesPayContractedRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)
                                    Else
                                        Schedule = GetUsersScheduleFromRegionNonVersionHelper(UserRegionId, UserOfficialId, StartDate, EndDate, SqlConnection, SqlTransaction)
                                        SchedulePosition.GetUsersSchedulePositionFromRegionNonVersionHelper(UserRegionId, UserOfficialId, StartDate, EndDate, Schedule, SqlConnection, SqlTransaction)
                                        ScheduleFine.GetUsersScheduleFineFromRegionNonVersionHelper(UserRegionId, UserOfficialId, StartDate, EndDate, Schedule, SqlConnection, SqlTransaction)
                                        ScheduleUserComment.GetUsersScheduleUserCommentFromRegionNonVersionHelper(UserRegionId, UserOfficialId, StartDate, EndDate, Schedule, SqlConnection, SqlTransaction)
                                        ScheduleCommentTeam.GetUsersScheduleCommentTeamFromRegionNonVersionHelper(UserRegionId, UserOfficialId, StartDate, EndDate, Schedule, SqlConnection, SqlTransaction)

                                        RegionUsers = RegionUser.LoadAllInRegionHelper(UserRegionId, Username, SqlConnection, SqlTransaction)
                                        RegionLeagues = RegionLeague.GetRegionLeaguesPayContractedHelper(UserRegionId, SqlConnection, SqlTransaction)
                                    End If

                                    Dim IsExecutiveOfUser As Boolean = False

                                    If RealUsername <> "" Then
                                        ExecutiveForRegions = New List(Of String)
                                        InRegions = New List(Of String)

                                        For Each RegionUser In RegionUsers
                                            If RegionUser.RealUsername = "" Then Continue For
                                            If RegionUser.RealUsername = Username AndAlso Not InRegions.Contains(RegionUser.RegionId) Then
                                                InRegions.Add(RegionUser.RegionId)
                                            End If
                                            If RegionUser.RealUsername = Username AndAlso RegionUser.IsExecutive AndAlso Not ExecutiveForRegions.Contains(RegionUser.RegionId) Then
                                                ExecutiveForRegions.Add(RegionUser.RegionId)
                                            End If
                                        Next


                                        For Each RegionUser In RegionUsers
                                            If ExecutiveForRegions.Contains(RegionUser.RegionId) AndAlso RegionUser.RealUsername = RealUsername Then
                                                IsExecutiveOfUser = True
                                            End If
                                        Next
                                    End If

                                    If RealUsername <> "" AndAlso IsExecutiveOfUser Then
                                        Dim HasRealUsername As Boolean = False
                                        For Each RegionUser In RegionUsers
                                            If RegionUser.RealUsername = RealUsername Then
                                                HasRealUsername = True
                                                Exit For
                                            End If
                                        Next

                                        If HasRealUsername Then
                                            Dim OtherUniqueRegionIds = RegionUser.GetAllMyRegionsIds(RealUsername, SqlConnection, SqlTransaction)
                                            For I As Integer = OtherUniqueRegionIds.Count - 1 To 0 Step -1
                                                If UniqueRegionIds.Contains(OtherUniqueRegionIds(I)) Then
                                                    OtherUniqueRegionIds.RemoveAt(I)
                                                End If
                                            Next

                                            If OtherUniqueRegionIds.Count > 0 Then
                                                Dim TSchedule = GetUsersScheduleFromRegionsNonVersionEmailHelper(OtherUniqueRegionIds, RealUsername, Email, LastName, FirstName, Date.MinValue, Date.MaxValue, SqlConnection, SqlTransaction)
                                                SchedulePosition.GetUsersSchedulePositionFromRegionsNonVersionEmailHelper(OtherUniqueRegionIds, RealUsername, Email, LastName, FirstName, Date.MinValue, Date.MaxValue, TSchedule, SqlConnection, SqlTransaction)
                                                ScheduleFine.GetUsersScheduleFineFromRegionsNonVersionEmailHelper(OtherUniqueRegionIds, RealUsername, Email, LastName, FirstName, Date.MinValue, Date.MaxValue, TSchedule, SqlConnection, SqlTransaction)
                                                ScheduleUserComment.GetUsersScheduleUserCommentFromRegionsNonVersionEmailHelper(OtherUniqueRegionIds, RealUsername, Email, LastName, FirstName, Date.MinValue, Date.MaxValue, TSchedule, SqlConnection, SqlTransaction)
                                                ScheduleCommentTeam.GetUsersScheduleCommentTeamFromRegionsNonVersionEmailHelper(OtherUniqueRegionIds, RealUsername, Email, LastName, FirstName, Date.MinValue, Date.MaxValue, TSchedule, SqlConnection, SqlTransaction)


                                                Dim TRegionUsers = RegionUser.LoadAllInRegionIdsHelper(OtherUniqueRegionIds, Username, SqlConnection, SqlTransaction)
                                                Dim TRegionLeagues = RegionLeague.GetRegionLeaguesPayContractedRegionIdsHelper(OtherUniqueRegionIds, SqlConnection, SqlTransaction)
                                                Dim TRegionProperties = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(OtherUniqueRegionIds, SqlConnection, SqlTransaction)

                                                RegionUsers.AddRange(TRegionUsers)
                                                RegionLeagues.AddRange(TRegionLeagues)
                                                RegionProperties.AddRange(TRegionProperties)

                                                Schedule.AddRange(TSchedule)
                                            End If
                                        End If
                                    End If

                                    Regions = New List(Of Region)
                                    For Each RegionProperty In RegionProperties
                                        Regions.Add(RegionProperty.ConvertToRegion)
                                    Next

                                End If
                            End If
                        Else
                            Schedule = GetUsersScheduleFromRegionsNonVersionHelper(Username, StartDate, EndDate, SqlConnection, SqlTransaction)
                            SchedulePosition.GetUsersSchedulePositionFromRegionsNonVersionHelper(Username, StartDate, EndDate, Schedule, SqlConnection, SqlTransaction)
                            ScheduleFine.GetUsersScheduleFineFromRegionsNonVersionHelper(Username, StartDate, EndDate, Schedule, SqlConnection, SqlTransaction)
                            ScheduleUserComment.GetUsersScheduleUserCommentFromRegionsNonVersionHelper(Username, StartDate, EndDate, Schedule, SqlConnection, SqlTransaction)
                            ScheduleCommentTeam.GetUsersScheduleCommentTeamFromRegionsNonVersionHelper(Username, StartDate, EndDate, Schedule, SqlConnection, SqlTransaction)

                            RegionLeagues = RegionLeague.GetMyRegionLeaguesPayContractedHelper(Username, SqlConnection, SqlTransaction)
                            RegionUsers = RegionUser.LoadAllInMyRegionsHelper(Username, SqlConnection, SqlTransaction)
                            RegionProperties = UmpireAssignor.RegionProperties.GetAllMyRegionPropertiesHelper(Username, SqlConnection, SqlTransaction)

                            Regions = New List(Of Region)
                            For Each RegionProperty In RegionProperties
                                Regions.Add(RegionProperty.ConvertToRegion)
                            Next
                        End If
                    Else
                        Dim Official As RegionUser = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, Username, SqlConnection, SqlTransaction)

                        Region = UmpireAssignor.Region.GetRegionHelper(RegionId, SqlConnection, SqlTransaction, True)

                        Regions = {Region}.ToList()

                        If Not Region.ShowFullScheduleToNonMembers AndAlso Official IsNot Nothing AndAlso Not Official.IsExecutive AndAlso Not Official.CanViewMasterSchedule Then
                            Return New ErrorObject("InvalidPermissions")
                        End If

                        Schedule = GetUsersScheduleFromRegionNonVersionHelper(RegionId, OfficialId, StartDate, EndDate, SqlConnection, SqlTransaction)
                        SchedulePosition.GetUsersSchedulePositionFromRegionNonVersionHelper(RegionId, OfficialId, StartDate, EndDate, Schedule, SqlConnection, SqlTransaction)
                        ScheduleFine.GetUsersScheduleFineFromRegionNonVersionHelper(RegionId, OfficialId, StartDate, EndDate, Schedule, SqlConnection, SqlTransaction)
                        ScheduleUserComment.GetUsersScheduleUserCommentFromRegionNonVersionHelper(RegionId, OfficialId, StartDate, EndDate, Schedule, SqlConnection, SqlTransaction)
                        ScheduleCommentTeam.GetUsersScheduleCommentTeamFromRegionNonVersionHelper(RegionId, OfficialId, StartDate, EndDate, Schedule, SqlConnection, SqlTransaction)

                        RegionUsers = RegionUser.LoadAllInRegionSimpleHelper(RegionId, SqlConnection, SqlTransaction)
                        RegionLeagues = RegionLeague.GetRegionLeaguesPayContractedHelper(RegionId, SqlConnection, SqlTransaction)
                    End If

                    For Each ScheduleItem In Schedule
                        For i As Integer = ScheduleItem.ScheduleFines.Count - 1 To 0 Step -1
                            If ScheduleItem.OfficialId.ToLower <> ScheduleItem.ScheduleFines(i).OfficialId.ToLower Then ScheduleItem.ScheduleFines.RemoveAt(i)
                        Next
                    Next

                    SqlTransaction.Commit()
                End Using
            End Using


            Return New With {
                .Success = True,
                .Schedule = Schedule,
                .RegionLeagues = RegionLeagues,
                .RegionUsers = RegionUsers,
                .Regions = Regions
            }
        Catch E As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function GetMergedSchedule(Regions As List(Of RegionProperties), TempSchedule As List(Of ScheduleTemp), Schedule As List(Of Schedule)) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        If TempSchedule Is Nothing Then Return Schedule

        TempSchedule.Sort(ScheduleTemp.BasicSorter)

        Schedule.Sort(UmpireAssignor.Schedule.BasicSorter)

        PublicCode.ProcessListPairs(TempSchedule, Schedule, AddressOf UmpireAssignor.Schedule.TempComparer,
                                    Sub(V1)
                                        If Not V1.IsDeleted Then
                                            Result.Add(V1.ToSchedule)
                                        End If
                                    End Sub,
                                    Sub(V2)
                                        Result.Add(V2)
                                    End Sub,
                                    Sub(V1, V2)
                                        If Not V1.IsDeleted Then
                                            Result.Add(GetSingleMergedSchedule(Regions.Find(Function(R) R.RegionId = V1.RegionId), V1, V2))
                                        End If
                                    End Sub)

        Return Result
    End Function

    Public Shared Function GetSingleMergedSchedule(Region As RegionProperties, TempSchedule As ScheduleTemp, Schedule As Schedule) As Schedule
        Dim Result As New Schedule

        If Schedule Is Nothing Then Return TempSchedule.ToSchedule
        If TempSchedule Is Nothing Then Return Nothing
        If TempSchedule.LinkedRegionIdModified Then Schedule.LinkedRegionId = TempSchedule.LinkedRegionId
        If TempSchedule.LinkedScheduleIdModified Then Schedule.LinkedScheduleId = TempSchedule.LinkedScheduleId
        If TempSchedule.GameNumberModified Then Schedule.GameNumber = TempSchedule.GameNumber
        If TempSchedule.GameTypeModified Then Schedule.GameType = TempSchedule.GameType
        If TempSchedule.LeagueIdModified Then Schedule.LeagueId = TempSchedule.LeagueId
        If TempSchedule.HomeTeamModified Then Schedule.HomeTeam = TempSchedule.HomeTeam
        If TempSchedule.HomeTeamScoreModified Then Schedule.HomeTeamScore = TempSchedule.HomeTeamScore
        If TempSchedule.HomeTeamScoreExtraModified Then Schedule.HomeTeamScoreExtra = TempSchedule.HomeTeamScoreExtra
        If TempSchedule.AwayTeamModified Then Schedule.AwayTeam = TempSchedule.AwayTeam
        If TempSchedule.AwayTeamScoreModified Then Schedule.AwayTeamScore = TempSchedule.AwayTeamScore
        If TempSchedule.AwayTeamScoreExtraModified Then Schedule.AwayTeamScoreExtra = TempSchedule.AwayTeamScoreExtra
        If TempSchedule.GameDateModified Then Schedule.GameDate = TempSchedule.GameDate
        If TempSchedule.ParkIdModified Then Schedule.ParkId = TempSchedule.ParkId
        If TempSchedule.CrewTypeModified Then Schedule.CrewType = TempSchedule.CrewType
        If TempSchedule.GameStatusModified Then Schedule.GameStatus = TempSchedule.GameStatus
        If TempSchedule.GameCommentModified Then Schedule.GameComment = TempSchedule.GameComment
        If TempSchedule.GameScoreModified Then Schedule.GameComment = TempSchedule.GameComment

        If TempSchedule.OfficialRegionIdModified Then Schedule.OfficialRegionId = TempSchedule.OfficialRegionId
        If TempSchedule.ScorekeeperRegionIdModified Then Schedule.ScorekeeperRegionId = TempSchedule.ScorekeeperRegionId
        If TempSchedule.SupervisorRegionIdModified Then Schedule.SupervisorRegionId = TempSchedule.SupervisorRegionId

        Schedule.SchedulePositions.Sort(SchedulePosition.BasicSorter)
        TempSchedule.SchedulePositions.Sort(SchedulePosition.BasicSorter)

        Dim TempSchedulePositions = Schedule.SchedulePositions
        Schedule.SchedulePositions = New List(Of SchedulePosition)

        Dim Crews = RegionLeaguePayContracted.GetSportCrewPositions(Region.Sport)
        Dim CrewsScorekeeper = RegionLeaguePayContracted.GetSportCrewPositionsScorekeepers(Region.Sport)
        Dim CrewsSupervisor = RegionLeaguePayContracted.GetSportCrewPositionsSupervisors(Region.Sport)

        PublicCode.ProcessListPairs(TempSchedule.SchedulePositions, TempSchedulePositions, AddressOf SchedulePosition.BasicComparer,
                                    Sub(V1)
                                        Schedule.SchedulePositions.Add(V1)
                                    End Sub,
                                    Sub(V2)
                                        Schedule.SchedulePositions.Add(V2)
                                    End Sub,
                                    Sub(V1, V2)
                                        Schedule.SchedulePositions.Add(V1)
                                    End Sub)

        For I As Integer = Schedule.SchedulePositions.Count - 1 To 0 Step -1
            Dim ContainsPosition As Boolean = False
            Dim SchedulePosition = Schedule.SchedulePositions(I)

            If Region.HasOfficials Then
                If TempSchedule.CrewType.ContainsKey("umpire") Then
                    If Crews(TempSchedule.CrewType("umpire").ToLower).Contains(SchedulePosition.PositionId) Then
                        ContainsPosition = True
                    End If
                End If
            End If

            If Region.HasScorekeepers Then
                If TempSchedule.CrewType.ContainsKey("scorekeeper") Then
                    If CrewsScorekeeper(TempSchedule.CrewType("scorekeeper").ToLower).Contains(SchedulePosition.PositionId) Then
                        ContainsPosition = True
                    End If
                End If
            End If

            If Region.HasSupervisors Then
                If TempSchedule.CrewType.ContainsKey("supervisor") Then
                    If CrewsSupervisor(TempSchedule.CrewType("supervisor").ToLower).Contains(SchedulePosition.PositionId) Then
                        ContainsPosition = True
                    End If
                End If
            End If

            If Not ContainsPosition Then
                Schedule.SchedulePositions.RemoveAt(I)
            End If
        Next

        Schedule.ScheduleFines.Sort(ScheduleFine.BasicSorter)
        TempSchedule.ScheduleFines.Sort(ScheduleFine.BasicSorter)

        Dim TempScheduleFines = Schedule.ScheduleFines
        Schedule.ScheduleFines = New List(Of ScheduleFine)

        PublicCode.ProcessListPairs(TempSchedule.ScheduleFines, TempScheduleFines, AddressOf ScheduleFine.BasicComparer,
                                    Sub(V1)
                                        If Not V1.IsDeleted Then Schedule.ScheduleFines.Add(V1)
                                    End Sub,
                                    Sub(V2)
                                        Schedule.ScheduleFines.Add(V2)
                                    End Sub,
                                    Sub(V1, V2)
                                        If Not V1.IsDeleted Then Schedule.ScheduleFines.Add(V1)
                                    End Sub)

        If TempSchedule.ScheduleCommentTeams.Count = 1 Then
            Schedule.ScheduleCommentTeams = {TempSchedule.ScheduleCommentTeams(0)}.ToList()
        End If

        Return Schedule
    End Function

    Public Function IsUserOnGame(Username As String, RegionProperties As List(Of RegionProperties), RegionUsers As List(Of RegionUser), Optional IgnoreFines As Boolean = False) As Boolean
        If Username = "" Then Return True
        Dim TRegionProperties As RegionProperties = PublicCode.BinarySearchItem(RegionProperties, New RegionProperties With {.RegionId = RegionId}, UmpireAssignor.RegionProperties.BasicSorter)
        If TRegionProperties Is Nothing Then Return False
        Dim RUI As Integer = PublicCode.BinarySearch_Left(RegionUsers, New RegionUser With {.RegionId = RegionId}, New GenericIComparer(Of RegionUser)(Function(RU1, RU2) RU1.RegionId.CompareTo(RU2.RegionId)))
        If RUI < 0 Then Return False

        For I As Integer = RUI To RegionUsers.Count - 1
            Dim RU = RegionUsers(I)
            If RU.RegionId <> RegionId Then Return False

            If RU.RealUsername = Username Then
                If TRegionProperties.EntityType = "referee" Then
                    If SchedulePositions.Find(Function(SP) SP.OfficialId = RU.Username) IsNot Nothing Then Return True
                    If Not IgnoreFines Then
                        If ScheduleFines.Find(Function(SF) SF.OfficialId = RU.Username) IsNot Nothing Then Return True
                    End If
                ElseIf TRegionProperties.EntityType = "team" Then
                    If RU.Positions.Contains("player") OrElse RU.Positions.Contains("coach") Then Return True
                    If RU.Positions.Contains("callup") Then
                        If ScheduleCallUps.Find(Function(SC) SC.Username = RU.Username) IsNot Nothing Then Return True
                    End If
                End If
            End If
        Next
        Return False
    End Function

    Public Shared Function GetUsersScheduleFromRegionNonVersionWithJoin(Username As String, RegionId As String, OfficialId As String, MinDate As DateTime, MaxDate As DateTime, Optional LoggedInUsername As String = "") As Object
        Dim Schedule As New List(Of Schedule)
        Dim RegionUsers = New List(Of RegionUser)
        Dim RegionParks = New List(Of Park)
        Dim RegionLeagues As New List(Of RegionLeaguePayContracted)
        Dim Teams As New List(Of Team)
        Dim Region As Region = Nothing
        Dim RegionProperties As New List(Of RegionProperties)

        Dim LinkedLeagueIds As New List(Of String)

        'Try
        Dim TResult = GetUsersScheduleFromRegionDateRange(Username, RegionId, OfficialId, MinDate, MaxDate, LoggedInUsername)
        If Not TResult.Success Then Return TResult

        RegionUsers = TResult.RegionUsers
        RegionParks = TResult.RegionParks
        RegionLeagues = TResult.RegionLeagues
        Teams = TResult.Teams
        RegionProperties = TResult.RegionProperties

        Dim TSchedule As List(Of ScheduleResult) = TResult.Schedule
        For Each ScheduleResult In TSchedule
            If ScheduleResult.Schedule.Count = 0 Then Continue For
            Dim ScheduleItem = ScheduleResult.Schedule.Last()
            If ScheduleItem.IsDeleted Then Continue For
            If ScheduleItem.IsUserOnGame(Username, RegionProperties, RegionUsers) Then
                Schedule.Add(ScheduleItem)
            End If
        Next

        Dim MyRegions As New List(Of Region)
        Dim MyRegionUsers As New List(Of RegionUser)

        If LoggedInUsername <> Username Then
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    MyRegions = Region.GetMyRegionsHelper(LoggedInUsername, SqlConnection, SqlTransaction)
                    MyRegionUsers = RegionUser.LoadAllMyRegionUsersFromRealUsername(LoggedInUsername, SqlConnection, SqlTransaction)
                End Using
            End Using
            Dim MySimilarLeagueIds As New List(Of String)

            For I As Integer = Schedule.Count - 1 To 0 Step -1
                Dim ScheduleItem = Schedule(I)
                Dim RP = RegionProperties.Find(Function(RPP) RPP.RegionId = ScheduleItem.RegionId)
                Dim CanSeeFullSchedule As Boolean = False
                If RP.ShowFullScheduleToNonMembers Then Continue For
                For Each MyRegionUser In MyRegionUsers
                    If MyRegionUser.RegionId = ScheduleItem.RegionId Then
                        If RP.EntityType = "team" Then
                            CanSeeFullSchedule = True
                        End If
                        If MyRegionUser.IsExecutive Then
                            CanSeeFullSchedule = True
                        End If
                        If MyRegionUser.CanViewMasterSchedule Then
                            CanSeeFullSchedule = True
                        End If
                    End If
                Next
                If Not CanSeeFullSchedule Then
                    If RP.EntityType = "league" Then
                        For Each Team In Teams
                            If Team.IsLinked AndAlso Team.RegionId = ScheduleItem.LinkedRegionId Then
                                If MyRegions.Find(Function(RI) RI.RegionId = Team.RealTeamId) IsNot Nothing Then
                                    CanSeeFullSchedule = True
                                End If
                            End If
                        Next
                    End If
                End If
                If Not CanSeeFullSchedule Then
                    Schedule.RemoveAt(I)
                End If
            Next
        End If

        Dim ExecutiveRegionUser = RegionUsers.Find(Function(RU)
                                                       Return RU.RealUsername = Username AndAlso RU.IsExecutive()
                                                   End Function)


        If ExecutiveRegionUser Is Nothing Then
            For Each ScheduleItem In Schedule
                If Not ScheduleItem.ContainsUsername(Username, RegionUsers) Then
                    ScheduleItem.ClearPostionsAndFines()
                End If
            Next
        End If

        '******* REMOVE EXCESS ENTITY PROPERTIES ********

        For I As Integer = RegionProperties.Count - 1 To 0 Step -1
            Dim FoundItem As Boolean = False
            Dim RP = RegionProperties(I)
            For Each ScheduleItem In Schedule
                If ScheduleItem.RegionId.ToLower = RP.RegionId.ToLower OrElse ScheduleItem.LinkedRegionId.ToLower = RP.RegionId.ToLower Then
                    FoundItem = True
                    Exit For
                End If
            Next
            If Not FoundItem Then RegionProperties.RemoveAt(I)
        Next

        For I As Integer = RegionUsers.Count - 1 To 0 Step -1
            Dim FoundItem As Boolean = False
            Dim RegionUser = RegionUsers(I)
            For Each ScheduleItem In Schedule
                If ScheduleItem.RegionId = RegionUser.RegionId Then
                    Dim RP = RegionProperties.Find(Function(RPP) RPP.RegionId = ScheduleItem.RegionId)
                    If RP.EntityType = "team" OrElse RP.EntityType = "leauge" Then
                        FoundItem = True
                        Exit For
                    ElseIf RP.EntityType = "referee" Then
                        For Each SchedulePosition In ScheduleItem.SchedulePositions
                            If SchedulePosition.OfficialId.ToLower = RegionUser.Username.ToLower Then
                                FoundItem = True
                                Exit For
                            End If
                        Next
                        For Each ScheduleFine In ScheduleItem.ScheduleFines
                            If ScheduleFine.OfficialId.ToLower = RegionUser.Username.ToLower Then
                                FoundItem = True
                                Exit For
                            End If
                        Next
                    End If
                End If
            Next

            If Not FoundItem Then RegionUsers.RemoveAt(I)
        Next

        For I As Integer = RegionParks.Count - 1 To 0 Step -1
            Dim FoundItem As Boolean = False
            Dim RegionPark = RegionParks(I)
            For Each ScheduleItem In Schedule
                If ScheduleItem.LinkedRegionId.ToLower = RegionPark.RegionId.ToLower AndAlso ScheduleItem.ParkId.ToLower = RegionPark.ParkId.ToLower Then
                    FoundItem = True
                    Exit For
                End If
                If ScheduleItem.RegionId.ToLower = RegionPark.RegionId.ToLower AndAlso ScheduleItem.ParkId.ToLower = RegionPark.ParkId.ToLower Then
                    FoundItem = True
                    Exit For
                End If
            Next
            If Not FoundItem Then RegionParks.RemoveAt(I)
        Next

        For I As Integer = RegionLeagues.Count - 1 To 0 Step -1
            Dim FoundItem As Boolean = False
            Dim RegionLeague = RegionLeagues(I)
            For Each ScheduleItem In Schedule
                If ScheduleItem.RegionId.ToLower = RegionLeague.RegionId.ToLower AndAlso ScheduleItem.LeagueId.ToLower = RegionLeague.LeagueId.ToLower Then
                    FoundItem = True
                    Exit For
                End If
            Next
            If Not FoundItem Then RegionLeagues.RemoveAt(I)
        Next

        For I As Integer = Teams.Count - 1 To 0 Step -1
            Dim FoundItem As Boolean = False
            Dim Team = Teams(I)
            For Each ScheduleItem In Schedule
                If ScheduleItem.LinkedRegionId = Team.RegionId AndAlso ScheduleItem.AwayTeam.ToLower = Team.TeamId.ToLower Then
                    FoundItem = True
                    Exit For
                End If
                If ScheduleItem.RegionId = Team.RegionId AndAlso ScheduleItem.AwayTeam.ToLower = Team.TeamId.ToLower Then
                    FoundItem = True
                    Exit For
                End If
                If ScheduleItem.LinkedRegionId = Team.RegionId AndAlso ScheduleItem.HomeTeam.ToLower = Team.TeamId.ToLower Then
                    FoundItem = True
                    Exit For
                End If
                If ScheduleItem.RegionId = Team.RegionId AndAlso ScheduleItem.HomeTeam.ToLower = Team.TeamId.ToLower Then
                    FoundItem = True
                    Exit For
                End If
            Next
            If Not FoundItem Then Teams.RemoveAt(I)
        Next

        Dim DisplayingScheduleRegionIds As New List(Of String)
        For Each ScheduleItem In Schedule
            If Not DisplayingScheduleRegionIds.Contains(ScheduleItem.RegionId) Then
                DisplayingScheduleRegionIds.Add(ScheduleItem.RegionId)
            End If
        Next

        Return New With {
            .Success = True,
            .Schedule = Schedule,
            .RegionUsers = RegionUsers,
            .RegionParks = RegionParks,
            .RegionProperties = RegionProperties,
            .RegionLeagues = RegionLeagues,
            .Teams = Teams,
            .DisplayingScheduleRegionIds = DisplayingScheduleRegionIds
        }
        'Catch E As Exception
        'Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Shared Function TrimHiddenScheduleData(ScheduleItem As Schedule, Regions As List(Of RegionProperties), RegionUsers As List(Of RegionUser), Username As String) As Schedule
        Dim Result As Schedule = ScheduleItem.CloneItem

        Dim IsExecutive As Boolean = False
        Dim CanViewSupervisors As Boolean = False
        Dim Region = Regions.Find(Function (R) R.RegionId = ScheduleItem.RegionId)
        If Region Is Nothing Then Return Result

        If Region.HasSupervisors Then
            Dim Positions = RegionLeaguePayContracted.GetSportPositionOrderSupervisor(Region.Sport)

            Dim SupervisorOfficialIds As New List(of String)
            For I As Integer = Result.SchedulePositions.Count -1 To 0 Step -1
                If Positions.Contains(Result.SchedulePositions(I).PositionId.ToLower) Then
                    SupervisorOfficialIds.Add(Result.SchedulePositions(I).OfficialId.ToLower)
                End If
            Next I

            For Each RegionUser In RegionUsers
                If RegionUser.RealUsername.ToLower = Username.ToLower Then
                    If RegionUser.IsExecutive Then
                        IsExecutive = True
                        Exit For
                    End If
                    If RegionUser.CanViewSupervisors Then
                        CanViewSupervisors = True
                    End If
                    If SupervisorOfficialIds.Contains(RegionUser.Username.toLower) Then
                        CanViewSupervisors = True
                    End If
                End If
            Next

            If IsExecutive Then Return Result

            If Not CanViewSupervisors Then
                If Result.CrewType.ContainsKey("supervisor") Then
                    Result.CrewType("supervisor") =  "0-man"
                Else
                    Result.CrewType.Add("supervisor", "0-man")
                End If
                
                For I As Integer = Result.SchedulePositions.Count -1 To 0 Step -1
                    If Positions.Contains(Result.SchedulePositions(I).PositionId.ToLower) Then
                        Result.SchedulePositions.RemoveAt(I)
                    End If
                Next
            End If
        End if

        Return Result
    End Function

    Public Shared Function GetAllMasterScheduleHelper(StartDate As Date, EndDate As Date, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        Dim CommandText As String = ""
        If StartDate = Date.MinValue Then
            CommandText = "SELECT RegionId, ScheduleId, VersionId, LinkedRegionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, IsDeleted, DateAdded, StatLinks FROM ScheduleVersion ORDER BY RegionId, ScheduleId, VersionId"
        Else
            CommandText = "SELECT RegionId, ScheduleId, VersionId, LinkedRegionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, IsDeleted, DateAdded, StatLinks FROM ScheduleVersion WHERE GameDate >= @MinGameDate AND GameDate < @MaxGameDate ORDER BY RegionId, ScheduleId, VersionId"
        End If

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            If StartDate <> Date.MinValue Then
                SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", StartDate))
                SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", EndDate))
            End If

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = Reader.GetString(),
                           .ScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .IsDeleted = Reader.GetBoolean(),
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .DateAdded = Reader.GetDateTime(),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetMasterScheduleFromRegionsHelper(RegionIds As List(Of String), StartDate As Date, EndDate As Date, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim CommandText As String = ""
        If StartDate = Date.MinValue Then
            CommandText = "SELECT RegionId, ScheduleId, VersionId, LinkedRegionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, IsDeleted, DateAdded, StatLinks FROM ScheduleVersion WHERE RegionId IN (" & RegionIdParams.ToString & ") ORDER BY RegionId, ScheduleId, VersionId, LinkedRegionId"
        Else
            CommandText = "SELECT RegionId, ScheduleId, VersionId, LinkedRegionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, IsDeleted, DateAdded, StatLinks FROM ScheduleVersion WHERE RegionId IN (" & RegionIdParams.ToString & ")  AND GameDate >= @MinGameDate AND GameDate < @MaxGameDate ORDER BY RegionId, ScheduleId, VersionId, LinkedRegionId"
        End If

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next
            If StartDate <> Date.MinValue Then
                SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", StartDate))
                SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", EndDate))
            End If

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = Reader.GetString(),
                           .ScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .IsDeleted = Reader.GetBoolean(),
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .DateAdded = Reader.GetDateTime(),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Private Shared Function SafeDeserializeCrewType(jsonString As String) As Dictionary(Of String, String)
        Try
            If String.IsNullOrEmpty(jsonString) Then
                Return New Dictionary(Of String, String)
            End If

            ' Clean the JSON string and ensure it's valid
            Dim cleanJson = jsonString.Trim().ToLower()
            If cleanJson.StartsWith("{") AndAlso cleanJson.EndsWith("}") Then
                Return JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(cleanJson)
            Else
                Return New Dictionary(Of String, String)
            End If
        Catch ex As Exception
            ' Log the error or handle it gracefully
            Debug.WriteLine($"Error in SafeDeserializeCrewType: {ex.Message}")
            Debug.WriteLine($"Stack Trace: {ex.StackTrace}")
            Return New Dictionary(Of String, String)
        End Try
    End Function

    Public Shared Function GetUsersScheduleFromRegionsRangeHelper(Username As String, StartDate As Date, EndDate As Date, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        Dim CommandText As String = ""

        If StartDate = Date.MinValue Then
            CommandText = <SQL>
SELECT        SV.RegionId, SV.ScheduleId, SV.VersionId, RU.Username, SV.LinkedRegionId, SV.LinkedScheduleId, SV.GameNumber, SV.GameType, SV.LeagueId, SV.HomeTeam, SV.HomeTeamScore, SV.HomeTeamScoreExtra, SV.AwayTeam, SV.AwayTeamScore, SV.AwayTeamScoreExtra, SV.GameDate, SV.ParkId, SV.CrewType, SV.GameStatus, SV.GameComment, SV.GameScore, SV.OfficialRegionId, SV.ScorekeeperRegionId, SV.SupervisorRegionId, SV.IsDeleted, SV.DateAdded, SV.StatLinks
FROM            ScheduleVersion AS SV, RegionUser AS RU
WHERE 
RU.RealUsername = @Username AND SV.RegionId = RU.RegionId AND RU.IsArchived = 0 AND
(ScheduleId IN (SELECT SV.ScheduleId FROM ScheduleVersion AS SV, SchedulePositionVersion AS SP, RegionUser as RU2 WHERE RU.RegionId = RU2.RegionId AND RU.Username = RU2.Username AND SV.RegionId = RU2.RegionId AND SV.RegionId = SP.RegionId AND SV.ScheduleId = SP.ScheduleId AND SV.VersionId = SP.VersionId AND SP.OfficialId = RU2.Username)
OR ScheduleId IN (SELECT SV.ScheduleId FROM ScheduleVersion AS SV, ScheduleFineVersion AS SF, RegionUser as RU2 WHERE RU.RegionId = RU2.RegionId AND RU.Username = RU2.Username AND SV.RegionId = RU2.RegionId AND SV.RegionId = SF.RegionId AND SV.ScheduleId = SF.ScheduleId AND SV.VersionId = SF.VersionId AND SF.OfficialId = RU2.Username)
OR ScheduleId IN (SELECT SV.ScheduleId FROM ScheduleVersion AS SV, ScheduleUserCommentVersion AS SU, RegionUser as RU2 WHERE RU.RegionId = RU2.RegionId AND RU.Username = RU2.Username AND SV.RegionId = RU2.RegionId AND SV.RegionId = SU.RegionId AND SV.ScheduleId = SU.ScheduleId AND SV.VersionId = SU.VersionId AND SU.OfficialId = RU2.Username))
ORDER BY SV.RegionId, SV.ScheduleId, SV.VersionId, RU.Username
                          </SQL>.Value()
        Else
            CommandText = <SQL>
SELECT        SV.RegionId, SV.ScheduleId, SV.VersionId, RU.Username, SV.LinkedRegionId, SV.LinkedScheduleId, SV.GameNumber, SV.GameType, SV.LeagueId, SV.HomeTeam, SV.HomeTeamScore, SV.HomeTeamScoreExtra, SV.AwayTeam, SV.AwayTeamScore, SV.AwayTeamScoreExtra, SV.GameDate, SV.ParkId, SV.CrewType, SV.GameStatus, SV.GameComment, SV.GameScore, SV.OfficialRegionId, SV.ScorekeeperRegionId, SV.SupervisorRegionId, SV.IsDeleted, SV.DateAdded, SV.StatLinks
FROM            ScheduleVersion AS SV, RegionUser AS RU
WHERE 
RU.RealUsername = @Username AND SV.RegionId = RU.RegionId AND RU.IsArchived = 0 AND
(ScheduleId IN (SELECT SV.ScheduleId FROM ScheduleVersion AS SV, SchedulePositionVersion AS SP, RegionUser as RU2 WHERE RU.RegionId = RU2.RegionId AND RU.Username = RU2.Username AND SV.RegionId = RU2.RegionId AND SV.RegionId = SP.RegionId AND SV.ScheduleId = SP.ScheduleId AND SV.VersionId = SP.VersionId AND SP.OfficialId = RU2.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR ScheduleId IN (SELECT SV.ScheduleId FROM ScheduleVersion AS SV, ScheduleFineVersion AS SF, RegionUser as RU2 WHERE RU.RegionId = RU2.RegionId AND RU.Username = RU2.Username AND SV.RegionId = RU2.RegionId AND SV.RegionId = SF.RegionId AND SV.ScheduleId = SF.ScheduleId AND SV.VersionId = SF.VersionId AND SF.OfficialId = RU2.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR ScheduleId IN (SELECT SV.ScheduleId FROM ScheduleVersion AS SV, ScheduleUserCommentVersion AS SU, RegionUser as RU2 WHERE RU.RegionId = RU2.RegionId AND RU.Username = RU2.Username AND SV.RegionId = RU2.RegionId AND SV.RegionId = SU.RegionId AND SV.ScheduleId = SU.ScheduleId AND SV.VersionId = SU.VersionId AND SU.OfficialId = RU2.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate))
ORDER BY SV.RegionId, SV.ScheduleId, SV.VersionId, RU.Username
                          </SQL>.Value()
        End If


        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))

            If StartDate <> Date.MinValue Then
                SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", StartDate))
                SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", EndDate))
            End If

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = Reader.GetString().ToLower,
                           .ScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .OfficialId = Reader.GetString(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = SafeDeserializeCrewType(Reader.GetString()),
                           .GameStatus = Reader.GetString().ToLower,
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .IsDeleted = Reader.GetBoolean(),
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .DateAdded = Reader.GetDateTime(),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetUsersScheduleFromRegionsRangeEmailHelper(RegionIds As List(Of String), Username As String, Email As String, LastName As String, FirstName As String, StartDate As Date, EndDate As Date, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        If RegionIds.Count = 0 Then Return Result

        Dim RegionIdParams As New StringBuilder()
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim UsernameSearch As String = ""
        If Username <> "" AndAlso Email <> "" Then
            UsernameSearch = "(RU.RealUsername = @Username Or RU.Email = @Email)"
        ElseIf Username <> "" Then
            UsernameSearch = "RU.RealUsername = @Username"
        ElseIf Email <> "" Then
            UsernameSearch = "RU.Email = @Email"
        End If

        If LastName <> "" OrElse FirstName <> "" Then
            UsernameSearch &= " AND RU.LastName = @LastName AND RU.FirstName = @FirstName"
        End If

        If UsernameSearch = "" Then
            UsernameSearch = "1 = 1"
        End If

        Dim CommandText = <SQL>
SELECT        SV.RegionId, SV.ScheduleId, SV.VersionId, RU.Username, SV.LinkedRegionId, SV.LinkedScheduleId, SV.GameNumber, SV.GameType, SV.LeagueId, SV.HomeTeam, SV.HomeTeamScore, SV.HomeTeamScoreExtra, SV.AwayTeam, SV.AwayTeamScore, SV.AwayTeamScoreExtra, SV.GameDate, SV.ParkId, SV.CrewType, SV.GameStatus, SV.GameComment, SV.GameScore, SV.OfficialRegionId, SV.ScorekeeperRegionId, SV.SupervisorRegionId, SV.IsDeleted, SV.DateAdded, SV.StatLinks
FROM            ScheduleVersion AS SV, RegionUser AS RU
WHERE 
RU.RegionId IN ({0}) AND {1} AND SV.RegionId = RU.RegionId AND RU.IsArchived = 0 AND
(ScheduleId IN (SELECT SV.ScheduleId FROM ScheduleVersion AS SV, SchedulePositionVersion AS SP, RegionUser as RU2 WHERE RU.RegionId = RU2.RegionId AND RU.Username = RU2.Username AND SV.RegionId = RU2.RegionId AND SV.RegionId = SP.RegionId AND SV.ScheduleId = SP.ScheduleId AND SV.VersionId = SP.VersionId AND SP.OfficialId = RU2.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR ScheduleId IN (SELECT SV.ScheduleId FROM ScheduleVersion AS SV, ScheduleFineVersion AS SF, RegionUser as RU2 WHERE RU.RegionId = RU2.RegionId AND RU.Username = RU2.Username AND SV.RegionId = RU2.RegionId AND SV.RegionId = SF.RegionId AND SV.ScheduleId = SF.ScheduleId AND SV.VersionId = SF.VersionId AND SF.OfficialId = RU2.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR ScheduleId IN (SELECT SV.ScheduleId FROM ScheduleVersion AS SV, ScheduleUserCommentVersion AS SU, RegionUser as RU2 WHERE RU.RegionId = RU2.RegionId AND RU.Username = RU2.Username AND SV.RegionId = RU2.RegionId AND SV.RegionId = SU.RegionId AND SV.ScheduleId = SU.ScheduleId AND SV.VersionId = SU.VersionId AND SU.OfficialId = RU2.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate))
ORDER BY SV.RegionId, SV.ScheduleId, SV.VersionId, RU.Username
                          </SQL>.Value().ToString().Replace("{0}", RegionIdParams.ToString()).Replace("{1}", UsernameSearch)

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next
            If Username <> "" Then SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            If Email <> "" Then SqlCommand.Parameters.Add(New SqlParameter("Email", Email))
            If LastName <> "" OrElse FirstName <> "" Then
                SqlCommand.Parameters.Add(New SqlParameter("LastName", LastName))
                SqlCommand.Parameters.Add(New SqlParameter("FirstName", FirstName))
            End If
            SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", StartDate))
            SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", EndDate))

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = Reader.GetString().ToLower,
                           .ScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .OfficialId = Reader.GetString(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .GameStatus = Reader.GetString().ToLower,
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .IsDeleted = Reader.GetBoolean(),
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .DateAdded = Reader.GetDateTime(),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function


    Public Shared Function GetUsersScheduleFromRegionRangeHelper(RegionId As String, OfficialId As String, StartDate As Date, EndDate As Date, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        Dim CommandText As String = ""

        CommandText = <SQL>
SELECT        ScheduleId, VersionId, LinkedRegionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, IsDeleted, DateAdded, StatLinks
FROM            ScheduleVersion
WHERE RegionId = @RegionId
AND (ScheduleId IN (SELECT SV.ScheduleId FROM ScheduleVersion AS SV, SchedulePositionVersion AS SP  WHERE SV.RegionId = @RegionId AND SV.RegionId = SP.RegionId AND SV.ScheduleId = SP.ScheduleId AND SV.VersionId = SP.VersionId AND SP.OfficialId = @OfficialId AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR ScheduleId IN (SELECT SV.ScheduleId FROM ScheduleVersion AS SV, ScheduleFineVersion AS SF WHERE SV.RegionId = @RegionId AND SV.RegionId = SF.RegionId AND SV.ScheduleId = SF.ScheduleId AND SV.VersionId = SF.VersionId AND SF.OfficialId = @OfficialId AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR ScheduleId IN (SELECT SV.ScheduleId FROM ScheduleVersion AS SV, ScheduleUserCommentVersion AS SU WHERE SV.RegionId = @RegionId AND SV.RegionId = SU.RegionId AND SV.ScheduleId = SU.ScheduleId AND SV.VersionId = SU.VersionId AND SU.OfficialId = @OfficialId AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate))
ORDER BY ScheduleId, VersionId
                          </SQL>.Value()

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("OfficialId", OfficialId))

            SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", StartDate))
            SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", EndDate))

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = RegionId.ToLower,
                           .ScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .OfficialId = OfficialId.ToLower,
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .IsDeleted = Reader.GetBoolean(),
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .DateAdded = Reader.GetDateTime(),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetMasterScheduleFromRegionTeamsNonVersionHelper(RegionId As String, Team1 As String, Team2 As String, StartDate As Date, EndDate As Date, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        Dim CommandText As String = "SELECT RegionId, ScheduleId, VersionId, LinkedRegionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, StatLinks FROM Schedule WHERE RegionId = @RegionId "

        If StartDate = Date.MinValue Then
        Else
            CommandText &= "AND GameDate >= @MinGameDate AND GameDate < @MaxGameDate "
        End If

        If Team1 = "" Then
        ElseIf Team1 <> "" AndAlso Team2 = "" Then
            CommandText &= "AND (HomeTeam = @Team1 OR AwayTeam = @Team1) "
        ElseIf Team1 <> "" AndAlso Team2 <> "" Then
            CommandText &= "AND (HomeTeam = @Team1 OR AwayTeam = @Team1 OR HomeTeam = @Team2 OR AwayTeam = @Team2) "
        End If

        CommandText &= "ORDER BY RegionId, ScheduleId"

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))

            If Team1 = "" Then
            ElseIf Team1 <> "" AndAlso Team2 = "" Then
                SqlCommand.Parameters.Add(New SqlParameter("Team1", Team1))
            ElseIf Team1 <> "" AndAlso Team2 <> "" Then
                SqlCommand.Parameters.Add(New SqlParameter("Team1", Team1))
                SqlCommand.Parameters.Add(New SqlParameter("Team2", Team2))
            End If

            If StartDate <> Date.MinValue Then
                SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", StartDate))
                SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", EndDate))
            End If

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = Reader.GetString(),
                           .ScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .IsDeleted = False,
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetMasterScheduleFromRegionsNonVersionHelper(RegionIds As List(Of String), StartDate As Date, EndDate As Date, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        If RegionIds.Count = 0 Then Return Result

        Dim RegionIdParams As New StringBuilder()
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim CommandText As String = ""
        If StartDate = Date.MinValue Then
            CommandText = "SELECT RegionId, ScheduleId, VersionId, LinkedRegionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, StatLinks FROM Schedule WHERE RegionId IN ({0}) ORDER BY RegionId, ScheduleId".Replace("{0}", RegionIdParams.ToString())
        Else
            CommandText = "SELECT RegionId, ScheduleId, VersionId, LinkedRegionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, StatLinks FROM Schedule WHERE RegionId IN ({0}) AND GameDate >= @MinGameDate AND GameDate < @MaxGameDate ORDER BY RegionId, ScheduleId".Replace("{0}", RegionIdParams.ToString())
        End If

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next

            If StartDate <> Date.MinValue Then
                SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", StartDate))
                SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", EndDate))
            End If

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = Reader.GetString(),
                           .ScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .IsDeleted = False,
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

        Public Shared Function GetMasterScheduleFromRegionsNonVersionAllRegionsHelper(StartDate As Date, EndDate As Date, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        Dim CommandText As String = ""
        If StartDate = Date.MinValue Then
            CommandText = "SELECT RegionId, ScheduleId, VersionId, LinkedRegionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, StatLinks FROM Schedule ORDER BY RegionId, ScheduleId"
        Else
            CommandText = "SELECT RegionId, ScheduleId, VersionId, LinkedRegionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, StatLinks FROM Schedule WHERE GameDate >= @MinGameDate AND GameDate < @MaxGameDate ORDER BY RegionId, ScheduleId"
        End If

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            If StartDate <> Date.MinValue Then
                SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", StartDate))
                SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", EndDate))
            End If

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = Reader.GetString(),
                           .ScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .IsDeleted = False,
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetIgnoredMasterScheduleFromRegionIdsNonVersionHelper(RegionIds As List(Of String), StartDate As Date, EndDate As Date, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of ScheduleIgnore)
        Dim Result As New List(Of ScheduleIgnore)

        If RegionIds.Count = 0 Then Return Result

        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim CommandText As String = ""
        If StartDate = Date.MinValue Then
            CommandText = "SELECT RegionId, LinkedRegionId, ScheduleId, VersionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, IsDeleted, DateAdded FROM ScheduleIgnore WHERE RegionId IN ({0}) ORDER BY RegionId, LinkedRegionId, ScheduleId".Replace("{0}", RegionIdParams.ToString())
        Else
            CommandText = "SELECT RegionId, LinkedRegionId, ScheduleId, VersionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, IsDeleted, DateAdded FROM ScheduleIgnore WHERE RegionId IN ({0}) AND GameDate >= @MinGameDate AND GameDate < @MaxGameDate ORDER BY RegionId, LinkedRegionId, ScheduleId".Replace("{0}", RegionIdParams.ToString())
        End If

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next

            If StartDate <> Date.MinValue Then
                SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", StartDate))
                SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", EndDate))
            End If

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New ScheduleIgnore With {
                           .RegionId = Reader.GetString(),
                           .LinkedRegionId = Reader.GetString(),
                           .ScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .IsDeleted = Reader.GetBoolean(),
                           .DateAdded = Reader.GetDateTime()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetUsersScheduleFromRegionsNonVersionHelper(Username As String, StartDate As Date, EndDate As Date, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        Dim CommandText As String = ""
        If StartDate = Date.MinValue Then
            CommandText = <SQL>
SELECT        S.RegionId, RU.Username, S.ScheduleId, S.VersionId, S.LinkedRegionId, S.LinkedScheduleId, S.GameNumber, S.GameType, S.LeagueId, S.HomeTeam, S.HomeTeamScore, S.HomeTeamScoreExtra, S.AwayTeam, S.AwayTeamScore, S.AwayTeamScoreExtra, S.GameDate, S.ParkId, S.CrewType, S.GameStatus, S.GameComment, S.GameScore, S.OfficialRegionId, S.ScorekeeperRegionId, S.SupervisorRegionId, S.StatLinks
FROM            Schedule As S, RegionUser AS RU
WHERE RU.RealUsername = @Username AND S.RegionId = RU.RegionId AND RU.IsArchived = 0
AND (ScheduleId IN (SELECT SP.ScheduleId FROM SchedulePosition AS SP WHERE SP.RegionId = RU.RegionId AND SP.OfficialId = RU.Username)
OR ScheduleId IN (SELECT SF.ScheduleId FROM ScheduleFine AS SF WHERE SF.RegionId = RU.RegionId AND SF.OfficialId = RU.Username)
OR ScheduleId IN (SELECT SU.ScheduleId FROM ScheduleUserComment AS SU WHERE SU.RegionId = RU.RegionId AND SU.OfficialId = RU.Username))
ORDER BY S.RegionId, S.ScheduleId, RU.Username
                          </SQL>
        Else
            CommandText = <SQL>
SELECT        S.RegionId, RU.Username, S.ScheduleId, S.VersionId, S.LinkedRegionId, S.LinkedScheduleId, S.GameNumber, S.GameType, S.LeagueId, S.HomeTeam, S.HomeTeamScore, S.HomeTeamScoreExtra, S.AwayTeam, S.AwayTeamScore, S.AwayTeamScoreExtra, S.GameDate, S.ParkId, S.CrewType, S.GameStatus, S.GameComment, S.GameScore, S.OfficialRegionId, S.ScorekeeperRegionId, S.SupervisorRegionId, S.StatLinks
FROM            Schedule AS S, RegionUser AS RU
WHERE RU.RealUsername = @Username AND S.RegionId = RU.RegionId AND RU.IsArchived = 0
AND (ScheduleId IN (SELECT SV.ScheduleId FROM Schedule AS SV, SchedulePosition AS SP WHERE SP.RegionId = RU.RegionId AND SV.RegionId = SP.RegionId AND SV.ScheduleId = SP.ScheduleId AND SP.OfficialId = RU.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR ScheduleId IN (SELECT SV.ScheduleId FROM Schedule AS SV, ScheduleFine AS SF WHERE SF.RegionId = RU.RegionId AND SV.ScheduleId = SF.ScheduleId AND SF.OfficialId = RU.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR ScheduleId IN (SELECT SV.ScheduleId FROM Schedule AS SV, ScheduleUserComment AS SU WHERE SU.RegionId = RU.RegionId AND SV.ScheduleId = SU.ScheduleId AND SU.OfficialId = RU.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate))
ORDER BY S.RegionId, S.ScheduleId, RU.Username
                          </SQL>
        End If

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))

            If StartDate <> Date.MinValue Then
                SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", StartDate))
                SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", EndDate))
            End If

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = Reader.GetString().ToLower,
                           .OfficialId = Reader.GetString().ToLower,
                           .ScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function


    Public Shared Function GetUsersScheduleFromRegionsNonVersionEmailHelper(RegionIds As List(Of String), Username As String, Email As String, LastName As String, FirstName As String, StartDate As Date, EndDate As Date, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim EmailCondition As String = ""
        If Username = "" Then
            EmailCondition = "RU.Email = @Email"
        ElseIf Email = "" Then
            EmailCondition = "RU.RealUsername = @Username"
        Else
            EmailCondition = "(RU.Email = @Email OR RU.RealUsername = @Username)"
        End If

        If FirstName <> "" Then
            EmailCondition &= " AND RU.FirstName = @FirstName"
        End If

        If LastName <> "" Then
            EmailCondition &= " AND RU.LastName = @LastName"
        End If

        Dim CommandText As String = ""
        If StartDate = Date.MinValue Then
            CommandText = <SQL>
SELECT        S.RegionId, RU.Username, S.ScheduleId, S.VersionId, S.LinkedRegionId, S.LinkedScheduleId, S.GameNumber, S.GameType, S.LeagueId, S.HomeTeam, S.HomeTeamScore, S.HomeTeamScoreExtra, S.AwayTeam, S.AwayTeamScore, S.AwayTeamScoreExtra, S.GameDate, S.ParkId, S.CrewType, S.GameStatus, S.GameComment, S.GameScore, S.OfficialRegionId, S.ScorekeeperRegionId, S.SupervisorRegionId, S.StatLinks
FROM            Schedule As S, RegionUser AS RU
WHERE RU.RegionId IN ({0}) AND {1} AND S.RegionId = RU.RegionId AND RU.IsArchived = 0
AND (ScheduleId IN (SELECT SP.ScheduleId FROM SchedulePosition AS SP WHERE SP.RegionId = RU.RegionId AND SP.OfficialId = RU.Username)
OR ScheduleId IN (SELECT SF.ScheduleId FROM ScheduleFine AS SF WHERE SF.RegionId = RU.RegionId AND SF.OfficialId = RU.Username)
OR ScheduleId IN (SELECT SU.ScheduleId FROM ScheduleUserComment AS SU WHERE SU.RegionId = RU.RegionId AND SU.OfficialId = RU.Username))
ORDER BY S.RegionId, S.ScheduleId, RU.Username
                          </SQL>.Value().Replace("{0}", RegionIdParams.ToString()).Replace("{1}", EmailCondition)
        Else
            CommandText = <SQL>
SELECT        S.RegionId, RU.Username, S.ScheduleId, S.VersionId, S.LinkedRegionId, S.LinkedScheduleId, S.GameNumber, S.GameType, S.LeagueId, S.HomeTeam, S.HomeTeamScore, S.HomeTeamScoreExtra, S.AwayTeam, S.AwayTeamScore, S.AwayTeamScoreExtra, S.GameDate, S.ParkId, S.CrewType, S.GameStatus, S.GameComment, S.GameScore, S.OfficialRegionId, S.ScorekeeperRegionId, S.SupervisorRegionId, S.StatLinks
FROM            Schedule AS S, RegionUser AS RU
WHERE RU.RegionId IN ({0}) AND {1} AND S.RegionId = RU.RegionId AND RU.IsArchived = 0
AND (ScheduleId IN (SELECT SV.ScheduleId FROM Schedule AS SV, SchedulePosition AS SP WHERE SP.RegionId = RU.RegionId AND SV.RegionId = SP.RegionId AND SV.ScheduleId = SP.ScheduleId AND SP.OfficialId = RU.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR ScheduleId IN (SELECT SV.ScheduleId FROM Schedule AS SV, ScheduleFine AS SF WHERE SF.RegionId = RU.RegionId AND SV.ScheduleId = SF.ScheduleId AND SF.OfficialId = RU.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR ScheduleId IN (SELECT SV.ScheduleId FROM Schedule AS SV, ScheduleUserComment AS SU WHERE SU.RegionId = RU.RegionId AND SV.ScheduleId = SU.ScheduleId AND SU.OfficialId = RU.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate))
ORDER BY S.RegionId, S.ScheduleId, RU.Username
                          </SQL>.Value().Replace("{0}", RegionIdParams.ToString()).Replace("{1}", EmailCondition)
        End If

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next

            If Username <> "" Then
                SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            End If

            If Email <> "" Then
                SqlCommand.Parameters.Add(New SqlParameter("Email", Email))
            End If
            SqlCommand.Parameters.Add(New SqlParameter("LastName", LastName))
            SqlCommand.Parameters.Add(New SqlParameter("FirstName", FirstName))

            If StartDate <> Date.MinValue Then
                SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", StartDate))
                SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", EndDate))
            End If

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = Reader.GetString().ToLower,
                           .OfficialId = Reader.GetString().ToLower,
                           .ScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetUsersScheduleFromRegionNonVersionHelper(RegionId As String, OfficialId As String, StartDate As Date, EndDate As Date, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        Dim CommandText As String = ""
        If StartDate = Date.MinValue Then
            CommandText = <SQL>
SELECT        ScheduleId, VersionId, LinkedRegionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, StatLinks
FROM            Schedule
WHERE RegionId = @RegionId
AND (ScheduleId IN (SELECT ScheduleId FROM SchedulePosition WHERE RegionId = @RegionId AND OfficialId = @OfficialId)
OR ScheduleId IN (SELECT ScheduleId FROM ScheduleFine WHERE RegionId = @RegionId AND OfficialId = @OfficialId)
OR ScheduleId IN (SELECT ScheduleId FROM ScheduleUserComment WHERE RegionId = @RegionId AND OfficialId = @OfficialId))
ORDER BY ScheduleId
                          </SQL>
        Else
            CommandText = <SQL>
SELECT        ScheduleId, VersionId, LinkedRegionId, LinkedScheduleId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, StatLinks
FROM            Schedule
WHERE RegionId = @RegionId
AND (ScheduleId IN (SELECT SV.ScheduleId FROM Schedule AS SV, SchedulePosition AS SP  WHERE SV.RegionId = @RegionId AND SV.RegionId = SP.RegionId AND SV.ScheduleId = SP.ScheduleId AND SP.OfficialId = @OfficialId AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR ScheduleId IN (SELECT SV.ScheduleId FROM Schedule AS SV, ScheduleFine AS SF WHERE SV.RegionId = @RegionId AND SV.RegionId = SF.RegionId AND SV.ScheduleId = SF.ScheduleId AND SF.OfficialId = @OfficialId AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR ScheduleId IN (SELECT SV.ScheduleId FROM Schedule AS SV, ScheduleUserComment AS SU WHERE SV.RegionId = @RegionId AND SV.RegionId = SU.RegionId AND SV.ScheduleId = SU.ScheduleId AND SU.OfficialId = @OfficialId AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate))
ORDER BY ScheduleId
                          </SQL>
        End If

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("OfficialId", OfficialId))

            If StartDate <> Date.MinValue Then
                SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", StartDate))
                SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", EndDate))
            End If

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = RegionId.ToLower,
                           .OfficialId = OfficialId,
                           .ScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetUsersScheduleFromRegionsNonVersionHelper(RegionIds As List(Of String), Username As String, MinDate As DateTime, MaxDate As DateTime, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        If RegionIds.Count = 0 Then Return Result

        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim CommandText As String = ""
        CommandText = <SQL>
SELECT        S.RegionId, RU.Username, S.ScheduleId, S.VersionId, S.LinkedRegionId, S.LinkedScheduleId, S.GameNumber, S.GameType, S.LeagueId, S.HomeTeam, S.HomeTeamScore, S.HomeTeamScoreExtra, S.AwayTeam, S.AwayTeamScore, S.AwayTeamScoreExtra, S.GameDate, S.ParkId, S.CrewType, S.GameStatus, S.GameComment, S.GameScore, S.OfficialRegionId, S.ScorekeeperRegionId, S.SupervisorRegionId, S.StatLinks
FROM            Schedule AS S, RegionUser As RU
WHERE S.RegionId IN ({0}) AND S.RegionId = RU.RegionId AND RU.RealUsername = @Username AND RU.IsArchived = 0
AND (S.ScheduleId IN (SELECT SV.ScheduleId FROM Schedule AS SV, SchedulePosition AS SP WHERE SV.RegionId = S.RegionId AND SV.RegionId = SP.RegionId AND SV.ScheduleId = SP.ScheduleId AND SP.OfficialId = RU.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR S.ScheduleId IN (SELECT SV.ScheduleId FROM Schedule AS SV, ScheduleFine AS SF WHERE SV.RegionId = S.RegionId AND SV.RegionId = SF.RegionId AND SV.ScheduleId = SF.ScheduleId AND SF.OfficialId = RU.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate)
OR S.ScheduleId IN (SELECT SV.ScheduleId FROM Schedule AS SV, ScheduleUserComment AS SU WHERE SV.RegionId = S.RegionId AND SV.RegionId = SU.RegionId AND SV.ScheduleId = SU.ScheduleId AND SU.OfficialId = RU.Username AND SV.GameDate >= @MinGameDate AND SV.GameDate &lt; @MaxGameDate))
ORDER BY S.RegionId, S.ScheduleId, RU.Username
                          </SQL>.Value().Replace("{0}", RegionIdParams.ToString())

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", MinDate))
            SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", MaxDate))

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = Reader.GetString(),
                           .OfficialId = Reader.GetString(),
                           .ScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetMasterScheduleFromRegionTempSingleHelper(RegionId As String, Username As String, ScheduleId As Integer, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As ScheduleTemp
        Dim Result As ScheduleTemp = Nothing
        Dim CommandText As String = ""
        CommandText = "SELECT LinkedRegionId, LinkedRegionIdModified, LinkedScheduleId, LinkedScheduleIdModified, GameNumber, GameNumberModified, GameType, GameTypeModified, LeagueId, LeagueIdModified, HomeTeam, HomeTeamModified, HomeTeamScore, HomeTeamScoreModified, HomeTeamScoreExtra, HomeTeamScoreExtraModified, AwayTeam, AwayTeamModified, AwayTeamScore, AwayTeamScoreModified, AwayTeamScoreExtra, AwayTeamScoreExtraModified, GameDate, GameDateModified, ParkId, ParkIdModified, CrewType, CrewTypeModified, GameStatus, GameStatusModified, GameComment, GameCommentModified, GameScore, GameScoreModified, OfficialRegionId, OfficialRegionIdModified, ScorekeeperRegionId, ScorekeeperRegionIdModified, SupervisorRegionId, SupervisorRegionIdModified, IsDeleted, OldScheduleId FROM ScheduleTemp WHERE RegionId = @RegionId AND UserSubmitId = @Username AND ScheduleId = @ScheduleId"

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            SqlCommand.Parameters.Add(New SqlParameter("ScheduleId", ScheduleId))

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result = New ScheduleTemp With {
                           .RegionId = RegionId.ToLower,
                           .ScheduleId = ScheduleId,
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedRegionIdModified = Reader.GetBoolean(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .LinkedScheduleIdModified = Reader.GetBoolean(),
                           .GameNumber = Reader.GetString(),
                           .GameNumberModified = Reader.GetBoolean(),
                           .GameType = Reader.GetString(),
                           .GameTypeModified = Reader.GetBoolean(),
                           .LeagueId = Reader.GetString(),
                           .LeagueIdModified = Reader.GetBoolean(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamModified = Reader.GetBoolean(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreModified = Reader.GetBoolean(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .HomeTeamScoreExtraModified = Reader.GetBoolean(),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamModified = Reader.GetBoolean(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreModified = Reader.GetBoolean(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeamScoreExtraModified = Reader.GetBoolean(),
                           .GameDate = Reader.GetDateTime(),
                           .GameDateModified = Reader.GetBoolean(),
                           .ParkId = Reader.GetString(),
                           .ParkIdModified = Reader.GetBoolean(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .CrewTypeModified = Reader.GetBoolean(),
                           .GameStatus = Reader.GetString(),
                           .GameStatusModified = Reader.GetBoolean(),
                           .GameComment = Reader.GetString(),
                           .GameCommentModified = Reader.GetBoolean(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .GameScoreModified = Reader.GetBoolean(),
                           .OfficialRegionId = Reader.GetString(),
                           .OfficialRegionIdModified = Reader.GetBoolean(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .ScorekeeperRegionIdModified = Reader.GetBoolean(),
                           .SupervisorRegionId = Reader.GetString(),
                           .SupervisorRegionIdModified = Reader.GetBoolean(),
                           .IsDeleted = Reader.GetBoolean(),
                           .OldScheduleId = Reader.GetInt32(),
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                .ScheduleCallUps = New List(Of ScheduleCallup)
                       }

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetMasterScheduleFromRegionsTempHelper(RegionIds As List(Of String), Username As String, StartDate As Date, EndDate As Date, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of ScheduleTemp)
        Dim Result As New List(Of ScheduleTemp)

        If RegionIds.Count = 0 Then Return Result

        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim CommandText As String = ""
        If StartDate = Date.MinValue Then
            CommandText = "SELECT RegionId, ScheduleId, LinkedRegionId, LinkedRegionIdModified, LinkedScheduleId, LinkedScheduleIdModified, GameNumber, GameNumberModified, GameType, GameTypeModified, LeagueId, LeagueIdModified, HomeTeam, HomeTeamModified, HomeTeamScore, HomeTeamScoreModified, HomeTeamScoreExtra, HomeTeamScoreExtraModified, AwayTeam, AwayTeamModified, AwayTeamScore, AwayTeamScoreModified, AwayTeamScoreExtra, AwayTeamScoreExtraModified, GameDate, GameDateModified, ParkId, ParkIdModified, CrewType, CrewTypeModified, GameStatus, GameStatusModified, GameComment, GameCommentModified, GameScore, GameScoreModified, OfficialRegionId, OfficialRegionIdModified, ScorekeeperRegionId, ScorekeeperRegionIdModified, SupervisorRegionId, SupervisorRegionIdModified, IsDeleted, OldScheduleId FROM ScheduleTemp WHERE RegionId IN ({0}) AND UserSubmitId = @Username ORDER BY RegionId, ScheduleId, LinkedRegionId".Replace("{0}", RegionIdParams.ToString())
        Else
            CommandText = "SELECT RegionId, ScheduleId, LinkedRegionId, LinkedRegionIdModified, LinkedScheduleId, LinkedScheduleIdModified, GameNumber, GameNumberModified, GameType, GameTypeModified, LeagueId, LeagueIdModified, HomeTeam, HomeTeamModified, HomeTeamScore, HomeTeamScoreModified, HomeTeamScoreExtra, HomeTeamScoreExtraModified, AwayTeam, AwayTeamModified, AwayTeamScore, AwayTeamScoreModified, AwayTeamScoreExtra, AwayTeamScoreExtraModified, GameDate, GameDateModified, ParkId, ParkIdModified, CrewType, CrewTypeModified, GameStatus, GameStatusModified, GameComment, GameCommentModified, GameScore, GameScoreModified, OfficialRegionId, OfficialRegionIdModified, ScorekeeperRegionId, ScorekeeperRegionIdModified, SupervisorRegionId, SupervisorRegionIdModified, IsDeleted, OldScheduleId FROM ScheduleTemp WHERE RegionId IN ({0}) AND UserSubmitId = @Username AND GameDate >= @MinGameDate AND GameDate < @MaxGameDate ORDER BY RegionId, ScheduleId, LinkedRegionId".Replace("{0}", RegionIdParams.ToString())
        End If

        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            If StartDate <> Date.MinValue Then
                SqlCommand.Parameters.Add(New SqlParameter("MinGameDate", StartDate))
                SqlCommand.Parameters.Add(New SqlParameter("MaxGameDate", EndDate))
            End If

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New ScheduleTemp With {
                           .RegionId = Reader.GetString(),
                           .ScheduleId = Reader.GetInt32(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedRegionIdModified = Reader.GetBoolean(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .LinkedScheduleIdModified = Reader.GetBoolean(),
                           .GameNumber = Reader.GetString(),
                           .GameNumberModified = Reader.GetBoolean(),
                           .GameType = Reader.GetString(),
                           .GameTypeModified = Reader.GetBoolean(),
                           .LeagueId = Reader.GetString(),
                           .LeagueIdModified = Reader.GetBoolean(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamModified = Reader.GetBoolean(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreModified = Reader.GetBoolean(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .HomeTeamScoreExtraModified = Reader.GetBoolean(),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamModified = Reader.GetBoolean(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreModified = Reader.GetBoolean(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeamScoreExtraModified = Reader.GetBoolean(),
                           .GameDate = Reader.GetDateTime(),
                           .GameDateModified = Reader.GetBoolean(),
                           .ParkId = Reader.GetString(),
                           .ParkIdModified = Reader.GetBoolean(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .CrewTypeModified = Reader.GetBoolean(),
                           .GameStatus = Reader.GetString(),
                           .GameStatusModified = Reader.GetBoolean(),
                           .GameComment = Reader.GetString(),
                           .GameCommentModified = Reader.GetBoolean(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .GameScoreModified = Reader.GetBoolean(),
                           .OfficialRegionId = Reader.GetString(),
                           .OfficialRegionIdModified = Reader.GetBoolean(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .ScorekeeperRegionIdModified = Reader.GetBoolean(),
                           .SupervisorRegionId = Reader.GetString(),
                           .SupervisorRegionIdModified = Reader.GetBoolean(),
                           .IsDeleted = Reader.GetBoolean(),
                           .OldScheduleId = Reader.GetInt32(),
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup)
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetSingleGameHelper(RegionId As String, ScheduleId As Integer, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As Schedule
        Dim Result As Schedule = Nothing

        Dim CommandText As String = "SELECT ScheduleId, LinkedRegionId, LinkedScheduleId, VersionId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, StatLinks FROM Schedule WHERE RegionId = @RegionId AND ScheduleId = @ScheduleId"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("ScheduleId", ScheduleId))


            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result = New Schedule With {
                           .RegionId = RegionId,
                           .ScheduleId = Reader.GetInt32(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .IsDeleted = False,
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .StatLinks = Reader.GetString()
                       }

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetLinkedGameHelper(LinkedRegionId As String, LinkedScheduleId As Integer, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        Dim CommandText As String = "SELECT RegionId, ScheduleId, LinkedRegionId, LinkedScheduleId, VersionId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, StatLinks FROM Schedule WHERE LinkedRegionId = @LinkedRegionId AND LinkedScheduleId = @LinkedScheduleId"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("LinkedRegionId", LinkedRegionId))
            SqlCommand.Parameters.Add(New SqlParameter("LinkedScheduleId", LinkedScheduleId))


            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = Reader.GetString(),
                           .ScheduleId = Reader.GetInt32(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .IsDeleted = False,
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Function GetGameCommmentWithScore(RegionUsers As List(Of RegionUser)) As String
        If GameScore IsNot Nothing Then
            GameScore.CleanScore
        End If

        If GameScore Is Nothing OrElse GameScore.Score Is Nothing OrElse GameScore.Score.Count = 0 Then
            Return GameComment
        End If

        Return GameScore.ScoreToStringLong(Me, RegionUsers) & VbCrlf & VbCrlf & GameComment
    End Function

    Public Shared Function GetGameListHelper(GameList As List(Of Tuple(Of String, Integer)), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As New List(Of Schedule)

        If GameList.Count = 0 Then Return Result

        Dim CommandText As String = "SELECT RegionId, ScheduleId, LinkedRegionId, LinkedScheduleId, VersionId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, StatLinks FROM Schedule WHERE {0}"

        Dim WhereClause As New StringBuilder()
        For I As Integer = 0 To GameList.Count - 1
            If I <> 0 Then WhereClause.Append(" OR ")
            WhereClause.Append("(RegionId = @RegionId{0} AND ScheduleId = @ScheduleId{0})".Replace("{0}", I + 1))
        Next

        Using SqlCommand As New SqlCommand(CommandText.Replace("{0}", WhereClause.ToString()), SQLConnection, SQLTransaction)
            For I As Integer = 0 To GameList.Count - 1
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & (I + 1), GameList(I).Item1))
                SqlCommand.Parameters.Add(New SqlParameter("ScheduleId" & (I + 1), GameList(I).Item2))
            Next

            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(New Schedule With {
                           .RegionId = Reader.GetString(),
                           .ScheduleId = Reader.GetInt32(),
                           .LinkedRegionId = Reader.GetString(),
                           .LinkedScheduleId = Reader.GetInt32(),
                           .VersionId = Reader.GetInt32(),
                           .GameNumber = Reader.GetString(),
                           .GameType = Reader.GetString(),
                           .LeagueId = Reader.GetString(),
                           .HomeTeam = Reader.GetString(),
                           .HomeTeamScore = Reader.GetString(),
                           .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .AwayTeam = Reader.GetString(),
                           .AwayTeamScore = Reader.GetString(),
                           .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                           .GameDate = Reader.GetDateTime(),
                           .ParkId = Reader.GetString(),
                           .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                           .GameStatus = Reader.GetString(),
                           .GameComment = Reader.GetString(),
                           .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                           .OfficialRegionId = Reader.GetString(),
                           .ScorekeeperRegionId = Reader.GetString(),
                           .SupervisorRegionId = Reader.GetString(),
                           .IsDeleted = False,
                           .SchedulePositions = New List(Of SchedulePosition),
                           .ScheduleFines = New List(Of ScheduleFine),
                           .ScheduleUserComments = New List(Of ScheduleUserComment),
                           .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                           .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                           .ScheduleCallUps = New List(Of ScheduleCallup),
                           .StatLinks = Reader.GetString()
                       })

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function GetSingleLinkedGameHelper(RegionId As String, LinkedRegionId As String, LinkedScheduleId As Integer, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As Schedule
        Dim Result As Schedule = Nothing

        Dim CommandText As String = "SELECT ScheduleId, LinkedRegionId, LinkedScheduleId, VersionId, GameNumber, GameType, LeagueId, HomeTeam, HomeTeamScore, HomeTeamScoreExtra, AwayTeam, AwayTeamScore, AwayTeamScoreExtra, GameDate, ParkId, CrewType, GameStatus, GameComment, GameScore, OfficialRegionId, ScorekeeperRegionId, SupervisorRegionId, StatLinks FROM Schedule WHERE RegionId = @RegionId AND LinkedRegionId = @LinkedRegionId AND LinkedScheduleId = @LinkedScheduleId"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", LinkedRegionId))
            SqlCommand.Parameters.Add(New SqlParameter("LinkedRegionId", LinkedRegionId))
            SqlCommand.Parameters.Add(New SqlParameter("LinkedScheduleId", LinkedScheduleId))


            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result = New Schedule With {
                    .RegionId = RegionId,
                    .ScheduleId = Reader.GetInt32(),
                    .LinkedRegionId = Reader.GetString(),
                    .LinkedScheduleId = Reader.GetInt32(),
                    .VersionId = Reader.GetInt32(),
                    .GameNumber = Reader.GetString(),
                    .GameType = Reader.GetString(),
                    .LeagueId = Reader.GetString(),
                    .HomeTeam = Reader.GetString(),
                    .HomeTeamScore = Reader.GetString(),
                    .HomeTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                    .AwayTeam = Reader.GetString(),
                    .AwayTeamScore = Reader.GetString(),
                    .AwayTeamScoreExtra = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString()),
                    .GameDate = Reader.GetDateTime(),
                    .ParkId = Reader.GetString(),
                    .CrewType = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.GetString().ToLower),
                    .GameStatus = Reader.GetString(),
                    .GameComment = Reader.GetString(),
                    .GameScore = GameScore.GameScoreFromString(Reader.GetString()),
                    .OfficialRegionId = Reader.GetString(),
                    .ScorekeeperRegionId = Reader.GetString(),
                    .SupervisorRegionId = Reader.GetString(),
                    .IsDeleted = False,
                    .SchedulePositions = New List(Of SchedulePosition),
                    .ScheduleFines = New List(Of ScheduleFine),
                    .ScheduleUserComments = New List(Of ScheduleUserComment),
                    .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
                    .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
                    .ScheduleCallUps = New List(Of ScheduleCallup),
                    .StatLinks = Reader.GetString()
                }

            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function RequiredGameNotificationMinusGameStatus(NewSchedule As Schedule, OldSchedule As Schedule) As Boolean
        If NewSchedule Is Nothing AndAlso OldSchedule IsNot Nothing Then
            Return True
        ElseIf NewSchedule IsNot Nothing AndAlso OldSchedule Is Nothing Then
            Return True
        ElseIf NewSchedule Is Nothing AndAlso OldSchedule Is Nothing Then
            Return False
        ElseIf NewSchedule IsNot Nothing AndAlso OldSchedule IsNot Nothing Then
            If NewSchedule.GameNumber <> OldSchedule.GameNumber Then Return True
            If NewSchedule.GameType <> OldSchedule.GameType Then Return True
            If NewSchedule.LeagueId <> OldSchedule.LeagueId Then Return True
            If NewSchedule.HomeTeam <> OldSchedule.HomeTeam Then Return True
            If NewSchedule.AwayTeam <> OldSchedule.AwayTeam Then Return True
            If NewSchedule.GameDate <> OldSchedule.GameDate Then Return True
            If NewSchedule.ParkId <> OldSchedule.ParkId Then Return True
            If NewSchedule.CrewType.Count <> OldSchedule.CrewType.Count Then Return True
            For Each KeyValue In NewSchedule.CrewType
                If Not OldSchedule.CrewType.ContainsKey(KeyValue.Key) Then Return True
                If Not OldSchedule.CrewType(KeyValue.Key).ToLower = KeyValue.Value.ToLower Then Return True
            Next

            Dim NewScheduleCommentTeamComment = ""
            Dim OldScheduleCommentTeamComment = ""

            Dim NewScheduleCommentTeam = NewSchedule.ScheduleCommentTeams.Find(Function(SCT) SCT.LinkedRegionId = NewSchedule.RegionId)
            Dim OldScheduleCommentTeam = OldSchedule.ScheduleCommentTeams.Find(Function(SCT) SCT.LinkedRegionId = OldSchedule.RegionId)

            If NewScheduleCommentTeam IsNot Nothing Then NewScheduleCommentTeamComment = NewScheduleCommentTeam.Comment
            If OldScheduleCommentTeam IsNot Nothing Then OldScheduleCommentTeamComment = OldScheduleCommentTeam.Comment

            If NewScheduleCommentTeamComment <> OldScheduleCommentTeamComment Then Return True

            If NewSchedule.IsDeleted <> OldSchedule.IsDeleted Then Return True
        End If
        Return False
    End Function

    Public Shared Function RequiredGameNotification(NewSchedule As Schedule, OldSchedule As Schedule) As Boolean
        If NewSchedule Is Nothing AndAlso OldSchedule IsNot Nothing Then
            Return True
        ElseIf NewSchedule IsNot Nothing AndAlso OldSchedule Is Nothing Then
            Return True
        ElseIf NewSchedule Is Nothing AndAlso OldSchedule Is Nothing Then
            Return False
        ElseIf NewSchedule IsNot Nothing AndAlso OldSchedule IsNot Nothing Then
            If NewSchedule.GameNumber <> OldSchedule.GameNumber Then Return True
            If NewSchedule.GameType <> OldSchedule.GameType Then Return True
            If NewSchedule.LeagueId <> OldSchedule.LeagueId Then Return True
            If NewSchedule.HomeTeam <> OldSchedule.HomeTeam Then Return True
            If NewSchedule.AwayTeam <> OldSchedule.AwayTeam Then Return True
            If NewSchedule.GameDate <> OldSchedule.GameDate Then Return True
            If NewSchedule.ParkId <> OldSchedule.ParkId Then Return True

            If NewSchedule.CrewType.Count <> OldSchedule.CrewType.Count Then Return True
            For Each KeyValue In NewSchedule.CrewType
                If Not OldSchedule.CrewType.ContainsKey(KeyValue.Key) Then Return True
                If Not OldSchedule.CrewType(KeyValue.Key).ToLower = KeyValue.Value.ToLower Then Return True
            Next
            If NewSchedule.GameStatus <> OldSchedule.GameStatus Then Return True
            If NewSchedule.GameComment <> OldSchedule.GameComment Then Return True
            If GameScore.IsDifferentScoreOnly(NewSchedule.GameScore, OldSchedule.GameScore) Then Return True
            If NewSchedule.OfficialRegionId <> OldSchedule.OfficialRegionId Then Return True
            If NewSchedule.ScorekeeperRegionId <> OldSchedule.ScorekeeperRegionId Then Return True
            If NewSchedule.SupervisorRegionId <> OldSchedule.SupervisorRegionId Then Return True

            Dim NewScheduleCommentTeamComment = ""
            Dim OldScheduleCommentTeamComment = ""

            Dim NewScheduleCommentTeam = NewSchedule.ScheduleCommentTeams.Find(Function(SCT) SCT.LinkedRegionId = NewSchedule.RegionId)
            Dim OldScheduleCommentTeam = OldSchedule.ScheduleCommentTeams.Find(Function(SCT) SCT.LinkedRegionId = OldSchedule.RegionId)

            If NewScheduleCommentTeam IsNot Nothing Then NewScheduleCommentTeamComment = NewScheduleCommentTeam.Comment
            If OldScheduleCommentTeam IsNot Nothing Then OldScheduleCommentTeamComment = OldScheduleCommentTeam.Comment

            If NewScheduleCommentTeamComment <> OldScheduleCommentTeamComment Then Return True

            If NewSchedule.IsDeleted <> OldSchedule.IsDeleted Then Return True
        End If
        Return False
    End Function

    Public Shared Function RequiredGameNotificationPosition(NewSchedule As Schedule, OldSchedule As Schedule, OfficialId As String) As String
        Dim NewUserPosition As String = ""
        Dim OldUserPosition As String = ""

        If NewSchedule IsNot Nothing Then
            Dim SchedulePosition = NewSchedule.SchedulePositions.Find(Function(V1) V1.OfficialId.ToLower = OfficialId.ToLower)
            If SchedulePosition IsNot Nothing Then
                NewUserPosition = SchedulePosition.PositionId
            End If
        End If

        If OldSchedule IsNot Nothing Then
            Dim SchedulePosition = OldSchedule.SchedulePositions.Find(Function(V1) V1.OfficialId.ToLower = OfficialId.ToLower)
            If SchedulePosition IsNot Nothing Then
                OldUserPosition = SchedulePosition.PositionId
            End If
        End If

        If NewUserPosition = "" AndAlso OldUserPosition <> "" Then
            Return "Removed"
        ElseIf NewUserPosition <> "" AndAlso OldUserPosition = "" Then
            Return "Added"
        ElseIf NewUserPosition <> OldUserPosition Then
            Return "ChangedPosition"
        ElseIf NewUserPosition = OldUserPosition Then
            Return "SamePosition"
        End If
        Return "SamePosition"
    End Function

    Public Shared Function RequiredGameNotificationFine(NewSchedule As Schedule, OldSchedule As Schedule, OfficialId As String) As Boolean

        Dim NewFine As ScheduleFine = Nothing
        Dim OldFine As ScheduleFine = Nothing

        If NewSchedule IsNot Nothing AndAlso NewSchedule.ScheduleFines IsNot Nothing Then
            NewFine = NewSchedule.ScheduleFines.Find(Function(V1) V1.OfficialId.ToLower = OfficialId.ToLower)
        End If

        If OldSchedule IsNot Nothing AndAlso OldSchedule.ScheduleFines IsNot Nothing Then
            OldFine = OldSchedule.ScheduleFines.Find(Function(V1) V1.OfficialId.ToLower = OfficialId.ToLower)
        End If

        If NewFine Is Nothing AndAlso OldFine Is Nothing Then Return False
        If (NewFine Is Nothing) <> (OldFine Is Nothing) Then Return True
        If NewFine.Amount <> OldFine.Amount Then Return True
        If NewFine.Comment <> OldFine.Comment Then Return True
        Return False
    End Function

    Public Shared Function GetInvolvedOfficials(NewSchedule As Schedule, OldSchedule As Schedule, Officials As List(Of RegionUser)) As List(Of RegionUser)
        Dim Result As New List(Of String)
        Dim RegionId As String = ""


        If NewSchedule IsNot Nothing Then
            RegionId = NewSchedule.RegionId
            For Each SchedulePosition In NewSchedule.SchedulePositions
                If Not Result.Contains(SchedulePosition.OfficialId.ToLower) Then
                    Result.Add(SchedulePosition.OfficialId.ToLower)
                End If
            Next

            For Each ScheduleFine In NewSchedule.ScheduleFines
                If Not Result.Contains(ScheduleFine.OfficialId.ToLower) Then
                    Result.Add(ScheduleFine.OfficialId.ToLower)
                End If
            Next

            For Each ScheduleUserComment In NewSchedule.ScheduleUserComments
                If Not Result.Contains(ScheduleUserComment.OfficialId.ToLower) Then
                    Result.Add(ScheduleUserComment.OfficialId.ToLower)
                End If
            Next

            For Each ScheduleUserCommentTeam In NewSchedule.ScheduleUserCommentTeams
                If Not Result.Contains(ScheduleUserCommentTeam.OfficialId.ToLower) Then
                    Result.Add(ScheduleUserCommentTeam.OfficialId.ToLower)
                End If
            Next
        End If

        If OldSchedule IsNot Nothing Then
            RegionId = OldSchedule.RegionId
            For Each SchedulePosition In OldSchedule.SchedulePositions
                If Not Result.Contains(SchedulePosition.OfficialId.ToLower) Then
                    Result.Add(SchedulePosition.OfficialId.ToLower)
                End If
            Next

            For Each ScheduleFine In OldSchedule.ScheduleFines
                If Not Result.Contains(ScheduleFine.OfficialId.ToLower) Then
                    Result.Add(ScheduleFine.OfficialId.ToLower)
                End If
            Next

            For Each ScheduleUserComment In OldSchedule.ScheduleUserComments
                If Not Result.Contains(ScheduleUserComment.OfficialId.ToLower) Then
                    Result.Add(ScheduleUserComment.OfficialId.ToLower)
                End If
            Next

            For Each ScheduleUserCommentTeam In OldSchedule.ScheduleUserCommentTeams
                If Not Result.Contains(ScheduleUserCommentTeam.OfficialId.ToLower) Then
                    Result.Add(ScheduleUserCommentTeam.OfficialId.ToLower)
                End If
            Next
        End If

        Dim RegionUsers As New List(Of RegionUser)
        For Each OfficialIdd In Result
            Dim RUserResult = Officials.Find(Function(V1) V1.RegionId = RegionId AndAlso V1.Username.ToLower = OfficialIdd.ToLower)
            If RUserResult IsNot Nothing Then
                RegionUsers.Add(RUserResult)
            Else
                RegionUsers.Add(New RegionUser With {.RegionId = RegionId, .Username = OfficialIdd})
            End If
        Next

        Return RegionUsers
    End Function

    Public Shared Function LinkSchedule(AssignorId As String, LinkedSchedule As List(Of Schedule)) As Object
        Dim Region As RegionProperties = Nothing
        Dim ScheduleId As Integer = -1

        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Dim UniqueRegionIds As New List(Of String)
                For Each LinkedScheduleItem In LinkedSchedule
                    If Not UniqueRegionIds.Contains(LinkedScheduleItem.RegionId) Then
                        UniqueRegionIds.Add(LinkedScheduleItem.RegionId)
                    End If
                Next

                Dim OfficialRegions = OfficialRegion.GetOfficialRegionsInRegionsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)
                Dim AssignorRegionIdsExec = RegionUser.GetAllMyRegionsIdExecs(AssignorId, SqlConnection, SqlTransaction)

                Dim LinkedLeagues = RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(
                    RegionLeague.GetRegionsLeaguesHelper(UniqueRegionIds, SqlConnection, SqlTransaction),
                    New List(Of RegionLeaguePay)
                )

                If Not AssignorId = "superadmin" Then
                    For Each LinkedScheduleItem In LinkedSchedule
                        If Not AssignorRegionIdsExec.Contains(LinkedScheduleItem.RegionId) Then
                            Return New ErrorObject("InvalidPermissions")
                        End If
                        If Not LinkedLeagues.Any(Function(L) L.RegionId = LinkedScheduleItem.RegionId AndAlso L.RealLeagueId <> "" AndAlso L.RealLeagueId = LinkedScheduleItem.LinkedRegionId) Then
                            Return New ErrorObject("InvalidPermissions")
                        End If
                    Next
                End If

                Dim LinkableSchedule = LinkedSchedule

                Dim MinDate = Data.SqlTypes.SqlDateTime.MaxValue
                Dim MaxDate = Data.SqlTypes.SqlDateTime.MinValue

                Dim RequiredAssignorIds As New List(Of String)

                For Each Item In LinkableSchedule
                    If Item.GameDate < MinDate Then MinDate = Item.GameDate
                    If Item.GameDate > MaxDate Then
                        MaxDate = Item.GameDate.AddMinutes(1)
                    End If

                    If Item.RegionId <> "" AndAlso Not RequiredAssignorIds.Contains(Item.RegionId) Then
                        RequiredAssignorIds.Add(Item.RegionId)
                    End If

                    If Item.LinkedRegionId <> "" AndAlso Not UniqueRegionIds.Contains(Item.LinkedRegionId) Then
                        UniqueRegionIds.Add(Item.LinkedRegionId)
                    End If
                Next

                Dim Regions = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)

                Dim AssignorsSchedule = GetMasterScheduleFromRegionsNonVersionHelper(RequiredAssignorIds, MinDate, MaxDate, SqlConnection, SqlTransaction)

                AssignorsSchedule.Sort(Schedule.LinkSorter)
                LinkableSchedule.Sort(Schedule.LinkSorter)

                Dim OldNewSchedule As List(Of ScheduleTemp) = GetMasterScheduleFromRegionsTempHelper(RequiredAssignorIds, AssignorId, Date.MinValue, Date.MaxValue, SqlConnection, SqlTransaction)
                SchedulePosition.GetMasterSchedulePositionFromRegionsTempHelper(RequiredAssignorIds, AssignorId, Date.MinValue, Date.MaxValue, OldNewSchedule, SqlConnection, SqlTransaction)
                ScheduleFine.GetMasterScheduleFineFromRegionsTempHelper(RequiredAssignorIds, AssignorId, Date.MinValue, Date.MaxValue, OldNewSchedule, SqlConnection, SqlTransaction)
                ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsTempHelper(RequiredAssignorIds, AssignorId, Date.MinValue, Date.MaxValue, OldNewSchedule, SqlConnection, SqlTransaction)
                ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsTempHelper(RequiredAssignorIds, AssignorId, Date.MinValue, Date.MaxValue, OldNewSchedule, SqlConnection, SqlTransaction)

                Dim CurrentScheduleIds = UmpireAssignor.ScheduleId.GetScheduleIdsFromRegionsHelper(RequiredAssignorIds, SqlConnection, SqlTransaction)

                Dim ScheduleTempSubmitteds As New List(Of ScheduleTempSubmitted)
                For Each UniqueRegionIdsWithLeague In RequiredAssignorIds
                    ScheduleTempSubmitteds.Add(New ScheduleTempSubmitted With {
                        .RegionId = UniqueRegionIdsWithLeague,
                        .IsSubmitted = True,
                        .UserSubmitId = AssignorId
                    })
                Next

                If True Then
                    Dim CommandSB As New StringBuilder()
                    CommandSB.Append("DELETE FROM ScheduleTempSubmitted WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId;")
                    Using SQLCommand2 As New SqlCommand(CommandSB.ToString().Replace("{0}", PublicCode.CreateParamStr("RegionId", RequiredAssignorIds)), SqlConnection, SqlTransaction)
                        PublicCode.CreateParamsSQLCommand("RegionId", RequiredAssignorIds, SQLCommand2)
                        SQLCommand2.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                        SQLCommand2.ExecuteNonQuery()
                    End Using
                    ScheduleTempSubmitted.BulkInsert(ScheduleTempSubmitteds, SqlConnection, SqlTransaction)
                End If

                Dim InsertScheduleTemps As New List(Of ScheduleTemp)
                Dim InsertSchedulePositionsTemp As New List(Of SchedulePositionFull)
                Dim InsertScheduleFinesTemp As New List(Of ScheduleFineFull)
                Dim InsertScheduleUserCommentsTemp As New List(Of ScheduleUserCommentFull)
                Dim InsertScheduleUserCommentTeamsTemp As New List(Of ScheduleUserCommentTeamFull)
                Dim InsertScheduleCommentTeamsTemp As New List(Of ScheduleCommentTeamFull)
                Dim InsertScheduleCallupsTemp As New List(Of ScheduleCallupFull)
                Dim DeleteScheduleTemps As New List(Of ScheduleTemp)
                Dim DeleteScheduleIgnores As New List(Of ScheduleIgnore)

                PublicCode.ProcessListPairs(LinkableSchedule, AssignorsSchedule, AddressOf Schedule.LinkComparer,
                                                Sub(LS)
                                                    If Not LS.IsDeleted Then
                                                        Dim OldNewScheduleItem = OldNewSchedule.Find(Function(ST) ST.LinkedRegionId = LS.LinkedRegionId AndAlso ST.LinkedScheduleId = LS.LinkedScheduleId)

                                                        DeleteScheduleIgnores.Add(New ScheduleIgnore With {
                                                            .RegionId = LS.RegionId,
                                                            .LinkedRegionId = LS.LinkedRegionId,
                                                            .ScheduleId = LS.ScheduleId
                                                        })

                                                        Dim TRegion = Regions.Find(Function(R) R.RegionId = LS.RegionId)

                                                        MergeTempDataRefereeToNewData(TRegion, LS, Nothing, OldNewScheduleItem)

                                                        UpsertToTempHelper(AssignorId, TRegion, Regions, LS, Nothing, OldNewScheduleItem, CurrentScheduleIds(LS.RegionId), InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp, DeleteScheduleTemps)
                                                    Else
                                                        Dim OldNewScheduleItem = OldNewSchedule.Find(Function(ST) ST.LinkedRegionId = LS.LinkedRegionId AndAlso ST.LinkedScheduleId = LS.LinkedScheduleId)

                                                        If OldNewScheduleItem IsNot Nothing Then
                                                            Dim TRegion = Regions.Find(Function(R) R.RegionId = LS.RegionId)
                                                            UpsertToTempHelper(AssignorId, TRegion, Regions, Nothing, Nothing, OldNewScheduleItem, CurrentScheduleIds(LS.RegionId), InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp, DeleteScheduleTemps)
                                                        End If
                                                    End If
                                                End Sub,
                                                Sub(ASc)
                                                End Sub,
                                            Sub(LS, ASc)
                                                LS.ScheduleId = ASc.ScheduleId

                                                DeleteScheduleIgnores.Add(New ScheduleIgnore With {
                                                    .RegionId = LS.RegionId,
                                                    .LinkedRegionId = LS.LinkedRegionId,
                                                    .ScheduleId = LS.ScheduleId
                                                })

                                                If LS.IsDeleted Then LS = Nothing

                                                Dim OldNewScheduleItem = OldNewSchedule.Find(Function(ST) ST.LinkedRegionId = ASc.LinkedRegionId AndAlso ST.LinkedScheduleId = ASc.LinkedScheduleId)

                                                Dim TRegion = Regions.Find(Function(R) R.RegionId = ASc.RegionId)

                                                MergeTempDataRefereeToNewData(TRegion, LS, ASc, OldNewScheduleItem)

                                                UpsertToTempHelper(AssignorId, TRegion, Regions, LS, ASc, OldNewScheduleItem, CurrentScheduleIds(ASc.RegionId), InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp, DeleteScheduleTemps)
                                            End Sub)


                ScheduleIgnore.BulkDelete(DeleteScheduleIgnores, SqlConnection, SqlTransaction)
                ScheduleTemp.BulkDelete(DeleteScheduleTemps, AssignorId, SqlConnection, SqlTransaction)
                ScheduleTemp.BulkInsert(InsertScheduleTemps, AssignorId, SqlConnection, SqlTransaction)
                SchedulePositionFull.BulkInsert(InsertSchedulePositionsTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleFineFull.BulkInsert(InsertScheduleFinesTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleUserCommentFull.BulkInsert(InsertScheduleUserCommentsTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleUserCommentTeamFull.BulkInsert(InsertScheduleUserCommentTeamsTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleCommentTeamFull.BulkInsert(InsertScheduleCommentTeamsTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleCallupFull.BulkInsert(InsertScheduleCallupsTemp, AssignorId, SqlConnection, SqlTransaction)

                UmpireAssignor.ScheduleId.UpsertHelper(CurrentScheduleIds.Values.ToList(), SqlConnection, SqlTransaction)

                SqlTransaction.Commit()
            End Using
        End Using

        Return New With {
            .Success = True,
            .Schedule = LinkedSchedule
        }
        'Catch E As Exception
        '    Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Private Shared Function ConvertToLinkScheduleItem(ScheduleItem As Schedule, OfficialRegionId As String, LinkedLeagues As List(Of RegionLeaguePayContracted)) As Schedule
        Dim Result As Schedule = ScheduleItem.CloneItem

        Dim RegionLeagueId As String = ScheduleItem.RegionId

        Result.LinkedRegionId = ScheduleItem.RegionId
        Result.LinkedScheduleId = ScheduleItem.ScheduleId

        Result.RegionId = OfficialRegionId

        Dim League = LinkedLeagues.Find(Function(L) L.RegionId = OfficialRegionId AndAlso L.RealLeagueId = Result.LinkedRegionId)

        If League IsNot Nothing Then
            Result.LeagueId = League.LeagueId
        Else
            Result.LeagueId = ScheduleItem.LeagueId
        End If

        Return Result
    End Function

    Private Shared Function ConvertToLinkScheduleItem(ScheduleItem As Schedule, OfficialRegionId As String, LinkedLeagues As List(Of RegionLeague)) As Schedule
        Dim Result As Schedule = ScheduleItem.CloneItem

        Dim RegionLeagueId As String = ScheduleItem.RegionId

        Result.LinkedRegionId = ScheduleItem.RegionId
        Result.LinkedScheduleId = ScheduleItem.ScheduleId

        Result.RegionId = OfficialRegionId

        Dim League = LinkedLeagues.Find(Function(L) L.RegionId = OfficialRegionId AndAlso L.RealLeagueId = Result.LinkedRegionId)

        Result.LeagueId = League.LeagueId

        Return Result
    End Function

    Private Shared Function ConvertBackToScheduleItemLeague(ScheduleItem As Schedule, LeagueRegion As Region) As Schedule
        Dim Result As Schedule = ScheduleItem.CloneItem

        Result.RegionId = Result.LinkedRegionId
        Result.LinkedRegionId = ""
        Result.LinkedScheduleId = ""
        Result.LeagueId = LeagueRegion.RegionId

        Return Result
    End Function


    Public Function CloneItem() As Schedule
        Dim Result = New Schedule With {
            .RegionId = Me.RegionId,
            .ScheduleId = Me.ScheduleId,
            .VersionId = Me.VersionId,
            .LinkedRegionId = Me.LinkedRegionId,
            .LinkedScheduleId = Me.LinkedScheduleId,
            .GameNumber = Me.GameNumber,
            .GameType = Me.GameType,
            .LeagueId = Me.LeagueId,
            .HomeTeam = Me.HomeTeam,
            .HomeTeamScore = Me.HomeTeamScore,
            .HomeTeamScoreExtra = New Dictionary(Of String, String),
            .AwayTeam = Me.AwayTeam,
            .AwayTeamScore = Me.AwayTeamScore,
            .AwayTeamScoreExtra = New Dictionary(Of String, String),
            .GameDate = Me.GameDate.AddDays(0),
            .ParkId = Me.ParkId,
            .CrewType = New Dictionary(Of String, String),
            .SchedulePositions = New List(Of SchedulePosition),
            .ScheduleFines = New List(Of ScheduleFine),
            .ScheduleUserComments = New List(Of ScheduleUserComment),
            .ScheduleUserCommentTeams = New List(Of ScheduleUserCommentTeam),
            .ScheduleCommentTeams = New List(Of ScheduleCommentTeam),
            .ScheduleCallUps = New List(Of ScheduleCallup),
            .GameStatus = Me.GameStatus,
            .GameComment = Me.GameComment,
            .OfficialRegionId = Me.OfficialRegionId,
            .ScorekeeperRegionId = Me.ScorekeeperRegionId,
            .SupervisorRegionId = Me.SupervisorRegionId,
            .IsDeleted = Me.IsDeleted,
            .StatLinks = Me.StatLinks,
            .OfficialId = Me.OfficialId,
            .DateAdded = Me.DateAdded,
            .DateNotified = Me.DateNotified
        }

        If Me.GameScore IsNot Nothing Then
            Result.GameScore = Me.GameScore.Clone()
        End If

        If Me.HomeTeamScoreExtra IsNot Nothing Then
            For Each HomeTeamScoreExtraItem In Me.HomeTeamScoreExtra
                Result.HomeTeamScoreExtra.Add(HomeTeamScoreExtraItem.Key, HomeTeamScoreExtraItem.Value)
            Next
        End if

        If Me.AwayTeamScoreExtra IsNot Nothing Then
            For Each AwayTeamScoreExtraItem In Me.AwayTeamScoreExtra
                Result.AwayTeamScoreExtra.Add(AwayTeamScoreExtraItem.Key, AwayTeamScoreExtraItem.Value)
            Next
        End If

        If Me.CrewType IsNot Nothing Then
            For Each CrewTypeKeyValue In Me.CrewType
                Result.CrewType.Add(CrewTypeKeyValue.Key, CrewTypeKeyValue.Value)
            Next
        End If

        If Me.SchedulePositions IsNot Nothing Then
            For Each SchedulePosition In Me.SchedulePositions
                Result.SchedulePositions.Add(New SchedulePosition With {
                                             .RegionId = Me.RegionId,
                                             .PositionId = SchedulePosition.PositionId,
                                             .OfficialId = SchedulePosition.OfficialId
                                         })
            Next
        End If

        If Me.ScheduleFines IsNot Nothing Then
            For Each ScheduleFine In Me.ScheduleFines
                Result.ScheduleFines.Add(New ScheduleFine With {
                                             .RegionId = Me.RegionId,
                                             .OfficialId = ScheduleFine.OfficialId,
                                             .Amount = ScheduleFine.Amount,
                                             .Comment = ScheduleFine.Comment
                                         })
            Next
        End If

        If Me.ScheduleUserComments IsNot Nothing Then
            For Each ScheduleUserComment In Me.ScheduleUserComments
                Result.ScheduleUserComments.Add(New ScheduleUserComment With {
                                             .RegionId = Me.RegionId,
                                             .OfficialId = ScheduleUserComment.OfficialId,
                                             .Comment = ScheduleUserComment.Comment
                                         })
            Next
        End If

        If Me.ScheduleUserCommentTeams IsNot Nothing Then
            For Each ScheduleUserCommentTeam In Me.ScheduleUserCommentTeams
                Result.ScheduleUserCommentTeams.Add(New ScheduleUserCommentTeam With {
                                             .RegionId = Me.RegionId,
                                             .LinkedRegionId = ScheduleUserCommentTeam.LinkedRegionId,
                                             .OfficialId = ScheduleUserCommentTeam.OfficialId,
                                             .Comment = ScheduleUserCommentTeam.Comment
                                         })
            Next
        End If

        If Me.ScheduleCommentTeams IsNot Nothing Then
            For Each ScheduleCommentTeam In Me.ScheduleCommentTeams
                Result.ScheduleCommentTeams.Add(New ScheduleCommentTeam With {
                                             .RegionId = Me.RegionId,
                                             .LinkedRegionId = ScheduleCommentTeam.LinkedRegionId,
                                             .Comment = ScheduleCommentTeam.Comment
                                         })
            Next
        End If

        If Me.ScheduleCallUps IsNot Nothing Then
            For Each ScheduleCallUp In Me.ScheduleCallUps
                Result.ScheduleCallUps.Add(New ScheduleCallup With {
                                             .RegionId = Me.RegionId,
                                             .LinkedRegionId = ScheduleCallUp.LinkedRegionId,
                                             .Username = ScheduleCallUp.Username
                                         })
            Next
        End If

        Return Result
    End Function

    Public Shared Function IgnoreSchedule(AssignorId As String, LinkedSchedule As List(Of Schedule)) As Object
        Dim Region As Region = Nothing
        Dim ScheduleId As Integer = -1

        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Dim UniqueRegionIds As New List(Of String)
                For Each LinkedScheduleItem In LinkedSchedule
                    If Not UniqueRegionIds.Contains(LinkedScheduleItem.RegionId) Then
                        UniqueRegionIds.Add(LinkedScheduleItem.RegionId)
                    End If
                Next

                Dim OfficialRegions = OfficialRegion.GetOfficialRegionsInRegionsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)
                Dim AssignorRegionIdsExec = RegionUser.GetAllMyRegionsIdExecs(AssignorId, SqlConnection, SqlTransaction)

                Dim LinkedLeagues = RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(
                    RegionLeague.GetRegionsLeaguesHelper(UniqueRegionIds, SqlConnection, SqlTransaction),
                    New List(Of RegionLeaguePay)
                )

                For Each LinkedScheduleItem In LinkedSchedule
                    If Not AssignorRegionIdsExec.Contains(LinkedScheduleItem.RegionId) Then
                        Return New ErrorObject("InvalidPermissions")
                    End If
                    If Not LinkedLeagues.Any(Function(L) L.RegionId = LinkedScheduleItem.RegionId AndAlso L.RealLeagueId <> "" AndAlso L.RealLeagueId = LinkedScheduleItem.LinkedRegionId) Then
                        Return New ErrorObject("InvalidPermissions")
                    End If
                Next

                Dim LinkableSchedule = LinkedSchedule

                Dim RequiredAssignorIds As New List(Of String)

                For Each Item In LinkableSchedule
                    If Item.RegionId <> "" AndAlso Not RequiredAssignorIds.Contains(Item.RegionId) Then
                        RequiredAssignorIds.Add(Item.RegionId)
                    End If
                Next

                Dim Regions = UmpireAssignor.Region.GetRegionFromRegionIdsHelper(RequiredAssignorIds, SqlConnection, SqlTransaction)

                For Each Region In Regions
                    If Region.RequiresPayment Then Return New ErrorObject("RequiresPayment")
                Next

                LinkableSchedule.Sort(Schedule.LinkSorter)

                Dim SQLCommand As New SqlCommand()
                SQLCommand.Connection = SqlConnection
                SQLCommand.Transaction = SqlTransaction
                Dim CommandSB As New StringBuilder()
                Dim CommandCount As Integer = 0

                SQLCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))

                Dim SQLCommandText = <SQL>
DELETE FROM ScheduleIgnore WHERE RegionId = @RegionId{0} AND LinkedRegionId = @LinkedRegionId{0} AND ScheduleId = @ScheduleId{0};
INSERT INTO ScheduleIgnore (
    RegionId,
    LinkedRegionId,
    ScheduleId,
    VersionId,
    GameNumber,
    LeagueId,
    HomeTeam,
    HomeTeamScore,
    HomeTeamScoreExtra,
    AwayTeam,
    AwayTeamScore,
    AwayTeamScoreExtra,
    GameDate,
    ParkId,
    CrewType,
    GameStatus,
    GameComment,
    GameScore,
    GameType,
    OfficialRegionId,
    ScorekeeperRegionId,
    SupervisorRegionId,
    LinkedScheduleId,
    IsDeleted,
    DateAdded
) VALUES (
    @RegionId{0},
    @LinkedRegionId{0},
    @ScheduleId{0},
    @VersionId{0},
    @GameNumber{0},
    @LeagueId{0},
    @HomeTeam{0},
    @HomeTeamScore{0},
    @HomeTeamScoreExtra{0},
    @AwayTeam{0},
    @AwayTeamScore{0},
    @AwayTeamScoreExtra{0},
    @GameDate{0},
    @ParkId{0},
    @CrewType{0},
    @GameStatus{0},
    @GameComment{0},
    @GameScore{0},
    @GameType{0},
    @OfficialRegionId{0},
    @ScorekeeperRegionId{0},
    @SupervisorRegionId{0},
    @LinkedScheduleId{0},
    @IsDeleted{0},
    @DateAdded{0}
);
                                         </SQL>.Value.ToString()

                For Each LinkableScheduleItem In LinkableSchedule
                    CommandCount += 1

                    CommandSB.Append(String.Format(SQLCommandText, CommandCount))

                    SQLCommand.Parameters.Add(New SqlParameter("RegionId" & CommandCount, LinkableScheduleItem.RegionId))
                    SQLCommand.Parameters.Add(New SqlParameter("LinkedRegionId" & CommandCount, LinkableScheduleItem.LinkedRegionId))
                    SQLCommand.Parameters.Add(New SqlParameter("ScheduleId" & CommandCount, LinkableScheduleItem.ScheduleId))
                    SQLCommand.Parameters.Add(New SqlParameter("VersionId" & CommandCount, LinkableScheduleItem.VersionId))
                    SQLCommand.Parameters.Add(New SqlParameter("GameNumber" & CommandCount, LinkableScheduleItem.GameNumber))
                    SQLCommand.Parameters.Add(New SqlParameter("GameType" & CommandCount, LinkableScheduleItem.GameType))
                    SQLCommand.Parameters.Add(New SqlParameter("LeagueId" & CommandCount, LinkableScheduleItem.LeagueId))
                    SQLCommand.Parameters.Add(New SqlParameter("HomeTeam" & CommandCount, LinkableScheduleItem.HomeTeam))
                    SQLCommand.Parameters.Add(New SqlParameter("HomeTeamScore" & CommandCount, LinkableScheduleItem.HomeTeamScore))
                    SQLCommand.Parameters.Add(New SqlParameter("HomeTeamScoreExtra" & CommandCount, JsonConvert.SerializeObject(LinkableScheduleItem.HomeTeamScoreExtra)))
                    SQLCommand.Parameters.Add(New SqlParameter("AwayTeam" & CommandCount, LinkableScheduleItem.AwayTeam))
                    SQLCommand.Parameters.Add(New SqlParameter("AwayTeamScore" & CommandCount, LinkableScheduleItem.AwayTeamScore))
                    SQLCommand.Parameters.Add(New SqlParameter("AwayTeamScoreExtra" & CommandCount, JsonConvert.SerializeObject(LinkableScheduleItem.AwayTeamScoreExtra)))
                    SQLCommand.Parameters.Add(New SqlParameter("GameDate" & CommandCount, LinkableScheduleItem.GameDate))
                    SQLCommand.Parameters.Add(New SqlParameter("ParkId" & CommandCount, LinkableScheduleItem.ParkId))
                    SQLCommand.Parameters.Add(New SqlParameter("CrewType" & CommandCount, JsonConvert.SerializeObject(LinkableScheduleItem.CrewType)))
                    SQLCommand.Parameters.Add(New SqlParameter("GameStatus" & CommandCount, LinkableScheduleItem.GameStatus))
                    SQLCommand.Parameters.Add(New SqlParameter("GameComment" & CommandCount, LinkableScheduleItem.GameComment))
                    SQLCommand.Parameters.Add(New SqlParameter("GameScore" & CommandCount, JsonConvert.SerializeObject(LinkableScheduleItem.GameScore)))
                    SQLCommand.Parameters.Add(New SqlParameter("LinkedScheduleId" & CommandCount, LinkableScheduleItem.LinkedScheduleId))
                    SQLCommand.Parameters.Add(New SqlParameter("OfficialRegionId" & CommandCount, LinkableScheduleItem.OfficialRegionId))
                    SQLCommand.Parameters.Add(New SqlParameter("ScorekeeperRegionId" & CommandCount, LinkableScheduleItem.ScorekeeperRegionId))
                    SQLCommand.Parameters.Add(New SqlParameter("SupervisorRegionId" & CommandCount, LinkableScheduleItem.SupervisorRegionId))
                    SQLCommand.Parameters.Add(New SqlParameter("IsDeleted" & CommandCount, LinkableScheduleItem.IsDeleted))
                    SQLCommand.Parameters.Add(New SqlParameter("DateAdded" & CommandCount, DateTime.UtcNow))

                    If CommandCount > 50 Then ExecuteSQLCommand(SqlConnection, SqlTransaction, SQLCommand, CommandSB, CommandCount, "allregions", AssignorId)
                Next

                If CommandCount > 0 Then
                    SQLCommand.CommandText = CommandSB.ToString()
                    SQLCommand.ExecuteNonQuery()
                End If

                SqlTransaction.Commit()
            End Using
        End Using

        Return New With {
            .Success = True
        }
        'Catch E As Exception
        'Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Shared Function UnIgnoreSchedule(AssignorId As String, LinkedSchedule As List(Of Schedule)) As Object
        Dim Region As Region = Nothing
        Dim ScheduleId As Integer = -1

        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Dim UniqueRegionIds As New List(Of String)
                For Each LinkedScheduleItem In LinkedSchedule
                    If Not UniqueRegionIds.Contains(LinkedScheduleItem.RegionId) Then
                        UniqueRegionIds.Add(LinkedScheduleItem.RegionId)
                    End If
                Next

                Dim OfficialRegions = OfficialRegion.GetOfficialRegionsInRegionsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)
                Dim AssignorRegionIdsExec = RegionUser.GetAllMyRegionsIdExecs(AssignorId, SqlConnection, SqlTransaction)

                Dim LinkedLeagues = RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(
                    RegionLeague.GetRegionsLeaguesHelper(UniqueRegionIds, SqlConnection, SqlTransaction),
                    New List(Of RegionLeaguePay)
                )

                For Each LinkedScheduleItem In LinkedSchedule
                    If Not AssignorRegionIdsExec.Contains(LinkedScheduleItem.RegionId) Then
                        Return New ErrorObject("InvalidPermissions")
                    End If
                    If Not LinkedLeagues.Any(Function(L) L.RegionId = LinkedScheduleItem.RegionId AndAlso L.RealLeagueId <> "" AndAlso L.RealLeagueId = LinkedScheduleItem.LinkedRegionId) Then
                        Return New ErrorObject("InvalidPermissions")
                    End If
                Next

                Dim LinkableSchedule = LinkedSchedule

                Dim RequiredAssignorIds As New List(Of String)

                For Each Item In LinkableSchedule
                    Dim TRegionId = Item.OfficialRegionId
                    If TRegionId = "" Then TRegionId = Item.ScorekeeperRegionId
                    If TRegionId = "" Then TRegionId = Item.SupervisorRegionId
                    If Item.RegionId <> "" AndAlso Not RequiredAssignorIds.Contains(TRegionId) Then
                        RequiredAssignorIds.Add(TRegionId)
                    End If
                Next

                Dim Regions = UmpireAssignor.Region.GetRegionFromRegionIdsHelper(RequiredAssignorIds, SqlConnection, SqlTransaction)

                For Each Region In Regions
                    If Region.RequiresPayment Then Return New ErrorObject("RequiresPayment")
                Next

                LinkableSchedule.Sort(Schedule.LinkSorter)


                Dim SQLCommand As New SqlCommand()
                SQLCommand.Connection = SqlConnection
                SQLCommand.Transaction = SqlTransaction
                Dim CommandSB As New StringBuilder()
                Dim CommandCount As Integer = 0

                SQLCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))

                Dim SQLCommandText = <SQL>
DELETE FROM ScheduleIgnore WHERE RegionId = @RegionId{0} AND LinkedRegionId = @LinkedRegionId{0} AND ScheduleId = @ScheduleId{0};
                                    </SQL>.Value.ToString()

                For Each LinkableScheduleItem In LinkableSchedule
                    CommandCount += 1

                    CommandSB.Append(String.Format(SQLCommandText, CommandCount))
                    Dim TRegionId = LinkableScheduleItem.OfficialRegionId
                    If TRegionId = "" Then TRegionId = LinkableScheduleItem.ScorekeeperRegionId
                    If TRegionId = "" Then TRegionId = LinkableScheduleItem.SupervisorRegionId

                    SQLCommand.Parameters.Add(New SqlParameter("RegionId" & CommandCount, LinkableScheduleItem.RegionId))
                    SQLCommand.Parameters.Add(New SqlParameter("LinkedRegionId" & CommandCount, LinkableScheduleItem.LinkedRegionId))
                    SQLCommand.Parameters.Add(New SqlParameter("ScheduleId" & CommandCount, LinkableScheduleItem.ScheduleId))

                    If CommandCount > 50 Then ExecuteSQLCommand(SqlConnection, SqlTransaction, SQLCommand, CommandSB, CommandCount, "allregions", AssignorId)
                Next

                If CommandCount > 0 Then
                    SQLCommand.CommandText = CommandSB.ToString()
                    SQLCommand.ExecuteNonQuery()
                End If

                SqlTransaction.Commit()
            End Using
        End Using

        Return New With {
            .Success = True
        }
        'Catch E As Exception
        'Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Shared Function UpsertSchedule(AssignorId As String, RegionId As String, Schedule As Schedule, OldSchedule As Schedule, Optional ByPassAdmin As Boolean = false) As Object
        Dim Regions As List(Of RegionProperties) = Nothing
        Dim ScheduleId As Integer = -1

        Dim OneSchedule = If(Schedule Is Nothing, OldSchedule, Schedule)

        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                If Not ByPassAdmin AndAlso (Assignor Is Nothing OrElse Not Assignor.IsExecutive) Then
                    Return New ErrorObject("InvalidPermissions")
                End If

                Dim CurrentScheduleId As New ScheduleId(RegionId, SqlConnection, SqlTransaction)

                Dim UniqueRegionIds As New List(Of String)
                UniqueRegionIds.Add(RegionId)

                Dim DisplaySchedule As Schedule = Schedule
                If DisplaySchedule Is Nothing Then DisplaySchedule = OldSchedule

                If DisplaySchedule.LinkedRegionId <> "" AndAlso Not UniqueRegionIds.Contains(DisplaySchedule.LinkedRegionId) Then
                    UniqueRegionIds.Add(DisplaySchedule.LinkedRegionId)
                End If

                Regions = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction, False)

                If Regions(0).RequiresPayment Then Return New ErrorObject("RequiresPayment")

                Dim CommandSB As New StringBuilder()

                CommandSB.Append("DELETE FROM ScheduleTempSubmitted WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId; ")
                CommandSB.Append("INSERT INTO ScheduleTempSubmitted (RegionId, UserSubmitId, IsSubmitted) VALUES (@RegionId, @AssignorId, 1); ")
                Using SQLCommand As New SqlCommand(CommandSB.ToString, SqlConnection, SqlTransaction)
                    SQLCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                    SQLCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                    SQLCommand.ExecuteNonQuery()
                End Using

                Dim TScheduleId As Integer = If(Schedule IsNot Nothing, Schedule.ScheduleId, OldSchedule.ScheduleId)

                Dim OldNewSchedule = GetMasterScheduleFromRegionTempSingleHelper(RegionId, AssignorId, TScheduleId, SqlConnection, SqlTransaction)
                SchedulePosition.GetMasterSchedulePositionFromRegionTempSingleHelper(RegionId, AssignorId, TScheduleId, OldNewSchedule, SqlConnection, SqlTransaction)
                ScheduleFine.GetMasterScheduleFineFromRegionTempSingleHelper(RegionId, AssignorId, TScheduleId, OldNewSchedule, SqlConnection, SqlTransaction)
                ScheduleUserComment.GetMasterScheduleFineFromRegionTempSingleHelper(RegionId, AssignorId, TScheduleId, OldNewSchedule, SqlConnection, SqlTransaction)
                ScheduleUserCommentTeam.GetMasterScheduleUserCommentTeamFromRegionTempSingleHelper(RegionId, AssignorId, TScheduleId, OldNewSchedule, SqlConnection, SqlTransaction)
                ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionTempSingleHelper(RegionId, AssignorId, TScheduleId, OldNewSchedule, SqlConnection, SqlTransaction)
                ScheduleCallup.GetMasterScheduleCallUpFromRegionTempSingleHelper(RegionId, AssignorId, TScheduleId, OldNewSchedule, SqlConnection, SqlTransaction)

                If OldSchedule Is Nothing AndAlso OldNewSchedule IsNot Nothing AndAlso OldNewSchedule.OldScheduleId > 0 Then
                    OldSchedule = GetSingleGameHelper(RegionId, OldNewSchedule.OldScheduleId, SqlConnection, SqlTransaction)
                    SchedulePosition.GetSingleSchedulePositionFromRegionHelper(RegionId, Schedule, SqlConnection, SqlTransaction)
                    ScheduleFine.GetSingleScheduleFineFromRegionHelper(RegionId, Schedule, SqlConnection, SqlTransaction)
                    ScheduleUserComment.GetSingleScheduleUserCommentFromRegionHelper(RegionId, Schedule, SqlConnection, SqlTransaction)
                    ScheduleUserCommentTeam.GetSingleScheduleUserCommentTeamFromRegionHelper(RegionId, Schedule, SqlConnection, SqlTransaction)
                    ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(RegionId, Schedule, SqlConnection, SqlTransaction)
                    ScheduleCallup.GetSingleScheduleCallUpFromRegionHelper(RegionId, Schedule, SqlConnection, SqlTransaction)
                End If

                Dim InsertScheduleTemps As New List(Of ScheduleTemp)
                Dim InsertSchedulePositionsTemp As New List(Of SchedulePositionFull)
                Dim InsertScheduleFinesTemp As New List(Of ScheduleFineFull)
                Dim InsertScheduleUserCommentsTemp As New List(Of ScheduleUserCommentFull)
                Dim InsertScheduleUserCommentTeamsTemp As New List(Of ScheduleUserCommentTeamFull)
                Dim InsertScheduleCommentTeamsTemp As New List(Of ScheduleCommentTeamFull)
                Dim InsertScheduleCallupsTemp As New List(Of ScheduleCallupFull)
                Dim DeleteScheduleTemps As New List(Of ScheduleTemp)

                Dim Region = Regions.Find(Function (R) R.RegionId = OneSchedule.RegionId)

                ScheduleId = UpsertToTempHelper(AssignorId, Region, Regions, Schedule, OldSchedule, OldNewSchedule, CurrentScheduleId, InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp, DeleteScheduleTemps)

                ScheduleTemp.BulkDelete(DeleteScheduleTemps, AssignorId, SqlConnection, SqlTransaction)
                ScheduleTemp.BulkInsert(InsertScheduleTemps, AssignorId, SqlConnection, SqlTransaction)
                SchedulePositionFull.BulkInsert(InsertSchedulePositionsTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleFineFull.BulkInsert(InsertScheduleFinesTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleUserCommentFull.BulkInsert(InsertScheduleUserCommentsTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleUserCommentTeamFull.BulkInsert(InsertScheduleUserCommentTeamsTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleCommentTeamFull.BulkInsert(InsertScheduleCommentTeamsTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleCallupFull.BulkInsert(InsertScheduleCallupsTemp, AssignorId, SqlConnection, SqlTransaction)

                UmpireAssignor.ScheduleId.UpsertHelper({CurrentScheduleId}.ToList(), SqlConnection, SqlTransaction)

                SqlTransaction.Commit()
            End Using
        End Using

        Return New With {
            .Success = True,
            .ScheduleId = ScheduleId
        }
        'Catch E As Exception
        'Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Shared Function DeleteSchedule(AssignorId As String, Schedule As ScheduleTemp) As Object
        Dim GameEmails As New Dictionary(Of String, GameEmail)
        Dim Region As Region = Nothing
        Dim ScheduleId As Integer = -1

        Dim RegionId As String = Schedule.RegionId

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                    If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                        Return New ErrorObject("InvalidPermissions")
                    End If

                    Region = UmpireAssignor.Region.GetRegionHelper(RegionId, SqlConnection, SqlTransaction, True)

                    If Region.RequiresPayment Then Return New ErrorObject("RequiresPayment")

                    Dim CommandText As String = "DELETE FROM ScheduleTemp WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId AND ScheduleId = @ScheduleId; DELETE FROM SchedulePositionTemp WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId AND ScheduleId = @ScheduleId; DELETE FROM ScheduleFineTemp WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId AND ScheduleId = @ScheduleId;  DELETE FROM ScheduleUserCommentTemp WHERE RegionId = @RegionId AND UserSubmitId = @AssignorId AND ScheduleId = @ScheduleId;"
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                        SqlCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                        SqlCommand.Parameters.Add(New SqlParameter("ScheduleId", Schedule.ScheduleId))
                        ScheduleId = Schedule.ScheduleId
                        SqlCommand.ExecuteNonQuery()
                    End Using

                    If Schedule.OldScheduleId Then
                        Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                            SqlCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                            SqlCommand.Parameters.Add(New SqlParameter("ScheduleId", Schedule.OldScheduleId))
                            ScheduleId = Schedule.ScheduleId
                            SqlCommand.ExecuteNonQuery()
                        End Using
                    End If

                    SqlTransaction.Commit()
                End Using
            End Using

            Return New With {
                .Success = True,
                .ScheduleId = ScheduleId
            }
        Catch E As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function BookOff(Username As String, Schedule As List(Of Schedule), Reasons As List(Of String)) As Object
        Username = Username.ToLower
        For Each ScheduleGame In Schedule
            ScheduleGame.RegionId = ScheduleGame.RegionId.ToLower
            ScheduleGame.OfficialId = ScheduleGame.OfficialId.ToLower
        Next

        Dim UniqueRegionIds As New List(Of String)
        For Each ScheduleGame In Schedule
            If ScheduleGame.RegionId <> "" AndAlso Not UniqueRegionIds.Contains(ScheduleGame.RegionId) Then UniqueRegionIds.Add(ScheduleGame.RegionId)
            If ScheduleGame.LinkedRegionId <> "" AndAlso Not UniqueRegionIds.Contains(ScheduleGame.LinkedRegionId) Then UniqueRegionIds.Add(ScheduleGame.LinkedRegionId)
        Next


        Dim UsersRegionUsers = RegionUser.GetUniqueRegionAndOfficialIds(Schedule)
        If UsersRegionUsers.Count = 0 Then Return New ErrorObject("InvalidPermissions")

        Dim GameEmails As New List(Of GameEmail)
        Dim BookOffEmails As New List(Of GameEmail)

        Dim DoBookOkff As New List(Of Boolean)
        For Each ScheduleGame In Schedule
            DoBookOkff.Add(True)
        Next

        Dim AllRegions As New Dictionary(Of String, UmpireAssignor.Region)

        Dim DateAdded As Date = Date.UtcNow

        Dim ScheduleNonBookOff As New List(Of ConfirmGameNotification)

        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()
                Dim Assignors = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsernames(Username, UniqueRegionIds, SqlConnection, SqlTransaction)
                Dim AllRegionProperties = RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)

                Dim Regions As List(Of RegionProperties) = RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)

                For I As Integer = Schedule.Count - 1 To 0 Step -1
                    Dim ScheduleGame = Schedule(I)
                    Dim RegionProperties = AllRegionProperties.Find(Function(RP) RP.RegionId = ScheduleGame.RegionId)
                    If RegionProperties.EntityType = "team" Then
                        ScheduleNonBookOff.Add(New ConfirmGameNotification With {
                            .RegionId = ScheduleGame.RegionId,
                            .ScheduleId = ScheduleGame.ScheduleId,
                            .LinkedRegionId = ScheduleGame.LinkedRegionId,
                            .VersionId = ScheduleGame.VersionId,
                            .OfficialId = ScheduleGame.OfficialId,
                            .Confirmed = 2
                        })
                        Schedule.RemoveAt(I)
                    End If
                Next

                If ScheduleNonBookOff.Count > 0 Then
                    Dim ScheduleResult = GameNotification.GetScheduleFromConfirmItemsHelper(AllRegionProperties, ScheduleNonBookOff, SqlConnection, SqlTransaction)
                    GameNotification.ConfirmGamesHelper(Username, Assignors, AllRegionProperties, ScheduleNonBookOff, ScheduleResult, SqlConnection, SqlTransaction)
                End If

                For Each ScheduleGame In Schedule
                    Dim GameIsOk As Boolean = False
                    For Each Assignor In Assignors
                        If Assignor.RegionId = ScheduleGame.RegionId Then
                            If Assignor.IsExecutive Then GameIsOk = True
                            If ScheduleGame.OfficialId = Assignor.Username.ToLower Then GameIsOk = True
                        End If
                    Next
                    If Not GameIsOk Then Return New ErrorObject("InvalidPermissions")
                Next


                For I As Integer = 0 To Schedule.Count - 1
                    Dim ScheduleGame = Schedule(I)
                    Dim RegionProperties = AllRegionProperties.Find(Function(RP) RP.RegionId = ScheduleGame.RegionId)
                    If Not RegionProperties.AllowBookOffs Then DoBookOkff(I) = False
                Next

                Dim DBSchedules As New List(Of Schedule)
                Dim OldDBSchedules As New List(Of Schedule)
                For I As Integer = 0 To Schedule.Count - 1
                    Dim ScheduleGame = Schedule(I)
                    Dim DBScheduleGame As Schedule = Nothing
                    Dim OldDBScheduleGame As Schedule = Nothing

                    If DoBookOkff(I) Then
                        DBScheduleGame = GetSingleGameHelper(ScheduleGame.RegionId, ScheduleGame.ScheduleId, SqlConnection, SqlTransaction)
                        If DBScheduleGame.LinkedRegionId <> "" AndAlso Not UniqueRegionIds.Contains(DBScheduleGame.LinkedRegionId) Then
                            UniqueRegionIds.Add(DBScheduleGame.LinkedRegionId)
                        End If

                        If DBScheduleGame IsNot Nothing Then
                            If DBScheduleGame.LinkedRegionId <> "" AndAlso Not UniqueRegionIds.Contains(DBScheduleGame.LinkedRegionId) Then
                                UniqueRegionIds.Add(DBScheduleGame.LinkedRegionId)
                            End If

                            OldDBScheduleGame = GetSingleGameHelper(ScheduleGame.RegionId, ScheduleGame.ScheduleId, SqlConnection, SqlTransaction)
                            SchedulePosition.GetSingleSchedulePositionFromRegionHelper(ScheduleGame.RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            SchedulePosition.GetSingleSchedulePositionFromRegionHelper(ScheduleGame.RegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)
                            ScheduleFine.GetSingleScheduleFineFromRegionHelper(ScheduleGame.RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            ScheduleFine.GetSingleScheduleFineFromRegionHelper(ScheduleGame.RegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)
                            ScheduleUserComment.GetSingleScheduleUserCommentFromRegionHelper(ScheduleGame.RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            ScheduleUserComment.GetSingleScheduleUserCommentFromRegionHelper(ScheduleGame.RegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)
                            ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(ScheduleGame.RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(ScheduleGame.RegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)
                        Else
                            DoBookOkff(I) = False
                        End If
                    End If
                    OldDBSchedules.Add(OldDBScheduleGame)
                    DBSchedules.Add(DBScheduleGame)
                Next

                Regions = RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)

                For I As Integer = 0 To Schedule.Count - 1
                    If DoBookOkff(I) Then
                        Dim ScheduleGame = Schedule(I)
                        Dim DBScheduleGame = DBSchedules(I)
                        Dim RegionProperties = AllRegionProperties.Find(Function(RP) RP.RegionId = ScheduleGame.RegionId)
                        Dim TimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(RegionProperties.TimeZone)
                        If Not RegionProperties.RegionIsLadderLeague AndAlso DateTime.UtcNow.AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours) > DBScheduleGame.GameDate.AddHours(-RegionProperties.BookOffHoursBefore) Then
                            DoBookOkff(I) = False
                        End If
                    End If
                Next

                Dim RegionLeagues = RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(RegionLeague.GetRegionsLeaguesHelper(UniqueRegionIds, SqlConnection, SqlTransaction), New List(Of RegionLeaguePay))
                Dim RegionUsers = RegionUser.LoadAllInRegionIdsHelper(UniqueRegionIds, Username, SqlConnection, SqlTransaction)

                For Each RegionLeague In RegionLeagues
                    If RegionLeague.RealLeagueId <> "" AndAlso Not UniqueRegionIds.Contains(RegionLeague.RealLeagueId) Then
                        UniqueRegionIds.Add(RegionLeague.RealLeagueId)
                    End If
                Next

                Dim RegionParks = Park.GetParksInRegionsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)
                Dim Teams = Team.GetTeamsInRegionsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)


                Dim DeleteSchedules As New List(Of Schedule)
                Dim InsertSchedules As New List(Of Schedule)
                Dim InsertSchedulePositions As New List(Of SchedulePositionFull)
                Dim InsertScheduleFines As New List(Of ScheduleFineFull)
                Dim InsertScheduleUserComments As New List(Of ScheduleUserCommentFull)
                Dim InsertScheduleUserCommentTeams As New List(Of ScheduleUserCommentTeamFull)
                Dim InsertScheduleCommentTeams As New List(Of ScheduleCommentTeamFull)
                Dim InsertScheduleCallups As New List(Of ScheduleCallupFull)

                For I As Integer = 0 To Schedule.Count - 1
                    Dim ScheduleGame = Schedule(I)
                    Dim DBScheduleGame = DBSchedules(I)
                    Dim OldDBScheduleGame = OldDBSchedules(I)

                    Dim OldVersionId = DBScheduleGame.VersionId

                    If DoBookOkff(I) Then

                        Dim Region = Regions.Find(Function(R) R.RegionId = ScheduleGame.RegionId)

                        If Not Region.RegionIsLadderLeague Then
                            For Each SP In DBScheduleGame.SchedulePositions
                                If SP.OfficialId.ToLower = ScheduleGame.OfficialId Then
                                    SP.OfficialId = ""
                                End If
                            Next
                        End if
                        Dim CurrentScheduleId As New ScheduleId

                        Dim AssignorsWithUser = New List(Of RegionUser)
                        For Each RegionUser In RegionUsers
                            If RegionUser.IsAssignor() AndAlso RegionUser.RegionId = Region.RegionId Then
                                AssignorsWithUser.Add(RegionUser)
                            End If
                        Next

                        Dim Official As RegionUser = Nothing
                        For Each RegionUser In RegionUsers
                            If RegionUser.Username.ToLower = ScheduleGame.OfficialId Then Official = RegionUser
                        Next

                        Dim DBScheduleTemp = New ScheduleTemp()
                        DBScheduleTemp.FromSchedule(DBScheduleGame, OldDBScheduleGame)

                        Dim GamesWhosScoresNeedToBeUpdated As New List(Of Schedule)

                        If Not Region.RegionIsLadderLeague Then
                            UpsertScheduleHelper(Regions, DBScheduleTemp, OldDBScheduleGame, CurrentScheduleId, RegionUsers, RegionParks, RegionLeagues, Teams, New List(Of OfficialRegion), GameEmails, New List(Of GameEmail), DateAdded, DeleteSchedules, InsertSchedules, InsertSchedulePositions, InsertScheduleFines, InsertScheduleUserComments, InsertScheduleUserCommentTeams, InsertScheduleCommentTeams, InsertScheduleCallups, GamesWhosScoresNeedToBeUpdated)
                        End If

                        For Each Assignor In AssignorsWithUser
                            Dim BookOffEmail As GameEmail = BookOffEmails.Find(Function(GE)
                                                                                   Return IsInvolvedOfficialInGameEmail(Assignor, GE)
                                                                               End Function)

                            If BookOffEmail Is Nothing Then
                                BookOffEmail = New GameEmail With {
                                    .IsBookOff = True,
                                    .RegionProperties = Regions,
                                    .RegionUser = Assignor,
                                    .RegionUsers = RegionUsers,
                                    .RegionLeagues = RegionLeagues,
                                    .RegionParks = RegionParks,
                                    .Teams = Teams,
                                    .User = User.GetUserInfoHelper(Assignor.RealUsername, SqlConnection, SqlTransaction),
                                    .ScheduleData = New List(Of ScheduleDataEmail)
                                }
                                BookOffEmails.Add(BookOffEmail)
                            End If

                            Dim IsDublicate As Boolean = False
                            For Each ScheduleData In BookOffEmail.ScheduleData
                                If ScheduleData.RegionUser.Username.ToLower = Official.Username.ToLower AndAlso ScheduleData.NewScheduleItem.ScheduleId = ScheduleGame.ScheduleId AndAlso ScheduleData.RegionUser.RegionId = Official.RegionId Then
                                    IsDublicate = True
                                End If
                            Next

                            If Not IsDublicate Then
                                BookOffEmail.ScheduleData.Add(New ScheduleDataEmail(Official, OldDBScheduleGame, OldDBScheduleGame, "", Region, False, Reasons(I)))
                            End If
                        Next

                        If Region.RegionIsLadderLeague Then
                            For Each Position In OldDBScheduleGame.SchedulePositions
                                Dim OfficialOnGame As RegionUser = PublicCode.BinarySearchItem(RegionUsers, New RegionUser(OldDBScheduleGame.RegionId, Position.OfficialId.Trim.ToLower), RegionUser.BasicSorter)
                                If OfficialOnGame Is Nothing Then Continue For
                                If OfficialOnGame.RealUsername = Username Then Continue For

                                Dim BookOffEmail As GameEmail = BookOffEmails.Find(Function(GE)
                                                        Return IsInvolvedOfficialInGameEmail(OfficialOnGame, GE)
                                                    End Function)

                                If BookOffEmail Is Nothing Then
                                    BookOffEmail = New GameEmail With {
                                        .IsBookOff = True,
                                        .RegionProperties = Regions,
                                        .RegionUser = OfficialOnGame,
                                        .RegionUsers = RegionUsers,
                                        .RegionLeagues = RegionLeagues,
                                        .RegionParks = RegionParks,
                                        .Teams = Teams,
                                        .User = User.GetUserInfoHelper(OfficialOnGame.RealUsername, SqlConnection, SqlTransaction),
                                        .ScheduleData = New List(Of ScheduleDataEmail)
                                    }
                                    BookOffEmails.Add(BookOffEmail)
                                End If

                                Dim IsDublicate As Boolean = False
                                For Each ScheduleData In BookOffEmail.ScheduleData
                                    If ScheduleData.RegionUser.Username.ToLower = Official.Username.ToLower AndAlso ScheduleData.NewScheduleItem.ScheduleId = ScheduleGame.ScheduleId AndAlso ScheduleData.RegionUser.RegionId = Official.RegionId Then
                                        IsDublicate = True
                                    End If
                                Next

                                If Not IsDublicate Then
                                    BookOffEmail.ScheduleData.Add(New ScheduleDataEmail(Official, OldDBScheduleGame, OldDBScheduleGame, "", Region, False, Reasons(I)))
                                End If
                            Next

                            If DBScheduleGame.GameNumber <> "" Then
                                DBScheduleTemp.GameStatus = "cancelled"
                                UpsertScheduleHelper(Regions, DBScheduleTemp, OldDBScheduleGame, CurrentScheduleId, RegionUsers, RegionParks, RegionLeagues, Teams, New List(Of OfficialRegion), New List(Of GameEmail), New List(Of GameEmail), DateAdded, DeleteSchedules, InsertSchedules, InsertSchedulePositions, InsertScheduleFines, InsertScheduleUserComments, InsertScheduleUserCommentTeams, InsertScheduleCommentTeams, InsertScheduleCallups, GamesWhosScoresNeedToBeUpdated)
                            Else
                                DeleteScheduleHelper(Regions, DBScheduleGame, RegionUsers, RegionParks, RegionLeagues, Teams, New List(Of OfficialRegion), New List(Of GameEmail), New List(Of GameEmail), DateAdded, DeleteSchedules, InsertSchedules, GamesWhosScoresNeedToBeUpdated)
                            End If

                        End If

                        GameNotification.ConfirmGameNotificationHelper(Official.RealUsername, ScheduleGame.RegionId, ScheduleGame.ScheduleId, ScheduleGame.OfficialId, OldDBScheduleGame.VersionId + 1, DateAdded, SqlConnection, SqlTransaction)
                        ScheduleBookOff.UpsertHelper(New ScheduleBookOff With {
                            .RegionId = ScheduleGame.RegionId,
                            .Username = ScheduleGame.OfficialId,
                            .ScheduleId = ScheduleGame.ScheduleId,
                            .VersionId = OldVersionId,
                            .Reason = Reasons(I)
                        }, DateAdded, SqlConnection, SqlTransaction)

                    End If
                Next


                UmpireAssignor.Schedule.BulkDelete(DeleteSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.Schedule.BulkInsert(InsertSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.Schedule.BulkInsertVersions(InsertSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.SchedulePositionFull.BulkInsert(InsertSchedulePositions, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleFineFull.BulkInsert(InsertScheduleFines, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleUserCommentFull.BulkInsert(InsertScheduleUserComments, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleUserCommentTeamFull.BulkInsert(InsertScheduleUserCommentTeams, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleCommentTeamFull.BulkInsert(InsertScheduleCommentTeams, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleCallupFull.BulkInsert(InsertScheduleCallups, SqlConnection, SqlTransaction)

                FixGameEmails(GameEmails, Nothing, SqlConnection, SqlTransaction)

                SqlTransaction.Commit()
            End Using
        End Using

        For Each GameEmail In BookOffEmails
            If GameEmail.ScheduleData.Count > 0 Then
                GameEmail.Send(Nothing)
            End If
        Next

        Return New With {
           .Success = True
       }
        'Catch E As Exception
        'Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Shared Function PostUserComment(Username As String, ScheduleUserComment As Schedule) As Object
        Username = Username.ToLower
        ScheduleUserComment.RegionId = ScheduleUserComment.RegionId.ToLower
        ScheduleUserComment.OfficialId = ScheduleUserComment.OfficialId.ToLower

        Dim GameEmails As New List(Of GameEmail)
        Dim UserCommentEmails As New Dictionary(Of String, GameEmail)

        Dim DoPostUserComment As Boolean = True

        Dim Regions As List(Of RegionProperties) = Nothing

        Dim DateAdded As Date = Date.UtcNow

        Dim RegionId As String = If(ScheduleUserComment.LinkedRegionId <> "", ScheduleUserComment.LinkedRegionId, ScheduleUserComment.RegionId)
        Dim LinkedRegionId As String = If(ScheduleUserComment.RegionId <> "", ScheduleUserComment.RegionId, ScheduleUserComment.LinkedRegionId)

        Dim UniqueRegionIds As New List(Of String)
        UniqueRegionIds.Add(ScheduleUserComment.RegionId)
        If ScheduleUserComment.LinkedRegionId <> "" AndAlso ScheduleUserComment.LinkedRegionId <> ScheduleUserComment.RegionId Then
            UniqueRegionIds.Add(ScheduleUserComment.LinkedRegionId)
        End If

        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, Username, SqlConnection, SqlTransaction, ScheduleUserComment.OfficialId)

                Dim GameIsOk As Boolean = False
                If Assignor IsNot Nothing AndAlso Assignor.IsExecutive Then GameIsOk = True
                If ScheduleUserComment.OfficialId = Assignor.Username.ToLower Then GameIsOk = True

                If Not GameIsOk Then Return New ErrorObject("InvalidPermissions")


                Regions = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)


                Dim Region = Regions.Find(Function(R) R.RegionId = RegionId)

                Dim DBSchedule As Schedule = Nothing
                Dim OldDBSchedule As Schedule = Nothing
                Dim ScheduleGame = ScheduleUserComment
                Dim DBScheduleGame As Schedule = Nothing
                Dim OldDBScheduleGame As Schedule = Nothing

                If DoPostUserComment Then
                    DBScheduleGame = GetSingleGameHelper(LinkedRegionId, ScheduleGame.ScheduleId, SqlConnection, SqlTransaction)
                    If DBScheduleGame IsNot Nothing Then
                        OldDBScheduleGame = GetSingleGameHelper(LinkedRegionId, ScheduleGame.ScheduleId, SqlConnection, SqlTransaction)

                        If Region.EntityType <> "team" Then
                            SchedulePosition.GetSingleSchedulePositionFromRegionHelper(LinkedRegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            SchedulePosition.GetSingleSchedulePositionFromRegionHelper(LinkedRegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)
                            ScheduleFine.GetSingleScheduleFineFromRegionHelper(LinkedRegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            ScheduleFine.GetSingleScheduleFineFromRegionHelper(LinkedRegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)
                            UmpireAssignor.ScheduleUserComment.GetSingleScheduleUserCommentFromRegionHelper(LinkedRegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            UmpireAssignor.ScheduleUserComment.GetSingleScheduleUserCommentFromRegionHelper(LinkedRegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)
                            UmpireAssignor.ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(LinkedRegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            UmpireAssignor.ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(LinkedRegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)

                        Else
                            DBScheduleGame.LinkedRegionId = ""
                            OldDBScheduleGame.LinkedRegionId = ""

                            UmpireAssignor.ScheduleUserCommentTeam.GetSingleScheduleUserCommentTeamFromRegionHelper(LinkedRegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            UmpireAssignor.ScheduleUserCommentTeam.GetSingleScheduleUserCommentTeamFromRegionHelper(LinkedRegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)

                            UmpireAssignor.ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(LinkedRegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            UmpireAssignor.ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(LinkedRegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)

                            UmpireAssignor.ScheduleCallup.GetSingleScheduleCallUpFromRegionHelper(LinkedRegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            UmpireAssignor.ScheduleCallup.GetSingleScheduleCallUpFromRegionHelper(LinkedRegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)
                        End If

                    Else
                        DoPostUserComment = False
                    End If
                End If

                OldDBSchedule = OldDBScheduleGame
                DBSchedule = DBScheduleGame

                Dim RegionLeagues = RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(RegionLeague.GetRegionLeaguesHelper(RegionId, SqlConnection, SqlTransaction), New List(Of RegionLeaguePay))
                Dim RegionUsers = RegionUser.LoadAllInRegionSimpleHelper(RegionId, SqlConnection, SqlTransaction)

                Dim TUniqueRegionIds As New List(Of String)
                TUniqueRegionIds.AddRange(UniqueRegionIds)
                For Each RegionLeague In RegionLeagues
                    If RegionLeague.RealLeagueId <> "" AndAlso Not TUniqueRegionIds.Contains(RegionLeague.RealLeagueId) Then TUniqueRegionIds.Add(RegionLeague.RealLeagueId)
                Next

                Regions = RegionProperties.GetRegionPropertiesRegionIdsHelper(TUniqueRegionIds, SqlConnection, SqlTransaction)
                Dim Teams = Team.GetTeamsInRegionsHelper(TUniqueRegionIds, SqlConnection, SqlTransaction)
                Dim RegionParks = Park.GetParksInRegionsHelper(TUniqueRegionIds, SqlConnection, SqlTransaction)

                Dim OldVersionId = DBScheduleGame.VersionId

                Dim DeleteSchedules As New List(Of Schedule)
                Dim InsertSchedules As New List(Of Schedule)
                Dim InsertSchedulePositions As New List(Of SchedulePositionFull)
                Dim InsertScheduleFines As New List(Of ScheduleFineFull)
                Dim InsertScheduleUserComments As New List(Of ScheduleUserCommentFull)
                Dim InsertScheduleUserCommentTeams As New List(Of ScheduleUserCommentTeamFull)
                Dim InsertScheduleCommentTeams As New List(Of ScheduleCommentTeamFull)
                Dim InsertScheduleCallups As New List(Of ScheduleCallupFull)

                If DoPostUserComment Then
                    Dim FoundUserComment As Boolean = False
                    If Region.EntityType = "team" Then
                        For I As Integer = DBScheduleGame.ScheduleUserCommentTeams.Count - 1 To 0 Step -1
                            Dim SU = DBScheduleGame.ScheduleUserCommentTeams(I)
                            If SU.LinkedRegionId = RegionId AndAlso SU.OfficialId.ToLower = ScheduleGame.OfficialId Then
                                If ScheduleGame.GameComment = "" Then
                                    DBScheduleGame.ScheduleUserCommentTeams(I).IsDeleted = True
                                Else
                                    SU.Comment = ScheduleGame.GameComment
                                End If
                                FoundUserComment = True
                            End If
                        Next

                        For Each SU In DBScheduleGame.ScheduleUserCommentTeams
                            If SU.LinkedRegionId = RegionId AndAlso SU.OfficialId.ToLower = ScheduleGame.OfficialId Then
                                SU.Comment = ScheduleGame.GameComment
                                FoundUserComment = True
                            End If
                        Next
                        If Not FoundUserComment Then
                            If ScheduleGame.GameComment <> "" Then
                                DBScheduleGame.ScheduleUserCommentTeams.Add(New ScheduleUserCommentTeam With {
                                    .RegionId = LinkedRegionId,
                                    .LinkedRegionId = RegionId,
                                    .OfficialId = ScheduleGame.OfficialId,
                                    .IsDeleted = False,
                                    .Comment = ScheduleGame.GameComment
                                })
                            End If
                        End If
                    Else
                        For I As Integer = DBScheduleGame.ScheduleUserComments.Count - 1 To 0 Step -1
                            Dim SU = DBScheduleGame.ScheduleUserComments(I)
                            If SU.OfficialId.ToLower = ScheduleGame.OfficialId Then
                                If ScheduleGame.GameComment = "" Then
                                    DBScheduleGame.ScheduleUserComments(I).IsDeleted = True
                                Else
                                    SU.Comment = ScheduleGame.GameComment
                                End If
                                FoundUserComment = True
                            End If
                        Next

                        For Each SU In DBScheduleGame.ScheduleUserComments
                            If SU.OfficialId.ToLower = ScheduleGame.OfficialId Then
                                SU.Comment = ScheduleGame.GameComment
                                FoundUserComment = True
                            End If
                        Next
                        If Not FoundUserComment Then
                            If ScheduleGame.GameComment <> "" Then
                                DBScheduleGame.ScheduleUserComments.Add(New ScheduleUserComment With {
                                    .RegionId = ScheduleGame.RegionId,
                                    .OfficialId = ScheduleGame.OfficialId,
                                    .IsDeleted = False,
                                    .Comment = ScheduleGame.GameComment
                                })
                            End If
                        End If
                    End If



                    Dim CurrentScheduleId As New ScheduleId

                    Dim AssignorsWithUser = New List(Of RegionUser)
                    For Each RegionUser In RegionUsers
                        If Region.EntityType = "team" Then
                            If RegionUser.IsCoach AndAlso RegionUser.RegionId = Region.RegionId Then
                                AssignorsWithUser.Add(RegionUser)
                            End If
                        Else
                            If RegionUser.IsAssignor() AndAlso RegionUser.RegionId = Region.RegionId Then
                                AssignorsWithUser.Add(RegionUser)
                            End If
                        End If
                    Next

                    Dim Official As RegionUser = Nothing
                    For Each RegionUser In RegionUsers
                        If RegionUser.Username.ToLower = ScheduleGame.OfficialId Then Official = RegionUser
                    Next

                    Dim DBScheduleTemp = New ScheduleTemp()
                    DBScheduleTemp.FromSchedule(DBScheduleGame, OldDBScheduleGame)

                    If OldDBScheduleGame.IsDifferent(DBScheduleTemp) Then
                        Dim GamesWhosScoresNeedToBeUpdated As New List(Of Schedule)

                        UpsertScheduleHelper(Regions, DBScheduleTemp, OldDBScheduleGame, CurrentScheduleId, RegionUsers, RegionParks, RegionLeagues, Teams, New List(Of OfficialRegion), Nothing, Nothing, DateAdded, DeleteSchedules, InsertSchedules, InsertSchedulePositions, InsertScheduleFines, InsertScheduleUserComments, InsertScheduleUserCommentTeams, InsertScheduleCommentTeams, InsertScheduleCallups, GamesWhosScoresNeedToBeUpdated)

                        For Each Assignor In AssignorsWithUser
                            Dim UserCommentEmail As GameEmail
                            If Not UserCommentEmails.ContainsKey(Assignor.RealUsername) Then
                                UserCommentEmail = New GameEmail With {
                                    .IsUserComment = True,
                                    .RegionProperties = Regions,
                                    .RegionUsers = RegionUsers,
                                    .RegionLeagues = RegionLeagues,
                                    .RegionParks = RegionParks,
                                    .Teams = Teams,
                                    .OfficialRegions = New List(Of OfficialRegion),
                                    .User = User.GetUserInfoHelper(Assignor.RealUsername, SqlConnection, SqlTransaction),
                                    .ScheduleData = New List(Of ScheduleDataEmail)
                                }
                                UserCommentEmails.Add(Assignor.RealUsername, UserCommentEmail)
                            Else
                                UserCommentEmail = UserCommentEmails(Assignor.RealUsername)
                            End If

                            Dim IsDublicate As Boolean = False
                            For Each ScheduleData In UserCommentEmail.ScheduleData
                                If ScheduleData.RegionUser.Username.ToLower = Official.Username.ToLower AndAlso ScheduleData.NewScheduleItem.ScheduleId = ScheduleGame.ScheduleId AndAlso ScheduleData.RegionUser.RegionId = Official.RegionId Then
                                    IsDublicate = True
                                End If
                            Next

                            If Not IsDublicate Then
                                UserCommentEmail.ScheduleData.Add(New ScheduleDataEmail(Official, DBScheduleGame, DBScheduleGame, "", Region, False))
                            End If
                        Next
                    End If
                End If

                UmpireAssignor.Schedule.BulkDelete(DeleteSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.Schedule.BulkInsert(InsertSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.Schedule.BulkInsertVersions(InsertSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.SchedulePositionFull.BulkInsert(InsertSchedulePositions, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleFineFull.BulkInsert(InsertScheduleFines, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleUserCommentFull.BulkInsert(InsertScheduleUserComments, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleUserCommentTeamFull.BulkInsert(InsertScheduleUserCommentTeams, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleCommentTeamFull.BulkInsert(InsertScheduleCommentTeams, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleCallupFull.BulkInsert(InsertScheduleCallups, SqlConnection, SqlTransaction)

                SqlTransaction.Commit()
            End Using
        End Using

        For Each GameEmail In UserCommentEmails
            If GameEmail.Value.ScheduleData.Count > 0 Then
                For Each Item In GameEmail.Value.ScheduleData
                    If Item.NewScheduleItem IsNot Nothing Then Item.NewScheduleItem.MergeTeamData(Item.NewScheduleItem.RegionId)
                    If Item.OldScheduleItem IsNot Nothing Then Item.OldScheduleItem.MergeTeamData(Item.OldScheduleItem.RegionId)
                Next
                GameEmail.Value.Send(Nothing)
            End If
        Next

        Return New With {
           .Success = True
        }
        'Catch E As Exception
        'Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Function CanShowSimpleEdit(Username As String, Regions As List(Of RegionProperties), RegionUsers As List(Of RegionUser), Teams As List(Of Team)) As ShowSimpleEdit
        Dim Result As New ShowSimpleEdit

        If IsDeleted Then Return Result

        Dim Region = Regions.Find(Function(R) R.RegionId = RegionId)

        Dim IsExecutive = RegionUsers.Find(Function(RU) RU.RegionId = RegionId AndAlso RU.RealUsername = Username AndAlso RU.IsExecutive()) IsNot Nothing

        If Region.EntityType = "team" Then
            If IsExecutive Then
                Result.CanShowSimpleEdit = True
                Result.CanEditGroupComment = True
                Result.CanEditCallUps = True
                Result.CanEditUserComments = True
                If LinkedRegionId <> "" Then
                    Dim LinkedRegion = Regions.Find(Function(R) R.RegionId = LinkedRegionId)

                    Dim HT As String = HomeTeam.ToLower().Trim()
                    Dim AT As String = AwayTeam.ToLower().Trim()

                    Dim IsHomeTeam = PublicCode.BinarySearchItem(Teams, New Team With {.RegionId = LinkedRegionId, .TeamId = HT}, Team.BasicSorter) IsNot Nothing
                    Dim IsAwayTeam = PublicCode.BinarySearchItem(Teams, New Team With {.RegionId = LinkedRegionId, .TeamId = AT}, Team.BasicSorter) IsNot Nothing

                    If IsHomeTeam AndAlso LinkedRegion.HomeTeamCanEnterScore Then Result.CanEditScore = True
                    If IsAwayTeam AndAlso LinkedRegion.AwayTeamCanEnterScore Then Result.CanEditScore = True

                    Dim CanCancelHoursBefore = Math.Max(If(IsHomeTeam, LinkedRegion.HomeTeamCanCancelGameHoursBefore, 0), If(IsAwayTeam, LinkedRegion.AwayTeamCanCancelGameHoursBefore, 0))

                    If CanCancelHoursBefore > 0 Then
                        Dim TimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(LinkedRegion.TimeZone)

                        Dim Now = Date.UtcNow.AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours)

                        Dim MdLower As Date = GameDate.AddHours(-CanCancelHoursBefore)
                        Dim MdUpper As Date = GameDate.AddMinutes(LinkedRegion.DefaultMaxGameLengthMins)

                        If Now >= MdLower AndAlso Now <= MdUpper Then Result.CanCancelGame = True
                    End If
                Else
                    Result.CanCancelGame = True
                    Result.CanEditScore = True
                End If
            End If
        ElseIf Region.EntityType = "referee" Then
            Dim LinkedRegion = Regions.Find(Function(R) R.RegionId = LinkedRegionId)

            If IsExecutive Then
                If LinkedRegion IsNot Nothing AndAlso LinkedRegion.ScorekeeperCanEnterScore Then
                    Result.CanShowSimpleEdit = True
                    Result.CanEditScore = True
                End If
            End If

            If Not Result.CanEditScore Then
                If CrewType IsNot Nothing AndAlso CrewType.ContainsKey("scorekeeper") Then
                    For Each PositionId In RegionLeaguePayContracted.GetSportCrewPositionsScorekeepers()(Region.Sport)(CrewType("scorekeeper"))
                        Dim SchedulePosition = SchedulePositions.Find(Function(SP) SP.PositionId = PositionId)
                        If SchedulePosition Is Nothing OrElse SchedulePosition.OfficialId = "" Then Continue For

                        Dim ScoreKeeperRegionUser As RegionUser = PublicCode.BinarySearchItem(RegionUsers, New RegionUser With {.RegionId = RegionId, .Username = SchedulePosition.OfficialId}, RegionUser.BasicSorter)

                        If ScoreKeeperRegionUser IsNot Nothing AndAlso ScoreKeeperRegionUser.RealUsername = Username Then
                            Result.CanShowSimpleEdit = True
                            Result.CanEditScore = True
                        End If
                    Next
                End If
            End If
        End If


        Return Result
    End Function

    Public Shared Function DoSimpleUpdate(Username As String, SchedulePairs As List(Of Tuple(Of Schedule, Schedule))) As Object
        Dim GameEmails As New List(Of GameEmail)
        Dim AssignorEmails As New List(Of GameEmail)

        Dim SimpleUpdateResponse As Object = Nothing

        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()
                SimpleUpdateResponse = DoSimpleUpdateHelper(Username, SchedulePairs, GameEmails, AssignorEmails, SqlConnection, SqlTransaction)

                If SimpleUpdateResponse Is Nothing Then
                    SqlTransaction.Commit()
                Else
                    Return SimpleUpdateResponse
                End If
            End Using
        End Using
        
        For Each GameEmail In AssignorEmails
            If GameEmail.ScheduleData.Count > 0 Then
                For Each Item In GameEmail.ScheduleData
                    If Item.NewScheduleItem IsNot Nothing Then Item.NewScheduleItem.MergeTeamData(Item.Region.RegionId)
                    If Item.OldScheduleItem IsNot Nothing Then Item.OldScheduleItem.MergeTeamData(Item.OldScheduleItem.RegionId)
                Next
                GameEmail.Send(Nothing)
            End If
        Next


        For Each GameEmail In GameEmails
            GameEmail.IsScheduleChangeEmail = true
            If GameEmail.ScheduleData.Count > 0 Then
                For Each Item In GameEmail.ScheduleData
                    If Item.NewScheduleItem IsNot Nothing Then Item.NewScheduleItem.MergeTeamData(Item.Region.RegionId)
                    If Item.OldScheduleItem IsNot Nothing Then Item.OldScheduleItem.MergeTeamData(Item.Region.RegionId)
                Next
                GameEmail.Send(Nothing)
            End If
        Next

        Dim ScheduleIds As New List(Of integer)
        For Each SchedulePairItem In SchedulePairs
            If SchedulePairItem.Item1 IsNot Nothing Then
                ScheduleIds.Add(SchedulePairItem.Item1.ScheduleId)
            Else If SchedulePairItem.Item2 IsNot Nothing Then
                ScheduleIds.Add(SchedulePairItem.Item2.ScheduleId)
            End iF
        Next

        Return New With {
           .Success = True,
           .ScheduleId = ScheduleIds
        }
        'Catch E As Exception
        'Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Shared Function DoSimpleUpdateHelper(Username As String, SchedulePairs As List(Of Tuple(Of Schedule, Schedule)), GameEmails As List(Of GameEmail), AssignorEmails As List(Of GameEmail), SqlConnection As SqlConnection, SqlTransaction As SqlTransaction) As Object
        For each SchedulePairItem In SchedulePairs
            Dim ScheduleItem = SchedulePairItem.Item1
            Dim OldScheduleItemInput = SchedulePairItem.Item2

            Username = Username.ToLower
            If ScheduleItem IsNot Nothing Then
                ScheduleItem.RegionId = ScheduleItem.RegionId.ToLower
                ScheduleItem.LinkedRegionId = ScheduleItem.LinkedRegionId.ToLower
            End If

            If OldScheduleItemInput IsNot Nothing Then
                OldScheduleItemInput.RegionId = OldScheduleItemInput.RegionId.ToLower
                OldScheduleItemInput.LinkedRegionId = OldScheduleItemInput.LinkedRegionId.ToLower
            End If

            Dim UserCommentEmails As New Dictionary(Of String, GameEmail)

            Dim Regions As List(Of RegionProperties) = Nothing
            Dim RegionUsers As New List(Of RegionUser)
            Dim Teams As New List(Of Team)
            Dim RegionParks As New List(Of Park)
            Dim RegionLeagues As New List(Of RegionLeaguePayContracted)
            Dim OfficialRegions As New List(Of OfficialRegion)

            Dim DateAdded As Date = Date.UtcNow

            Dim RegionId As String = ""
            Dim ScheduleId As Integer = -1
            Dim LinkedRegionId As String = ""
            Dim LinkedScheduleId As Integer = -1
            Dim AScheduleItem As Schedule = ScheduleItem


            If ScheduleItem IsNot Nothing Then
                RegionId = ScheduleItem.RegionId
                ScheduleId = ScheduleItem.ScheduleId
                LinkedRegionId = If(ScheduleItem.LinkedRegionId <> "", ScheduleItem.LinkedRegionId, ScheduleItem.RegionId)
                LinkedScheduleId = If(ScheduleItem.LinkedScheduleId > 0, ScheduleItem.LinkedScheduleId, ScheduleItem.ScheduleId)
            Else
                RegionId = OldScheduleItemInput.RegionId
                ScheduleId = OldScheduleItemInput.ScheduleId
                LinkedRegionId = If(OldScheduleItemInput.LinkedRegionId <> "", OldScheduleItemInput.LinkedRegionId, OldScheduleItemInput.RegionId)
                LinkedScheduleId = If(OldScheduleItemInput.LinkedScheduleId > 0, OldScheduleItemInput.LinkedScheduleId, OldScheduleItemInput.ScheduleId)
                AScheduleItem = OldScheduleItemInput
            End If

            Dim UniqueRegionIds As New List(Of String)
            UniqueRegionIds.Add(RegionId)
            If LinkedRegionId <> "" AndAlso LinkedRegionId <> RegionId Then
                UniqueRegionIds.Add(LinkedRegionId)
            End If

            Dim IsLadderLeague As Boolean = False
            Dim OldScheduleItem As Schedule = Nothing

            Dim FullSchedule As New List(Of Schedule)
            Dim OldFullSchedule As new List(Of Schedule)


            Dim TUniqueRegionIds As New List(Of String)
            TUniqueRegionIds.AddRange(UniqueRegionIds)
            For Each RegionLeague In RegionLeagues
                If RegionLeague.RealLeagueId <> "" AndAlso Not TUniqueRegionIds.Contains(RegionLeague.RealLeagueId) Then TUniqueRegionIds.Add(RegionLeague.RealLeagueId)
            Next

            OfficialRegions = OfficialRegion.GetOfficialRegionsInRegionsHelper(TUniqueRegionIds, SqlConnection, SqlTransaction)
            Teams = Team.GetTeamsInRegionsHelper(TUniqueRegionIds, SqlConnection, SqlTransaction)

            Dim MoreUniqueRegionIds As New List(Of String)
            MoreUniqueRegionIds.AddRange(TUniqueRegionIds)
            For Each Team In Teams
                If Team.RealTeamId <> "" AndAlso Not MoreUniqueRegionIds.Contains(Team.RealTeamId) Then
                    MoreUniqueRegionIds.Add(Team.RealTeamId)
                End If
            Next
            For Each OfficialRegion In OfficialRegions
                If OfficialRegion.RealOfficialRegionId <> "" AndAlso Not MoreUniqueRegionIds.Contains(OfficialRegion.RealOfficialRegionId) Then
                    MoreUniqueRegionIds.Add(OfficialRegion.RealOfficialRegionId)
                End If
            Next

            Regions = RegionProperties.GetRegionPropertiesRegionIdsHelper(MoreUniqueRegionIds, SqlConnection, SqlTransaction)
            RegionLeagues = RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(RegionLeague.GetRegionsLeaguesHelper(MoreUniqueRegionIds, SqlConnection, SqlTransaction), New List(Of RegionLeaguePay))
            RegionUsers = RegionUser.LoadAllInRegionIdsHelper(MoreUniqueRegionIds, Username, SqlConnection, SqlTransaction)
            RegionParks = Park.GetParksInRegionsHelper(MoreUniqueRegionIds, SqlConnection, SqlTransaction)

            Dim MyRegionUser = RegionUsers.Find(Function(RU) RU.RealUsername.toLower = Username)

            If Regions.Count = 1 AndAlso Regions(0).RegionIsLadderLeague Then
                OldScheduleItem = GetSingleGameHelper(Regions(0).RegionId, ScheduleId, SqlConnection, SqlTransaction)

                If OldScheduleItem IsNot Nothing Then
                    SchedulePosition.GetSingleSchedulePositionFromRegionHelper(RegionId, OldScheduleItem, SqlConnection, SqlTransaction)

                    Dim UniqueOfficials = GetUniqueOfficialIdsFromSchedule({OldScheduleItem}.ToList, RegionUsers)
                    If Not UniqueOfficials.Contains(MyRegionUser.Username.ToLower) Then
                        Return New ErrorObject("CantEditNotYourOwnGame")
                    End If
                End if
                Dim UniqueOfficials2 = GetUniqueOfficialIdsFromSchedule({AScheduleItem}.ToList, RegionUsers)
                If Not UniqueOfficials2.Contains(MyRegionUser.Username.ToLower) Then
                    Return New ErrorObject("CantEditNotYourOwnGame")
                End If
                IsLadderLeague = True
            End If

            Dim ShowSimpleEdit = AScheduleItem.CanShowSimpleEdit(Username, Regions, RegionUsers, Teams)
            If Not ShowSimpleEdit.CanShowSimpleEdit AndAlso Not IsLadderLeague Then Return New ErrorObject("InvalidPermissions")

            Dim Region = Regions.Find(Function(R) R.RegionId = RegionId)

            Dim DBSchedule As Schedule = Nothing
            Dim DBLinkedSchedule As Schedule = Nothing
            Dim OldDBSchedule As Schedule = Nothing
            Dim OldDBLinkedSchedule As Schedule = Nothing
            Dim ScheduleGame = AScheduleItem
            Dim DBScheduleGame As Schedule = Nothing
            Dim OldDBScheduleGame As Schedule = Nothing

            Dim CurrentScheduleIds As New Dictionary(Of String, ScheduleId)
            For Each TUniqueRegionId In TUniqueRegionIds
                CurrentScheduleIds.Add(TUniqueRegionId, New ScheduleId(RegionId, SqlConnection, SqlTransaction))
            Next

            Dim DeleteSchedules As New List(Of Schedule)
            Dim InsertSchedules As New List(Of Schedule)
            Dim InsertSchedulePositions As New List(Of SchedulePositionFull)
            Dim InsertScheduleFines As New List(Of ScheduleFineFull)
            Dim InsertScheduleUserComments As New List(Of ScheduleUserCommentFull)
            Dim InsertScheduleUserCommentTeams As New List(Of ScheduleUserCommentTeamFull)
            Dim InsertScheduleCommentTeams As New List(Of ScheduleCommentTeamFull)
            Dim InsertScheduleCallups As New List(Of ScheduleCallupFull)
            Dim GamesWhosScoresNeedToBeUpdated As New List(Of Schedule)

            If Region.EntityType = "referee" Then
                DBScheduleGame = GetSingleGameHelper(ScheduleGame.RegionId, ScheduleGame.ScheduleId, SqlConnection, SqlTransaction)

                If DBScheduleGame Is Nothing Then
                    ScheduleGame.ScheduleId = CurrentScheduleIds(ScheduleGame.RegionId).ScheduleId
                    ScheduleGame.VersionId = 1
                    CurrentScheduleIds(ScheduleGame.RegionId).ScheduleId += 1
                End If

                If IsLadderLeague Then
                    Dim UniqueLadderLeagueRegionIds As New List(Of String)
                    For Each UniqueRegionId In UniqueRegionIds
                        Dim UniqueRegion = RegionProperties.GetItem(Regions, UniqueRegionId)
                        If UniqueRegion.RegionIsLadderLeague Then
                                UniqueLadderLeagueRegionIds.Add(UniqueRegionId)
                        End If
                    Next

                    If UniqueLadderLeagueRegionIds.Count > 0 Then
                        OldFullSchedule = GetMasterScheduleFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, SqlConnection, SqlTransaction)
                        SchedulePosition.GetMasterSchedulePositionFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, FullSchedule, SqlConnection, SqlTransaction)
                        ScheduleFine.GetMasterScheduleFineFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds,Date.MinValue, Date.MaxValue, FullSchedule, SqlConnection, SqlTransaction)
                        ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, FullSchedule, SqlConnection, SqlTransaction)
                        ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue,  FullSchedule, SqlConnection, SqlTransaction)
                    End if

                    If ScheduleItem Is Nothing Then
                        DeleteScheduleHelper(Regions, OldScheduleItemInput, RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, GameEmails, AssignorEmails, DateAdded, DeleteSchedules, InsertSchedules, GamesWhosScoresNeedToBeUpdated)
                    Else
                        If DBScheduleGame IsNot Nothing Then
                            SchedulePosition.GetSingleSchedulePositionFromRegionHelper(RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            ScheduleFine.GetSingleScheduleFineFromRegionHelper(RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            ScheduleUserComment.GetSingleScheduleUserCommentFromRegionHelper(RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            ScheduleUserCommentTeam.GetSingleScheduleUserCommentTeamFromRegionHelper(RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                            ScheduleCallup.GetSingleScheduleCallUpFromRegionHelper(RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                        End If

                        UpsertScheduleHelper(Regions, New ScheduleTemp().FromSchedule(ScheduleGame, OldScheduleItemInput), DBScheduleGame, CurrentScheduleIds(ScheduleItem.RegionId), RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, GameEmails, AssignorEmails, Date.UtcNow, DeleteSchedules, InsertSchedules, InsertSchedulePositions, InsertScheduleFines, InsertScheduleUserComments, InsertScheduleUserCommentTeams, InsertScheduleCommentTeams, InsertScheduleCallups, GamesWhosScoresNeedToBeUpdated)
                    End If
                Else
                    If DBScheduleGame IsNot Nothing Then
                        If DBScheduleGame.LinkedRegionId <> "" AndAlso ShowSimpleEdit.CanEditScore Then
                            DBLinkedSchedule = GetSingleGameHelper(DBSchedule.LinkedRegionId, DBSchedule.LinkedScheduleId, SqlConnection, SqlTransaction)
                            UmpireAssignor.ScheduleUserCommentTeam.GetSingleScheduleUserCommentTeamFromRegionHelper(DBSchedule.LinkedRegionId, DBLinkedSchedule, SqlConnection, SqlTransaction)
                            UmpireAssignor.ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(DBSchedule.LinkedRegionId, DBLinkedSchedule, SqlConnection, SqlTransaction)
                            UmpireAssignor.ScheduleCallup.GetSingleScheduleCallUpFromRegionHelper(DBSchedule.LinkedRegionId, DBLinkedSchedule, SqlConnection, SqlTransaction)

                            OldDBLinkedSchedule = DBLinkedSchedule.CloneItem()
                        End If

                        SchedulePosition.GetSingleSchedulePositionFromRegionHelper(ScheduleGame.RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                        ScheduleFine.GetSingleScheduleFineFromRegionHelper(ScheduleGame.RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                        UmpireAssignor.ScheduleUserComment.GetSingleScheduleUserCommentFromRegionHelper(ScheduleGame.RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                        UmpireAssignor.ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(ScheduleGame.RegionId, DBScheduleGame, SqlConnection, SqlTransaction)

                        OldDBSchedule = DBScheduleGame.CloneItem()

                        If ShowSimpleEdit.CanEditScore Then
                            DBScheduleGame.HomeTeamScore = ScheduleGame.HomeTeamScore
                            DBScheduleGame.HomeTeamScoreExtra = ScheduleGame.HomeTeamScoreExtra
                            DBScheduleGame.AwayTeamScore = ScheduleGame.AwayTeamScore
                            DBScheduleGame.AwayTeamScoreExtra = ScheduleGame.AwayTeamScoreExtra

                            If DBLinkedSchedule IsNot Nothing Then
                                DBLinkedSchedule.HomeTeamScore = ScheduleGame.HomeTeamScore
                                DBLinkedSchedule.HomeTeamScoreExtra = ScheduleGame.HomeTeamScoreExtra
                                DBLinkedSchedule.AwayTeamScore = ScheduleGame.AwayTeamScore
                                DBLinkedSchedule.AwayTeamScoreExtra = ScheduleGame.AwayTeamScoreExtra

                                UpsertScheduleHelper(Regions, New ScheduleTemp().FromSchedule(DBLinkedSchedule, OldDBLinkedSchedule), OldDBLinkedSchedule, CurrentScheduleIds(DBLinkedSchedule.RegionId), RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, GameEmails, AssignorEmails, Date.UtcNow, DeleteSchedules, InsertSchedules, InsertSchedulePositions, InsertScheduleFines, InsertScheduleUserComments, InsertScheduleUserCommentTeams, InsertScheduleCommentTeams, InsertScheduleCallups, GamesWhosScoresNeedToBeUpdated)
                            End If

                            UpsertScheduleHelper(Regions, New ScheduleTemp().FromSchedule(DBScheduleGame, OldDBSchedule), OldDBSchedule, CurrentScheduleIds(DBScheduleGame.RegionId), RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, GameEmails, AssignorEmails, Date.UtcNow, DeleteSchedules, InsertSchedules, InsertSchedulePositions, InsertScheduleFines, InsertScheduleUserComments, InsertScheduleUserCommentTeams, InsertScheduleCommentTeams, InsertScheduleCallups, GamesWhosScoresNeedToBeUpdated)
                        End If
                    End If
                End If
            ElseIf Region.EntityType = "team" Then
                DBScheduleGame = GetSingleGameHelper(LinkedRegionId, LinkedScheduleId, SqlConnection, SqlTransaction)
                If DBScheduleGame IsNot Nothing Then

                    UmpireAssignor.ScheduleUserCommentTeam.GetSingleScheduleUserCommentTeamFromRegionHelper(LinkedRegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                    UmpireAssignor.ScheduleCallup.GetSingleScheduleCallUpFromRegionHelper(LinkedRegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                    UmpireAssignor.ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(LinkedRegionId, DBScheduleGame, SqlConnection, SqlTransaction)

                    OldDBSchedule = DBScheduleGame.CloneItem()

                    If ShowSimpleEdit.CanEditScore Then
                        DBScheduleGame.HomeTeamScore = ScheduleGame.HomeTeamScore
                        DBScheduleGame.HomeTeamScoreExtra = ScheduleGame.HomeTeamScoreExtra
                        DBScheduleGame.AwayTeamScore = ScheduleGame.AwayTeamScore
                        DBScheduleGame.AwayTeamScoreExtra = ScheduleGame.AwayTeamScoreExtra
                    End If

                    If ShowSimpleEdit.CanEditGroupComment Then
                        DBScheduleGame.ScheduleCommentTeams.RemoveAll(Function(SC) SC.LinkedRegionId = ScheduleGame.RegionId)
                        DBScheduleGame.ScheduleCommentTeams.AddRange(ScheduleGame.ScheduleCommentTeams.FindAll(Function(SC) SC.LinkedRegionId = ScheduleGame.RegionId))
                        DBScheduleGame.ScheduleCommentTeams.Sort(ScheduleCommentTeam.BasicSorter)
                    End If

                    If ShowSimpleEdit.CanEditCallUps Then
                        DBScheduleGame.ScheduleCallUps.RemoveAll(Function(SC) SC.LinkedRegionId = ScheduleGame.RegionId)
                        DBScheduleGame.ScheduleCallUps.AddRange(ScheduleGame.ScheduleCallUps.FindAll(Function(SC) SC.LinkedRegionId = ScheduleGame.RegionId))
                        DBScheduleGame.ScheduleCallUps.Sort(ScheduleCallup.BasicSorter)
                    End If

                    If ShowSimpleEdit.CanEditUserComments Then
                        DBScheduleGame.ScheduleUserCommentTeams.RemoveAll(Function(SC) SC.LinkedRegionId = ScheduleGame.RegionId)
                        DBScheduleGame.ScheduleUserCommentTeams.AddRange(ScheduleGame.ScheduleUserCommentTeams.FindAll(Function(SC) SC.LinkedRegionId = ScheduleGame.RegionId))
                        DBScheduleGame.ScheduleUserCommentTeams.Sort(ScheduleUserCommentTeam.BasicSorter)
                    End If

                    If ShowSimpleEdit.CanCancelGame Then
                        DBScheduleGame.GameStatus = ScheduleGame.GameStatus
                    End If

                    UpsertScheduleHelper(Regions, New ScheduleTemp().FromSchedule(DBScheduleGame, OldDBSchedule), OldDBSchedule, CurrentScheduleIds(LinkedRegionId), RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, GameEmails, AssignorEmails, Date.UtcNow, DeleteSchedules, InsertSchedules, InsertSchedulePositions, InsertScheduleFines, InsertScheduleUserComments, InsertScheduleUserCommentTeams, InsertScheduleCommentTeams, InsertScheduleCallups, GamesWhosScoresNeedToBeUpdated)
                End If
            Else
                Return New ErrorObject("InvalidPermissions")
            End If

            UmpireAssignor.Schedule.BulkDelete(DeleteSchedules, SqlConnection, SqlTransaction)
            UmpireAssignor.Schedule.BulkInsert(InsertSchedules, SqlConnection, SqlTransaction)
            UmpireAssignor.Schedule.BulkInsertVersions(InsertSchedules, SqlConnection, SqlTransaction)
            UmpireAssignor.SchedulePositionFull.BulkInsert(InsertSchedulePositions, SqlConnection, SqlTransaction)
            UmpireAssignor.ScheduleFineFull.BulkInsert(InsertScheduleFines, SqlConnection, SqlTransaction)
            UmpireAssignor.ScheduleUserCommentFull.BulkInsert(InsertScheduleUserComments, SqlConnection, SqlTransaction)
            UmpireAssignor.ScheduleUserCommentTeamFull.BulkInsert(InsertScheduleUserCommentTeams, SqlConnection, SqlTransaction)
            UmpireAssignor.ScheduleCommentTeamFull.BulkInsert(InsertScheduleCommentTeams, SqlConnection, SqlTransaction)
            UmpireAssignor.ScheduleCallupFull.BulkInsert(InsertScheduleCallups, SqlConnection, SqlTransaction)

            UmpireAssignor.ScheduleId.UpsertHelper(CurrentScheduleIds.Values.ToList(), SqlConnection, SqlTransaction)

            FixGameEmails(GameEmails, AssignorEmails, SqlConnection, SqlTransaction)

            UpdateLadderLeagueAfterScheduleSave(UniqueRegionIds, Regions, RegionUsers, GamesWhosScoresNeedToBeUpdated, OldFullSchedule, FullSchedule, GameEmails, SqlConnection, SqlTransaction)
        Next
        Return Nothing
    End Function

    Public Shared Function SetCallups(Username As String, RegionId As String, LinkedRegionId As String, ScheduleId As String, Usernames As List(Of String)) As Object
        Username = Username.ToLower
        RegionId = RegionId.ToLower
        LinkedRegionId = LinkedRegionId.ToLower
        For I As Integer = 0 To Usernames.Count - 1
            Usernames(I) = Usernames(I).ToLower
        Next

        Dim CallUpEmails As New Dictionary(Of String, GameEmail)

        Dim DoPostUserComment As Boolean = True

        Dim Region As UmpireAssignor.RegionProperties = Nothing

        Dim Regions As New List(Of RegionProperties)

        Dim DateAdded As Date = Date.UtcNow
        Dim RegionGameIds As New List(Of String)

        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, Username, SqlConnection, SqlTransaction)

                Dim GameIsOk As Boolean = False
                If Assignor.IsExecutive Then GameIsOk = True

                If Not GameIsOk Then Return New ErrorObject("InvalidPermissions")

                RegionGameIds.Add(RegionId)
                If LinkedRegionId <> "" AndAlso RegionId <> LinkedRegionId Then
                    RegionGameIds.Add(LinkedRegionId)
                End If

                Dim RegionProperties As New List(Of UmpireAssignor.RegionProperties)
                RegionProperties = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(RegionGameIds, SqlConnection, SqlTransaction)
                Regions = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(RegionGameIds, SqlConnection, SqlTransaction)
                Region = Regions.Find(Function(R) R.RegionId = RegionId)

                If Region.EntityType <> "team" Then Return New ErrorObject("InvalidPermissions")

                Dim DBSchedule As Schedule = Nothing
                Dim OldDBSchedule As Schedule = Nothing
                Dim DBScheduleGame As Schedule = Nothing
                Dim OldDBScheduleGame As Schedule = Nothing

                If DoPostUserComment Then
                    DBScheduleGame = GetSingleGameHelper(LinkedRegionId, ScheduleId, SqlConnection, SqlTransaction)
                    If DBScheduleGame IsNot Nothing Then
                        OldDBScheduleGame = GetSingleGameHelper(LinkedRegionId, ScheduleId, SqlConnection, SqlTransaction)


                        DBScheduleGame.LinkedRegionId = ""
                        OldDBScheduleGame.LinkedRegionId = ""

                        UmpireAssignor.ScheduleUserCommentTeam.GetSingleScheduleUserCommentTeamFromRegionHelper(LinkedRegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                        UmpireAssignor.ScheduleUserCommentTeam.GetSingleScheduleUserCommentTeamFromRegionHelper(LinkedRegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)

                        UmpireAssignor.ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(LinkedRegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                        UmpireAssignor.ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(LinkedRegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)

                        UmpireAssignor.ScheduleCallup.GetSingleScheduleCallUpFromRegionHelper(LinkedRegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                        UmpireAssignor.ScheduleCallup.GetSingleScheduleCallUpFromRegionHelper(LinkedRegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)
                    Else
                        DoPostUserComment = False
                    End If
                End If

                OldDBSchedule = OldDBScheduleGame
                DBSchedule = DBScheduleGame

                Dim RegionUsers = RegionUser.LoadAllInRegionSimpleHelper(RegionId, SqlConnection, SqlTransaction)
                Dim RegionLeagues = RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(RegionLeague.GetRegionLeaguesHelper(RegionId, SqlConnection, SqlTransaction), New List(Of RegionLeaguePay))

                Dim TUniqueRegionIds As New List(Of String)
                If RegionId <> "" Then TUniqueRegionIds.Add(RegionId)
                For Each RegionLeague In RegionLeagues
                    If RegionLeague.RealLeagueId <> "" AndAlso Not TUniqueRegionIds.Contains(RegionLeague.RealLeagueId) Then TUniqueRegionIds.Add(RegionLeague.RealLeagueId)
                Next

                Dim TimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(Region.TimeZone)

                If DBSchedule.GameDate < Date.UtcNow.AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours) Then Return New ErrorObject("GameHasPassed")

                Dim RegionParks = Park.GetParksInRegionsHelper(TUniqueRegionIds, SqlConnection, SqlTransaction)
                Dim Teams = Team.GetTeamsInRegionsHelper(TUniqueRegionIds, SqlConnection, SqlTransaction)

                Dim CallupUsers As New List(Of RegionUser)
                For Each TUsername In Usernames
                    Dim TRegionUser = RegionUsers.Find(Function(RU) RU.Username = TUsername)
                    If TRegionUser IsNot Nothing Then
                        If CallupUsers.Find(Function(RU) RU.Username = TUsername) Is Nothing Then
                            CallupUsers.Add(TRegionUser)
                        End If
                    End If
                Next

                Dim OldVersionId = DBScheduleGame.VersionId

                Dim AddedCallups As New List(Of RegionUser)
                Dim RemovedCallups As New List(Of RegionUser)

                Regions = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(TUniqueRegionIds, SqlConnection, SqlTransaction)

                Dim DeleteSchedules As New List(Of Schedule)
                Dim InsertSchedules As New List(Of Schedule)
                Dim InsertSchedulePositions As New List(Of SchedulePositionFull)
                Dim InsertScheduleFines As New List(Of ScheduleFineFull)
                Dim InsertScheduleUserComments As New List(Of ScheduleUserCommentFull)
                Dim InsertScheduleUserCommentTeams As New List(Of ScheduleUserCommentTeamFull)
                Dim InsertScheduleCommentTeams As New List(Of ScheduleCommentTeamFull)
                Dim InsertScheduleCallups As New List(Of ScheduleCallupFull)
                Dim GamesWhosScoresNeedUpdated As New List(Of Schedule)

                If DoPostUserComment Then
                    Dim FoundUserComment As Boolean = False

                    For Each ScheduleCallup In DBScheduleGame.ScheduleCallUps
                        Dim TCallupUser = CallupUsers.Find(Function(CU) CU.RegionId = RegionId AndAlso CU.Username = ScheduleCallup.Username)
                        If TCallupUser Is Nothing Then
                            RemovedCallups.Add(RegionUsers.Find(Function(RU) RU.RegionId = RegionId AndAlso RU.Username = ScheduleCallup.Username))
                        End If
                    Next

                    For Each CallupUser In CallupUsers
                        Dim TScheduleCallUp = DBScheduleGame.ScheduleCallUps.Find(Function(SC) SC.LinkedRegionId = RegionId AndAlso SC.Username = CallupUser.Username)
                        If TScheduleCallUp Is Nothing Then
                            AddedCallups.Add(CallupUser)
                            DBScheduleGame.ScheduleCallUps.Add(New ScheduleCallup With {.LinkedRegionId = RegionId, .Username = CallupUser.Username})
                        End If
                    Next

                    DBScheduleGame.ScheduleCallUps = New List(Of ScheduleCallup)
                    For Each AddedCallup In AddedCallups
                        DBScheduleGame.ScheduleCallUps.Add(New ScheduleCallup With {.LinkedRegionId = RegionId, .Username = AddedCallup.Username})
                    Next
                    For Each RemovedCallup In RemovedCallups
                        DBScheduleGame.ScheduleCallUps.Add(New ScheduleCallup With {.LinkedRegionId = RegionId, .Username = RemovedCallup.Username, .IsDeleted = True})
                    Next

                    Dim CurrentScheduleId As New ScheduleId

                    Dim DBScheduleTemp = New ScheduleTemp()
                    DBScheduleTemp.FromSchedule(DBScheduleGame, OldDBSchedule)

                    If OldDBScheduleGame.IsDifferent(DBScheduleTemp) Then

                        UpsertScheduleHelper(Regions, DBScheduleTemp, OldDBScheduleGame, CurrentScheduleId, RegionUsers, RegionParks, RegionLeagues, Teams, New List(Of OfficialRegion), Nothing, New List(Of GameEmail), DateAdded, DeleteSchedules, InsertSchedules, InsertSchedulePositions, InsertScheduleFines, InsertScheduleUserComments, InsertScheduleUserCommentTeams, InsertScheduleCommentTeams, InsertScheduleCallups, GamesWhosScoresNeedUpdated)

                        Dim ConvertedScheduleItem = ConvertToLinkScheduleItem(DBScheduleGame, RegionId, RegionLeagues)

                        For Each AddedCallup In AddedCallups
                            Dim CallUpEmail As GameEmail
                            If Not CallUpEmails.ContainsKey(AddedCallup.Username) Then
                                CallUpEmail = New GameEmail With {
                                    .IsAddedCallup = True,
                                    .RegionProperties = RegionProperties,
                                    .RegionUsers = RegionUsers,
                                    .RegionLeagues = RegionLeagues,
                                    .RegionParks = RegionParks,
                                    .Teams = Teams,
                                    .User = User.GetUserInfoHelper(AddedCallup.RealUsername, SqlConnection, SqlTransaction),
                                    .ScheduleData = New List(Of ScheduleDataEmail),
                                    .RegionUser = AddedCallup
                                }
                                CallUpEmails.Add(AddedCallup.Username, CallUpEmail)
                            Else
                                CallUpEmail = CallUpEmails(AddedCallup.Username)
                            End If
                            CallUpEmail.ScheduleData.Add(New ScheduleDataEmail(AddedCallup, ConvertedScheduleItem, Nothing, "", Region, False))
                        Next

                        For Each RemovedCallup In RemovedCallups
                            Dim CallUpEmail As GameEmail
                            If Not CallUpEmails.ContainsKey(RemovedCallup.Username) Then
                                CallUpEmail = New GameEmail With {
                                    .IsRemovedCallup = True,
                                    .RegionProperties = RegionProperties,
                                    .RegionUsers = RegionUsers,
                                    .RegionLeagues = RegionLeagues,
                                    .RegionParks = RegionParks,
                                    .Teams = Teams,
                                    .User = User.GetUserInfoHelper(RemovedCallup.RealUsername, SqlConnection, SqlTransaction),
                                    .ScheduleData = New List(Of ScheduleDataEmail),
                                    .RegionUser = RemovedCallup
                                }
                                CallUpEmails.Add(RemovedCallup.Username, CallUpEmail)
                            Else
                                CallUpEmail = CallUpEmails(RemovedCallup.Username)
                            End If
                            CallUpEmail.ScheduleData.Add(New ScheduleDataEmail(RemovedCallup, Nothing, ConvertedScheduleItem, "", Region, False))
                        Next

                    End If
                End If

                UmpireAssignor.Schedule.BulkDelete(DeleteSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.Schedule.BulkInsert(InsertSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.Schedule.BulkInsertVersions(InsertSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.SchedulePositionFull.BulkInsert(InsertSchedulePositions, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleFineFull.BulkInsert(InsertScheduleFines, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleUserCommentFull.BulkInsert(InsertScheduleUserComments, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleUserCommentTeamFull.BulkInsert(InsertScheduleUserCommentTeams, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleCommentTeamFull.BulkInsert(InsertScheduleCommentTeams, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleCallupFull.BulkInsert(InsertScheduleCallups, SqlConnection, SqlTransaction)

                SqlTransaction.Commit()
            End Using
        End Using

        For Each GameEmail In CallUpEmails
            If GameEmail.Value.ScheduleData.Count > 0 Then
                For Each Item In GameEmail.Value.ScheduleData
                    If Item.NewScheduleItem IsNot Nothing Then Item.NewScheduleItem.MergeTeamData(Item.Region.RegionId)
                    If Item.OldScheduleItem IsNot Nothing Then Item.OldScheduleItem.MergeTeamData(Item.Region.RegionId)
                Next
                GameEmail.Value.Send(Region)
            End If
        Next

        Return New With {
           .Success = True
        }
        'Catch E As Exception
        'Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Shared Function RemoveCallups(Username As String, RegionId As String, LinkedRegionId As String, ScheduleId As String, Usernames As List(Of String)) As Object
        Username = Username.ToLower
        RegionId = RegionId.ToLower
        LinkedRegionId = LinkedRegionId.ToLower
        For I As Integer = 0 To Usernames.Count - 1
            Usernames(I) = Usernames(I).ToLower
        Next

        Dim GameEmails As New List(Of GameEmail)
        Dim CallUpEmails As New Dictionary(Of String, GameEmail)

        Dim DoPostUserComment As Boolean = True

        Dim Region As UmpireAssignor.RegionProperties = Nothing

        Dim Regions As New List(Of RegionProperties)

        Dim DateAdded As Date = Date.UtcNow

        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(LinkedRegionId, Username, SqlConnection, SqlTransaction)

                Dim GameIsOk As Boolean = False
                If Assignor.IsExecutive Then GameIsOk = True

                If Not GameIsOk Then Return New ErrorObject("InvalidPermissions")

                Dim RegionProperties As UmpireAssignor.RegionProperties = Nothing
                Region = UmpireAssignor.RegionProperties.GetRegionPropertiesHelper(LinkedRegionId, SqlConnection, SqlTransaction)

                If Region.EntityType <> "Team" Then Return New ErrorObject("InvalidPermissions")

                Dim DBSchedule As Schedule = Nothing
                Dim OldDBSchedule As Schedule = Nothing
                Dim DBScheduleGame As Schedule = Nothing
                Dim OldDBScheduleGame As Schedule = Nothing

                If DoPostUserComment Then
                    DBScheduleGame = GetSingleGameHelper(RegionId, ScheduleId, SqlConnection, SqlTransaction)
                    If DBScheduleGame IsNot Nothing Then
                        OldDBScheduleGame = GetSingleGameHelper(RegionId, ScheduleId, SqlConnection, SqlTransaction)


                        DBScheduleGame.LinkedRegionId = ""
                        OldDBScheduleGame.LinkedRegionId = ""

                        UmpireAssignor.ScheduleUserCommentTeam.GetSingleScheduleUserCommentTeamFromRegionHelper(RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                        UmpireAssignor.ScheduleUserCommentTeam.GetSingleScheduleUserCommentTeamFromRegionHelper(RegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)

                        UmpireAssignor.ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                        UmpireAssignor.ScheduleCommentTeam.GetSingleScheduleCommentTeamFromRegionHelper(RegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)

                        UmpireAssignor.ScheduleCallup.GetSingleScheduleCallUpFromRegionHelper(RegionId, DBScheduleGame, SqlConnection, SqlTransaction)
                        UmpireAssignor.ScheduleCallup.GetSingleScheduleCallUpFromRegionHelper(RegionId, OldDBScheduleGame, SqlConnection, SqlTransaction)
                    Else
                        DoPostUserComment = False
                    End If
                End If

                Dim DeleteSchedules As New List(Of Schedule)
                Dim InsertSchedules As New List(Of Schedule)
                Dim InsertSchedulePositions As New List(Of SchedulePositionFull)
                Dim InsertScheduleFines As New List(Of ScheduleFineFull)
                Dim InsertScheduleUserComments As New List(Of ScheduleUserCommentFull)
                Dim InsertScheduleUserCommentTeams As New List(Of ScheduleUserCommentTeamFull)
                Dim InsertScheduleCommentTeams As New List(Of ScheduleCommentTeamFull)
                Dim InsertScheduleCallups As New List(Of ScheduleCallupFull)
                Dim GamesWhosScoresNeedToBeUpdated As New List(Of Schedule)

                OldDBSchedule = OldDBScheduleGame
                DBSchedule = DBScheduleGame

                Dim RegionLeagues = RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(RegionLeague.GetRegionLeaguesHelper(LinkedRegionId, SqlConnection, SqlTransaction), New List(Of RegionLeaguePay))
                Dim RegionUsers = RegionUser.LoadAllInRegionSimpleHelper(LinkedRegionId, SqlConnection, SqlTransaction)

                Dim TUniqueRegionIds As New List(Of String)
                If RegionId <> "" Then TUniqueRegionIds.Add(RegionId)
                For Each RegionLeague In RegionLeagues
                    If RegionLeague.RealLeagueId <> "" AndAlso Not TUniqueRegionIds.Contains(RegionLeague.RealLeagueId) Then TUniqueRegionIds.Add(RegionLeague.RealLeagueId)
                Next

                Regions = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(TUniqueRegionIds, SqlConnection, SqlTransaction)

                Dim RegionParks = Park.GetParksInRegionsHelper(TUniqueRegionIds, SqlConnection, SqlTransaction)
                Dim Teams = Team.GetTeamsInRegionsHelper(TUniqueRegionIds, SqlConnection, SqlTransaction)

                Dim CallupUsers As New List(Of RegionUser)
                For Each TUsername In Usernames
                    Dim TRegionUser = RegionUsers.Find(Function(RU) RU.Username = TUsername)
                    If TRegionUser IsNot Nothing Then
                        If CallupUsers.Find(Function(RU) RU.Username = TUsername) Is Nothing Then
                            CallupUsers.Add(TRegionUser)
                        End If
                    End If
                Next

                Dim OldVersionId = DBScheduleGame.VersionId

                Dim RemovedCallups As New List(Of RegionUser)

                If DoPostUserComment Then
                    Dim FoundUserComment As Boolean = False

                    For Each CallupUser In CallupUsers
                        Dim TScheduleCallUp = DBScheduleGame.ScheduleCallUps.FindIndex(Function(SC) SC.LinkedRegionId = LinkedRegionId AndAlso SC.Username = CallupUser.Username)
                        If TScheduleCallUp <> -1 Then
                            DBScheduleGame.ScheduleCallUps.RemoveAt(TScheduleCallUp)
                            RemovedCallups.Add(CallupUser)
                        End If
                    Next

                    Dim CurrentScheduleId As New ScheduleId

                    Dim DBScheduleTemp = New ScheduleTemp()
                    DBScheduleTemp.FromSchedule(DBScheduleGame, OldDBScheduleGame)

                    If OldDBScheduleGame.IsDifferent(DBScheduleTemp) Then

                        UpsertScheduleHelper({Region}.ToList(), DBScheduleTemp, OldDBScheduleGame, CurrentScheduleId, RegionUsers, RegionParks, RegionLeagues, Teams, New List(Of OfficialRegion), GameEmails, New List(Of GameEmail), DateAdded, DeleteSchedules, InsertSchedules, InsertSchedulePositions, InsertScheduleFines, InsertScheduleUserComments, InsertScheduleUserCommentTeams, InsertScheduleCommentTeams, InsertScheduleCallups, GamesWhosScoresNeedToBeUpdated)

                        For Each RemovedCallup In RemovedCallups
                            Dim CallUpEmail As GameEmail
                            If Not CallUpEmails.ContainsKey(RemovedCallup.RealUsername) Then
                                CallUpEmail = New GameEmail With {
                                    .IsRemovedCallup = True,
                                    .RegionProperties = Regions,
                                    .RegionUsers = RegionUsers,
                                    .RegionLeagues = RegionLeagues,
                                    .RegionParks = RegionParks,
                                    .User = User.GetUserInfoHelper(RemovedCallup.RealUsername, SqlConnection, SqlTransaction),
                                    .ScheduleData = New List(Of ScheduleDataEmail),
                                    .RegionUser = RemovedCallup
                                }
                                CallUpEmails.Add(Assignor.RealUsername, CallUpEmail)
                            Else
                                CallUpEmail = CallUpEmails(Assignor.RealUsername)
                            End If
                            CallUpEmail.ScheduleData.Add(New ScheduleDataEmail(RemovedCallup, Nothing, DBScheduleGame, "", Region, False))
                        Next
                    End If
                End If

                UmpireAssignor.Schedule.BulkDelete(DeleteSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.Schedule.BulkInsert(InsertSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.Schedule.BulkInsertVersions(InsertSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.SchedulePositionFull.BulkInsert(InsertSchedulePositions, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleFineFull.BulkInsert(InsertScheduleFines, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleUserCommentFull.BulkInsert(InsertScheduleUserComments, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleUserCommentTeamFull.BulkInsert(InsertScheduleUserCommentTeams, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleCommentTeamFull.BulkInsert(InsertScheduleCommentTeams, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleCallupFull.BulkInsert(InsertScheduleCallups, SqlConnection, SqlTransaction)

                SqlTransaction.Commit()
            End Using
        End Using

        Dim UniqueRegionIds = {RegionId}.ToList()

        For Each GameEmail In CallUpEmails
            If GameEmail.Value.ScheduleData.Count > 0 Then
                For Each Item In GameEmail.Value.ScheduleData
                    If Item.NewScheduleItem IsNot Nothing Then Item.NewScheduleItem.MergeTeamData(Item.Region.RegionId)
                    If Item.OldScheduleItem IsNot Nothing Then Item.OldScheduleItem.MergeTeamData(Item.Region.RegionId)
                Next
                GameEmail.Value.Send(Nothing)
            End If
        Next

        Return New With {
           .Success = True
        }
        'Catch E As Exception
        'Return New ErrorObject("UnknownError")
        'End Try
    End Function


    Public Shared Function IsInvolvedOfficialInGameEmail(InvolvedOfficial As RegionUser, GE As GameEmail) As Boolean
        If GE.RegionUser.LastName.ToLower = InvolvedOfficial.LastName.ToLower AndAlso
   GE.RegionUser.FirstName.ToLower = InvolvedOfficial.FirstName.ToLower AndAlso
   (GE.RegionUser.RealUsername.ToLower = InvolvedOfficial.RealUsername.ToLower OrElse
    (((GE.RegionUser.RealUsername.ToLower <> "" AndAlso InvolvedOfficial.RealUsername.ToLower = "") OrElse (GE.RegionUser.RealUsername.ToLower = "" AndAlso InvolvedOfficial.RealUsername.ToLower <> "")) AndAlso GE.RegionUser.Email.ToLower = InvolvedOfficial.Email.ToLower AndAlso InvolvedOfficial.Email.ToLower <> "")) Then
            Return True
        End If
        Return False
    End Function

    Public Shared Sub UpsertScheduleNotifyTeamHelper(Region As RegionProperties, CheckIsTeamOnGame As Boolean, NewSchedule As Schedule, OldSchedule As Schedule, RegionUsers As List(Of RegionUser), Regions As List(Of RegionProperties), RegionLeagues As List(Of RegionLeaguePayContracted), RegionParks As List(Of Park), Teams As List(Of Team), OfficialRegions As List(Of OfficialRegion), GameEmails As List(Of GameEmail))
        Dim GameChanged = RequiredGameNotification(NewSchedule, OldSchedule)

        Dim IsOnOldSchedule As Boolean = False
        Dim IsOnNewSchedule As Boolean = False

        If CheckIsTeamOnGame Then
            Dim HomeTeamNewGame As Team = Nothing
            Dim AwayTeamNewGame As Team = Nothing

            If NewSchedule IsNot Nothing Then
                HomeTeamNewGame = Teams.Find(Function(T) T.RealTeamId <> "" AndAlso T.RegionId = NewSchedule.LinkedRegionId AndAlso T.TeamId = NewSchedule.HomeTeam.Trim().ToLower AndAlso T.RealTeamId = Region.RegionId)
                AwayTeamNewGame = Teams.Find(Function(T) T.RealTeamId <> "" AndAlso T.RegionId = NewSchedule.LinkedRegionId AndAlso T.TeamId = NewSchedule.AwayTeam.Trim().ToLower AndAlso T.RealTeamId = Region.RegionId)
            End If

            Dim HomeTeamOldGame As Team = Nothing
            Dim AwayTeamOldGame As Team = Nothing

            If OldSchedule IsNot Nothing Then
                HomeTeamOldGame = Teams.Find(Function(T) T.RealTeamId <> "" AndAlso T.RegionId = OldSchedule.LinkedRegionId AndAlso T.TeamId = OldSchedule.HomeTeam.Trim().ToLower AndAlso T.RealTeamId = Region.RegionId)
                AwayTeamOldGame = Teams.Find(Function(T) T.RealTeamId <> "" AndAlso T.RegionId = OldSchedule.LinkedRegionId AndAlso T.TeamId = OldSchedule.AwayTeam.Trim().ToLower AndAlso T.RealTeamId = Region.RegionId)
            End If

            If HomeTeamNewGame IsNot Nothing OrElse AwayTeamNewGame IsNot Nothing Then IsOnNewSchedule = True
            If HomeTeamOldGame IsNot Nothing OrElse AwayTeamOldGame IsNot Nothing Then IsOnOldSchedule = True
        Else
            IsOnOldSchedule = OldSchedule IsNot Nothing
            IsOnNewSchedule = NewSchedule IsNot Nothing
        End If

        Dim TOldSchedule = If(IsOnOldSchedule, OldSchedule, Nothing)
        Dim TNewSchedule = If(IsOnNewSchedule, NewSchedule, Nothing)

        For Each RegionUser In RegionUsers
            If RegionUser.RegionId = Region.RegionId AndAlso (RegionUser.IsCoach OrElse RegionUser.IsPlayer OrElse RegionUser.IsCallup) Then
                Dim TIsOnOldSchedule As Boolean = IsOnOldSchedule
                Dim TIsOnNewSchedule As Boolean = IsOnNewSchedule

                Dim TTOldSchedule = If(TIsOnOldSchedule, OldSchedule, Nothing)
                Dim TTNewSchedule = If(TIsOnNewSchedule, NewSchedule, Nothing)

                If RegionUser.IsCallup Then
                    If TIsOnOldSchedule Then TIsOnOldSchedule = OldSchedule.ScheduleCallUps.Any(Function(SC) SC.Username = RegionUser.Username)
                    If TIsOnNewSchedule Then TIsOnNewSchedule = NewSchedule.ScheduleCallUps.Any(Function(SC) SC.Username = RegionUser.Username)

                    TTOldSchedule = If(TIsOnOldSchedule, OldSchedule, Nothing)
                    TTNewSchedule = If(TIsOnNewSchedule, NewSchedule, Nothing)
                End If

                If (TIsOnNewSchedule OrElse TIsOnOldSchedule) AndAlso (GameChanged OrElse TIsOnNewSchedule <> TIsOnOldSchedule) Then
                    Dim RealUser As User = Nothing
                    If RegionUser.RealUsername <> "" Then
                        'RealUser = User.GetUserInfoHelper(RegionUser.RealUsername, SQLConnection, SQLTransaction)
                    End If

                    Dim TGameEmail = GameEmails.Find(Function(GE)
                                                         Return IsInvolvedOfficialInGameEmail(RegionUser, GE)
                                                     End Function)

                    If TGameEmail Is Nothing Then
                        TGameEmail = New GameEmail With {
                            .User = RealUser,
                            .RegionProperties = Regions,
                            .RegionUser = RegionUser,
                            .RegionUsers = RegionUsers,
                            .RegionParks = RegionParks,
                            .RegionLeagues = RegionLeagues,
                            .Teams = Teams,
                            .OfficialRegions = OfficialRegions
                        }
                        GameEmails.Add(TGameEmail)
                    End If

                    TGameEmail.ScheduleData.Add(New ScheduleDataEmail(
                        RegionUser,
                        TTNewSchedule,
                        TTOldSchedule,
                        False,
                        Region,
                        False
                    ))
                End If

            End If
        Next
    End Sub

    Private Shared Sub EmailChange(Region As RegionProperties, Regions As List(Of RegionProperties), NewSchedule As Schedule, OldSchedule As Schedule, RegionUsers As List(Of RegionUser), RegionParks As List(Of Park), RegionLeagues As List(Of RegionLeaguePayContracted), OfficialRegions As List(Of OfficialRegion), Teams As List(Of Team), GameEmails As List(Of GameEmail), AssignorEmails As List(Of GameEmail))
        If Not Region.IsDemo Then
            If Region.EntityType = "referee" Then
                Dim InvolvedOfficials = GetInvolvedOfficials(NewSchedule, OldSchedule, RegionUsers)
                Dim GameChanged = RequiredGameNotification(NewSchedule, OldSchedule)

                For Each InvolvedOfficial In InvolvedOfficials
                    If InvolvedOfficial.Email = "" AndAlso (InvolvedOfficial.AlternateEmails Is Nothing OrElse InvolvedOfficial.AlternateEmails.Count = 0) Then Continue For
                    If InvolvedOfficial.Username.Trim = "" Then Continue For
                    Dim ChangeFine = RequiredGameNotificationFine(NewSchedule, OldSchedule, InvolvedOfficial.Username)
                    Dim ChangePosition = RequiredGameNotificationPosition(NewSchedule, OldSchedule, InvolvedOfficial.Username)
                    If (ChangePosition = "SamePosition" AndAlso (GameChanged OrElse ChangeFine)) OrElse ChangePosition <> "SamePosition" Then

                        If ChangePosition = "SamePosition" Then ChangePosition = "ChangedPosition"
                        Dim RealUser As User = Nothing
                        If InvolvedOfficial.RealUsername <> "" AndAlso InvolvedOfficial.IsLinked Then
                            'RealUser = User.GetUserInfoHelper(InvolvedOfficial.RealUsername, SQLConnection, SQLTransaction)
                        End If

                        Dim TGameEmail = GameEmails.Find(Function(GE)
                                                             Return IsInvolvedOfficialInGameEmail(InvolvedOfficial, GE)
                                                         End Function)

                        If TGameEmail Is Nothing Then
                            TGameEmail = New GameEmail With {
                                .User = RealUser,
                                .RegionProperties = Regions,
                                .RegionUser = InvolvedOfficial,
                                .RegionUsers = RegionUsers,
                                .RegionParks = RegionParks,
                                .RegionLeagues = RegionLeagues,
                                .Teams = Teams,
                                .OfficialRegions = OfficialRegions
                            }
                            GameEmails.Add(TGameEmail)
                        End If

                        TGameEmail.ScheduleData.Add(New ScheduleDataEmail(
                            InvolvedOfficial,
                            NewSchedule,
                            OldSchedule,
                            ChangePosition,
                            Region,
                            False
                        ))
                    End If
                Next
            ElseIf Region.EntityType = "team" Then
                UpsertScheduleNotifyTeamHelper(Region, False, NewSchedule, OldSchedule, RegionUsers, Regions, RegionLeagues, RegionParks, Teams, OfficialRegions, GameEmails)
            ElseIf Region.EntityType = "league" Then
                Dim OfficialRegionIds As New List(Of String)
                Dim TeamRegionIds As New List(Of String)

                Dim TScheduleList As New List(Of Schedule)
                If OldSchedule IsNot Nothing Then TScheduleList.Add(OldSchedule)
                If NewSchedule IsNot Nothing Then TScheduleList.Add(NewSchedule)

                For Each TScheduleItem In TScheduleList
                    Dim HomeTeam = Teams.Find(Function(Team) Team.RealTeamId <> "" AndAlso Team.RegionId = Region.RegionId AndAlso Team.TeamId = TScheduleItem.HomeTeam.Trim().ToLower())
                    Dim AwayTeam = Teams.Find(Function(Team) Team.RealTeamId <> "" AndAlso Team.RegionId = Region.RegionId AndAlso Team.TeamId = TScheduleItem.AwayTeam.Trim().ToLower())

                    Dim OfficialRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = Region.RegionId AndAlso O.OfficialRegionId = TScheduleItem.OfficialRegionId.Trim().ToLower())
                    Dim ScorekeeperRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = Region.RegionId AndAlso O.OfficialRegionId = TScheduleItem.ScorekeeperRegionId.Trim().ToLower())
                    Dim SupervisorRegion = OfficialRegions.Find(Function(O) O.RealOfficialRegionId <> "" AndAlso O.RegionId = Region.RegionId AndAlso O.OfficialRegionId = TScheduleItem.SupervisorRegionId.Trim().ToLower())

                    If HomeTeam IsNot Nothing AndAlso Not TeamRegionIds.Contains(HomeTeam.RealTeamId) Then TeamRegionIds.Add(HomeTeam.RealTeamId)
                    If AwayTeam IsNot Nothing AndAlso Not TeamRegionIds.Contains(AwayTeam.RealTeamId) Then TeamRegionIds.Add(AwayTeam.RealTeamId)

                    If OfficialRegion IsNot Nothing AndAlso Not OfficialRegionIds.Contains(OfficialRegion.RealOfficialRegionId) Then OfficialRegionIds.Add(OfficialRegion.RealOfficialRegionId)
                    If ScorekeeperRegion IsNot Nothing AndAlso Not OfficialRegionIds.Contains(ScorekeeperRegion.RealOfficialRegionId) Then OfficialRegionIds.Add(ScorekeeperRegion.RealOfficialRegionId)
                    If SupervisorRegion IsNot Nothing AndAlso Not OfficialRegionIds.Contains(SupervisorRegion.RealOfficialRegionId) Then OfficialRegionIds.Add(ScorekeeperRegion.RealOfficialRegionId)
                Next

                For Each TeamRegionId In TeamRegionIds
                    Dim TOldSchedule As Schedule = Nothing
                    Dim TNewSchedule As Schedule = Nothing

                    Dim TRegion As RegionProperties = Regions.Find(Function(R) R.RegionId = TeamRegionId)
                    If OldSchedule IsNot Nothing Then
                        TOldSchedule = ConvertToLinkScheduleItem(OldSchedule, TeamRegionId, RegionLeagues)
                    End If

                    If NewSchedule IsNot Nothing Then
                        TNewSchedule = ConvertToLinkScheduleItem(NewSchedule, TeamRegionId, RegionLeagues)
                    End If

                    If TRegion IsNot Nothing Then
                        UpsertScheduleNotifyTeamHelper(TRegion, True, TNewSchedule, TOldSchedule, RegionUsers, Regions, RegionLeagues, RegionParks, Teams, OfficialRegions, GameEmails)
                    End If
                Next

                For Each TOfficialRegionId In OfficialRegionIds
                    Dim TOldSchedule As Schedule = Nothing
                    Dim TNewSchedule As Schedule = Nothing

                    Dim TRegion As RegionProperties = Regions.Find(Function(R) R.RegionId = TOfficialRegionId)

                    If TRegion Is Nothing Then Continue For

                    If OldSchedule IsNot Nothing Then
                        TOldSchedule = ConvertToLinkScheduleItem(OldSchedule, TOfficialRegionId, RegionLeagues)
                    End If

                    If NewSchedule IsNot Nothing Then
                        TNewSchedule = ConvertToLinkScheduleItem(NewSchedule, TOfficialRegionId, RegionLeagues)
                    End If

                    Dim GameChanged = RequiredGameNotification(NewSchedule, OldSchedule)

                    If GameChanged Then
                        For Each InvolvedOfficial In RegionUsers
                            If InvolvedOfficial.RegionId = TOfficialRegionId AndAlso InvolvedOfficial.IsAssignor Then
                                If InvolvedOfficial.Email = "" AndAlso (InvolvedOfficial.AlternateEmails Is Nothing OrElse InvolvedOfficial.AlternateEmails.Count = 0) Then Continue For
                                If InvolvedOfficial.Username.Trim = "" Then Continue For

                                Dim RealUser As User = Nothing
                                If InvolvedOfficial.RealUsername <> "" AndAlso InvolvedOfficial.IsLinked Then
                                    'RealUser = User.GetUserInfoHelper(InvolvedOfficial.RealUsername, SQLConnection, SQLTransaction)
                                End If

                                Dim TGameEmail = AssignorEmails.Find(Function(GE)
                                                                         Return IsInvolvedOfficialInGameEmail(InvolvedOfficial, GE)
                                                                     End Function)

                                If TGameEmail Is Nothing Then
                                    TGameEmail = New GameEmail With {
                                        .IsAssignorEmail = True,
                                        .User = RealUser,
                                        .RegionProperties = Regions,
                                        .RegionUser = InvolvedOfficial,
                                        .RegionUsers = RegionUsers,
                                        .RegionParks = RegionParks,
                                        .RegionLeagues = RegionLeagues,
                                        .Teams = Teams,
                                        .OfficialRegions = OfficialRegions
                                    }
                                    AssignorEmails.Add(TGameEmail)
                                End If

                                TGameEmail.ScheduleData.Add(New ScheduleDataEmail(
                                    InvolvedOfficial,
                                    TNewSchedule,
                                    TOldSchedule,
                                    False,
                                    TRegion,
                                    False
                                ))
                            End If
                        Next
                    End If
                Next
            End If
        End If
    End Sub

    Public Shared Sub UpsertScheduleHelper(Regions As List(Of RegionProperties), NewScheduleTemp As ScheduleTemp, OldSchedule As Schedule, ByRef CurrentScheduleId As ScheduleId, RegionUsers As List(Of RegionUser), RegionParks As List(Of Park), RegionLeagues As List(Of RegionLeaguePayContracted), Teams As List(Of Team), OfficialRegions As List(Of OfficialRegion), GameEmails As List(Of GameEmail), AssignorEmails As List(Of GameEmail), DateAdded As Date, DeleteSchedules As List(Of Schedule), InsertSchedules As List(Of Schedule), InsertSchedulePositions As List(Of SchedulePositionFull), InsertScheduleFines As List(Of ScheduleFineFull), InsertScheduleUserComments As List(Of ScheduleUserCommentFull), InsertScheduleUserCommentTeams As List(Of ScheduleUserCommentTeamFull), InsertScheduleCommentTeams As List(Of ScheduleCommentTeamFull), InsertScheduleCallups As List(Of ScheduleCallupFull), GamesThatScoresNeedToBeUpdated As List(Of Schedule))
        If NewScheduleTemp.IsDeleted Then
            DeleteScheduleHelper(Regions, OldSchedule, RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, GameEmails, AssignorEmails, DateAdded, DeleteSchedules, InsertSchedules, GamesThatScoresNeedToBeUpdated)
            Exit sub
        End If

        Dim AddCurrentIndex As Boolean = True

        Dim Region As RegionProperties = Nothing
        If NewScheduleTemp IsNot Nothing Then
            Region = Regions.Find(Function(R) R.RegionId = NewScheduleTemp.RegionId)
        ElseIf OldSchedule IsNot Nothing Then
            Region = Regions.Find(Function(R) R.RegionId = OldSchedule.RegionId)
        End If

        Dim NewSchedule = NewScheduleTemp.ToScheduleFromSchedule(Region, OldSchedule)

        If OldSchedule Is Nothing Then
            NewSchedule.VersionId = 1
        Else
            If NewSchedule IsNot Nothing Then
                If NewSchedule.StatLinks = "" Then
                    NewSchedule.StatLinks = OldSchedule.StatLinks
                End If
            End If
            Dim OD As Date = OldSchedule.GameDate
            Dim ND As Date = NewSchedule.GameDate
            If OD.Year <> ND.Year OrElse OD.Month <> ND.Month OrElse OD.Day <> ND.Day Then
                DeleteScheduleHelper(Regions, OldSchedule, RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, GameEmails, AssignorEmails, DateAdded, DeleteSchedules, InsertSchedules, GamesThatScoresNeedToBeUpdated)
                NewSchedule.ScheduleId = CurrentScheduleId.ScheduleId
                NewSchedule.VersionId = 1
                OldSchedule = Nothing
            Else
                NewSchedule.VersionId = OldSchedule.VersionId + 1
                AddCurrentIndex = False
            End If
        End If

        DeleteSchedules.Add(New Schedule With {
            .RegionId = NewSchedule.RegionId.ToLower,
            .ScheduleId = NewSchedule.ScheduleId
        })

        InsertSchedules.Add(New Schedule With {
            .RegionId = NewSchedule.RegionId.ToLower,
            .ScheduleId = NewSchedule.ScheduleId,
            .VersionId = NewSchedule.VersionId,
            .LinkedRegionId = NewSchedule.LinkedRegionId,
            .LinkedScheduleId = NewSchedule.LinkedScheduleId,
            .GameNumber = NewSchedule.GameNumber,
            .GameType = NewSchedule.GameType,
            .LeagueId = NewSchedule.LeagueId,
            .HomeTeam = NewSchedule.HomeTeam,
            .HomeTeamScore = NewSchedule.HomeTeamScore,
            .HomeTeamScoreExtra = NewSchedule.HomeTeamScoreExtra,
            .AwayTeam = NewSchedule.AwayTeam,
            .AwayTeamScore = NewSchedule.AwayTeamScore,
            .AwayTeamScoreExtra = NewSchedule.AwayTeamScoreExtra,
            .GameDate = NewSchedule.GameDate,
            .ParkId = NewSchedule.ParkId,
            .CrewType = NewSchedule.CrewType,
            .GameStatus = NewSchedule.GameStatus,
            .GameComment = NewSchedule.GameComment,
            .GameScore = NewSchedule.GameScore,
            .OfficialRegionId = NewSchedule.OfficialRegionId,
            .ScorekeeperRegionId = NewSchedule.ScorekeeperRegionId,
            .SupervisorRegionId = NewSchedule.SupervisorRegionId,
            .StatLinks = NewSchedule.StatLinks,
            .DateAdded = DateAdded
        })

        SchedulePosition.UpsertSchedulePositionHelper(Region.RegionId.ToLower, NewSchedule, InsertSchedulePositions)
        ScheduleFine.UpsertScheduleFineHelper(Region.RegionId.ToLower, NewSchedule, InsertScheduleFines)
        ScheduleUserComment.UpsertScheduleUserCommentHelper(Region.RegionId.ToLower, NewSchedule, InsertScheduleUserComments)
        ScheduleUserCommentTeam.UpsertScheduleUserCommentTeamHelper(Region.RegionId.ToLower, NewSchedule, InsertScheduleUserCommentTeams)
        ScheduleCommentTeam.UpsertScheduleCommentTeamHelper(Region.RegionId.ToLower, NewSchedule, InsertScheduleCommentTeams)
        ScheduleCallup.UpsertScheduleCallUpHelper(Region.RegionId.ToLower, NewSchedule, InsertScheduleCallups)

        If Region.RegionIsLadderLeague Then
            If OldSchedule Is Nothing Then
                GamesThatScoresNeedToBeUpdated.Add(NewSchedule)
            ElseIf NewSchedule Is Nothing Then
                GamesThatScoresNeedToBeUpdated.Add(OldSchedule)
            ElseIf OldSchedule IsNot Nothing AndAlso NewSchedule IsNot Nothing Then
                If GameScore.IsDifferentGameScoreAndGame(OldSchedule, NewSchedule) OrElse GameScore.IsDifferentScoreOnly(OldSchedule.GameScore, NewSchedule.GameScore) Then
                    GamesThatScoresNeedToBeUpdated.Add(NewSchedule)
                End If
            End If
        End If

        If GameEmails IsNot Nothing Then
            EmailChange(Region, Regions, NewSchedule, OldSchedule, RegionUsers, RegionParks, RegionLeagues, OfficialRegions, Teams, GameEmails, AssignorEmails)
        End If

        If AddCurrentIndex Then
            CurrentScheduleId.ScheduleId += 1
        End If
    End Sub

    Public Shared Sub DeleteTempScheduleItemHelper(Schedule As Schedule, DeleteScheduleTemps As List(Of ScheduleTemp))
        'CommandCount += 1
        'CommandSB.Append(String.Format("DELETE FROM ScheduleTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId AND ScheduleId = @ScheduleId{0}; ", CommandCount))
        'CommandSB.Append(String.Format("DELETE FROM SchedulePositionTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId AND ScheduleId = @ScheduleId{0}; ", CommandCount))
        'CommandSB.Append(String.Format("DELETE FROM ScheduleFineTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId AND ScheduleId = @ScheduleId{0}; ", CommandCount))
        'CommandSB.Append(String.Format("DELETE FROM ScheduleUserCommentTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId AND ScheduleId = @ScheduleId{0}; ", CommandCount))
        'CommandSB.Append(String.Format("DELETE FROM ScheduleUserCommentTeamTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId AND ScheduleId = @ScheduleId{0}; ", CommandCount))
        'CommandSB.Append(String.Format("DELETE FROM ScheduleCommentTeamTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId AND ScheduleId = @ScheduleId{0}; ", CommandCount))
        'CommandSB.Append(String.Format("DELETE FROM ScheduleCallUpTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId AND ScheduleId = @ScheduleId{0}; ", CommandCount))

        'SQLCommand.Parameters.Add(New SqlParameter("RegionId" & CommandCount, Schedule.RegionId))
        'SQLCommand.Parameters.Add(New SqlParameter("ScheduleId" & CommandCount, Schedule.ScheduleId))
        DeleteScheduleTemps.Add(New ScheduleTemp With {.RegionId = Schedule.RegionId, .ScheduleId = Schedule.ScheduleId})
    End Sub

    Public Shared Sub MarkTempIsDeleted(OldSchedule As Schedule, InsertScheduleTemps As List(Of ScheduleTemp), Optional OldScheduleId As Integer = -1)
        InsertScheduleTemps.Add(New ScheduleTemp With {
            .RegionId = OldSchedule.RegionId,
            .ScheduleId = OldSchedule.ScheduleId,
            .LinkedRegionId = OldSchedule.LinkedRegionId,
            .LinkedRegionIdModified = True,
            .LinkedScheduleId = OldSchedule.LinkedScheduleId,
            .LinkedScheduleIdModified = True,
            .GameNumber = "",
            .GameNumberModified = False,
            .GameType = "",
            .GameTypeModified = False,
            .LeagueId = "",
            .LeagueIdModified = False,
            .HomeTeam = "",
            .HomeTeamModified = False,
            .HomeTeamScore = "",
            .HomeTeamScoreModified = False,
            .HomeTeamScoreExtra = New Dictionary(Of String, String),
            .HomeTeamScoreExtraModified = False,
            .AwayTeam = "",
            .AwayTeamModified = False,
            .AwayTeamScore = "",
            .AwayTeamScoreModified = False,
            .AwayTeamScoreExtra = New Dictionary(Of String, String),
            .AwayTeamScoreExtraModified = False,
            .GameDate = OldSchedule.GameDate,
            .GameDateModified = False,
            .ParkId = "",
            .ParkIdModified = False,
            .CrewType = New Dictionary(Of String, String),
            .CrewTypeModified = False,
            .GameStatus = "",
            .GameStatusModified = False,
            .GameComment = "",
            .GameCommentModified = False,
            .GameScore = Nothing,
            .GameScoreModified = False,
            .OfficialRegionId = "",
            .OfficialRegionIdModified = False,
            .ScorekeeperRegionId = "",
            .ScorekeeperRegionIdModified = False,
            .SupervisorRegionId = "",
            .SupervisorRegionIdModified = False,
            .OldScheduleId = OldScheduleId,
            .IsDeleted = True
        })
    End Sub

    Public Shared Sub CreateTempScheduleItemHelper(NewSchedule As Schedule, OldSchedule As Schedule, Region As RegionProperties, Regions As List(Of RegionProperties), InsertScheduleTemps As List(Of ScheduleTemp), InsertSchedulePositionsTemp As List(Of SchedulePositionFull), InsertScheduleFinesTemp As List(Of ScheduleFineFull), InsertScheduleUserCommentsTemp As List(Of ScheduleUserCommentFull), InsertScheduleUserCommentTeamsTemp As List(Of ScheduleUserCommentTeamFull), InsertScheduleCommentTeamsTemp As List(Of ScheduleCommentTeamFull), InsertScheduleCallupsTemp As List(Of ScheduleCallupFull), Optional OldScheduleId As Integer = -1)
        Dim CrewTypeModified As Boolean = OldSchedule Is Nothing OrElse NewSchedule.CrewType.Count <> OldSchedule.CrewType.Count
        If Not CrewTypeModified Then
            For Each KeyValue In NewSchedule.CrewType
                If Not OldSchedule.CrewType.ContainsKey(KeyValue.Key) Then
                    CrewTypeModified = True
                    Exit For
                End If
                If OldSchedule.CrewType(KeyValue.Key) <> KeyValue.Value Then
                    CrewTypeModified = True
                    Exit For
                End If
            Next
        End If

        Dim TRegion = Region
        If NewSchedule.LinkedRegionId <> "" Then
            TRegion = Regions.Find(Function(RP) RP.RegionId = NewSchedule.LinkedRegionId)
            If TRegion Is Nothing Then TRegion = Region
        End If

        Dim StandingCategory As StandingItem = StatGame.StandingCategories.Find(Function(SC) SC.StandingIndex = TRegion.StandingIndex).StandingItem

        Dim HomeTeamScoreExtraModified As Boolean = OldSchedule Is Nothing
        If Not HomeTeamScoreExtraModified Then
            For Each StandingFormula In StandingCategory.StandingFormulas
                If StandingFormula.VariableType <> "" Then
                    HomeTeamScoreExtraModified = HomeTeamScoreExtraModified OrElse If(NewSchedule.HomeTeamScoreExtra.ContainsKey(StandingFormula.StandingId), NewSchedule.HomeTeamScoreExtra(StandingFormula.StandingId), "0") = If(OldSchedule.HomeTeamScoreExtra.ContainsKey(StandingFormula.StandingId), OldSchedule.HomeTeamScoreExtra(StandingFormula.StandingId), "0")
                End If
            Next
        End If

        Dim AwayTeamScoreExtraModified As Boolean = OldSchedule Is Nothing
        If Not AwayTeamScoreExtraModified Then
            For Each StandingFormula In StandingCategory.StandingFormulas
                If StandingFormula.VariableType <> "" Then
                    AwayTeamScoreExtraModified = AwayTeamScoreExtraModified OrElse If(NewSchedule.AwayTeamScoreExtra.ContainsKey(StandingFormula.StandingId), NewSchedule.AwayTeamScoreExtra(StandingFormula.StandingId), "0") = If(OldSchedule.AwayTeamScoreExtra.ContainsKey(StandingFormula.StandingId), OldSchedule.AwayTeamScoreExtra(StandingFormula.StandingId), "0")
                End If
            Next
        End If

        InsertScheduleTemps.Add(New ScheduleTemp With {
            .RegionId = NewSchedule.RegionId,
            .ScheduleId = NewSchedule.ScheduleId,
            .LinkedRegionId = NewSchedule.LinkedRegionId,
            .LinkedRegionIdModified = OldSchedule Is Nothing OrElse NewSchedule.LinkedRegionId <> OldSchedule.LinkedRegionId,
            .LinkedScheduleId = NewSchedule.LinkedScheduleId,
            .LinkedScheduleIdModified = OldSchedule Is Nothing OrElse NewSchedule.LinkedScheduleId <> OldSchedule.LinkedScheduleId,
            .GameNumber = NewSchedule.GameNumber,
            .GameNumberModified = OldSchedule Is Nothing OrElse NewSchedule.GameNumber <> OldSchedule.GameNumber,
            .GameType = NewSchedule.GameType,
            .GameTypeModified = OldSchedule Is Nothing OrElse NewSchedule.GameType <> OldSchedule.GameType,
            .LeagueId = NewSchedule.LeagueId,
            .LeagueIdModified = OldSchedule Is Nothing OrElse NewSchedule.LeagueId <> OldSchedule.LeagueId,
            .HomeTeam = NewSchedule.HomeTeam,
            .HomeTeamModified = OldSchedule Is Nothing OrElse NewSchedule.HomeTeam <> OldSchedule.HomeTeam,
            .HomeTeamScore = NewSchedule.HomeTeamScore,
            .HomeTeamScoreModified = OldSchedule Is Nothing OrElse NewSchedule.HomeTeamScore <> OldSchedule.HomeTeamScore,
            .HomeTeamScoreExtra = NewSchedule.HomeTeamScoreExtra,
            .HomeTeamScoreExtraModified = HomeTeamScoreExtraModified,
            .AwayTeam = NewSchedule.AwayTeam,
            .AwayTeamModified = OldSchedule Is Nothing OrElse NewSchedule.AwayTeam <> OldSchedule.AwayTeam,
            .AwayTeamScore = NewSchedule.AwayTeamScore,
            .AwayTeamScoreModified = OldSchedule Is Nothing OrElse NewSchedule.AwayTeamScore <> OldSchedule.AwayTeamScore,
            .AwayTeamScoreExtra = NewSchedule.AwayTeamScoreExtra,
            .AwayTeamScoreExtraModified = AwayTeamScoreExtraModified,
            .GameDate = NewSchedule.GameDate,
            .GameDateModified = OldSchedule Is Nothing OrElse NewSchedule.GameDate <> OldSchedule.GameDate,
            .ParkId = NewSchedule.ParkId,
            .ParkIdModified = OldSchedule Is Nothing OrElse NewSchedule.ParkId <> OldSchedule.ParkId,
            .CrewType = NewSchedule.CrewType,
            .CrewTypeModified = OldSchedule Is Nothing OrElse CrewTypeModified,
            .GameStatus = NewSchedule.GameStatus,
            .GameStatusModified = OldSchedule Is Nothing OrElse NewSchedule.GameStatus <> OldSchedule.GameStatus,
            .GameComment = NewSchedule.GameComment,
            .GameCommentModified = OldSchedule Is Nothing OrElse NewSchedule.GameComment <> OldSchedule.GameComment,
            .GameScore = NewSchedule.GameScore,
            .GameScoreModified = OldSchedule Is Nothing OrElse GameScore.IsDifferentScoreOnly(NewSchedule.GameScore, OldSchedule.GameScore),
            .OfficialRegionId = NewSchedule.OfficialRegionId,
            .OfficialRegionIdModified = OldSchedule Is Nothing OrElse NewSchedule.OfficialRegionId <> OldSchedule.OfficialRegionId,
            .ScorekeeperRegionId = NewSchedule.ScorekeeperRegionId,
            .ScorekeeperRegionIdModified = OldSchedule Is Nothing OrElse NewSchedule.ScorekeeperRegionId <> OldSchedule.ScorekeeperRegionId,
            .SupervisorRegionId = NewSchedule.SupervisorRegionId,
            .SupervisorRegionIdModified = OldSchedule Is Nothing OrElse NewSchedule.SupervisorRegionId <> OldSchedule.SupervisorRegionId,
            .OldScheduleId = OldScheduleId,
            .IsDeleted = False
        })


        Dim Crews = RegionLeaguePayContracted.GetSportCrewPositions()(Region.Sport)
        Dim CrewsScorekeeper = RegionLeaguePayContracted.GetSportCrewPositionsScorekeepers()(Region.Sport)
        Dim CrewsSupervisor = RegionLeaguePayContracted.GetSportCrewPositionsSupervisors()(Region.Sport)

        Dim Positions As New List(Of String)
        Dim PositionsScorekeeper As New List(Of String)
        Dim PositionsSupervisor As New List(Of String)

        If Region.HasOfficials AndAlso NewSchedule.CrewType.ContainsKey("umpire") Then
            Positions = Crews(NewSchedule.CrewType("umpire"))
        End If

        If Region.HasScorekeepers AndAlso NewSchedule.CrewType.ContainsKey("scorekeeper") Then
            PositionsScorekeeper = CrewsScorekeeper(NewSchedule.CrewType("scorekeeper"))
        End If

        If Region.HasSupervisors AndAlso NewSchedule.CrewType.ContainsKey("supervisor") Then
            PositionsSupervisor = CrewsSupervisor(NewSchedule.CrewType("supervisor"))
        End If

        Dim NewSchedulePositionsTemp = NewSchedule.SchedulePositions
        NewSchedule.SchedulePositions = New List(Of SchedulePosition)
        For Each Position In Positions
            Dim NewSchedulePosition = NewSchedulePositionsTemp.Find(Function(V1) V1.PositionId = Position)
            If NewSchedulePosition Is Nothing Then
                NewSchedule.SchedulePositions.Add(New SchedulePosition With {
                    .PositionId = Position,
                    .OfficialId = ""
                })
            Else
                NewSchedule.SchedulePositions.Add(NewSchedulePosition)
            End If
        Next

        For Each Position In PositionsScorekeeper
            Dim NewSchedulePosition = NewSchedulePositionsTemp.Find(Function(V1) V1.PositionId = Position)
            If NewSchedulePosition Is Nothing Then
                NewSchedule.SchedulePositions.Add(New SchedulePosition With {
                    .PositionId = Position,
                    .OfficialId = ""
                })
            Else
                NewSchedule.SchedulePositions.Add(NewSchedulePosition)
            End If
        Next

        For Each Position In PositionsSupervisor
            Dim NewSchedulePosition = NewSchedulePositionsTemp.Find(Function(V1) V1.PositionId = Position)
            If NewSchedulePosition Is Nothing Then
                NewSchedule.SchedulePositions.Add(New SchedulePosition With {
                    .PositionId = Position,
                    .OfficialId = ""
                })
            Else
                NewSchedule.SchedulePositions.Add(NewSchedulePosition)
            End If
        Next

        If OldSchedule IsNot Nothing Then
            Dim OldSchedulePositionsTemp = OldSchedule.SchedulePositions
            OldSchedule.SchedulePositions = New List(Of SchedulePosition)
            For Each Position In Positions
                Dim OldSchedulePosition = OldSchedulePositionsTemp.Find(Function(V1) V1.PositionId = Position)
                If OldSchedulePosition Is Nothing Then
                    OldSchedule.SchedulePositions.Add(New SchedulePosition With {
                        .PositionId = Position,
                        .OfficialId = ""
                    })
                Else
                    OldSchedule.SchedulePositions.Add(OldSchedulePosition)
                End If
            Next

            For Each Position In PositionsScorekeeper
                Dim OldSchedulePosition = OldSchedulePositionsTemp.Find(Function(V1) V1.PositionId = Position)
                If OldSchedulePosition Is Nothing Then
                    OldSchedule.SchedulePositions.Add(New SchedulePosition With {
                        .PositionId = Position,
                        .OfficialId = ""
                    })
                Else
                    OldSchedule.SchedulePositions.Add(OldSchedulePosition)
                End If
            Next

            For Each Position In PositionsSupervisor
                Dim OldSchedulePosition = OldSchedulePositionsTemp.Find(Function(V1) V1.PositionId = Position)
                If OldSchedulePosition Is Nothing Then
                    OldSchedule.SchedulePositions.Add(New SchedulePosition With {
                        .PositionId = Position,
                        .OfficialId = ""
                    })
                Else
                    OldSchedule.SchedulePositions.Add(OldSchedulePosition)
                End If
            Next

            For I As Integer = 0 To NewSchedule.SchedulePositions.Count - 1
                If NewSchedule.SchedulePositions(I).OfficialId <> OldSchedule.SchedulePositions(I).OfficialId Then
                    InsertSchedulePositionsTemp.Add(New SchedulePositionFull With {
                        .RegionId = NewSchedule.RegionId,
                        .ScheduleId = NewSchedule.ScheduleId,
                        .PositionId = NewSchedule.SchedulePositions(I).PositionId,
                        .OfficialId = NewSchedule.SchedulePositions(I).OfficialId.Trim()
                    })
                End If
            Next
        Else
            For I As Integer = 0 To NewSchedule.SchedulePositions.Count - 1
                InsertSchedulePositionsTemp.Add(New SchedulePositionFull With {
                    .RegionId = NewSchedule.RegionId,
                    .ScheduleId = NewSchedule.ScheduleId,
                    .PositionId = NewSchedule.SchedulePositions(I).PositionId,
                    .OfficialId = NewSchedule.SchedulePositions(I).OfficialId.Trim()
                })
            Next
        End If

        If OldSchedule Is Nothing Then
            For Each ScheduleFine In NewSchedule.ScheduleFines
                InsertScheduleFinesTemp.Add(New ScheduleFineFull With {
                    .RegionId = NewSchedule.RegionId,
                    .ScheduleId = NewSchedule.ScheduleId,
                    .OfficialId = ScheduleFine.OfficialId.Trim(),
                    .Amount = ScheduleFine.Amount,
                    .Comment = ScheduleFine.Comment,
                    .IsDeleted = False
                })
            Next
        Else
            NewSchedule.ScheduleFines.Sort(ScheduleFine.BasicSorter)
            OldSchedule.ScheduleFines.Sort(ScheduleFine.BasicSorter)

            PublicCode.ProcessListPairs(
                NewSchedule.ScheduleFines,
                OldSchedule.ScheduleFines,
                AddressOf ScheduleFine.BasicComparer,
                Sub(V1)
                    InsertScheduleFinesTemp.Add(New ScheduleFineFull With {
                        .RegionId = NewSchedule.RegionId,
                        .ScheduleId = NewSchedule.ScheduleId,
                        .OfficialId = V1.OfficialId.Trim(),
                        .Amount = V1.Amount,
                        .Comment = V1.Comment,
                        .IsDeleted = False
                    })
                End Sub,
                Sub(V2)
                    InsertScheduleFinesTemp.Add(New ScheduleFineFull With {
                        .RegionId = NewSchedule.RegionId,
                        .ScheduleId = NewSchedule.ScheduleId,
                        .OfficialId = V2.OfficialId.Trim(),
                        .Amount = V2.Amount,
                        .Comment = V2.Comment,
                        .IsDeleted = True
                    })
                End Sub,
                Sub(V1, V2)
                    If V1.Comment <> V2.Comment OrElse V1.Amount <> V2.Amount Then
                        InsertScheduleFinesTemp.Add(New ScheduleFineFull With {
                            .RegionId = NewSchedule.RegionId,
                            .ScheduleId = NewSchedule.ScheduleId,
                            .OfficialId = V1.OfficialId.Trim(),
                            .Amount = V1.Amount,
                            .Comment = V1.Comment,
                            .IsDeleted = False
                        })
                    End If
                End Sub
            )
        End If

        If Region.EntityType = "referee" Then
            For I As Integer = NewSchedule.ScheduleUserComments.Count - 1 To 0 Step -1
                If NewSchedule.ScheduleUserComments(I).Comment = "" Then
                    NewSchedule.ScheduleUserComments.RemoveAt(I)
                End If
            Next

            If OldSchedule Is Nothing Then
                For Each ScheduleUserComment In NewSchedule.ScheduleUserComments
                    InsertScheduleUserCommentsTemp.Add(New ScheduleUserCommentFull With {
                        .RegionId = NewSchedule.RegionId,
                        .ScheduleId = NewSchedule.ScheduleId,
                        .OfficialId = ScheduleUserComment.OfficialId.Trim(),
                        .Comment = ScheduleUserComment.Comment,
                        .IsDeleted = False
                    })
                Next
            Else
                NewSchedule.ScheduleUserComments.Sort(ScheduleUserComment.BasicSorter)
                OldSchedule.ScheduleUserComments.Sort(ScheduleUserComment.BasicSorter)

                PublicCode.ProcessListPairs(
                    NewSchedule.ScheduleUserComments,
                    OldSchedule.ScheduleUserComments,
                    AddressOf ScheduleUserComment.BasicComparer,
                    Sub(V1)
                        InsertScheduleUserCommentsTemp.Add(New ScheduleUserCommentFull With {
                            .RegionId = NewSchedule.RegionId,
                            .ScheduleId = NewSchedule.ScheduleId,
                            .OfficialId = V1.OfficialId.Trim(),
                            .Comment = V1.Comment,
                            .IsDeleted = False
                        })
                    End Sub,
                    Sub(V2)
                        InsertScheduleUserCommentsTemp.Add(New ScheduleUserCommentFull With {
                            .RegionId = NewSchedule.RegionId,
                            .ScheduleId = NewSchedule.ScheduleId,
                            .OfficialId = V2.OfficialId.Trim(),
                            .Comment = V2.Comment,
                            .IsDeleted = True
                        })
                    End Sub,
                    Sub(V1, V2)
                        If V1.Comment <> V2.Comment Then
                            InsertScheduleUserCommentsTemp.Add(New ScheduleUserCommentFull With {
                                .RegionId = NewSchedule.RegionId,
                                .ScheduleId = NewSchedule.ScheduleId,
                                .OfficialId = V1.OfficialId.Trim(),
                                .Comment = V1.Comment,
                                .IsDeleted = False
                            })
                        End If
                    End Sub
                )
            End If
        End If

        If Region.EntityType = "team" Then
            For I As Integer = NewSchedule.ScheduleUserComments.Count - 1 To 0 Step -1
                If NewSchedule.ScheduleUserComments(I).Comment = "" Then
                    NewSchedule.ScheduleUserComments.RemoveAt(I)
                End If
            Next

            If OldSchedule Is Nothing Then
                For Each ScheduleUserCommentTeam In NewSchedule.ScheduleUserComments
                    InsertScheduleUserCommentTeamsTemp.Add(New ScheduleUserCommentTeamFull With {
                        .RegionId = NewSchedule.RegionId,
                        .ScheduleId = NewSchedule.ScheduleId,
                        .LinkedRegionId = NewSchedule.RegionId,
                        .OfficialId = ScheduleUserCommentTeam.OfficialId.Trim(),
                        .Comment = ScheduleUserCommentTeam.Comment,
                        .IsDeleted = False
                    })
                Next
            Else
                NewSchedule.ScheduleUserComments.Sort(ScheduleUserComment.BasicSorter)
                OldSchedule.ScheduleUserComments.Sort(ScheduleUserComment.BasicSorter)

                PublicCode.ProcessListPairs(
                    NewSchedule.ScheduleUserComments,
                    OldSchedule.ScheduleUserComments,
                    AddressOf ScheduleUserComment.BasicComparer,
                    Sub(V1)
                        InsertScheduleUserCommentTeamsTemp.Add(New ScheduleUserCommentTeamFull With {
                            .RegionId = NewSchedule.RegionId,
                            .ScheduleId = NewSchedule.ScheduleId,
                            .LinkedRegionId = NewSchedule.RegionId,
                            .OfficialId = V1.OfficialId.Trim(),
                            .Comment = V1.Comment,
                            .IsDeleted = False
                        })
                    End Sub,
                    Sub(V2)
                        InsertScheduleUserCommentTeamsTemp.Add(New ScheduleUserCommentTeamFull With {
                            .RegionId = NewSchedule.RegionId,
                            .ScheduleId = NewSchedule.ScheduleId,
                            .LinkedRegionId = NewSchedule.RegionId,
                            .OfficialId = V2.OfficialId.Trim(),
                            .Comment = V2.Comment,
                            .IsDeleted = True
                        })
                    End Sub,
                    Sub(V1, V2)
                        If V1.Comment <> V2.Comment Then
                            InsertScheduleUserCommentTeamsTemp.Add(New ScheduleUserCommentTeamFull With {
                                .RegionId = NewSchedule.RegionId,
                                .ScheduleId = NewSchedule.ScheduleId,
                                .LinkedRegionId = NewSchedule.RegionId,
                                .OfficialId = V1.OfficialId.Trim(),
                                .Comment = V1.Comment,
                                .IsDeleted = False
                            })
                        End If
                    End Sub
                )

            End If
        End If

        For I As Integer = NewSchedule.ScheduleCommentTeams.Count - 1 To 0 Step -1
            If NewSchedule.ScheduleCommentTeams(I).Comment = "" Then
                NewSchedule.ScheduleCommentTeams.RemoveAt(I)
            End If
        Next

        If OldSchedule Is Nothing Then
            For Each ScheduleCommentTeam In NewSchedule.ScheduleCommentTeams
                InsertScheduleCommentTeamsTemp.Add(New ScheduleCommentTeamFull With {
                    .RegionId = NewSchedule.RegionId,
                    .ScheduleId = NewSchedule.ScheduleId,
                    .LinkedRegionId = NewSchedule.RegionId,
                    .Comment = ScheduleCommentTeam.Comment,
                    .IsDeleted = False
                })
            Next
        Else
            NewSchedule.ScheduleCommentTeams.Sort(ScheduleCommentTeam.BasicSorter)
            OldSchedule.ScheduleCommentTeams.Sort(ScheduleCommentTeam.BasicSorter)

            PublicCode.ProcessListPairs(
                NewSchedule.ScheduleCommentTeams,
                OldSchedule.ScheduleCommentTeams,
                AddressOf ScheduleCommentTeam.BasicComparer,
                Sub(V1)
                    InsertScheduleCommentTeamsTemp.Add(New ScheduleCommentTeamFull With {
                        .RegionId = NewSchedule.RegionId,
                        .ScheduleId = NewSchedule.ScheduleId,
                        .LinkedRegionId = NewSchedule.RegionId,
                        .Comment = V1.Comment,
                        .IsDeleted = False
                    })
                End Sub,
                Sub(V2)
                    InsertScheduleCommentTeamsTemp.Add(New ScheduleCommentTeamFull With {
                        .RegionId = NewSchedule.RegionId,
                        .ScheduleId = NewSchedule.ScheduleId,
                        .LinkedRegionId = NewSchedule.RegionId,
                        .Comment = V2.Comment,
                        .IsDeleted = True
                    })
                End Sub,
                Sub(V1, V2)
                    If V1.Comment <> V2.Comment Then
                        InsertScheduleCommentTeamsTemp.Add(New ScheduleCommentTeamFull With {
                            .RegionId = NewSchedule.RegionId,
                            .ScheduleId = NewSchedule.ScheduleId,
                            .LinkedRegionId = NewSchedule.RegionId,
                            .Comment = V1.Comment,
                            .IsDeleted = False
                        })
                    End If
                End Sub
            )
        End If

        If OldSchedule Is Nothing Then
            For Each ScheduleCallUp In NewSchedule.ScheduleCallUps
                InsertScheduleCallupsTemp.Add(New ScheduleCallupFull With {
                    .RegionId = NewSchedule.RegionId,
                    .ScheduleId = NewSchedule.ScheduleId,
                    .LinkedRegionId = NewSchedule.RegionId,
                    .Username = ScheduleCallUp.Username.Trim(),
                    .IsDeleted = False
                })
            Next
        Else
            NewSchedule.ScheduleCallUps.Sort(ScheduleCallup.BasicSorter)
            OldSchedule.ScheduleCallUps.Sort(ScheduleCallup.BasicSorter)

            PublicCode.ProcessListPairs(
                NewSchedule.ScheduleCallUps,
                OldSchedule.ScheduleCallUps,
                AddressOf ScheduleCallup.BasicComparer,
                Sub(V1)
                    InsertScheduleCallupsTemp.Add(New ScheduleCallupFull With {
                        .RegionId = NewSchedule.RegionId,
                        .ScheduleId = NewSchedule.ScheduleId,
                        .LinkedRegionId = NewSchedule.RegionId,
                        .Username = V1.Username.Trim(),
                        .IsDeleted = False
                    })
                End Sub,
                Sub(V2)
                    InsertScheduleCallupsTemp.Add(New ScheduleCallupFull With {
                        .RegionId = NewSchedule.RegionId,
                        .ScheduleId = NewSchedule.ScheduleId,
                        .LinkedRegionId = NewSchedule.RegionId,
                        .Username = V2.Username.Trim(),
                        .IsDeleted = True
                    })
                End Sub,
                Sub(V1, V2)
                End Sub
            )
        End If
    End Sub

    Public Shared Function IsSameDate(GameDate1 As Date, GameDate2 As Date) As Boolean
        Return GameDate1.Year = GameDate2.Year AndAlso GameDate1.Month = GameDate2.Month AndAlso GameDate1.Day = GameDate2.Day
    End Function

    Public Shared Function UpsertToTempHelper(AssignorId As String, Region As RegionProperties, Regions As List(Of RegionProperties), NewSchedule As Schedule, OldSchedule As Schedule, OldNewSchedule As ScheduleTemp, ByRef CurrentScheduleId As ScheduleId, InsertScheduleTemps As List(Of ScheduleTemp), InsertSchedulePositionsTemp As List(Of SchedulePositionFull), InsertScheduleFinesTemp As List(Of ScheduleFineFull), InsertScheduleUserCommentsTemp As List(Of ScheduleUserCommentFull), InsertScheduleUserCommentTeamsTemp As List(Of ScheduleUserCommentTeamFull), InsertScheduleCommentTeamsTemp As List(Of ScheduleCommentTeamFull), InsertScheduleCallupsTemp As List(Of ScheduleCallupFull), DeleteScheduleTemps As List(Of ScheduleTemp)) As Integer
        Dim ScheduleId As Integer = -1

        Dim NewOldDifferent As Boolean
        Dim NewNewDifferent As Boolean

        If NewSchedule IsNot Nothing AndAlso OldSchedule IsNot Nothing Then
            NewSchedule.LinkedRegionId = OldSchedule.LinkedRegionId
            NewSchedule.LinkedScheduleId = OldSchedule.LinkedScheduleId

            If NewSchedule.LinkedRegionId <> "" Then
                NewSchedule.GameComment = OldSchedule.GameComment
            End If
        End If

        If NewSchedule Is Nothing Then
            NewOldDifferent = OldSchedule IsNot Nothing
            NewNewDifferent = OldNewSchedule IsNot Nothing
        Else
            NewOldDifferent = NewSchedule.IsDifferent(OldSchedule)
            If OldNewSchedule IsNot Nothing Then
                NewNewDifferent = NewSchedule.IsDifferent(OldNewSchedule)
            Else
                NewNewDifferent = False
            End If
        End If

        If NewSchedule IsNot Nothing Then
            Dim TRegion = Region
            If NewSchedule.LinkedRegionId <> "" Then
                TRegion = Regions.Find(Function(RU) RU.RegionId = NewSchedule.LinkedRegionId)
                If TRegion Is Nothing Then TRegion = Region
            End If

            Dim StandingCategory As StandingItem = StatGame.StandingCategories.Find(Function(SC) SC.StandingIndex = TRegion.StandingIndex).StandingItem

            Dim THomeTeamScoreExtra As New Dictionary(Of String, String)
            Dim TAwayTeamScoreExtra As New Dictionary(Of String, String)

            For Each StandingFormula In StandingCategory.StandingFormulas
                If StandingFormula.VariableType <> "" Then
                    If NewSchedule.HomeTeamScoreExtra IsNot Nothing AndAlso NewSchedule.HomeTeamScoreExtra.ContainsKey(StandingFormula.StandingId) Then
                        THomeTeamScoreExtra.Add(StandingFormula.StandingId, NewSchedule.HomeTeamScoreExtra(StandingFormula.StandingId))
                    Else
                        THomeTeamScoreExtra.Add(StandingFormula.StandingId, "")
                    End If
                End If
            Next

            For Each StandingFormula In StandingCategory.StandingFormulas
                If StandingFormula.VariableType <> "" Then
                    If NewSchedule.AwayTeamScoreExtra IsNot Nothing AndAlso NewSchedule.AwayTeamScoreExtra.ContainsKey(StandingFormula.StandingId) Then
                        TAwayTeamScoreExtra.Add(StandingFormula.StandingId, NewSchedule.AwayTeamScoreExtra(StandingFormula.StandingId))
                    Else
                        TAwayTeamScoreExtra.Add(StandingFormula.StandingId, "")
                    End If
                End If
            Next

            NewSchedule.HomeTeamScoreExtra = THomeTeamScoreExtra
            NewSchedule.AwayTeamScoreExtra = TAwayTeamScoreExtra
        End If

        If Not NewOldDifferent AndAlso Not NewNewDifferent Then Return -1

        Dim DeleteOldNewSchedule As Boolean = False

        If NewSchedule IsNot Nothing Then
            Dim TOldSchedule As Schedule = Nothing
            If OldSchedule IsNot Nothing Then TOldSchedule = OldSchedule
            If OldNewSchedule IsNot Nothing Then TOldSchedule = OldNewSchedule.ToSchedule()


        End If

        If NewSchedule IsNot Nothing AndAlso OldNewSchedule IsNot Nothing Then
            DeleteTempScheduleItemHelper(OldNewSchedule.ToSchedule(), DeleteScheduleTemps)
            If OldNewSchedule.OldScheduleId >= 0 Then
                Dim TSchedule = OldNewSchedule.ToSchedule()
                TSchedule.ScheduleId = OldNewSchedule.OldScheduleId
                DeleteTempScheduleItemHelper(TSchedule, DeleteScheduleTemps)
            End If
            If IsSameDate(NewSchedule.GameDate, OldNewSchedule.GameDate) Then
                If NewSchedule.IsDifferent(OldSchedule) Then
                    CreateTempScheduleItemHelper(NewSchedule, OldSchedule, Region, Regions, InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp)
                End If
                Return NewSchedule.ScheduleId
            Else
                If OldSchedule IsNot Nothing Then
                    If IsSameDate(NewSchedule.GameDate, OldSchedule.GameDate) Then
                        DeleteTempScheduleItemHelper(OldSchedule, DeleteScheduleTemps)
                        If NewSchedule.IsDifferent(OldSchedule) Then
                            NewSchedule.ScheduleId = OldSchedule.ScheduleId
                            CreateTempScheduleItemHelper(NewSchedule, OldSchedule, Region, Regions, InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp)
                        End If
                        Return OldNewSchedule.OldScheduleId
                    Else
                        Dim OldScheduleId As Integer = -1
                        If IsSameDate(OldNewSchedule.GameDate, OldSchedule.GameDate) Then
                            NewSchedule.ScheduleId = CurrentScheduleId.ScheduleId
                            CurrentScheduleId.ScheduleId += 1
                            OldScheduleId = OldSchedule.ScheduleId
                            OldSchedule.IsDeleted = True
                            MarkTempIsDeleted(OldSchedule, InsertScheduleTemps, NewSchedule.ScheduleId)
                        End If
                        CreateTempScheduleItemHelper(NewSchedule, OldSchedule, Region, Regions, InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp, OldScheduleId)
                        Return NewSchedule.ScheduleId
                    End If
                Else
                    CreateTempScheduleItemHelper(NewSchedule, OldSchedule, Region, Regions, InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp)
                    Return NewSchedule.ScheduleId
                End If
            End If
        Else
            If NewSchedule IsNot Nothing Then
                If OldSchedule IsNot Nothing Then
                    Dim OldScheduleId As Integer = -1
                    If Not IsSameDate(NewSchedule.GameDate, OldSchedule.GameDate) Then
                        NewSchedule.ScheduleId = CurrentScheduleId.ScheduleId
                        CurrentScheduleId.ScheduleId += 1
                        OldScheduleId = OldSchedule.ScheduleId
                        OldSchedule.IsDeleted = True
                        MarkTempIsDeleted(OldSchedule, InsertScheduleTemps, NewSchedule.ScheduleId)
                        CreateTempScheduleItemHelper(NewSchedule, Nothing, Region, Regions, InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp, OldScheduleId)
                    Else
                        CreateTempScheduleItemHelper(NewSchedule, OldSchedule, Region, Regions, InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp, OldScheduleId)
                    End If
                Else
                    NewSchedule.ScheduleId = CurrentScheduleId.ScheduleId
                    CurrentScheduleId.ScheduleId += 1
                    CreateTempScheduleItemHelper(NewSchedule, OldSchedule, Region, Regions, InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp)
                End If
                Return NewSchedule.ScheduleId
            Else
                If OldNewSchedule IsNot Nothing Then
                    If OldSchedule IsNot Nothing Then
                        If IsSameDate(OldNewSchedule.GameDate, OldSchedule.GameDate) Then
                            DeleteTempScheduleItemHelper(OldSchedule, DeleteScheduleTemps)
                            MarkTempIsDeleted(OldSchedule, InsertScheduleTemps)
                        Else
                            DeleteTempScheduleItemHelper(OldNewSchedule.ToSchedule(), DeleteScheduleTemps)
                        End If
                        Return OldSchedule.ScheduleId
                    Else
                        DeleteTempScheduleItemHelper(OldNewSchedule.ToSchedule(), DeleteScheduleTemps)
                        Return OldNewSchedule.ScheduleId
                    End If
                Else
                    MarkTempIsDeleted(OldSchedule, InsertScheduleTemps)
                    Return OldSchedule.ScheduleId
                End If
            End If
        End If
    End Function

    Public Shared Sub DeleteScheduleHelper(Regions As List(Of RegionProperties), OldSchedule As Schedule, RegionUsers As List(Of RegionUser), RegionParks As List(Of Park), RegionLeagues As List(Of RegionLeaguePayContracted), Teams As List(Of Team), OfficialRegions As List(Of OfficialRegion), GameEmails As List(Of GameEmail), AssignorEmails As List(Of GameEmail), DateAdded As Date, DeleteSchedules As List(Of Schedule), InsertSchedules As List(Of Schedule), GamesThatScoresNeedToBeUpdated As List(Of Schedule))
        If OldSchedule Is Nothing Then Return

        Dim Region As RegionProperties = Regions.Find(Function(R) R.RegionId = OldSchedule.RegionId)

        DeleteSchedules.Add(New Schedule With {
            .RegionId = Region.RegionId.ToLower,
            .ScheduleId = OldSchedule.ScheduleId
        })

        InsertSchedules.Add(New Schedule With {
            .RegionId = Region.RegionId.ToLower,
            .ScheduleId = OldSchedule.ScheduleId,
            .VersionId = OldSchedule.VersionId + 1,
            .LinkedRegionId = OldSchedule.LinkedRegionId,
            .LinkedScheduleId = OldSchedule.LinkedScheduleId,
            .GameNumber = OldSchedule.GameNumber,
            .GameType = OldSchedule.GameType,
            .LeagueId = OldSchedule.LeagueId,
            .HomeTeam = OldSchedule.HomeTeam,
            .HomeTeamScore = OldSchedule.HomeTeamScore,
            .HomeTeamScoreExtra = OldSchedule.HomeTeamScoreExtra,
            .AwayTeam = OldSchedule.AwayTeam,
            .AwayTeamScore = OldSchedule.AwayTeamScore,
            .AwayTeamScoreExtra = OldSchedule.AwayTeamScoreExtra,
            .GameDate = OldSchedule.GameDate,
            .ParkId = OldSchedule.ParkId,
            .CrewType = OldSchedule.CrewType,
            .GameStatus = OldSchedule.GameStatus,
            .GameComment = OldSchedule.GameComment,
            .GameScore = OldSchedule.GameScore,
            .OfficialRegionId = OldSchedule.OfficialRegionId,
            .ScorekeeperRegionId = OldSchedule.ScorekeeperRegionId,
            .SupervisorRegionId = OldSchedule.SupervisorRegionId,
            .IsDeleted = True,
            .DateAdded = DateAdded,
            .StatLinks = OldSchedule.StatLinks
        })

        EmailChange(Region, Regions, Nothing, OldSchedule, RegionUsers, RegionParks, RegionLeagues, OfficialRegions, Teams, GameEmails, AssignorEmails)

        If Region.RegionIsLadderLeague Then
            If OldSchedule IsNot Nothing Then
                Dim OldScheduleClone = OldSchedule.CloneItem
                OldScheduleClone.IsDeleted = True
                GamesThatScoresNeedToBeUpdated.Add(OldScheduleClone)
            End iF
        End If
    End Sub

    Public Shared Function GetLastDownloadedScheduleRegionIds(AssignorId As String, UniqueRegionIds As List(Of String), UniqueRegionIdsWithLeagues As List(Of String), RegionLeagues As List(Of RegionLeague), Teams As List(Of Team), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of Schedule)
        Dim Result As List(Of Schedule) = Nothing

        Dim DownloadedDates As New Dictionary(Of String, Date)

        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To UniqueRegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim CommandText As String = "SELECT RegionId, DownloadedDate FROM ScheduleDownloadDate WHERE RegionId IN ({0}) AND UserId = @AssignorId ORDER BY RegionId".Replace("{0}", RegionIdParams.ToString())

        Using SQLCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To UniqueRegionIds.Count
                SQLCommand.Parameters.Add(New SqlParameter("RegionId" & I, UniqueRegionIds(I - 1)))
            Next
            SQLCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
            Dim Reader = SQLCommand.ExecuteReader
            While Reader.Read
                DownloadedDates.Add(Reader.GetString(0), Reader.GetDateTime(1))
            End While
            Reader.Close()
        End Using

        Result = New List(Of Schedule)

        Dim ScheduleResults As New List(Of ScheduleResult)

        Dim ScheduleVersions = GetMasterScheduleFromRegionsHelper(UniqueRegionIdsWithLeagues, Date.MinValue, Date.MaxValue, SQLConnection, SQLTransaction)
        SchedulePosition.GetMasterSchedulePositionFromRegionsHelper(UniqueRegionIdsWithLeagues, Date.MinValue, Date.MaxValue, ScheduleVersions, SQLConnection, SQLTransaction)
        ScheduleFine.GetMasterScheduleFineFromRegionsHelper(UniqueRegionIdsWithLeagues, Date.MinValue, Date.MaxValue, ScheduleVersions, SQLConnection, SQLTransaction)
        ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsHelper(UniqueRegionIdsWithLeagues, Date.MinValue, Date.MaxValue, ScheduleVersions, SQLConnection, SQLTransaction)
        ScheduleUserCommentTeam.GetMasterScheduleUserCommentTeamFromRegionsHelper(UniqueRegionIdsWithLeagues, Date.MinValue, Date.MaxValue, ScheduleVersions, SQLConnection, SQLTransaction)
        ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsHelper(UniqueRegionIdsWithLeagues, Date.MinValue, Date.MaxValue, ScheduleVersions, SQLConnection, SQLTransaction)
        ScheduleCallup.GetMasterScheduleCallUpFromRegionsHelper(UniqueRegionIdsWithLeagues, Date.MinValue, Date.MaxValue, ScheduleVersions, SQLConnection, SQLTransaction)

        ScheduleResults = UmpireAssignor.ScheduleResult.ConvertIntoScheduleResultNoOfficial(ScheduleVersions, New List(Of ScheduleConfirm), New List(Of ScheduleTeamConfirm))

        For Each ScheduleResult In ScheduleResults
            Dim TRegionId = ScheduleResult.Schedule(0).RegionId

            If UniqueRegionIds.Contains(TRegionId) Then
                Dim DownloadedDate = Date.UtcNow.AddDays(1)
                If DownloadedDates.ContainsKey(TRegionId) Then DownloadedDate = DownloadedDates(TRegionId)
                For I As Integer = ScheduleResult.Schedule.Count - 1 To 0 Step -1
                    If ScheduleResult.Schedule(I).DateAdded <= DownloadedDate Then
                        If Not ScheduleResult.Schedule(I).IsDeleted Then
                            Result.Add(ScheduleResult.Schedule(I))
                        End If
                        Exit For
                    End If
                Next
            Else
                Dim RealTeamIds As New List(Of String)
                For Each ScheduleItem In ScheduleResult.Schedule
                    For Each Team In Teams
                        If Team.RealTeamId <> "" AndAlso Team.IsLinked AndAlso Team.RegionId = TRegionId AndAlso (Team.TeamId = ScheduleItem.HomeTeam.Trim.ToLower OrElse Team.TeamId = ScheduleItem.AwayTeam.Trim.ToLower) AndAlso Not RealTeamIds.Contains(Team.RealTeamId) AndAlso UniqueRegionIds.Contains(Team.RealTeamId) Then
                            RealTeamIds.Add(Team.RealTeamId)
                        End If
                    Next
                Next

                For Each RealTeamId In RealTeamIds
                    Dim DownloadedDate = Date.UtcNow.AddDays(1)
                    If DownloadedDates.ContainsKey(TRegionId) Then DownloadedDate = DownloadedDates(RealTeamId)

                    Dim NewScheduleResult As New ScheduleResult

                    For Each ScheduleItem In ScheduleResult.Schedule
                        NewScheduleResult.Schedule.Add(ConvertToLinkScheduleItem(ScheduleItem, RealTeamId, RegionLeagues))
                    Next
                    MergeTeamData({NewScheduleResult}.ToList())

                    For I As Integer = NewScheduleResult.Schedule.Count - 1 To 0 Step -1
                        Dim SI = ScheduleResult.Schedule(I)
                        If NewScheduleResult.Schedule(I).DateAdded <= DownloadedDate Then
                            If Not NewScheduleResult.Schedule(I).IsDeleted Then
                                If Teams.Any(Function(T) T.RealTeamId = RealTeamId AndAlso T.IsLinked AndAlso T.RegionId = TRegionId AndAlso (T.TeamId = SI.HomeTeam.Trim.ToLower OrElse T.TeamId = SI.AwayTeam.Trim.ToLower)) Then
                                    Result.Add(NewScheduleResult.Schedule(I))
                                End If
                            End If
                            Exit For
                        End If
                    Next
                Next
            End If
        Next

        Result.Sort(Schedule.BasicSorter)

        Return Result
    End Function

    Private Shared Sub ExecuteSQLCommand(SQLConnection As SqlConnection, SQLTransaction As SqlTransaction, ByRef SqlCommand As SqlCommand, ByRef CommandSB As StringBuilder, ByRef CommandCount As Integer, RegionId As String, AssignorId As String)
        SqlCommand.CommandText = CommandSB.ToString()
        SqlCommand.ExecuteNonQuery()

        SqlCommand = New SqlCommand()
        SqlCommand.Connection = SQLConnection
        SqlCommand.Transaction = SQLTransaction
        CommandSB = New StringBuilder()
        CommandCount = 0

        If RegionId <> "allregions" AndAlso RegionId <> "alleditableregions" Then
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
        End If
        SqlCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
    End Sub

    Public Shared Sub MergeTempDataToNewData(NewItem As Schedule, OldItem As Schedule, TempItem As ScheduleTemp)
        If NewItem IsNot Nothing Then
            NewItem.ScheduleUserComments = New List(Of ScheduleUserComment)
            NewItem.ScheduleCallUps = New List(Of ScheduleCallup)
            NewItem.ScheduleCommentTeams = New List(Of ScheduleCommentTeam)

            If OldItem IsNot Nothing Then
                For Each ScheduleUserComment In OldItem.ScheduleUserComments
                    NewItem.ScheduleUserComments.Add(ScheduleUserComment)
                Next

                For Each ScheduleCommentTeam In OldItem.ScheduleCommentTeams
                    NewItem.ScheduleCommentTeams.Add(ScheduleCommentTeam)
                Next

                For Each ScheduleCallUp In OldItem.ScheduleCallUps
                    NewItem.ScheduleCallUps.Add(ScheduleCallUp)
                Next
            End If

            If TempItem IsNot Nothing Then

                For Each ScheduleUserComment In TempItem.ScheduleUserComments
                    If ScheduleUserComment.IsDeleted Then
                        NewItem.ScheduleUserComments.RemoveAll(Function(SU) SU.OfficialId = ScheduleUserComment.OfficialId)
                    Else
                        Dim OldUserComment = NewItem.ScheduleUserComments.Find(Function(SU) SU.OfficialId = ScheduleUserComment.OfficialId)
                        If OldUserComment Is Nothing Then
                            NewItem.ScheduleUserComments.Add(ScheduleUserComment)
                        Else
                            OldUserComment.Comment = ScheduleUserComment.Comment
                        End If
                    End If
                Next

                For Each ScheduleCommentTeam In TempItem.ScheduleCommentTeams
                    If ScheduleCommentTeam.IsDeleted Then
                        NewItem.ScheduleCommentTeams.Clear()
                    Else
                        Dim OldCommentTeam = If(NewItem.ScheduleCommentTeams.Count = 1, NewItem.ScheduleCommentTeams(0), Nothing)
                        If OldCommentTeam Is Nothing Then
                            NewItem.ScheduleCommentTeams.Add(ScheduleCommentTeam)
                        Else
                            OldCommentTeam.Comment = ScheduleCommentTeam.Comment
                        End If
                    End If
                Next

                For Each ScheduleCallUp In TempItem.ScheduleCallUps
                    If ScheduleCallUp.IsDeleted Then
                        NewItem.ScheduleCallUps.RemoveAll(Function(SC) SC.LinkedRegionId = ScheduleCallUp.LinkedRegionId AndAlso SC.Username = ScheduleCallUp.Username)
                    Else
                        Dim OldCallUp = NewItem.ScheduleCallUps.Find(Function(SC) SC.LinkedRegionId = ScheduleCallUp.LinkedRegionId AndAlso SC.Username = ScheduleCallUp.Username)
                        If OldCallUp Is Nothing Then
                            NewItem.ScheduleCallUps.Add(OldCallUp)
                        End If
                    End If
                Next

            End If
        End If
    End Sub

    Public Shared Sub MergeTempDataRefereeToNewData(Region As RegionProperties, NewItem As Schedule, OldItem As Schedule, TempItem As ScheduleTemp)
        If NewItem IsNot Nothing Then

            NewItem.SchedulePositions = New List(Of SchedulePosition)
            If NewItem.CrewType IsNot Nothing AndAlso NewItem.CrewType.ContainsKey("umpire") Then
                If RegionLeaguePayContracted.GetSportCrewPositions(Region.Sport).ContainsKey(NewItem.CrewType("umpire")) Then
                    For Each PositionId In RegionLeaguePayContracted.GetSportCrewPositions(Region.Sport)(NewItem.CrewType("umpire"))
                        NewItem.SchedulePositions.Add(New SchedulePosition With {.PositionId = PositionId, .OfficialId = ""})
                    Next
                End If
            End If
            If NewItem.CrewType IsNot Nothing AndAlso NewItem.CrewType.ContainsKey("scorekeeper") Then
                If RegionLeaguePayContracted.GetSportCrewPositionsScorekeepers(Region.Sport).ContainsKey(NewItem.CrewType("scorekeeper")) Then
                    For Each PositionId In RegionLeaguePayContracted.GetSportCrewPositionsScorekeepers(Region.Sport)(NewItem.CrewType("scorekeeper"))
                        NewItem.SchedulePositions.Add(New SchedulePosition With {.PositionId = PositionId, .OfficialId = ""})
                    Next
                End If
            End If
            If NewItem.CrewType IsNot Nothing AndAlso NewItem.CrewType.ContainsKey("supervisor") Then
                If RegionLeaguePayContracted.GetSportCrewPositionsSupervisors(Region.Sport).ContainsKey(NewItem.CrewType("supervisor")) Then
                    For Each PositionId In RegionLeaguePayContracted.GetSportCrewPositionsSupervisors(Region.Sport)(NewItem.CrewType("supervisor"))
                        NewItem.SchedulePositions.Add(New SchedulePosition With {.PositionId = PositionId, .OfficialId = ""})
                    Next
                End If
            End If

            NewItem.ScheduleFines = New List(Of ScheduleFine)
            NewItem.ScheduleUserComments = New List(Of ScheduleUserComment)
            NewItem.ScheduleCommentTeams = New List(Of ScheduleCommentTeam)

            If OldItem IsNot Nothing Then
                For Each SchedulePosition In OldItem.SchedulePositions
                    Dim OldSchedulePosition = NewItem.SchedulePositions.Find(Function(SP) SP.PositionId = SchedulePosition.PositionId)
                    If OldSchedulePosition IsNot Nothing Then OldSchedulePosition.OfficialId = SchedulePosition.OfficialId
                Next

                For Each ScheduleFine In OldItem.ScheduleFines
                    NewItem.ScheduleFines.Add(ScheduleFine)
                Next

                For Each ScheduleUserComment In OldItem.ScheduleUserComments
                    NewItem.ScheduleUserComments.Add(ScheduleUserComment)
                Next

                For Each ScheduleCommentTeam In OldItem.ScheduleCommentTeams
                    NewItem.ScheduleCommentTeams.Add(ScheduleCommentTeam)
                Next
            End If

            If TempItem IsNot Nothing Then

                For Each SchedulePosition In TempItem.SchedulePositions
                    Dim OldPosition = NewItem.SchedulePositions.Find(Function(SP) SP.PositionId = SchedulePosition.PositionId)
                    If OldPosition IsNot Nothing Then
                        OldPosition.OfficialId = SchedulePosition.OfficialId
                    End If
                Next

                For Each ScheduleFine In TempItem.ScheduleFines
                    If ScheduleFine.IsDeleted Then
                        NewItem.ScheduleFines.RemoveAll(Function(SF) SF.OfficialId = ScheduleFine.OfficialId)
                    Else
                        Dim OldFine = NewItem.ScheduleFines.Find(Function(SU) SU.OfficialId = ScheduleFine.OfficialId)
                        If OldFine Is Nothing Then
                            NewItem.ScheduleFines.Add(ScheduleFine)
                        Else
                            OldFine.Amount = ScheduleFine.Amount
                            OldFine.Comment = ScheduleFine.Comment
                        End If
                    End If
                Next

                For Each ScheduleUserComment In TempItem.ScheduleUserComments
                    If ScheduleUserComment.IsDeleted Then
                        NewItem.ScheduleUserComments.RemoveAll(Function(SU) SU.OfficialId = ScheduleUserComment.OfficialId)
                    Else
                        Dim OldUserComment = NewItem.ScheduleUserComments.Find(Function(SU) SU.OfficialId = ScheduleUserComment.OfficialId)
                        If OldUserComment Is Nothing Then
                            NewItem.ScheduleUserComments.Add(ScheduleUserComment)
                        Else
                            OldUserComment.Comment = ScheduleUserComment.Comment
                        End If
                    End If
                Next

                For Each ScheduleCommentTeam In TempItem.ScheduleCommentTeams
                    If ScheduleCommentTeam.IsDeleted Then
                        NewItem.ScheduleCommentTeams.Clear()
                    Else
                        Dim OldCommentTeam = If(NewItem.ScheduleCommentTeams.Count = 1, NewItem.ScheduleCommentTeams(0), Nothing)
                        If OldCommentTeam Is Nothing Then
                            NewItem.ScheduleCommentTeams.Add(ScheduleCommentTeam)
                        Else
                            OldCommentTeam.Comment = ScheduleCommentTeam.Comment
                        End If
                    End If
                Next

            End If
        End If
    End Sub

    Public Shared Sub BulkInsertScheduleTempSubmitted(ScheduleTempSubmitted)

    End Sub

    Public Shared Function UploadToTemp(AssignorId As String, RegionId As String, Schedules As List(Of Schedule), Optional ByPassPermissions As Boolean = False, Optional IgnoreDeletes As Boolean = False) As Object
        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Dim UniqueRegionIds As New List(Of String)
                If RegionId = "allregions" OrElse RegionId = "alleditableregions" Then
                    UniqueRegionIds = RegionUser.GetAllMyRegionsIdExecs(AssignorId, SqlConnection, SqlTransaction)
                Else
                    UniqueRegionIds.Add(RegionId)

                    If Not ByPassPermissions Then
                        Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                        If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                            Return New ErrorObject("InvalidPermissions")
                        End If
                    End If
                End If

                UniqueRegionIds.Sort()

                Dim Regions = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction, False)
                Dim RegionLeagues = RegionLeague.GetRegionsLeaguesHelper(UniqueRegionIds, SqlConnection, SqlTransaction)

                Dim UniqueRegionIdsWithLeagues As New List(Of String)
                Dim NotYetFetched As New List(Of String)
                UniqueRegionIdsWithLeagues.AddRange(UniqueRegionIds)

                For Each RegionLeague In RegionLeagues
                    Dim TRegion = Regions.Find(Function(R) R.RegionId = RegionLeague.RegionId)
                    If TRegion.EntityType = "team" AndAlso RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked AndAlso Not UniqueRegionIdsWithLeagues.Contains(RegionLeague.RealLeagueId) Then
                        UniqueRegionIdsWithLeagues.Add(RegionLeague.RealLeagueId)
                        NotYetFetched.Add(RegionLeague.RealLeagueId)
                    End If
                Next

                For Each Region In Regions
                    If Region.RequiresPayment Then Return New ErrorObject("RequiresPayment")
                Next

                Dim Teams = Team.GetTeamsInRegionsHelper(UniqueRegionIdsWithLeagues, SqlConnection, SqlTransaction)

                Dim CurrentSchedule = New List(Of Schedule)

                If ByPassPermissions Then
                    CurrentSchedule = GetMasterScheduleFromRegionsNonVersionHelper(UniqueRegionIds, Date.MinValue, Date.MaxValue, SqlConnection, SqlTransaction)
                Else
                    CurrentSchedule = GetLastDownloadedScheduleRegionIds(AssignorId, UniqueRegionIds, UniqueRegionIdsWithLeagues, RegionLeagues, Teams, SqlConnection, SqlTransaction)
                End If

                Dim CurrentScheduleIds = UmpireAssignor.ScheduleId.GetScheduleIdsFromRegionsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)

                'INSERT INTO ScheduleTempSubmitted (RegionId, UserSubmitId, IsSubmitted) VALUES (@RegionId{0}, @AssignorId, 1); ".Replace("{0}", CommandCount))

                Dim CommandSB As New StringBuilder()
                CommandSB.Append("DELETE FROM ScheduleTempSubmitted WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId;")
                CommandSB.Append("DELETE FROM ScheduleTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
                CommandSB.Append("DELETE FROM SchedulePositionTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
                CommandSB.Append("DELETE FROM ScheduleFineTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
                CommandSB.Append("DELETE FROM ScheduleUserCommentTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
                CommandSB.Append("DELETE FROM ScheduleUserCommentTeamTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
                CommandSB.Append("DELETE FROM ScheduleCommentTeamTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
                CommandSB.Append("DELETE FROM ScheduleCallUpTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
                Using SQLCommand2 As New SqlCommand(CommandSB.ToString().Replace("{0}", PublicCode.CreateParamStr("RegionId", UniqueRegionIdsWithLeagues)), SqlConnection, SqlTransaction)
                    PublicCode.CreateParamsSQLCommand("RegionId", UniqueRegionIdsWithLeagues, SQLCommand2)
                    SQLCommand2.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
                    SQLCommand2.ExecuteNonQuery()
                End Using

                Dim ScheduleTempSubmitteds As New List(Of ScheduleTempSubmitted)
                For Each UniqueRegionIdsWithLeague In UniqueRegionIdsWithLeagues
                    ScheduleTempSubmitteds.Add(New ScheduleTempSubmitted With {
                        .RegionId = UniqueRegionIdsWithLeague,
                        .IsSubmitted = True,
                        .UserSubmitId = AssignorId
                    })
                Next

                ScheduleTempSubmitted.BulkInsert(ScheduleTempSubmitteds, SqlConnection, SqlTransaction)

                Schedules.Sort(Schedule.BasicSorter)

                For Each Schedule In Schedules
                    If Schedule.ScheduleId >= CurrentScheduleIds(Schedule.RegionId).ScheduleId Then
                        Schedule.ScheduleId = -1
                    End If
                Next

                Dim OldNewSchedule = GetMasterScheduleFromRegionsTempHelper(UniqueRegionIdsWithLeagues, AssignorId, Date.MinValue, Date.MaxValue, SqlConnection, SqlTransaction)
                SchedulePosition.GetMasterSchedulePositionFromRegionsTempHelper(UniqueRegionIdsWithLeagues, AssignorId, Date.MinValue, Date.MaxValue, OldNewSchedule, SqlConnection, SqlTransaction)
                ScheduleFine.GetMasterScheduleFineFromRegionsTempHelper(UniqueRegionIdsWithLeagues, AssignorId, Date.MinValue, Date.MaxValue, OldNewSchedule, SqlConnection, SqlTransaction)
                ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsTempHelper(UniqueRegionIdsWithLeagues, AssignorId, Date.MinValue, Date.MaxValue, OldNewSchedule, SqlConnection, SqlTransaction)
                ScheduleUserCommentTeam.GetMasterScheduleUserCommentTeamFromRegionsTempHelper(UniqueRegionIdsWithLeagues, AssignorId, Date.MinValue, Date.MaxValue, OldNewSchedule, SqlConnection, SqlTransaction)
                ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsTempHelper(UniqueRegionIdsWithLeagues, AssignorId, Date.MinValue, Date.MaxValue, OldNewSchedule, SqlConnection, SqlTransaction)
                ScheduleCallup.GetMasterScheduleCallUpFromRegionsTempHelper(UniqueRegionIdsWithLeagues, AssignorId, Date.MinValue, Date.MaxValue, OldNewSchedule, SqlConnection, SqlTransaction)

                For Each OldNewScheduleItem In OldNewSchedule
                    MergeUserCommentData(OldNewScheduleItem.ScheduleUserComments, OldNewScheduleItem.ScheduleUserCommentTeams)
                Next

                Dim InsertScheduleTemps As New List(Of ScheduleTemp)
                Dim InsertSchedulePositionsTemp As New List(Of SchedulePositionFull)
                Dim InsertScheduleFinesTemp As New List(Of ScheduleFineFull)
                Dim InsertScheduleUserCommentsTemp As New List(Of ScheduleUserCommentFull)
                Dim InsertScheduleUserCommentTeamsTemp As New List(Of ScheduleUserCommentTeamFull)
                Dim InsertScheduleCommentTeamsTemp As New List(Of ScheduleCommentTeamFull)
                Dim InsertScheduleCallupsTemp As New List(Of ScheduleCallupFull)
                Dim DeleteScheduleTemps As New List(Of ScheduleTemp)

                PublicCode.ProcessListPairs(Schedules, CurrentSchedule, AddressOf Schedule.BasicComparerSimple,
                    Sub(V1)
                    End Sub,
                    Sub(V2)
                    End Sub,
                    Sub(V1, V2)
                        Dim TRegion = Regions.Find(Function(R) R.RegionId = V1.RegionId)
                        If TRegion.EntityType = "referee" Then
                            V1.LinkedRegionId = V2.LinkedRegionId
                            V1.LinkedScheduleId = V2.LinkedScheduleId
                        End If
                    End Sub)

                PublicCode.ProcessListPairs(Schedules, CurrentSchedule, AddressOf Schedule.BasicComparer,
                    Sub(V1)
                        Dim TRegion = Regions.Find(Function(R) R.RegionId = V1.RegionId)
                        Dim TOldNewSchedule As ScheduleTemp = Nothing
                        Dim TOldNewScheduleIndex = OldNewSchedule.BinarySearch(New ScheduleTemp With {.RegionId = V1.RegionId, .ScheduleId = V1.ScheduleId}, ScheduleTemp.BasicSorterNoLinkedRegionId)

                        If TOldNewScheduleIndex >= 0 Then TOldNewSchedule = OldNewSchedule(TOldNewScheduleIndex)

                        Dim V2 As Schedule = Nothing

                        If TOldNewSchedule IsNot Nothing AndAlso TOldNewSchedule.OldScheduleId >= 0 Then
                            V2 = PublicCode.BinarySearchItem(Schedules, New Schedule With {.RegionId = V1.RegionId, .ScheduleId = TOldNewSchedule.OldScheduleId}, Schedule.BasicSorter)
                        End If

                        MergeTempDataToNewData(V1, V2, TOldNewSchedule)

                        UpsertToTempHelper(AssignorId, TRegion, Regions, V1, V2, TOldNewSchedule, CurrentScheduleIds(V1.RegionId), InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp, DeleteScheduleTemps)
                    End Sub,
                    Sub(V2)
                        If Not IgnoreDeletes Then
                            If Not V2.IsDeleted AndAlso V2.LinkedRegionId = "" Then
                                Dim TRegion = Regions.Find(Function(R) R.RegionId = V2.RegionId)

                                Dim TOldNewSchedule As ScheduleTemp = Nothing
                                Dim TOldNewScheduleIndex = OldNewSchedule.BinarySearch(New ScheduleTemp With {.RegionId = V2.RegionId, .ScheduleId = V2.ScheduleId}, ScheduleTemp.BasicSorterNoLinkedRegionId)

                                If TOldNewScheduleIndex >= 0 Then TOldNewSchedule = OldNewSchedule(TOldNewScheduleIndex)

                                UpsertToTempHelper(AssignorId, TRegion, Regions, Nothing, V2, TOldNewSchedule, CurrentScheduleIds(V2.RegionId), InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp, DeleteScheduleTemps)
                            End If
                        End If
                    End Sub,
                    Sub(V1, V2)
                        If Not V2.IsDeleted Then
                            Dim TRegion = Regions.Find(Function(R) R.RegionId = V1.RegionId)

                            If (V2.LinkedRegionId <> "" AndAlso TRegion.EntityType = "team") Then Exit Sub

                            Dim TOldNewSchedule As ScheduleTemp = Nothing
                            Dim TOldNewScheduleIndex = OldNewSchedule.BinarySearch(New ScheduleTemp With {.RegionId = V1.RegionId, .ScheduleId = V1.ScheduleId}, ScheduleTemp.BasicSorterNoLinkedRegionId)

                            If TOldNewScheduleIndex >= 0 Then TOldNewSchedule = OldNewSchedule(TOldNewScheduleIndex)

                            If TRegion.EntityType = "referee" Then
                                V1.LinkedRegionId = V2.LinkedRegionId
                                V1.LinkedScheduleId = V2.LinkedScheduleId
                                V1.OfficialRegionId = V2.OfficialRegionId
                                V1.ScorekeeperRegionId = V2.ScorekeeperRegionId
                                V1.SupervisorRegionId = V2.SupervisorRegionId
                            End If

                            MergeTempDataToNewData(V1, V2, TOldNewSchedule)

                            UpsertToTempHelper(AssignorId, TRegion, Regions, V1, V2, TOldNewSchedule, CurrentScheduleIds(V1.RegionId), InsertScheduleTemps, InsertSchedulePositionsTemp, InsertScheduleFinesTemp, InsertScheduleUserCommentsTemp, InsertScheduleUserCommentTeamsTemp, InsertScheduleCommentTeamsTemp, InsertScheduleCallupsTemp, DeleteScheduleTemps)
                        End If
                    End Sub)

                ScheduleTemp.BulkDelete(DeleteScheduleTemps, AssignorId, SqlConnection, SqlTransaction)
                ScheduleTemp.BulkInsert(InsertScheduleTemps, AssignorId, SqlConnection, SqlTransaction)
                SchedulePositionFull.BulkInsert(InsertSchedulePositionsTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleFineFull.BulkInsert(InsertScheduleFinesTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleUserCommentFull.BulkInsert(InsertScheduleUserCommentsTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleUserCommentTeamFull.BulkInsert(InsertScheduleUserCommentTeamsTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleCommentTeamFull.BulkInsert(InsertScheduleCommentTeamsTemp, AssignorId, SqlConnection, SqlTransaction)
                ScheduleCallupFull.BulkInsert(InsertScheduleCallupsTemp, AssignorId, SqlConnection, SqlTransaction)

                UmpireAssignor.ScheduleId.UpsertHelper(CurrentScheduleIds.Values.ToList(), SqlConnection, SqlTransaction)

                SqlTransaction.Commit()
            End Using
        End Using
        Return New With {.Success = True}
        'Catch ex As Exception
        'Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Function IsRealUserOnGameWithFine(RealUsername As String, RegionUsers As List(Of RegionUser)) As Boolean
        If SchedulePositions IsNot Nothing Then
            For Each SchedulePosition In SchedulePositions
                Dim RU As RegionUser = PublicCode.BinarySearchItem(RegionUsers, New RegionUser With {.RegionId = RegionId, .Username = SchedulePosition.OfficialId}, RegionUser.BasicSorter)
                If RU IsNot Nothing AndAlso RU.RealUsername = RealUsername Then Return True
            Next
        End If

        If ScheduleFines IsNot Nothing Then
            For Each ScheduleFine In ScheduleFines
                Dim RU As RegionUser = PublicCode.BinarySearchItem(RegionUsers, New RegionUser With {.RegionId = RegionId, .Username = ScheduleFine.OfficialId}, RegionUser.BasicSorter)
                If RU IsNot Nothing AndAlso RU.RealUsername = RealUsername Then Return True
            Next
        End If

        Return False
    End Function

    Public Function IsDifferentLinked(Schedule As Schedule) As Boolean
        If GameNumber <> Schedule.GameNumber Then Return True
        If GameType <> Schedule.GameType Then Return True
        If LeagueId <> Schedule.LeagueId Then Return True
        If HomeTeam <> Schedule.HomeTeam Then Return True
        If AwayTeam <> Schedule.AwayTeam Then Return True
        If GameDate <> Schedule.GameDate Then Return True
        If ParkId <> Schedule.ParkId Then Return True
        If GameStatus <> Schedule.GameStatus Then Return True
        If GameComment <> Schedule.GameComment Then Return True
        If GameScore.IsDifferentScoreOnly(GameScore, Schedule.GameScore) Then Return True
        If IsDeleted <> Schedule.IsDeleted Then Return True
        Return False
    End Function

    Public Function IsDifferent(Schedule As Schedule) As Boolean
        If Schedule Is Nothing Then Return True
        If RequiredGameNotification(Me, Schedule) Then Return True
        If HomeTeamScore <> Schedule.HomeTeamScore Then Return True
        If AwayTeamScore <> Schedule.AwayTeamScore Then Return True

        For Each HomeTeamScoreExtraItem In HomeTeamScoreExtra
            If Schedule.HomeTeamScoreExtra.ContainsKey(HomeTeamScoreExtraItem.Key) Then
                If Schedule.HomeTeamScoreExtra(HomeTeamScoreExtraItem.Key) <> HomeTeamScoreExtraItem.Value Then Return True
            End If
        Next
        For Each HomeTeamScoreExtraItem In Schedule.HomeTeamScoreExtra
            If HomeTeamScoreExtra.ContainsKey(HomeTeamScoreExtraItem.Key) Then
                If HomeTeamScoreExtra(HomeTeamScoreExtraItem.Key) <> HomeTeamScoreExtraItem.Value Then Return True
            End If
        Next

        For Each AwayTeamScoreExtraItem In AwayTeamScoreExtra
            If Schedule.AwayTeamScoreExtra.ContainsKey(AwayTeamScoreExtraItem.Key) Then
                If Schedule.AwayTeamScoreExtra(AwayTeamScoreExtraItem.Key) <> AwayTeamScoreExtraItem.Value Then Return True
            End If
        Next
        For Each AwayTeamScoreExtraItem In Schedule.AwayTeamScoreExtra
            If AwayTeamScoreExtra.ContainsKey(AwayTeamScoreExtraItem.Key) Then
                If AwayTeamScoreExtra(AwayTeamScoreExtraItem.Key) <> AwayTeamScoreExtraItem.Value Then Return True
            End If
        Next

        If GameComment <> Schedule.GameComment Then Return True

        If GameScore.IsDifferentScoreOnly(GameScore, Schedule.GameScore) Then Return True

        If IsDeleted <> Schedule.IsDeleted Then Return True

        If Me.SchedulePositions.Count <> Schedule.SchedulePositions.Count Then Return True
        If Me.ScheduleFines.Count <> Schedule.ScheduleFines.Count Then Return True
        If Me.ScheduleUserCommentTeams.Count <> Schedule.ScheduleUserCommentTeams.Count Then Return True
        If Me.ScheduleUserComments.Count <> Schedule.ScheduleUserComments.Count Then Return True
        If Me.ScheduleCommentTeams.Count <> Schedule.ScheduleCommentTeams.Count Then Return True
        If Me.ScheduleCallUps.Count <> Schedule.ScheduleCallUps.Count Then Return True

        Me.SchedulePositions.Sort(SchedulePosition.BasicSorter)
        Schedule.SchedulePositions.Sort(SchedulePosition.BasicSorter)

        For I As Integer = 0 To SchedulePositions.Count - 1
            If SchedulePositions(I).OfficialId <> Schedule.SchedulePositions(I).OfficialId Then Return True
            If SchedulePositions(I).PositionId <> Schedule.SchedulePositions(I).PositionId Then Return True
        Next

        Me.ScheduleFines.Sort(ScheduleFine.BasicSorter)
        Schedule.ScheduleFines.Sort(ScheduleFine.BasicSorter)

        For I As Integer = 0 To ScheduleFines.Count - 1
            If ScheduleFines(I).OfficialId <> Schedule.ScheduleFines(I).OfficialId Then Return True
            If ScheduleFines(I).Amount <> Schedule.ScheduleFines(I).Amount Then Return True
            If ScheduleFines(I).Comment <> Schedule.ScheduleFines(I).Comment Then Return True
        Next

        Me.ScheduleUserComments.Sort(ScheduleUserComment.BasicSorter)
        Schedule.ScheduleUserComments.Sort(ScheduleUserComment.BasicSorter)

        For I As Integer = 0 To ScheduleUserComments.Count - 1
            If ScheduleUserComments(I).OfficialId <> Schedule.ScheduleUserComments(I).OfficialId Then Return True
            If ScheduleUserComments(I).Comment <> Schedule.ScheduleUserComments(I).Comment Then Return True
        Next

        Me.ScheduleUserCommentTeams.Sort(ScheduleUserCommentTeam.BasicSorter)
        Schedule.ScheduleUserCommentTeams.Sort(ScheduleUserCommentTeam.BasicSorter)

        For I As Integer = 0 To ScheduleUserCommentTeams.Count - 1
            If ScheduleUserCommentTeams(I).LinkedRegionId <> Schedule.ScheduleUserCommentTeams(I).LinkedRegionId Then Return True
            If ScheduleUserCommentTeams(I).OfficialId <> Schedule.ScheduleUserCommentTeams(I).OfficialId Then Return True
            If ScheduleUserCommentTeams(I).Comment <> Schedule.ScheduleUserCommentTeams(I).Comment Then Return True
        Next


        Me.ScheduleCommentTeams.Sort(ScheduleCommentTeam.BasicSorter)
        Schedule.ScheduleCommentTeams.Sort(ScheduleCommentTeam.BasicSorter)

        For I As Integer = 0 To ScheduleCommentTeams.Count - 1
            If ScheduleCommentTeams(I).LinkedRegionId <> Schedule.ScheduleCommentTeams(I).LinkedRegionId Then Return True
            If ScheduleCommentTeams(I).Comment <> Schedule.ScheduleCommentTeams(I).Comment Then Return True
        Next

        Me.ScheduleCallUps.Sort(ScheduleCallup.BasicSorter)
        Schedule.ScheduleCallUps.Sort(ScheduleCallup.BasicSorter)

        For I As Integer = 0 To ScheduleCallUps.Count - 1
            If ScheduleCallUps(I).LinkedRegionId <> Schedule.ScheduleCallUps(I).LinkedRegionId Then Return True
            If ScheduleCallUps(I).Username <> Schedule.ScheduleCallUps(I).Username Then Return True
        Next

        If (GameScore Is Nothing) <> (Schedule.GameScore Is Nothing) Then
            Return True
        End If

        If GameScore IsNot Nothing Then
            If Not PublicCode.ListsAreEqual(GameScore.Score, Schedule.GameScore.Score, Function (V1, V2) PublicCode.ListsAreEqual(V1, V2)) Then Return True
        End If

        Return False
    End Function

    Public Function ContainsTeamMemberId(Username As String, RegionUsers As List(Of RegionUser)) As Boolean
        Dim RegionUser = PublicCode.BinarySearchItem(RegionUsers, New RegionUser With {.RegionId = RegionId, .Username = Username}, Function(RU1, RU2)
                                                                                                                                        Dim Comp As Integer = -RU1.RegionId.CompareTo(RU2.RegionId)
                                                                                                                                        If Comp = 0 Then Comp = -RU1.Username.CompareTo(RU2.Username)
                                                                                                                                        Return Comp
                                                                                                                                    End Function)
        If RegionUser IsNot Nothing Then
            If RegionUser.IsCoach OrElse RegionUser.IsPlayer Then
                Return True
            ElseIf RegionUser.IsCallup Then
                Return ScheduleCallUps.Any(Function(SC) SC.Username = Username)
            End If
        End If
        Return False
    End Function

    Public Shared Sub EmailRequireConfirms(ByRef Users As List(Of User))
        Dim RegionUsersByRealUsername As New Dictionary(Of String, List(Of RegionUser))
        Dim UniqueRegionUsers As New List(Of Tuple(Of List(Of RegionUser), List(Of Tuple(Of Schedule, Schedule, RegionProperties))))
        Dim UniqueRegionUsersSearch As New Dictionary(Of String, Dictionary(Of String, Tuple(Of List(Of RegionUser), List(Of Tuple(Of Schedule, Schedule, RegionProperties)))))

        Dim Regions As List(Of RegionProperties) = Nothing
        Dim Schedule As New List(Of ScheduleResult)
        Dim ScheduleAvailable As New List(Of Schedule)
        Dim Availability As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of DateTime, Integer)))
        Dim RegionUsers As List(Of RegionUser) = Nothing
        Dim RegionLeagues As List(Of RegionLeaguePayContracted) = Nothing
        Dim RegionLeaguePayContracteds As List(Of RegionLeaguePayContracted) = Nothing
        Dim Parks As List(Of Park) = Nothing
        Dim Teams As List(Of Team) = Nothing
        Dim OfficialRegions As List(Of OfficialRegion) = Nothing


        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()
                If Users Is Nothing Then Users = User.GetAllUsersInfoHelper(SqlConnection, SqlTransaction)
                Regions = UmpireAssignor.RegionProperties.GetAllRegionPropertiesHelper(SqlConnection, SqlTransaction)

                RegionUsers = RegionUser.GetAllRegionUsersHelper(SqlConnection, SqlTransaction)
                Dim TRegionLeagues = RegionLeague.GetAllRegionLeaguesPayContractedHelper(SqlConnection, SqlTransaction)
                Teams = Team.GetAllTeamsHelper(SqlConnection, SqlTransaction)
                OfficialRegions = OfficialRegion.GetAllOfficialRegionsHelper(SqlConnection, SqlTransaction)

                RegionLeagues = RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(TRegionLeagues.Item1, New List(Of RegionLeaguePay))
                RegionLeaguePayContracteds = TRegionLeagues.Item2

                Parks = Park.GetAllParksHelper(RegionLeaguePayContracteds, SqlConnection, SqlTransaction)

                Dim MinDate = Date.UtcNow.AddHours(-12)
                Dim MaxDate = Date.UtcNow.AddYears(1)

                Dim ScheduleVersions = UmpireAssignor.Schedule.GetAllMasterScheduleHelper(MinDate, MaxDate, SqlConnection, SqlTransaction)
                UmpireAssignor.SchedulePosition.GetAllMasterSchedulePositionHelper(MinDate, MaxDate, ScheduleVersions, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleFine.GetAllMasterScheduleFineHelper(MinDate, MaxDate, ScheduleVersions, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleCallup.GetAllMasterScheduleCallUpHelper(MinDate, MaxDate, ScheduleVersions, SqlConnection, SqlTransaction)

                Dim ScheduleConfirms = UmpireAssignor.ScheduleConfirm.GetAllMasterScheduleConfirm(MinDate, MaxDate, SqlConnection, SqlTransaction)
                Dim ScheduleTeamConfirms = UmpireAssignor.ScheduleTeamConfirm.GetAllMasterScheduleTeamConfirm(MinDate, MaxDate, SqlConnection, SqlTransaction)

                Schedule = ScheduleResult.ConvertIntoScheduleResultNoOfficial(ScheduleVersions, ScheduleConfirms, ScheduleTeamConfirms)
            End Using
        End Using

        Dim NewScheduleResults As New List(Of ScheduleResult)
        For Each ScheduleResult In Schedule
            Dim TRegionId = ScheduleResult.Schedule(0).RegionId
            Dim Region = Regions.Find(Function(R) R.RegionId = TRegionId)

            Dim TimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(Region.TimeZone)

            If ScheduleResult.Schedule.Last().GameDate < Date.UtcNow.AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours) Then
                Continue For
            End If

            If Region.EntityType = "referee" Then
                NewScheduleResults.Add(ScheduleResult)
            ElseIf Region.EntityType = "team" Then
                NewScheduleResults.Add(ScheduleResult)
            ElseIf Region.EntityType = "league" Then
                Dim RealTeamIds As New List(Of String)
                For Each ScheduleItem In ScheduleResult.Schedule
                    Dim HomeTeam = Teams.Find(Function(T) T.RegionId = TRegionId AndAlso T.RealTeamId <> "" AndAlso T.TeamId = ScheduleItem.HomeTeam)
                    Dim AwayTeam = Teams.Find(Function(T) T.RegionId = TRegionId AndAlso T.RealTeamId <> "" AndAlso T.TeamId = ScheduleItem.AwayTeam)
                    If HomeTeam IsNot Nothing AndAlso Not RealTeamIds.Contains(HomeTeam.RealTeamId) Then RealTeamIds.Add(HomeTeam.RealTeamId)
                    If AwayTeam IsNot Nothing AndAlso Not RealTeamIds.Contains(AwayTeam.RealTeamId) Then RealTeamIds.Add(AwayTeam.RealTeamId)
                Next

                For Each RealTeamId In RealTeamIds
                    Dim NewScheduleResult = New ScheduleResult
                    NewScheduleResult.ScheduleTeamConfirms = ScheduleResult.ScheduleTeamConfirms

                    Dim LastFoundTeamId As Integer = -1
                    Dim I As Integer = 0
                    For Each ScheduleItem In ScheduleResult.Schedule
                        Dim HomeTeam = Teams.Find(Function(T) T.RegionId = TRegionId AndAlso T.RealTeamId <> "" AndAlso T.TeamId = ScheduleItem.HomeTeam AndAlso T.RealTeamId = RealTeamId)
                        Dim AwayTeam = Teams.Find(Function(T) T.RegionId = TRegionId AndAlso T.RealTeamId <> "" AndAlso T.TeamId = ScheduleItem.AwayTeam AndAlso T.RealTeamId = RealTeamId)

                        NewScheduleResult.Schedule.Add(ConvertToLinkScheduleItem(ScheduleItem, RealTeamId, RegionLeaguePayContracteds))
                        If HomeTeam IsNot Nothing OrElse AwayTeam IsNot Nothing Then LastFoundTeamId = I
                        I += 1
                    Next

                    If LastFoundTeamId <> ScheduleResult.Schedule.Count - 1 Then
                        NewScheduleResult.Schedule.RemoveRange(LastFoundTeamId + 1, NewScheduleResult.Schedule.Count - 1 - LastFoundTeamId)
                        Dim ClonedItem = ScheduleResult.Schedule.Last().CloneItem()
                        ClonedItem.IsDeleted = True
                        ScheduleResult.Schedule.Add(ClonedItem)
                    End If

                    NewScheduleResults.Add(NewScheduleResult)
                Next
            End If
        Next
        Schedule = NewScheduleResults

        MergeTeamData(Schedule)

        For Each RegionUser In RegionUsers
            If Not RegionUsersByRealUsername.ContainsKey(RegionUser.RealUsername) Then
                RegionUsersByRealUsername.Add(RegionUser.RealUsername, New List(Of RegionUser))
            End If
            RegionUsersByRealUsername(RegionUser.RealUsername).Add(RegionUser)
        Next

        For Each RegionUserByRealUsername In RegionUsersByRealUsername
            If RegionUserByRealUsername.Key <> "" Then
                Dim UniqueUserNames As New List(Of Tuple(Of String, String))

                For Each RegionUser In RegionUserByRealUsername.Value
                    Dim FoundUniqueUserName As Boolean = False

                    Dim RUF As String = RegionUser.FirstName.ToLower
                    Dim RUL As String = RegionUser.LastName.ToLower

                    For Each UniqueUsername In UniqueUserNames
                        If UniqueUsername.Item1 = RUF AndAlso UniqueUsername.Item2 = RUL Then
                            FoundUniqueUserName = True
                        End If
                    Next

                    If Not FoundUniqueUserName Then
                        UniqueUserNames.Add(New Tuple(Of String, String)(RUF, RUL))
                    End If
                Next

                For Each UniqueUsername In UniqueUserNames
                    Dim UniqueRegionUsersItem As New List(Of RegionUser)

                    Dim TTuple = New Tuple(Of List(Of RegionUser), List(Of Tuple(Of Schedule, Schedule, RegionProperties)))(UniqueRegionUsersItem, New List(Of Tuple(Of Schedule, Schedule, RegionProperties)))
                    UniqueRegionUsers.Add(TTuple)

                    For Each RegionUser In RegionUserByRealUsername.Value
                        Dim RUF As String = RegionUser.FirstName.ToLower
                        Dim RUL As String = RegionUser.LastName.ToLower

                        If UniqueUsername.Item1 = RUF AndAlso UniqueUsername.Item2 = RUL Then
                            UniqueRegionUsersItem.Add(RegionUser)
                            If Not UniqueRegionUsersSearch.ContainsKey(RegionUser.RegionId) Then UniqueRegionUsersSearch.Add(RegionUser.RegionId, New Dictionary(Of String, Tuple(Of List(Of RegionUser), List(Of Tuple(Of Schedule, Schedule, RegionProperties)))))
                            If Not UniqueRegionUsersSearch(RegionUser.RegionId).ContainsKey(RegionUser.Username) Then UniqueRegionUsersSearch(RegionUser.RegionId).Add(RegionUser.Username, TTuple)
                        End If
                    Next

                Next
            End If
        Next

        Dim ScheduleRegion As New Dictionary(Of String, List(Of ScheduleResult))

        For Each Region In Regions
            If Not ScheduleRegion.ContainsKey(Region.RegionId) Then ScheduleRegion.Add(Region.RegionId, New List(Of ScheduleResult))
        Next

        For Each ScheduleItem In Schedule
            If Not ScheduleRegion.ContainsKey(ScheduleItem.Schedule(0).RegionId) Then ScheduleRegion.Add(ScheduleItem.Schedule(0).RegionId, New List(Of ScheduleResult))
            ScheduleRegion(ScheduleItem.Schedule(0).RegionId).Add(ScheduleItem)
        Next

        For Each ScheduleRegionItem In ScheduleRegion
            Dim Region = Regions.Find(Function(R) R.RegionId = ScheduleRegionItem.Key)

            Dim TimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(Region.TimeZone)

            For Each ScheduleItem In ScheduleRegionItem.Value
                If Region.EntityType = "referee" Then
                    Dim UniqueOfficials = GetUniqueOfficialIdsFromSchedule(ScheduleItem.Schedule, RegionUsers)

                    For Each UniqueOfficial In UniqueOfficials
                        If UniqueRegionUsersSearch(Region.RegionId).ContainsKey(UniqueOfficial) Then
                            Dim UniqueRegionUsersSearchItem = UniqueRegionUsersSearch(Region.RegionId)(UniqueOfficial)

                            Dim NewSchedule = ScheduleItem.Schedule.Last()
                            Dim OldSchedule As Schedule = Nothing
                            Dim VersionId As Integer = 0

                            Dim ScheduleConfirm = ScheduleItem.ScheduleConfirms.Find(Function(SC) SC.Username = UniqueOfficial)
                            If NewSchedule.IsDeleted OrElse Not NewSchedule.ContainsOfficialId(UniqueOfficial) Then
                                NewSchedule = Nothing
                            End If

                            If ScheduleConfirm IsNot Nothing Then

                                VersionId = Math.Min(ScheduleConfirm.VersionId, ScheduleItem.Schedule.Count)

                                If NewSchedule IsNot Nothing Then
                                    For I As Integer = VersionId To ScheduleItem.Schedule.Count - 1
                                        If ScheduleItem.Schedule(I).IsDeleted OrElse Not ScheduleItem.Schedule(I).ContainsOfficialId(UniqueOfficial) Then
                                            VersionId = 0
                                            ScheduleConfirm = Nothing
                                            Exit For
                                        End If
                                    Next
                                Else
                                    For I As Integer = VersionId To ScheduleItem.Schedule.Count - 1
                                        If Not ScheduleItem.Schedule(I).IsDeleted AndAlso ScheduleItem.Schedule(I).ContainsOfficialId(UniqueOfficial) Then
                                            VersionId = 0
                                            ScheduleConfirm = Nothing
                                            Exit For
                                        End If
                                    Next
                                End If

                                If ScheduleConfirm IsNot Nothing Then
                                    OldSchedule = ScheduleItem.Schedule(VersionId - 1)
                                    If OldSchedule.IsDeleted OrElse Not OldSchedule.ContainsOfficialId(UniqueOfficial) Then
                                        OldSchedule = Nothing
                                    End If
                                End If
                            End If

                            If OldSchedule Is Nothing AndAlso NewSchedule Is Nothing Then
                                For I As Integer = VersionId To ScheduleItem.Schedule.Count - 1
                                    If Not ScheduleItem.Schedule(I).IsDeleted AndAlso ScheduleItem.Schedule(I).ContainsOfficialId(UniqueOfficial) Then
                                        OldSchedule = ScheduleItem.Schedule(I)
                                    End If
                                Next
                            End If

                            If NewSchedule IsNot Nothing AndAlso OldSchedule IsNot Nothing Then
                                Dim PositionStatus = RequiredGameNotificationPosition(NewSchedule, OldSchedule, UniqueOfficial)
                                If PositionStatus = "Removed" Then
                                    UniqueRegionUsersSearchItem.Item2.Add(New Tuple(Of Schedule, Schedule, RegionProperties)(Nothing, OldSchedule, Region))
                                ElseIf PositionStatus = "Added" Then
                                    UniqueRegionUsersSearchItem.Item2.Add(New Tuple(Of Schedule, Schedule, RegionProperties)(NewSchedule, Nothing, Region))
                                ElseIf PositionStatus = "ChangedPosition" Then
                                    UniqueRegionUsersSearchItem.Item2.Add(New Tuple(Of Schedule, Schedule, RegionProperties)(NewSchedule, OldSchedule, Region))
                                Else
                                    If RequiredGameNotification(NewSchedule, OldSchedule) OrElse RequiredGameNotificationFine(NewSchedule, OldSchedule, UniqueOfficial) Then
                                        UniqueRegionUsersSearchItem.Item2.Add(New Tuple(Of Schedule, Schedule, RegionProperties)(NewSchedule, OldSchedule, Region))
                                    End If
                                End If
                            ElseIf NewSchedule IsNot Nothing AndAlso OldSchedule Is Nothing Then
                                UniqueRegionUsersSearchItem.Item2.Add(New Tuple(Of Schedule, Schedule, RegionProperties)(NewSchedule, Nothing, Region))
                            ElseIf NewSchedule Is Nothing AndAlso OldSchedule IsNot Nothing Then
                                UniqueRegionUsersSearchItem.Item2.Add(New Tuple(Of Schedule, Schedule, RegionProperties)(Nothing, OldSchedule, Region))
                            ElseIf NewSchedule Is Nothing AndAlso OldSchedule Is Nothing Then
                                'Do Nothing
                            End If
                        End If
                    Next
                ElseIf Region.EntityType = "team" Then
                    Dim UniqueOfficials = GetUniqueOfficialIdsFromScheduleTeam(ScheduleItem.Schedule, RegionUsers)

                    For Each UniqueOfficial In UniqueOfficials
                        If UniqueRegionUsersSearch.ContainsKey(Region.RegionId) AndAlso UniqueRegionUsersSearch(Region.RegionId).ContainsKey(UniqueOfficial) Then
                            Dim UniqueRegionUsersSearchItem = UniqueRegionUsersSearch(Region.RegionId)(UniqueOfficial)

                            Dim NewSchedule = ScheduleItem.Schedule.Last()
                            Dim OldSchedule As Schedule = Nothing
                            Dim VersionId As Integer = 0

                            Dim ScheduleTeamConfirm = ScheduleItem.ScheduleTeamConfirms.Find(Function(SC) SC.Username = UniqueOfficial)

                            If NewSchedule.IsDeleted OrElse NewSchedule.IsCancelled() OrElse Not NewSchedule.ContainsTeamMemberId(UniqueOfficial, RegionUsers) Then
                                NewSchedule = Nothing
                            End If

                            If ScheduleTeamConfirm IsNot Nothing Then
                                If ScheduleTeamConfirm.Confirmed = 0 Then
                                    VersionId = 0
                                    ScheduleTeamConfirm = Nothing
                                Else
                                    VersionId = Math.Min(ScheduleTeamConfirm.VersionId, ScheduleItem.Schedule.Count)

                                    If NewSchedule IsNot Nothing Then
                                        For I As Integer = VersionId To ScheduleItem.Schedule.Count - 1
                                            If ScheduleItem.Schedule(I).IsDeleted OrElse ScheduleItem.Schedule(I).IsCancelled OrElse Not ScheduleItem.Schedule(I).ContainsTeamMemberId(UniqueOfficial, RegionUsers) Then
                                                VersionId = 0
                                                ScheduleTeamConfirm = Nothing
                                                Exit For
                                            End If
                                        Next
                                    Else
                                        For I As Integer = VersionId To ScheduleItem.Schedule.Count - 1
                                            If Not ScheduleItem.Schedule(I).IsDeleted AndAlso Not ScheduleItem.Schedule(I).IsCancelled AndAlso ScheduleItem.Schedule(I).ContainsTeamMemberId(UniqueOfficial, RegionUsers) Then
                                                VersionId = 0
                                                ScheduleTeamConfirm = Nothing
                                                Exit For
                                            End If
                                        Next
                                    End If

                                    If ScheduleTeamConfirm IsNot Nothing Then
                                        OldSchedule = ScheduleItem.Schedule(VersionId - 1)
                                        If OldSchedule.IsDeleted OrElse OldSchedule.IsCancelled OrElse Not OldSchedule.ContainsTeamMemberId(UniqueOfficial, RegionUsers) Then
                                            OldSchedule = Nothing
                                        End If
                                    End If
                                End If
                            End If

                            If OldSchedule Is Nothing AndAlso NewSchedule Is Nothing Then
                                For I As Integer = VersionId To ScheduleItem.Schedule.Count - 1
                                    If Not ScheduleItem.Schedule(I).IsDeleted AndAlso Not ScheduleItem.Schedule(I).IsCancelled AndAlso ScheduleItem.Schedule(I).ContainsTeamMemberId(UniqueOfficial, RegionUsers) Then
                                        OldSchedule = ScheduleItem.Schedule(I)
                                    End If
                                Next
                            End If

                            Dim GameDate = ScheduleItem.Schedule.Last().GameDate

                            If NewSchedule IsNot Nothing OrElse OldSchedule IsNot Nothing Then
                                If NewSchedule Is Nothing AndAlso OldSchedule IsNot Nothing Then
                                    UniqueRegionUsersSearchItem.Item2.Add(New Tuple(Of Schedule, Schedule, RegionProperties)(Nothing, OldSchedule, Region))
                                ElseIf NewSchedule IsNot Nothing AndAlso OldSchedule Is Nothing Then
                                    If Date.UtcNow.AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours).AddDays(Region.EmailConfirmDaysBefore) > GameDate Then
                                        UniqueRegionUsersSearchItem.Item2.Add(New Tuple(Of Schedule, Schedule, RegionProperties)(NewSchedule, Nothing, Region))
                                    End If
                                ElseIf NewSchedule IsNot Nothing AndAlso OldSchedule IsNot Nothing Then
                                    If Date.UtcNow.AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours).AddDays(Region.EmailConfirmDaysBefore) > GameDate Then
                                        If RequiredGameNotification(NewSchedule, OldSchedule) Then
                                            UniqueRegionUsersSearchItem.Item2.Add(New Tuple(Of Schedule, Schedule, RegionProperties)(NewSchedule, OldSchedule, Region))
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    Next
                End If
            Next
        Next

        For Each UniqueRegionUser In UniqueRegionUsers
            Dim UserIndex = Users.BinarySearch(New User With {.Username = UniqueRegionUser.Item1.First().RealUsername}, UmpireAssignor.User.BasicSorter)

            If UserIndex < 0 Then Continue For
            Dim User = Users(UserIndex)

            If Not User.EmailGamesRequiringConfirm Then Continue For

            UniqueRegionUser.Item2.Sort(New GenericIComparer(Of Tuple(Of Schedule, Schedule, RegionProperties))(Function(V1, V2)
                                                                                                                    Dim VV1 = If(V1.Item1 IsNot Nothing, V1.Item1, V1.Item2)
                                                                                                                    Dim VV2 = If(V2.Item1 IsNot Nothing, V2.Item1, V2.Item2)
                                                                                                                    Dim Comp As Integer = VV1.GameDate.CompareTo(VV2.GameDate)
                                                                                                                    If Comp = 0 Then Comp = VV1.ParkId.CompareTo(VV2.ParkId)
                                                                                                                    If Comp = 0 Then Comp = VV1.RegionId.CompareTo(VV2.RegionId)
                                                                                                                    If Comp = 0 Then Comp = VV1.ScheduleId.CompareTo(VV2.ScheduleId)
                                                                                                                    Return Comp
                                                                                                                End Function))

            If UniqueRegionUser.Item2.Count > 0 Then

                Dim GameEmail As New GameEmail With {
                .User = User,
                .IsConfirmGame = True,
                .ScheduleData = New List(Of ScheduleDataEmail),
                .RegionUsers = RegionUsers,
                .RegionProperties = Regions,
                .RegionParks = Parks,
                .RegionLeagues = RegionLeagues,
                .OfficialRegions = OfficialRegions,
                .Teams = Teams
            }

                For Each GIA In UniqueRegionUser.Item2
                    Dim RegionUser = UniqueRegionUser.Item1.Find(Function(RU) RU.RegionId = GIA.Item3.RegionId)

                    GameEmail.ScheduleData.Add(New ScheduleDataEmail(RegionUser, GIA.Item1, GIA.Item2, "", GIA.Item3, False))
                Next

                GameEmail.Send(Nothing)
            End If
        Next

    End Sub

    Public Shared Sub SMSTomorrowsGames(ByRef Users As List(Of User))
        Dim RegionUsersByRealUsername As New Dictionary(Of String, List(Of RegionUser))
        Dim UniqueRegionUsers As New List(Of Tuple(Of List(Of RegionUser), List(Of Schedule)))
        Dim UniqueRegionUsersSearch As New Dictionary(Of String, Dictionary(Of String, Tuple(Of List(Of RegionUser), List(Of Schedule))))

        Dim Regions As List(Of RegionProperties) = Nothing
        Dim Schedule As New List(Of Schedule)
        Dim ScheduleAvailable As New List(Of Schedule)
        Dim RegionUsers As List(Of RegionUser) = Nothing
        Dim RegionLeagues As List(Of RegionLeaguePayContracted) = Nothing
        Dim RegionLeaguePayContracteds As List(Of RegionLeaguePayContracted) = Nothing
        Dim Parks As List(Of Park) = Nothing
        Dim Teams As List(Of Team) = Nothing

        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()
                If Users Is Nothing Then Users = User.GetAllUsersInfoHelper(SqlConnection, SqlTransaction)

                Dim TNow = Date.UtcNow

                Dim MinDate = New Date(TNow.Year, TNow.Month, TNow.Day).AddDays(-1)
                Dim MaxDate = New Date(TNow.Year, TNow.Month, TNow.Day).AddDays(3)

                Dim TSchedule = UmpireAssignor.Schedule.GetMasterScheduleFromRegionsNonVersionAllRegionsHelper(MinDate, MaxDate, SqlConnection, SqlTransaction)
                UmpireAssignor.SchedulePosition.GetMasterSchedulePositionFromRegionsNonVersionAllRegionsHelper(MinDate, MaxDate, TSchedule, SqlConnection, SqlTransaction)

                Schedule.AddRange(TSchedule)

                Dim UniqueRegionIdsInSchedule = TSchedule.Select(Function(S) S.RegionId).Distinct().ToList()
                
                Regions = RegionProperties.GetAllRegionPropertiesHelper(SqlConnection, SqlTransaction)
                Regions = Regions.FindAll(Function (R) UniqueRegionIdsInSchedule.Contains(R.RegionId))
                RegionUsers = RegionUser.GetAllRegionUsersHelper(SqlConnection, SqlTransaction)
                Dim TRegionLeagues = RegionLeague.GetAllRegionLeaguesPayContractedHelper(SqlConnection, SqlTransaction)
                Teams = Team.GetAllTeamsHelper(SqlConnection, SqlTransaction)

                RegionLeagues = RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(TRegionLeagues.Item1, New List(Of RegionLeaguePay))
                RegionLeaguePayContracteds = TRegionLeagues.Item2

                Parks = Park.GetAllParksHelper(RegionLeaguePayContracteds, SqlConnection, SqlTransaction)
            End Using
        End Using

        Dim TTSchedule As New List(Of Schedule)
        For Each ScheduleItem In Schedule
            Try
            If ScheduleItem.GameStatus <> "cancelled" AndAlso ScheduleItem.GameStatus <> "weather" Then
                Dim TRegionId As String = ScheduleItem.RegionId
                Dim Region = Regions.Find(Function(R) R.RegionId = TRegionId)

                Dim TimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(Region.TimeZone)

                Dim GameDate = ScheduleItem.GameDate
                If GameDate < Date.UtcNow.AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours - 4).AddDays(1) OrElse GameDate > Date.UtcNow.AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours).AddDays(2) Then Continue For

                If GameDate.Day <> Date.UtcNow.AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours - 4).Day + 1 Then Continue For

                If Region.EntityType = "team" OrElse Region.EntityType = "referee" Then
                        TTSchedule.Add(ScheduleItem)
                    ElseIf Region.EntityType = "league" Then
                        Dim RealTeamIds As New List(Of String)

                        Dim HomeTeam As Team = Teams.Find(Function(T) T.RealTeamId <> "" AndAlso T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.HomeTeam.ToLower)
                        Dim AwayTeam As Team = Teams.Find(Function(T) T.RealTeamId <> "" AndAlso T.RegionId = ScheduleItem.RegionId AndAlso T.TeamId = ScheduleItem.AwayTeam.ToLower)

                        If HomeTeam IsNot Nothing AndAlso Not RealTeamIds.Contains(HomeTeam.RealTeamId) Then RealTeamIds.Add(HomeTeam.RealTeamId)
                        If AwayTeam IsNot Nothing AndAlso Not RealTeamIds.Contains(AwayTeam.RealTeamId) Then RealTeamIds.Add(AwayTeam.RealTeamId)

                        For Each RealTeamId In RealTeamIds
                            TTSchedule.Add(ConvertToLinkScheduleItem(ScheduleItem, RealTeamId, RegionLeaguePayContracteds))
                        Next
                    End If
                End If
                Catch
                End Try
        Next
        Schedule = TTSchedule

        For Each RegionUser In RegionUsers
            If Not RegionUsersByRealUsername.ContainsKey(RegionUser.RealUsername) Then
                RegionUsersByRealUsername.Add(RegionUser.RealUsername, New List(Of RegionUser))
            End If
            RegionUsersByRealUsername(RegionUser.RealUsername).Add(RegionUser)
        Next

        For Each RegionUserByRealUsername In RegionUsersByRealUsername
            Try
            If RegionUserByRealUsername.Key <> "" Then
                Dim UniqueUserNames As New List(Of Tuple(Of String, String))

                For Each RegionUser In RegionUserByRealUsername.Value
                    Dim FoundUniqueUserName As Boolean = False

                    Dim RUF As String = RegionUser.FirstName.ToLower
                    Dim RUL As String = RegionUser.LastName.ToLower

                    For Each UniqueUsername In UniqueUserNames
                        If UniqueUsername.Item1 = RUF AndAlso UniqueUsername.Item2 = RUL Then
                            FoundUniqueUserName = True
                        End If
                    Next

                    If Not FoundUniqueUserName Then
                        UniqueUserNames.Add(New Tuple(Of String, String)(RUF, RUL))
                    End If
                Next

                For Each UniqueUsername In UniqueUserNames
                    Dim UniqueRegionUsersItem As New List(Of RegionUser)

                    Dim TTuple = New Tuple(Of List(Of RegionUser), List(Of Schedule))(UniqueRegionUsersItem, New List(Of Schedule))
                    UniqueRegionUsers.Add(TTuple)

                    For Each RegionUser In RegionUserByRealUsername.Value
                        Dim RUF As String = RegionUser.FirstName.ToLower
                        Dim RUL As String = RegionUser.LastName.ToLower

                        If UniqueUsername.Item1 = RUF AndAlso UniqueUsername.Item2 = RUL Then
                            UniqueRegionUsersItem.Add(RegionUser)
                            If Not UniqueRegionUsersSearch.ContainsKey(RegionUser.RegionId) Then UniqueRegionUsersSearch.Add(RegionUser.RegionId, New Dictionary(Of String, Tuple(Of List(Of RegionUser), List(Of Schedule))))
                            If Not UniqueRegionUsersSearch(RegionUser.RegionId).ContainsKey(RegionUser.Username) Then UniqueRegionUsersSearch(RegionUser.RegionId).Add(RegionUser.Username, TTuple)
                        End If
                    Next

                Next
            End If
                Catch
                End Try
        Next

        Dim ScheduleRegion As New Dictionary(Of String, List(Of Schedule))

        For Each Region In Regions
            If Not ScheduleRegion.ContainsKey(Region.RegionId) Then ScheduleRegion.Add(Region.RegionId, New List(Of Schedule))
        Next

        For Each ScheduleItem In Schedule
            If Not ScheduleRegion.ContainsKey(ScheduleItem.RegionId) Then ScheduleRegion.Add(ScheduleItem.RegionId, New List(Of Schedule))
            ScheduleRegion(ScheduleItem.RegionId).Add(ScheduleItem)
        Next

        For Each ScheduleRegionItem In ScheduleRegion
            Dim Region = Regions.Find(Function(R) R.RegionId = ScheduleRegionItem.Key)

            Try
                For Each ScheduleItem In ScheduleRegionItem.Value
                    If Region.EntityType = "referee" Then
                        Dim UniqueOfficials = GetUniqueOfficialIdsFromSchedule({ScheduleItem}.ToList(), RegionUsers)

                        For Each UniqueOfficial In UniqueOfficials
                            If UniqueRegionUsersSearch.ContainsKey(Region.RegionId) AndAlso UniqueRegionUsersSearch(Region.RegionId).ContainsKey(UniqueOfficial) Then
                                Dim UniqueRegionUsersSearchItem = UniqueRegionUsersSearch(Region.RegionId)(UniqueOfficial)
                                Dim NewSchedule = ScheduleItem


                                If Not NewSchedule.ContainsOfficialIdOnPosition(UniqueOfficial) OrElse (NewSchedule.GameStatus <> "" AndAlso NewSchedule.GameStatus <> "normal") Then
                                    NewSchedule = Nothing
                                End If

                                If NewSchedule IsNot Nothing Then
                                    UniqueRegionUsersSearchItem.Item2.Add(NewSchedule)
                                End If

                            End If
                        Next
                    ElseIf Region.EntityType = "team" Then
                        Dim UniqueOfficials = GetUniqueOfficialIdsFromScheduleTeam({ScheduleItem}.ToList(), RegionUsers)

                        For Each UniqueOfficial In UniqueOfficials
                            If UniqueRegionUsersSearch.ContainsKey(Region.RegionId) AndAlso UniqueRegionUsersSearch(Region.RegionId).ContainsKey(UniqueOfficial) Then
                                Dim UniqueRegionUsersSearchItem = UniqueRegionUsersSearch(Region.RegionId)(UniqueOfficial)
                                Dim NewSchedule = ScheduleItem


                                If Not NewSchedule.ContainsTeamMemberId(UniqueOfficial, RegionUsers) OrElse (NewSchedule.GameStatus <> "" AndAlso NewSchedule.GameStatus <> "normal") Then
                                    NewSchedule = Nothing
                                End If

                                If NewSchedule IsNot Nothing Then
                                    UniqueRegionUsersSearchItem.Item2.Add(NewSchedule)
                                End If

                            End If
                        Next
                    End If
                Next
            Catch E As Exception
                PublicCode.SendEmailStandard("integration@asportsmanager.com", "Error SMSTomorrowgames for region " & ScheduleRegionItem.Key, E.Message & vbCrLf & vbCrLf & E.StackTrace)
            End Try
        Next

        For Each UniqueRegionUser In UniqueRegionUsers
            Try
                Dim UserIndex = Users.BinarySearch(New User With {.Username = UniqueRegionUser.Item1.First().RealUsername}, UmpireAssignor.User.BasicSorter)

                If UserIndex < 0 Then Continue For
                If UniqueRegionUser.Item2.Count = 0 Then Continue For

                Dim User = Users(UserIndex)
                If Not User.SMSGameReminders Then Continue For

                Dim UserCellNumber As String = User.GetCellPhoneNumber()
                If UserCellNumber = "" Then Continue For

                UniqueRegionUser.Item2.Sort(New GenericIComparer(Of Schedule)(Function(V1, V2)
                                                                                  Dim Comp As Integer = V1.GameDate.CompareTo(V2.GameDate)
                                                                                  If Comp = 0 Then Comp = V1.ParkId.CompareTo(V2.ParkId)
                                                                                  If Comp = 0 Then Comp = V1.RegionId.CompareTo(V2.RegionId)
                                                                                  If Comp = 0 Then Comp = V1.ScheduleId.CompareTo(V2.ScheduleId)
                                                                                  Return Comp
                                                                              End Function))


                Dim T As String = "Text"
                Dim L As String = User.PreferredLanguage
                Dim CI As New Globalization.CultureInfo(L)

                Dim SMSScheduleBuilder As New StringBuilder()

                Dim TNow = Date.UtcNow.AddHours(PublicCode.OlsonTimeZoneToTimeZoneInfo(User.TimeZone).GetUtcOffset(Date.UtcNow).TotalHours)

                Dim DateText As String = TNow.AddDays(1).ToString("ddddd", CI) & ", " & TNow.AddDays(1).ToString("m", CI)
                Dim GameCountText As String = Languages.GetText("Generic", If(UniqueRegionUser.Item2.Count > 1, "GamePlural", "GameSingular"), T, L)

                SMSScheduleBuilder.Append(String.Format(Languages.GetText("SMSGameReminder", "Body", T, L), User.FirstName, UniqueRegionUser.Item2.Count, GameCountText, DateText))


                For Each GIA In UniqueRegionUser.Item2
                    SMSScheduleBuilder.Append(vbCrLf)
                    SMSScheduleBuilder.Append(vbCrLf)
                    GIA.SMSGame(SMSScheduleBuilder, Regions, UniqueRegionUser.Item1, RegionUsers, Parks, RegionLeagues, Teams, L, False)
                    SMSScheduleBuilder.Append(".")
                Next

                Dim SMSMessage As String = SMSScheduleBuilder.ToString()
                Try
                    PublicCode.SendSMS(UserCellNumber, SMSMessage)
                Catch
                End Try
            Catch E As Exception
                Dim RealUsername As String = "Unknown User"
                If UniqueRegionUser.Item1 Is Nothing AndAlso UniqueRegionUser.Item1.First IsNot Nothing Then
                    RealUsername = UniqueRegionUser.Item1.First.RealUsername
                End If

                PublicCode.SendEmailStandard("integration@asportsmanager.com", "Error SMSTomorrowgames for " & RealUsername, E.Message & vbCrLf & vbCrLf & E.StackTrace)
            End Try
        Next
    End Sub

    Public Sub SMSGame(SMSScheduleBuilder As StringBuilder, Regions As List(Of RegionProperties), MyRegionUsers As List(Of RegionUser), RegionUsers As List(Of RegionUser), Parks As List(Of Park), RegionLeagues As List(Of RegionLeaguePayContracted), Teams As List(Of Team), L As String, DoSendDate As Boolean)
        Dim GIA = Me
        Dim T As String = "Text"
        Dim CI As New Globalization.CultureInfo(L)

        Dim MyPositionId As String = ""

        Dim MyRegion = Regions.Find(Function(R) R.RegionId = RegionId)

        If MyRegion.EntityType = "referee" Then
            For Each SchedulePosition In GIA.SchedulePositions
                Dim DoExitFor As Boolean = False
                For Each RegionUser In MyRegionUsers
                    If RegionUser.RegionId = GIA.RegionId AndAlso RegionUser.Username = SchedulePosition.OfficialId.Trim.ToLower Then
                        MyPositionId = SchedulePosition.PositionId
                        DoExitFor = True
                        Exit For
                    End If
                Next
                If DoExitFor Then Exit For
            Next
        End If

        Dim NotMyRegionUsers As New List(Of String)
        If MyRegion.EntityType = "referee" Then
            For Each SchedulePosition In GIA.SchedulePositions
                Dim PosOfficialId As String = SchedulePosition.OfficialId.Trim.ToLower
                For Each RegionUser In MyRegionUsers
                    If RegionUser.RegionId = GIA.RegionId AndAlso RegionUser.Username <> PosOfficialId AndAlso PosOfficialId <> "" Then
                        If Not NotMyRegionUsers.Contains(PosOfficialId) Then
                            NotMyRegionUsers.Add(PosOfficialId)
                        End If
                    End If
                Next
            Next
        End If


        Dim LinkedRegionId As String = GIA.LinkedRegionId
        If LinkedRegionId = "" Then LinkedRegionId = GIA.RegionId

        Dim ParkIndex As Integer = Parks.BinarySearch(New Park With {.RegionId = LinkedRegionId, .ParkId = GIA.ParkId.Trim().ToLower}, Park.BasicSorter)
        Dim ParkText As String = If(ParkIndex >= 0, Parks(ParkIndex).ParkName, GIA.ParkId)

        Dim HomeTeamIndex As Integer = Teams.BinarySearch(New Team With {.RegionId = LinkedRegionId, .TeamId = GIA.HomeTeam.Trim().ToLower}, Team.BasicSorter)
        Dim HomeTeamText As String = If(HomeTeamIndex >= 0, Teams(HomeTeamIndex).TeamName, GIA.HomeTeam)

        Dim AwayTeamIndex As Integer = Teams.BinarySearch(New Team With {.RegionId = LinkedRegionId, .TeamId = GIA.AwayTeam.Trim().ToLower}, Team.BasicSorter)
        Dim AwayTeamText As String = If(AwayTeamIndex >= 0, Teams(AwayTeamIndex).TeamName, GIA.AwayTeam)

        Dim LeagueIndex As Integer = RegionLeagues.BinarySearch(New RegionLeaguePayContracted With {.RegionId = GIA.RegionId, .LeagueId = GIA.LeagueId.Trim().ToLower}, RegionLeaguePayContracted.BasicSorter)
        Dim LeagueText As String = If(LeagueIndex >= 0, RegionLeagues(LeagueIndex).LeagueName, GIA.LeagueId)

        Dim DateText As String = GIA.GameDate.ToString("ddddd", CI) & ", " & GIA.GameDate.ToString("m", CI)

        Dim GameString As String = ""

        Dim Comment = GIA.GameComment

        If GIA.ScheduleCommentTeams.Count = 1 Then
            If Comment <> "" Then Comment = Comment & " | "
            Comment = GIA.ScheduleCommentTeams(0).Comment
        End If

        Dim GameFormat As String = "GameFormat"
        If MyRegion.EntityType = "referee" Then
            GameFormat = "GameFormat"
            If HomeTeamText.Trim = "" AndAlso AwayTeamText.Trim = "" Then
                GameFormat = "GameFormatNoTeams"
            End If
        Else
            GameFormat = "GameFormatTeam"
            If HomeTeamText.Trim = "" AndAlso AwayTeamText.Trim = "" Then
                GameFormat = "GameFormatTeamNoTeams"
            End If
        End If

        'Get Partner
        If MyRegion.RegionIsLadderLeague Then
            Dim PartnerName = ""
            If NotMyRegionUsers.Count = 1 Then
                PartnerName = RegionUser.GetFullNameFromRegionUsers(MyRegion.RegionId, NotMyRegionUsers(0), RegionUsers)
            End If

            Dim SMSBodyFormat = If(PartnerName = "",
                                            If (Comment.Trim() = "", 
                                                "GameFormatLadderLeagueNoPartnerNoComment",
                                                "GameFormatLadderLeagueNoPartner"
                                            ), 
                                            If(Comment.Trim() = "",
                                                "GameFormatLadderLeagueNoComment",
                                                "GameFormatLadderLeague"
                                           )
                                       )
            Dim SMSBodyFormatText = Languages.GetText("SMSGameReminder", SMSBodyFormat, T, L)
            Dim GameTypeText = Languages.GetText("Generic", GIA.GameType, T, L)
            Dim GameTimeText = GIA.GameDate.ToString("HH:mm", CI) & If(DoSendDate, ", " & DateText, "")

            GameString = String.Format(SMSBodyFormatText, GameTypeText, GameTimeText, ParkText, Comment, PartnerName) 
            
            SMSScheduleBuilder.Append(GameString)
            If GameScore IsNot Nothing AndAlso GameScore.Score IsNot Nothing AndAlso GameScore.Score.Count > 0 Then
                SMSScheduleBuilder.Append(", " & GameScore.ScoreToString)
            End If

            Exit Sub
        End If

        GameString = String.Format(Languages.GetText("SMSGameReminder", GameFormat, T, L),
                                   "#" & GIA.GameNumber, '0
                                   Languages.GetText("Generic", GIA.GameType, T, L), '1
                                   GIA.GameDate.ToString("HH:mm", CI) & If(DoSendDate, ", " & DateText, ""), '2
                                   LeagueText, Languages.GetText("Generic", MyPositionId, T, L), '3
                                   ParkText, '4
                                   HomeTeamText, '5
                                   AwayTeamText, '6
                                   Comment)  '7

        SMSScheduleBuilder.Append(GameString)
        If GameScore IsNot Nothing AndAlso GameScore.Score IsNot Nothing AndAlso GameScore.Score.Count > 0 Then
            SMSScheduleBuilder.Append(", " & GameScore.ScoreToString)
        End If

        If MyRegion.EntityType = "team" Then
            Dim PList As New List(Of Tuple(Of String, String))
            For Each SchedulePosition In GIA.SchedulePositions
                If SchedulePosition.PositionId <> MyPositionId AndAlso SchedulePosition.OfficialId.Trim <> "" Then
                    PList.Add(New Tuple(Of String, String)(SchedulePosition.PositionId, SchedulePosition.OfficialId))
                End If
            Next

            If PList.Count > 0 Then
                SMSScheduleBuilder.Append(" ")
                SMSScheduleBuilder.Append(Languages.GetText("Generic", "WithLower", T, L))
                SMSScheduleBuilder.Append(" ")
            End If

            For I As Integer = 0 To PList.Count - 1
                If I = PList.Count - 1 AndAlso I <> 0 Then
                    SMSScheduleBuilder.Append(" ")
                    SMSScheduleBuilder.Append(Languages.GetText("Generic", "And", T, L))
                    SMSScheduleBuilder.Append(" ")
                Else
                    If I <> 0 Then
                        SMSScheduleBuilder.Append(", ")
                    End If
                End If

                Dim RegionUserIndex As Integer = RegionUsers.BinarySearch(New RegionUser With {.RegionId = GIA.RegionId, .Username = PList(I).Item2.Trim().ToLower}, RegionUser.BasicSorter)
                Dim RegionUserText As String = If(RegionUserIndex >= 0, RegionUsers(RegionUserIndex).FirstName & " " & RegionUsers(RegionUserIndex).LastName, PList(I).Item2)

                SMSScheduleBuilder.Append(RegionUserText)
                SMSScheduleBuilder.Append(" ")
                SMSScheduleBuilder.Append(Languages.GetText("Generic", PList(I).Item1, T, L))
            Next
        End If
    End Sub

    Public Shared Sub EmailAvailableGames(ByRef Users As List(Of User))
        Dim RegionUsersByRealUsername As New Dictionary(Of String, List(Of RegionUser))
        Dim UniqueRegionUsers As New List(Of List(Of RegionUser))

        Dim Regions As List(Of RegionProperties) = Nothing
        Dim Schedule As New List(Of Schedule)
        Dim ScheduleAvailable As New List(Of Schedule)
        Dim Availability As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of DateTime, Integer)))
        Dim RegionUsers As List(Of RegionUser) = Nothing
        Dim RegionLeagues As List(Of RegionLeaguePayContracted) = Nothing
        Dim Teams As List(Of Team) = Nothing
        Dim RegionLeaguePayContracteds As List(Of RegionLeaguePayContracted) = Nothing
        Dim Parks As List(Of Park) = Nothing

        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()
                If Users Is Nothing Then Users = User.GetAllUsersInfoHelper(SqlConnection, SqlTransaction)
                Regions = RegionProperties.GetAllRegionPropertiesHelper(SqlConnection, SqlTransaction)
                RegionUsers = RegionUser.GetAllRegionUsersHelper(SqlConnection, SqlTransaction)
                Dim TRegionLeagues = RegionLeague.GetAllRegionLeaguesPayContractedHelper(SqlConnection, SqlTransaction)

                RegionLeagues = RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(TRegionLeagues.Item1, New List(Of RegionLeaguePay))
                RegionLeaguePayContracteds = TRegionLeagues.Item2

                Parks = Park.GetAllParksHelper(RegionLeaguePayContracteds, SqlConnection, SqlTransaction)

                Teams = Team.GetAllTeamsHelper(SqlConnection, SqlTransaction)

                Dim TotalMaxDateShowAvailableSpots As Date = Date.MinValue
                Dim MinDate = Date.UtcNow.AddDays(-1)

                For Each Region In Regions
                    Dim MaxDateShowAvailableSpots = Region.MaxDateShowAvailableSpots.AddDays(1)
                    If MaxDateShowAvailableSpots > TotalMaxDateShowAvailableSpots Then TotalMaxDateShowAvailableSpots = MaxDateShowAvailableSpots

                    Availability.Add(Region.RegionId, UmpireAssignor.UserAvailability.GetGlobalAvailabilityFromRegionWithRangeHelper(Region.RegionId, MinDate, MaxDateShowAvailableSpots, SqlConnection, SqlTransaction))
                Next


                For Each Region In Regions
                    Dim TimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(Region.TimeZone)

                    Dim TMinDate = Date.UtcNow.AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours)

                    If Region.EntityType <> "referee" Then Continue For
                    Dim TSchedule = UmpireAssignor.Schedule.GetMasterScheduleFromRegionsNonVersionHelper({Region.RegionId}.ToList(), TMinDate, TotalMaxDateShowAvailableSpots, SqlConnection, SqlTransaction)
                    UmpireAssignor.SchedulePosition.GetMasterSchedulePositionFromRegionsNonVersionHelper({Region.RegionId}.ToList(), TMinDate, TotalMaxDateShowAvailableSpots, TSchedule, SqlConnection, SqlTransaction)

                    Schedule.AddRange(TSchedule)
                Next
            End Using
        End Using

        For Each RegionUser In RegionUsers
            If Not RegionUsersByRealUsername.ContainsKey(RegionUser.RealUsername) Then
                RegionUsersByRealUsername.Add(RegionUser.RealUsername, New List(Of RegionUser))
            End If
            RegionUsersByRealUsername(RegionUser.RealUsername).Add(RegionUser)
        Next

        For Each RegionUserByRealUsername In RegionUsersByRealUsername
            If RegionUserByRealUsername.Key <> "" Then
                Dim UniqueUserNames As New List(Of Tuple(Of String, String))

                For Each RegionUser In RegionUserByRealUsername.Value
                    Dim FoundUniqueUserName As Boolean = False

                    Dim RUF As String = RegionUser.FirstName.ToLower
                    Dim RUL As String = RegionUser.LastName.ToLower

                    For Each UniqueUsername In UniqueUserNames
                        If UniqueUsername.Item1 = RUF AndAlso UniqueUsername.Item2 = RUL Then
                            FoundUniqueUserName = True
                        End If
                    Next

                    If Not FoundUniqueUserName Then
                        UniqueUserNames.Add(New Tuple(Of String, String)(RUF, RUL))
                    End If
                Next

                For Each UniqueUsername In UniqueUserNames
                    Dim UniqueRegionUsersItem As New List(Of RegionUser)

                    For Each RegionUser In RegionUserByRealUsername.Value
                        Dim RUF As String = RegionUser.FirstName.ToLower
                        Dim RUL As String = RegionUser.LastName.ToLower

                        If UniqueUsername.Item1 = RUF AndAlso UniqueUsername.Item2 = RUL Then
                            UniqueRegionUsersItem.Add(RegionUser)
                        End If
                    Next

                    UniqueRegionUsers.Add(UniqueRegionUsersItem)
                Next
            End If
        Next

        Dim ScheduleRegion As New Dictionary(Of String, List(Of Schedule))
        Dim ScheduleRegionAvailable As New Dictionary(Of String, List(Of Tuple(Of Schedule, Boolean, Boolean, boolean)))

        For Each Region In Regions
            If Not ScheduleRegion.ContainsKey(Region.RegionId) Then ScheduleRegion.Add(Region.RegionId, New List(Of Schedule))
        Next

        For Each ScheduleItem In Schedule
            If Not ScheduleRegion.ContainsKey(ScheduleItem.RegionId) Then ScheduleRegion.Add(ScheduleItem.RegionId, New List(Of Schedule))
            ScheduleRegion(ScheduleItem.RegionId).Add(ScheduleItem)
        Next

        For Each ScheduleRegionItem In ScheduleRegion
            ScheduleRegionItem.Value.Sort(UmpireAssignor.Schedule.DateSorter)
        Next

        For Each ScheduleRegionItem In ScheduleRegion
            ScheduleRegionAvailable.Add(ScheduleRegionItem.Key, New List(Of Tuple(Of Schedule, Boolean, Boolean, Boolean)))
            Dim Region = Regions.Find(Function(R) R.RegionId = ScheduleRegionItem.Key)
            If Region Is Nothing Then Continue For
            If Region.EntityType <> "referee" Then Continue For

            Dim TimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(Region.TimeZone)

            Dim MaxDateShowAvailableSpots = Region.MaxDateShowAvailableSpots.AddDays(1)

            Dim CrewList = RegionLeaguePayContracted.GetSportCrewPositions()(Region.Sport)
            Dim CrewListScorekeeper = RegionLeaguePayContracted.GetSportCrewPositionsScorekeepers()(Region.Sport)
            Dim CrewListSupervisor = RegionLeaguePayContracted.GetSportCrewPositionsSupervisors()(Region.Sport)

            Dim Positions = RegionLeaguePayContracted.GetSportPositionOrder()(Region.Sport)
            Dim PositionsScorekeeper = RegionLeaguePayContracted.GetSportPositionOrderScorekeeper()(Region.Sport)
            Dim PositionsSupervisor = RegionLeaguePayContracted.GetSportPositionOrderSupervisor()(Region.Sport)

            For Each ScheduleRegionItemItem In ScheduleRegionItem.Value
                If ScheduleRegionItemItem.GameDate < Date.UtcNow.AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours) Then Continue For
                If ScheduleRegionItemItem.GameDate >= MaxDateShowAvailableSpots Then Continue For
                If ScheduleRegionItemItem.GameStatus <> "" AndAlso ScheduleRegionItemItem.GameStatus <> "normal" Then Continue For

                Dim IsAvailableUmpire As Boolean = False
                Dim IsAvailableScorekeeper As Boolean = False
                Dim IsAvailableSupervisor As Boolean = False

                For Each SchedulePosition In ScheduleRegionItemItem.SchedulePositions
                    If Positions.Contains(SchedulePosition.PositionId.ToLower) Then
                        If ScheduleRegionItemItem.CrewType.ContainsKey("umpire") AndAlso CrewList(ScheduleRegionItemItem.CrewType("umpire")).Contains(SchedulePosition.PositionId) Then
                            If SchedulePosition.OfficialId = "" Then
                                IsAvailableUmpire = True
                                Exit For
                            End If
                        End If
                    End If
                    If PositionsScorekeeper.Contains(SchedulePosition.PositionId.ToLower) Then
                        If ScheduleRegionItemItem.CrewType.ContainsKey("scorekeeper") AndAlso CrewListScorekeeper(ScheduleRegionItemItem.CrewType("scorekeeper")).Contains(SchedulePosition.PositionId) Then
                            If SchedulePosition.OfficialId = "" Then
                                IsAvailableScorekeeper = True
                                Exit For
                            End If
                        End If
                    End If
                    If PositionsSupervisor.Contains(SchedulePosition.PositionId.ToLower) Then
                        If ScheduleRegionItemItem.CrewType.ContainsKey("supervisor") AndAlso CrewListScorekeeper(ScheduleRegionItemItem.CrewType("supervisor")).Contains(SchedulePosition.PositionId) Then
                            If SchedulePosition.OfficialId = "" Then
                                IsAvailableSupervisor = True
                                Exit For
                            End If
                        End If
                    End If
                Next

                ScheduleRegionAvailable(ScheduleRegionItem.Key).Add(New Tuple(Of Schedule, Boolean, Boolean, Boolean)(ScheduleRegionItemItem, IsAvailableUmpire, IsAvailableScorekeeper, IsAvailableSupervisor))
            Next
        Next

        For Each UniqueRegionUser In UniqueRegionUsers
            Dim User = Users.Find(Function(U) U.Username = UniqueRegionUser(0).RealUsername)
            If User Is Nothing Then Continue For
            If Not User.EmailAvailableGames Then Continue For

            Dim UsersRegions As New List(Of RegionProperties)
            For Each UniqueRegionUserItem In UniqueRegionUser
                If UniqueRegionUserItem Is Nothing Then Continue For
                Dim FoundUsersRegion As Boolean = False
                For Each UsersRegion In UsersRegions
                    If UsersRegion Is Nothing Then Continue For
                    If UsersRegion.RegionId = UniqueRegionUserItem.RegionId Then
                        FoundUsersRegion = True
                    End If
                Next
                If Not FoundUsersRegion Then
                    UsersRegions.Add(Regions.Find(Function(R) R.RegionId = UniqueRegionUserItem.RegionId))
                End If
            Next

            Dim UsersScheduleRegions As New Dictionary(Of String, List(Of Schedule))
            For Each UsersRegion In UsersRegions
                If UsersRegion Is Nothing Then Continue For
                UsersScheduleRegions.Add(UsersRegion.RegionId, ScheduleRegion(UsersRegion.RegionId))
            Next

            Dim GamesAreAvailable As New List(Of Tuple(Of Schedule, Boolean, RegionProperties))

            For Each UsersRegion In UsersRegions
                If UsersRegion Is Nothing Then Continue For
                Dim IsUmpire As Boolean = False
                Dim IsScorekeeper As Boolean = False
                Dim IsSupervisor As Boolean = False

                For Each UniqueRegionUserItem In UniqueRegionUser
                    If UniqueRegionUserItem.RegionId = UsersRegion.RegionId Then
                        If UniqueRegionUserItem.Positions.Contains("official") Then IsUmpire = True
                        If UniqueRegionUserItem.Positions.Contains("scorekeeper") Then IsScorekeeper = True
                        If UniqueRegionUserItem.Positions.Contains("supervisor") Then IsSupervisor = True
                    End If
                Next

                Dim MyRegion = Regions.Find(Function(R) R.RegionId = UsersRegion.RegionId)
                Dim MyRegionUser = UniqueRegionUser.Find(Function(RU) RU.RegionId = UsersRegion.RegionId)

                For Each ScheduleItem In ScheduleRegionAvailable(UsersRegion.RegionId)
                    If (ScheduleItem.Item2 AndAlso IsUmpire) OrElse (ScheduleItem.Item3 AndAlso IsScorekeeper) OrElse (ScheduleItem.Item4 AndAlso IsSupervisor) Then
                        Dim TAvailability = New Dictionary(Of DateTime, Integer)

                        If Availability.ContainsKey(UsersRegion.RegionId) AndAlso Availability(UsersRegion.RegionId).ContainsKey(MyRegionUser.Username) Then
                            TAvailability = Availability(UsersRegion.RegionId)(MyRegionUser.Username)
                        End If

                        Dim League = PublicCode.BinarySearchItem(RegionLeagues, New RegionLeaguePayContracted(MyRegion.RegionId, ScheduleItem.Item1.LeagueId.ToLower), RegionLeaguePayContracted.BasicSorter)

                        If MyRegionUser.CanRequestLeague(MyRegion, League) Then
                            GamesAreAvailable.Add(New Tuple(Of Schedule, Boolean, RegionProperties)(ScheduleItem.Item1, ScheduleItem.Item1.IsAvailable2(MyRegion, Regions, MyRegionUser, UniqueRegionUser, RegionLeagues, Teams, TAvailability, UsersScheduleRegions), MyRegion))
                        End if
                    End If
                Next
            Next

            GamesAreAvailable.Sort(New GenericIComparer(Of Tuple(Of Schedule, Boolean, RegionProperties))(Function(V1, V2)
                                                                                                              Dim Comp = V1.Item1.GameDate.CompareTo(V2.Item1.GameDate)
                                                                                                              If Comp = 0 Then Comp = V1.Item1.ParkId.ToLower.CompareTo(V2.Item1.ParkId.ToLower)
                                                                                                              If Comp = 0 Then Comp = V1.Item1.RegionId.CompareTo(V2.Item1.RegionId)
                                                                                                              If Comp = 0 Then Comp = V1.Item1.ScheduleId.CompareTo(V2.Item1.ScheduleId)
                                                                                                              Return Comp
                                                                                                          End Function))

            If GamesAreAvailable.Count = 0 Then Continue For

            Dim GameEmail As New GameEmail With {
                .User = User,
                .IsAvailableSpots = True,
                .ScheduleData = New List(Of ScheduleDataEmail),
                .RegionProperties = Regions,
                .RegionUsers = RegionUsers,
                .RegionParks = Parks,
                .RegionLeagues = RegionLeagues,
                .Teams = Teams
            }

            For Each GIA In GamesAreAvailable
                Dim RegionUser = UniqueRegionUser.Find(Function(RU) RU.RegionId = GIA.Item3.RegionId)
                GameEmail.ScheduleData.Add(New ScheduleDataEmail(RegionUser, GIA.Item1, GIA.Item1, "", GIA.Item3, GIA.Item2))
            Next

            GameEmail.Send(Nothing)
        Next

    End Sub

    Public Function GetArriveBeforeTimeMins(Regions As List(Of RegionProperties), Leagues As List(Of RegionLeaguePayContracted), Teams As List(Of Team)) As Integer
        Dim MyRegion As RegionProperties = PublicCode.BinarySearchItem(Regions, New RegionProperties With {.RegionId = RegionId}, RegionProperties.BasicSorter)
        Dim MyLeague As RegionLeaguePayContracted = PublicCode.BinarySearchItem(Leagues, New RegionLeaguePayContracted With {.RegionId = RegionId, .LeagueId = LeagueId.ToLower.Trim}, RegionLeaguePayContracted.BasicSorter)

        If LinkedRegionId = "" Then
            If MyRegion.EntityType = "team" Then
                If GameType = "practice" Then
                    If MyLeague IsNot Nothing Then
                        Return MyLeague.ArriveBeforePracticeMins
                    Else
                        Return MyRegion.DefaultArriveBeforePracticeMins
                    End If
                Else
                    If MyLeague IsNot Nothing Then
                        Return MyLeague.ArriveBeforeMins
                    Else
                        Return MyRegion.DefaultArriveBeforeMins
                    End If
                End If
            Else
                If MyLeague IsNot Nothing Then
                    Return MyLeague.ArriveBeforeMins
                Else
                    Return MyRegion.DefaultArriveBeforeMins
                End If
            End If
        Else
            If MyRegion.EntityType = "team" Then

                If GameType = "practice" Then
                    If MyLeague IsNot Nothing Then
                        Return MyLeague.ArriveBeforePracticeMins
                    Else
                        Return MyRegion.DefaultArriveBeforePracticeMins
                    End If
                Else
                    If MyLeague Is Nothing OrElse MyLeague.RealLeagueId = "" Then
                        If MyLeague IsNot Nothing Then
                            Return MyLeague.ArriveBeforeMins
                        Else
                            Return MyRegion.DefaultArriveBeforeMins
                        End If
                    Else
                        Dim HT As Team = PublicCode.BinarySearchItem(Teams, New Team With {.RegionId = MyLeague.RealLeagueId, .TeamId = HomeTeam.Trim.ToLower}, Team.BasicSorter)
                        Dim AT As Team = PublicCode.BinarySearchItem(Teams, New Team With {.RegionId = MyLeague.RealLeagueId, .TeamId = AwayTeam.Trim.ToLower}, Team.BasicSorter)

                        If HT IsNot Nothing Then
                            Return MyLeague.ArriveBeforeMins
                        ElseIf AT IsNot Nothing Then
                            Return MyLeague.ArriveBeforeAwayMins
                        Else
                            Return MyLeague.ArriveBeforeMins
                        End If
                    End If
                End If
            Else
                If MyLeague IsNot Nothing Then
                    Return MyLeague.ArriveBeforeMins
                Else
                    Return MyRegion.DefaultArriveBeforeMins
                End If
            End If
        End If
    End Function

    Public Function GetMaxGameLengthMins(Regions As List(Of RegionProperties), Leagues As List(Of RegionLeaguePayContracted), Teams As List(Of Team)) As Integer
        Dim MyRegion = PublicCode.BinarySearchItem(Regions, New RegionProperties With {.RegionId = RegionId}, RegionProperties.BasicSorter)
        Dim MyLeague = PublicCode.BinarySearchItem(Leagues, New RegionLeaguePayContracted With {.RegionId = RegionId, .LeagueId = LeagueId.ToLower.Trim}, RegionLeaguePayContracted.BasicSorter)


        Dim LinkedRegion As RegionProperties = Nothing
        If LinkedRegionId <> "" Then
            LinkedRegion = PublicCode.BinarySearchItem(Regions, New RegionProperties With {.RegionId = LinkedRegionId}, RegionProperties.BasicSorter)
        End If


        If MyRegion.EntityType = "team" Then
            If GameType = "practice" Then
                If LinkedRegion IsNot Nothing Then
                    Return LinkedRegion.DefaultMaxGameLengthPracticeMins
                ElseIf MyLeague IsNot Nothing Then
                    Return MyLeague.MaxGameLengthPracticeMins
                Else
                    Return MyRegion.DefaultMaxGameLengthPracticeMins
                End If
            Else
                If LinkedRegion IsNot Nothing Then
                    Return LinkedRegion.DefaultMaxGameLengthMins
                ElseIf MyLeague IsNot Nothing Then
                    Return MyLeague.MaxGameLengthMins
                Else
                    Return MyRegion.DefaultMaxGameLengthMins
                End If
            End If
        Else
            If LinkedRegion IsNot Nothing Then
                Return LinkedRegion.DefaultMaxGameLengthMins
            ElseIf MyLeague IsNot Nothing Then
                Return MyLeague.MaxGameLengthMins
            Else
                Return MyRegion.DefaultMaxGameLengthMins
            End If
        End If
    End Function

    Public Function IsAvailableOnly(Regions As List(Of RegionProperties), RegionLeagues As List(Of RegionLeaguePayContracted), Teams As List(Of Team), Availability As Dictionary(Of Date, Integer)) As Boolean
        If Availability Is Nothing Then Return False

        Dim ArriveBeforeMins = GetArriveBeforeTimeMins(Regions, RegionLeagues, Teams)
        Dim MaxGameLengthMins = GetMaxGameLengthMins(Regions, RegionLeagues, Teams)

        Dim LowerGameDateFull = GameDate.AddMinutes(-ArriveBeforeMins)
        Dim UpperDateFull = GameDate.AddMinutes(MaxGameLengthMins)

        Dim BeforeDate = GameDate.AddMinutes(-ArriveBeforeMins)
        BeforeDate = BeforeDate.AddMinutes(-BeforeDate.Minute).AddSeconds(-BeforeDate.Second).AddMilliseconds(-BeforeDate.Millisecond)

        Dim AfterDate = GameDate.AddMinutes(MaxGameLengthMins)
        AfterDate = AfterDate.AddMinutes((60 - AfterDate.Minute) Mod 60).AddSeconds(-AfterDate.Second).AddMilliseconds(-AfterDate.Millisecond)

        Dim CurrentRegion = Regions.Find(Function (R) R.RegionId = RegionId)

        If CurrentRegion Is Nothing Then Return False

        Dim TDate = BeforeDate.AddDays(0)
        While TDate < AfterDate
            If Availability.ContainsKey(TDate) Then
                If CurrentRegion.EnableDualAvailability Then
                    Dim GameTypeL = GameType.ToLower.Trim()
                    If GameType = "game" OrElse GameType =  "playoff" OrElse GameType = "playoffs" then
                        If Availability(TDate) <> 1  AndAlso Availability(TDate) <> 5 Then Return False
                    ElseIf GameType = "practice" OrElse GameType = "exhibition" Then
                        If Availability(TDate) <> 3 AndAlso Availability(TDate) <> 5 Then Return False
                    Else
                        If Availability(TDate) <> 1 AndAlso Availability(TDate) <> 3 AndAlso Availability(TDate) <> 5 Then Return False
                    End If
                Else
                    If Availability(TDate) <> 1 Then Return False
                End If
            Else
                Return False
            End If

            TDate = TDate.AddHours(1)
        End While

        Return True
    End Function

    Public Function IsAvailable2(MyRegion As RegionProperties, Regions As List(Of RegionProperties), MyRegionUser As RegionUser, RegionUsers As List(Of RegionUser), RegionLeagues As List(Of RegionLeaguePayContracted), Teams As List(Of Team), Availability As Dictionary(Of Date, Integer), Schedule As Dictionary(Of String, List(Of Schedule))) As Boolean
        If SchedulePositions.Find(Function(SP) SP.OfficialId.ToLower = MyRegionUser.Username) IsNot Nothing Then Return False

        If Not IsAvailableOnly(Regions, RegionLeagues, Teams, Availability) Then Return False

        Dim ArriveBeforeMins = GetArriveBeforeTimeMins(Regions, RegionLeagues, Teams)
        Dim MaxGameLengthMins = GetMaxGameLengthMins(Regions, RegionLeagues, Teams)

        Dim LowerGameDateFull = GameDate.AddMinutes(-ArriveBeforeMins)
        Dim UpperDateFull = GameDate.AddMinutes(MaxGameLengthMins)

        Dim RegionTimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(MyRegion.TimeZone)

        For Each ScheduleRegion In Schedule
            Dim CurrentRegion = Regions.Find(Function(R) R.RegionId = ScheduleRegion.Key)
            Dim CurrentRegionUser = RegionUsers.Find(Function(RU) RU.RegionId = ScheduleRegion.Key)
            Dim CurrentTimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(CurrentRegion.TimeZone)

            Dim LowerIndex = ScheduleRegion.Value.BinarySearch(New Schedule With {.GameDate = GameDate.AddDays(-1), .ParkId = "", .RegionId = "", .ScheduleId = -1}, UmpireAssignor.Schedule.DateSorter)
            Dim UpperIndex = ScheduleRegion.Value.BinarySearch(New Schedule With {.GameDate = GameDate.AddDays(1), .ParkId = "ZZZZZZZZZZZZZZZZZ", .RegionId = "ZZZZZZZZZZZZZ", .ScheduleId = Integer.MaxValue}, UmpireAssignor.Schedule.DateSorter)

            If LowerIndex < 0 Then LowerIndex = -LowerIndex - 1
            If UpperIndex < 0 Then UpperIndex = -UpperIndex
            If UpperIndex >= ScheduleRegion.Value.Count Then UpperIndex = ScheduleRegion.Value.Count - 1

            For I As Integer = LowerIndex To UpperIndex
                Dim ScheduleItem = ScheduleRegion.Value(I)

                Dim ScheduleItemGameDate = ScheduleItem.GameDate
                If MyRegion.TimeZone <> CurrentRegion.TimeZone Then
                    ScheduleItemGameDate = ScheduleItemGameDate.AddHours((CurrentTimeZone.GetUtcOffset(Date.UtcNow).TotalHours - RegionTimeZone.GetUtcOffset(Date.UtcNow).TotalHours))
                End If

                If (Math.Abs((ScheduleItemGameDate - GameDate).TotalHours) < 24 AndAlso ParkId <> ScheduleItem.ParkId AndAlso (ScheduleItem.GameStatus.ToLower = "normal" OrElse ScheduleItem.GameStatus.ToLower = "")) AndAlso Not (ScheduleItem.RegionId = RegionId AndAlso ScheduleItem.ScheduleId = ScheduleId) Then
                    If ScheduleItem.IsUserOnGame(CurrentRegionUser.Username, Regions, RegionUsers) Then
                        Dim GD = ScheduleItemGameDate

                        Dim ArriveBeforeMinsOther = ScheduleItem.GetArriveBeforeTimeMins(Regions, RegionLeagues, Teams)
                        Dim MaxGameLengthMinsOther = ScheduleItem.GetMaxGameLengthMins(Regions, RegionLeagues, Teams)

                        Dim OtherLowerGameDateFull = ScheduleItemGameDate.AddMinutes(-ArriveBeforeMinsOther)
                        Dim OtherUpperDateFull = ScheduleItemGameDate.AddMinutes(MaxGameLengthMinsOther)

                        If PublicCode.IsInRange(LowerGameDateFull, UpperDateFull, OtherLowerGameDateFull, OtherUpperDateFull) Then Return False
                    End If
                End If
            Next
        Next
        Return True
    End Function

    Public Function GetIntersectingGames(MyRegion As RegionProperties, Regions As List(Of RegionProperties), MyRegionUser As RegionUser, RegionUsers As List(Of RegionUser), RegionLeagues As List(Of RegionLeaguePayContracted), Teams As List(Of Team), Schedule As List(Of Schedule)) As List(Of Schedule)
        Dim ArriveBeforeMins = GetArriveBeforeTimeMins(Regions, RegionLeagues, Teams)
        Dim MaxGameLengthMins = GetMaxGameLengthMins(Regions, RegionLeagues, Teams)

        Dim LowerGameDateFull = GameDate.AddMinutes(-ArriveBeforeMins)
        Dim UpperDateFull = GameDate.AddMinutes(MaxGameLengthMins)

        Dim RegionTimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(MyRegion.TimeZone)

        Dim LowerIndex = Schedule.BinarySearch(New Schedule With {.GameDate = GameDate.AddDays(-1), .ParkId = "", .RegionId = "", .ScheduleId = -1}, UmpireAssignor.Schedule.DateSorter)
        Dim UpperIndex = Schedule.BinarySearch(New Schedule With {.GameDate = GameDate.AddDays(1), .ParkId = "ZZZZZZZZZZZZZZZZZ", .RegionId = "ZZZZZZZZZZZZZ", .ScheduleId = Integer.MaxValue}, UmpireAssignor.Schedule.DateSorter)

        If LowerIndex < 0 Then LowerIndex = -LowerIndex - 1
        If UpperIndex < 0 Then UpperIndex = -UpperIndex
        If UpperIndex >= Schedule.Count Then UpperIndex = Schedule.Count - 1

        Dim Result As New List(Of Schedule)

        Dim ParkTrim = ParkId.Trim.ToLower

        For I As Integer = LowerIndex To UpperIndex
            Dim ScheduleItem = Schedule(I)
            Dim RegionId As String = ScheduleItem.RegionId

            Dim CurrentRegion = RegionProperties.GetItem(Regions, RegionId)
            Dim CurrentRegionUser = UmpireAssignor.RegionUser.GetItem(RegionUsers, RegionId)
            Dim CurrentTimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(CurrentRegion.TimeZone)

            Dim ScheduleItemGameDate = ScheduleItem.GameDate
            If MyRegion.TimeZone <> CurrentRegion.TimeZone Then
                ScheduleItemGameDate = ScheduleItemGameDate.AddHours((CurrentTimeZone.GetUtcOffset(Date.UtcNow).TotalHours - RegionTimeZone.GetUtcOffset(Date.UtcNow).TotalHours))
            End If

            If (Math.Abs((ScheduleItemGameDate - GameDate).TotalHours) < 24 AndAlso ParkTrim <> ScheduleItem.ParkId.Trim.ToLower AndAlso (ScheduleItem.GameStatus.ToLower = "normal" OrElse ScheduleItem.GameStatus.ToLower = "")) AndAlso Not (ScheduleItem.RegionId = RegionId AndAlso ScheduleItem.ScheduleId = ScheduleId) Then
                Dim GD = ScheduleItemGameDate

                Dim ArriveBeforeMinsOther = ScheduleItem.GetArriveBeforeTimeMins(Regions, RegionLeagues, Teams)
                Dim MaxGameLengthMinsOther = ScheduleItem.GetMaxGameLengthMins(Regions, RegionLeagues, Teams)

                Dim OtherLowerGameDateFull = ScheduleItemGameDate.AddMinutes(-ArriveBeforeMinsOther)
                Dim OtherUpperDateFull = ScheduleItemGameDate.AddMinutes(MaxGameLengthMinsOther)

                If PublicCode.IsInRange(LowerGameDateFull, UpperDateFull, OtherLowerGameDateFull, OtherUpperDateFull) Then
                    Result.Add(ScheduleItem)
                End If
            End If
        Next

        Return Result
    End Function

    Public Shared Function GetMaxPositionListFromSchedule(Region As Region, Schedule As List(Of Schedule)) As List(Of String)
        Dim Result As New List(Of String)

        Dim TotalCrewList = RegionLeaguePayContracted.GetSportCrewPositions()
        Dim CrewList As Dictionary(Of String, List(Of String))

        If TotalCrewList.ContainsKey(Region.Sport.ToLower) Then
            CrewList = TotalCrewList(Region.Sport)
        Else
            Return Result
        End If

        For Each ScheduleItem In Schedule
            Dim PositionList As List(Of String) = Nothing

            If ScheduleItem.CrewType.ContainsKey("umpire") Then
                If CrewList.ContainsKey(ScheduleItem.CrewType("umpire").ToLower) Then
                    PositionList = CrewList(ScheduleItem.CrewType("umpire").ToLower)
                Else
                    Continue For
                End If

                For Each Position In PositionList
                    If Not Result.Contains(Position.ToLower) Then Result.Add(Position.ToLower)
                Next
            End If
        Next

        Dim PositionOrdering = RegionLeaguePayContracted.GetSportPositionOrder()(Region.Sport)
        Result.Sort(New GenericIComparer(Of String)(Function(V1, V2)
                                                        Dim Index1 = PositionOrdering.IndexOf(V1)
                                                        Dim Index2 = PositionOrdering.IndexOf(V2)
                                                        Return Index1.CompareTo(Index2)
                                                    End Function))


        Return Result
    End Function

    Public Shared Function GetMaxPositionListFromScheduleScorekeepers(Region As Region, Schedule As List(Of Schedule)) As List(Of String)
        Dim Result As New List(Of String)

        Dim TotalCrewList = RegionLeaguePayContracted.GetSportCrewPositionsScorekeepers()
        Dim CrewList As Dictionary(Of String, List(Of String))

        If TotalCrewList.ContainsKey(Region.Sport.ToLower) Then
            CrewList = TotalCrewList(Region.Sport)
        Else
            Return Result
        End If

        For Each ScheduleItem In Schedule
            Dim PositionList As List(Of String) = Nothing

            If ScheduleItem.CrewType.ContainsKey("scorekeeper") Then
                If CrewList.ContainsKey(ScheduleItem.CrewType("scorekeeper").ToLower) Then
                    PositionList = CrewList(ScheduleItem.CrewType("scorekeeper").ToLower)
                Else
                    Continue For
                End If

                For Each Position In PositionList
                    If Not Result.Contains(Position.ToLower) Then Result.Add(Position.ToLower)
                Next
            End If
        Next

        Dim PositionOrdering = RegionLeaguePayContracted.GetSportPositionOrderScorekeeper()(Region.Sport)
        Result.Sort(New GenericIComparer(Of String)(Function(V1, V2)
                                                        Dim Index1 = PositionOrdering.IndexOf(V1)
                                                        Dim Index2 = PositionOrdering.IndexOf(V2)
                                                        Return Index1.CompareTo(Index2)
                                                    End Function))


        Return Result
    End Function

        Public Shared Function GetMaxPositionListFromScheduleSupervisors(Region As Region, Schedule As List(Of Schedule)) As List(Of String)
        Dim Result As New List(Of String)

        Dim TotalCrewList = RegionLeaguePayContracted.GetSportCrewPositionsSupervisors()
        Dim CrewList As Dictionary(Of String, List(Of String))

        If TotalCrewList.ContainsKey(Region.Sport.ToLower) Then
            CrewList = TotalCrewList(Region.Sport)
        Else
            Return Result
        End If

        For Each ScheduleItem In Schedule
            Dim PositionList As List(Of String) = Nothing

            If ScheduleItem.CrewType.ContainsKey("supervisor") Then
                If CrewList.ContainsKey(ScheduleItem.CrewType("supervisor").ToLower) Then
                    PositionList = CrewList(ScheduleItem.CrewType("supervisor").ToLower)
                Else
                    Continue For
                End If

                For Each Position In PositionList
                    If Not Result.Contains(Position.ToLower) Then Result.Add(Position.ToLower)
                Next
            End If
        Next

        Dim PositionOrdering = RegionLeaguePayContracted.GetSportPositionOrderSupervisor()(Region.Sport)
        Result.Sort(New GenericIComparer(Of String)(Function(V1, V2)
                                                        Dim Index1 = PositionOrdering.IndexOf(V1)
                                                        Dim Index2 = PositionOrdering.IndexOf(V2)
                                                        Return Index1.CompareTo(Index2)
                                                    End Function))


        Return Result
    End Function


    Public Function IsDifferent(Schedule As ScheduleTemp) As Boolean
        If Schedule Is Nothing Then Return True
        If Schedule.IsDeleted Then Return True

        If Schedule.GameNumberModified AndAlso Schedule.GameNumber <> GameNumber Then Return True
        If Schedule.GameTypeModified AndAlso Schedule.GameType <> GameType Then Return True
        If Schedule.LeagueIdModified AndAlso Schedule.LeagueId <> LeagueId Then Return True
        If Schedule.HomeTeamModified AndAlso Schedule.HomeTeam <> HomeTeam Then Return True
        If Schedule.HomeTeamScoreModified AndAlso Schedule.HomeTeamScore <> HomeTeamScore Then Return True
        If Schedule.AwayTeamModified AndAlso Schedule.AwayTeam <> AwayTeam Then Return True
        If Schedule.AwayTeamScoreModified AndAlso Schedule.AwayTeamScore <> AwayTeamScore Then Return True


        If Schedule.HomeTeamScoreExtraModified Then
            For Each HomeTeamScoreExtraItem In HomeTeamScoreExtra
                If Schedule.HomeTeamScoreExtra.ContainsKey(HomeTeamScoreExtraItem.Key) Then
                    If Schedule.HomeTeamScoreExtra(HomeTeamScoreExtraItem.Key) <> HomeTeamScoreExtraItem.Value Then Return True
                End If
            Next
            For Each HomeTeamScoreExtraItem In Schedule.HomeTeamScoreExtra
                If HomeTeamScoreExtra.ContainsKey(HomeTeamScoreExtraItem.Key) Then
                    If HomeTeamScoreExtra(HomeTeamScoreExtraItem.Key) <> HomeTeamScoreExtraItem.Value Then Return True
                End If
            Next
        End If

        If Schedule.AwayTeamScoreExtraModified Then
            For Each AwayTeamScoreExtraItem In AwayTeamScoreExtra
                If Schedule.AwayTeamScoreExtra.ContainsKey(AwayTeamScoreExtraItem.Key) Then
                    If Schedule.AwayTeamScoreExtra(AwayTeamScoreExtraItem.Key) <> AwayTeamScoreExtraItem.Value Then Return True
                End If
            Next
            For Each AwayTeamScoreExtraItem In Schedule.AwayTeamScoreExtra
                If AwayTeamScoreExtra.ContainsKey(AwayTeamScoreExtraItem.Key) Then
                    If AwayTeamScoreExtra(AwayTeamScoreExtraItem.Key) <> AwayTeamScoreExtraItem.Value Then Return True
                End If
            Next
        End If

        If Schedule.GameDateModified AndAlso Schedule.GameDate <> GameDate Then Return True
        If Schedule.ParkIdModified AndAlso Schedule.ParkId <> ParkId Then Return True

        If Schedule.CrewTypeModified Then
            If Schedule.CrewType.Count <> CrewType.Count Then Return True
            For Each KeyValue In CrewType
                If Not Schedule.CrewType.ContainsKey(KeyValue.Key) Then Return True
                If Not Schedule.CrewType(KeyValue.Key).ToLower = KeyValue.Value.ToLower Then Return True
            Next
        End If

        If Schedule.GameStatusModified AndAlso Schedule.GameStatus <> GameStatus Then Return True
        If Schedule.GameCommentModified AndAlso Schedule.GameComment <> GameComment Then Return True
        If Schedule.GameScoreModified AndAlso GameScore.IsDifferentScoreOnly(Schedule.GameScore, GameScore) Then Return True
        If Schedule.OfficialRegionIdModified AndAlso Schedule.OfficialRegionId <> OfficialRegionId Then Return True
        If Schedule.ScorekeeperRegionIdModified AndAlso Schedule.ScorekeeperRegionId <> ScorekeeperRegionId Then Return True
        If Schedule.SupervisorRegionIdModified AndAlso Schedule.SupervisorRegionId <> SupervisorRegionId Then Return True

        If Schedule.IsDeleted <> IsDeleted Then Return True


        For Each SchedulePosition In SchedulePositions
            Dim ScheduleSchedulePosition = Schedule.SchedulePositions.Find(Function(SP) SP.PositionId = SchedulePosition.PositionId)
            If ScheduleSchedulePosition IsNot Nothing Then
                If SchedulePosition.OfficialId.ToLower <> ScheduleSchedulePosition.OfficialId.ToLower Then Return True
            End If
        Next

        For Each SchedulePosition In Schedule.SchedulePositions
            Dim ScheduleSchedulePosition = SchedulePositions.Find(Function(SP) SP.PositionId = SchedulePosition.PositionId)
            If ScheduleSchedulePosition Is Nothing Then
                Return True
            End If
        Next

        For Each ScheduleFine In ScheduleFines
            Dim ScheduleScheduleFine = Schedule.ScheduleFines.Find(Function(SF) SF.OfficialId = ScheduleFine.OfficialId)
            If ScheduleScheduleFine IsNot Nothing Then
                If ScheduleFine.Amount <> ScheduleScheduleFine.Amount Then Return True
                If ScheduleFine.Comment <> ScheduleScheduleFine.Comment Then Return True
            End If
        Next

        For Each ScheduleFine In Schedule.ScheduleFines
            If ScheduleFines.Find(Function(SF) SF.OfficialId = ScheduleFine.OfficialId AndAlso ScheduleFine.IsDeleted = False) Is Nothing Then Return True
        Next

        For Each ScheduleUserComment In ScheduleUserComments
            Dim ScheduleScheduleUserComment = Schedule.ScheduleUserComments.Find(Function(SF) SF.OfficialId = ScheduleUserComment.OfficialId)
            If ScheduleScheduleUserComment IsNot Nothing Then
                If ScheduleUserComment.Comment <> ScheduleScheduleUserComment.Comment Then Return True
                If ScheduleScheduleUserComment.IsDeleted Then Return True
            End If
        Next

        For Each ScheduleUserComment In Schedule.ScheduleUserComments
            If ScheduleUserComments.Find(Function(SF) SF.OfficialId = ScheduleUserComment.OfficialId) Is Nothing Then Return True
        Next

        For Each ScheduleUserCommentTeam In ScheduleUserCommentTeams
            Dim ScheduleScheduleUserCommentTeam = Schedule.ScheduleUserCommentTeams.Find(Function(SF) SF.OfficialId = ScheduleUserCommentTeam.OfficialId AndAlso SF.LinkedRegionId = ScheduleUserCommentTeam.LinkedRegionId)
            If ScheduleScheduleUserCommentTeam IsNot Nothing Then
                If ScheduleUserCommentTeam.Comment <> ScheduleScheduleUserCommentTeam.Comment Then Return True
            End If
        Next

        For Each ScheduleUserCommentTeam In Schedule.ScheduleUserCommentTeams
            If ScheduleUserCommentTeams.Find(Function(SF) SF.OfficialId = ScheduleUserCommentTeam.OfficialId AndAlso SF.LinkedRegionId = ScheduleUserCommentTeam.LinkedRegionId) Is Nothing Then Return True
        Next

        For Each ScheduleCommentTeam In ScheduleCommentTeams
            Dim ScheduleScheduleCommentTeam = Schedule.ScheduleCommentTeams.Find(Function(SF) SF.LinkedRegionId = ScheduleCommentTeam.LinkedRegionId)
            If ScheduleScheduleCommentTeam IsNot Nothing Then
                If ScheduleCommentTeam.Comment <> ScheduleScheduleCommentTeam.Comment Then Return True
            End If
        Next

        For Each ScheduleCommentTeam In Schedule.ScheduleCommentTeams
            If ScheduleCommentTeams.Find(Function(SF) SF.LinkedRegionId = ScheduleCommentTeam.LinkedRegionId) Is Nothing Then Return True
        Next

        For Each ScheduleCallup In Schedule.ScheduleCallUps
            Dim ScheduleScheduleCallup = ScheduleCallUps.Find(Function(SC) SC.LinkedRegionId = ScheduleCallup.LinkedRegionId AndAlso SC.Username = ScheduleCallup.Username)
            If ScheduleCallup.IsDeleted Then
                If ScheduleScheduleCallup IsNot Nothing Then Return True
            Else
                If ScheduleScheduleCallup Is Nothing Then Return True
            End If
        Next

        Return False
    End Function

    Public Shared Function CheckAllRegionsIsSubmitted(RegionIds As List(Of String), AssignorId As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As Boolean
        Dim Result As Boolean = False

        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim CommandText As String = "SELECT IsSubmitted FROM ScheduleTempSubmitted WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId".Replace("{0}", RegionIdParams.ToString())
        Using SqlCommand2 As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand2.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next
            SqlCommand2.Parameters.Add(New SqlParameter("AssignorId", AssignorId))

            Dim Reader = SqlCommand2.ExecuteReader
            While Reader.Read
                If Reader.GetBoolean(0) Then Result = True
            End While
            Reader.Close()
        End Using

        Return Result
    End Function

    Public Shared Function SaveTemp(AssignorId As String, RegionId As String, Optional ByPassPermissions As Boolean = False) As Object
        Dim GameEmails As New List(Of GameEmail)
        Dim AssignorEmails As New List(Of GameEmail)
        Dim Region As RegionProperties = Nothing

        Dim DateAdded As Date = Date.UtcNow
        Dim RegionUsers As New List(Of RegionUser)

        Dim InsertScheduleCallups As New List(Of ScheduleCallupFull)
        Dim GamesThatScoresNeedToBeUpdated As new List(Of Schedule)

        Dim FullSchedule As New List(Of Schedule)
        Dim OldFullSchedule As new List(Of Schedule)

        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Dim UniqueRegionIds As New List(Of String)

                If RegionId = "allregions" OrElse RegionId = "alleditableregions" Then
                    UniqueRegionIds = RegionUser.GetAllMyRegionsIdExecs(AssignorId, SqlConnection, SqlTransaction)
                Else
                    UniqueRegionIds.Add(RegionId)
                    Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                    If ByPassPermissions AndAlso (Assignor Is Nothing OrElse Not Assignor.IsExecutive) Then
                        Return New ErrorObject("InvalidPermissions")
                    End If
                End If

                Dim IsSubmitted As Boolean = CheckAllRegionsIsSubmitted(UniqueRegionIds, AssignorId, SqlConnection, SqlTransaction)

                If Not IsSubmitted Then Return New ErrorObject("NoChanges")

                Dim ScheduleTemps = GetMasterScheduleFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, SqlConnection, SqlTransaction)
                SchedulePosition.GetMasterSchedulePositionFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, ScheduleTemps, SqlConnection, SqlTransaction)
                ScheduleFine.GetMasterScheduleFineFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, ScheduleTemps, SqlConnection, SqlTransaction)
                ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, ScheduleTemps, SqlConnection, SqlTransaction)
                ScheduleUserCommentTeam.GetMasterScheduleUserCommentTeamFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, ScheduleTemps, SqlConnection, SqlTransaction)
                ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, ScheduleTemps, SqlConnection, SqlTransaction)
                ScheduleCallup.GetMasterScheduleCallUpFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, ScheduleTemps, SqlConnection, SqlTransaction)

                Dim Regions = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)

                Dim UniqueLadderLeagueRegionIds As New List(Of String)
                For Each UniqueRegionId In UniqueRegionIds
                    Dim UniqueRegion = RegionProperties.GetItem(Regions, UniqueRegionId)
                    If UniqueRegion.RegionIsLadderLeague Then
                            UniqueLadderLeagueRegionIds.Add(UniqueRegionId)
                    End If
                Next

                If UniqueLadderLeagueRegionIds.Count > 0 Then
                    OldFullSchedule = GetMasterScheduleFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, SqlConnection, SqlTransaction)
                    SchedulePosition.GetMasterSchedulePositionFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, OldFullSchedule, SqlConnection, SqlTransaction)
                    ScheduleFine.GetMasterScheduleFineFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds,Date.MinValue, Date.MaxValue, OldFullSchedule, SqlConnection, SqlTransaction)
                    ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, OldFullSchedule, SqlConnection, SqlTransaction)
                    ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue,  OldFullSchedule, SqlConnection, SqlTransaction)
                End if

                Dim RegionLeagues = RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(RegionLeague.GetRegionsLeaguesHelper(UniqueRegionIds, SqlConnection, SqlTransaction), New List(Of RegionLeaguePay))

                Dim UniqueRegionIdsWithTeamsLeagues As New List(Of String)
                UniqueRegionIdsWithTeamsLeagues.AddRange(UniqueRegionIds)
                For Each RegionLeague In RegionLeagues
                    If RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked AndAlso Not UniqueRegionIdsWithTeamsLeagues.Contains(RegionLeague.RealLeagueId) AndAlso Regions.Any(Function(R) R.RegionId = RegionLeague.RegionId AndAlso R.EntityType = "team") Then UniqueRegionIdsWithTeamsLeagues.Add(RegionLeague.RealLeagueId)
                Next

                Dim UniqueRegionIdsWithLeagues As New List(Of String)
                Dim NotYetFetchedRegionIds As New List(Of String)
                UniqueRegionIdsWithLeagues.AddRange(UniqueRegionIds)
                For Each RegionLeague In RegionLeagues
                    If RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked AndAlso Not UniqueRegionIdsWithLeagues.Contains(RegionLeague.RealLeagueId) Then
                        UniqueRegionIdsWithLeagues.Add(RegionLeague.RealLeagueId)
                        NotYetFetchedRegionIds.Add(RegionLeague.RealLeagueId)
                    End If
                Next

                Regions.AddRange(UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIdsWithLeagues, SqlConnection, SqlTransaction))
                Regions.Sort(UmpireAssignor.RegionProperties.BasicSorter)

                Dim Teams = Team.GetTeamsInRegionsHelper(UniqueRegionIdsWithLeagues, SqlConnection, SqlTransaction)
                Dim OfficialRegions = OfficialRegion.GetOfficialRegionsInRegionsHelper(UniqueRegionIdsWithLeagues, SqlConnection, SqlTransaction)
                Dim RegionParks = Park.GetParksInRegionsHelper(UniqueRegionIdsWithLeagues, SqlConnection, SqlTransaction)

                Dim NotYetFetchedLeagues As New List(Of String)
                For Each Team In Teams
                    If Team.RealTeamId <> "" AndAlso Team.IsLinked AndAlso Not UniqueRegionIds.Contains(Team.RealTeamId) AndAlso Not NotYetFetchedLeagues.Contains(Team.RealTeamId) Then
                        NotYetFetchedLeagues.Add(Team.RealTeamId)
                    End If
                Next
                For Each OfficialRegion In OfficialRegions
                    If OfficialRegion.RealOfficialRegionId <> "" AndAlso OfficialRegion.IsLinked AndAlso Not UniqueRegionIds.Contains(OfficialRegion.RealOfficialRegionId) AndAlso Not NotYetFetchedLeagues.Contains(OfficialRegion.RealOfficialRegionId) Then
                        NotYetFetchedLeagues.Add(OfficialRegion.RealOfficialRegionId)
                    End If
                Next

                Dim AllRegionIdsSoFar As New List(Of String)
                AllRegionIdsSoFar.AddRange(UniqueRegionIds)
                AllRegionIdsSoFar.AddRange(NotYetFetchedLeagues)

                RegionUsers = RegionUser.LoadAllInRegionIdsHelper(AllRegionIdsSoFar, "superadmin", SqlConnection, SqlTransaction)

                RegionLeagues.AddRange(RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(RegionLeague.GetRegionsLeaguesHelper(NotYetFetchedLeagues, SqlConnection, SqlTransaction), New List(Of RegionLeaguePay)))
                RegionLeagues.Sort(RegionLeaguePayContracted.BasicSorter)

                Regions.AddRange(UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(NotYetFetchedLeagues, SqlConnection, SqlTransaction))
                Regions.Sort(UmpireAssignor.RegionProperties.BasicSorter)

                Dim TempSchedules = GetMasterScheduleFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, SqlConnection, SqlTransaction)
                SchedulePosition.GetMasterSchedulePositionFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, TempSchedules, SqlConnection, SqlTransaction)
                ScheduleFine.GetMasterScheduleFineFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, TempSchedules, SqlConnection, SqlTransaction)
                ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, TempSchedules, SqlConnection, SqlTransaction)
                ScheduleUserCommentTeam.GetMasterScheduleUserCommentTeamFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, TempSchedules, SqlConnection, SqlTransaction)
                ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, TempSchedules, SqlConnection, SqlTransaction)
                ScheduleCallup.GetMasterScheduleCallUpFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, TempSchedules, SqlConnection, SqlTransaction)

                Dim Schedules As New List(Of Schedule)
                For Each ScheduleItem In TempSchedules
                    If UniqueRegionIds.Contains(ScheduleItem.RegionId) Then
                        Schedules.Add(ScheduleItem)
                    Else
                        Dim TRegionId = ScheduleItem.RegionId
                        Dim RealTeamIds As New List(Of String)
                        For Each Team In Teams
                            If Team.RealTeamId <> "" AndAlso Team.IsLinked AndAlso Team.RegionId = TRegionId AndAlso (Team.TeamId = ScheduleItem.HomeTeam.Trim.ToLower OrElse Team.TeamId = ScheduleItem.AwayTeam.Trim.ToLower) AndAlso Not RealTeamIds.Contains(Team.RealTeamId) AndAlso UniqueRegionIds.Contains(Team.RealTeamId) Then
                                RealTeamIds.Add(Team.RealTeamId)
                            End If
                        Next

                        For Each RealTeamId In RealTeamIds
                            If Teams.Any(Function(T) T.RealTeamId = RealTeamId AndAlso T.IsLinked AndAlso T.RegionId = TRegionId AndAlso (T.TeamId = ScheduleItem.HomeTeam.Trim.ToLower OrElse T.TeamId = ScheduleItem.AwayTeam.Trim.ToLower)) Then
                                Dim NewScheduleItem = ConvertToLinkScheduleItem(ScheduleItem, RealTeamId, RegionLeagues)
                                NewScheduleItem.MergeTeamData(RealTeamId, True)
                                Schedules.Add(ConvertToLinkScheduleItem(ScheduleItem, RealTeamId, RegionLeagues))
                            End If
                        Next
                    End If
                Next

                ScheduleTemps.Sort(ScheduleTemp.BasicSorter)
                Schedules.Sort(Schedule.BasicSorter)

                Dim CurrentScheduleIds As New Dictionary(Of String, ScheduleId)
                For Each TRegionId In UniqueRegionIdsWithTeamsLeagues
                    CurrentScheduleIds.Add(TRegionId, New ScheduleId(TRegionId, SqlConnection, SqlTransaction))
                Next

                Dim DeleteSchedules As New List(Of Schedule)
                Dim InsertSchedules As New List(Of Schedule)
                Dim InsertSchedulePositions As New List(Of SchedulePositionFull)
                Dim InsertScheduleFines As New List(Of ScheduleFineFull)
                Dim InsertScheduleUserComments As New List(Of ScheduleUserCommentFull)
                Dim InsertScheduleUserCommentTeams As New List(Of ScheduleUserCommentTeamFull)
                Dim InsertScheduleCommentTeams As New List(Of ScheduleCommentTeamFull)
                
                PublicCode.ProcessListPairs(ScheduleTemps, Schedules, AddressOf Schedule.TempComparer,
                                                Sub(TRU)
                                                    If Not TRU.IsDeleted Then
                                                        Dim TRegion = Regions.Find(Function(R) R.RegionId = TRU.RegionId)
                                                        If TRU.LinkedRegionId <> "" AndAlso TRegion.EntityType = "team" Then Exit Sub
                                                        UpsertScheduleHelper(Regions, TRU, Nothing, CurrentScheduleIds(TRU.RegionId), RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, GameEmails, AssignorEmails, DateAdded, DeleteSchedules, InsertSchedules, InsertSchedulePositions, InsertScheduleFines, InsertScheduleUserComments, InsertScheduleUserCommentTeams, InsertScheduleCommentTeams, InsertScheduleCallups, GamesThatScoresNeedToBeUpdated)
                                                    End If
                                                End Sub,
                                                Sub(RU)
                                                End Sub,
                                                Sub(TRU, RU)
                                                    If RU.IsDifferent(TRU) Then
                                                        If TRU.IsDeleted Then
                                                            Dim TRegion = Regions.Find(Function(R) R.RegionId = RU.RegionId)
                                                            If TRU.LinkedRegionId <> "" AndAlso TRegion.EntityType = "team" Then Exit Sub
                                                            DeleteScheduleHelper(Regions, RU, RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, GameEmails, AssignorEmails, DateAdded, DeleteSchedules, InsertSchedules, GamesThatScoresNeedToBeUpdated)
                                                        Else
                                                            Dim TRegion = Regions.Find(Function(R) R.RegionId = TRU.RegionId)
                                                            If RU.LinkedRegionId <> "" Then
                                                                TRU.LinkedRegionId = RU.LinkedRegionId
                                                                TRU.LinkedScheduleId = RU.LinkedScheduleId
                                                                If TRegion.EntityType = "team" Then

                                                                    Dim ShowSimpleEdit = RU.CanShowSimpleEdit(AssignorId, Regions, RegionUsers, Teams)

                                                                    RU = PublicCode.BinarySearchItem(TempSchedules, New Schedule With {.RegionId = RU.LinkedRegionId, .ScheduleId = RU.LinkedScheduleId}, Schedule.BasicSorterSimple)
                                                                    If RU Is Nothing Then Exit Sub
                                                                    Dim RU2 = RU.CloneItem()

                                                                    If ShowSimpleEdit.CanEditScore Then
                                                                        RU2.HomeTeamScore = TRU.HomeTeamScore
                                                                        RU2.HomeTeamScoreExtra = TRU.HomeTeamScoreExtra
                                                                        RU2.AwayTeamScore = TRU.AwayTeamScore
                                                                        RU2.AwayTeamScoreExtra = TRU.AwayTeamScoreExtra
                                                                    End If

                                                                    If ShowSimpleEdit.CanEditGroupComment Then
                                                                        RU2.ScheduleCommentTeams.RemoveAll(Function(SC) SC.LinkedRegionId = TRU.RegionId)
                                                                        RU2.ScheduleCommentTeams.AddRange(TRU.ScheduleCommentTeams.FindAll(Function(SC) SC.LinkedRegionId = TRU.RegionId))
                                                                        RU2.ScheduleCommentTeams.Sort(ScheduleCommentTeam.BasicSorter)
                                                                    End If

                                                                    If ShowSimpleEdit.CanEditCallUps Then
                                                                        RU2.ScheduleCallUps.RemoveAll(Function(SC) SC.LinkedRegionId = TRU.RegionId)
                                                                        RU2.ScheduleCallUps.AddRange(TRU.ScheduleCallUps.FindAll(Function(SC) SC.LinkedRegionId = TRU.RegionId))
                                                                        RU2.ScheduleCallUps.Sort(ScheduleCallup.BasicSorter)
                                                                    End If

                                                                    If ShowSimpleEdit.CanEditUserComments Then
                                                                        RU2.ScheduleUserCommentTeams.RemoveAll(Function(SC) SC.LinkedRegionId = TRU.RegionId)
                                                                        RU2.ScheduleUserCommentTeams.AddRange(TRU.ScheduleUserCommentTeams.FindAll(Function(SC) SC.LinkedRegionId = TRU.RegionId))
                                                                        RU2.ScheduleUserCommentTeams.Sort(ScheduleUserCommentTeam.BasicSorter)
                                                                    End If

                                                                    If ShowSimpleEdit.CanCancelGame Then
                                                                        RU2.GameStatus = TRU.GameStatus
                                                                    End If

                                                                    TRU = New ScheduleTemp
                                                                    TRU.FromSchedule(RU2, RU)

                                                                    RU = RU2
                                                                End If
                                                            End If

                                                            UpsertScheduleHelper(Regions, TRU, RU, CurrentScheduleIds(TRU.RegionId), RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, GameEmails, AssignorEmails, DateAdded, DeleteSchedules, InsertSchedules, InsertSchedulePositions, InsertScheduleFines, InsertScheduleUserComments, InsertScheduleUserCommentTeams, InsertScheduleCommentTeams, InsertScheduleCallups, GamesThatScoresNeedToBeUpdated)
                                                        End If
                                                    End If
                                                End Sub)

                UmpireAssignor.Schedule.BulkDelete(DeleteSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.Schedule.BulkInsert(InsertSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.Schedule.BulkInsertVersions(InsertSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.SchedulePositionFull.BulkInsert(InsertSchedulePositions, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleFineFull.BulkInsert(InsertScheduleFines, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleUserCommentFull.BulkInsert(InsertScheduleUserComments, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleUserCommentTeamFull.BulkInsert(InsertScheduleUserCommentTeams, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleCommentTeamFull.BulkInsert(InsertScheduleCommentTeams, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleCallupFull.BulkInsert(InsertScheduleCallups, SqlConnection, SqlTransaction)

                UmpireAssignor.ScheduleId.UpsertHelper(CurrentScheduleIds.Values.ToList(), SqlConnection, SqlTransaction)

                CancelTempRegionsHelper(AssignorId, UniqueRegionIdsWithTeamsLeagues, SqlConnection, SqlTransaction)

                FixGameEmails(GameEmails, AssignorEmails, SqlConnection, SqlTransaction)
                
                UpdateLadderLeagueAfterScheduleSave(UniqueRegionIds, Regions, RegionUsers, GamesThatScoresNeedToBeUpdated, OldFullSchedule, FullSchedule, GameEmails, SqlConnection, SqlTransaction)

                SqlTransaction.Commit()
            End Using
        End Using

        For Each GameEmail In GameEmails
            Try
                GameEmail.IsScheduleChangeEmail = true
                GameEmail.Send(Region)
            Catch ex As Exception
                PublicCode.SendEmailStandard(
                    "integration@asportsmanager.com",
                    "Coding Error saving schedule temp 1 gameemail",
                    ex.Message & vbCrLf & vbCrLf & ex.StackTrace 
                )
            End Try
        Next
        SendLastMinuteChanges(GameEmails, RegionUsers)

        For Each AssignorEmail In AssignorEmails
            AssignorEmail.Send(Region)
        Next
        SendLastMinuteChanges(AssignorEmails, RegionUsers)

        Return New With {.Success = True}
        'Catch ex As Exception
        '    PublicCode.SendEmailStandard(
        '        "integration@asportsmanager.com",
        '        "Coding Error saving schedule temp",
        '        ex.Message & vbCrLf & vbCrLf & ex.StackTrace 
        '    )
        '    Return New With {.Success = False}
        'End Try
    End Function

    Public Shared Function SaveTempSchedule(AssignorId As String, RegionId As String, ScheduleIds As List(Of String), Optional ByPassPermissions As Boolean = False) As Object
        Dim GameEmails As New List(Of GameEmail)
        Dim AssignorEmails As New List(Of GameEmail)
        Dim Region As RegionProperties = Nothing

        Dim DateAdded As Date = Date.UtcNow
        Dim RegionUsers As New List(Of RegionUser)

        Dim InsertScheduleCallups As New List(Of ScheduleCallupFull)
        Dim GamesThatScoresNeedToBeUpdated As New List(Of Schedule)

        Dim FullSchedule As New List(Of Schedule)
        Dim OldFullSchedule As New List(Of Schedule)

        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Dim UniqueRegionIds As New List(Of String)
                UniqueRegionIds.Add(RegionId)
                Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                If ByPassPermissions AndAlso (Assignor Is Nothing OrElse Not Assignor.IsExecutive) Then
                    Return New ErrorObject("InvalidPermissions")
                End If

                'Dim IsSubmitted As Boolean = CheckAllRegionsIsSubmitted(UniqueRegionIds, AssignorId, SqlConnection, SqlTransaction)

                'If Not IsSubmitted Then Return New ErrorObject("NoChanges")

                Dim ScheduleTemps = GetMasterScheduleFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, SqlConnection, SqlTransaction)
                SchedulePosition.GetMasterSchedulePositionFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, ScheduleTemps, SqlConnection, SqlTransaction)
                ScheduleFine.GetMasterScheduleFineFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, ScheduleTemps, SqlConnection, SqlTransaction)
                ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, ScheduleTemps, SqlConnection, SqlTransaction)
                ScheduleUserCommentTeam.GetMasterScheduleUserCommentTeamFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, ScheduleTemps, SqlConnection, SqlTransaction)
                ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, ScheduleTemps, SqlConnection, SqlTransaction)
                ScheduleCallup.GetMasterScheduleCallUpFromRegionsTempHelper(UniqueRegionIds, AssignorId, Date.MinValue, Date.MaxValue, ScheduleTemps, SqlConnection, SqlTransaction)

                Dim Regions = UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)

                Dim UniqueLadderLeagueRegionIds As New List(Of String)
                For Each UniqueRegionId In UniqueRegionIds
                    Dim UniqueRegion = RegionProperties.GetItem(Regions, UniqueRegionId)
                    If UniqueRegion.RegionIsLadderLeague Then
                        UniqueLadderLeagueRegionIds.Add(UniqueRegionId)
                    End If
                Next

                If UniqueLadderLeagueRegionIds.Count > 0 Then
                    OldFullSchedule = GetMasterScheduleFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, SqlConnection, SqlTransaction)
                    SchedulePosition.GetMasterSchedulePositionFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, OldFullSchedule, SqlConnection, SqlTransaction)
                    ScheduleFine.GetMasterScheduleFineFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, OldFullSchedule, SqlConnection, SqlTransaction)
                    ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, OldFullSchedule, SqlConnection, SqlTransaction)
                    ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, OldFullSchedule, SqlConnection, SqlTransaction)
                End If

                Dim RegionLeagues = RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(RegionLeague.GetRegionsLeaguesHelper(UniqueRegionIds, SqlConnection, SqlTransaction), New List(Of RegionLeaguePay))

                Dim UniqueRegionIdsWithTeamsLeagues As New List(Of String)
                UniqueRegionIdsWithTeamsLeagues.AddRange(UniqueRegionIds)
                For Each RegionLeague In RegionLeagues
                    If RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked AndAlso Not UniqueRegionIdsWithTeamsLeagues.Contains(RegionLeague.RealLeagueId) AndAlso Regions.Any(Function(R) R.RegionId = RegionLeague.RegionId AndAlso R.EntityType = "team") Then UniqueRegionIdsWithTeamsLeagues.Add(RegionLeague.RealLeagueId)
                Next

                Dim UniqueRegionIdsWithLeagues As New List(Of String)
                Dim NotYetFetchedRegionIds As New List(Of String)
                UniqueRegionIdsWithLeagues.AddRange(UniqueRegionIds)
                For Each RegionLeague In RegionLeagues
                    If RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked AndAlso Not UniqueRegionIdsWithLeagues.Contains(RegionLeague.RealLeagueId) Then
                        UniqueRegionIdsWithLeagues.Add(RegionLeague.RealLeagueId)
                        NotYetFetchedRegionIds.Add(RegionLeague.RealLeagueId)
                    End If
                Next

                Regions.AddRange(UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(UniqueRegionIdsWithLeagues, SqlConnection, SqlTransaction))
                Regions.Sort(UmpireAssignor.RegionProperties.BasicSorter)

                Dim Teams = Team.GetTeamsInRegionsHelper(UniqueRegionIdsWithLeagues, SqlConnection, SqlTransaction)
                Dim OfficialRegions = OfficialRegion.GetOfficialRegionsInRegionsHelper(UniqueRegionIdsWithLeagues, SqlConnection, SqlTransaction)
                Dim RegionParks = Park.GetParksInRegionsHelper(UniqueRegionIdsWithLeagues, SqlConnection, SqlTransaction)

                Dim NotYetFetchedLeagues As New List(Of String)
                For Each Team In Teams
                    If Team.RealTeamId <> "" AndAlso Team.IsLinked AndAlso Not UniqueRegionIds.Contains(Team.RealTeamId) AndAlso Not NotYetFetchedLeagues.Contains(Team.RealTeamId) Then
                        NotYetFetchedLeagues.Add(Team.RealTeamId)
                    End If
                Next
                For Each OfficialRegion In OfficialRegions
                    If OfficialRegion.RealOfficialRegionId <> "" AndAlso OfficialRegion.IsLinked AndAlso Not UniqueRegionIds.Contains(OfficialRegion.RealOfficialRegionId) AndAlso Not NotYetFetchedLeagues.Contains(OfficialRegion.RealOfficialRegionId) Then
                        NotYetFetchedLeagues.Add(OfficialRegion.RealOfficialRegionId)
                    End If
                Next

                Dim AllRegionIdsSoFar As New List(Of String)
                AllRegionIdsSoFar.AddRange(UniqueRegionIds)
                AllRegionIdsSoFar.AddRange(NotYetFetchedLeagues)

                RegionUsers = RegionUser.LoadAllInRegionIdsHelper(AllRegionIdsSoFar, "superadmin", SqlConnection, SqlTransaction)

                RegionLeagues.AddRange(RegionLeaguePayContracted.ConvertToRegionLeaguePayContracted(RegionLeague.GetRegionsLeaguesHelper(NotYetFetchedLeagues, SqlConnection, SqlTransaction), New List(Of RegionLeaguePay)))
                RegionLeagues.Sort(RegionLeaguePayContracted.BasicSorter)

                Regions.AddRange(UmpireAssignor.RegionProperties.GetRegionPropertiesRegionIdsHelper(NotYetFetchedLeagues, SqlConnection, SqlTransaction))
                Regions.Sort(UmpireAssignor.RegionProperties.BasicSorter)

                Dim TempSchedules = GetMasterScheduleFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, SqlConnection, SqlTransaction)
                SchedulePosition.GetMasterSchedulePositionFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, TempSchedules, SqlConnection, SqlTransaction)
                ScheduleFine.GetMasterScheduleFineFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, TempSchedules, SqlConnection, SqlTransaction)
                ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, TempSchedules, SqlConnection, SqlTransaction)
                ScheduleUserCommentTeam.GetMasterScheduleUserCommentTeamFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, TempSchedules, SqlConnection, SqlTransaction)
                ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, TempSchedules, SqlConnection, SqlTransaction)
                ScheduleCallup.GetMasterScheduleCallUpFromRegionsNonVersionHelper(UniqueRegionIdsWithTeamsLeagues, Date.MinValue, Date.MaxValue, TempSchedules, SqlConnection, SqlTransaction)

                Dim Schedules As New List(Of Schedule)
                For Each ScheduleItem In TempSchedules
                    ' Granular Hack - If not equal to ScheduleId function param, skip
                    If Not ScheduleIds.Contains(ScheduleItem.ScheduleId) Then
                        Continue For
                    End If

                    If UniqueRegionIds.Contains(ScheduleItem.RegionId) Then
                        Schedules.Add(ScheduleItem)
                    Else
                        Dim TRegionId = ScheduleItem.RegionId
                        Dim RealTeamIds As New List(Of String)
                        For Each Team In Teams
                            If Team.RealTeamId <> "" AndAlso Team.IsLinked AndAlso Team.RegionId = TRegionId AndAlso (Team.TeamId = ScheduleItem.HomeTeam.Trim.ToLower OrElse Team.TeamId = ScheduleItem.AwayTeam.Trim.ToLower) AndAlso Not RealTeamIds.Contains(Team.RealTeamId) AndAlso UniqueRegionIds.Contains(Team.RealTeamId) Then
                                RealTeamIds.Add(Team.RealTeamId)
                            End If
                        Next

                        For Each RealTeamId In RealTeamIds
                            If Teams.Any(Function(T) T.RealTeamId = RealTeamId AndAlso T.IsLinked AndAlso T.RegionId = TRegionId AndAlso (T.TeamId = ScheduleItem.HomeTeam.Trim.ToLower OrElse T.TeamId = ScheduleItem.AwayTeam.Trim.ToLower)) Then
                                Dim NewScheduleItem = ConvertToLinkScheduleItem(ScheduleItem, RealTeamId, RegionLeagues)
                                NewScheduleItem.MergeTeamData(RealTeamId, True)
                                Schedules.Add(ConvertToLinkScheduleItem(ScheduleItem, RealTeamId, RegionLeagues))
                            End If
                        Next
                    End If
                Next

                ScheduleTemps.Sort(ScheduleTemp.BasicSorter)
                Schedules.Sort(Schedule.BasicSorter)

                Dim CurrentScheduleIds As New Dictionary(Of String, ScheduleId)
                For Each TRegionId In UniqueRegionIdsWithTeamsLeagues
                    CurrentScheduleIds.Add(TRegionId, New ScheduleId(TRegionId, SqlConnection, SqlTransaction))
                Next

                Dim DeleteSchedules As New List(Of Schedule)
                Dim InsertSchedules As New List(Of Schedule)
                Dim InsertSchedulePositions As New List(Of SchedulePositionFull)
                Dim InsertScheduleFines As New List(Of ScheduleFineFull)
                Dim InsertScheduleUserComments As New List(Of ScheduleUserCommentFull)
                Dim InsertScheduleUserCommentTeams As New List(Of ScheduleUserCommentTeamFull)
                Dim InsertScheduleCommentTeams As New List(Of ScheduleCommentTeamFull)

                PublicCode.ProcessListPairs(ScheduleTemps, Schedules, AddressOf Schedule.TempComparer,
                                                Sub(TRU)
                                                    ' Granular Hack - If not equal to ScheduleId function param, skip
                                                    If Not ScheduleIds.Contains(TRU.ScheduleId) Then Exit Sub

                                                    If Not TRU.IsDeleted Then
                                                        Dim TRegion = Regions.Find(Function(R) R.RegionId = TRU.RegionId)
                                                        If TRU.LinkedRegionId <> "" AndAlso TRegion.EntityType = "team" Then Exit Sub

                                                        UpsertScheduleHelper(Regions, TRU, Nothing, CurrentScheduleIds(TRU.RegionId), RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, GameEmails, AssignorEmails, DateAdded, DeleteSchedules, InsertSchedules, InsertSchedulePositions, InsertScheduleFines, InsertScheduleUserComments, InsertScheduleUserCommentTeams, InsertScheduleCommentTeams, InsertScheduleCallups, GamesThatScoresNeedToBeUpdated)
                                                    End If
                                                End Sub,
                                                Sub(RU)
                                                End Sub,
                                                Sub(TRU, RU)
                                                    ' Granular Hack - If not equal to ScheduleId function param, skip
                                                    If Not ScheduleIds.Contains(TRU.ScheduleId) Or Not ScheduleIds.Contains(RU.ScheduleId) Then Exit Sub

                                                    If RU.IsDifferent(TRU) Then
                                                        If TRU.IsDeleted Then
                                                            Dim TRegion = Regions.Find(Function(R) R.RegionId = RU.RegionId)
                                                            If TRU.LinkedRegionId <> "" AndAlso TRegion.EntityType = "team" Then Exit Sub
                                                            DeleteScheduleHelper(Regions, RU, RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, GameEmails, AssignorEmails, DateAdded, DeleteSchedules, InsertSchedules, GamesThatScoresNeedToBeUpdated)
                                                        Else
                                                            Dim TRegion = Regions.Find(Function(R) R.RegionId = TRU.RegionId)
                                                            If RU.LinkedRegionId <> "" Then
                                                                TRU.LinkedRegionId = RU.LinkedRegionId
                                                                TRU.LinkedScheduleId = RU.LinkedScheduleId
                                                                If TRegion.EntityType = "team" Then

                                                                    Dim ShowSimpleEdit = RU.CanShowSimpleEdit(AssignorId, Regions, RegionUsers, Teams)

                                                                    RU = PublicCode.BinarySearchItem(TempSchedules, New Schedule With {.RegionId = RU.LinkedRegionId, .ScheduleId = RU.LinkedScheduleId}, Schedule.BasicSorterSimple)
                                                                    If RU Is Nothing Then Exit Sub
                                                                    Dim RU2 = RU.CloneItem()

                                                                    If ShowSimpleEdit.CanEditScore Then
                                                                        RU2.HomeTeamScore = TRU.HomeTeamScore
                                                                        RU2.HomeTeamScoreExtra = TRU.HomeTeamScoreExtra
                                                                        RU2.AwayTeamScore = TRU.AwayTeamScore
                                                                        RU2.AwayTeamScoreExtra = TRU.AwayTeamScoreExtra
                                                                    End If

                                                                    If ShowSimpleEdit.CanEditGroupComment Then
                                                                        RU2.ScheduleCommentTeams.RemoveAll(Function(SC) SC.LinkedRegionId = TRU.RegionId)
                                                                        RU2.ScheduleCommentTeams.AddRange(TRU.ScheduleCommentTeams.FindAll(Function(SC) SC.LinkedRegionId = TRU.RegionId))
                                                                        RU2.ScheduleCommentTeams.Sort(ScheduleCommentTeam.BasicSorter)
                                                                    End If

                                                                    If ShowSimpleEdit.CanEditCallUps Then
                                                                        RU2.ScheduleCallUps.RemoveAll(Function(SC) SC.LinkedRegionId = TRU.RegionId)
                                                                        RU2.ScheduleCallUps.AddRange(TRU.ScheduleCallUps.FindAll(Function(SC) SC.LinkedRegionId = TRU.RegionId))
                                                                        RU2.ScheduleCallUps.Sort(ScheduleCallup.BasicSorter)
                                                                    End If

                                                                    If ShowSimpleEdit.CanEditUserComments Then
                                                                        RU2.ScheduleUserCommentTeams.RemoveAll(Function(SC) SC.LinkedRegionId = TRU.RegionId)
                                                                        RU2.ScheduleUserCommentTeams.AddRange(TRU.ScheduleUserCommentTeams.FindAll(Function(SC) SC.LinkedRegionId = TRU.RegionId))
                                                                        RU2.ScheduleUserCommentTeams.Sort(ScheduleUserCommentTeam.BasicSorter)
                                                                    End If

                                                                    If ShowSimpleEdit.CanCancelGame Then
                                                                        RU2.GameStatus = TRU.GameStatus
                                                                    End If

                                                                    TRU = New ScheduleTemp
                                                                    TRU.FromSchedule(RU2, RU)

                                                                    RU = RU2
                                                                End If
                                                            End If

                                                            UpsertScheduleHelper(Regions, TRU, RU, CurrentScheduleIds(TRU.RegionId), RegionUsers, RegionParks, RegionLeagues, Teams, OfficialRegions, GameEmails, AssignorEmails, DateAdded, DeleteSchedules, InsertSchedules, InsertSchedulePositions, InsertScheduleFines, InsertScheduleUserComments, InsertScheduleUserCommentTeams, InsertScheduleCommentTeams, InsertScheduleCallups, GamesThatScoresNeedToBeUpdated)
                                                        End If
                                                    End If
                                                End Sub)

                UmpireAssignor.Schedule.BulkDelete(DeleteSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.Schedule.BulkInsert(InsertSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.Schedule.BulkInsertVersions(InsertSchedules, SqlConnection, SqlTransaction)
                UmpireAssignor.SchedulePositionFull.BulkInsert(InsertSchedulePositions, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleFineFull.BulkInsert(InsertScheduleFines, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleUserCommentFull.BulkInsert(InsertScheduleUserComments, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleUserCommentTeamFull.BulkInsert(InsertScheduleUserCommentTeams, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleCommentTeamFull.BulkInsert(InsertScheduleCommentTeams, SqlConnection, SqlTransaction)
                UmpireAssignor.ScheduleCallupFull.BulkInsert(InsertScheduleCallups, SqlConnection, SqlTransaction)

                Dim tempList = CurrentScheduleIds.Values.ToList()

                UmpireAssignor.ScheduleId.UpsertHelper(CurrentScheduleIds.Values.ToList(), SqlConnection, SqlTransaction)

                CancelTempScheduleHelper(AssignorId, UniqueRegionIdsWithTeamsLeagues, ScheduleIds, SqlConnection, SqlTransaction)
                'CancelTempRegionsHelper(AssignorId, UniqueRegionIdsWithTeamsLeagues, SqlConnection, SqlTransaction)

                FixGameEmails(GameEmails, AssignorEmails, SqlConnection, SqlTransaction)

                UpdateLadderLeagueAfterScheduleSave(UniqueRegionIds, Regions, RegionUsers, GamesThatScoresNeedToBeUpdated, OldFullSchedule, FullSchedule, GameEmails, SqlConnection, SqlTransaction)

                SqlTransaction.Commit()
            End Using
        End Using

        For Each GameEmail In GameEmails
            Try
                GameEmail.IsScheduleChangeEmail = True
                GameEmail.Send(Region)
            Catch ex As Exception
                PublicCode.SendEmailStandard(
                    "integration@asportsmanager.com",
                    "Coding Error saving schedule temp 1 gameemail",
                    ex.Message & vbCrLf & vbCrLf & ex.StackTrace
                )
            End Try
        Next
        SendLastMinuteChanges(GameEmails, RegionUsers)

        For Each AssignorEmail In AssignorEmails
            AssignorEmail.Send(Region)
        Next
        SendLastMinuteChanges(AssignorEmails, RegionUsers)

        Return New With {.Success = True}
        'Catch ex As Exception
        '    PublicCode.SendEmailStandard(
        '        "integration@asportsmanager.com",
        '        "Coding Error saving schedule temp",
        '        ex.Message & vbCrLf & vbCrLf & ex.StackTrace 
        '    )
        '    Return New With {.Success = False}
        'End Try
    End Function


    Private Shared Sub UpdateLadderLeagueAfterScheduleSave(UniqueRegionIds As List(Of String), Regions As List(Of RegionProperties), RegionUsers As List(Of RegionUser), GamesThatScoresNeedToBeUpdated As List(Of Schedule), ByRef OldFullSchedule As List(Of Schedule), ByRef FullSchedule As List(Of Schedule), GameEmails As List(Of GameEmail), SqlConnection As SqlConnection, SqlTransaction As SqlTransaction)
        Dim UniqueLadderLeagueRegionIds As New List(Of String)
        For Each UniqueRegionId In UniqueRegionIds
            Dim UniqueRegion = RegionProperties.GetItem(Regions, UniqueRegionId)
            If UniqueRegion.RegionIsLadderLeague Then
                    UniqueLadderLeagueRegionIds.Add(UniqueRegionId)
            End If
        Next

        If UniqueLadderLeagueRegionIds.Count > 0 Then
            FullSchedule = GetMasterScheduleFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, SqlConnection, SqlTransaction)
            SchedulePosition.GetMasterSchedulePositionFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, FullSchedule, SqlConnection, SqlTransaction)
            ScheduleFine.GetMasterScheduleFineFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds,Date.MinValue, Date.MaxValue, FullSchedule, SqlConnection, SqlTransaction)
            ScheduleUserComment.GetMasterScheduleUserCommentFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue, FullSchedule, SqlConnection, SqlTransaction)
            ScheduleCommentTeam.GetMasterScheduleCommentTeamFromRegionsNonVersionHelper(UniqueLadderLeagueRegionIds, Date.MinValue, Date.MaxValue,  FullSchedule, SqlConnection, SqlTransaction)
        End If

        For Each UniqueRegionId In UniqueLadderLeagueRegionIds
            Dim UniqueRegion = RegionProperties.GetItem(Regions, UniqueRegionId)
            LadderLeague.UpdateGameRanks(
                RegionUser.FilterByRegionId(RegionUsers, UniqueRegionId),
                FilterByRegionId(OldFullSchedule, UniqueRegionId),
                FilterByRegionId(FullSchedule, UniqueRegionId),
                FilterByRegionId(LadderLeague.SortedGamesWhosScoresNeedUpdatingConversion(GamesThatScoresNeedToBeUpdated), UniqueRegionId),
                GameEmails,
                SqlConnection,
                SqlTransaction)
        Next
    End Sub
    Public Shared Sub FixGameEmails(GameEmails As List(Of GameEmail), AssignorEmails As List(Of GameEmail), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        Dim AllRealUsernames As New List(Of String)

        If GameEmails IsNot Nothing Then
            For Each GameEmail In GameEmails
                If GameEmail.RegionUser IsNot Nothing AndAlso GameEmail.RegionUser.RealUsername <> "" AndAlso GameEmail.RegionUser.IsLinked Then
                    If Not AllRealUsernames.Contains(GameEmail.RegionUser.RealUsername) Then AllRealUsernames.Add(GameEmail.RegionUser.RealUsername)
                End If
            Next
        End If

        If AssignorEmails IsNot Nothing Then
            For Each GameEmail In AssignorEmails
                If GameEmail.RegionUser IsNot Nothing AndAlso GameEmail.RegionUser.RealUsername <> "" AndAlso GameEmail.RegionUser.IsLinked Then
                    If Not AllRealUsernames.Contains(GameEmail.RegionUser.RealUsername) Then AllRealUsernames.Add(GameEmail.RegionUser.RealUsername)
                End If
            Next
        End If

        Dim Users = User.GetUsersInfoHelper(AllRealUsernames, SQLConnection, SQLTransaction)

        If GameEmails IsNot Nothing Then
            For Each GameEmail In GameEmails
                If GameEmail.RegionUser IsNot Nothing AndAlso GameEmail.RegionUser.RealUsername <> "" AndAlso GameEmail.RegionUser.IsLinked Then
                    GameEmail.User = Users.Find(Function(U) U.Username = GameEmail.RegionUser.RealUsername)
                End If
            Next
        End If

        If AssignorEmails IsNot Nothing Then
            For Each GameEmail In AssignorEmails
                If GameEmail.RegionUser IsNot Nothing AndAlso GameEmail.RegionUser.RealUsername <> "" AndAlso GameEmail.RegionUser.IsLinked Then
                    GameEmail.User = Users.Find(Function(U) U.Username = GameEmail.RegionUser.RealUsername)
                End If
            Next
        End If
    End Sub

    Public Shared Sub SendLastMinuteChanges(GameEmails As List(Of GameEmail), RegionUsers As List(Of RegionUser))

        For Each GameEmail In GameEmails
            Dim ScheduleDataFilter As New List(Of ScheduleDataEmail)
            For Each ScheduleData In GameEmail.ScheduleData
                Dim TNewItem = ScheduleData.NewScheduleItem
                Dim TOldItem = ScheduleData.OldScheduleItem
                Dim Region = ScheduleData.Region

                Dim TimeZone = PublicCode.OlsonTimeZoneToTimeZoneInfo(Region.TimeZone)

                If Region.EntityType = "referee" Then
                    If TNewItem IsNot Nothing AndAlso Not ScheduleData.NewScheduleItem.ContainsOfficialIdOnPosition(ScheduleData.RegionUser.Username) Then
                        TNewItem = Nothing
                    End If

                    If TOldItem IsNot Nothing AndAlso Not ScheduleData.OldScheduleItem.ContainsOfficialIdOnPosition(ScheduleData.RegionUser.Username) Then
                        TOldItem = Nothing
                    End If
                ElseIf Region.EntityType = "team" Then
                    If TNewItem IsNot Nothing AndAlso Not ScheduleData.NewScheduleItem.ContainsTeamMemberId(ScheduleData.RegionUser.Username, RegionUsers) Then
                        TNewItem = Nothing
                    End If

                    If TOldItem IsNot Nothing AndAlso Not ScheduleData.OldScheduleItem.ContainsTeamMemberId(ScheduleData.RegionUser.Username, RegionUsers) Then
                        TOldItem = Nothing
                    End If
                End If

                Dim TTuple As New ScheduleDataEmail(ScheduleData.RegionUser, TNewItem, TOldItem, ScheduleData.RegionId, ScheduleData.Region, ScheduleData.IsAvailable)
                ScheduleData = TTuple

                Dim MinDate = Date.UtcNow.AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours)
                Dim MaxDate = Date.UtcNow.AddDays(2).AddHours(TimeZone.GetUtcOffset(Date.UtcNow).TotalHours)

                If ScheduleData.NewScheduleItem IsNot Nothing Then
                    If ScheduleData.NewScheduleItem.GameDate >= MinDate AndAlso ScheduleData.NewScheduleItem.GameDate <= MaxDate Then
                        If ScheduleData.OldScheduleItem IsNot Nothing Then
                            If (ScheduleData.NewScheduleItem.IsCancelled()) AndAlso (ScheduleData.OldScheduleItem.IsCancelled()) Then
                                Continue For
                            End If
                        Else
                            If (ScheduleData.NewScheduleItem.IsCancelled()) Then
                                Continue For
                            End If
                        End If
                        ScheduleDataFilter.Add(ScheduleData)
                        Continue For
                    End If
                End If

                If ScheduleData.OldScheduleItem IsNot Nothing Then
                    If ScheduleData.OldScheduleItem.GameDate >= MinDate AndAlso ScheduleData.OldScheduleItem.GameDate <= MaxDate Then
                        If ScheduleData.NewScheduleItem Is Nothing Then
                            If (ScheduleData.OldScheduleItem.IsCancelled) Then
                                Continue For
                            End If
                        End If
                        ScheduleDataFilter.Add(ScheduleData)
                        Continue For
                    End If
                End If
            Next

            If ScheduleDataFilter.Count = 0 Then Continue For
            If GameEmail.User Is Nothing Then Continue For
            Dim UserCellNumber As String = GameEmail.User.GetCellPhoneNumber()
            If UserCellNumber = "" Then Continue For
            If Not GameEmail.User.SMSLastMinuteChanges Then Continue For

            Dim ScheduleSB As New StringBuilder

            Dim L As String = GameEmail.User.PreferredLanguage
            Dim T As String = "Text"

            ScheduleSB.Append(String.Format(Languages.GetText("SMSLastMinuteChanges", "LastMinute", T, L), GameEmail.User.FirstName))
            For Each ScheduleDataItem In ScheduleDataFilter
                If ScheduleDataItem.NewScheduleItem IsNot Nothing AndAlso ScheduleDataItem.OldScheduleItem Is Nothing Then
                    ScheduleSB.Append(vbCrLf)
                    ScheduleSB.Append(vbCrLf)
                    ScheduleSB.Append(Languages.GetText("SMSLastMinuteChanges", "NewGame", T, L))
                    ScheduleSB.Append(" ")
                    ScheduleDataItem.NewScheduleItem.SMSGame(ScheduleSB, {ScheduleDataItem.Region}.ToList(), {ScheduleDataItem.RegionUser}.ToList(), GameEmail.RegionUsers, GameEmail.RegionParks, GameEmail.RegionLeagues, GameEmail.Teams, L, True)
                    ScheduleSB.Append(".")
                ElseIf ScheduleDataItem.NewScheduleItem Is Nothing AndAlso ScheduleDataItem.OldScheduleItem IsNot Nothing Then
                    ScheduleSB.Append(vbCrLf)
                    ScheduleSB.Append(vbCrLf)
                    ScheduleSB.Append(Languages.GetText("SMSLastMinuteChanges", "Removed", T, L))
                    ScheduleSB.Append(" ")
                    ScheduleDataItem.OldScheduleItem.SMSGame(ScheduleSB, {ScheduleDataItem.Region}.ToList(), {ScheduleDataItem.RegionUser}.ToList(), GameEmail.RegionUsers, GameEmail.RegionParks, GameEmail.RegionLeagues, GameEmail.Teams, L, True)
                    ScheduleSB.Append(".")
                ElseIf ScheduleDataItem.NewScheduleItem IsNot Nothing AndAlso ScheduleDataItem.OldScheduleItem IsNot Nothing Then
                    ScheduleSB.Append(vbCrLf)
                    ScheduleSB.Append(vbCrLf)
                    If (ScheduleDataItem.NewScheduleItem.IsCancelled) AndAlso
                        Not (ScheduleDataItem.OldScheduleItem.IsCancelled) Then
                        ScheduleDataItem.OldScheduleItem.SMSGame(ScheduleSB, {ScheduleDataItem.Region}.ToList(), {ScheduleDataItem.RegionUser}.ToList(), GameEmail.RegionUsers, GameEmail.RegionParks, GameEmail.RegionLeagues, GameEmail.Teams, L, True)
                        ScheduleSB.Append(" ")
                        ScheduleSB.Append(Languages.GetText("SMSLastMinuteChanges", "BeenCancelled", T, L))
                    ElseIf (ScheduleDataItem.OldScheduleItem.IsCancelled) AndAlso
                        Not (ScheduleDataItem.NewScheduleItem.IsCancelled) Then
                        ScheduleDataItem.NewScheduleItem.SMSGame(ScheduleSB, {ScheduleDataItem.Region}.ToList(), {ScheduleDataItem.RegionUser}.ToList(), GameEmail.RegionUsers, GameEmail.RegionParks, GameEmail.RegionLeagues, GameEmail.Teams, L, True)
                        ScheduleSB.Append(" ")
                        ScheduleSB.Append(Languages.GetText("SMSLastMinuteChanges", "IsNowBackOn", T, L))
                        ScheduleSB.Append(".")
                    Else
                        ScheduleSB.Append(Languages.GetText("SMSLastMinuteChanges", "WasGame", T, L))
                        ScheduleSB.Append(vbCr)
                        ScheduleDataItem.OldScheduleItem.SMSGame(ScheduleSB, {ScheduleDataItem.Region}.ToList(), {ScheduleDataItem.RegionUser}.ToList(), GameEmail.RegionUsers, GameEmail.RegionParks, GameEmail.RegionLeagues, GameEmail.Teams, L, True)
                        ScheduleSB.Append(vbCrLf)
                        ScheduleSB.Append(Languages.GetText("SMSLastMinuteChanges", "IsNow", T, L))
                        ScheduleSB.Append(vbCrLf)
                        ScheduleDataItem.NewScheduleItem.SMSGame(ScheduleSB, {ScheduleDataItem.Region}.ToList(), {ScheduleDataItem.RegionUser}.ToList(), GameEmail.RegionUsers, GameEmail.RegionParks, GameEmail.RegionLeagues, GameEmail.Teams, L, True)
                        ScheduleSB.Append(".")
                    End If
                ElseIf ScheduleDataItem.NewScheduleItem Is Nothing AndAlso ScheduleDataItem.OldScheduleItem Is Nothing Then
                    'Do Nothing
                End If
            Next

            Try
                PublicCode.SendSMS(UserCellNumber, ScheduleSB.ToString())
            Catch
            End Try
        Next
    End Sub

    Public Shared Function CancelTemp(AssignorId As String, RegionId As String) As Object
        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim UniqueRegionIds As New List(Of String)
                    If RegionId = "allregions" OrElse RegionId = "alleditableregions" Then
                        UniqueRegionIds = RegionUser.GetAllMyRegionsIdExecs(AssignorId, SqlConnection, SqlTransaction)
                    Else
                        UniqueRegionIds.Add(RegionId)
                        Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                        If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                            Return New ErrorObject("InvalidPermissions")
                        End If
                    End If

                    Dim Regions = UmpireAssignor.Region.GetRegionFromRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)

                    For Each Region In Regions
                        'If Region.RequiresPayment Then Return New ErrorObject("RequiresPayment")
                    Next

                    CancelTempRegionsHelper(AssignorId, Region.ToRegionIds(Regions), SqlConnection, SqlTransaction)

                    SqlTransaction.Commit()
                End Using
            End Using
            Return New With {.Success = True}
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function CancelTempRegionSchedule(AssignorId As String, RegionId As String, ScheduleIds As List(Of String))
        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim UniqueRegionIds As New List(Of String)
                    If RegionId = "allregions" OrElse RegionId = "alleditableregions" Then
                        UniqueRegionIds = RegionUser.GetAllMyRegionsIdExecs(AssignorId, SqlConnection, SqlTransaction)
                    Else
                        UniqueRegionIds.Add(RegionId)
                        Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, AssignorId, SqlConnection, SqlTransaction)
                        If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                            Return New ErrorObject("InvalidPermissions")
                        End If
                    End If

                    Dim Regions = UmpireAssignor.Region.GetRegionFromRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction)

                    For Each Region In Regions
                        'If Region.RequiresPayment Then Return New ErrorObject("RequiresPayment")
                    Next

                    CancelTempScheduleHelper(AssignorId, New List(Of String) From {RegionId}, ScheduleIds, SqlConnection, SqlTransaction)

                    SqlTransaction.Commit()
                End Using
            End Using
            Return New With {.Success = True}
        Catch ex As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Sub CancelTempHelper(AssignorId As String, RegionId As String, SQLCOnnection As SqlConnection, SQLTransaction As SqlTransaction, SQLCommand As SqlCommand, CommandSB As StringBuilder, ByRef CommandCount As Integer)
        CommandCount += 1
        CommandSB.Append("DELETE FROM ScheduleTempSubmitted WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId; ".Replace("{0}", CommandCount))
        CommandSB.Append("DELETE FROM ScheduleTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId; ".Replace("{0}", CommandCount))
        CommandSB.Append("DELETE FROM SchedulePositionTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId; ".Replace("{0}", CommandCount))
        CommandSB.Append("DELETE FROM ScheduleFineTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId; ".Replace("{0}", CommandCount))
        CommandSB.Append("DELETE FROM ScheduleUserCommentTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId; ".Replace("{0}", CommandCount))
        CommandSB.Append("DELETE FROM ScheduleUserCommentTeamTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId; ".Replace("{0}", CommandCount))
        CommandSB.Append("DELETE FROM ScheduleCommentTeamTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId; ".Replace("{0}", CommandCount))
        CommandSB.Append("DELETE FROM ScheduleCallUpTemp WHERE RegionId = @RegionId{0} AND UserSubmitId = @AssignorId; ".Replace("{0}", CommandCount))
        SQLCommand.Parameters.Add(New SqlParameter("RegionId" & CommandCount, RegionId))
    End Sub

    Public Shared Sub CancelTempRegionsHelper(AssignorId As String, RegionIds As List(Of String), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        Dim CommandSB As New StringBuilder()
        CommandSB.Append("DELETE FROM ScheduleTempSubmitted WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM ScheduleTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM SchedulePositionTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM ScheduleFineTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM ScheduleUserCommentTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM ScheduleUserCommentTeamTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM ScheduleCommentTeamTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM ScheduleCallUpTemp WHERE RegionId IN ({0}) AND UserSubmitId = @AssignorId; ")
        Using SQLCommand As New SqlCommand(CommandSB.ToString().Replace("{0}", PublicCode.CreateParamStr("RegionId", RegionIds)), SQLConnection, SQLTransaction)
            SQLCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
            PublicCode.CreateParamsSQLCommand("RegionId", RegionIds, SQLCommand)
            SQLCommand.ExecuteNonQuery()
        End Using
    End Sub

    Public Shared Sub CancelTempScheduleHelper(AssignorId As String, RegionIds As List(Of String), ScheduleIds As List(Of String), SqlConnection As SqlConnection, SQLTransaction As SqlTransaction)
        Dim CommandSB As New StringBuilder()
        'CommandSB.Append("DELETE FROM ScheduleTempSubmitted WHERE RegionId IN ({0}) AND ScheduleId = @ScheduleId AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM ScheduleTemp WHERE RegionId IN ({0}) AND ScheduleId {1} AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM SchedulePositionTemp WHERE RegionId IN ({0}) AND ScheduleId {1} AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM ScheduleFineTemp WHERE RegionId IN ({0}) AND ScheduleId {1} AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM ScheduleUserCommentTemp WHERE RegionId IN ({0}) AND ScheduleId {1} AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM ScheduleUserCommentTeamTemp WHERE RegionId IN ({0}) AND ScheduleId {1} AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM ScheduleCommentTeamTemp WHERE RegionId IN ({0}) AND ScheduleId {1} AND UserSubmitId = @AssignorId; ")
        CommandSB.Append("DELETE FROM ScheduleCallUpTemp WHERE RegionId IN ({0}) AND ScheduleId {1} AND UserSubmitId = @AssignorId; ")
        Using SQLCommand As New SqlCommand(CommandSB.ToString().Replace("{0}", PublicCode.CreateParamStr("RegionId", RegionIds)).Replace("{1}", PublicCode.CreateSqlInStatement(ScheduleIds)), SqlConnection, SQLTransaction)
            SQLCommand.Parameters.Add(New SqlParameter("AssignorId", AssignorId))
            PublicCode.CreateParamsSQLCommand("RegionId", RegionIds, SQLCommand)
            SQLCommand.ExecuteNonQuery()
        End Using
    End Sub

End Class
