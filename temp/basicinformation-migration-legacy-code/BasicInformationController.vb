Imports System.Net
Imports System.Web.Http

Public Class BasicInformationController
    Inherits ApiController

    ' GET api/basicinformation
    Public Function GetValues() As Object
        Dim Cookies = PublicCode.GetCookies(Request)
        If Not UmpireAssignor.User.SessionTokenExists(Cookies) Then Return New ErrorObject("InvalidSessionToken")
        Return UmpireAssignor.User.GetBasicInformation(Cookies("username"))
    End Function

End Class
