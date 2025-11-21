Imports System.Data.SqlClient
Imports Newtonsoft.Json

Public Class RegionPropertiesFetcher
    Public Property TRegionProperties As New List(Of RegionProperties)
    Public Property RegionIdHelper As New RegionIdHelper

    Public Function GetRegionPropertiesRegionIdsHelper(RegionIds As List(Of String), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionProperties)
        Dim ToFetchRegionIds = RegionIdHelper.DoFetch(RegionIds)

        If ToFetchRegionIds.Count > 0 Then
            TRegionProperties.AddRange(RegionProperties.GetRegionPropertiesRegionIdsHelper(ToFetchRegionIds, SQLConnection, SQLTransaction))
            TRegionProperties.Sort(RegionProperties.BasicSorter)
        End If

        Return TRegionProperties
    End Function
End Class

Public Class ExtraScoreParameter
    Public Property ExtraScoreParameterId As String
    Public Property VariableType As String
    Public Property NameAndShorthands As Dictionary(Of String, NameShorthand)
End Class

Public Class RegionProperties
    Public Property RegionId As String
    Public Property RegionName As String
    Public Property Sport As String
    Public Property EntityType As String
    Public Property Season As Integer
    Public Property RegistrationDate As Date
    Public Property DefaultCrew As Dictionary(Of String, String)
    Public Property ShowAddress As Boolean
    Public Property ShowRank As Boolean
    Public Property ShowSubRank As Boolean
    Public Property ShowRankToNonMembers as Boolean
    Public Property ShowSubRankToNonMembers as Boolean
    Public Property DefaultContactListSort As String
    Public Property ShowRankInSchedule As Boolean
    Public Property ShowSubRankInSchedule As Boolean
    Public Property MaxSubRankRequest as Decimal
    Public Property SubRankIsNumber as Boolean
    Public Property ShowTeamsInSchedule As Boolean
    Public Property SortSubRankDesc As Boolean
    Public Property SeniorRankNameEnglish As String
    Public Property SeniorRankNameFrench As String
    Public Property JuniorRankNameEnglish As String
    Public Property JuniorRankNameFrench As String
    Public Property IntermediateRankNameEnglish As String
    Public Property IntermediateRankNameFrench As String
    Public Property NoviceRankNameEnglish As String
    Public Property NoviceRankNameFrench As String
    Public Property RookieRankNameEnglish As String
    Public Property RookieRankNameFrench As String
    Public Property AllowBookOffs As Boolean
    Public Property BookOffHoursBefore As Integer
    Public Property LastExecutedCronJob As Date
    Public Property HasPaid As Boolean
    Public Property PaymentData As String
    Public Property MaxDateShowAvailableSpots As Date
    Public Property EmailAvailableSpots As Boolean
    Public Property EmailRequestedGames As Boolean
    Public Property DefaultArriveBeforeMins As Integer
    Public Property DefaultMaxGameLengthMins As Integer
    Public Property OnlyAllowedToConfirmDaysBefore As Integer
    Public Property EmailConfirmDaysBefore As Integer
    Public Property HasOfficials As Boolean
    Public Property HasScorekeepers As Boolean
    Public Property HasSupervisors As Boolean
    Public Property IsDemo As Boolean
    Public Property Country As String
    Public Property State As String
    Public Property City As String
    Public Property Address As String
    Public Property PostalCode As String
    Public Property ShowLinksToNonMembers As Boolean
    Public Property ShowParksToNonMembers As Boolean
    Public Property ShowLeaguesToNonMembers As Boolean
    Public Property ShowOfficialRegionsToNonMembers As Boolean
    Public Property ShowTeamsToNonMembers As Boolean
    Public Property ShowHolidayListToNonMembers As Boolean
    Public Property ShowContactListToNonMembers As Boolean
    Public Property ShowAvailabilityDueDateToNonMembers As Boolean
    Public Property ShowAvailabilityToNonMembers As Boolean
    Public Property ShowAvailableSpotsToNonMembers As Boolean
    Public Property ShowFullScheduleToNonMembers As Boolean

    Public Property ShowStandingsToNonMembers As Boolean
    Public Property ShowStatsToNonMembers As Boolean
    Public Property ShowMainPageToNonMembers As Boolean
    Public Property HasEnteredMainPage As Boolean
    Public Property ExtraScoreParameters As List(Of ExtraScoreParameter)
    Public Property HomeTeamCanEnterScore As Boolean
    Public Property AwayTeamCanEnterScore As Boolean
    Public Property ScorekeeperCanEnterScore As Boolean
    Public Property TeamCanEnterStats As Boolean
    Public Property ScorekeeperCanEnterStats As Boolean
    Public Property RegionIsLadderLeague As Boolean
    Public Property NumberOfPlayersInLadderLeague As Integer
    Public Property HomeTeamCanCancelGameHoursBefore As Integer
    Public Property AwayTeamCanCancelGameHoursBefore As Integer

    Public Property MinimumValue As Dictionary(Of String, String)
    Public Property MinimumPercentage As Dictionary(Of String, String)
    Public Property UniquePlayers As Boolean

    Public Property TimeZone As String
    Public Property AutoSyncCancellationHoursBefore As Integer
    Public Property AutoSyncSchedule As Boolean

    Public Property StatIndex As Integer
    Public Property StandingIndex As Integer

    Public Property DefaultArriveBeforeAwayMins As Integer
    Public Property DefaultArriveBeforePracticeMins As Integer
    Public Property DefaultMaxGameLengthPracticeMins As Integer

    Public Property IncludeAfternoonAvailabilityOnWeekdays As Boolean
    Public Property IncludeMorningAvailabilityOnWeekdays As Boolean
    Public Property EnableNotFilledInInAvailability As Boolean
    Public Property EnableDualAvailability As Boolean
    Public Property IsAvailableText As Dictionary(Of String, String)
    Public Property IsAvailableDualText As Dictionary(Of String, String)
    Public Property IsAvailableCombinedText As Dictionary(Of String, String)
    Public Property ShowOnlyDueDatesRangeForAvailability As Boolean

    Public Property ShowRankInGlobalAvailability As Boolean
    Public Property ShowSubRankInGlobalAvailability As Boolean
    Public Property SortGlobalAvailabilityByRank As Boolean
    Public Property SortGlobalAvailabilityBySubRank As Boolean
    Public Property NotifyPartnerOfCancellation As Boolean


    Public Property ShowPhotosToNonMembers As Boolean
    Public Property ShowArticlesToNonMembers As Boolean
    Public Property ShowWallToNonMembers As Boolean
    Public Property LeagueRankMaxes As List(Of Integer)
    Public Property LinkedRegionIds As List(Of String)

    Public Property PhotoId As Guid
    Public Property HasPhotoId As Boolean
    Public Property StatLinks As String = ""

    Public Sub New()

    End Sub

    Public Sub New(RegionId As String)
        Me.RegionId = RegionId
    End Sub

    
    Public Shared CrewTypes As List(Of String) = { "official", "scorekeeper", "supervisor" }.ToList()

    Public Shared Function CrewTypeToPlural(CrewType As String) As String
        CrewType = CrewType.ToLower
        If CrewType = "official" OrElse CrewType = "officials" OrElse CrewType = "umpire" OrElse CrewType = "umpires" Then
            Return "Officials"
        ElseIf CrewType = "scorekeeper" OrElse CrewType = "scorekeepers" Then
            Return "Scorekeepers"
        ElseIf CrewType = "supervisor" OrElse CrewType = "supervisors" Then
            Return "Supervisors"
        End If
        Return ""
    End Function

    Public Function HasCrewType(CrewType As string) As Boolean
        CrewType = CrewType.ToLower
        If CrewType = "official" OrElse CrewType = "officials" OrElse CrewType = "umpire" OrElse CrewType = "umpires" Then
            Return HasOfficials
        ElseIf CrewType = "scorekeeper" OrElse CrewType = "scorekeepers" Then
            Return HasScorekeepers
        ElseIf CrewType = "supervisor" OrElse CrewType = "supervisors" Then
            Return HasSupervisors
        End If
        Return False
    End Function

    Public Shared Function IsValidRank(Dic As Dictionary(Of String, String), CrewType As String) As Boolean
        CrewType = CrewType.ToLower
        If Dic Is Nothing Then Return False
        If Not Dic.ContainsKey(CrewType) Then Return False
        Dim Rank = Dic(CrewType).Trim.ToLower
        If Rank = "" Then Return False
        Return RegionUser.RankCode.Contains(rank)
    End Function

    Public Function RankToText(Rank As String, L As string) As String
        Rank = Rank.ToLower
        Dim IsEnglish = L.ToLower.Contains("en")
        If Rank = "senior" Then
            Return IIf(IsEnglish, SeniorRankNameEnglish, SeniorRankNameFrench)
        Elseif Rank = "junior" Then
            Return IIf(IsEnglish, JuniorRankNameEnglish, JuniorRankNameFrench)
        Elseif Rank = "intermediate" Then
            Return IIf(IsEnglish, IntermediateRankNameEnglish, IntermediateRankNameFrench)
        ElseIf Rank = "novice" Then
            Return IIf(IsEnglish, NoviceRankNameEnglish, NoviceRankNameFrench)
        Elseif Rank = "rookie" Then
            Return IIf(IsEnglish, RookieRankNameEnglish, RookieRankNameFrench)
        End If
        Return ""
    End Function

    Private Function RankTextToRankEnglish(RankText As String)
        If RankText = PublicCode.RemoveDiacritics(SeniorRankNameEnglish).ToLower Then
            Return "senior"
        else if RankText = PublicCode.RemoveDiacritics(JuniorRankNameEnglish).ToLower Then
            Return "junior"
        else if RankText = PublicCode.RemoveDiacritics(IntermediateRankNameEnglish).ToLower Then
            Return "intermediate"
        else if RankText = PublicCode.RemoveDiacritics(NoviceRankNameEnglish).ToLower Then
            Return "novice"
        else if RankText = PublicCode.RemoveDiacritics(RookieRankNameEnglish).ToLower Then
            Return "rookie"
        End If
        return ""
    End Function

    Private Function RankTextToRankFrench(RankText As String)
        If RankText = PublicCode.RemoveDiacritics(SeniorRankNameFrench).ToLower Then
            Return "senior"
        else if RankText = PublicCode.RemoveDiacritics(JuniorRankNameFrench).ToLower Then
            Return "junior"
        else if RankText = PublicCode.RemoveDiacritics(IntermediateRankNameFrench).ToLower Then
            Return "intermediate"
        else if RankText = PublicCode.RemoveDiacritics(NoviceRankNameFrench).ToLower Then
            Return "novice"
        else if RankText = PublicCode.RemoveDiacritics(RookieRankNameFrench).ToLower Then
            Return "rookie"
        End If
        return ""
    End Function

    Public Function RankTextToRank(RankText As String, L As String) as String
        RankText = PublicCode.RemoveDiacritics(RankText).ToLower
        if L.ToLower().Contains("en") Then
            Dim NewRankText = RankTextToRankEnglish(RankText)
            If RankText <> "" Then
                Return NewRankText
            End If
            Return RankTextToRankFrench(RankText)
        Else
            Dim NewRankText = RankTextToRankFrench(RankText)
            If RankText <> "" Then
                Return NewRankText
            End If
            Return RankTextToRankEnglish(RankText)
        End If
    End Function


    Public Shared Function ToRegionIds(Regions As List(Of RegionProperties)) As List(Of String)
        Dim Result As New List(Of String)
        For Each Region In Regions
            Result.Add(Region.RegionId)
        Next
        Return Result
    End Function

    Public Shared Function BasicComparer(V1 As RegionProperties, V2 As RegionProperties) As Integer
        Return V1.RegionId.CompareTo(V2.RegionId)
    End Function

    Public Shared BasicSorter As New GenericIComparer(Of RegionProperties)(AddressOf BasicComparer)

    Public Function RequiresPayment() As Boolean
        If Me.HasPaid Then Return False
        Return Date.UtcNow > Me.RegistrationDate.AddMonths(2)
    End Function

    Public Shared Function CreateParamStr(VariableName As String, Variables As List(Of RegionProperties)) As String
        Dim SB As New StringBuilder()
        For I As Integer = 0 To Variables.Count - 1
            If I <> 0 Then SB.Append(", ")
            SB.Append("@" & VariableName & I)
        Next
        Return SB.ToString()
    End Function

    Public Shared Sub CreateParamsSQLCommand(VariableName As String, Variables As List(Of RegionProperties), SQLCommand As SqlCommand)
        For I As Integer = 0 To Variables.Count - 1
            SQLCommand.Parameters.Add(New SqlParameter(VariableName & I, Variables(I).RegionId))
        Next
    End Sub

    Public Function ConvertToRegion() As Region
        Dim Region As New Region

        Region.RegionId = RegionId
        Region.RegionName = RegionName
        Region.Sport = Sport
        Region.EntityType = EntityType
        Region.Season = Season
        Region.RegistrationDate = RegistrationDate
        Region.DefaultCrew = DefaultCrew
        Region.ShowAddress = ShowAddress
        Region.ShowRank = ShowRank
        Region.ShowSubRank = ShowSubRank
        Region.ShowRankToNonMembers = ShowRankToNonMembers
        Region.ShowSubRankToNonMembers = ShowSubRankToNonMembers
        Region.DefaultContactListSort = DefaultContactListSort
        Region.ShowRankInSchedule = ShowRankInSchedule
        Region.ShowSubRankInSchedule = ShowSubRankInSchedule
        Region.MaxSubRankRequest = MaxSubRankRequest
        Region.SubRankIsNumber = SubRankIsNumber
        Region.ShowTeamsInSchedule = ShowTeamsInSchedule
        Region.SortSubRankDesc = SortSubRankDesc
        Region.SeniorRankNameEnglish = SeniorRankNameEnglish
        Region.SeniorRankNameFrench = SeniorRankNameFrench
        Region.JuniorRankNameEnglish = JuniorRankNameEnglish
        Region.JuniorRankNameFrench = JuniorRankNameFrench
        Region.IntermediateRankNameEnglish = IntermediateRankNameEnglish
        Region.IntermediateRankNameFrench = IntermediateRankNameFrench
        Region.NoviceRankNameEnglish = NoviceRankNameEnglish
        Region.NoviceRankNameFrench = NoviceRankNameFrench
        Region.RookieRankNameEnglish = RookieRankNameEnglish
        Region.RookieRankNameFrench = RookieRankNameFrench
        Region.AllowBookOffs = AllowBookOffs
        Region.BookOffHoursBefore = BookOffHoursBefore
        Region.LastExecutedCronJob = LastExecutedCronJob
        Region.HasPaid = HasPaid
        Region.PaymentData = PaymentData
        Region.MaxDateShowAvailableSpots = MaxDateShowAvailableSpots
        Region.EmailAvailableSpots = EmailAvailableSpots
        Region.EmailRequestedGames = EmailRequestedGames
        Region.DefaultArriveBeforeMins = DefaultArriveBeforeMins
        Region.DefaultMaxGameLengthMins = DefaultMaxGameLengthMins
        Region.OnlyAllowedToConfirmDaysBefore = OnlyAllowedToConfirmDaysBefore
        Region.EmailConfirmDaysBefore = EmailConfirmDaysBefore
        Region.HasOfficials = HasOfficials
        Region.HasScorekeepers = HasScorekeepers
        Region.HasSupervisors = HasSupervisors
        Region.IsDemo = IsDemo
        Region.Country = Country
        Region.State = State
        Region.City = City
        Region.Address = Address
        Region.PostalCode = PostalCode
        Region.ShowLinksToNonMembers = ShowLinksToNonMembers
        Region.ShowParksToNonMembers = ShowParksToNonMembers
        Region.ShowLeaguesToNonMembers = ShowLeaguesToNonMembers
        Region.ShowOfficialRegionsToNonMembers = ShowOfficialRegionsToNonMembers
        Region.ShowTeamsToNonMembers = ShowTeamsToNonMembers
        Region.ShowHolidayListToNonMembers = ShowHolidayListToNonMembers
        Region.ShowContactListToNonMembers = ShowContactListToNonMembers
        Region.ShowAvailabilityDueDateToNonMembers = ShowAvailabilityDueDateToNonMembers
        Region.ShowAvailabilityToNonMembers = ShowAvailabilityToNonMembers
        Region.ShowAvailableSpotsToNonMembers = ShowAvailableSpotsToNonMembers
        Region.ShowFullScheduleToNonMembers = ShowFullScheduleToNonMembers
        Region.ShowStandingsToNonMembers = ShowStandingsToNonMembers
        Region.ShowStatsToNonMembers = ShowStatsToNonMembers
        Region.ShowMainPageToNonMembers = ShowMainPageToNonMembers
        Region.HasEnteredMainPage = HasEnteredMainPage
        Region.ExtraScoreParameters = ExtraScoreParameters
        Region.HomeTeamCanEnterScore = HomeTeamCanEnterScore
        Region.AwayTeamCanEnterScore = AwayTeamCanEnterScore
        Region.ScorekeeperCanEnterScore = ScorekeeperCanEnterScore
        Region.TeamCanEnterStats = TeamCanEnterStats
        Region.ScorekeeperCanEnterStats = ScorekeeperCanEnterStats
        Region.RegionIsLadderLeague  = RegionIsLadderLeague 
        Region.NumberOfPlayersInLadderLeague  = NumberOfPlayersInLadderLeague 
        Region.HomeTeamCanCancelGameHoursBefore = HomeTeamCanCancelGameHoursBefore
        Region.AwayTeamCanCancelGameHoursBefore = AwayTeamCanCancelGameHoursBefore
        Region.MinimumValue = MinimumValue
        Region.MinimumPercentage = MinimumPercentage
        Region.UniquePlayers = UniquePlayers
        Region.TimeZone = TimeZone
        Region.AutoSyncCancellationHoursBefore = AutoSyncCancellationHoursBefore
        Region.AutoSyncSchedule = AutoSyncSchedule
        Region.StatIndex = StatIndex
        Region.StandingIndex = StandingIndex

        Region.DefaultArriveBeforeAwayMins = DefaultArriveBeforeAwayMins
        Region.DefaultArriveBeforePracticeMins = DefaultArriveBeforePracticeMins
        Region.DefaultMaxGameLengthPracticeMins = DefaultMaxGameLengthPracticeMins

        Region.IncludeAfternoonAvailabilityOnWeekdays = IncludeAfternoonAvailabilityOnWeekdays
        Region.IncludeMorningAvailabilityOnWeekdays = IncludeMorningAvailabilityOnWeekdays
        Region.EnableNotFilledInInAvailability = EnableNotFilledInInAvailability

        Region.EnableDualAvailability = EnableDualAvailability
        Region.IsAvailableText  = IsAvailableText
        Region.IsAvailableDualText  = IsAvailableDualText
        Region.IsAvailableCombinedText  = IsAvailableCombinedText

        Region.ShowOnlyDueDatesRangeForAvailability = ShowOnlyDueDatesRangeForAvailability

        Region.ShowRankInGlobalAvailability = ShowRankInGlobalAvailability 
        Region.ShowSubRankInGlobalAvailability = ShowSubRankInGlobalAvailability
        Region.SortGlobalAvailabilityByRank = SortGlobalAvailabilityByRank
        Region.SortGlobalAvailabilityBySubRank = SortGlobalAvailabilityBySubRank
        Region.NotifyPartnerOfCancellation = NotifyPartnerOfCancellation

        Region.ShowPhotosToNonMembers = ShowPhotosToNonMembers
        Region.ShowArticlesToNonMembers = ShowArticlesToNonMembers
        Region.ShowWallToNonMembers = ShowWallToNonMembers

        Region.LeagueRankMaxes = LeagueRankMaxes
        Region.LinkedRegionIds = LinkedRegionIds

        Region.PhotoId = PhotoId
        Region.HasPhotoId = HasPhotoId

        Region.StatLinks = StatLinks

        Return Region
    End Function

    Public Shared Function GetItem(Regions As List(Of RegionProperties), RegionId As String) As RegionProperties
        Return PublicCode.BinarySearchItem(Regions, New RegionProperties With {.RegionId = RegionId}, RegionProperties.BasicSorter)
    End Function

    Public Sub New(Region As Region)
        RegionId = Region.RegionId
        RegionName = Region.RegionName
        Sport = Region.Sport
        EntityType = Region.EntityType
        Season = Region.Season
        RegistrationDate = Region.RegistrationDate
        DefaultCrew = Region.DefaultCrew
        ShowAddress = Region.ShowAddress
        ShowRank = Region.ShowRank
        ShowSubRank  = Region.ShowSubRank
        ShowRankToNonMembers = Region.ShowRankToNonMembers
        ShowSubRankToNonMembers = Region.ShowSubRankToNonMembers
        DefaultContactListSort = Region.DefaultContactListSort
        ShowRankInSchedule = Region.ShowRankInSchedule
        ShowSubRankInSchedule = Region.ShowSubRankInSchedule
        MaxSubRankRequest = Region.MaxSubRankRequest
        SubRankIsNumber = Region.SubRankIsNumber
        ShowTeamsInSchedule = Region.ShowTeamsInSchedule
        SortSubRankDesc = Region.SortSubRankDesc
        SeniorRankNameEnglish = Region.SeniorRankNameEnglish
        SeniorRankNameFrench = Region.SeniorRankNameFrench
        JuniorRankNameEnglish = Region.JuniorRankNameEnglish
        JuniorRankNameFrench = Region.JuniorRankNameFrench
        IntermediateRankNameEnglish = Region.IntermediateRankNameEnglish
        IntermediateRankNameFrench = Region.IntermediateRankNameFrench
        NoviceRankNameEnglish = Region.NoviceRankNameEnglish
        NoviceRankNameFrench = Region.NoviceRankNameFrench
        RookieRankNameEnglish = Region.RookieRankNameEnglish
        RookieRankNameFrench = Region.RookieRankNameFrench
        AllowBookOffs = Region.AllowBookOffs
        BookOffHoursBefore = Region.BookOffHoursBefore
        LastExecutedCronJob = Region.LastExecutedCronJob
        HasPaid = Region.HasPaid
        PaymentData = Region.PaymentData
        MaxDateShowAvailableSpots = Region.MaxDateShowAvailableSpots
        EmailAvailableSpots = Region.EmailAvailableSpots
        EmailRequestedGames = Region.EmailRequestedGames
        DefaultArriveBeforeMins = Region.DefaultArriveBeforeMins
        DefaultMaxGameLengthMins = Region.DefaultMaxGameLengthMins
        OnlyAllowedToConfirmDaysBefore = Region.OnlyAllowedToConfirmDaysBefore
        EmailConfirmDaysBefore = Region.EmailConfirmDaysBefore
        HasOfficials = Region.HasOfficials
        HasScorekeepers = Region.HasScorekeepers
        HasSupervisors = Region.HasSupervisors
        IsDemo = Region.IsDemo
        Country = Region.Country
        State = Region.State
        City = Region.City
        Address = Region.Address
        PostalCode = Region.PostalCode
        ShowLinksToNonMembers = Region.ShowLinksToNonMembers
        ShowParksToNonMembers = Region.ShowParksToNonMembers
        ShowLeaguesToNonMembers = Region.ShowLeaguesToNonMembers
        ShowOfficialRegionsToNonMembers = Region.ShowOfficialRegionsToNonMembers
        ShowTeamsToNonMembers = Region.ShowTeamsToNonMembers
        ShowHolidayListToNonMembers = Region.ShowHolidayListToNonMembers
        ShowContactListToNonMembers = Region.ShowContactListToNonMembers
        ShowAvailabilityDueDateToNonMembers = Region.ShowAvailabilityDueDateToNonMembers
        ShowAvailabilityToNonMembers = Region.ShowAvailabilityToNonMembers
        ShowAvailableSpotsToNonMembers = Region.ShowAvailableSpotsToNonMembers
        ShowFullScheduleToNonMembers = Region.ShowFullScheduleToNonMembers
        ShowStandingsToNonMembers = Region.ShowStandingsToNonMembers
        ShowStatsToNonMembers = Region.ShowStatsToNonMembers
        ShowMainPageToNonMembers = Region.ShowMainPageToNonMembers
        HasEnteredMainPage = Region.HasEnteredMainPage
        ExtraScoreParameters = Region.ExtraScoreParameters
        HomeTeamCanEnterScore = Region.HomeTeamCanEnterScore
        AwayTeamCanEnterScore = Region.AwayTeamCanEnterScore
        ScorekeeperCanEnterScore = Region.ScorekeeperCanEnterScore
        TeamCanEnterStats = Region.TeamCanEnterStats
        ScorekeeperCanEnterStats = Region.ScorekeeperCanEnterStats
        RegionIsLadderLeague = Region.RegionIsLadderLeague
        NumberOfPlayersInLadderLeague = Region.NumberOfPlayersInLadderLeague
        HomeTeamCanCancelGameHoursBefore = Region.HomeTeamCanCancelGameHoursBefore
        AwayTeamCanCancelGameHoursBefore = Region.AwayTeamCanCancelGameHoursBefore
        MinimumValue = Region.MinimumValue
        MinimumPercentage = Region.MinimumPercentage
        UniquePlayers = Region.UniquePlayers
        TimeZone = Region.TimeZone
        AutoSyncCancellationHoursBefore = Region.AutoSyncCancellationHoursBefore
        AutoSyncSchedule = Region.AutoSyncSchedule
        StatIndex = Region.StatIndex
        StandingIndex = Region.StandingIndex

        DefaultArriveBeforeAwayMins = Region.DefaultArriveBeforeAwayMins
        DefaultArriveBeforePracticeMins = Region.DefaultArriveBeforePracticeMins
        DefaultMaxGameLengthPracticeMins = Region.DefaultMaxGameLengthPracticeMins

        IncludeAfternoonAvailabilityOnWeekdays = Region.IncludeAfternoonAvailabilityOnWeekdays
        IncludeMorningAvailabilityOnWeekdays = Region.IncludeMorningAvailabilityOnWeekdays
        EnableNotFilledInInAvailability = Region.EnableNotFilledInInAvailability

        EnableDualAvailability = Region.EnableDualAvailability
        IsAvailableText = Region.IsAvailableText
        IsAvailableDualText = Region.IsAvailableDualText
        IsAvailableCombinedText = Region.IsAvailableCombinedText

        ShowOnlyDueDatesRangeForAvailability = Region.ShowOnlyDueDatesRangeForAvailability

        ShowRankInGlobalAvailability = Region.ShowRankInGlobalAvailability
        ShowSubRankInGlobalAvailability = Region.ShowSubRankInGlobalAvailability
        SortGlobalAvailabilityByRank = Region.SortGlobalAvailabilityByRank
        SortGlobalAvailabilityBySubRank = Region.SortGlobalAvailabilityBySubRank
        NotifyPartnerOfCancellation = Region.NotifyPartnerOfCancellation

        ShowPhotosToNonMembers = Region.ShowPhotosToNonMembers
        ShowArticlesToNonMembers = Region.ShowArticlesToNonMembers
        ShowWallToNonMembers = Region.ShowWallToNonMembers

        LeagueRankMaxes = Region.LeagueRankMaxes
        LinkedRegionIds = Region.LinkedRegionIds

        PhotoId = Region.PhotoId
        HasPhotoId = Region.HasPhotoId

        StatLinks = Region.StatLinks
    End Sub

    Public Shared Function ReadRegionProperties(Reader As SQLReaderIncrementor) As RegionProperties
        Dim Result As New RegionProperties With {
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
            .LastExecutedCronJob = Reader.GetDateTime(),
            .HasPaid = Reader.GetBoolean(),
            .PaymentData = Reader.GetString(),
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
            .StatLinks = Reader.GetString()
        }

        Return Result
    End Function

    Public Shared Sub BulkDelete(Regions As List(Of RegionProperties), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        If Regions.Count = 0 Then Return

        Dim ParamsSB As New StringBuilder()
        For I As Integer = 0 To Regions.Count - 1
            If I <> 0 Then ParamsSB.Append(", ")
            ParamsSB.Append("@RegionId" & I)
        Next

        Dim CommandText As String = "DELETE FROM [Region] WHERE RegionId IN ({0})".Replace("{0}", ParamsSB.ToString())
        Using SqlCommand = New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 0 To Regions.Count - 1
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, Regions(I).RegionId))
            Next
            SqlCommand.ExecuteNonQuery()
        End Using
    End Sub

    Public Shared Sub BulkInsert(Regions As List(Of RegionProperties), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        If Regions.Count = 0 Then Return

        Dim RegionTable As New DataTable("Region")
        RegionTable.Columns.Add(New DataColumn("RegionID", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("RegionName", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("Sport", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("Season", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("RegistrationDate", GetType(DateTime)))
        RegionTable.Columns.Add(New DataColumn("DefaultCrew", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("ShowAddress", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowRank", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowSubRank", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowRankToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowSubRankToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("DefaultContactListSort", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("ShowRankInSchedule", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowSubRankInSchedule", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("MaxSubRankRequest", GetType(Decimal)))
        RegionTable.Columns.Add(New DataColumn("SubRankIsNumber", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowTeamsInSchedule", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("SortSubRankDesc", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("SeniorRankNameEnglish", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("SeniorRankNameFrench", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("JuniorRankNameEnglish", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("JuniorRankNameFrench", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("IntermediateRankNameEnglish", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("IntermediateRankNameFrench", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("NoviceRankNameEnglish", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("NoviceRankNameFrench", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("RookieRankNameEnglish", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("RookieRankNameFrench", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("AllowBookOffs", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("BookOffHoursBefore", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("HasPaid", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("PaymentData", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("LastExecutedCronJob", GetType(DateTime)))
        RegionTable.Columns.Add(New DataColumn("MaxDateShowAvailableSpots", GetType(DateTime)))
        RegionTable.Columns.Add(New DataColumn("EmailAvailableSpots", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("EmailRequestedGames", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("DefaultArriveBeforeMins", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("DefaultMaxGameLengthMins", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("HasOfficials", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("HasScorekeepers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("HasSupervisors", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("IsDemo", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("Country", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("State", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("City", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("Address", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("PostalCode", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("ShowLinksToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowParksToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowLeaguesToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowHolidayListToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowContactListToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowAvailabilityToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowAvailableSpotsToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowFullScheduleToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowAvailabilityDueDateToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("EntityType", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("ShowOfficialRegionsToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowTeamsToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("OnlyAllowedToConfirmDaysBefore", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("ShowStandingsToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowStatsToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ExtraScoreParameters", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("HomeTeamCanEnterScore", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ScorekeeperCanEnterScore", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("TeamCanEnterStats", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ScorekeeperCanEnterStats", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("RegionIsLadderLeague", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("NumberOfPlayersInLadderLeague", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("AwayTeamCanEnterScore", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("HomeTeamCanCancelGameHoursBefore", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("AwayTeamCanCancelGameHoursBefore", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("StatIndex", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("MinimumValue", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("MinimumPercentage", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("UniquePlayers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("StandingIndex", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("TimeZone", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("AutoSyncCancellationHoursBefore", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("AutoSyncSchedule", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("DefaultArriveBeforeAwayMins", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("DefaultArriveBeforePracticeMins", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("DefaultMaxGameLengthPracticeMins", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("ShowMainPageToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("HasEnteredMainPage", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("EmailConfirmDaysBefore", GetType(Int32)))
        RegionTable.Columns.Add(New DataColumn("IncludeAfternoonAvailabilityOnWeekdays", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("IncludeMorningAvailabilityOnWeekdays", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("EnableNotFilledInInAvailability", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowOnlyDueDatesRangeForAvailability", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowRankInGlobalAvailability", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowSubRankInGlobalAvailability", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("SortGlobalAvailabilityByRank", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("SortGlobalAvailabilityBySubRank", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("NotifyPartnerOfCancellation", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("PhotoId", GetType(Guid)))
        RegionTable.Columns.Add(New DataColumn("HasPhotoId", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowPhotosToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowArticlesToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("ShowWallToNonMembers", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("StatLinks", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("LeagueRankMaxes", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("LinkedRegionIds", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("EnableDualAvailability ", GetType(Boolean)))
        RegionTable.Columns.Add(New DataColumn("IsAvailableText", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("IsAvailableDualText", GetType(String)))
        RegionTable.Columns.Add(New DataColumn("IsAvailableCombinedText", GetType(String)))


        For Each Region In Regions
            Dim Row = RegionTable.NewRow()
            Row("RegionID") = Region.RegionId
            Row("RegionName") = Region.RegionName
            Row("Sport") = Region.Sport
            Row("Season") = Region.Season
            Row("RegistrationDate") = Region.RegistrationDate
            Row("DefaultCrew") = JsonConvert.SerializeObject(Region.DefaultCrew)
            Row("ShowAddress") = Region.ShowAddress
            Row("ShowRank") = Region.ShowRank
            Row("ShowSubRank") = Region.ShowSubRank
            Row("ShowRankToNonMembers") = Region.ShowRankToNonMembers
            Row("ShowSubRankToNonMembers") = Region.ShowSubRankToNonMembers
            Row("DefaultContactListSort") = Region.DefaultContactListSort
            Row("ShowRankInSchedule") = Region.ShowRankInSchedule
            Row("ShowSubRankInSchedule") = Region.ShowSubRankInSchedule
            Row("MaxSubRankRequest") = Region.MaxSubRankRequest
            Row("SubRankIsNumber") = Region.SubRankIsNumber
            Row("ShowTeamsInSchedule") = Region.ShowTeamsInSchedule
            Row("SortSubRankDesc") = Region.SortSubRankDesc
            Row("SeniorRankNameEnglish") = Region.SeniorRankNameEnglish
            Row("SeniorRankNameFrench") = Region.SeniorRankNameFrench
            Row("JuniorRankNameEnglish") = Region.JuniorRankNameEnglish
            Row("JuniorRankNameFrench") = Region.JuniorRankNameFrench
            Row("IntermediateRankNameEnglish") = Region.IntermediateRankNameEnglish
            Row("IntermediateRankNameFrench") = Region.IntermediateRankNameFrench
            Row("NoviceRankNameEnglish") = Region.NoviceRankNameEnglish
            Row("NoviceRankNameFrench") = Region.NoviceRankNameFrench
            Row("RookieRankNameEnglish") = Region.RookieRankNameEnglish
            Row("RookieRankNameFrench") = Region.RookieRankNameFrench
            
            Row("AllowBookOffs") = Region.AllowBookOffs
            Row("BookOffHoursBefore") = Region.BookOffHoursBefore
            Row("HasPaid") = Region.HasPaid
            Row("PaymentData") = Region.PaymentData
            Row("LastExecutedCronJob") = Region.LastExecutedCronJob
            Row("MaxDateShowAvailableSpots") = Region.MaxDateShowAvailableSpots
            Row("EmailAvailableSpots") = Region.EmailAvailableSpots
            Row("EmailRequestedGames") = Region.EmailRequestedGames
            Row("DefaultArriveBeforeMins") = Region.DefaultArriveBeforeMins
            Row("DefaultMaxGameLengthMins") = Region.DefaultMaxGameLengthMins
            Row("HasOfficials") = Region.HasOfficials
            Row("HasScorekeepers") = Region.HasScorekeepers
            Row("HasSupervisors") = Region.HasSupervisors
            Row("IsDemo") = Region.IsDemo
            Row("Country") = Region.Country
            Row("State") = Region.State
            Row("City") = Region.City
            Row("Address") = Region.Address
            Row("PostalCode") = Region.PostalCode
            Row("ShowLinksToNonMembers") = Region.ShowLinksToNonMembers
            Row("ShowParksToNonMembers") = Region.ShowParksToNonMembers
            Row("ShowLeaguesToNonMembers") = Region.ShowLeaguesToNonMembers
            Row("ShowHolidayListToNonMembers") = Region.ShowHolidayListToNonMembers
            Row("ShowContactListToNonMembers") = Region.ShowContactListToNonMembers
            Row("ShowAvailabilityToNonMembers") = Region.ShowAvailabilityToNonMembers
            Row("ShowAvailableSpotsToNonMembers") = Region.ShowAvailableSpotsToNonMembers
            Row("ShowFullScheduleToNonMembers") = Region.ShowFullScheduleToNonMembers
            Row("ShowAvailabilityDueDateToNonMembers") = Region.ShowAvailabilityDueDateToNonMembers
            Row("EntityType") = Region.EntityType
            Row("ShowOfficialRegionsToNonMembers") = Region.ShowOfficialRegionsToNonMembers
            Row("ShowTeamsToNonMembers") = Region.ShowTeamsToNonMembers
            Row("OnlyAllowedToConfirmDaysBefore") = Region.OnlyAllowedToConfirmDaysBefore
            Row("ShowStandingsToNonMembers") = Region.ShowStandingsToNonMembers
            Row("ShowStatsToNonMembers") = Region.ShowStatsToNonMembers
            Row("ExtraScoreParameters") = JsonConvert.SerializeObject(Region.ExtraScoreParameters)
            Row("HomeTeamCanEnterScore") = Region.HomeTeamCanEnterScore
            Row("ScorekeeperCanEnterScore") = Region.ScorekeeperCanEnterScore
            Row("TeamCanEnterStats") = Region.TeamCanEnterStats
            Row("ScorekeeperCanEnterStats") = Region.ScorekeeperCanEnterStats
            Row("RegionIsLadderLeague") = Region.RegionIsLadderLeague
            Row("NumberOfPlayersInLadderLeague") = Region.NumberOfPlayersInLadderLeague
            Row("AwayTeamCanEnterScore") = Region.AwayTeamCanEnterScore
            Row("HomeTeamCanCancelGameHoursBefore") = Region.HomeTeamCanCancelGameHoursBefore
            Row("AwayTeamCanCancelGameHoursBefore") = Region.AwayTeamCanCancelGameHoursBefore
            Row("StatIndex") = Region.StatIndex
            Row("MinimumValue") = JsonConvert.SerializeObject(Region.MinimumValue)
            Row("MinimumPercentage") = JsonConvert.SerializeObject(Region.MinimumPercentage)
            Row("UniquePlayers") = Region.UniquePlayers
            Row("StandingIndex") = Region.StandingIndex
            Row("TimeZone") = Region.TimeZone
            Row("AutoSyncCancellationHoursBefore") = Region.AutoSyncCancellationHoursBefore
            Row("AutoSyncSchedule") = Region.AutoSyncSchedule
            Row("DefaultArriveBeforeAwayMins") = Region.DefaultArriveBeforeAwayMins
            Row("DefaultArriveBeforePracticeMins") = Region.DefaultArriveBeforePracticeMins
            Row("DefaultMaxGameLengthPracticeMins") = Region.DefaultMaxGameLengthPracticeMins
            Row("ShowMainPageToNonMembers") = Region.ShowMainPageToNonMembers
            Row("HasEnteredMainPage") = Region.HasEnteredMainPage
            Row("EmailConfirmDaysBefore") = Region.EmailConfirmDaysBefore
            Row("IncludeAfternoonAvailabilityOnWeekdays") = Region.IncludeAfternoonAvailabilityOnWeekdays
            Row("IncludeMorningAvailabilityOnWeekdays") = Region.IncludeMorningAvailabilityOnWeekdays
            Row("EnableNotFilledInInAvailability") = Region.EnableNotFilledInInAvailability
            Row("EnableDualAvailability") = Region.EnableDualAvailability
            Row("IsAvailableText") = JsonConvert.SerializeObject(Region.IsAvailableText)
            Row("IsAvailableDualText") = JsonConvert.SerializeObject(Region.IsAvailableDualText)
            Row("IsAvailableCombinedText") = JsonConvert.SerializeObject(Region.IsAvailableCombinedText)
            Row("ShowOnlyDueDatesRangeForAvailability") = Region.ShowOnlyDueDatesRangeForAvailability
            Row("ShowRankInGlobalAvailability") = Region.ShowRankInGlobalAvailability
            Row("ShowSubRankInGlobalAvailability") = Region.ShowSubRankInGlobalAvailability
            Row("SortGlobalAvailabilityByRank") = Region.SortGlobalAvailabilityByRank
            Row("SortGlobalAvailabilityBySubRank") = Region.SortGlobalAvailabilityBySubRank
            Row("NotifyPartnerOfCancellation") = Region.NotifyPartnerOfCancellation
            Row("PhotoId") = Region.PhotoId
            Row("HasPhotoId") = Region.HasPhotoId
            Row("ShowPhotosToNonMembers") = Region.ShowPhotosToNonMembers
            Row("ShowArticlesToNonMembers") = Region.ShowArticlesToNonMembers
            Row("ShowWallToNonMembers") = Region.ShowWallToNonMembers
            Row("StatLinks") = Region.StatLinks
            Row("LeagueRankMaxes") = JsonConvert.SerializeObject(Region.LeagueRankMaxes)
            Row("LinkedRegionIds") = JsonConvert.SerializeObject(Region.LinkedRegionIds)

            RegionTable.Rows.Add(Row)
        Next

        Using BulkCopy As New SqlBulkCopy(SQLConnection, SqlBulkCopyOptions.Default, SQLTransaction)
            BulkCopy.DestinationTableName = "Region"
            BulkCopy.BulkCopyTimeout = 10000
            BulkCopy.WriteToServer(RegionTable)
        End Using
    End Sub

    Public Shared Function GetAllRegionPropertiesHelper(SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionProperties)
        Dim Result As New List(Of RegionProperties)

        Dim CommandText As String = Region.SelectStr
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(ReadRegionProperties(Reader))
            End While
            Reader.Close()
        End Using
        Return Result
    End Function

    Public Shared Function GetRegionProperties(Username As String, RegionId As String) As Object
        Dim RegionProperties As RegionProperties = Nothing

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, Username, SqlConnection, SqlTransaction)

                    RegionProperties = GetRegionPropertiesHelper(RegionId, SqlConnection, SqlTransaction, Assignor Is Nothing OrElse Not Assignor.IsExecutive())

                    SqlTransaction.Commit()
                End Using
            End Using

            Return New With {
                .Success = True,
                .RegionProperties = RegionProperties
            }
        Catch E As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function GetRegionPropertiesHelper(RegionId As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction, Optional HidePaymentData As Boolean = True) As RegionProperties
        Dim Result As RegionProperties = Nothing

        Dim CommandText As String = Region.SelectStr & " WHERE RegionId = @RegionId"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result = ReadRegionProperties(Reader)
            End While

            If Result IsNot Nothing Then
                If HidePaymentData Then
                    Result.HasPaid = False
                    Result.PaymentData = ""
                End If
            End If

            Reader.Close()
        End Using
        Return Result
    End Function

    Public Shared Function GetRegionPropertiesRegionIdsHelper(RegionIds As List(Of String), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction, Optional HidePaymentData As Boolean = True) As List(Of RegionProperties)
        Dim RegionIdParams As New StringBuilder()
        Dim IsFirst As Boolean = True
        For I As Integer = 1 To RegionIds.Count
            If I <> 1 Then RegionIdParams.Append(", ")
            RegionIdParams.Append("@RegionId" & I)
        Next

        Dim Result As New List(Of RegionProperties)

        If RegionIds.Count = 0 Then Return Result

        Dim CommandText As String = Region.SelectStr & " WHERE RegionId IN ({0}) ORDER BY RegionId".Replace("{0}", RegionIdParams.ToString())
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 1 To RegionIds.Count
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, RegionIds(I - 1)))
            Next
            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(ReadRegionProperties(Reader))
            End While

            If HidePaymentData Then
                For Each RP In Result
                    RP.HasPaid = False
                    RP.PaymentData = ""
                Next
            End If

            Reader.Close()
        End Using
        Return Result
    End Function

    Public Shared Function GetRegionIdsFromRegions(Regions As List(Of RegionProperties)) As List(Of String)
        Dim RegionIds As New List(Of String)
        For Each Region In Regions
            RegionIds.Add(Region.RegionId)
        Next
        Return RegionIds
    End Function

    Public Shared Function GetRegionPropertiesLikeRegionIdHelper(RegionId As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionProperties)
        Dim Result As New List(Of RegionProperties)

        Dim CommandText As String = Region.SelectStr & " WHERE RegionId LIKE @RegionId ORDER BY RegionId"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)
            While Reader.Read
                Result.Add(ReadRegionProperties(Reader))
            End While
            Reader.Close()
        End Using
        Return Result
    End Function

    Public Shared Function GetAllMyRegionProperties(Username As String) As Object
        Dim RegionProperties As List(Of RegionProperties) = Nothing

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    RegionProperties = GetAllMyRegionPropertiesHelper(Username, SqlConnection, SqlTransaction)
                    SqlTransaction.Commit()
                End Using
            End Using

            Return New With {
                .Success = True,
                .RegionProperties = RegionProperties
            }
        Catch E As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function GetAllMyRegionPropertiesHelper(Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionProperties)
        Dim Result As New List(Of RegionProperties)

        Dim CommandText As String = Region.SelectStr & " WHERE RegionId IN (SELECT RegionId FROM RegionUser WHERE RealUsername = @Username AND IsArchived = 0) ORDER BY RegionId"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)

            While Reader.Read
                Result.Add(ReadRegionProperties(Reader))
            End While
            Reader.Close()
        End Using
        Return Result
    End Function

    Public Shared Function GetAllMyRegionPropertiesImExec(Username As String) As Object
        Dim RegionProperties As List(Of RegionProperties) = Nothing

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()
                    RegionProperties = GetAllMyRegionPropertiesImExecHelper(Username, SqlConnection, SqlTransaction)
                    SqlTransaction.Commit()
                End Using
            End Using

            Return New With {
                .Success = True,
                .RegionProperties = RegionProperties
            }
        Catch E As Exception
            Return New ErrorObject("UnknownError")
        End Try
    End Function

    Public Shared Function GetAllMyRegionPropertiesImExecHelper(Username As String, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of RegionProperties)
        Dim Result As New List(Of RegionProperties)

        Dim CommandText As String = Region.SelectStr & " WHERE RegionId IN (SELECT RegionId FROM RegionUser WHERE RealUsername = @Username AND IsExecutive = 1 AND IsArchived = 0) ORDER BY RegionId"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("Username", Username))
            Dim Reader = New SQLReaderIncrementor(SqlCommand.ExecuteReader)

            While Reader.Read
                Result.Add(ReadRegionProperties(Reader))
            End While
            Reader.Close()
        End Using
        Return Result
    End Function

    Public Shared Function Upsert(Username As String, RegionProperties As RegionProperties)
        Dim Parks As New List(Of Park)

        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()

                Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionProperties.RegionId, Username, SqlConnection, SqlTransaction)
                If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                    Return New ErrorObject("InvalidPermissions")
                End If

                Dim ValidationObject = RegionProperties.Validate()
                If ValidationObject IsNot Nothing Then
                    Return ValidationObject
                End If

                UpsertHelper(RegionProperties, SqlConnection, SqlTransaction)

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

    Public Shared Function UpdatePayment(RegionId As String)
        Dim Parks As New List(Of Park)

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim Region = UmpireAssignor.Region.GetRegionHelper(RegionId, SqlConnection, SqlTransaction)

                    If Region IsNot Nothing Then
                        Dim CommandText = "UPDATE Region SET HasPaid = @HasPaid, PaymentData = @PaymentData WHERE RegionId = @RegionId"
                        Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionId))
                            SqlCommand.Parameters.Add(New SqlParameter("HasPaid", True))
                            SqlCommand.Parameters.Add(New SqlParameter("PaymentData", ""))
                            SqlCommand.ExecuteNonQuery()
                        End Using
                    End If

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

    Public Shared Function SetEntityProfilePic(Username As String, SetEntityProfileObject As SetEntityProfileObject) As Object
        Dim Parks As New List(Of Park)

        Try
            Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
                SqlConnection.Open()
                Using SqlTransaction = SqlConnection.BeginTransaction()

                    Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(SetEntityProfileObject.RegionId, Username, SqlConnection, SqlTransaction)
                    If SetEntityProfileObject.EntityType = "regionuser" Then
                        Dim TRegionUser = RegionUser.LoadBasicFromUsername(SetEntityProfileObject.RegionId, SetEntityProfileObject.Id, SqlConnection, SqlTransaction)
                        If Not (TRegionUser.AllowInfoLink AndAlso TRegionUser.RealUsername = Username) Then
                            If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                                Return New ErrorObject("InvalidPermissions")
                            End If
                        End If
                    Else
                        If Assignor Is Nothing OrElse Not Assignor.IsExecutive Then
                            Return New ErrorObject("InvalidPermissions")
                        End If
                    End If

                    Dim TableName As String = ""
                    Dim EntityQuery As String = ""

                    If SetEntityProfileObject.EntityType = "regionuser" Then
                        TableName = "RegionUser"
                        EntityQuery = " AND Username = @Id"
                    ElseIf SetEntityProfileObject.EntityType = "region" Then
                        TableName = "Region"
                    ElseIf SetEntityProfileObject.EntityType = "league" Then
                        TableName = "RegionLeague"
                        EntityQuery = " AND LeagueId = @Id"
                    ElseIf SetEntityProfileObject.EntityType = "team" Then
                        TableName = "Team"
                        EntityQuery = " AND TeamId = @Id"
                    ElseIf SetEntityProfileObject.EntityType = "officialregion" Then
                        TableName = "OfficialRegion"
                        EntityQuery = " AND OfficialRegionId = @Id"
                    ElseIf SetEntityProfileObject.EntityType = "park" Then
                        TableName = "Park"
                        EntityQuery = " AND ParkId = @Id"
                    End If

                    Dim CommandText = "UPDATE {0} SET PhotoId = @PhotoId, HasPhotoId = @HasPhotoId WHERE RegionId = @RegionId{1}".Replace("{0}", TableName).Replace("{1}", EntityQuery)
                    Using SqlCommand As New SqlCommand(CommandText, SqlConnection, SqlTransaction)
                        SqlCommand.Parameters.Add(New SqlParameter("RegionId", SetEntityProfileObject.RegionId))
                        If SetEntityProfileObject.EntityType <> "region" Then
                            SqlCommand.Parameters.Add(New SqlParameter("Id", SetEntityProfileObject.Id))
                        End If
                        SqlCommand.Parameters.Add(New SqlParameter("PhotoId", SetEntityProfileObject.PhotoId))
                        SqlCommand.Parameters.Add(New SqlParameter("HasPhotoId", SetEntityProfileObject.HasPhotoId))

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

    Public Shared Sub UpsertHelper(RegionProperties As RegionProperties, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)

        Dim CommandText = "UPDATE Region SET RegionName = @RegionName, DefaultCrew = @DefaultCrew, ShowAddress = @ShowAddress, ShowRank = @ShowRank, ShowSubRank = @ShowSubRank, ShowRankToNonMembers = @ShowRankToNonMembers, ShowSubRankToNonMembers = @ShowSubRankToNonMembers, DefaultContactListSort = @DefaultContactListSort, ShowRankInSchedule = @ShowRankInSchedule, ShowSubRankInSchedule = @ShowSubRankInSchedule, MaxSubRankRequest = @MaxSubRankRequest, SubRankIsNumber = @SubRankIsNumber, ShowTeamsInSchedule = @ShowTeamsInSchedule, SortSubRankDesc = @SortSubRankDesc, SeniorRankNameEnglish = @SeniorRankNameEnglish, SeniorRankNameFrench = @SeniorRankNameFrench, JuniorRankNameEnglish = @JuniorRankNameEnglish, JuniorRankNameFrench = @JuniorRankNameFrench, IntermediateRankNameEnglish = @IntermediateRankNameEnglish, IntermediateRankNameFrench = @IntermediateRankNameFrench, NoviceRankNameEnglish = @NoviceRankNameEnglish, NoviceRankNameFrench = @NoviceRankNameFrench, RookieRankNameEnglish = @RookieRankNameEnglish, RookieRankNameFrench = @RookieRankNameFrench, AllowBookOffs = @AllowBookOffs, BookOffHoursBefore = @BookOffHoursBefore, MaxDateShowAvailableSpots = @MaxDateShowAvailableSpots, EmailAvailableSpots = @EmailAvailableSpots, EmailRequestedGames = @EmailRequestedGames, DefaultArriveBeforeMins = @DefaultArriveBeforeMins, DefaultMaxGameLengthMins = @DefaultMaxGameLengthMins, OnlyAllowedToConfirmDaysBefore = @OnlyAllowedToConfirmDaysBefore, EmailConfirmDaysBefore = @EmailConfirmDaysBefore, HasOfficials = @HasOfficials, HasScorekeepers = @HasScorekeepers, HasSupervisors = @HasSupervisors, Country = @Country, State = @State, City = @City, Address = @Address, PostalCode = @PostalCode, ShowLinksToNonMembers = @ShowLinksToNonMembers, ShowParksToNonMembers = @ShowParksToNonMembers, ShowLeaguesToNonMembers = @ShowLeaguesToNonMembers, ShowOfficialRegionsToNonMembers = @ShowOfficialRegionsToNonMembers, ShowTeamsToNonMembers = @ShowTeamsToNonMembers, ShowHolidayListToNonMembers = @ShowHolidayListToNonMembers, ShowContactListToNonMembers = @ShowContactListToNonMembers, ShowAvailabilityDueDateToNonMembers = @ShowAvailabilityDueDateToNonMembers, ShowAvailabilityToNonMembers = @ShowAvailabilityToNonMembers, ShowAvailableSpotsToNonMembers = @ShowAvailableSpotsToNonMembers, ShowFullScheduleToNonMembers = @ShowFullScheduleToNonMembers, ShowStandingsToNonMembers = @ShowStandingsToNonMembers, ShowStatsToNonMembers = @ShowStatsToNonMembers, ShowMainPageToNonMembers = @ShowMainPageToNonMembers, ExtraScoreParameters = @ExtraScoreParameters, HomeTeamCanEnterScore = @HomeTeamCanEnterScore, AwayTeamCanEnterScore = @AwayTeamCanEnterScore, ScorekeeperCanEnterScore = @ScorekeeperCanEnterScore, TeamCanEnterStats = @TeamCanEnterStats, ScorekeeperCanEnterStats = @ScorekeeperCanEnterStats, RegionIsLadderLeague  = @RegionIsLadderLeague , NumberOfPlayersInLadderLeague  = @NumberOfPlayersInLadderLeague , HomeTeamCanCancelGameHoursBefore = @HomeTeamCanCancelGameHoursBefore, AwayTeamCanCancelGameHoursBefore = @AwayTeamCanCancelGameHoursBefore, MinimumValue = @MinimumValue, MinimumPercentage = @MinimumPercentage, TimeZone = @TimeZone, AutoSyncCancellationHoursBefore = @AutoSyncCancellationHoursBefore, AutoSyncSchedule = @AutoSyncSchedule, UniquePlayers = @UniquePlayers, StandingIndex = @StandingIndex, DefaultArriveBeforeAwayMins = @DefaultArriveBeforeAwayMins, DefaultArriveBeforePracticeMins = @DefaultArriveBeforePracticeMins, DefaultMaxGameLengthPracticeMins = @DefaultMaxGameLengthPracticeMins, IncludeAfternoonAvailabilityOnWeekdays = @IncludeAfternoonAvailabilityOnWeekdays, IncludeMorningAvailabilityOnWeekdays = @IncludeMorningAvailabilityOnWeekdays, EnableNotFilledInInAvailability = @EnableNotFilledInInAvailability, EnableDualAvailability = @EnableDualAvailability, IsAvailableText = @IsAvailableText, IsAvailableDualText = @IsAvailableDualText, IsAvailableCombinedText = @IsAvailableCombinedText, ShowOnlyDueDatesRangeForAvailability = @ShowOnlyDueDatesRangeForAvailability, ShowRankInGlobalAvailability = @ShowRankInGlobalAvailability, ShowSubRankInGlobalAvailability = @ShowSubRankInGlobalAvailability, SortGlobalAvailabilityByRank = @SortGlobalAvailabilityByRank, SortGlobalAvailabilityBySubRank = @SortGlobalAvailabilityBySubRank, NotifyPartnerOfCancellation = @NotifyPartnerOfCancellation, ShowPhotosToNonMembers = @ShowPhotosToNonMembers, ShowArticlesToNonMembers = @ShowArticlesToNonMembers, ShowWallToNonMembers = @ShowWallToNonMembers, LeagueRankMaxes = @LeagueRankMaxes, LinkedRegionIds = @LinkedRegionIds WHERE RegionId = @RegionId"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", RegionProperties.RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("RegionName", RegionProperties.RegionName))
            SqlCommand.Parameters.Add(New SqlParameter("DefaultCrew", JsonConvert.SerializeObject(RegionProperties.DefaultCrew)))
            SqlCommand.Parameters.Add(New SqlParameter("ShowAddress", RegionProperties.ShowAddress))
            SqlCommand.Parameters.Add(New SqlParameter("ShowRank", RegionProperties.ShowRank))
            SqlCommand.Parameters.Add(New SqlParameter("ShowSubRank", RegionProperties.ShowSubRank))
            SqlCommand.Parameters.Add(New SqlParameter("ShowRankToNonMembers", RegionProperties.ShowRankToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowSubRankToNonMembers", RegionProperties.ShowSubRankToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("DefaultContactListSort", RegionProperties.DefaultContactListSort))
            SqlCommand.Parameters.Add(New SqlParameter("ShowRankInSchedule", RegionProperties.ShowRankInSchedule))
            SqlCommand.Parameters.Add(New SqlParameter("ShowSubRankInSchedule", RegionProperties.ShowSubRankInSchedule))

            SqlCommand.Parameters.Add(New SqlParameter("MaxSubRankRequest", RegionProperties.MaxSubRankRequest))
            SqlCommand.Parameters.Add(New SqlParameter("SubRankIsNumber", RegionProperties.SubRankIsNumber))
            SqlCommand.Parameters.Add(New SqlParameter("ShowTeamsInSchedule", RegionProperties.ShowTeamsInSchedule))
            SqlCommand.Parameters.Add(New SqlParameter("SortSubRankDesc", RegionProperties.SortSubRankDesc))

            SqlCommand.Parameters.Add(New SqlParameter("SeniorRankNameEnglish", RegionProperties.SeniorRankNameEnglish))
            SqlCommand.Parameters.Add(New SqlParameter("SeniorRankNameFrench", RegionProperties.SeniorRankNameFrench))
            SqlCommand.Parameters.Add(New SqlParameter("JuniorRankNameEnglish", RegionProperties.JuniorRankNameEnglish))
            SqlCommand.Parameters.Add(New SqlParameter("JuniorRankNameFrench", RegionProperties.JuniorRankNameFrench))
            SqlCommand.Parameters.Add(New SqlParameter("IntermediateRankNameEnglish", RegionProperties.IntermediateRankNameEnglish))
            SqlCommand.Parameters.Add(New SqlParameter("IntermediateRankNameFrench", RegionProperties.IntermediateRankNameFrench))
            SqlCommand.Parameters.Add(New SqlParameter("NoviceRankNameEnglish", RegionProperties.NoviceRankNameEnglish))
            SqlCommand.Parameters.Add(New SqlParameter("NoviceRankNameFrench", RegionProperties.NoviceRankNameFrench))
            SqlCommand.Parameters.Add(New SqlParameter("RookieRankNameEnglish", RegionProperties.RookieRankNameEnglish))
            SqlCommand.Parameters.Add(New SqlParameter("RookieRankNameFrench", RegionProperties.RookieRankNameFrench))
            
            SqlCommand.Parameters.Add(New SqlParameter("AllowBookOffs", RegionProperties.AllowBookOffs))
            SqlCommand.Parameters.Add(New SqlParameter("BookOffHoursBefore", RegionProperties.BookOffHoursBefore))
            SqlCommand.Parameters.Add(New SqlParameter("MaxDateShowAvailableSpots", RegionProperties.MaxDateShowAvailableSpots))
            SqlCommand.Parameters.Add(New SqlParameter("EmailAvailableSpots", RegionProperties.EmailAvailableSpots))
            SqlCommand.Parameters.Add(New SqlParameter("EmailRequestedGames", RegionProperties.EmailRequestedGames))
            SqlCommand.Parameters.Add(New SqlParameter("DefaultArriveBeforeMins", RegionProperties.DefaultArriveBeforeMins))
            SqlCommand.Parameters.Add(New SqlParameter("DefaultMaxGameLengthMins", RegionProperties.DefaultMaxGameLengthMins))
            SqlCommand.Parameters.Add(New SqlParameter("OnlyAllowedToConfirmDaysBefore", RegionProperties.OnlyAllowedToConfirmDaysBefore))
            SqlCommand.Parameters.Add(New SqlParameter("EmailConfirmDaysBefore", RegionProperties.EmailConfirmDaysBefore))
            SqlCommand.Parameters.Add(New SqlParameter("HasOfficials", RegionProperties.HasOfficials))
            SqlCommand.Parameters.Add(New SqlParameter("HasScorekeepers", RegionProperties.HasScorekeepers))
            SqlCommand.Parameters.Add(New SqlParameter("HasSupervisors", RegionProperties.HasSupervisors))
            SqlCommand.Parameters.Add(New SqlParameter("Country", RegionProperties.Country))
            SqlCommand.Parameters.Add(New SqlParameter("State", RegionProperties.State))
            SqlCommand.Parameters.Add(New SqlParameter("City", RegionProperties.City))
            SqlCommand.Parameters.Add(New SqlParameter("Address", RegionProperties.Address))
            SqlCommand.Parameters.Add(New SqlParameter("PostalCode", RegionProperties.PostalCode))
            SqlCommand.Parameters.Add(New SqlParameter("ShowLinksToNonMembers", RegionProperties.ShowLinksToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowParksToNonMembers", RegionProperties.ShowParksToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowLeaguesToNonMembers", RegionProperties.ShowLeaguesToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowOfficialRegionsToNonMembers", RegionProperties.ShowOfficialRegionsToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowTeamsToNonMembers", RegionProperties.ShowTeamsToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowHolidayListToNonMembers", RegionProperties.ShowHolidayListToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowContactListToNonMembers", RegionProperties.ShowContactListToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowAvailabilityDueDateToNonMembers", RegionProperties.ShowAvailabilityDueDateToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowAvailabilityToNonMembers", RegionProperties.ShowAvailabilityToNonMembers AndAlso RegionProperties.ShowContactListToNonMembers AndAlso RegionProperties.ShowHolidayListToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowAvailableSpotsToNonMembers", RegionProperties.ShowAvailableSpotsToNonMembers AndAlso RegionProperties.ShowParksToNonMembers AndAlso RegionProperties.ShowLeaguesToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowFullScheduleToNonMembers", RegionProperties.ShowFullScheduleToNonMembers AndAlso RegionProperties.ShowAvailableSpotsToNonMembers AndAlso RegionProperties.ShowParksToNonMembers AndAlso RegionProperties.ShowLeaguesToNonMembers AndAlso RegionProperties.ShowContactListToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowStandingsToNonMembers", RegionProperties.ShowStandingsToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowStatsToNonMembers", RegionProperties.ShowStatsToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowMainPageToNonMembers", RegionProperties.ShowMainPageToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ExtraScoreParameters", JsonConvert.SerializeObject(New List(Of ExtraScoreParameter))))
            SqlCommand.Parameters.Add(New SqlParameter("HomeTeamCanEnterScore", RegionProperties.HomeTeamCanEnterScore))
            SqlCommand.Parameters.Add(New SqlParameter("AwayTeamCanEnterScore", RegionProperties.AwayTeamCanEnterScore))
            SqlCommand.Parameters.Add(New SqlParameter("ScorekeeperCanEnterScore", RegionProperties.ScorekeeperCanEnterScore))
            SqlCommand.Parameters.Add(New SqlParameter("TeamCanEnterStats", RegionProperties.TeamCanEnterStats))
            SqlCommand.Parameters.Add(New SqlParameter("ScorekeeperCanEnterStats", RegionProperties.ScorekeeperCanEnterStats))
            SqlCommand.Parameters.Add(New SqlParameter("RegionIsLadderLeague", RegionProperties.RegionIsLadderLeague))
            SqlCommand.Parameters.Add(New SqlParameter("NumberOfPlayersInLadderLeague", RegionProperties.NumberOfPlayersInLadderLeague))
            SqlCommand.Parameters.Add(New SqlParameter("HomeTeamCanCancelGameHoursBefore", RegionProperties.HomeTeamCanCancelGameHoursBefore))
            SqlCommand.Parameters.Add(New SqlParameter("AwayTeamCanCancelGameHoursBefore", RegionProperties.AwayTeamCanCancelGameHoursBefore))
            SqlCommand.Parameters.Add(New SqlParameter("MinimumValue", JsonConvert.SerializeObject(RegionProperties.MinimumValue)))
            SqlCommand.Parameters.Add(New SqlParameter("MinimumPercentage", JsonConvert.SerializeObject(RegionProperties.MinimumPercentage)))
            SqlCommand.Parameters.Add(New SqlParameter("UniquePlayers", RegionProperties.UniquePlayers))
            SqlCommand.Parameters.Add(New SqlParameter("StandingIndex", RegionProperties.StandingIndex))
            SqlCommand.Parameters.Add(New SqlParameter("TimeZone", RegionProperties.TimeZone))
            SqlCommand.Parameters.Add(New SqlParameter("AutoSyncCancellationHoursBefore", RegionProperties.AutoSyncCancellationHoursBefore))
            SqlCommand.Parameters.Add(New SqlParameter("AutoSyncSchedule", RegionProperties.AutoSyncSchedule))

            SqlCommand.Parameters.Add(New SqlParameter("DefaultArriveBeforeAwayMins", RegionProperties.DefaultArriveBeforeAwayMins))
            SqlCommand.Parameters.Add(New SqlParameter("DefaultArriveBeforePracticeMins", RegionProperties.DefaultArriveBeforePracticeMins))
            SqlCommand.Parameters.Add(New SqlParameter("DefaultMaxGameLengthPracticeMins", RegionProperties.DefaultMaxGameLengthPracticeMins))

            SqlCommand.Parameters.Add(New SqlParameter("IncludeAfternoonAvailabilityOnWeekdays", RegionProperties.IncludeAfternoonAvailabilityOnWeekdays))
            SqlCommand.Parameters.Add(New SqlParameter("IncludeMorningAvailabilityOnWeekdays", RegionProperties.IncludeMorningAvailabilityOnWeekdays))
            SqlCommand.Parameters.Add(New SqlParameter("EnableNotFilledInInAvailability", RegionProperties.EnableNotFilledInInAvailability))
            SqlCommand.Parameters.Add(New SqlParameter("EnableDualAvailability ", RegionProperties.EnableDualAvailability ))
            SqlCommand.Parameters.Add(New SqlParameter("IsAvailableText", JsonConvert.SerializeObject(RegionProperties.IsAvailableText)))
            SqlCommand.Parameters.Add(New SqlParameter("IsAvailableDualText", JsonConvert.SerializeObject(RegionProperties.IsAvailableDualText)))
            SqlCommand.Parameters.Add(New SqlParameter("IsAvailableCombinedText", JsonConvert.SerializeObject(RegionProperties.IsAvailableCombinedText)))

            SqlCommand.Parameters.Add(New SqlParameter("ShowOnlyDueDatesRangeForAvailability", RegionProperties.ShowOnlyDueDatesRangeForAvailability))

            SqlCommand.Parameters.Add(New SqlParameter("ShowRankInGlobalAvailability", RegionProperties.ShowRankInGlobalAvailability))
            SqlCommand.Parameters.Add(New SqlParameter("ShowSubRankInGlobalAvailability", RegionProperties.ShowSubRankInGlobalAvailability))
            SqlCommand.Parameters.Add(New SqlParameter("SortGlobalAvailabilityByRank", RegionProperties.SortGlobalAvailabilityByRank))
            SqlCommand.Parameters.Add(New SqlParameter("SortGlobalAvailabilityBySubRank", RegionProperties.SortGlobalAvailabilityBySubRank))
            SqlCommand.Parameters.Add(New SqlParameter("NotifyPartnerOfCancellation", RegionProperties.NotifyPartnerOfCancellation))

            SqlCommand.Parameters.Add(New SqlParameter("ShowPhotosToNonMembers", RegionProperties.ShowPhotosToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowArticlesToNonMembers", RegionProperties.ShowArticlesToNonMembers))
            SqlCommand.Parameters.Add(New SqlParameter("ShowWallToNonMembers", RegionProperties.ShowWallToNonMembers))

            SqlCommand.Parameters.Add(New SqlParameter("LeagueRankMaxes", JsonConvert.SerializeObject(RegionProperties.LeagueRankMaxes)))
            SqlCommand.Parameters.Add(New SqlParameter("LinkedRegionIds", JsonConvert.SerializeObject(RegionProperties.LinkedRegionIds)))

            SqlCommand.ExecuteNonQuery()
        End Using
    End Sub

    Public Function Validate() As Object
        RegionId = RegionId.ToLower
        If RegionId.Length = 0 Then Return New ErrorObject("InvalidRegionId")
        If RegionName.Length = 0 Then Return New ErrorObject("InvalidRegionName")

        For I As Integer = 0 To LinkedRegionIds.Count - 1
            LinkedRegionIds(I) = LinkedRegionIds(I).ToLower()
        Next
        LinkedRegionIds.Sort()

        For I As Integer = 1 To LinkedRegionIds.Count - 1
            If LinkedRegionIds(I) = LinkedRegionIds(I - 1) Then
                Return New ErrorObject("InvalidLeagueRanks")
            End If
        Next

        If LeagueRankMaxes.Count <> 15 Then Return New ErrorObject("InvalidLeagueRanks")
        If LeagueRankMaxes(0) < 0 OrElse LeagueRankMaxes(1) < 0 OrElse LeagueRankMaxes(2) < 0 OrElse LeagueRankMaxes(3) < 0 OrElse LeagueRankMaxes(4) < 0 Then Return New ErrorObject("InvalidLeagueRanks")
        If LeagueRankMaxes(5) < 0 OrElse LeagueRankMaxes(6) < 0 OrElse LeagueRankMaxes(7) < 0 OrElse LeagueRankMaxes(8) < 0 OrElse LeagueRankMaxes(9) < 0 Then Return New ErrorObject("InvalidLeagueRanks")
        If LeagueRankMaxes(10) < 0 OrElse LeagueRankMaxes(11) < 0 OrElse LeagueRankMaxes(12) < 0 OrElse LeagueRankMaxes(13) < 0 OrElse LeagueRankMaxes(14) < 0 Then Return New ErrorObject("InvalidLeagueRanks")
        Return Nothing
    End Function
End Class
