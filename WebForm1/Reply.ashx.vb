Imports System
Imports System.Configuration
Imports System.Data
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Web
Imports WebForm1

' Reply page for the studio owner: opened from the "Reply" button in the alert email.
Public Class Reply
    Implements IHttpHandler

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property

    ' ---------- helpers ----------

    Private Shared Function Enc(value As String) As String
        Return WebUtility.HtmlEncode(If(value, ""))
    End Function

    Private Shared Function Cfg(settingName As String, Optional defaultValue As String = "") As String
        Dim value As String = ConfigurationManager.AppSettings(settingName)
        If String.IsNullOrWhiteSpace(value) Then Return defaultValue
        Return value.Trim()
    End Function

    ' Returns an HTML-encoded http/https address, or "" if the value is not a safe web address
    Private Shared Function SafeUrl(value As String) As String
        Dim parsed As Uri = Nothing
        If Uri.TryCreate(value, UriKind.Absolute, parsed) AndAlso (parsed.Scheme = Uri.UriSchemeHttp OrElse parsed.Scheme = Uri.UriSchemeHttps) Then
            Return Enc(parsed.AbsoluteUri)
        End If
        Return ""
    End Function

    ' ---------- main request ----------

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest

        context.Response.ContentType = "text/html; charset=utf-8"
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache)
        context.Response.Cache.SetNoStore()
        context.Response.AddHeader("X-Content-Type-Options", "nosniff")
        context.Response.AddHeader("X-Frame-Options", "DENY")
        context.Response.AddHeader("Referrer-Policy", "no-referrer")

        ' Security: only someone with the admin key can open this page
        Dim key As String = If(context.Request("key"), "")
        Dim adminKey As String = Cfg("AdminKey")
        Dim id As Integer

        If String.IsNullOrWhiteSpace(adminKey) OrElse Not String.Equals(key, adminKey, StringComparison.Ordinal) OrElse Not Integer.TryParse(context.Request("id"), id) OrElse id <= 0 Then
            context.Response.StatusCode = 404
            Return
        End If

        ' Visitor details
        Dim row As DataRow = VisitRequestStore.GetById(id)
        If row Is Nothing Then
            context.Response.StatusCode = 404
            Return
        End If

        Dim name As String = Convert.ToString(row("FullName"))
        Dim email As String = Convert.ToString(row("Email"))
        Dim interest As String = Convert.ToString(row("Interest"))
        Dim note As String = Convert.ToString(row("Message"))

        ' Company and signature details (all from Web.config)
        Dim company As String = Cfg("CompanyName", "Furthera Pty Ltd")
        Dim ownerName As String = Cfg("MailFromName")
        Dim ownerEmail As String = Cfg("OwnerEmail")
        Dim website As String = Cfg("CompanyWebsite")
        If website <> "" AndAlso Not website.StartsWith("http", StringComparison.OrdinalIgnoreCase) Then website = "https://" & website
        Dim address As String = Cfg("CompanyAddress")

        ' Picture: an optional public logo address, otherwise Content/signature.png if it exists
        Dim logo As String = SafeUrl(Cfg("CompanyLogoUrl"))
        If logo = "" Then
            Dim localPath As String = context.Server.MapPath("~/Content/signature.png")
            If File.Exists(localPath) Then logo = Enc(VirtualPathUtility.ToAbsolute("~/Content/signature.png"))
        End If

        ' Default reply
        Dim subject As String = "Re: Your visit request"
        Dim message As String = Mailer.DefaultReplyText(name, interest)
        Dim status As String = ""
        Dim statusOk As Boolean = False

        ' Process the reply form
        If context.Request.HttpMethod = "POST" Then

            Dim submittedSubject As String = System.Text.RegularExpressions.Regex.Replace(If(context.Request.Form("subject"), ""), "[\r\n]+", " ").Trim()
            If submittedSubject <> "" Then
                subject = If(submittedSubject.Length > 150, submittedSubject.Substring(0, 150), submittedSubject)
            End If

            message = If(context.Request.Form("message"), "")
            If message.Length > 4000 Then message = message.Substring(0, 4000)

            If String.IsNullOrWhiteSpace(message) Then
                status = "Please write a message before sending."
            Else
                Try
                    Mailer.SendReply(email, name, subject, message)
                    statusOk = True
                    status = "Your reply was successfully sent to " & email & "."
                Catch ex As Exception
                    System.Diagnostics.Trace.TraceError("Reply sending error: " & ex.ToString())
                    status = "The reply could not be sent. Please check your email configuration."
                End Try
            End If
        End If

        ' ---------- build the page ----------

        Dim sb As New StringBuilder()

        sb.AppendLine("<!DOCTYPE html>")
        sb.AppendLine("<html lang='en'><head><meta charset='utf-8'>")
        sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1'>")
        sb.AppendLine("<meta name='robots' content='noindex,nofollow'>")
        sb.AppendLine("<title>Reply | " & Enc(company) & "</title>")

        sb.AppendLine("<style>")
        sb.AppendLine("*{box-sizing:border-box}")
        sb.AppendLine("body{margin:0;padding:40px 18px;background:#f4f5f7;color:#252b36;font-family:Segoe UI,Arial,sans-serif}")
        sb.AppendLine(".container{max-width:850px;margin:auto}")
        sb.AppendLine(".topbar{display:flex;justify-content:space-between;align-items:center;margin-bottom:25px}")
        sb.AppendLine(".brand{font-size:21px;font-weight:700;color:#252b36}")
        sb.AppendLine(".secure{font-size:12px;color:#78818e}")
        sb.AppendLine(".card{background:#fff;border:1px solid #e8eaf0;border-radius:16px;padding:30px;margin-bottom:20px;box-shadow:0 8px 30px rgba(20,30,50,.05)}")
        sb.AppendLine("h1{font-size:25px;margin:0 0 8px;color:#252b36}")
        sb.AppendLine(".subtitle{color:#818997;margin:0 0 25px;font-size:14px}")
        sb.AppendLine(".customer{display:grid;grid-template-columns:1fr 1fr;gap:15px;background:#f7f8fa;border-radius:12px;padding:18px;margin-bottom:25px}")
        sb.AppendLine(".detail-label{display:block;font-size:11px;text-transform:uppercase;letter-spacing:1px;color:#8b93a0;margin-bottom:5px}")
        sb.AppendLine(".detail-value{font-size:14px;font-weight:600;overflow-wrap:anywhere}")
        sb.AppendLine(".original-message{padding:18px;background:#faf8f4;border-left:4px solid #b99557;border-radius:8px;margin-bottom:25px}")
        sb.AppendLine(".original-message h3{font-size:13px;margin:0 0 10px;color:#8b744d}")
        sb.AppendLine(".original-message p{margin:0;color:#606775;white-space:pre-wrap;overflow-wrap:anywhere}")
        sb.AppendLine("label{display:block;font-size:13px;font-weight:600;margin-bottom:17px;color:#424957}")
        sb.AppendLine("input,textarea{width:100%;display:block;margin-top:8px;padding:13px 14px;border:1px solid #dfe3e9;border-radius:9px;background:#fff;font:14px Segoe UI,Arial,sans-serif;color:#252b36}")
        sb.AppendLine("textarea{min-height:220px;resize:vertical;line-height:1.7}")
        sb.AppendLine("input:focus,textarea:focus{outline:none;border-color:#b99557;box-shadow:0 0 0 3px rgba(185,149,87,.12)}")
        sb.AppendLine(".signature{border:1px solid #e7e9ed;border-radius:12px;padding:20px;margin-top:25px;background:#fff}")
        sb.AppendLine(".signature-title{font-size:11px;text-transform:uppercase;letter-spacing:1px;color:#8b93a0;margin-bottom:15px}")
        sb.AppendLine(".signature-logo{width:88px;height:88px;object-fit:cover;border-radius:50%;margin-bottom:15px;display:block}")
        sb.AppendLine(".signature-company{font-size:17px;font-weight:700;color:#252b36;margin-bottom:5px}")
        sb.AppendLine(".signature-owner{font-size:13px;color:#525c6b;margin-bottom:8px}")
        sb.AppendLine(".signature a{color:#a17c3d;text-decoration:none;font-size:13px}")
        sb.AppendLine(".signature-address{font-size:12px;color:#818997;margin-top:8px}")
        sb.AppendLine(".actions{display:flex;align-items:center;gap:15px;margin-top:25px;flex-wrap:wrap}")
        sb.AppendLine(".send{border:0;border-radius:9px;background:#252b36;color:#fff;padding:14px 25px;font-size:14px;font-weight:600;cursor:pointer}")
        sb.AppendLine(".send:hover{background:#b99557;color:#fff}")
        sb.AppendLine(".hint{font-size:12px;color:#9299a5}")
        sb.AppendLine(".alert{padding:14px 16px;border-radius:9px;font-size:13px;margin-bottom:20px}")
        sb.AppendLine(".success{background:#eaf6ed;color:#267342}")
        sb.AppendLine(".error{background:#fff1ed;color:#b43d2e}")
        sb.AppendLine(".footer{text-align:center;color:#a0a6b0;font-size:12px;padding:15px}")
        sb.AppendLine("@media(max-width:600px){body{padding:20px 12px}.card{padding:20px}.customer{grid-template-columns:1fr}.topbar{align-items:flex-start;gap:10px;flex-direction:column}h1{font-size:22px}}")
        sb.AppendLine("</style></head>")

        sb.AppendLine("<body><div class='container'>")

        ' Header
        sb.AppendLine("<div class='topbar'>")
        sb.AppendLine("<div class='brand'>" & Enc(company) & "</div>")
        sb.AppendLine("<div class='secure'>PRIVATE ADMIN REPLY</div>")
        sb.AppendLine("</div>")

        sb.AppendLine("<div class='card'>")
        sb.AppendLine("<h1>Reply to customer</h1>")
        sb.AppendLine("<p class='subtitle'>Compose and send a professional response to this visit request.</p>")

        ' Status message
        If status <> "" Then
            sb.AppendLine("<div class='alert " & If(statusOk, "success", "error") & "' role='status'>" & Enc(status) & "</div>")
        End If

        ' Customer details
        sb.AppendLine("<div class='customer'>")
        sb.AppendLine("<div><span class='detail-label'>Customer name</span><div class='detail-value'>" & Enc(name) & "</div></div>")
        sb.AppendLine("<div><span class='detail-label'>Email address</span><div class='detail-value'>" & Enc(email) & "</div></div>")
        sb.AppendLine("<div><span class='detail-label'>Interested in</span><div class='detail-value'>" & Enc(interest) & "</div></div>")
        sb.AppendLine("</div>")

        ' Original message from the customer
        If Not String.IsNullOrWhiteSpace(note) Then
            sb.AppendLine("<div class='original-message'>")
            sb.AppendLine("<h3>ORIGINAL CUSTOMER MESSAGE</h3>")
            sb.AppendLine("<p>" & Enc(note) & "</p>")
            sb.AppendLine("</div>")
        End If

        ' Reply form (hidden once the reply has been sent)
        If Not statusOk Then

            sb.AppendLine("<form method='post' action='' autocomplete='off'>")
            sb.AppendLine("<input type='hidden' name='id' value='" & id.ToString() & "'>")
            sb.AppendLine("<input type='hidden' name='key' value='" & Enc(key) & "'>")

            sb.AppendLine("<label>Email subject")
            sb.AppendLine("<input type='text' name='subject' maxlength='150' required value='" & Enc(subject) & "'>")
            sb.AppendLine("</label>")

            sb.AppendLine("<label>Your message")
            sb.AppendLine("<textarea name='message' maxlength='4000' required placeholder='Write your reply here...'>" & Enc(message) & "</textarea>")
            sb.AppendLine("</label>")

            ' Signature preview
            sb.AppendLine("<div class='signature'>")
            sb.AppendLine("<div class='signature-title'>Email signature preview</div>")

            If logo <> "" Then
                sb.AppendLine("<img class='signature-logo' src='" & logo & "' alt='" & Enc(company) & "'>")
            End If

            sb.AppendLine("<div class='signature-company'>" & Enc(company) & "</div>")

            If ownerName <> "" Then
                sb.AppendLine("<div class='signature-owner'>" & Enc(ownerName) & "</div>")
            End If

            If ownerEmail <> "" Then
                sb.AppendLine("<div><a href='mailto:" & Enc(ownerEmail) & "'>" & Enc(ownerEmail) & "</a></div>")
            End If

            If website <> "" Then
                Dim safeWebsite As String = SafeUrl(website)
                If safeWebsite <> "" Then
                    sb.AppendLine("<div><a href='" & safeWebsite & "' target='_blank' rel='noopener noreferrer'>" & Enc(website) & "</a></div>")
                End If
            End If

            If address <> "" Then
                sb.AppendLine("<div class='signature-address'>" & Enc(address) & "</div>")
            End If

            sb.AppendLine("</div>")

            ' Send button
            sb.AppendLine("<div class='actions'>")
            sb.AppendLine("<button type='submit' class='send'>Send reply &rarr;</button>")
            sb.AppendLine("<span class='hint'>Your picture and signature are added automatically below your message.</span>")
            sb.AppendLine("</div>")

            sb.AppendLine("</form>")
        End If

        sb.AppendLine("</div>")

        ' Footer
        sb.AppendLine("<div class='footer'>&copy; " & DateTime.Now.Year.ToString() & " " & Enc(company) & ". All rights reserved.</div>")

        sb.AppendLine("</div></body></html>")

        context.Response.Write(sb.ToString())

    End Sub

End Class