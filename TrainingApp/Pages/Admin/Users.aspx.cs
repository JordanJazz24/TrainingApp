using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.BLL.Admin;
using TrainingApp.BLL.Auth;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Shared;
using TrainingApp.UI.Master;

namespace TrainingApp.Pages.Admin
{
    // Gestión de usuarios (spec 013): inicio del administrador. Solo UI: las reglas (quién puede,
    // no borrarse a sí mismo) las decide UserAdminService.
    public partial class Users : Page
    {
        private const string PageKey   = "UsersPage";
        private const string SearchKey = "UsersSearch";

        private readonly UserAdminService _admin = new UserAdminService();
        private readonly TrainingGuideService _guideService = new TrainingGuideService();

        private int CurrentPage
        {
            get { return ViewState[PageKey] as int? ?? 1; }
            set { ViewState[PageKey] = value; }
        }

        private string SearchText
        {
            get { return ViewState[SearchKey] as string ?? string.Empty; }
            set { ViewState[SearchKey] = value; }
        }

        // Admin autenticado (Session escrita solo por el servidor tras el login).
        private int AdminUserId => (int)Session[Constants.SessionKeys.UserId];

        // Un admin al que otro le quitó el rol (en otro proceso, con RoleCache vigente) llega a
        // la BLL, que lanza UnauthorizedAccessException: vuelve a su inicio en vez de un error,
        // nunca al login (B-3, AC-2 de la spec 018). Con la caché invalidada, el request
        // siguiente ya lleva su rol nuevo. Cubre también Cambiar y Quitar área (spec 021, 12b).
        protected override void OnError(EventArgs e)
        {
            if (Server.GetLastError()?.GetBaseException() is UnauthorizedAccessException)
            {
                Server.ClearError();
                SessionHelper.RedirectHomeAfterRoleChange();
                return;
            }
            base.OnError(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!SessionHelper.EnsureUserSession())
            {
                Response.Redirect(SessionHelper.LoginUrl);
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
                Response.Redirect(SessionHelper.ChangePasswordUrl);
                return;
            }

            BindEligibleAreas();
            if (!IsPostBack)
                BindUsers();
        }

        protected void BtnSearch_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            SearchText  = TxtSearch.Text.Trim();
            CurrentPage = 1;
            HfSelected.Value = string.Empty;
            BindUsers();
        }

        protected void BtnPrev_Click(object sender, EventArgs e)
        {
            CurrentPage = Math.Max(1, CurrentPage - 1);
            HfSelected.Value = string.Empty;
            BindUsers();
        }

        protected void BtnNext_Click(object sender, EventArgs e)
        {
            CurrentPage = CurrentPage + 1;
            HfSelected.Value = string.Empty;
            BindUsers();
        }

        protected void BtnCreate_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string code1 = TxtNewCode1.Text.Trim();
            var role = DdlNewRole.SelectedValue == ((int)UserRole.Administrator).ToString()
                ? UserRole.Administrator : UserRole.Operator;

            UserAdminResult result;
            try
            {
                result = _admin.Create(AdminUserId, code1, role);
            }
            catch (InvalidOperationException)
            {
                // La lista de empleados nunca se pudo descargar (EmployeeRepository).
                ShowMessage("error", "El servicio de empleados no está disponible. Intente más tarde.");
                return;
            }

            if (result == UserAdminResult.Ok)
            {
                TxtNewCode1.Text = string.Empty;
                ShowMessage("success", "Usuario " + code1 + " creado como " + RoleText(role) +
                    ". Entra con la contraseña por defecto y la cambia en su primer acceso.");
            }
            else
            {
                ShowResult(result);
            }
            BindUsers();
        }

        protected void RptUsers_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!int.TryParse(e.CommandArgument as string, out int userId)) return;

            switch (e.CommandName)
            {
                case "Reset":
                    ShowResult(_admin.ResetPassword(AdminUserId, userId),
                        "Contraseña restablecida: entra con la contraseña por defecto y debe cambiarla.");
                    break;

                case "Unlock":
                    ShowResult(_admin.Unlock(AdminUserId, userId), "Cuenta desbloqueada.");
                    break;

                case "Delete":
                    ShowResult(_admin.Delete(AdminUserId, userId), "Usuario borrado.");
                    break;

                // Spec 021 (D-172): el área elegida viaja en el select de la fila (sin
                // EventValidation); la BLL valida que sea elegible y que la cuenta sea Operator.
                case "SetArea":
                    if (int.TryParse(Request.Form["area_" + userId], out int areaId))
                        ShowResult(_admin.SetArea(AdminUserId, userId, areaId), "Área cambiada.");
                    break;
            }
            BindUsers();
        }

        // Las áreas que se pueden asignar (D-176), una sola vez en una <template> que copia
        // cada fila al abrir Cambiar área (P-11). En cada carga: el Repeater no usa ViewState.
        private void BindEligibleAreas()
        {
            RptAreaGroups.DataSource = _guideService.GetEligibleAreas()
                .GroupBy(a => a.ProductName)
                .Select(g => new
                {
                    ProductName = g.Key,
                    Areas = g.Select(a => new { a.AreaId, a.AreaName, Label = AreaLabel.Of(a.ProductName, a.AreaName) })
                })
                .ToList();
            RptAreaGroups.DataBind();
        }

        private void BindUsers()
        {
            var page = _admin.Search(AdminUserId, SearchText, CurrentPage);
            CurrentPage = page.Page;

            var now = DateTime.UtcNow;
            var rows = page.Users.Select(u => ToRow(u, now)).ToList();

            // La ficha abierta es la elegida (HfSelected); si ya no está en la página, la primera
            // cuenta con problema y, si no hay, la primera.
            var selected = rows.FirstOrDefault(r => r.Id.ToString() == HfSelected.Value)
                ?? rows.FirstOrDefault(r => r.IsProblem)
                ?? rows.FirstOrDefault();
            foreach (var row in rows) row.IsSelected = row == selected;
            HfSelected.Value = selected?.Id.ToString() ?? string.Empty;

            RptDirectory.DataSource = rows;
            RptDirectory.DataBind();
            RptUsers.DataSource = rows;
            RptUsers.DataBind();

            LitCountLocked.Text  = page.Summary.Locked.ToString();
            LitCountPending.Text = page.Summary.PendingFirstLogin.ToString();
            LitCountNoArea.Text  = page.Summary.NoArea.ToString();

            LblEmpty.Visible  = page.Total == 0;
            LitPageInfo.Text  = page.Total == 0 ? string.Empty :
                string.Format("Página {0} de {1} · {2} usuarios", page.Page, page.PageCount, page.Total);
            BtnPrev.Visible   = page.Page > 1;
            BtnNext.Visible   = page.Page < page.PageCount;
        }

        private UserListRow ToRow(UserSummary u, DateTime nowUtc)
        {
            var state = UserAdminService.StateOf(u, nowUtc);
            string who = u.Code1 + " (" + u.Name + ")";
            bool isOperator = u.Role == UserRole.Operator;
            string areaText = !isOperator ? "—"
                : AreaLabel.Of(u.ProductName, u.AreaName) ?? AreaLabel.NoArea;
            return new UserListRow
            {
                Id                = u.Id,
                Code1             = u.Code1,
                Name              = u.Name,
                RoleText          = RoleText(u.Role),
                RoleCss           = u.Role == UserRole.Administrator ? "mt-badge mt-badge-accent" : "mt-badge mt-badge-idle",
                RoleIcon          = u.Role == UserRole.Administrator ? "bi bi-shield-lock" : "bi bi-person",
                StateText         = StateText(state, u),
                StateCss          = StateCss(state),
                StateIcon         = StateIcon(state),
                CreatedText       = SessionHelper.ToLocal(u.CreatedAt).ToString("dd/MM/yyyy"),
                LastLoginText     = u.LastLoginAt.HasValue
                    ? SessionHelper.ToLocal(u.LastLoginAt.Value).ToString("dd/MM/yyyy HH:mm") : "—",
                AreaText          = areaText,
                IsOperator        = isOperator,
                AreaId            = u.AreaId,
                Who               = who,
                IsLocked          = state == UserAccountState.Locked,
                IsSelf            = u.Id == AdminUserId,
                HasHistory        = u.SessionCount > 0,
                ConfirmReset      = "¿Restablecer la contraseña de " + who + "? Entrará con la contraseña por defecto y deberá cambiarla.",
                ConfirmUnlock     = "¿Desbloquear la cuenta de " + who + "?",
                ConfirmDelete     = u.SessionCount > 0
                    ? "¿Borrar a " + who + "? Tiene " + u.SessionCount + (u.SessionCount == 1 ? " sesión" : " sesiones") +
                      " de entrenamiento: se borrarán su historial y sus puntajes y no se pueden recuperar." +
                      " Los indicadores del dashboard cambian." // spec 018, D-142 (AC-23)
                    : "¿Borrar a " + who + "?",
                Initials          = Initials(u.Name),
                IsAdmin           = !isOperator,
                IsProblem         = state != UserAccountState.Active,
                StateShort        = StateShort(state),
                NoArea            = isOperator && u.AreaId == null,
                AreaShort         = isOperator ? areaText : RoleText(u.Role),
                AreaNote          = isOperator && u.AreaId == null
                    ? "Aún no eligió su área: no cuenta como «Sin iniciar» en Operadores."
                    : "Producto · Área. Se puede corregir aquí.",
                HintIcon          = HintIcon(state),
                LongHint          = LongHint(state, u, nowUtc),
                LastAgoText       = u.LastLoginAt.HasValue
                    ? ReportText.DaysAgo((SessionHelper.ToLocal(nowUtc).Date - SessionHelper.ToLocal(u.LastLoginAt.Value).Date).Days)
                    : "Nunca ha entrado",
                SessionsText      = !isOperator ? "No entrena (administrador)" : SessionsText(u.SessionCount),
                DeleteNote        = u.Id == AdminUserId
                    ? "Es tu cuenta: nadie se borra a sí mismo. Pídele a otro administrador que lo haga."
                    : u.SessionCount > 0
                        ? "Tiene " + SessionsText(u.SessionCount) + " de entrenamiento: se borrarán su historial y sus puntajes y no se pueden recuperar. Los indicadores del dashboard cambian."
                        : "No tiene historial de entrenamiento. La cuenta se borra y no se puede recuperar."
            };
        }

        private static string Initials(string name)
        {
            var words = (name ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "?";
            return (words[0].Substring(0, 1) + (words.Length > 1 ? words[1].Substring(0, 1) : string.Empty)).ToUpperInvariant();
        }

        private static string SessionsText(int count)
            => count == 0 ? "Ninguna" : count + (count == 1 ? " sesión" : " sesiones");

        private static string StateShort(UserAccountState state)
        {
            switch (state)
            {
                case UserAccountState.Locked:            return "Bloqueada";
                case UserAccountState.PendingFirstLogin: return "Debe cambiar";
                default:                                 return "Activa";
            }
        }

        private static string HintIcon(UserAccountState state)
        {
            switch (state)
            {
                case UserAccountState.Locked:            return "bi bi-hourglass-split";
                case UserAccountState.PendingFirstLogin: return "bi bi-key";
                default:                                 return "bi bi-check-circle";
            }
        }

        // Qué pasa con el acceso de la cuenta (brief 8). El número de intentos sale de la política.
        private static string LongHint(UserAccountState state, User u, DateTime nowUtc)
        {
            switch (state)
            {
                case UserAccountState.Locked:
                    int minutes = (int)Math.Ceiling((u.LockedUntil.Value - nowUtc).TotalMinutes);
                    return CountWord(LoginLockPolicy.MaxFailedLogins) + " intentos fallidos seguidos. Se desbloquea sola a las " +
                        SessionHelper.ToLocal(u.LockedUntil.Value).ToString("HH:mm") + " (en " + minutes + " min); puedes desbloquearla ya.";
                case UserAccountState.PendingFirstLogin:
                    return u.LastLoginAt.HasValue
                        ? "Su contraseña fue restablecida: entra con la contraseña por defecto y debe cambiarla."
                        : "Todavía no entra. Su contraseña es la que viene por defecto y debe cambiarla antes de usar la app.";
                default:
                    return "Entra con su contraseña personal.";
            }
        }

        private static string CountWord(int n)
        {
            string[] words = { "Cero", "Un", "Dos", "Tres", "Cuatro", "Cinco", "Seis", "Siete", "Ocho", "Nueve", "Diez" };
            return n >= 0 && n < words.Length ? words[n] : n.ToString();
        }

        private static string RoleText(UserRole role)
            => role == UserRole.Administrator ? "Administrador" : "Operador";

        private static string StateText(UserAccountState state, User u)
        {
            switch (state)
            {
                case UserAccountState.Locked:
                    return "Bloqueada hasta las " + SessionHelper.ToLocal(u.LockedUntil.Value).ToString("HH:mm");
                case UserAccountState.PendingFirstLogin:
                    return "Debe cambiar la contraseña";
                default:
                    return "Activa";
            }
        }

        // Insignia de estado (brief 019): texto e ícono además del color.
        private static string StateCss(UserAccountState state)
        {
            switch (state)
            {
                case UserAccountState.Locked:            return "mt-badge mt-badge-bad";
                case UserAccountState.PendingFirstLogin: return "mt-badge mt-badge-warn";
                default:                                 return "mt-badge mt-badge-ok";
            }
        }

        private static string StateIcon(UserAccountState state)
        {
            switch (state)
            {
                case UserAccountState.Locked:            return "bi bi-lock";
                case UserAccountState.PendingFirstLogin: return "bi bi-key";
                default:                                 return "bi bi-check-circle";
            }
        }

        private void ShowResult(UserAdminResult result, string okText = null)
        {
            switch (result)
            {
                case UserAdminResult.Ok:
                    if (okText != null) ShowMessage("success", okText);
                    break;
                case UserAdminResult.UserNotFound:
                    ShowMessage("warning", "El usuario ya no existe.");
                    break;
                case UserAdminResult.NotInEmployeeApi:
                    ShowMessage("warning", "El código no está en la lista de empleados.");
                    break;
                case UserAdminResult.AlreadyExists:
                    ShowMessage("warning", "El código ya tiene cuenta.");
                    break;
                case UserAdminResult.OwnAccount:
                    ShowMessage("warning", "No puedes borrar tu propia cuenta.");
                    break;
            }
        }

        private void ShowMessage(string icon, string text)
        {
            string title = icon == "success" ? "Listo" : icon == "error" ? "Error" : "Aviso";
            ((AdminMaster)Master).ShowMessage(icon, title, text);
        }
    }
}
