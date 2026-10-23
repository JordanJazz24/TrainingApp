using System;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.BLL.Reports;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Admin
{
    // Resumen (spec 018, AC-9 a AC-12), el inicio del administrador. Solo UI: los números los
    // calcula DashboardService; la página arma las tablas y los datos de las gráficas. Los filtros
    // viven en la URL (?period=all&guide=…): nada se guarda en Session.
    public partial class Summary : Page
    {
        private const string PageUrl  = "~/Pages/Admin/Summary.aspx";
        private const string StepsUrl = "~/Pages/Admin/Steps.aspx";
        private const string PeriodAll = "all";

        private readonly DashboardService _dashboard = new DashboardService();

        // Usuario autenticado (Session escrita solo por el servidor tras el login).
        private int UserId => (int)Session[Constants.SessionKeys.UserId];

        // Datos de las gráficas, serializados para el <script> (JavaScriptSerializer escapa < y >).
        protected string ChartJson { get; private set; } = "null";

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
                Bind(ReadFilter());
        }

        protected void BtnFilter_Click(object sender, EventArgs e)
        {
            int.TryParse(DdlGuide.SelectedValue, out int guideId);
            Response.Redirect(BuildUrl(PageUrl, DdlPeriod.SelectedValue == PeriodAll, guideId > 0 ? guideId : (int?)null));
        }

        // Los filtros de la URL; un valor inválido vale el de inicio (el servicio también valida).
        private SummaryFilter ReadFilter()
        {
            var filter = new SummaryFilter
            {
                Period = Request.QueryString["period"] == PeriodAll ? ReportPeriod.All : ReportPeriod.Last30Days
            };
            if (int.TryParse(Request.QueryString["guide"], out int guideId))
                filter.GuideId = guideId;
            return filter;
        }

        private void Bind(SummaryFilter filter)
        {
            var report  = _dashboard.GetSummary(UserId, filter);
            var applied = report.Applied;
            bool all    = applied.Period == ReportPeriod.All;

            DdlPeriod.SelectedValue = all ? PeriodAll : string.Empty;
            DdlGuide.Items.Clear();
            DdlGuide.Items.Add(new ListItem("Todas", string.Empty));
            foreach (var g in report.Guides)
                DdlGuide.Items.Add(new ListItem(g.AreaName, g.Id.ToString()) { Selected = g.Id == applied.GuideId });

            // Cada bloque dice a qué período corresponde (AC-9).
            string period = ReportText.Period(applied.Period);
            LitRatesPeriod.Text    = HttpUtility.HtmlEncode(period);
            LitActivityPeriod.Text = HttpUtility.HtmlEncode(period);
            LitTopPeriod.Text      = HttpUtility.HtmlEncode(period);
            // Pasos con el mismo período y guía, en Assessment: los mismos números (D-147).
            LnkSteps.NavigateUrl = BuildUrl(StepsUrl, all, applied.GuideId);

            var guides = report.Guides.ToDictionary(g => g.Id);
            var rows = report.GuideSummaries.Select(s => new SummaryGuideRow
            {
                GuideName        = guides[s.TrainingGuideId].AreaName,
                GuideTitle       = guides[s.TrainingGuideId].Title,
                Passed           = s.Passed,
                Failed           = s.Failed,
                InTraining       = s.InTraining,
                Assessed         = s.AssessedOperators,
                PassRateText     = ReportText.Percent(s.PassRate),
                FirstAttemptText = ReportText.Percent(s.FirstAttemptRate),
                AverageText      = ReportText.Score(s.AverageScore)
            }).ToList();

            bool hasStates = rows.Any(r => r.Passed + r.Failed + r.InTraining > 0);
            PhStates.Visible    = hasStates;
            LblNoStates.Visible = !hasStates;
            RptStates.DataSource = rows;
            RptStates.DataBind();
            // Sin Assessment completados en el período no hay tasa ni puntaje que mostrar (AC-11).
            bool hasRates = report.GuideSummaries.Any(s => s.AssessedOperators + s.FirstAttemptOperators > 0);
            PhRates.Visible    = hasRates;
            LblNoRates.Visible = !hasRates;
            RptRates.DataSource = rows;
            RptRates.DataBind();

            bool hasActivity = report.Weeks.Count > 0;
            PhActivity.Visible    = hasActivity;
            LblNoActivity.Visible = !hasActivity;
            if (hasActivity)
                LitActivityTotals.Text = HttpUtility.HtmlEncode(string.Format(
                    "En el período: {0} Guided · {1} Assessment · {2} Práctica libre",
                    report.Weeks.Sum(w => w.Guided), report.Weeks.Sum(w => w.Assessment), report.Weeks.Sum(w => w.FreePractice)));

            bool hasTop = report.TopSteps.Count > 0;
            PhTop.Visible    = hasTop;
            LblNoTop.Visible = !hasTop;
            RptTop.DataSource = report.TopSteps.Select(s => new SummaryTopRow
            {
                StepName        = s.StepName,
                Sessions        = s.Sessions,
                FailureRateText = ReportText.Percent(s.FailureRate)
            }).ToList();
            RptTop.DataBind();

            ChartJson = new JavaScriptSerializer().Serialize(new
            {
                states = hasStates ? new
                {
                    labels   = rows.Select(r => r.GuideName),
                    passed   = rows.Select(r => r.Passed),
                    failed   = rows.Select(r => r.Failed),
                    training = rows.Select(r => r.InTraining)
                } : null,
                weeks = hasActivity ? new
                {
                    // Con todo el historial las semanas pueden ser de años distintos.
                    labels     = report.Weeks.Select(w => w.WeekStart.ToString(all ? "dd/MM/yy" : "dd/MM")),
                    guided     = report.Weeks.Select(w => w.Guided),
                    assessment = report.Weeks.Select(w => w.Assessment),
                    practice   = report.Weeks.Select(w => w.FreePractice)
                } : null,
                top = hasTop ? new
                {
                    labels   = report.TopSteps.Select(s => s.StepName),
                    rates    = report.TopSteps.Select(s => Math.Round((s.FailureRate ?? 0m) * 100m, 1)),
                    sessions = report.TopSteps.Select(s => s.Sessions)
                } : null
            });
        }

        private string BuildUrl(string page, bool allPeriod, int? guideId)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (allPeriod) query["period"] = PeriodAll;
            if (guideId.HasValue) query["guide"] = guideId.Value.ToString();
            var url = ResolveUrl(page);
            return query.Count == 0 ? url : url + "?" + query;
        }
    }
}
