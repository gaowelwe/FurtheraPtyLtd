Imports System.Collections.Generic
Imports System.Linq

Public Class Piece
    Public Property Name As String
    Public Property Category As String
    Public Property Material As String
    Public Property Note As String
End Class

Public Class _Default
    Inherits System.Web.UI.Page

    Private Shared ReadOnly Pieces As New List(Of Piece) From {
        New Piece With {.Name = "Ember Bowl", .Category = "Ceramics", .Material = "Wood-fired stoneware", .Note = "Ash glaze that pools differently in every firing."},
        New Piece With {.Name = "Tidewater Vase", .Category = "Ceramics", .Material = "Hand-coiled clay", .Note = "A leaning silhouette, finished with river-clay slip."},
        New Piece With {.Name = "Moss Throw", .Category = "Textiles", .Material = "Plant-dyed merino", .Note = "Dyed in walnut hull and lichen, woven in small lots."},
        New Piece With {.Name = "Dune Cushion", .Category = "Textiles", .Material = "Stonewashed linen", .Note = "Softens with every season it spends in your home."},
        New Piece With {.Name = "Hearth Stool", .Category = "Wood", .Material = "Solid walnut", .Note = "Carved from a single block, oiled by hand."},
        New Piece With {.Name = "Fern Tray", .Category = "Wood", .Material = "Olive wood", .Note = "Each grain pattern is unique; no two trays match."}
    }

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If IsPostBack Then Return

        rptPieces.DataSource = Pieces
        rptPieces.DataBind()

        rptCats.DataSource = Pieces.Select(Function(p) p.Category).Distinct().ToList()
        rptCats.DataBind()
    End Sub

    Protected Sub btnRequest_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnRequest.Click
        Page.Validate()
        If Not Page.IsValid Then Return

        Dim message As String = txtMessage.Text.Trim()
        If message.Length > 600 Then message = message.Substring(0, 600)

        Dim name As String = txtName.Text.Trim()
        Dim email As String = txtEmail.Text.Trim()
        Dim newId As Integer

        Try
            newId = VisitRequestStore.Save(name, email, ddlInterest.SelectedValue, message)
        Catch ex As Exception
            System.Diagnostics.Trace.TraceError(ex.ToString())
            Dim detail As String = ""
            If Context.IsDebuggingEnabled Then detail = " (" & Server.HtmlEncode(ex.Message) & ")"
            ShowMessage("We couldn't save your request just now. Please try again in a moment." & detail, False)
            Return
        End Try

        ' The request is already saved, so an email problem must not lose it.
        Dim emailed As Boolean = False
        Try
            Mailer.SendVisitConfirmation(email, name, ddlInterest.SelectedValue)
            emailed = True
        Catch ex As Exception
            System.Diagnostics.Trace.TraceError(ex.ToString())
        End Try

        Try
            Mailer.SendOwnerAlert(email, name, ddlInterest.SelectedValue, message, newId)
        Catch ex As Exception
            System.Diagnostics.Trace.TraceError(ex.ToString())
        End Try

        Dim follow As String = If(emailed,
            "A confirmation has been emailed to " & Server.HtmlEncode(email) & ".",
            "We will write to " & Server.HtmlEncode(email) & " within two days.")

        ShowMessage("Thank you, " & Server.HtmlEncode(name) &
                    ". Your visit request was sent successfully. " & follow, True)
        pnlForm.Visible = False
    End Sub

    Private Sub ShowMessage(ByVal html As String, ByVal success As Boolean)
        litConfirm.Text = html
        pnlConfirm.CssClass = If(success, "confirm-ok", "confirm-error")
        pnlConfirm.Visible = True
        ClientScript.RegisterStartupScript(Me.GetType(), "toVisit",
            "document.getElementById('visit').scrollIntoView({behavior:'smooth',block:'center'});", True)
    End Sub
End Class