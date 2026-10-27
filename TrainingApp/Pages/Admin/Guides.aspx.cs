using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.BLL.Admin;
using TrainingApp.Domain;
using TrainingApp.Shared;
using TrainingApp.UI.Master;

namespace TrainingApp.Pages.Admin
{
    // Guías (spec 020): el tipo de Resource Setup de cada guía publicada. Solo UI: quién puede y
    // qué guías se cambian lo decide GuideAdminService.
    public partial class Guides : Page
    {
        private readonly GuideAdminService _admin = new GuideAdminService();

        // Admin autenticado (Session escrita solo por el servidor tras el login).
        private int AdminUserId => (int)Session[Constants.SessionKeys.UserId];

        // Un admin al que otro le quitó el rol llega a la BLL, que lanza
        // UnauthorizedAccessException: vuelve a su inicio, nunca al login (como Usuarios, B-3).
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
            // y sin la capacidad vuelve al inicio del rol, nunca al login (AC-9).
            if (!SessionHelper.CanManageGuides())
            {
                SessionHelper.RedirectHome();
                return;
            }

            if (SessionHelper.MustChangePassword())
            {
                Response.Redirect(SessionHelper.ChangePasswordUrl);
                return;
            }

            if (!IsPostBack)
                BindGuides();
        }

        protected void RptGuides_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "SetType" || !int.TryParse(e.CommandArgument as string, out int guideId)) return;

            var ddl = e.Item.FindControl("DdlType") as DropDownList;
            if (ddl == null || !int.TryParse(ddl.SelectedValue, out int value) ||
                !Enum.IsDefined(typeof(ResourceSetupType), value))
                return;

            var type = (ResourceSetupType)value;
            if (_admin.SetResourceSetupType(AdminUserId, guideId, type) == GuideAdminResult.Ok)
                ShowMessage("success", "Listo", "La guía quedó con el Resource Setup " + TypeText(type) +
                    ". Vale para las sesiones que empiecen desde ahora.");
            else
                ShowMessage("warning", "Aviso", "La guía ya no está publicada.");
            BindGuides();
        }

        private void BindGuides()
        {
            var guides = _admin.GetPublishedGuides(AdminUserId);
            RptGuides.DataSource = guides.Select(ToRow).ToList();
            RptGuides.DataBind();
            LblEmpty.Visible = guides.Count == 0;
        }

        private static GuideListRow ToRow(PublishedGuide g)
        {
            bool custom = g.ResourceSetupType == ResourceSetupType.Custom;
            return new GuideListRow
            {
                Id          = g.Id,
                ProductName = g.ProductName,
                AreaName    = g.AreaName,
                Title       = g.Title,
                // Siempre un valor del selector: uno desconocido no rompe el DataBind.
                TypeValue   = custom ? "1" : "0",
                TypeText    = TypeText(g.ResourceSetupType),
                TypeCss     = custom ? "mt-badge mt-badge-accent" : "mt-badge mt-badge-idle",
                TypeIcon    = custom ? "bi bi-stars" : "bi bi-gear",
                ConfirmText = "¿Guardar el Resource Setup elegido para «" + g.Title + "»? " +
                              "Vale para las sesiones que empiecen desde ahora; las que están en curso siguen con el suyo."
            };
        }

        private static string TypeText(ResourceSetupType type)
            => type == ResourceSetupType.Custom ? "Custom" : "Estándar";

        private void ShowMessage(string icon, string title, string text)
            => ((AdminMaster)Master).ShowMessage(icon, title, text);
    }
}
