using System;
using System.Web.UI;

namespace TrainingApp.Pages.Admin
{
    // Gestión de usuarios (spec 013): inicio del administrador.
    public partial class Users : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!SessionHelper.EnsureUserSession())
            {
                Response.Redirect(SessionHelper.AdminLoginUrl);
                return;
            }

            // Web.config ya deja pasar solo al rol Administrator; la página lo vuelve a revisar
            // y un operador vuelve a su inicio, sin bucle al login (AC-4).
            if (!SessionHelper.CanManageUsers())
            {
                SessionHelper.RedirectHome();
                return;
            }

            // Primer acceso o reset: la contraseña se cambia antes de usar el módulo (AC-6).
            if (SessionHelper.MustChangePassword())
            {
                Response.Redirect(SessionHelper.AdminChangePasswordUrl);
                return;
            }
        }
    }
}
