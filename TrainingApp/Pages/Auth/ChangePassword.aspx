<%@ Page Title="Cambio de contraseña" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="ChangePassword.aspx.cs"
    Inherits="TrainingApp.Pages.Auth.ChangePassword" %>

<%-- Cambio de contraseña único, neutro, para los dos roles (spec 016, D-62): primer acceso y
     tras un reset (spec 013). Franja de marca a pantalla completa (brief 022, alternativa 2a;
     AdminMaster.FullBleed): ningún rol ve el menú ni Cerrar sesión hasta guardar. Los errores
     del servidor salen en la línea de estado, ya no en un SweetAlert. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
  <div class="pw-page">
    <div class="pw-band">
      <div class="pw-band-top">
        <span class="pw-logo"><span class="pw-logo-mark"><i class="bi bi-display" aria-hidden="true"></i></span>TrainingApp</span>
        <%-- Sin la barra superior, el interruptor va aquí; theme.js lo busca por este id. --%>
        <button type="button" id="mtThemeToggle" class="btn btn-secondary btn-sm" aria-label="Cambiar a modo oscuro">
          <i class="bi bi-moon" aria-hidden="true"></i><span>Oscuro</span>
        </button>
      </div>

      <%-- Solo en primer acceso o tras un reset (misma condición que PhFirstLogin). --%>
      <asp:PlaceHolder ID="PhSteps" runat="server" Visible="false">
        <ol class="mt-steps" aria-label="Primer acceso">
          <li class="mt-step is-done"><span class="mt-step-mark"><i class="bi bi-check-lg" aria-hidden="true"></i></span>Entraste</li>
          <li class="mt-step is-current" aria-current="step"><span class="mt-step-mark">2</span>Contraseña personal</li>
          <li class="mt-step is-todo"><span class="mt-step-mark">3</span>Tu inicio</li>
        </ol>
      </asp:PlaceHolder>
    </div>

    <asp:Panel ID="PnlChangePassword" runat="server" CssClass="admin-card pw-card" DefaultButton="BtnSave">
      <h1>Cambio de contraseña</h1>

      <asp:PlaceHolder ID="PhFirstLogin" runat="server" Visible="false">
        <p class="admin-card-lead">
          Por seguridad, debes establecer una contraseña personal antes de continuar.
          Como contraseña actual, usa la misma con la que entraste.
        </p>
      </asp:PlaceHolder>

      <div class="mb-3">
        <asp:Label runat="server" AssociatedControlID="TxtCurrentPassword" CssClass="form-label">Contraseña actual</asp:Label>
        <div class="input-group">
          <span class="input-group-text" aria-hidden="true"><i class="bi bi-key"></i></span>
          <asp:TextBox ID="TxtCurrentPassword" runat="server" TextMode="Password" CssClass="form-control" />
        </div>
        <asp:RequiredFieldValidator ID="RfvCurrentPassword" runat="server"
            ControlToValidate="TxtCurrentPassword"
            ErrorMessage="Ingrese la contraseña actual."
            CssClass="admin-validation" Display="Dynamic" />
      </div>

      <div class="pw-pair">
        <div>
          <asp:Label runat="server" AssociatedControlID="TxtNewPassword" CssClass="form-label">Nueva contraseña</asp:Label>
          <div class="input-group">
            <span class="input-group-text" aria-hidden="true"><i class="bi bi-shield-lock"></i></span>
            <asp:TextBox ID="TxtNewPassword" runat="server" TextMode="Password" CssClass="form-control"
                aria-describedby="pwRules" />
          </div>
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
        <div>
          <asp:Label runat="server" AssociatedControlID="TxtConfirmPassword" CssClass="form-label">Confirmar contraseña</asp:Label>
          <div class="input-group">
            <span class="input-group-text" aria-hidden="true"><i class="bi bi-shield-check"></i></span>
            <asp:TextBox ID="TxtConfirmPassword" runat="server" TextMode="Password" CssClass="form-control"
                aria-describedby="pwRules" />
          </div>
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
      </div>

      <%-- Requisitos que ya valida la app (página y AuthService). Estáticos: no se marcan en vivo. --%>
      <ul class="pw-rules" id="pwRules" aria-label="Requisitos de la contraseña">
        <li><i class="bi bi-dot" aria-hidden="true"></i>Al menos 8 caracteres</li>
        <li><i class="bi bi-dot" aria-hidden="true"></i>Distinta de la contraseña por defecto</li>
        <li><i class="bi bi-dot" aria-hidden="true"></i>Igual en Nueva y Confirmar</li>
      </ul>

      <%-- Error del servidor; el alto está reservado para que Guardar no salte. Sin ViewState:
           el mensaje es solo de este postback. --%>
      <div class="mt-status-line" role="status" aria-live="polite">
        <asp:PlaceHolder ID="PhError" runat="server" Visible="false" EnableViewState="false">
          <i class="bi bi-exclamation-octagon-fill" aria-hidden="true"></i><asp:Literal ID="LitError" runat="server" Mode="Encode" />
        </asp:PlaceHolder>
      </div>

      <asp:LinkButton ID="BtnSave" runat="server" CssClass="btn btn-primary w-100" OnClick="BtnSave_Click">
        <i class="bi bi-check-lg" aria-hidden="true"></i>Guardar y continuar</asp:LinkButton>
      <%-- Sale sin cambiar la contraseña: cierra la sesión y vuelve al login. --%>
      <asp:LinkButton ID="BtnCancel" runat="server" CssClass="btn btn-link pw-cancel"
          CausesValidation="false" OnClick="BtnCancel_Click">
        <i class="bi bi-box-arrow-left" aria-hidden="true"></i>Cancelar y cerrar sesión</asp:LinkButton>
    </asp:Panel>
  </div>
</asp:Content>
