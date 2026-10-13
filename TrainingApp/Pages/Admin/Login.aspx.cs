using System;
using System.Web.Security;
using System.Web.UI;
using TrainingApp.BLL.Auth;
using TrainingApp.UI.Master;

namespace TrainingApp.Pages.Admin
{
    // Login del módulo administrativo (spec 013). Acepta solo a quien puede administrar; el
    // operador recibe "Sin acceso administrativo" sin sumar un fallo (AC-2b).
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
                result = _authService.Login(TxtCode1.Text.Trim(), TxtPassword.Text, LoginPortal.Admin);
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

                case LoginStatus.WrongPortal:
                    ShowError("Sin acceso administrativo.");
                    break;

                // FirstLogin y Success solo difieren en User.IsFirstLogin, que SetUserSession
                // guarda en la Session (AC-6: el admin creado por script cambia su contraseña).
                case LoginStatus.FirstLogin:
                    FormsAuthentication.SetAuthCookie(result.User.Code1, false);
                    SessionHelper.SetUserSession(result.User);
                    Response.Redirect(SessionHelper.AdminChangePasswordUrl);
                    break;

                case LoginStatus.Success:
                    FormsAuthentication.SetAuthCookie(result.User.Code1, false);
                    SessionHelper.SetUserSession(result.User);
                    Response.Redirect(SessionHelper.AdminHomeUrl);
                    break;
            }
        }

        private void ShowError(string message) => ((AdminMaster)Master).ShowMessage("error", "Error", message);
    }
}
