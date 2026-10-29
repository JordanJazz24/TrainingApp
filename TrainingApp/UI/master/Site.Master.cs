using System;
using System.Web;
using TrainingApp.Pages;
using TrainingApp.Shared;
using TrainingApp.UI.Master;

namespace TrainingApp
{
    // Inicio del operador (StartSession) y "Elige tu área". La barra superior (spec 022, brief
    // 0) muestra al usuario autenticado, su área si la página la da (ShowArea) y su Cerrar
    // sesión (D-13, D-163); la página ya validó la sesión y el rol.
    public partial class SiteMaster : AntiXsrfMasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            bool signedIn = Context.User.Identity.IsAuthenticated && Session[Constants.SessionKeys.Code1] != null;
            PhUser.Visible = signedIn;
            if (!signedIn) return;

            string code1 = Session[Constants.SessionKeys.Code1].ToString();
            string name  = Session[Constants.SessionKeys.Name] as string;
            string shown = string.IsNullOrWhiteSpace(name) ? code1 : name;
            string code  = string.IsNullOrWhiteSpace(name) ? string.Empty : "Code1 " + code1;
            LitUser.Text      = HttpUtility.HtmlEncode(shown);
            LitInitials.Text  = HttpUtility.HtmlEncode(TopBar.Initials(shown));
            LitUserCode1.Text = HttpUtility.HtmlEncode(code);
            UserAccount.Attributes["title"] = TopBar.AccountTitle(shown, code);
        }

        /// <summary>
        /// "Tu área" en la barra ("Producto · Área", spec 021). La llama la página que ya cargó
        /// el área (StartSession); queda en el ViewState para los postbacks.
        /// </summary>
        public void ShowArea(string areaText)
        {
            PhArea.Visible = !string.IsNullOrWhiteSpace(areaText);
            LitArea.Text   = HttpUtility.HtmlEncode(areaText ?? string.Empty);
        }

        // El mismo cierre que tenía StartSession: abandona las sesiones en curso, termina la
        // Session y vuelve al login.
        protected void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionHelper.Logout();
        }
    }
}
