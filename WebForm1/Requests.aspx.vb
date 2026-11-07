Imports System.Configuration

Public Class Requests
    Inherits System.Web.UI.Page

    Protected WithEvents gvRequests As Global.System.Web.UI.WebControls.GridView

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        ' Personal data: only show it when the correct key is in the address (Requests.aspx?key=...)
        If Request.QueryString("key") <> ConfigurationManager.AppSettings("AdminKey") Then
            Response.StatusCode = 404
            gvRequests.Visible = False
            Return
        End If

        If Not IsPostBack Then
            gvRequests.DataSource = VisitRequestStore.GetAll()
            gvRequests.DataBind()
        End If
    End Sub
End Class