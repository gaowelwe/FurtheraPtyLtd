<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="Default.aspx.vb" Inherits="WebForm1._Default" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Pule Gaowelwe | Chief Developer Officer </title>
  <link rel="preconnect" href="https://fonts.googleapis.com" />
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
  <link href="https://fonts.googleapis.com/css2?family=Fraunces:ital,opsz,wght,SOFT@0,9..144,300..700,100;1,9..144,300..500,100&family=Jost:wght@300;400;500&display=swap" rel="stylesheet" />
  <link rel="stylesheet" href="Content/site.css" />
</head>
<body class="is-loading">
<form id="form1" runat="server">
  <header class="nav">
    <a href="#top" class="mark">Pule Gaowelwe</a>
    <nav><a href="#story">Our story</a><a href="#collection">Collection</a><a href="#visit">Visit</a></nav>
  </header>

  <main id="top">
    <section class="hero">
      <div class="blob blob-a" data-depth="18"></div>
      <div class="blob blob-b" data-depth="-26"></div>
      <h1><span>Made slowly,</span> <span>from earth</span> <span>and fibre.</span></h1>
      <p class="lede">A small atelier for stoneware, plant-dyed wool and solid walnut. Every piece is shaped by hand, in lots of twelve or fewer.</p>
      <a class="btn" href="#collection">Explore the collection</a>
    </section>

    <svg class="wave" viewBox="0 0 1440 120" preserveAspectRatio="none" aria-hidden="true"><path d="M0,60 C240,120 480,0 720,50 C960,100 1200,10 1440,60 L1440,120 L0,120Z"/></svg>

    <section id="story" class="story">
      <p class="pull">We begin each piece with a material and wait for it to tell us what it wants to become.</p>
      <div class="story-cols">
        <p>Clay is dug within a day's drive of the studio. Wool is dyed in vats of walnut hull, madder and lichen. Walnut is milled from fallen trees, never felled ones.</p>
        <p>Nothing here is rushed, so nothing here is identical. The variations in glaze, weave and grain are the signature of the hands that made it.</p>
      </div>
    </section>

    <section id="collection" class="collection">
      <h2>The current collection</h2>
      <div class="chips" role="group" aria-label="Filter by material">
        <button type="button" class="chip active" data-filter="all">All pieces</button>
        <asp:Repeater ID="rptCats" runat="server">
          <ItemTemplate><button type="button" class="chip" data-filter="<%# Container.DataItem %>"><%# Container.DataItem %></button></ItemTemplate>
        </asp:Repeater>
      </div>
      <ul class="pieces">
        <asp:Repeater ID="rptPieces" runat="server">
          <ItemTemplate>
            <li class="piece" data-cat="<%#: Eval("Category") %>" tabindex="0">
              <span class="piece-cat"><%#: Eval("Category") %></span>
              <h3><%#: Eval("Name") %></h3>
              <p class="material"><%#: Eval("Material") %></p>
              <p class="note"><%#: Eval("Note") %></p>
            </li>
          </ItemTemplate>
        </asp:Repeater>
      </ul>
    </section>

    <section id="visit" class="visit">
      <div class="visit-copy">
        <h2>Come and hold something</h2>
        <p>Studio visits are by appointment, with tea and no obligation. Send a note and we will propose two times.</p>
      </div>
      <div class="visit-form">
        <asp:Panel ID="pnlConfirm" runat="server" Visible="false">
          <p class="confirm" role="status"><asp:Literal ID="litConfirm" runat="server" /></p>
        </asp:Panel>
        <asp:Panel ID="pnlForm" runat="server" CssClass="fields">
          <label>Your name
            <asp:TextBox ID="txtName" runat="server" MaxLength="80" autocomplete="name" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtName" Display="Dynamic" CssClass="field-validation-error" ErrorMessage="Tell us your name." />
          </label>
          <label>Email
            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" autocomplete="email" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail" Display="Dynamic" CssClass="field-validation-error" ErrorMessage="Add an email so we can reply." />
            <asp:RegularExpressionValidator runat="server" ControlToValidate="txtEmail" Display="Dynamic" CssClass="field-validation-error" ErrorMessage="That email doesn't look right." ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$" />
          </label>
          <label>I would like
            <asp:DropDownList ID="ddlInterest" runat="server">
              <asp:ListItem>Private viewing</asp:ListItem>
              <asp:ListItem>A commissioned piece</asp:ListItem>
              <asp:ListItem>Wholesale enquiry</asp:ListItem>
            </asp:DropDownList>
          </label>
          <label>A few words (optional)
            <asp:TextBox ID="txtMessage" runat="server" TextMode="MultiLine" Rows="3" MaxLength="600" />
          </label>
          <div class="submit-row">
            <asp:Button ID="btnRequest" runat="server" CssClass="btn" Text="Request a visit" />
            <span id="sendSpinner" class="send-status" hidden aria-live="polite">
              <span class="spinner" aria-hidden="true"></span>
              <span>Sending your request…</span>
            </span>
          </div>
        </asp:Panel>
      </div>
    </section>
  </main>

  <footer>Pule Gaowelwe · Handmade in small lots</footer>
</form>
<script src="Scripts/Javascript.js"></script>
</body>
</html>