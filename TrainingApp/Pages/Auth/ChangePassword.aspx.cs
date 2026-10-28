using System;
using System.Web.UI;
using TrainingApp.BLL.Auth;
using TrainingApp.Shared;
using TrainingApp.UI.Master;

namespace TrainingApp.Pages.Auth
{
    // Cambio de contraseña único para los dos roles (spec 016, D-62): primer acceso y tras un
    // reset (spec 013). Las reglas (contraseña actual correcta, nueva distinta de la por
    // defecto) viven en AuthService (auditoría AUD-09); aquí solo se muestra el resultado.
    public partial class ChangePassword : Page
    {
        private readonly AuthService _authService = new AuthService();

        // Pantalla completa (brief 022, 2a): sin la barra superior ni el menú para ningún rol
        // hasta guardar; la página trae su propio interruptor de tema.
        protected void Page_Init(object sender, EventArgs e)
        {
            ((AdminMaster)Master).FullBleed = true;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Re-hidrata la Session si se perdió con el login vigente (auditoría AUD-10).
            if (!SessionHelper.EnsureUserSession())
            {
                Response.Redirect(SessionHelper.LoginUrl);
                return;
            }

            if (!IsPostBack)
                PhFirstLogin.Visible = PhSteps.Visible = SessionHelper.MustChangePassword();
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
                    SessionHelper.Logout();
                    return;
            }

            Session[Constants.SessionKeys.IsFirstLogin] = false;
            SessionHelper.RedirectHome();
        }

        // Salir sin cambiar la contraseña: cierra la sesión y vuelve al login.
        protected void BtnCancel_Click(object sender, EventArgs e)
        {
            SessionHelper.Logout();
        }

        // Error del servidor en la línea de estado sobre Guardar (brief 022, 2a); ya no en SweetAlert.
        private void ShowError(string message)
        {
            LitError.Text = message;
            PhError.Visible = true;
        }
    }
}
