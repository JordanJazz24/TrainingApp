using System;
using System.Web.Security;
using System.Web.UI;
using TrainingApp.BLL.Auth;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Auth
{
    public partial class ChangePassword : Page
    {
        private readonly AuthService _authService = new AuthService();

        protected void Page_Load(object sender, EventArgs e)
        {
            // Re-hidrata la Session si se perdió con el login vigente (auditoría AUD-10).
            if (!SessionHelper.EnsureOperatorSession())
            {
                Response.Redirect("~/Pages/Auth/Login.aspx");
                return;
            }

            if (!IsPostBack)
                PhFirstLogin.Visible = SessionHelper.MustChangePassword();
        }

        // Las reglas (contraseña actual correcta, nueva distinta de la por defecto) viven en
        // AuthService (auditoría AUD-09); aquí solo se muestra el resultado.
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

                case ChangePasswordStatus.OperatorNotFound:
                    // Operador eliminado con el login vigente: se cierra la sesión.
                    FormsAuthentication.SignOut();
                    Session.Abandon();
                    Response.Redirect("~/Pages/Auth/Login.aspx");
                    return;
            }

            Session[Constants.SessionKeys.IsFirstLogin] = false;
            Response.Redirect("~/Pages/Training/StartSession.aspx");
        }

        private void ShowError(string message)
        {
            LblError.Text    = message;
            LblError.Visible = true;
        }
    }
}
