using System;
using System.Web;
using TrainingApp.Pages;
using TrainingApp.Shared;
using TrainingApp.UI.Master;

namespace TrainingApp
{
    // Inicio del operador (StartSession). La barra muestra al usuario autenticado y su
    // Cerrar sesión (D-13, D-163); la página ya validó la sesión y el rol.
    public partial class SiteMaster : AntiXsrfMasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            bool signedIn = Context.User.Identity.IsAuthenticated && Session[Constants.SessionKeys.Code1] != null;
            PhUser.Visible = signedIn;
            if (!signedIn) return;

            string code1 = Session[Constants.SessionKeys.Code1].ToString();
            string name  = Session[Constants.SessionKeys.Name] as string;
            LitUser.Text      = HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(name) ? code1 : name);
            LitUserCode1.Text = string.IsNullOrWhiteSpace(name) ? string.Empty : HttpUtility.HtmlEncode(code1);
        }

        // El mismo cierre que tenía StartSession: abandona las sesiones en curso, termina la
        // Session y vuelve al login.
        protected void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionHelper.Logout();
        }
    }
}
