<%@ Page Title="Login" Language="C#" MasterPageFile="~/UI/master/Auth.Master"
    AutoEventWireup="true" CodeBehind="Login.aspx.cs"
    Inherits="TrainingApp.Pages.Auth.Login" %>

<%-- Formulario del login SAP NetWeaver real (captura "Login SAP NETWEAVER.png"). Los
     errores del servidor salen en un SweetAlert (AuthMaster.ShowError). --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:Panel ID="PnlLogin" runat="server" CssClass="nw-form" DefaultButton="BtnLogin">
        <asp:Label runat="server" AssociatedControlID="TxtCode1" CssClass="nw-label">
            User <span class="nw-star">*</span>
        </asp:Label>
        <asp:TextBox ID="TxtCode1" runat="server" CssClass="nw-input" MaxLength="50" />
        <asp:RequiredFieldValidator ID="RfvCode1" runat="server"
            ControlToValidate="TxtCode1"
            ErrorMessage="Ingrese su código de empleado."
            CssClass="nw-validation" Display="Dynamic" />

        <asp:Label runat="server" AssociatedControlID="TxtPassword" CssClass="nw-label">
            Password <span class="nw-star">*</span>
        </asp:Label>
        <asp:TextBox ID="TxtPassword" runat="server" TextMode="Password" CssClass="nw-input" />
        <asp:RequiredFieldValidator ID="RfvPassword" runat="server"
            ControlToValidate="TxtPassword"
            ErrorMessage="Ingrese su contraseña."
            CssClass="nw-validation" Display="Dynamic" />

        <div class="nw-actions">
            <asp:Button ID="BtnLogin" runat="server" Text="Log On"
                CssClass="nw-button" OnClick="BtnLogin_Click" />
        </div>
    </asp:Panel>
</asp:Content>
