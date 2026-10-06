using System.Web;
using System.Web.Security;
using TrainingApp.BLL.Auth;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.Pages
{
    /// <summary>
    /// Helper para mantener sesión del operador sincronizada con FormsAuthentication.
    /// Previene NullReferenceException cuando Session se pierde pero cookie auth sigue válido.
    /// Vive en la capa de presentación (usa HttpContext) y lee el operador por la BLL
    /// (auditoría AUD-15).
    /// </summary>
    public static class SessionHelper
    {
        /// <summary>
        /// Asegura que sesión esté poblada con los datos del operador.
        /// Si sesión está vacía pero usuario está autenticado, re-hidrata desde BD.
        /// </summary>
        /// <returns>True si sesión está poblada; False si usuario no está autenticado o fue eliminado de BD.</returns>
        public static bool EnsureOperatorSession()
        {
            var context = HttpContext.Current;
            if (context.Session == null || !context.User.Identity.IsAuthenticated)
                return false;

            // Session ya está poblada
            if (context.Session[Constants.SessionKeys.OperatorId] != null)
                return true;

            // Session perdida pero FormsAuth sigue válido — re-hidratar desde BD
            string code1 = context.User.Identity.Name;
            var op = new AuthService().GetOperatorByCode1(code1);

            if (op == null)
            {
                // Operador eliminado pero cookie aún válido — forzar logout
                FormsAuthentication.SignOut();
                context.Session.Clear();
                return false;
            }

            // Re-establecer sesión
            SetOperatorSession(op);
            return true;
        }

        /// <summary>
        /// Escribe en la Session los datos del operador autenticado. La usan el login y la
        /// re-hidratación, así una clave nueva no puede quedar en un solo lado.
        /// </summary>
        public static void SetOperatorSession(Operator op)
        {
            var session = HttpContext.Current.Session;
            session[Constants.SessionKeys.OperatorId]   = op.Id;
            session[Constants.SessionKeys.Code1]        = op.Code1;
            session[Constants.SessionKeys.Name]         = op.Name;
            session[Constants.SessionKeys.IsFirstLogin] = op.IsFirstLogin;
        }

        /// <summary>
        /// Cierra la sesión del operador y lo manda al login. Antes abandona sus sesiones de
        /// entrenamiento en curso (spec 009, D-9); si eso falla, igual cierra la Session y el
        /// login (auditoría AUD-35). No depende de la sesión de entrenamiento (AUD-33).
        /// </summary>
        public static void Logout()
        {
            var context = HttpContext.Current;
            try
            {
                if (EnsureOperatorSession())
                    new TrainingSessionService().AbandonAllActive(
                        (int)context.Session[Constants.SessionKeys.OperatorId]);
            }
            finally
            {
                context.Session?.Abandon();
                FormsAuthentication.SignOut();
            }
            context.Response.Redirect("~/Pages/Auth/Login.aspx");
        }

        /// <summary>
        /// True mientras el operador no cambió la contraseña del primer acceso (auditoría
        /// AUD-08): las páginas de entrenamiento lo mandan a ChangePassword.
        /// </summary>
        public static bool MustChangePassword()
        {
            var session = HttpContext.Current.Session;
            return session != null && session[Constants.SessionKeys.IsFirstLogin] as bool? == true;
        }
    }
}
