using System;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.UI;
using TrainingApp.BLL.Auth;
using TrainingApp.Pages;
using TrainingApp.Shared;

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

            var account = RoleCache.GetAccount(user.Identity.Name);
            if (account == null) return;

            var principal = new UserPrincipal(user.Identity, account.UserId, account.Role);
            Context.User = principal;
            Thread.CurrentPrincipal = principal;
        }

        // Error no controlado (AUD-28): se registra en App_Data/Logs (fecha, URL, usuario y
        // excepción) y el usuario sigue viendo la página de customErrors (no se limpia el
        // error). Los 404 no se registran: no son fallas de la app. Nunca lanza: una falla aquí
        // taparía el error original.
        void Application_Error(object sender, EventArgs e)
        {
            try
            {
                var error = Server.GetLastError();
                if (error == null || (error as HttpException)?.GetHttpCode() == 404) return;

                var user = Context.User?.Identity?.IsAuthenticated == true ? Context.User.Identity.Name : "(sin login)";
                AppLog.Write("Error", Request.HttpMethod + " " + AppLog.Clean(Request.RawUrl, 300) +
                    " | usuario " + AppLog.Clean(user) + Environment.NewLine + Describe(error));
            }
            catch
            {
                // Sin request (ej. un error al arrancar) o sin log: el error sigue su curso.
            }
        }

        // La excepción con su cadena de InnerException; cada mensaje en una línea y corto
        // (AppLog.Clean), porque algunos repiten lo que mandó el cliente. Las de validación de
        // ASP.NET (un valor con marcado, ej. "<x", que puede ser una contraseña) y de ViewState
        // (traen el campo del formulario) traen datos del formulario en el mensaje: de esas
        // solo el tipo y el stack.
        private static string Describe(Exception error)
        {
            var text = new StringBuilder();
            for (var current = error; current != null; current = current.InnerException)
            {
                text.Append("  ").Append(current.GetType().FullName);
                if (!(current is HttpRequestValidationException || current is ViewStateException))
                    text.Append(": ").Append(AppLog.Clean(current.Message, 500));
                text.AppendLine().AppendLine(current.StackTrace);
            }
            return text.ToString().TrimEnd();
        }
    }
}
