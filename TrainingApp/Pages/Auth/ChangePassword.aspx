<%@ Page Title="Change Password" Language="C#" MasterPageFile="~/UI/master/Auth.Master"
    AutoEventWireup="true" CodeBehind="ChangePassword.aspx.cs"
    Inherits="TrainingApp.Pages.Auth.ChangePassword" %>

<%-- Cambio de contraseña con el mismo marco y formulario del login NetWeaver. No hay
     captura de esta pantalla del real: los textos (Old/New/Confirm Password, Change) siguen
     los del cambio de contraseña de SAP NetWeaver. Los errores del servidor salen en un
     SweetAlert (AuthMaster.ShowError). --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:PlaceHolder ID="PhFirstLogin" runat="server" Visible="false">
        <p class="nw-message">
            Por seguridad, debes establecer una contraseña personal antes de continuar.
            Como contraseña actual, usa la misma con la que entraste.
        </p>
    </asp:PlaceHolder>

    <asp:Panel ID="PnlChangePassword" runat="server" CssClass="nw-form" DefaultButton="BtnSave">
        <asp:Label runat="server" AssociatedControlID="TxtCurrentPassword" CssClass="nw-label">
            Old Password <span class="nw-star">*</span>
        </asp:Label>
        <asp:TextBox ID="TxtCurrentPassword" runat="server" TextMode="Password" CssClass="nw-input" />
        <asp:RequiredFieldValidator ID="RfvCurrentPassword" runat="server"
            ControlToValidate="TxtCurrentPassword"
            ErrorMessage="Ingrese la contraseña actual."
            CssClass="nw-validation" Display="Dynamic" />

        <asp:Label runat="server" AssociatedControlID="TxtNewPassword" CssClass="nw-label">
            New Password <span class="nw-star">*</span>
        </asp:Label>
        <asp:TextBox ID="TxtNewPassword" runat="server" TextMode="Password" CssClass="nw-input" />
        <asp:RequiredFieldValidator ID="RfvNewPassword" runat="server"
            ControlToValidate="TxtNewPassword"
            ErrorMessage="Ingrese la nueva contraseña."
            CssClass="nw-validation" Display="Dynamic" />
        <asp:RegularExpressionValidator ID="RevPasswordLength" runat="server"
            ControlToValidate="TxtNewPassword"
            ValidationExpression=".{8,}"
            ErrorMessage="La contraseña debe tener al menos 8 caracteres."
            CssClass="nw-validation" Display="Dynamic" />

        <asp:Label runat="server" AssociatedControlID="TxtConfirmPassword" CssClass="nw-label">
            Confirm Password <span class="nw-star">*</span>
        </asp:Label>
        <asp:TextBox ID="TxtConfirmPassword" runat="server" TextMode="Password" CssClass="nw-input" />
        <asp:RequiredFieldValidator ID="RfvConfirmPassword" runat="server"
            ControlToValidate="TxtConfirmPassword"
            ErrorMessage="Confirme la nueva contraseña."
            CssClass="nw-validation" Display="Dynamic" />
        <asp:CompareValidator ID="CvPasswords" runat="server"
            ControlToValidate="TxtConfirmPassword"
            ControlToCompare="TxtNewPassword"
            ErrorMessage="Las contraseñas no coinciden."
            CssClass="nw-validation" Display="Dynamic" />

        <div class="nw-actions">
            <asp:Button ID="BtnSave" runat="server" Text="Change"
                CssClass="nw-button" OnClick="BtnSave_Click" />
            <%-- Sale sin cambiar la contraseña: cierra la sesión y vuelve al Login. --%>
            <asp:Button ID="BtnCancel" runat="server" Text="Cancel"
                CssClass="nw-button" CausesValidation="false" OnClick="BtnCancel_Click" />
        </div>
    </asp:Panel>
</asp:Content>
