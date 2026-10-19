using System;
using System.Web;
using TrainingApp.Pages;
using TrainingApp.Shared;

namespace TrainingApp.UI.Master
{
    public partial class AdminMaster : AntiXsrfMasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Barra del usuario solo con un admin autenticado. En el login y en el cambio de
            // contraseña de un operador, el nombre neutro de la app (spec 016, D-80).
            bool signedIn = Context.User.Identity.IsAuthenticated && SessionHelper.CanManageUsers()
                && Session[Constants.SessionKeys.Code1] != null;
            LitBrand.Text  = signedIn ? "MES Training · Administración" : "Simulador de entrenamiento MES";
            PhUser.Visible = signedIn;
            if (signedIn)
            {
                LitUser.Text      = HttpUtility.HtmlEncode(Session[Constants.SessionKeys.Code1].ToString());
                LitLastLogin.Text = HttpUtility.HtmlEncode(SessionHelper.LastAccessText() ?? string.Empty);
            }
        }

        protected void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionHelper.Logout();
        }

        // Mensajes del servidor en un SweetAlert (icon: success, error, warning, info).
        public void ShowMessage(string icon, string title, string text)
        {
            string script = string.Format(
                "Swal.fire({{ icon: '{0}', title: '{1}', text: '{2}' }});",
                HttpUtility.JavaScriptStringEncode(icon),
                HttpUtility.JavaScriptStringEncode(title),
                HttpUtility.JavaScriptStringEncode(text));
            Page.ClientScript.RegisterStartupScript(GetType(), "adminMessage", script, true);
        }
    }
}
