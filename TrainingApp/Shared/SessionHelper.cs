using System.Web;
using System.Web.Security;
using TrainingApp.DAL.Repositories;

namespace TrainingApp.Shared
{
    /// <summary>
    /// Helper para mantener la sesión del operador sincronizada con FormsAuthentication.
    /// Previene NullReferenceException cuando Session se pierde pero el cookie de auth sigue válido.
    /// </summary>
    public static class SessionHelper
    {
        /// <summary>
        /// Asegura que la sesión esté poblada con los datos del operador.
        /// Si la sesión está vacía pero el usuario está autenticado, re-hidrata desde la BD.
        /// </summary>
        /// <returns>True si la sesión está poblada; False si el usuario no está autenticado o fue eliminado de la BD.</returns>
        public static bool EnsureOperatorSession()
        {
            var context = HttpContext.Current;
            if (context?.Session == null || !context.User.Identity.IsAuthenticated)
                return false;

            // Session ya poblada
            if (context.Session[Constants.SessionKeys.OperatorId] != null)
                return true;

            // Session perdida pero FormsAuth sigue válido → re-hidratar desde BD
            string code1 = context.User.Identity.Name;
            var operatorRepo = new OperatorRepository();
            var op = operatorRepo.GetByCode1(code1);

            if (op == null)
            {
                // Operador eliminado de la BD pero cookie aún válido → forzar logout
                FormsAuthentication.SignOut();
                context.Session.Clear();
                return false;
            }

            // Re-establecer sesión
            context.Session[Constants.SessionKeys.OperatorId] = op.Id;
            context.Session[Constants.SessionKeys.Code1] = op.Code1;
            context.Session[Constants.SessionKeys.Name] = op.Name;
            return true;
        }
    }
}
