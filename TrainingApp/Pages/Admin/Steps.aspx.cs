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

            LitScope.Text = HttpUtility.HtmlEncode(ReportText.Period(applied.Period) + " · " + ReportText.Mode(applied.Mode));
            ShowsSkips = report.ShowsSkips;
            PhSkipsNote.Visible = report.ShowsSkips;
            PhAllGuidesNote.Visible = !applied.GuideId.HasValue;

            var guides = report.Guides.ToDictionary(g => g.Id);
            RptGuides.DataSource = report.Rankings.Select(r => new StepGuideGroup
            {
                GuideName  = r.TrainingGuideId.HasValue ? guides[r.TrainingGuideId.Value].AreaName : "Todas las guías",
                GuideTitle = r.TrainingGuideId.HasValue ? guides[r.TrainingGuideId.Value].Title : string.Empty,
                Rows       = r.Steps.Select(s => new StepListRow
                {
                    StepName            = s.StepName,
                    Sessions            = s.Sessions,
                    FailureRateText     = ReportText.Percent(s.FailureRate),
                    FailedAttempts      = s.FailedAttempts,
                    TimesWithoutSuccess = s.TimesWithoutSuccess,
                    Skips               = s.Skips,
                    MedianText          = ReportText.Duration(s.MedianDuration),
                    Reasons             = s.Reasons.Select(ReportText.ReasonCount).ToList()
                }).ToList()
            }).ToList();
            RptGuides.DataBind();
            LblEmpty.Visible = report.Rankings.Count == 0;
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
