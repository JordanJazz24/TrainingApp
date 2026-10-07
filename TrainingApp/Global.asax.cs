using System;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using TrainingApp.BLL.Auth;

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
    }
}