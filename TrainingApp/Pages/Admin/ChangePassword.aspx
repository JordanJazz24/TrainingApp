<%@ Page Title="Cambio de contraseña" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="ChangePassword.aspx.cs"
    Inherits="TrainingApp.Pages.Admin.ChangePassword" %>

<%-- Cambio de contraseña del admin (spec 013, AC-2d): misma lógica que el de los operadores
     (AuthService.ChangePassword), con el diseño administrativo. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:Panel ID="PnlChangePassword" runat="server" CssClass="admin-card" DefaultButton="BtnSave">
        <h1>Cambio de contraseña</h1>

        <asp:PlaceHolder ID="PhFirstLogin" runat="server" Visible="false">
            <p class="text-muted small">
                Por seguridad, debes establecer una contraseña personal antes de continuar.
                Como contraseña actual, usa la misma con la que entraste.
            </p>
        </asp:PlaceHolder>

        <div class="mb-3">
            <asp:Label runat="server" AssociatedControlID="TxtCurrentPassword" CssClass="form-label">Contraseña actual</asp:Label>
            <asp:TextBox ID="TxtCurrentPassword" runat="server" TextMode="Password" CssClass="form-control" />
            <asp:RequiredFieldValidator ID="RfvCurrentPassword" runat="server"
                ControlToValidate="TxtCurrentPassword"
                ErrorMessage="Ingrese la contraseña actual."
                CssClass="admin-validation" Display="Dynamic" />
        </div>

        <div class="mb-3">
            <asp:Label runat="server" AssociatedControlID="TxtNewPassword" CssClass="form-label">Nueva contraseña</asp:Label>
            <asp:TextBox ID="TxtNewPassword" runat="server" TextMode="Password" CssClass="form-control" />
            <asp:RequiredFieldValidator ID="RfvNewPassword" runat="server"
                ControlToValidate="TxtNewPassword"
                ErrorMessage="Ingrese la nueva contraseña."
                CssClass="admin-validation" Display="Dynamic" />
            <asp:RegularExpressionValidator ID="RevPasswordLength" runat="server"
                ControlToValidate="TxtNewPassword"
                ValidationExpression=".{8,}"
                ErrorMessage="La contraseña debe tener al menos 8 caracteres."
                CssClass="admin-validation" Display="Dynamic" />
        </div>

        <div class="mb-4">
            <asp:Label runat="server" AssociatedControlID="TxtConfirmPassword" CssClass="form-label">Confirmar contraseña</asp:Label>
            <asp:TextBox ID="TxtConfirmPassword" runat="server" TextMode="Password" CssClass="form-control" />
            <asp:RequiredFieldValidator ID="RfvConfirmPassword" runat="server"
                ControlToValidate="TxtConfirmPassword"
                ErrorMessage="Confirme la nueva contraseña."
                CssClass="admin-validation" Display="Dynamic" />
            <asp:CompareValidator ID="CvPasswords" runat="server"
                ControlToValidate="TxtConfirmPassword"
                ControlToCompare="TxtNewPassword"
                ErrorMessage="Las contraseñas no coinciden."
                CssClass="admin-validation" Display="Dynamic" />
        </div>

        <div class="d-flex gap-2">
            <asp:Button ID="BtnSave" runat="server" Text="Guardar"
                CssClass="btn btn-primary flex-fill" OnClick="BtnSave_Click" />
            <%-- Sale sin cambiar la contraseña: cierra la sesión y vuelve al login de admin. --%>
            <asp:Button ID="BtnCancel" runat="server" Text="Cancelar"
                CssClass="btn btn-outline-secondary flex-fill" CausesValidation="false" OnClick="BtnCancel_Click" />
        </div>
    </asp:Panel>
</asp:Content>
