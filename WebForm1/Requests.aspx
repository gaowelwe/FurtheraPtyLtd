<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="Requests.aspx.vb" Inherits="WebForm1.Requests" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
  <meta charset="utf-8" />
  <title>Visit Requests & Inquiries | Pule Gaowelwe Atelier</title>
  <style>
    body{margin:0;padding:3rem;background:#241b16;color:#e8dccb;font:16px/1.6 'Jost',system-ui,sans-serif}
    h1{font-family:'Fraunces',Georgia,serif;font-weight:300;font-size:2.5rem;margin:0 0 2rem;color:#c4a05a}
    .table-container{background:#32261e;padding:1.5rem;border-radius:1rem;box-shadow:0 10px 30px rgba(0,0,0,0.3)}
    table{border-collapse:collapse;width:100%}
    th,td{padding:.8rem 1rem;text-align:left;border-bottom:1px solid #43342a;vertical-align:top}
    th{color:#c4a05a;font-weight:500;font-size:1.05rem}
    tr:hover td{background:rgba(255,255,255,0.02)}
  </style>
</head>
<body>
<form id="form1" runat="server">
  <h1>Visit Requests & Inquiries</h1>
  <div class="table-container">
    <asp:GridView ID="gvRequests" runat="server" AutoGenerateColumns="true" GridLines="None"
                  EmptyDataText="No visit requests or messages recorded yet." />
  </div>
</form>
</body>
</html>