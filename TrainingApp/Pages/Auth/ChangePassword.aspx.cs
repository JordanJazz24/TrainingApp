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
            if (!User.Identity.IsAuthenticated)
                Response.Redirect("~/Pages/Auth/Login.aspx");
        }

        protected void BtnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            if (TxtNewPassword.Text == Constants.DEFAULT_PASSWORD)
            {
                ShowError("La nueva contraseña no puede ser la contraseña por defecto.");
                return;
            }

            var operatorId = Session[Constants.SessionKeys.OperatorId];
            if (operatorId == null)
            {
                Response.Redirect("~/Pages/Auth/Login.aspx");
                return;
            }

            _authService.ActivateAccount((int)operatorId, TxtNewPassword.Text);
            Response.Redirect("~/Pages/Training/Dashboard.aspx");
        }

        private void ShowError(string message)
        {
            LblError.Text    = message;
            LblError.Visible = true;
        }
    }
}
