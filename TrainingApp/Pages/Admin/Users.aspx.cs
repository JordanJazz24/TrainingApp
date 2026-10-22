using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.BLL.Admin;
using TrainingApp.BLL.Auth;
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
        // la BLL, que lanza UnauthorizedAccessException: vuelve a su inicio en vez de un error.
        protected override void OnError(EventArgs e)
        {
            if (Server.GetLastError()?.GetBaseException() is UnauthorizedAccessException)
            {
                Server.ClearError();
                RoleCache.Invalidate(User.Identity.Name);
                Response.Redirect(SessionHelper.LoginUrl);
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

            if (!IsPostBack)
                BindUsers();
        }

        protected void BtnSearch_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            SearchText  = TxtSearch.Text.Trim();
            CurrentPage = 1;
            BindUsers();
        }

        protected void BtnPrev_Click(object sender, EventArgs e)
        {
            CurrentPage = Math.Max(1, CurrentPage - 1);
            BindUsers();
        }

        protected void BtnNext_Click(object sender, EventArgs e)
        {
            CurrentPage = CurrentPage + 1;
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
            }
            BindUsers();
        }

        private void BindUsers()
        {
            var page = _admin.Search(AdminUserId, SearchText, CurrentPage);
            CurrentPage = page.Page;

            var now = DateTime.UtcNow;
            RptUsers.DataSource = page.Users.Select(u => ToRow(u, now)).ToList();
            RptUsers.DataBind();

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
            return new UserListRow
            {
                Id                = u.Id,
                Code1             = u.Code1,
                Name              = u.Name,
                RoleText          = RoleText(u.Role),
                StateText         = StateText(state, u),
                StateCss          = StateCss(state),
                CreatedText       = SessionHelper.ToLocal(u.CreatedAt).ToString("dd/MM/yyyy"),
                LastLoginText     = u.LastLoginAt.HasValue
                    ? SessionHelper.ToLocal(u.LastLoginAt.Value).ToString("dd/MM/yyyy HH:mm") : "—",
                IsLocked          = state == UserAccountState.Locked,
                IsSelf            = u.Id == AdminUserId,
                HasHistory        = u.SessionCount > 0,
                ConfirmReset      = "¿Restablecer la contraseña de " + who + "? Entrará con la contraseña por defecto y deberá cambiarla.",
                ConfirmUnlock     = "¿Desbloquear la cuenta de " + who + "?",
                ConfirmDelete     = u.SessionCount > 0
                    ? "¿Borrar a " + who + "? Tiene " + u.SessionCount + (u.SessionCount == 1 ? " sesión" : " sesiones") +
                      " de entrenamiento: se borrarán su historial y sus puntajes y no se pueden recuperar." +
                      " Los indicadores del dashboard cambian." // spec 018, D-142 (AC-23)
                    : "¿Borrar a " + who + "?"
            };
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
                    return "Primer acceso pendiente";
                default:
                    return "Activa";
            }
        }

        private static string StateCss(UserAccountState state)
        {
            switch (state)
            {
                case UserAccountState.Locked:            return "badge bg-danger";
                case UserAccountState.PendingFirstLogin: return "badge bg-warning text-dark";
                default:                                 return "badge bg-success";
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
