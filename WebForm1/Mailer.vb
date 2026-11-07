Imports System.Collections.Generic
Imports System.Configuration
Imports System.Net
Imports System.Net.Mail
Imports System.Text

' Sends the visitor a confirmation, alerts the studio owner, and sends branded replies.
Public Module Mailer

    Private Const Brand As String = "Furthera Pty Ltd"
    Private Const Accent As String = "#c4a05a"
    Private Const AccentText As String = "#8a6a2f"
    Private Const Ink As String = "#241b16"

    ' Line-break characters written with plain .NET so no Visual Basic constants are needed
    Private ReadOnly CR As String = Convert.ToChar(13).ToString()
    Private ReadOnly LF As String = Convert.ToChar(10).ToString()
    Private ReadOnly CRLF As String = Convert.ToChar(13).ToString() & Convert.ToChar(10).ToString()

    ' ---------- helpers ----------

    Private Function Setting(key As String) As String
        Return If(ConfigurationManager.AppSettings(key), "").Trim()
    End Function

    Private Function Clean(value As String) As String
        Return If(value, "").Replace(CR, " ").Replace(LF, " ").Trim()
    End Function

    Private Function Enc(value As String) As String
        Return WebUtility.HtmlEncode(If(value, ""))
    End Function

    Private Function FullName() As String
        Return Clean(Setting("MailFromName"))
    End Function

    Private Function FirstName(name As String) As String
        Dim parts() As String = Clean(name).Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
        Return If(parts.Length > 0, parts(0), "there")
    End Function

    Private Function WebsiteUrl() As String
        Dim w As String = Setting("CompanyWebsite")
        If w = "" Then Return ""
        Return If(w.StartsWith("http", StringComparison.OrdinalIgnoreCase), w, "https://" & w)
    End Function

    ' Full path of the picture used in the signature (Content/signature.png by default), or "" if missing
    Private Function SignatureImagePath() As String
        Dim rel As String = Setting("SignatureImage")
        If rel = "" Then rel = "~/Content/signature.png"
        Try
            Dim p As String = System.Web.Hosting.HostingEnvironment.MapPath(rel)
            If p IsNot Nothing AndAlso System.IO.File.Exists(p) Then Return p
        Catch
        End Try
        Return ""
    End Function

    ' Plain-text signature (used in the plain-text part of emails)
    Private Function SignatureText() As String
        Dim lines As New List(Of String)()
        If FullName() <> "" Then lines.Add(FullName())
        lines.Add(Brand)
        If Setting("OwnerEmail") <> "" Then lines.Add(Setting("OwnerEmail"))
        If WebsiteUrl() <> "" Then lines.Add(WebsiteUrl())
        If Setting("CompanyAddress") <> "" Then lines.Add(Setting("CompanyAddress"))
        Return String.Join(CRLF, lines)
    End Function

    ' Designed signature: picture on the left, details on the right
    Private Function SignatureHtml(withImage As Boolean) As String
        Dim sb As New StringBuilder()
        sb.AppendLine("<table role='presentation' cellpadding='0' cellspacing='0' style='margin-top:28px;border-top:2px solid " & Accent & ";'><tr>")
        If withImage Then
            sb.AppendLine("<td style='padding:20px 20px 0 0;vertical-align:top;'><img src='cid:sigimg' width='88' height='88' alt='' style='display:block;width:88px;height:88px;border:0;border-radius:50%;'></td>")
        End If
        sb.AppendLine("<td style='padding:20px 0 0;vertical-align:top;font-size:14px;line-height:1.7;color:#6f645a;'>")
        If FullName() <> "" Then sb.AppendLine("<div style='font-size:18px;font-weight:600;color:" & Ink & ";'>" & Enc(FullName()) & "</div>")
        sb.AppendLine("<div style='color:" & AccentText & ";font-weight:600;'>" & Enc(Brand) & "</div>")
        If Setting("OwnerEmail") <> "" Then sb.AppendLine("<div><a href='mailto:" & Enc(Setting("OwnerEmail")) & "' style='color:#6f645a;text-decoration:none;'>" & Enc(Setting("OwnerEmail")) & "</a></div>")
        If WebsiteUrl() <> "" Then sb.AppendLine("<div><a href='" & Enc(WebsiteUrl()) & "' style='color:#6f645a;text-decoration:none;'>" & Enc(WebsiteUrl()) & "</a></div>")
        If Setting("CompanyAddress") <> "" Then sb.AppendLine("<div>" & Enc(Setting("CompanyAddress")) & "</div>")
        sb.AppendLine("</td></tr></table>")
        Return sb.ToString()
    End Function

    ' Opening text for a reply (used to prefill the reply page)
    Public Function DefaultReplyText(visitorName As String, interest As String) As String
        Return "Hello " & FirstName(visitorName) & "," & CRLF & CRLF &
               "Thank you for your request (" & Clean(interest) & ")." & CRLF & CRLF &
               "Warm regards,"
    End Function

    ' mailto fallback link (plain text only, no picture possible)
    Private Function ReplyLink(visitorEmail As String, visitorName As String, interest As String) As String
        Dim body As String = DefaultReplyText(visitorName, interest) & CRLF & SignatureText()
        Return "mailto:" & Uri.EscapeDataString(visitorEmail).Replace("%40", "@") &
               "?subject=" & Uri.EscapeDataString("Re: your visit request") &
               "&body=" & Uri.EscapeDataString(body)
    End Function

    ' Link to the reply page on your site (needs SiteUrl in Web.config)
    Private Function ReplyPageUrl(requestId As Integer) As String
        Dim baseUrl As String = Setting("SiteUrl").TrimEnd("/"c)
        Dim key As String = Setting("AdminKey")
        If baseUrl = "" OrElse key = "" OrElse requestId <= 0 Then Return ""
        Return baseUrl & "/Reply.ashx?id=" & requestId & "&key=" & Uri.EscapeDataString(key)
    End Function

    ' ---------- layout ----------

    Private Function Layout(preheader As String, bodyHtml As String, footerText As String) As String
        Dim sb As New StringBuilder()
        sb.AppendLine("<!DOCTYPE html>")
        sb.AppendLine("<html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>")
        sb.AppendLine("<body style='margin:0;padding:0;background:#efe9df;'>")
        sb.AppendLine("<div style='display:none;max-height:0;overflow:hidden;opacity:0;'>" & Enc(preheader) & "</div>")
        sb.AppendLine("<table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#efe9df;'><tr><td align='center' style='padding:32px 12px;'>")
        sb.AppendLine("<table role='presentation' width='600' cellpadding='0' cellspacing='0' style='width:100%;max-width:600px;background:#ffffff;border-radius:16px;overflow:hidden;font-family:Segoe UI,Helvetica,Arial,sans-serif;'>")
        sb.AppendLine("<tr><td style='background:" & Ink & ";padding:28px 36px;border-bottom:4px solid " & Accent & ";'>")
        sb.AppendLine("<span style='font-family:Georgia,serif;font-size:22px;font-style:italic;color:#e8dccb;'>" & Enc(Brand) & "</span></td></tr>")
        sb.AppendLine("<tr><td style='padding:36px;color:#2b2420;font-size:16px;line-height:1.6;'>")
        sb.AppendLine(bodyHtml)
        sb.AppendLine("</td></tr>")
        sb.AppendLine("<tr><td style='padding:20px 36px 28px;background:#f7f3ec;color:#6f645a;font-size:13px;line-height:1.5;'>")
        sb.AppendLine(Enc(footerText) & "<br>" & Enc(Brand))
        sb.AppendLine("</td></tr></table></td></tr></table></body></html>")
        Return sb.ToString()
    End Function

    Private Function Heading(text As String) As String
        Return "<h1 style='margin:0 0 20px;font-family:Georgia,serif;font-weight:normal;font-size:28px;line-height:1.2;color:" & Ink & ";'>" & Enc(text) & "</h1>"
    End Function

    Private Function Row(label As String, valueHtml As String) As String
        Return "<tr><td style='padding:12px 0;border-bottom:1px solid #eee7dc;width:110px;color:#6f645a;font-size:14px;vertical-align:top;'>" & Enc(label) & "</td>" &
               "<td style='padding:12px 0;border-bottom:1px solid #eee7dc;font-size:16px;color:" & Ink & ";'>" & valueHtml & "</td></tr>"
    End Function

    ' Sends a plain-text version plus the designed HTML version (with the signature picture embedded)
    Private Sub Send(msg As MailMessage, plainText As String, html As String, Optional imagePath As String = "")
        msg.IsBodyHtml = False
        msg.Body = plainText

        Dim view As AlternateView = AlternateView.CreateAlternateViewFromString(html, Nothing, "text/html")
        If imagePath <> "" Then
            Dim lr As New LinkedResource(imagePath)
            lr.ContentId = "sigimg"
            lr.TransferEncoding = System.Net.Mime.TransferEncoding.Base64
            view.LinkedResources.Add(lr)
        End If
        msg.AlternateViews.Add(view)

        ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol Or SecurityProtocolType.Tls12
        Using client As New SmtpClient(Setting("SmtpHost"), CInt(Setting("SmtpPort")))
            client.EnableSsl = True
            client.Credentials = New NetworkCredential(Setting("SmtpUser"), Setting("SmtpPassword"))
            client.Send(msg)
        End Using
    End Sub

    ' ---------- email to the visitor (automatic confirmation) ----------

    Public Sub SendVisitConfirmation(toEmail As String, toName As String, interest As String)
        Dim first As String = FirstName(toName)
        Dim img As String = SignatureImagePath()

        Dim body As New StringBuilder()
        body.AppendLine(Heading("Request received"))
        body.AppendLine("<p style='margin:0 0 16px;'>Hello " & Enc(first) & ",</p>")
        body.AppendLine("<p style='margin:0 0 24px;'>Thank you for getting in touch. We have your request and will write back within two days to propose two times for your visit.</p>")
        body.AppendLine("<table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#f7f3ec;border-left:4px solid " & Accent & ";border-radius:8px;'><tr><td style='padding:16px 20px;'>")
        body.AppendLine("<div style='font-size:13px;color:#6f645a;'>Your request</div>")
        body.AppendLine("<div style='font-size:17px;color:" & Ink & ";font-weight:600;'>" & Enc(Clean(interest)) & "</div>")
        body.AppendLine("</td></tr></table>")
        body.AppendLine("<p style='margin:32px 0 0;'>Warm regards,</p>")
        body.AppendLine(SignatureHtml(img <> ""))

        Dim html As String = Layout("We received your visit request and will reply within two days.", body.ToString(),
                                    "You are receiving this email because you submitted a visit request on our website.")
        Dim plain As String = "Hello " & first & "," & CRLF & CRLF &
                              "Thank you for getting in touch (" & Clean(interest) & "). We will write back within two days to propose two times for your visit." &
                              CRLF & CRLF & "Warm regards," & CRLF & SignatureText()

        Using msg As New MailMessage()
            msg.From = New MailAddress(Setting("MailFrom"), FullName())
            msg.To.Add(New MailAddress(toEmail, Clean(toName)))
            msg.Subject = "We received your visit request"
            Send(msg, plain, html, img)
        End Using
    End Sub

    ' ---------- email to the studio (new request alert) ----------

    Public Sub SendOwnerAlert(visitorEmail As String, visitorName As String, interest As String, message As String, requestId As Integer)
        Dim owner As String = Setting("OwnerEmail")
        If owner = "" Then Return

        Dim name As String = Clean(visitorName)
        Dim noteHtml As String = If(String.IsNullOrWhiteSpace(message),
            "<span style='color:#6f645a;font-style:italic;'>No message left.</span>",
            Enc(message.Trim()).Replace(CRLF, "<br>").Replace(LF, "<br>"))

        Dim pageUrl As String = ReplyPageUrl(requestId)
        Dim replyHref As String = If(pageUrl <> "", pageUrl, ReplyLink(visitorEmail, name, interest))

        Dim body As New StringBuilder()
        body.AppendLine(Heading("New visit request"))
        body.AppendLine("<table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='margin:0 0 28px;'>")
        body.AppendLine(Row("Name", Enc(name)))
        body.AppendLine(Row("Email", "<a href='mailto:" & Enc(visitorEmail) & "' style='color:" & Ink & ";'>" & Enc(visitorEmail) & "</a>"))
        body.AppendLine(Row("Interest", Enc(Clean(interest))))
        body.AppendLine(Row("Message", noteHtml))
        body.AppendLine("</table>")
        body.AppendLine("<a href='" & Enc(replyHref) & "' style='display:inline-block;background:" & Accent & ";color:" & Ink & ";text-decoration:none;font-weight:600;padding:14px 28px;border-radius:999px;'>Reply to " & Enc(FirstName(name)) & "</a>")
        If pageUrl <> "" Then
            body.AppendLine("<p style='margin:16px 0 0;font-size:13px;color:#6f645a;'>Opens a reply page with your signature and picture.</p>")
        End If

        Dim html As String = Layout("New visit request from " & name, body.ToString(),
                                    "This alert was sent automatically from your website's visit form.")
        Dim plain As String = "New visit request" & CRLF & CRLF &
                              "Name: " & name & CRLF &
                              "Email: " & visitorEmail & CRLF &
                              "Interest: " & Clean(interest) & CRLF &
                              "Message: " & If(message, "").Trim() & CRLF & CRLF &
                              "Reply: " & replyHref

        Using msg As New MailMessage()
            msg.From = New MailAddress(Setting("MailFrom"), Brand)
            msg.To.Add(New MailAddress(owner))
            msg.ReplyToList.Add(New MailAddress(visitorEmail, name))
            msg.Subject = "New visit request from " & name
            Send(msg, plain, html)
        End Using
    End Sub

    ' ---------- your reply to the visitor (sent from the reply page) ----------

    Public Sub SendReply(toEmail As String, toName As String, subject As String, messageText As String)
        Dim img As String = SignatureImagePath()
        Dim text As String = If(messageText, "").Trim()

        Dim body As New StringBuilder()
        body.AppendLine("<div style='margin:0;'>" & Enc(text).Replace(CRLF, "<br>").Replace(LF, "<br>") & "</div>")
        body.AppendLine(SignatureHtml(img <> ""))

        Dim html As String = Layout("A reply from " & Brand, body.ToString(),
                                    "You are receiving this email because you contacted us through our website.")
        Dim plain As String = text & CRLF & CRLF & SignatureText()

        Using msg As New MailMessage()
            msg.From = New MailAddress(Setting("MailFrom"), If(FullName() <> "", FullName(), Brand))
            msg.To.Add(New MailAddress(toEmail, Clean(toName)))
            If Setting("OwnerEmail") <> "" Then msg.ReplyToList.Add(New MailAddress(Setting("OwnerEmail")))
            msg.Subject = Clean(subject)
            Send(msg, plain, html, img)
        End Using
    End Sub

End Module