using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;
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
                Bind(ReportFilters.FollowUp(Request.QueryString));
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

            // La cola en dos partes (spec 022, brief 5): "Llamar primero" y "Después". Quién va en
            // cada una lo decide FollowUpSummary; el orden es el de la BLL.
            var guides = report.Guides.ToDictionary(g => g.Id);
            var detailUrl = VirtualPathUtility.ToAbsolute("~/Pages/Admin/Operator.aspx") + "?id=";
            var first = report.Rows.Where(FollowUpSummary.IsCallFirst).ToList();
            var later = report.Rows.Where(r => !FollowUpSummary.IsCallFirst(r)).ToList();
            RptFirst.DataSource = first.Select(r => ListRow(r, guides, detailUrl)).ToList();
            RptFirst.DataBind();
            RptLater.DataSource = later.Select(r => ListRow(r, guides, detailUrl)).ToList();
            RptLater.DataBind();
            LnkExport.NavigateUrl = ReportFilters.ExportUrl(ReportFilters.ViewFollowUp, Request.QueryString);

            var sum = report.Summary;
            bool any = sum.Cases > 0;
            PhTabs.Visible   = any;
            PhQueue.Visible  = any;
            PnlEmpty.Visible = !any;
            PhFirst.Visible    = sum.CallFirst > 0;
            PnlNoFirst.Visible = sum.CallFirst == 0;
            PhLater.Visible    = sum.Later > 0;
            PhTabLater.Visible = sum.Later > 0;

            LitTabFirst.Text   = sum.CallFirst.ToString();
            LitTabLater.Text   = sum.Later.ToString();
            LitS1.Text         = sum.RepeatedFailures.ToString();
            LitS2.Text         = sum.Inactive.ToString();
            LitS3.Text         = sum.StuckStep.ToString();
            LitCount.Text      = HttpUtility.HtmlEncode(string.Format("{0} de {1}",
                ReportText.Cases(sum.Cases), sum.Operators == 1 ? "1 operador" : sum.Operators + " operadores"));
            LitFirstCount.Text = HttpUtility.HtmlEncode(ReportText.Cases(sum.CallFirst) + " con "
                + FollowUpSummary.CallFirstMinRules + " o más reglas");
            LitLaterCount.Text = HttpUtility.HtmlEncode(ReportText.Cases(sum.Later) + " con 1 regla");

            DashboardGuide applied;
            LitEmptyTitle.Text = HttpUtility.HtmlEncode(report.Applied.GuideId.HasValue
                && guides.TryGetValue(report.Applied.GuideId.Value, out applied)
                ? "Nadie en la cola de " + applied.AreaName
                : "Nadie en la cola");

            // Columna lateral: los casos de cada regla y su barra sobre el total.
            LitTotal.Text     = sum.Cases.ToString();
            LitTotalText.Text = HttpUtility.HtmlEncode((sum.Cases == 1 ? "caso" : "casos") + " de "
                + (sum.Operators == 1 ? "1 operador" : sum.Operators + " operadores"));
            LitBarS1.Text = sum.RepeatedFailures.ToString();
            LitBarS2.Text = sum.Inactive.ToString();
            LitBarS3.Text = sum.StuckStep.ToString();
            BarS1.Style["width"] = Percent(sum.RepeatedFailures, sum.Cases);
            BarS2.Style["width"] = Percent(sum.Inactive, sum.Cases);
            BarS3.Style["width"] = Percent(sum.StuckStep, sum.Cases);
        }

        private static FollowUpListRow ListRow(FollowUpRow r, IDictionary<int, DashboardGuide> guides, string detailUrl)
            => new FollowUpListRow
            {
                Code1            = r.Code1,
                Name             = r.Name,
                GuideName        = guides[r.TrainingGuideId].AreaName,
                GuideTitle       = guides[r.TrainingGuideId].Title,
                StateText        = ReportText.State(r.State),
                StateCss         = ReportText.StateCss(r.State),
                StateIcon        = ReportText.StateIcon(r.State),
                LastActivityText = ReportText.Date(r.LastActivity),
                RuleCountText    = ReportText.FollowUpRuleCount(r.Reasons.Count),
                Lines            = r.Reasons.SelectMany(ReportText.FollowUpReasonLines).ToList(),
                DetailUrl        = detailUrl + r.UserId
            };

        // Ancho de la barra (solo presentación): la parte del total, en %.
        private static string Percent(int n, int total)
            => (total == 0 ? 0 : (int)Math.Round(n * 100.0 / total)) + "%";

    }
}
