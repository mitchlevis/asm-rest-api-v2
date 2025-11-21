Imports System.Data.SqlClient
Imports System.Net.Mail
Imports System.Net.Http
Imports System.Net
Imports System.Web.Http
Imports Newtonsoft.Json
Imports UmpireAssignor
Imports System.Web
Imports System.IO
Imports System.Drawing
Imports System.Drawing.Drawing2D

Public Class LeaguePreference
    Public Property RegionId As String
    Public Property RealLeagueId As String
    Public Property CrewType As String
    Public Property GroupBelow As Boolean

    Public Function Clone() As LeaguePreference
        Return New LeaguePreference With {
            .RegionId = RegionId,
            .RealLeagueId = RealLeagueId,
            .CrewType = CrewType,
            .GroupBelow = GroupBelow
        }
    End Function
End Class

Public Class LeaguePreferenceModified2
    Public Property RegionId As String
    Public Property RealLeagueId As String
    Public Property CrewType As String
    Public Property Rank As Integer

    Public Function Clone() As LeaguePreferenceModified2
        Return New LeaguePreferenceModified2 With {
            .RegionId = RegionId,
            .RealLeagueId = RealLeagueId,
            .CrewType = CrewType,
            .Rank = Rank
        }
    End Function
End Class

Public Class LeagueNotAllowed
    Public Property RegionId As String
    Public Property RealLeagueId As String
    Public Property CrewType As String

    Public Function Clone() As LeagueNotAllowed
        Return New LeagueNotAllowed With {
            .RegionId = RegionId,
            .RealLeagueId = RealLeagueId,
            .CrewType = CrewType
        }
    End Function
End Class

Public Class LeaguePreferenceModified
    Public Property RegionId As String
    Public Property RealLeagueId As String
    Public Property CrewType As String
    Public Property LeagueName As String
    Public Property GroupBelow As Boolean
    Public Property IsLinked As Boolean
    Public Property LeagueRank As Integer

    Public Function Clone() As LeaguePreferenceModified
        Return New LeaguePreferenceModified With {
            .RegionId = RegionId,
            .RealLeagueId = RealLeagueId,
            .CrewType = CrewType,
            .LeagueName = LeagueName,
            .GroupBelow = GroupBelow,
            .IsLinked = IsLinked,
            .LeagueRank = LeagueRank
        }
    End Function
End Class

Public Class TeamPreference
    Public Property RealLeagueId As String
    Public Property TeamId As String
    Public Property CrewType As String
    Public Function Clone() As TeamPreference
        Return New TeamPreference With {
            .RealLeagueId = RealLeagueId,
            .TeamId = TeamId,
            .CrewType = CrewType
        }
    End Function
End Class

Public Class MaxGameAmountWeek
    Public Property MinDate As DateTime
    Public Property MaxDate As DateTime
    Public Property DaysIncluded As List(Of Boolean)
    Public Property MaxGames As Integer

    Public Function Clone() As MaxGameAmountWeek
        Dim TDaysIncluded As New List(Of Boolean)
        For Each D In DaysIncluded
            TDaysIncluded.Add(D)
        Next

        Return New MaxGameAmountWeek With {
            .MinDate = MinDate,
            .MaxDate = MaxDate,
            .DaysIncluded = TDaysIncluded,
            .MaxGames = MaxGames
        }
    End Function
End Class

Public Class MaxGameAmountDateRange
    Public Property MinDate As DateTime
    Public Property MaxDate As DateTime
    Public Property DayRangeCount As Integer
    Public Property MaxGames As Integer

    Public Function Clone() As MaxGameAmountDateRange
        Return New MaxGameAmountDateRange With {
            .MinDate = MinDate,
            .MaxDate = MaxDate,
            .DayRangeCount = DayRangeCount,
            .MaxGames = MaxGames
        }
    End Function
End Class

Public Class PartnerPreference
    Public Property Username As String

    Public Function Clone() As PartnerPreference
        Return New PartnerPreference With {
            .Username = Username
        }
    End Function
End Class

Public Class PartnerNotAllowed
    Public Property Username As String

    Public Function Clone() As PartnerNotAllowed
        Return New PartnerNotAllowed With {
            .Username = Username
        }
    End Function
End Class

Public Class PositionPreference
    Public Property PositionId As String
    Public Property Preference As Integer

    Public Function Clone() As PositionPreference
        Return New PositionPreference With {
            .PositionId = PositionId,
            .Preference = Preference
        }
    End Function
End Class

Public Class ConsecutiveGameCount
    Public Property PreferedConsecutiveGamesCount As Integer
    Public Property MaxConsecutiveGamesCount As Integer
    Public Function Clone() As ConsecutiveGameCount
        Return New ConsecutiveGameCount With {
            .PreferedConsecutiveGamesCount = PreferedConsecutiveGamesCount,
            .MaxConsecutiveGamesCount = MaxConsecutiveGamesCount
        }
    End Function
End Class

Public Class BackToBackPositionPreference
    Public Property PositionId1 As String
    Public Property PositionId2 As String
    Public Property Preference As Integer

    Public Function Clone() As BackToBackPositionPreference
        Return New BackToBackPositionPreference With {
            .PositionId1 = PositionId1,
            .PositionId2 = PositionId2,
            .Preference = Preference
        }
    End Function
End Class

Public Class MaxDrivingDistance
    Public Property MinDate As DateTime
    Public Property MaxDate As DateTime
    Public Property DaysIncluded As List(Of Boolean)
    Public Property Minutes As Integer

    Public Function Clone() As MaxDrivingDistance
        Dim TDaysIncluded As New List(Of Boolean)
        For Each D In DaysIncluded
            TDaysIncluded.Add(D)
        Next

        Return New MaxDrivingDistance With {
            .MinDate = MinDate,
            .MaxDate = MaxDate,
            .DaysIncluded = TDaysIncluded,
            .Minutes = Minutes
        }
    End Function
End Class

Public Class AddressSchedule
    Public Property Country As String
    Public Property State As String
    Public Property City As String
    Public Property Address As String
    Public Property PostalCode As String
    Public Property MinDate As DateTime
    Public Property MaxDate As DateTime
    Public Property DaysIncluded As List(Of Boolean)

    Public Function Clone() As AddressSchedule
        Dim TDaysIncluded As New List(Of Boolean)
        For Each D In DaysIncluded
            TDaysIncluded.Add(D)
        Next

        Return New AddressSchedule With {
            .Country = Country,
            .State = State,
            .City = City,
            .Address = Address,
            .PostalCode = PostalCode,
            .MinDate = MinDate,
            .MaxDate = MaxDate,
            .DaysIncluded = TDaysIncluded
        }
    End Function
End Class

Public Class LiftAddress
    Public Property MinDate As DateTime
    Public Property MaxDate As DateTime
    Public Property DaysIncluded As List(Of Boolean)
    Public Property Minutes As Integer

    Public Function Clone() As LiftAddress
        Dim TDaysIncluded As New List(Of Boolean)
        For Each D In DaysIncluded
            TDaysIncluded.Add(D)
        Next

        Return New LiftAddress With {
            .MinDate = MinDate,
            .MaxDate = MaxDate,
            .DaysIncluded = TDaysIncluded,
            .Minutes = Minutes
        }
    End Function
End Class

Public Class AcceptLift
    Public Property MinDate As DateTime
    Public Property MaxDate As DateTime
    Public Property DaysIncluded As List(Of Boolean)
    Public Property AcceptLift As Boolean

    Public Function Clone() As AcceptLift
        Dim TDaysIncluded As New List(Of Boolean)
        For Each D In DaysIncluded
            TDaysIncluded.Add(D)
        Next

        Return New AcceptLift With {
            .MinDate = MinDate,
            .MaxDate = MaxDate,
            .DaysIncluded = TDaysIncluded,
            .AcceptLift = AcceptLift
        }
    End Function
End Class

Public Class AdvancedAvailabilityEnabled
    Public Property LeaguePreferencesEnabled As Integer = 0
    Public Property LeagueNotAllowedEnabled As Integer = 0
    Public Property TeamPreferencesEnabled As Integer = 0
    Public Property MaxGameAmountWeeksEnabled As Integer = 0
    Public Property MaxGameAmountDateRangesEnabled As Integer = 0
    Public Property PartnerPreferencesEnabled As Integer = 0
    Public Property PartnerNotAllowedsEnabled As Integer = 0
    Public Property PositionPreferencesEnabled As Integer = 0
    Public Property ConsecutiveGameCountEnabled As Integer = 0
    Public Property BackToBackPositionPreferencesEnabled As Integer = 0
    Public Property MaxDrivingDistancesEnabled As Integer = 0
    Public Property HomeAddressSchedulesEnabled As Integer = 0
    Public Property DepartingAddressSchedulesEnabled As Integer = 0
    Public Property LiftsFromAddresssEnabled As Integer = 0
    Public Property LiftsFromParksEnabled As Integer = 0
    Public Property AcceptLiftsEnabled As Integer = 0
End Class

Public Class AdvancedAvailabilityProperties
    Public Property LeaguePreferences As New List(Of LeaguePreference)
    Public Property LeagueNotAllowed As New List(Of LeagueNotAllowed)
    Public Property TeamPreferences As New List(Of TeamPreference)
    Public Property MaxGameAmountWeeks As New List(Of MaxGameAmountWeek)
    Public Property MaxGameAmountDateRanges As New List(Of MaxGameAmountDateRange)
    Public Property PartnerPreferences As New List(Of PartnerPreference)
    Public Property PartnerNotAlloweds As New List(Of PartnerNotAllowed)
    Public Property PositionPreferences As New List(Of PositionPreference)
    Public Property ConsecutiveGameCount As New ConsecutiveGameCount With {.MaxConsecutiveGamesCount = 100, .PreferedConsecutiveGamesCount = 100}
    Public Property BackToBackPositionPreferences As New List(Of BackToBackPositionPreference)
    Public Property MaxDrivingDistances As New List(Of MaxDrivingDistance)
    Public Property HomeAddressSchedules As New List(Of AddressSchedule)
    Public Property DepartingAddressSchedules As New List(Of AddressSchedule)
    Public Property LiftsFromAddresss As New List(Of LiftAddress)
    Public Property LiftsFromParks As New List(Of LiftAddress)
    Public Property AcceptLifts As New List(Of AcceptLift)
    Public Property AdvancedAvailabilityEnabled As New AdvancedAvailabilityEnabled
End Class
Public Class AdvancedAvailability
    Public Property RegionId As String
    Public Property Username As String
    Public Property AdvancedAvailabilityProperties As New AdvancedAvailabilityProperties

    Public Shared Function BasicComparer(V1 As AdvancedAvailability, V2 As AdvancedAvailability)
        Dim Comp As Integer = V1.RegionId.CompareTo(V2.RegionId)
        If Comp = 0 Then Comp = V1.Username.CompareTo(V2.Username)
        Return Comp
    End Function

    Public Shared BasicSorter = New GenericIComparer(Of AdvancedAvailability)(AddressOf BasicComparer)

    Public Shared Function GetItem(AdvancedAvailabilities As List(Of AdvancedAvailability), RegionId As String, Username As String) As AdvancedAvailability
        Return PublicCode.BinarySearchItem(AdvancedAvailabilities, New AdvancedAvailability With {.RegionId = RegionId, .Username = Username}, BasicSorter)
    End Function

    Public Shared Function GetAdvancedAvailability(MyUsername As String, RegionId As String, Username As String) As Object
        Dim AdvancedAvailability As AdvancedAvailability = Nothing
        Dim RegionAdvancedAvailability As AdvancedAvailability = Nothing
        Dim RegionLeagues As New Dictionary(Of String, List(Of RegionLeaguePayContracted))
        Dim RegionUsers As New List(Of RegionUser)
        Dim Region As RegionProperties = Nothing
        Dim RegionUser As RegionUser = Nothing
        Dim LinkedRegions As New List(Of RegionProperties)
        Dim Teams As New Dictionary(Of String, List(Of Team))
        Dim AllRegionUsers As New List(Of RegionUser)
        Dim AllRegions As New List(Of RegionProperties)
        Dim AllPositions As New List(Of String)

        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()
                Dim Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(RegionId, MyUsername, SqlConnection, SqlTransaction)

                If RegionId = Username Then
                    If Assignor Is Nothing Then
                        Return New ErrorObject("InvalidPermissions")
                    End If
                Else
                    RegionUser = UmpireAssignor.RegionUser.LoadBasicFromUsername(RegionId, Username, SqlConnection, SqlTransaction)

                    If Assignor Is Nothing OrElse RegionUser Is Nothing Then
                        Return New ErrorObject("InvalidPermissions")
                    End If
                    If Not Assignor.IsExecutive AndAlso MyUsername <> RegionUser.RealUsername Then
                        Return New ErrorObject("InvalidPermissions")
                    End If
                End If

                Region = RegionProperties.GetRegionPropertiesHelper(RegionId, SqlConnection, SqlTransaction)
                If Region.EntityType <> "referee" Then
                    Return New ErrorObject("InvalidPermissions")
                End If
                LinkedRegions = RegionProperties.GetRegionPropertiesRegionIdsHelper(Region.LinkedRegionIds, SqlConnection, SqlTransaction)

                Dim UniqueRegionIds As New List(Of String)


                UniqueRegionIds.Add(RegionId)
                AllRegions.Add(Region)
                For Each LinkedRegion In LinkedRegions
                    If Not UniqueRegionIds.Contains(LinkedRegion.RegionId) Then
                        UniqueRegionIds.Add(LinkedRegion.RegionId)
                        AllRegions.Add(LinkedRegion)
                    End If
                Next

                AllRegions.Sort(RegionProperties.BasicSorter)

                AllRegionUsers = UmpireAssignor.RegionUser.LoadAllInRegionIdsHelper(UniqueRegionIds, "", SqlConnection, SqlTransaction)

                If RegionId <> Username Then
                    For Each AllRegionUser In AllRegionUsers
                        If RegionUser.Username <> AllRegionUser.Username OrElse RegionUser.RegionId <> AllRegionUser.RegionId Then
                            If RegionUser.IsSimilarUser(AllRegionUser) Then
                                RegionUsers.Add(AllRegionUser)
                            End If
                        Else
                            RegionUsers.Add(RegionUser)
                        End If
                    Next
                End If

                RegionLeagues = RegionLeague.ConvertToDic(UniqueRegionIds, RegionLeague.GetRegionLeaguesPayContractedRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction))

                RegionAdvancedAvailability = GetRegionAdvancedAvailabilityHelper(Region, AllRegions, RegionLeagues, SqlConnection, SqlTransaction)

                If RegionId = Username Then
                    AdvancedAvailability = RegionAdvancedAvailability
                    AllPositions = GetAllPositionsRegion(Region, AllRegions)
                Else
                    AdvancedAvailability = GetAdvancedAvailabilityHelper(Region, RegionAdvancedAvailability, AllRegions, RegionUser, RegionUsers, RegionLeagues, SqlConnection, SqlTransaction)
                    AllPositions = GetAllPositions(Region, AllRegions, RegionUser, RegionUsers)
                End If

                Dim RealRegionIds As New List(Of String)
                For Each RegionLeagueTup In RegionLeagues
                    For Each RegionLeague In RegionLeagueTup.Value
                        If RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked Then
                            If Not RealRegionIds.Contains(RegionLeague.RealLeagueId) Then
                                RealRegionIds.Add(RegionLeague.RealLeagueId)
                            End If
                        End If
                    Next
                Next

                Teams = Team.ConvertToDic(RealRegionIds, Team.GetTeamsInRegionsHelper(RealRegionIds, SqlConnection, SqlTransaction))

            End Using
        End Using

        Return New With {
            .Success = True,
            .AdvancedAvailability = AdvancedAvailability,
            .RegionAdvancedAvailabilityEnabled = RegionAdvancedAvailability.AdvancedAvailabilityProperties.AdvancedAvailabilityEnabled,
            .Region = Region,
            .RegionLeagues = RegionLeagues,
            .RegionUser = RegionUser,
            .RegionUsers = RegionUsers,
            .LinkedRegions = LinkedRegions,
            .Teams = Teams,
            .AllRegionUsers = AllRegionUsers,
            .AllPositions = AllPositions
        }
        'Catch ex As Exception
        'Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Shared Function GetAdvancedAvailabilityHelper(Region As RegionProperties, RegionAdvancedAvailability As AdvancedAvailability, Regions As List(Of RegionProperties), RegionUser As RegionUser, RegionUsers As List(Of RegionUser), RegionLeagues As Dictionary(Of String, List(Of RegionLeaguePayContracted)), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As AdvancedAvailability
        Dim AdvancedAvailability As AdvancedAvailability = Nothing

        FixRegionAdvancedAvailability(RegionAdvancedAvailability, Region, Regions, RegionLeagues)

        Dim CommandText As String = "SELECT AdvancedAvailabilityProperties FROM AdvancedAvailability WHERE RegionId = @RegionId AND Username = @Username"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", Region.RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("Username", RegionUser.Username))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                AdvancedAvailability = New AdvancedAvailability With {
                    .RegionId = Region.RegionId,
                    .Username = RegionUser.Username,
                    .AdvancedAvailabilityProperties = JsonConvert.DeserializeObject(Of AdvancedAvailabilityProperties)(Reader.GetString(0))
                }
            End While
            Reader.Close()
        End Using

        If AdvancedAvailability Is Nothing Then
            AdvancedAvailability = New AdvancedAvailability With {.RegionId = Region.RegionId, .Username = RegionUser.Username}
            LoadFromRegionAdvancedAvailability(AdvancedAvailability, RegionAdvancedAvailability)
        End If

        FixAdvancedAvailability(AdvancedAvailability, RegionAdvancedAvailability, Region, Regions, RegionUser, RegionUsers, RegionLeagues)

        Return AdvancedAvailability
    End Function

    Public Shared Sub LoadFromRegionAdvancedAvailability(AdvancedAvailability As AdvancedAvailability, RegionAdvancedAvailability As AdvancedAvailability)
        Dim AA = AdvancedAvailability.AdvancedAvailabilityProperties
        Dim RA = RegionAdvancedAvailability.AdvancedAvailabilityProperties

        AA.LeaguePreferences = New List(Of LeaguePreference)
        For Each Item In RA.LeaguePreferences
            AA.LeaguePreferences.Add(Item.Clone())
        Next

        AA.LeagueNotAllowed = New List(Of LeagueNotAllowed)
        For Each Item In RA.LeagueNotAllowed
            AA.LeagueNotAllowed.Add(Item.Clone())
        Next

        AA.TeamPreferences = New List(Of TeamPreference)
        For Each Item In RA.TeamPreferences
            AA.TeamPreferences.Add(Item.Clone())
        Next

        AA.MaxGameAmountWeeks = New List(Of MaxGameAmountWeek)
        For Each Item In RA.MaxGameAmountWeeks
            AA.MaxGameAmountWeeks.Add(Item.Clone())
        Next

        AA.MaxGameAmountDateRanges = New List(Of MaxGameAmountDateRange)
        For Each Item In RA.MaxGameAmountDateRanges
            AA.MaxGameAmountDateRanges.Add(Item.Clone())
        Next

        AA.PartnerPreferences = New List(Of PartnerPreference)
        For Each Item In RA.PartnerPreferences
            AA.PartnerPreferences.Add(Item.Clone())
        Next

        AA.PartnerNotAlloweds = New List(Of PartnerNotAllowed)
        For Each Item In RA.PartnerNotAlloweds
            AA.PartnerNotAlloweds.Add(Item.Clone())
        Next

        AA.PositionPreferences = New List(Of PositionPreference)
        For Each Item In RA.PositionPreferences
            AA.PositionPreferences.Add(Item.Clone())
        Next

        AA.ConsecutiveGameCount = RA.ConsecutiveGameCount.Clone

        AA.BackToBackPositionPreferences = New List(Of BackToBackPositionPreference)
        For Each Item In RA.BackToBackPositionPreferences
            AA.BackToBackPositionPreferences.Add(Item.Clone())
        Next

        AA.MaxDrivingDistances = New List(Of MaxDrivingDistance)
        For Each Item In RA.MaxDrivingDistances
            AA.MaxDrivingDistances.Add(Item.Clone())
        Next

        AA.HomeAddressSchedules = New List(Of AddressSchedule)
        For Each Item In RA.HomeAddressSchedules
            AA.HomeAddressSchedules.Add(Item.Clone())
        Next

        AA.DepartingAddressSchedules = New List(Of AddressSchedule)
        For Each Item In RA.DepartingAddressSchedules
            AA.DepartingAddressSchedules.Add(Item.Clone())
        Next

        AA.LiftsFromAddresss = New List(Of LiftAddress)
        For Each Item In RA.LiftsFromAddresss
            AA.LiftsFromAddresss.Add(Item.Clone())
        Next

        AA.LiftsFromParks = New List(Of LiftAddress)
        For Each Item In RA.LiftsFromParks
            AA.LiftsFromParks.Add(Item.Clone())
        Next

        AA.AcceptLifts = New List(Of AcceptLift)
        For Each Item In RA.AcceptLifts
            AA.AcceptLifts.Add(Item.Clone())
        Next
    End Sub

    Public Shared Function GetRegionAdvancedAvailabilityHelper(Region As RegionProperties, Regions As List(Of RegionProperties), RegionLeagues As Dictionary(Of String, List(Of RegionLeaguePayContracted)), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As AdvancedAvailability
        Dim AdvancedAvailability As AdvancedAvailability = Nothing

        Dim CommandText As String = "SELECT AdvancedAvailabilityProperties FROM AdvancedAvailability WHERE RegionId = @RegionId AND Username = @Username"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", Region.RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("Username", Region.RegionId))

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                AdvancedAvailability = New AdvancedAvailability With {
                    .RegionId = Region.RegionId,
                    .Username = Region.RegionId,
                    .AdvancedAvailabilityProperties = JsonConvert.DeserializeObject(Of AdvancedAvailabilityProperties)(Reader.GetString(0))
                }
            End While
            Reader.Close()
        End Using

        If AdvancedAvailability Is Nothing Then
            AdvancedAvailability = New AdvancedAvailability With {.RegionId = Region.RegionId, .Username = Region.RegionId}
        End If


        FixRegionAdvancedAvailability(AdvancedAvailability, Region, Regions, RegionLeagues)

        Return AdvancedAvailability
    End Function


    Public Shared Function GetAdvancedAvailabilityFromRegionsHelper(Regions As List(Of RegionProperties), RegionUsers As Dictionary(Of String, List(Of RegionUser)), RegionLeagues As Dictionary(Of String, List(Of RegionLeaguePayContracted)), SQLConnection As SqlConnection, SQLTransaction As SqlTransaction) As List(Of AdvancedAvailability)
        Dim AdvancedAvailabilities As New List(Of AdvancedAvailability)
        Dim RegionAdvancedAvailabilities As New List(Of AdvancedAvailability)

        Dim RegionIdStr As New StringBuilder
        For I As Integer = 0 To Regions.Count - 1
            If I <> 0 Then RegionIdStr.Append(", ")
            RegionIdStr.Append("@RegionId" & I)
        Next

        Dim CommandText As String = "SELECT RegionId, Username, AdvancedAvailabilityProperties FROM AdvancedAvailability WHERE RegionId IN ({0}) ORDER BY RegionId, Username".Replace("{0}", RegionIdStr.ToString())
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            For I As Integer = 0 To Regions.Count - 1
                SqlCommand.Parameters.Add(New SqlParameter("RegionId" & I, Regions(I).RegionId))
            Next

            Dim Reader = SqlCommand.ExecuteReader
            While Reader.Read
                Dim TRegionId As String = Reader.GetString(0)
                Dim TUsername As String = Reader.GetString(1)

                If TRegionId = TUsername Then
                    RegionAdvancedAvailabilities.Add(New AdvancedAvailability With {
                        .RegionId = TRegionId,
                        .Username = TUsername,
                        .AdvancedAvailabilityProperties = JsonConvert.DeserializeObject(Of AdvancedAvailabilityProperties)(Reader.GetString(2))
                    })
                Else
                    AdvancedAvailabilities.Add(New AdvancedAvailability With {
                        .RegionId = TRegionId,
                        .Username = TUsername,
                        .AdvancedAvailabilityProperties = JsonConvert.DeserializeObject(Of AdvancedAvailabilityProperties)(Reader.GetString(2))
                    })
                End If

            End While
            Reader.Close()
        End Using

        For Each Region In Regions
            Dim AdvancedAvailability = GetItem(RegionAdvancedAvailabilities, Region.RegionId, Region.RegionId)

            If AdvancedAvailability Is Nothing Then
                AdvancedAvailability = New AdvancedAvailability With {.RegionId = Region.RegionId, .Username = Region.RegionId}
                RegionAdvancedAvailabilities.Add(AdvancedAvailability)
                RegionAdvancedAvailabilities.Sort(BasicSorter)
            End If

            FixRegionAdvancedAvailability(AdvancedAvailability, Region, Regions, RegionLeagues)
        Next


        For Each Region In Regions
            If Not RegionUsers.ContainsKey(Region.RegionId) Then Continue For

            Dim RegionAdvancedAvailability = GetItem(RegionAdvancedAvailabilities, Region.RegionId, Region.RegionId)

            For Each RegionUser In RegionUsers(Region.RegionId)
                Dim AdvancedAvailability = GetItem(AdvancedAvailabilities, RegionUser.RegionId, RegionUser.Username)
                If AdvancedAvailability Is Nothing Then
                    AdvancedAvailability = New AdvancedAvailability With {.RegionId = Region.RegionId, .Username = RegionUser.Username}
                    LoadFromRegionAdvancedAvailability(AdvancedAvailability, RegionAdvancedAvailability)
                    AdvancedAvailabilities.Add(AdvancedAvailability)
                    AdvancedAvailabilities.Sort(BasicSorter)
                End If
                FixAdvancedAvailability(AdvancedAvailability, RegionAdvancedAvailability, Region, Regions, RegionUser, RegionUsers(Region.RegionId), RegionLeagues)
            Next
        Next

        Return AdvancedAvailabilities
    End Function

    Public Shared Function GetAllPositionsRegion(Region As RegionProperties, Regions As List(Of RegionProperties)) As List(Of String)
        Dim AllPositions As New List(Of String)

        If Region.HasOfficials Then AllPositions.AddRange(RegionLeaguePayContracted.GetSportPositionOrder()(Region.Sport))
        For Each TRegion In Regions
            If TRegion.RegionId = Region.RegionId OrElse Not Region.LinkedRegionIds.Contains(TRegion.RegionId) Then Continue For

            If TRegion.HasOfficials Then
                Dim TPositions = RegionLeaguePayContracted.GetSportPositionOrder()(TRegion.Sport)
                For Each TPosition In TPositions
                    If Not AllPositions.Contains(TPosition) Then AllPositions.Add(TPosition)
                Next
            End If
        Next

        If Region.HasScorekeepers Then AllPositions.AddRange(RegionLeaguePayContracted.GetSportPositionOrderScorekeeper()(Region.Sport))
        For Each TRegion In Regions
            If TRegion.RegionId = Region.RegionId OrElse Not Region.LinkedRegionIds.Contains(TRegion.RegionId) Then Continue For
            If TRegion.HasScorekeepers Then
                Dim TPositions = RegionLeaguePayContracted.GetSportPositionOrderScorekeeper()(TRegion.Sport)
                For Each TPosition In TPositions
                    If Not AllPositions.Contains(TPosition) Then AllPositions.Add(TPosition)
                Next
            End If
        Next

        If Region.HasSupervisors Then AllPositions.AddRange(RegionLeaguePayContracted.GetSportPositionOrderSupervisor()(Region.Sport))
        For Each TRegion In Regions
            If TRegion.RegionId = Region.RegionId OrElse Not Region.LinkedRegionIds.Contains(TRegion.RegionId) Then Continue For
            If TRegion.HasSupervisors Then
                Dim TPositions = RegionLeaguePayContracted.GetSportPositionOrderSupervisor()(TRegion.Sport)
                For Each TPosition In TPositions
                    If Not AllPositions.Contains(TPosition) Then AllPositions.Add(TPosition)
                Next
            End If
        Next

        Return AllPositions
    End Function

    Public Shared Sub FixRegionAdvancedAvailability(RegionAdvancedAvailability As AdvancedAvailability, Region As RegionProperties, Regions As List(Of RegionProperties), RegionLeagues As Dictionary(Of String, List(Of RegionLeaguePayContracted)))
        Dim DefaultSortOrder As New List(Of LeaguePreference)

        For I As Integer = 0 To 2

            Dim CrewType As String = "officials"
            If I = 1 Then CrewType = "scorekeepers"
            If I = 2 Then CrewType = "supervisors"

            For Each LeagueRegion In RegionLeagues
                If LeagueRegion.Key <> Region.RegionId Then Continue For
                If I = 0 AndAlso Not Region.HasOfficials Then Continue For
                If I = 1 AndAlso Not Region.HasScorekeepers Then Continue For
                If I = 2 AndAlso Not Region.HasSupervisors Then Continue For

                LeagueRegion.Value.Sort(RegionLeaguePayContracted.LeagueRankSorter)

                For Each League In LeagueRegion.Value
                    If League.IsLinked AndAlso League.RealLeagueId <> "" Then
                        If DefaultSortOrder.Find(Function(LP) LP.RegionId = "" AndAlso LP.RealLeagueId = League.RealLeagueId AndAlso LP.CrewType = CrewType) Is Nothing Then
                            DefaultSortOrder.Add(New LeaguePreference With {.RegionId = "", .RealLeagueId = League.RealLeagueId, .CrewType = CrewType, .GroupBelow = False})
                        End If
                    Else
                        DefaultSortOrder.Add(New LeaguePreference With {.RegionId = League.RegionId, .RealLeagueId = League.LeagueId, .CrewType = CrewType, .GroupBelow = False})
                    End If
                Next

                LeagueRegion.Value.Sort(RegionLeaguePayContracted.BasicSorter)
            Next

            For Each LeagueRegion In RegionLeagues
                If LeagueRegion.Key = Region.RegionId Then Continue For
                If Not Region.LinkedRegionIds.Contains(LeagueRegion.Key) Then Continue For

                Dim TRegion = RegionProperties.GetItem(Regions, LeagueRegion.Key)

                If I = 0 AndAlso Not TRegion.HasOfficials Then Continue For
                If I = 1 AndAlso Not TRegion.HasScorekeepers Then Continue For
                If I = 2 AndAlso Not TRegion.HasSupervisors Then Continue For

                LeagueRegion.Value.Sort(RegionLeaguePayContracted.LeagueRankSorter)

                For Each League In LeagueRegion.Value
                    If League.IsLinked AndAlso League.RealLeagueId <> "" Then
                        If DefaultSortOrder.Find(Function(LP) LP.RegionId = "" AndAlso LP.RealLeagueId = League.RealLeagueId AndAlso LP.CrewType = CrewType) Is Nothing Then
                            DefaultSortOrder.Add(New LeaguePreference With {.RegionId = "", .RealLeagueId = League.RealLeagueId, .CrewType = CrewType, .GroupBelow = False})
                        End If
                    Else
                        DefaultSortOrder.Add(New LeaguePreference With {.RegionId = League.RegionId, .RealLeagueId = League.LeagueId, .CrewType = CrewType, .GroupBelow = False})
                    End If
                Next

                LeagueRegion.Value.Sort(RegionLeaguePayContracted.BasicSorter)
            Next
        Next

        Dim RA = RegionAdvancedAvailability.AdvancedAvailabilityProperties

        Dim RLPS = RA.LeaguePreferences

        For Each DefaultSortOrderItem In DefaultSortOrder
            If RLPS.Find(Function(LP) LP.RegionId = DefaultSortOrderItem.RegionId AndAlso LP.RealLeagueId = DefaultSortOrderItem.RealLeagueId AndAlso LP.CrewType = DefaultSortOrderItem.CrewType) Is Nothing Then
                RLPS.Add(DefaultSortOrderItem)
            End If
        Next

        For I As Integer = RLPS.Count - 1 To 0 Step -1
            Dim TLP = RLPS(I)

            If DefaultSortOrder.Find(Function(LP) LP.RegionId = TLP.RegionId AndAlso LP.RealLeagueId = TLP.RealLeagueId AndAlso LP.CrewType = TLP.CrewType) Is Nothing Then
                If I > 0 AndAlso Not TLP.GroupBelow AndAlso RLPS(I - 1).GroupBelow Then
                    RLPS(I - 1).GroupBelow = False
                End If
                RLPS.RemoveAt(I)
            End If
        Next

        Dim AllPositions = GetAllPositionsRegion(Region, Regions)

        Dim TempPositionPreferences As New List(Of PositionPreference)

        For I As Integer = 0 To AllPositions.Count - 1
            Dim Position = AllPositions(I)

            Dim TPP = RA.PositionPreferences.Find(Function(PP) PP.PositionId = Position)

            If TPP IsNot Nothing Then
                TempPositionPreferences.Add(TPP)
            Else
                TempPositionPreferences.Add(New PositionPreference With {.PositionId = Position, .Preference = AllPositions.Count - I})
            End If
        Next
        RA.PositionPreferences = TempPositionPreferences

    End Sub

    Public Shared Function GetAllPositions(Region As RegionProperties, Regions As List(Of RegionProperties), RegionUser As RegionUser, RegionUsers As List(Of RegionUser)) As List(Of String)
        Dim AllPositions As New List(Of String)

        If RegionUser.IsOfficial Then AllPositions.AddRange(RegionLeaguePayContracted.GetSportPositionOrder()(Region.Sport))
        For Each TRegion In Regions
            If TRegion.RegionId = Region.RegionId OrElse Not Region.LinkedRegionIds.Contains(TRegion.RegionId) Then Continue For

            Dim TRegionUser = UmpireAssignor.RegionUser.GetItem(RegionUsers, TRegion.RegionId)
            If TRegionUser Is Nothing Then Continue For

            If TRegionUser.IsOfficial Then
                Dim TPositions = RegionLeaguePayContracted.GetSportPositionOrder()(TRegion.Sport)
                For Each TPosition In TPositions
                    If Not AllPositions.Contains(TPosition) Then AllPositions.Add(TPosition)
                Next
            End If
        Next

        If RegionUser.IsScorekeeper Then AllPositions.AddRange(RegionLeaguePayContracted.GetSportPositionOrderScorekeeper()(Region.Sport))
        For Each TRegion In Regions
            If TRegion.RegionId = Region.RegionId OrElse Not Region.LinkedRegionIds.Contains(TRegion.RegionId) Then Continue For
            Dim TRegionUser = UmpireAssignor.RegionUser.GetItem(RegionUsers, TRegion.RegionId)
            If TRegionUser Is Nothing Then Continue For

            If TRegionUser.IsScorekeeper Then
                Dim TPositions = RegionLeaguePayContracted.GetSportPositionOrderScorekeeper()(TRegion.Sport)
                For Each TPosition In TPositions
                    If Not AllPositions.Contains(TPosition) Then AllPositions.Add(TPosition)
                Next
            End If
        Next

        If RegionUser.IsSupervisor Then AllPositions.AddRange(RegionLeaguePayContracted.GetSportPositionOrderSupervisor()(Region.Sport))
        For Each TRegion In Regions
            If TRegion.RegionId = Region.RegionId OrElse Not Region.LinkedRegionIds.Contains(TRegion.RegionId) Then Continue For
            Dim TRegionUser = UmpireAssignor.RegionUser.GetItem(RegionUsers, TRegion.RegionId)
            If TRegionUser Is Nothing Then Continue For

            If TRegionUser.IsSupervisor Then
                Dim TPositions = RegionLeaguePayContracted.GetSportPositionOrderSupervisor()(TRegion.Sport)
                For Each TPosition In TPositions
                    If Not AllPositions.Contains(TPosition) Then AllPositions.Add(TPosition)
                Next
            End If
        Next

        Return AllPositions
    End Function

    Public Shared Sub FixAdvancedAvailability(AdvancedAvailability As AdvancedAvailability, RegionAdvancedAvailability As AdvancedAvailability, Region As RegionProperties, Regions As List(Of RegionProperties), RegionUser As RegionUser, RegionUsers As List(Of RegionUser), RegionLeagues As Dictionary(Of String, List(Of RegionLeaguePayContracted)))
        Dim RA = RegionAdvancedAvailability.AdvancedAvailabilityProperties
        Dim AA = AdvancedAvailability.AdvancedAvailabilityProperties

        Dim RLPS = RA.LeaguePreferences
        Dim LPS = AA.LeaguePreferences

        Dim DefaultSortOrder As New List(Of LeaguePreference)

        For I As Integer = 0 To 2

            Dim CrewType As String = "officials"
            If I = 1 Then CrewType = "scorekeepers"
            If I = 2 Then CrewType = "supervisor"

            For Each LeagueRegion In RegionLeagues
                If LeagueRegion.Key <> Region.RegionId Then Continue For
                If I = 0 AndAlso Not RegionUser.IsOfficial Then Continue For
                If I = 1 AndAlso Not RegionUser.IsScorekeeper Then Continue For
                If I = 2 AndAlso Not RegionUser.IsSupervisor Then Continue For

                LeagueRegion.Value.Sort(RegionLeaguePayContracted.LeagueRankSorter)

                For Each League In LeagueRegion.Value
                    If League.IsLinked AndAlso League.RealLeagueId <> "" Then
                        If DefaultSortOrder.Find(Function(LP) LP.RegionId = "" AndAlso LP.RealLeagueId = League.RealLeagueId AndAlso LP.CrewType = CrewType) Is Nothing Then
                            DefaultSortOrder.Add(New LeaguePreference With {.RegionId = "", .RealLeagueId = League.RealLeagueId, .CrewType = CrewType, .GroupBelow = False})
                        End If
                    Else
                        DefaultSortOrder.Add(New LeaguePreference With {.RegionId = League.RegionId, .RealLeagueId = League.LeagueId, .CrewType = CrewType, .GroupBelow = False})
                    End If
                Next

                LeagueRegion.Value.Sort(RegionLeaguePayContracted.BasicSorter)
            Next

            For Each LeagueRegion In RegionLeagues
                If LeagueRegion.Key = Region.RegionId Then Continue For
                If Not Region.LinkedRegionIds.Contains(LeagueRegion.Key) Then Continue For

                Dim TRegionUser = RegionUser.GetItem(RegionUsers, LeagueRegion.Key)
                If TRegionUser Is Nothing Then Continue For

                If I = 0 AndAlso Not TRegionUser.IsOfficial Then Continue For
                If I = 1 AndAlso Not TRegionUser.IsScorekeeper Then Continue For
                If I = 2 AndAlso Not TRegionUser.IsSupervisor Then Continue For

                LeagueRegion.Value.Sort(RegionLeaguePayContracted.LeagueRankSorter)

                For Each League In LeagueRegion.Value
                    If League.IsLinked AndAlso League.RealLeagueId <> "" Then
                        If DefaultSortOrder.Find(Function(LP) LP.RegionId = "" AndAlso LP.RealLeagueId = League.RealLeagueId AndAlso LP.CrewType = CrewType) Is Nothing Then
                            DefaultSortOrder.Add(New LeaguePreference With {.RegionId = "", .RealLeagueId = League.RealLeagueId, .CrewType = CrewType, .GroupBelow = False})
                        End If
                    Else
                        DefaultSortOrder.Add(New LeaguePreference With {.RegionId = League.RegionId, .RealLeagueId = League.LeagueId, .CrewType = CrewType, .GroupBelow = False})
                    End If
                Next

                LeagueRegion.Value.Sort(RegionLeaguePayContracted.BasicSorter)
            Next
        Next

        Dim LastAdded As LeaguePreference = Nothing

        For I As Integer = 0 To RLPS.Count - 1
            Dim RLP = RLPS(I)
            If DefaultSortOrder.Find(Function(LP) LP.RegionId = RLP.RegionId AndAlso LP.RealLeagueId = RLP.RealLeagueId AndAlso LP.CrewType = RLP.CrewType) Is Nothing Then
                If LastAdded IsNot Nothing Then
                    LastAdded.GroupBelow = False
                End If
                If Not RLP.GroupBelow Then LastAdded = Nothing
                Continue For
            End If
            If LPS.Find(Function(LP) LP.RegionId = RLP.RegionId AndAlso LP.RealLeagueId = RLP.RealLeagueId AndAlso LP.CrewType = RLP.CrewType) IsNot Nothing Then
                If LastAdded IsNot Nothing Then
                    LastAdded.GroupBelow = False
                End If
                If Not RLP.GroupBelow Then LastAdded = Nothing
                Continue For
            End If

            LastAdded = RLP.Clone()
            LPS.Add(LastAdded)
        Next

        For I As Integer = LPS.Count - 1 To 0 Step -1
            Dim TLP = LPS(I)

            If DefaultSortOrder.Find(Function(LP) LP.RegionId = TLP.RegionId AndAlso LP.RealLeagueId = TLP.RealLeagueId AndAlso LP.CrewType = TLP.CrewType) Is Nothing Then
                If I > 0 AndAlso Not TLP.GroupBelow AndAlso LPS(I - 1).GroupBelow Then
                    LPS(I - 1).GroupBelow = False
                End If
                LPS.RemoveAt(I)
            End If
        Next

        Dim AllPositions = GetAllPositions(Region, Regions, RegionUser, RegionUsers)

        Dim TempPositionPreferences As New List(Of PositionPreference)

        For I As Integer = 0 To AllPositions.Count - 1
            Dim Position = AllPositions(I)

            Dim TPP = AA.PositionPreferences.Find(Function(PP) PP.PositionId = Position)

            If TPP IsNot Nothing Then
                TempPositionPreferences.Add(TPP)
            Else
                Dim TPPA = AA.PositionPreferences.Find(Function(PP) PP.PositionId = Position)
                If TPPA IsNot Nothing Then
                    TempPositionPreferences.Add(TPPA)
                Else
                    TempPositionPreferences.Add(New PositionPreference With {.PositionId = Position, .Preference = AllPositions.Count - I})
                End If
            End If
        Next
        AA.PositionPreferences = TempPositionPreferences

    End Sub

    Public Shared Function UpsertAdvancedAvailability(MyUsername As String, NewAdvancedAvailability As AdvancedAvailability) As Object
        NewAdvancedAvailability.RegionId = NewAdvancedAvailability.RegionId.ToLower
        NewAdvancedAvailability.Username = NewAdvancedAvailability.Username.ToLower

        Dim RegionId As String = NewAdvancedAvailability.RegionId
        Dim Username As String = NewAdvancedAvailability.Username

        Dim Assignor As RegionUser = Nothing
        Dim RegionUser As RegionUser = Nothing

        Dim AdvancedAvailability As AdvancedAvailability = Nothing
        Dim RegionAdvancedAvailability As AdvancedAvailability = Nothing
        Dim RegionLeagues As New Dictionary(Of String, List(Of RegionLeaguePayContracted))
        Dim RegionUsers As New List(Of RegionUser)
        Dim Region As RegionProperties = Nothing
        Dim LinkedRegions As New List(Of RegionProperties)
        Dim Teams As New Dictionary(Of String, List(Of Team))
        Dim AllRegionUsers As New List(Of RegionUser)

        'Try
        Using SqlConnection As New SqlConnection(PublicCode.GetConnectionString())
            SqlConnection.Open()
            Using SqlTransaction = SqlConnection.BeginTransaction()
                Assignor = UmpireAssignor.RegionUser.GetUsernameAndPositionsFromRealUsername(NewAdvancedAvailability.RegionId, MyUsername, SqlConnection, SqlTransaction)

                If RegionId = Username Then
                    If Assignor Is Nothing Then
                        Return New ErrorObject("InvalidPermissions")
                    End If
                    If Not Assignor.IsExecutive Then Return New ErrorObject("InvalidPermissions")
                Else
                    RegionUser = UmpireAssignor.RegionUser.LoadBasicFromUsername(RegionId, Username, SqlConnection, SqlTransaction)

                    If Assignor Is Nothing OrElse RegionUser Is Nothing Then
                        Return New ErrorObject("InvalidPermissions")
                    End If
                    If Not Assignor.IsExecutive AndAlso MyUsername <> RegionUser.RealUsername Then
                        Return New ErrorObject("InvalidPermissions")
                    End If
                End If

                Region = RegionProperties.GetRegionPropertiesHelper(RegionId, SqlConnection, SqlTransaction)
                If Region.EntityType <> "referee" Then
                    Return New ErrorObject("InvalidPermissions")
                End If
                LinkedRegions = RegionProperties.GetRegionPropertiesRegionIdsHelper(Region.LinkedRegionIds, SqlConnection, SqlTransaction)

                Dim UniqueRegionIds As New List(Of String)
                Dim AllRegions As New List(Of RegionProperties)

                UniqueRegionIds.Add(RegionId)
                AllRegions.Add(Region)
                For Each LinkedRegion In LinkedRegions
                    If Not UniqueRegionIds.Contains(LinkedRegion.RegionId) Then
                        UniqueRegionIds.Add(LinkedRegion.RegionId)
                        AllRegions.Add(LinkedRegion)
                    End If
                Next

                AllRegions.Sort(RegionProperties.BasicSorter)

                AllRegionUsers = UmpireAssignor.RegionUser.LoadAllInRegionIdsHelper(UniqueRegionIds, "", SqlConnection, SqlTransaction)

                If RegionId <> Username Then
                    For Each AllRegionUser In AllRegionUsers
                        If RegionUser.Username <> AllRegionUser.Username OrElse RegionUser.RegionId <> AllRegionUser.RegionId Then
                            If RegionUser.IsSimilarUser(AllRegionUser) Then
                                RegionUsers.Add(AllRegionUser)
                            End If
                        Else
                            RegionUsers.Add(RegionUser)
                        End If
                    Next
                End If

                RegionLeagues = RegionLeague.ConvertToDic(UniqueRegionIds, RegionLeague.GetRegionLeaguesPayContractedRegionIdsHelper(UniqueRegionIds, SqlConnection, SqlTransaction))

                RegionAdvancedAvailability = GetRegionAdvancedAvailabilityHelper(Region, AllRegions, RegionLeagues, SqlConnection, SqlTransaction)

                If RegionId = Username Then
                    AdvancedAvailability = RegionAdvancedAvailability
                Else
                    AdvancedAvailability = GetAdvancedAvailabilityHelper(Region, RegionAdvancedAvailability, AllRegions, RegionUser, AllRegionUsers, RegionLeagues, SqlConnection, SqlTransaction)
                End If

                Dim RealRegionIds As New List(Of String)
                For Each RegionLeagueTup In RegionLeagues
                    For Each RegionLeague In RegionLeagueTup.Value
                        If RegionLeague.RealLeagueId <> "" AndAlso RegionLeague.IsLinked Then
                            If Not RealRegionIds.Contains(RegionLeague.RealLeagueId) Then
                                RealRegionIds.Add(RegionLeague.RealLeagueId)
                            End If
                        End If
                    Next
                Next

                Teams = Team.ConvertToDic(RealRegionIds, Team.GetTeamsInRegionsHelper(RealRegionIds, SqlConnection, SqlTransaction))

                If Not Assignor.IsExecutive Then
                    Dim RA = RegionAdvancedAvailability.AdvancedAvailabilityProperties
                    Dim AA = AdvancedAvailability.AdvancedAvailabilityProperties

                    Dim RE = RA.AdvancedAvailabilityEnabled
                    Dim AE = AA.AdvancedAvailabilityEnabled


                    If (AE.LeaguePreferencesEnabled = 0 AndAlso RE.LeaguePreferencesEnabled = 1) OrElse AE.LeaguePreferencesEnabled = 2 Then AA.LeaguePreferences = RA.LeaguePreferences
                    If (AE.LeagueNotAllowedEnabled = 0 AndAlso RE.LeagueNotAllowedEnabled = 1) OrElse AE.LeagueNotAllowedEnabled = 2 Then AA.LeagueNotAllowed = RA.LeagueNotAllowed

                    If (AE.TeamPreferencesEnabled = 0 AndAlso RE.TeamPreferencesEnabled = 1) OrElse AE.TeamPreferencesEnabled = 2 Then AA.TeamPreferences = RA.TeamPreferences
                    If (AE.MaxGameAmountWeeksEnabled = 0 AndAlso RE.MaxGameAmountWeeksEnabled = 1) OrElse AE.MaxGameAmountWeeksEnabled = 2 Then AA.MaxGameAmountWeeks = RA.MaxGameAmountWeeks
                    If (AE.MaxGameAmountDateRangesEnabled = 0 AndAlso RE.MaxGameAmountDateRangesEnabled = 1) OrElse AE.MaxGameAmountDateRangesEnabled = 2 Then AA.MaxGameAmountDateRanges = RA.MaxGameAmountDateRanges
                    If (AE.PartnerPreferencesEnabled = 0 AndAlso RE.PartnerPreferencesEnabled = 1) OrElse AE.PartnerPreferencesEnabled = 2 Then AA.PartnerPreferences = RA.PartnerPreferences
                    If (AE.PartnerNotAllowedsEnabled = 0 AndAlso RE.PartnerNotAllowedsEnabled = 1) OrElse AE.PartnerNotAllowedsEnabled = 2 Then AA.PartnerNotAlloweds = RA.PartnerNotAlloweds
                    If (AE.PositionPreferencesEnabled = 0 AndAlso RE.PositionPreferencesEnabled = 1) OrElse AE.PositionPreferencesEnabled = 2 Then AA.PositionPreferences = RA.PositionPreferences
                    If (AE.ConsecutiveGameCountEnabled = 0 AndAlso RE.ConsecutiveGameCountEnabled = 1) OrElse AE.ConsecutiveGameCountEnabled = 2 Then AA.ConsecutiveGameCount = RA.ConsecutiveGameCount
                    If (AE.BackToBackPositionPreferencesEnabled = 0 AndAlso RE.BackToBackPositionPreferencesEnabled = 1) OrElse AE.BackToBackPositionPreferencesEnabled = 2 Then AA.BackToBackPositionPreferences = RA.BackToBackPositionPreferences

                    If (AE.MaxDrivingDistancesEnabled = 0 AndAlso RE.MaxDrivingDistancesEnabled = 1) OrElse AE.MaxDrivingDistancesEnabled = 2 Then AA.MaxDrivingDistances = RA.MaxDrivingDistances
                    If (AE.HomeAddressSchedulesEnabled = 0 AndAlso RE.HomeAddressSchedulesEnabled = 1) OrElse AE.HomeAddressSchedulesEnabled = 2 Then AA.HomeAddressSchedules = RA.HomeAddressSchedules
                    If (AE.DepartingAddressSchedulesEnabled = 0 AndAlso RE.DepartingAddressSchedulesEnabled = 1) OrElse AE.DepartingAddressSchedulesEnabled = 2 Then AA.DepartingAddressSchedules = RA.DepartingAddressSchedules
                    If (AE.LiftsFromAddresssEnabled = 0 AndAlso RE.LiftsFromAddresssEnabled = 1) OrElse AE.LiftsFromAddresssEnabled = 2 Then AA.LiftsFromAddresss = RA.LiftsFromAddresss
                    If (AE.LiftsFromParksEnabled = 0 AndAlso RE.LiftsFromParksEnabled = 1) OrElse AE.LiftsFromParksEnabled = 2 Then AA.LiftsFromParks = RA.LiftsFromParks
                    If (AE.AcceptLiftsEnabled = 0 AndAlso RE.AcceptLiftsEnabled = 1) OrElse AE.AcceptLiftsEnabled = 2 Then AA.AcceptLifts = RA.AcceptLifts
                End If

                If RegionId = Username Then
                    FixRegionAdvancedAvailability(NewAdvancedAvailability, Region, AllRegions, RegionLeagues)
                Else
                    FixAdvancedAvailability(NewAdvancedAvailability, RegionAdvancedAvailability, Region, AllRegions, RegionUser, RegionUsers, RegionLeagues)
                End If

                UpsertHelper(NewAdvancedAvailability, SqlConnection, SqlTransaction)
                SqlTransaction.Commit()
            End Using
        End Using

        Return New With {
            .Success = True
        }
        'Catch E As Exception
        '    Return New ErrorObject("UnknownError")
        'End Try
    End Function

    Public Shared Sub UpsertHelper(AdvancedAvailability As AdvancedAvailability, SQLConnection As SqlConnection, SQLTransaction As SqlTransaction)
        Dim CommandText As String = "DELETE FROM AdvancedAvailability WHERE RegionId = @RegionId AND Username = @Username; INSERT INTO AdvancedAvailability (RegionId, Username, AdvancedAvailabilityProperties) VALUES (@RegionId, @Username, @AdvancedAvailabilityProperties)"
        Using SqlCommand As New SqlCommand(CommandText, SQLConnection, SQLTransaction)
            SqlCommand.Parameters.Add(New SqlParameter("RegionId", AdvancedAvailability.RegionId))
            SqlCommand.Parameters.Add(New SqlParameter("Username", AdvancedAvailability.Username))
            SqlCommand.Parameters.Add(New SqlParameter("AdvancedAvailabilityProperties", JsonConvert.SerializeObject(AdvancedAvailability.AdvancedAvailabilityProperties)))
            SqlCommand.ExecuteNonQuery()
        End Using
    End Sub
End Class
