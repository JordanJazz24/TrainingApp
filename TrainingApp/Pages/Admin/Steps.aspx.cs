using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Admin
{
    // Pasos (spec 018, AC-17 y AC-18). Solo UI: el ranking lo arma DashboardService con
    // StepRuns. Los filtros viven en la URL (?period=all&guide=…&mode=practice): nada se guarda
    // en Session.
    public partial class Steps : Page
    {
        private const string PageUrl = "~/Pages/Admin/Steps.aspx";

        // Los motivos que se ven debajo de cada paso; "Ver todos" muestra el resto (D-160).
        private const int VisibleReasons = 2;

        private readonly DashboardService _dashboard = new DashboardService();

        // Usuario autenticado (Session escrita solo por el servidor tras el login).
        private int UserId => (int)Session[Constants.SessionKeys.UserId];

        // La columna de saltos (AC-18), para el markup.
        protected bool ShowsSkips { get; private set; }

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
                Bind(ReportFilters.Steps(Request.QueryString));
        }

        protected void BtnFilter_Click(object sender, EventArgs e)
        {
            int.TryParse(DdlGuide.SelectedValue, out int guideId);
            Response.Redirect(BuildUrl(DdlPeriod.SelectedValue == ReportFilters.PeriodAll, guideId > 0 ? guideId : (int?)null,
                DdlMode.SelectedValue == ReportFilters.ModePractice));
        }

        private void Bind(StepFilter filter)
        {
            var report = _dashboard.GetSteps(UserId, filter);

            // Los controles muestran los filtros que de verdad se aplicaron.
            var applied = report.Applied;
            DdlPeriod.SelectedValue = applied.Period == ReportPeriod.All ? ReportFilters.PeriodAll : string.Empty;
            DdlMode.SelectedValue   = applied.Mode == SessionMode.FreePractice ? ReportFilters.ModePractice : string.Empty;
            DdlGuide.Items.Clear();
            DdlGuide.Items.Add(new ListItem("Todas", string.Empty));
            foreach (var g in report.Guides)
                DdlGuide.Items.Add(new ListItem(g.AreaName, g.Id.ToString()) { Selected = g.Id == applied.GuideId });

            // "Qué pasos fallan más en Assessment, últimos 30 días."
            LitScope.Text = HttpUtility.HtmlEncode(ReportText.Mode(applied.Mode) + ", " + ReportText.Period(applied.Period).ToLowerInvariant());
            ShowsSkips = report.ShowsSkips;
            PhSkipsNote.Visible = report.ShowsSkips;
            PhAllGuidesNote.Visible = !applied.GuideId.HasValue;

            var guides = report.Guides.ToDictionary(g => g.Id);
            RptGuides.DataSource = report.Rankings.Select(r => new StepGuideGroup
            {
                GuideName  = r.TrainingGuideId.HasValue ? guides[r.TrainingGuideId.Value].AreaName : "Todas las guías",
                GuideTitle = r.TrainingGuideId.HasValue ? guides[r.TrainingGuideId.Value].Title : string.Empty,
                Rows       = r.Steps.Select((s, i) =>
                {
                    var reasons = s.Reasons.Select(ReportText.ReasonCount).ToList();
                    return new StepListRow
                    {
                        Rank                = i + 1,
                        StepName            = s.StepName,
                        FailureRateText     = ReportText.Percent(s.FailureRate),
                        FailureWidth        = ReportText.BarWidth((s.FailureRate ?? 0m) * 100m),
                        SessionsText        = ReportText.SessionsWithFailure(s.SessionsWithFailure, s.Sessions),
                        FailedAttempts      = s.FailedAttempts,
                        TimesWithoutSuccess = s.TimesWithoutSuccess,
                        Skips               = s.Skips,
                        MedianText          = ReportText.Duration(s.MedianDuration),
                        TopReasons          = reasons.Take(VisibleReasons).ToList(),
                        MoreReasons         = reasons.Skip(VisibleReasons).ToList()
                    };
                }).ToList()
            }).ToList();
            RptGuides.DataBind();
            RptGuides.Visible = report.Rankings.Count > 0;
            PnlEmpty.Visible  = report.Rankings.Count == 0;
            LnkExport.NavigateUrl = ReportFilters.ExportUrl(ReportFilters.ViewSteps, Request.QueryString);
        }

        private string BuildUrl(bool allPeriod, int? guideId, bool practice)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (allPeriod) query["period"] = ReportFilters.PeriodAll;
            if (guideId.HasValue) query["guide"] = guideId.Value.ToString();
            if (practice) query["mode"] = ReportFilters.ModePractice;
            var url = ResolveUrl(PageUrl);
            return query.Count == 0 ? url : url + "?" + query;
        }
    }
}
