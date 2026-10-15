using System;
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
        // Logins e inicios de cada rol (spec 013): el de SAP NetWeaver es para entrenar; el
        // administrativo tiene su propio login y su propio cambio de contraseña.
        public const string TrainingLoginUrl          = "~/Pages/Auth/Login.aspx";
        public const string TrainingHomeUrl           = "~/Pages/Training/StartSession.aspx";
        public const string TrainingChangePasswordUrl = "~/Pages/Auth/ChangePassword.aspx";
        public const string AdminLoginUrl             = "~/Pages/Admin/Login.aspx";
        public const string AdminHomeUrl              = "~/Pages/Admin/Users.aspx";
        public const string AdminChangePasswordUrl    = "~/Pages/Admin/ChangePassword.aspx";

        /// <summary>
        /// Rol del usuario autenticado en este request (lo arma Global.asax con la BLL); null
        /// sin login o sin cuenta local.
        /// </summary>
        public static UserRole? CurrentRole()
            => (HttpContext.Current.User as UserPrincipal)?.Role;

        public static bool CanTrain()
            => CurrentRole() is UserRole role && AccessPolicy.CanTrain(role);

        public static bool CanManageUsers()
            => CurrentRole() is UserRole role && AccessPolicy.CanManageUsers(role);

        /// <summary>
        /// Inicio del usuario según su capacidad: el módulo administrativo o la selección de
        /// entrenamiento.
        /// </summary>
        public static string HomeUrl() => CanManageUsers() ? AdminHomeUrl : TrainingHomeUrl;

        /// <summary>
        /// Una página que el usuario no puede usar lo manda a su inicio, nunca al login (spec
        /// 013: sin bucle). Sin rol (cuenta borrada con la cookie vigente) cierra la sesión.
        /// </summary>
        public static void RedirectHome()
        {
            if (CurrentRole() == null)
                Logout();
            else
                HttpContext.Current.Response.Redirect(HomeUrl());
        }

        /// <summary>
        /// Asegura que sesión esté poblada con los datos del operador.
        /// Si sesión está vacía pero usuario está autenticado, re-hidrata desde BD.
        /// </summary>
        /// <returns>True si sesión está poblada; False si usuario no está autenticado o fue eliminado de BD.</returns>
        public static bool EnsureUserSession()
        {
            var context = HttpContext.Current;
            if (context.Session == null || !context.User.Identity.IsAuthenticated)
                return false;

            // Session ya está poblada
            if (context.Session[Constants.SessionKeys.UserId] != null)
                return true;

            // Session perdida pero FormsAuth sigue válido — re-hidratar desde BD
            string code1 = context.User.Identity.Name;
            var user = new AuthService().GetUserByCode1(code1);

            if (user == null)
            {
                // Operador eliminado pero cookie aún válido — forzar logout
                FormsAuthentication.SignOut();
                context.Session.Clear();
                return false;
            }

            // Re-establecer sesión
            SetUserSession(user);
            return true;
        }

        /// <summary>
        /// Escribe en la Session los datos del operador autenticado. La usan el login y la
        /// re-hidratación, así una clave nueva no puede quedar en un solo lado.
        /// </summary>
        public static void SetUserSession(User user, LoginResult login = null)
        {
            var session = HttpContext.Current.Session;
            session[Constants.SessionKeys.UserId]       = user.Id;
            session[Constants.SessionKeys.Code1]        = user.Code1;
            session[Constants.SessionKeys.Name]         = user.Name;
            session[Constants.SessionKeys.IsFirstLogin] = user.IsFirstLogin;

            // Solo el login conoce el acceso anterior (spec 013, AC-17).
            if (login != null)
                session[Constants.SessionKeys.LastAccess] = login.PreviousLoginAt.HasValue
                    ? "Último acceso: " + ToLocal(login.PreviousLoginAt.Value).ToString("dd/MM/yyyy HH:mm")
                    : "Primer acceso";
        }

        /// <summary>
        /// "Último acceso: …" o "Primer acceso" del login actual; null si la Session se
        /// re-hidrató (no se sabe el anterior).
        /// </summary>
        public static string LastAccessText()
            => HttpContext.Current.Session?[Constants.SessionKeys.LastAccess] as string;

        // Hora de planta (Costa Rica, UTC-6 sin horario de verano), sin depender de la zona
        // horaria configurada en el servidor.
        private static readonly TimeZoneInfo PlantTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");

        /// <summary>
        /// Fecha UTC de la BD en la hora de planta, para mostrarla.
        /// </summary>
        public static DateTime ToLocal(DateTime utc)
            => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), PlantTimeZone);

        /// <summary>
        /// Mensaje de cuenta bloqueada (spec 013, AC-14/15), el mismo en los dos logins.
        /// </summary>
        public static string LockedMessage(DateTime lockedUntilUtc)
            => "Cuenta bloqueada; intente de nuevo a las " + ToLocal(lockedUntilUtc).ToString("HH:mm") + ".";

        /// <summary>
        /// Cierra la sesión del operador y lo manda al login. Antes abandona sus sesiones de
        /// entrenamiento en curso (spec 009, D-9); si eso falla, igual cierra la Session y el
        /// login (auditoría AUD-35). No depende de la sesión de entrenamiento (AUD-33).
        /// </summary>
        public static void Logout()
        {
            var context = HttpContext.Current;
            // El admin vuelve a su login (spec 013); el resto, al de SAP.
            string loginUrl = CanManageUsers() ? AdminLoginUrl : TrainingLoginUrl;
            try
            {
                if (EnsureUserSession())
                    new TrainingSessionService().AbandonAllActive(
                        (int)context.Session[Constants.SessionKeys.UserId]);
            }
            finally
            {
                context.Session?.Abandon();
                FormsAuthentication.SignOut();
            }
            context.Response.Redirect(loginUrl);
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
