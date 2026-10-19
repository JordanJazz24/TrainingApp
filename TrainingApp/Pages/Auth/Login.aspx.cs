using System;
using System.Web.Security;
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

                // FirstLogin y Success solo difieren en User.IsFirstLogin, que
                // SetUserSession guarda en la Session (auditoría AUD-08).
                case LoginStatus.FirstLogin:
                    SignIn(result);
                    Response.Redirect(SessionHelper.ChangePasswordUrl);
                    break;

                case LoginStatus.Success:
                    SignIn(result);
                    Response.Redirect(SessionHelper.HomeUrl(result.User.Role));
                    break;
            }
        }

        // SetUserSession descarta una Session de otra cuenta (fijación de sesión, 2026-10-19).
        private static void SignIn(LoginResult result)
        {
            FormsAuthentication.SetAuthCookie(result.User.Code1, false);
            SessionHelper.SetUserSession(result.User, result);
        }

        private void ShowError(string message) => ((AdminMaster)Master).ShowMessage("error", "Error", message);
    }
}
