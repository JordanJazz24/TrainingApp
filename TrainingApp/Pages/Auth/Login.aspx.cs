using System;
using System.Web.Security;
using System.Web.UI;
using TrainingApp.BLL.Auth;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Auth
{
    public partial class Login : Page
    {
        private readonly AuthService _authService = new AuthService();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (User.Identity.IsAuthenticated)
                Response.Redirect("~/Pages/Training/StartSession.aspx");
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

                case LoginStatus.FirstLogin:
                    FormsAuthentication.SetAuthCookie(result.Operator.Code1, false);
                    SetSession(result.Operator, isFirstLogin: true);
                    Response.Redirect("~/Pages/Auth/ChangePassword.aspx");
                    break;

                case LoginStatus.Success:
                    FormsAuthentication.SetAuthCookie(result.Operator.Code1, false);
                    SetSession(result.Operator, isFirstLogin: false);
                    Response.Redirect("~/Pages/Training/StartSession.aspx");
                    break;
            }
        }

        private void SetSession(Operator op, bool isFirstLogin)
        {
            Session[Constants.SessionKeys.OperatorId]   = op.Id;
            Session[Constants.SessionKeys.Code1]        = op.Code1;
            Session[Constants.SessionKeys.Name]         = op.Name;
            Session[Constants.SessionKeys.IsFirstLogin] = isFirstLogin;
        }

        private void ShowError(string message)
        {
            LblError.Text    = message;
            LblError.Visible = true;
        }
    }
}
