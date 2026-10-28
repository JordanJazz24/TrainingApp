using System;
using System.Web;
using System.Web.UI.WebControls;
using TrainingApp.Pages;
using TrainingApp.Shared;

namespace TrainingApp.UI.Master
{
    public partial class AdminMaster : AntiXsrfMasterPage
    {
        // Login (brief 022, 3b): sin barra superior ni .admin-main; la página trae su propio
        // interruptor de tema. La pone la página en Page_Init.
        public bool FullBleed { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Usuario y Cerrar sesión solo con un admin autenticado. La marca es siempre el
            // nombre neutro de la app (spec 016, D-80; mockup de la Fase 11).
            bool signedIn = Context.User.Identity.IsAuthenticated && SessionHelper.CanManageUsers()
                && Session[Constants.SessionKeys.Code1] != null;
            PhUser.Visible = signedIn;
            if (signedIn)
            {
                LitUser.Text      = HttpUtility.HtmlEncode(Session[Constants.SessionKeys.Code1].ToString());
                LitLastLogin.Text = HttpUtility.HtmlEncode(SessionHelper.LastAccessText() ?? string.Empty);
            }

            // Menú del dashboard (spec 018): solo con la capacidad de ver reportes.
            PhMenu.Visible = signedIn && SessionHelper.CanViewReports();
            if (PhMenu.Visible)
            {
                LnkUsers.Visible  = SessionHelper.CanManageUsers();
                LnkGuides.Visible = SessionHelper.CanManageGuides();
                MarkCurrent(LnkSummary, "~/Pages/Admin/Summary.aspx");
                MarkCurrent(LnkOperators,"~/Pages/Admin/Operators.aspx", "~/Pages/Admin/Operator.aspx");
                MarkCurrent(LnkSteps, "~/Pages/Admin/Steps.aspx");
                MarkCurrent(LnkFollowUp, "~/Pages/Admin/FollowUp.aspx");
                MarkCurrent(LnkUsers, "~/Pages/Admin/Users.aspx");
                MarkCurrent(LnkGuides, "~/Pages/Admin/Guides.aspx");
            }
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            if (!FullBleed) return;
            AdminBar.Visible = false;
            AdminMain.Attributes["class"] = "admin-main-bleed";
        }

        // La vista actual (o su detalle) queda marcada en el menú. Se compara el archivo de la
        // página: con FriendlyUrls la URL del request no lleva ".aspx".
        private void MarkCurrent(HyperLink link, params string[] pages)
        {
            var current = Page.AppRelativeVirtualPath;
            foreach (var page in pages)
                if (string.Equals(current, page, StringComparison.OrdinalIgnoreCase))
                {
                    link.CssClass = "active";
                    link.Attributes["aria-current"] = "page";
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
