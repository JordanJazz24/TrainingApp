<%@ Page Title="Acceso administrativo" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Login.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.Login" %>

<%-- Login del módulo administrativo (spec 013): el de SAP NetWeaver queda para los operadores.
     Misma lógica (AuthService.Login), misma cookie y mismo bloqueo. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:Panel ID="PnlLogin" runat="server" CssClass="admin-card" DefaultButton="BtnLogin">
        <h1>Acceso administrativo</h1>

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
</asp:Content>
