using System.Web;
using System.Web.Security;
using TrainingApp.DAL.Repositories;

namespace TrainingApp.Shared
{
    /// <summary>
    /// Helper para mantener sesión del operador sincronizada con FormsAuthentication.
    /// Previene NullReferenceException cuando Session se pierde pero cookie auth sigue válido.
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
            var operatorRepo = new OperatorRepository();
            var op = operatorRepo.GetByCode1(code1);

            if (op == null)
            {
                // Operador eliminado pero cookie aún válido — forzar logout
                FormsAuthentication.SignOut();
                context.Session.Clear();
                return false;
            }

            // Re-establecer sesión
            context.Session[Constants.SessionKeys.OperatorId] = op.Id;
            context.Session[Constants.SessionKeys.Code1] = op.Code1;
            context.Session[Constants.SessionKeys.Name] = op.Name;
            context.Session[Constants.SessionKeys.IsFirstLogin] = op.IsFirstLogin;
            return true;
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
