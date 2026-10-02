<%@ Page Title="Cambiar Contraseña" Language="C#" MasterPageFile="~/UI/master/Site.Master"
    AutoEventWireup="true" CodeBehind="ChangePassword.aspx.cs"
    Inherits="TrainingApp.Pages.Auth.ChangePassword" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row justify-content-center mt-5">
        <div class="col-md-4">
            <h2 class="mb-2">Cambiar Contraseña</h2>
            <asp:PlaceHolder ID="PhFirstLogin" runat="server" Visible="false">
                <p class="text-muted mb-4">
                    Por seguridad, debes establecer una contraseña personal antes de continuar.
                    Como contraseña actual, usa la misma con la que entraste.
                </p>
            </asp:PlaceHolder>

            <asp:Label ID="LblError" runat="server" CssClass="alert alert-danger d-block mb-3"
                Visible="false" />
            <asp:Label ID="LblSuccess" runat="server" CssClass="alert alert-success d-block mb-3"
                Visible="false" />

            <div class="mb-3">
                <label for="TxtCurrentPassword" class="form-label">Contraseña actual</label>
                <asp:TextBox ID="TxtCurrentPassword" runat="server" TextMode="Password"
                    CssClass="form-control" />
                <asp:RequiredFieldValidator ID="RfvCurrentPassword" runat="server"
                    ControlToValidate="TxtCurrentPassword"
                    ErrorMessage="Ingrese la contraseña actual."
                    CssClass="text-danger small" Display="Dynamic" />
            </div>

            <div class="mb-3">
                <label for="TxtNewPassword" class="form-label">Nueva contraseña</label>
                <asp:TextBox ID="TxtNewPassword" runat="server" TextMode="Password"
                    CssClass="form-control" />
                <asp:RequiredFieldValidator ID="RfvNewPassword" runat="server"
                    ControlToValidate="TxtNewPassword"
                    ErrorMessage="Ingrese la nueva contraseña."
                    CssClass="text-danger small" Display="Dynamic" />
                <asp:RegularExpressionValidator ID="RevPasswordLength" runat="server"
                    ControlToValidate="TxtNewPassword"
                    ValidationExpression=".{8,}"
                    ErrorMessage="La contraseña debe tener al menos 8 caracteres."
                    CssClass="text-danger small" Display="Dynamic" />
            </div>

            <div class="mb-4">
                <label for="TxtConfirmPassword" class="form-label">Confirmar contraseña</label>
                <asp:TextBox ID="TxtConfirmPassword" runat="server" TextMode="Password"
                    CssClass="form-control" />
                <asp:RequiredFieldValidator ID="RfvConfirmPassword" runat="server"
                    ControlToValidate="TxtConfirmPassword"
                    ErrorMessage="Confirme la nueva contraseña."
                    CssClass="text-danger small" Display="Dynamic" />
                <asp:CompareValidator ID="CvPasswords" runat="server"
                    ControlToValidate="TxtConfirmPassword"
                    ControlToCompare="TxtNewPassword"
                    ErrorMessage="Las contraseñas no coinciden."
                    CssClass="text-danger small" Display="Dynamic" />
            </div>

            <asp:Button ID="BtnSave" runat="server" Text="Guardar"
                CssClass="btn btn-primary w-100" OnClick="BtnSave_Click" />
        </div>
    </div>
</asp:Content>
