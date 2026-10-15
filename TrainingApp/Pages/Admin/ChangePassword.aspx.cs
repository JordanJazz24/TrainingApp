using System;
using System.Web.Security;
using System.Web.UI;
using TrainingApp.BLL.Auth;
using TrainingApp.Shared;
using TrainingApp.UI.Master;

namespace TrainingApp.Pages.Admin
{
    // Cambio de contraseña del admin (spec 013): primer acceso y tras un reset. Las reglas
    // viven en AuthService (auditoría AUD-09); aquí solo se muestra el resultado.
    public partial class ChangePassword : Page
    {
        private readonly AuthService _authService = new AuthService();

        protected void Page_Load(object sender, EventArgs e)
        {
            // Sin sesión: al login de admin (Web.config deja esta página abierta).
            if (!SessionHelper.EnsureUserSession())
            {
                Response.Redirect(SessionHelper.AdminLoginUrl);
                return;
            }

            // Un operador cambia su contraseña en la pantalla de SAP: vuelve a su inicio.
            if (!SessionHelper.CanManageUsers())
            {
                SessionHelper.RedirectHome();
                return;
            }

            if (!IsPostBack)
                PhFirstLogin.Visible = SessionHelper.MustChangePassword();
        }

        protected void BtnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var status = _authService.ChangePassword(User.Identity.Name,
                TxtCurrentPassword.Text, TxtNewPassword.Text);

            switch (status)
            {
                case ChangePasswordStatus.InvalidCurrentPassword:
                    ShowError("La contraseña actual no es correcta.");
                    return;

                case ChangePasswordStatus.NewPasswordIsDefault:
                    ShowError("La nueva contraseña no puede ser la contraseña por defecto.");
                    return;

                case ChangePasswordStatus.UserNotFound:
                    // Cuenta eliminada con el login vigente: se cierra la sesión.
                    FormsAuthentication.SignOut();
                    Session.Abandon();
                    Response.Redirect(SessionHelper.AdminLoginUrl);
                    return;
            }

            Session[Constants.SessionKeys.IsFirstLogin] = false;
            Response.Redirect(SessionHelper.AdminHomeUrl);
        }

        protected void BtnCancel_Click(object sender, EventArgs e)
        {
            SessionHelper.Logout();
        }

        private void ShowError(string message) => ((AdminMaster)Master).ShowMessage("error", "Error", message);
    }
}
