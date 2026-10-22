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
        // Un solo login y un solo cambio de contraseña, neutros, para los dos roles (spec 016,
        // D-62); el inicio depende del rol (spec 013).
        public const string LoginUrl          = "~/Pages/Auth/Login.aspx";
        public const string ChangePasswordUrl = "~/Pages/Auth/ChangePassword.aspx";
        public const string TrainingHomeUrl   = "~/Pages/Training/StartSession.aspx";
        public const string AdminHomeUrl      = "~/Pages/Admin/Users.aspx";

        // Cookie de la Session de ASP.NET (Web.config no cambia el nombre por defecto).
        private const string SessionCookieName = "ASP.NET_SessionId";

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

        public static bool CanViewReports()
            => CurrentRole() is UserRole role && AccessPolicy.CanViewReports(role);

        /// <summary>
        /// Inicio del usuario según su capacidad: el módulo administrativo o la selección de
        /// entrenamiento.
        /// </summary>
        public static string HomeUrl() => CurrentRole() is UserRole role ? HomeUrl(role) : TrainingHomeUrl;

        /// <summary>
        /// Inicio de un rol. El login lo usa con el rol de la cuenta: en ese request el
        /// principal todavía es anónimo.
        /// </summary>
        public static string HomeUrl(UserRole role)
            => AccessPolicy.CanManageUsers(role) ? AdminHomeUrl : TrainingHomeUrl;

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

            string code1 = context.User.Identity.Name?.Trim();

            // Session ya está poblada, y con la cuenta de la cookie: mismo Code1 y mismo Id que
            // el principal. Si es de otro (la Session de otro navegador o de un login anterior)
            // o de una cuenta borrada y vuelta a crear con el mismo Code1, se descarta entera,
            // PodState incluido, y se sigue como una Session perdida (fijación de sesión,
            // 2026-10-19).
            if (context.Session[Constants.SessionKeys.UserId] != null)
            {
                var principal = context.User as UserPrincipal;
                if (principal != null && SessionBelongsTo(code1, principal.UserId))
                    return true;
                context.Session.Clear();
            }

            // Session vacía con FormsAuth válido: el primer request después del login (SignIn la
            // deja con un ID nuevo) o una Session perdida. Se llena desde la BD.
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

        // ¿La Session tiene los datos de esta cuenta (Code1 sin distinguir mayúsculas y Id)?
        private static bool SessionBelongsTo(string code1, int userId)
        {
            var session = HttpContext.Current.Session;
            var sessionCode1 = (session[Constants.SessionKeys.Code1] as string)?.Trim();
            return session[Constants.SessionKeys.UserId] as int? == userId &&
                   string.Equals(sessionCode1, code1?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Abre la sesión de un login correcto con un ID de Session nuevo (fijación de sesión,
        /// AUD-28): descarta la Session que traía el navegador y expira su cookie, así el
        /// request siguiente recibe un ID nuevo y EnsureUserSession la llena desde la BD. El
        /// último acceso anterior (solo lo conoce el login, spec 013 AC-17) viaja en el ticket
        /// de Forms, que va cifrado y firmado.
        /// </summary>
        public static void SignIn(LoginResult login)
        {
            var context = HttpContext.Current;
            context.Session?.Abandon();
            ExpireSessionCookie(context);

            var cookie = FormsAuthentication.GetAuthCookie(login.User.Code1, false);
            var ticket = FormsAuthentication.Decrypt(cookie.Value);
            var lastAccess = login.PreviousLoginAt.HasValue
                ? "Último acceso: " + ToLocal(login.PreviousLoginAt.Value).ToString("dd/MM/yyyy HH:mm")
                : "Primer acceso";
            cookie.Value = FormsAuthentication.Encrypt(new FormsAuthenticationTicket(ticket.Version,
                ticket.Name, ticket.IssueDate, ticket.Expiration, ticket.IsPersistent, lastAccess,
                ticket.CookiePath));
            context.Response.Cookies.Add(cookie);
        }

        /// <summary>
        /// Escribe en la Session los datos del operador autenticado, desde la BD
        /// (EnsureUserSession). Es el único lugar que la llena: una clave nueva va aquí.
        /// </summary>
        public static void SetUserSession(User user)
        {
            var session = HttpContext.Current.Session;
            session[Constants.SessionKeys.UserId]       = user.Id;
            session[Constants.SessionKeys.Code1]        = user.Code1;
            session[Constants.SessionKeys.Name]         = user.Name;
            session[Constants.SessionKeys.IsFirstLogin] = user.IsFirstLogin;
        }

        /// <summary>
        /// "Último acceso: …" o "Primer acceso" del login actual, del ticket de Forms (SignIn);
        /// null sin login.
        /// </summary>
        public static string LastAccessText()
        {
            var text = (HttpContext.Current.User?.Identity as FormsIdentity)?.Ticket.UserData;
            return string.IsNullOrEmpty(text) ? null : text;
        }

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
        /// Mensaje de cuenta bloqueada (spec 013, AC-14/15).
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
                ExpireSessionCookie(context);
            }
            context.Response.Redirect(LoginUrl);
        }

        // Abandon conserva el id de la Session: se expira su cookie para que el próximo request
        // de este navegador reciba uno nuevo (fijación de sesión, 2026-10-19; AUD-28).
        private static void ExpireSessionCookie(HttpContext context)
            => context.Response.Cookies.Add(new HttpCookie(SessionCookieName, string.Empty)
            {
                Expires = DateTime.UtcNow.AddYears(-1)
            });

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
