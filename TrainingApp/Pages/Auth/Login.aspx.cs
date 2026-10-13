using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using TrainingApp.BLL.Auth;
using TrainingApp.UI.Master;

namespace TrainingApp.Pages.Auth
{
    public partial class Login : Page
    {
        private readonly AuthService _authService = new AuthService();

        // Login de SAP NetWeaver: es parte de la simulación y solo para operadores (spec 013).
        protected void Page_Load(object sender, EventArgs e)
        {
            if (User.Identity.IsAuthenticated)
            {
                SessionHelper.RedirectHome();
                return;
            }

            // Forms Authentication tiene un solo loginUrl (este): un request sin sesión al módulo
            // administrativo sigue al login de admin (AC-2c).
            if (IsAdminReturnUrl(Request.QueryString["ReturnUrl"]))
                Response.Redirect(SessionHelper.AdminLoginUrl);
        }

        private static bool IsAdminReturnUrl(string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl) || !returnUrl.StartsWith("/")) return false;
            try
            {
                return VirtualPathUtility.ToAppRelative(returnUrl)
                    .StartsWith("~/Pages/Admin/", StringComparison.OrdinalIgnoreCase);
            }
            catch (HttpException)
            {
                // Ruta inválida (ej. "/../x", que sale de la app): no es del módulo.
                return false;
            }
        }

        protected void BtnLogin_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            LoginResult result;
            try
            {
                result = _authService.Login(TxtCode1.Text.Trim(), TxtPassword.Text, LoginPortal.Training);
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

                // Un admin en el login de SAP (spec 013, AC-2b): no entra y no suma un fallo.
                case LoginStatus.WrongPortal:
                    ((AuthMaster)Master).ShowError("Use el acceso administrativo.",
                        "Ir al acceso administrativo", ResolveUrl(SessionHelper.AdminLoginUrl));
                    break;

                // FirstLogin y Success solo difieren en User.IsFirstLogin, que
                // SetUserSession guarda en la Session (auditoría AUD-08).
                case LoginStatus.FirstLogin:
                    FormsAuthentication.SetAuthCookie(result.User.Code1, false);
                    SessionHelper.SetUserSession(result.User);
                    Response.Redirect(SessionHelper.TrainingChangePasswordUrl);
                    break;

                case LoginStatus.Success:
                    FormsAuthentication.SetAuthCookie(result.User.Code1, false);
                    SessionHelper.SetUserSession(result.User);
                    Response.Redirect(SessionHelper.TrainingHomeUrl);
                    break;
            }
        }

        private void ShowError(string message) => ((AuthMaster)Master).ShowError(message);
    }
}
