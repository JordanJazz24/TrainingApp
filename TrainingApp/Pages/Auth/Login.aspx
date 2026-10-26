<%@ Page Title="Iniciar sesión" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Login.aspx.cs"
    Inherits="TrainingApp.Pages.Auth.Login" %>

<%-- Login único de la app, neutro, para operadores y administradores (spec 016, D-62). El
     diseño de SAP NetWeaver pasa al login de MES simulado dentro de la sesión (D-63). Los
     errores del servidor salen en un SweetAlert (AdminMaster.ShowMessage). --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
  <div class="admin-auth">
    <asp:Panel ID="PnlLogin" runat="server" CssClass="admin-card" DefaultButton="BtnLogin">
        <h1 class="mb-4">Iniciar sesión</h1>

        <div class="mb-3">
            <asp:Label runat="server" AssociatedControlID="TxtCode1" CssClass="form-label">Código de empleado</asp:Label>
            <asp:TextBox ID="TxtCode1" runat="server" CssClass="form-control" MaxLength="50" />
            <asp:RequiredFieldValidator ID="RfvCode1" runat="server"
                ControlToValidate="TxtCode1"
                ErrorMessage="Ingrese su código de empleado."
                CssClass="admin-validation" Display="Dynamic" />
        </div>

        <div class="mb-4">
            <asp:Label runat="server" AssociatedControlID="TxtPassword" CssClass="form-label">Contraseña</asp:Label>
            <asp:TextBox ID="TxtPassword" runat="server" TextMode="Password" CssClass="form-control" />
            <asp:RequiredFieldValidator ID="RfvPassword" runat="server"
                ControlToValidate="TxtPassword"
                ErrorMessage="Ingrese su contraseña."
                CssClass="admin-validation" Display="Dynamic" />
        </div>

        <asp:Button ID="BtnLogin" runat="server" Text="Entrar"
            CssClass="btn btn-primary w-100" OnClick="BtnLogin_Click" />
    </asp:Panel>
  </div>
</asp:Content>
