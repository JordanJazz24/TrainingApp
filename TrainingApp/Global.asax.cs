using System;
using System.Threading;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using TrainingApp.BLL.Auth;
using TrainingApp.Pages;

namespace TrainingApp
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Descarga la lista de empleados en segundo plano: el primer login no la espera.
            new AuthService().WarmUpEmployees();
        }

        // Rol del usuario autenticado (spec 013): antes de la autorización por <location>, que
        // corre antes de que exista la Session. Lo da la BLL con una caché corta (RoleCache).
        // Sin cuenta local (borrada con la cookie vigente) queda sin rol: las páginas cierran la
        // sesión (SessionHelper).
        void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var user = Context.User;
            if (user?.Identity == null || !user.Identity.IsAuthenticated) return;

            var role = RoleCache.GetRole(user.Identity.Name);
            if (!role.HasValue) return;

            var principal = new UserPrincipal(user.Identity, role.Value);
            Context.User = principal;
            Thread.CurrentPrincipal = principal;
        }
    }
}
