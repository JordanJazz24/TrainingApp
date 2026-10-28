using System;
using System.Web.UI;
using TrainingApp.BLL.Auth;
using TrainingApp.UI.Master;

namespace TrainingApp.Pages.Auth
{
    // Login único de la app (spec 016, D-62): los dos roles entran aquí y cada uno va a su
    // inicio. Es el loginUrl de Forms Authentication.
    public partial class Login : Page
    {
        private readonly AuthService _authService = new AuthService();

        // Panel dividido a pantalla completa (brief 022, 3b): sin la barra del master.
        protected void Page_Init(object sender, EventArgs e)
        {
            ((AdminMaster)Master).FullBleed = true;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (User.Identity.IsAuthenticated)
                SessionHelper.RedirectHome();
        }

        protected void BtnLogin_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            LoginResult result;
            try
            {
                result = _authService.Login(TxtCode1.Text.Trim(), TxtPassword.Text);
            }
            catch (InvalidOperationException)
            {
                ShowError("El servicio de validación no está disponible. Intente más tarde.");
                return;
            }

            switch (result.Status)
            {
                case LoginStatus.EmployeeNotFound:
                    ShowError("Código de empleado no registrado.");
                    break;

                case LoginStatus.InvalidCredentials:
                    ShowError("Contraseña incorrecta.");
                    break;

                // Cuenta bloqueada por logins fallidos (spec 013, AC-14/15).
                case LoginStatus.Locked:
                    ShowError(SessionHelper.LockedMessage(result.LockedUntil.Value));
                    break;

                // FirstLogin y Success solo difieren en User.IsFirstLogin, que la página
                // siguiente lee de la BD al llenar la Session (auditoría AUD-08). SignIn deja la
                // Session con un ID nuevo (AUD-28).
                case LoginStatus.FirstLogin:
                    SessionHelper.SignIn(result);
                    Response.Redirect(SessionHelper.ChangePasswordUrl);
                    break;

                case LoginStatus.Success:
                    SessionHelper.SignIn(result);
                    Response.Redirect(SessionHelper.HomeUrl(result.User.Role));
                    break;
            }
        }

        // Error del servidor en la línea de estado sobre Entrar (brief 022, 3b); ya no en SweetAlert.
        private void ShowError(string message)
        {
            LitError.Text = message;
            PhError.Visible = true;
        }
    }
}
