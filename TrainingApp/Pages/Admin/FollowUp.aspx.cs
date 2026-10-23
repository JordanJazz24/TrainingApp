using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.BLL.Reports;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Admin
{
    // Seguimiento (spec 018, AC-19 y AC-20). Solo UI: las reglas las decide FollowUpRules por
    // DashboardService. El filtro de guía vive en la URL (?guide=…): nada se guarda en Session.
    public partial class FollowUp : Page
    {
        private const string PageUrl = "~/Pages/Admin/FollowUp.aspx";

        private readonly DashboardService _dashboard = new DashboardService();

        // Usuario autenticado (Session escrita solo por el servidor tras el login).
        private int UserId => (int)Session[Constants.SessionKeys.UserId];

        // Quien perdió la capacidad con el RoleCache vigente vuelve a su inicio, nunca al login
        // (AC-2), como en Operadores.
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

            if (!SessionHelper.CanViewReports())
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
            {
                var filter = new FollowUpFilter();
                if (int.TryParse(Request.QueryString["guide"], out int guideId))
                    filter.GuideId = guideId;
                Bind(filter);
            }
        }

        protected void BtnFilter_Click(object sender, EventArgs e)
        {
            int.TryParse(DdlGuide.SelectedValue, out int guideId);
            var url = ResolveUrl(PageUrl);
            Response.Redirect(guideId > 0 ? url + "?guide=" + guideId : url);
        }

        private void Bind(FollowUpFilter filter)
        {
            var report = _dashboard.GetFollowUp(UserId, filter);

            DdlGuide.Items.Clear();
            DdlGuide.Items.Add(new ListItem("Todas", string.Empty));
            foreach (var g in report.Guides)
                DdlGuide.Items.Add(new ListItem(g.AreaName, g.Id.ToString()) { Selected = g.Id == report.Applied.GuideId });

            var guides = report.Guides.ToDictionary(g => g.Id);
            var detailUrl = VirtualPathUtility.ToAbsolute("~/Pages/Admin/Operator.aspx") + "?id=";
            RptRows.DataSource = report.Rows.Select(r => new FollowUpListRow
            {
                Code1            = r.Code1,
                Name             = r.Name,
                GuideName        = guides[r.TrainingGuideId].AreaName,
                GuideTitle       = guides[r.TrainingGuideId].Title,
                StateText        = ReportText.State(r.State),
                StateCss         = ReportText.StateCss(r.State),
                LastActivityText = ReportText.Date(r.LastActivity),
                Reasons          = r.Reasons.SelectMany(ReasonLines).ToList(),
                DetailUrl        = detailUrl + r.UserId
            }).ToList();
            RptRows.DataBind();

            LblEmpty.Visible = report.Rows.Count == 0;
            int operators = report.Rows.Select(r => r.UserId).Distinct().Count();
            LitCount.Text = report.Rows.Count == 0 ? string.Empty : HttpUtility.HtmlEncode(string.Format(
                "{0} operadores · {1} casos (operador y guía)", operators, report.Rows.Count));
        }

        // Una línea por regla; S-3, una por paso.
        private static IEnumerable<string> ReasonLines(FollowUpReason reason)
        {
            switch (reason.Rule)
            {
                case FollowUpRule.RepeatedFailures:
                    return new[] { string.Format("S-1 · Reprobó {0} Assessment sin aprobar ninguno", reason.FailedAssessments) };
                case FollowUpRule.Inactive:
                    return new[] { string.Format("S-2 · {0} días sin sesiones de esta guía", reason.DaysInactive) };
                case FollowUpRule.StuckStep:
                    return reason.Steps.Select(s => string.Format("S-3 · {0}: sin acierto en {1} Assessment", s.StepName, s.Times));
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason.Rule, "Regla de seguimiento sin texto.");
            }
        }
    }
}
