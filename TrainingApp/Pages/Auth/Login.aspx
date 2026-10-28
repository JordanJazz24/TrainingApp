<%@ Page Title="Iniciar sesión" Language="C#" MasterPageFile="~/UI/master/Admin.Master"
    AutoEventWireup="true" CodeBehind="Login.aspx.cs"
    Inherits="TrainingApp.Pages.Auth.Login" %>

<%-- Login único de la app, neutro, para operadores y administradores (spec 016, D-62). El
     diseño de SAP NetWeaver pasa al login de MES simulado dentro de la sesión (D-63).
     Panel dividido (brief 022, alternativa 3b): identidad a la izquierda y formulario a la
     derecha, a pantalla completa (AdminMaster.FullBleed). Los errores del servidor salen en
     la línea de estado sobre Entrar, ya no en un SweetAlert. --%>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
  <div class="auth-split">
    <aside class="auth-brand" aria-label="TrainingApp">
        <i class="bi bi-display auth-watermark" aria-hidden="true"></i>
        <div class="auth-logo">
            <span class="auth-logo-mark"><i class="bi bi-display" aria-hidden="true"></i></span>TrainingApp
        </div>
        <div class="auth-pitch">
            <span class="auth-eyebrow">Simulador de entrenamiento · SAP MES</span>
            <p class="auth-headline">Entrena y evalúa los flujos de SAP MES de manufactura médica.</p>
        </div>
        <div class="auth-brand-foot">
            <i class="bi bi-people" aria-hidden="true"></i>Un solo acceso para operadores y administradores
        </div>
    </aside>

    <div class="auth-side">
        <div class="auth-side-top">
            <%-- Sin la barra superior, el interruptor de tema va aquí; theme.js lo busca por este
                 id y le cambia el ícono y el texto. --%>
            <button type="button" id="mtThemeToggle" class="btn btn-secondary btn-sm"
                aria-label="Cambiar a modo oscuro">
                <i class="bi bi-moon" aria-hidden="true"></i><span>Oscuro</span>
            </button>
        </div>
        <div class="auth-side-body">
            <asp:Panel ID="PnlLogin" runat="server" CssClass="auth-form" DefaultButton="BtnLogin">
                <h1>Iniciar sesión</h1>
                <p class="auth-lead">Con tu código de empleado y tu contraseña.</p>

                <div class="mb-3">
                    <asp:Label runat="server" AssociatedControlID="TxtCode1" CssClass="form-label">Código de empleado</asp:Label>
                    <div class="input-group">
                        <span class="input-group-text" aria-hidden="true"><i class="bi bi-person-badge"></i></span>
                        <asp:TextBox ID="TxtCode1" runat="server" CssClass="form-control" MaxLength="50" />
                    </div>
                    <asp:RequiredFieldValidator ID="RfvCode1" runat="server"
                        ControlToValidate="TxtCode1"
                        ErrorMessage="Ingrese su código de empleado."
                        CssClass="admin-validation" Display="Dynamic" />
                </div>

                <div>
                    <asp:Label runat="server" AssociatedControlID="TxtPassword" CssClass="form-label">Contraseña</asp:Label>
                    <div class="input-group">
                        <span class="input-group-text" aria-hidden="true"><i class="bi bi-key"></i></span>
                        <asp:TextBox ID="TxtPassword" runat="server" TextMode="Password" CssClass="form-control" />
                    </div>
                    <asp:RequiredFieldValidator ID="RfvPassword" runat="server"
                        ControlToValidate="TxtPassword"
                        ErrorMessage="Ingrese su contraseña."
                        CssClass="admin-validation" Display="Dynamic" />
                </div>

                <%-- Error del servidor; el alto está reservado para que Entrar no salte. Sin
                     ViewState: el mensaje es solo de este postback. --%>
                <div class="mt-status-line" role="status" aria-live="polite">
                    <asp:PlaceHolder ID="PhError" runat="server" Visible="false" EnableViewState="false">
                        <i class="bi bi-exclamation-octagon-fill" aria-hidden="true"></i><asp:Literal ID="LitError" runat="server" Mode="Encode" />
                    </asp:PlaceHolder>
                </div>

                <asp:LinkButton ID="BtnLogin" runat="server" CssClass="btn btn-primary w-100"
                    OnClick="BtnLogin_Click">
                    <i class="bi bi-box-arrow-in-right" aria-hidden="true"></i>Entrar</asp:LinkButton>

                <div class="auth-note">
                    <i class="bi bi-info-circle" aria-hidden="true"></i>
                    <span><strong>¿Primer acceso?</strong> Entra con la contraseña por defecto; enseguida te pedirá crear una nueva.</span>
                </div>
            </asp:Panel>
        </div>
    </div>
  </div>
</asp:Content>
