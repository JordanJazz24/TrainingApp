using System;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Admin
{
    // Resumen (spec 018, AC-9 a AC-12; diseño de la Fase 11), el inicio del administrador. Solo
    // UI: los números los calcula DashboardService; la página arma los textos y las semanas de
    // la gráfica. Los filtros
    // viven en la URL (?period=all&guide=…): nada se guarda en Session.
    public partial class Summary : Page
    {
        private const string PageUrl  = "~/Pages/Admin/Summary.aspx";
        private const string StepsUrl = "~/Pages/Admin/Steps.aspx";
        private const string FollowUpUrl = "~/Pages/Admin/FollowUp.aspx";

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
                Bind(ReportFilters.Summary(Request.QueryString));
        }

        protected void BtnFilter_Click(object sender, EventArgs e)
        {
            int.TryParse(DdlGuide.SelectedValue, out int guideId);
            Response.Redirect(BuildUrl(PageUrl, DdlPeriod.SelectedValue == ReportFilters.PeriodAll, guideId > 0 ? guideId : (int?)null));
        }

        private void Bind(SummaryFilter filter)
        {
            var report  = _dashboard.GetSummary(UserId, filter);
            var applied = report.Applied;
            bool all    = applied.Period == ReportPeriod.All;

            DdlPeriod.SelectedValue = all ? ReportFilters.PeriodAll : string.Empty;
            DdlGuide.Items.Clear();
            DdlGuide.Items.Add(new ListItem("Todas", string.Empty));
            foreach (var g in report.Guides)
                DdlGuide.Items.Add(new ListItem(g.AreaName, g.Id.ToString()) { Selected = g.Id == applied.GuideId });

            // Cada bloque dice a qué período corresponde (AC-9).
            string period = ReportText.Period(applied.Period);
            LitReadyPeriod.Text    = HttpUtility.HtmlEncode("Estado con todo el historial. Tasas y puntaje: " + period.ToLowerInvariant() + ".");
            LitActivityPeriod.Text = HttpUtility.HtmlEncode(
                "Sesiones " + ReportText.Grain(report.Activity.Grain) + ", " + period.ToLowerInvariant());
            LitTopPeriod.Text      = HttpUtility.HtmlEncode(period.ToLowerInvariant());
            // Pasos con el mismo período y guía, en Assessment: los mismos números (D-147).
            LnkSteps.NavigateUrl    = BuildUrl(StepsUrl, all, applied.GuideId);
            // Seguimiento solo filtra por guía (sus reglas no dependen del período).
            LnkFollowUp.NavigateUrl = BuildUrl(FollowUpUrl, false, applied.GuideId);
            // Las sesiones con el período y la guía del Resumen (AC-22).
            LnkExport.NavigateUrl = ReportFilters.ExportUrl(ReportFilters.ViewSessions, Request.QueryString);
            LblExportSlow.Visible = all;

            var guides = report.Guides.ToDictionary(g => g.Id);
            var rows = report.GuideSummaries.Select(s => GuideRow(s, guides[s.TrainingGuideId])).ToList();

            // Sin estados ni tasas no hay nada que mostrar en "¿Quién está listo?" (AC-11).
            bool hasStates = report.GuideSummaries.Any(s =>
                s.Passed + s.Failed + s.InTraining + s.AssessedOperators + s.FirstAttemptOperators > 0);
            PhStates.Visible    = hasStates;
            LblNoStates.Visible = !hasStates;
            RptGuides.DataSource = rows;
            RptGuides.DataBind();

            var buckets = report.Activity.Buckets;
            bool hasActivity = buckets.Count > 0;
            PhActivity.Visible    = hasActivity;
            LblNoActivity.Visible = !hasActivity;
            if (hasActivity)
                LitActivityTotals.Text = HttpUtility.HtmlEncode(string.Format(
                    "En el período: {0} Guided, {1} Práctica libre y {2} Assessment.",
                    buckets.Sum(w => w.Guided), buckets.Sum(w => w.FreePractice), buckets.Sum(w => w.Assessment)));

            bool hasTop = report.TopSteps.Count > 0;
            PhTop.Visible    = hasTop;
            LblNoTop.Visible = !hasTop;
            RptTop.DataSource = report.TopSteps.Select(s => new SummaryTopRow
            {
                StepName        = s.StepName,
                FailureRateText = ReportText.Percent(s.FailureRate),
                FailureWidth    = ReportText.BarWidth((s.FailureRate ?? 0m) * 100m),
                Detail          = ReportText.SessionsWithFailure(s.SessionsWithFailure, s.Sessions)
            }).ToList();
            RptTop.DataBind();

            ChartJson = new JavaScriptSerializer().Serialize(new
            {
                weeks = hasActivity ? new
                {
                    // Por mes, o con todo el historial, pueden ser de años distintos.
                    labels     = buckets.Select(w => w.Start.ToString(
                        report.Activity.Grain == ActivityGrain.Month ? "MM/yy" : all ? "dd/MM/yy" : "dd/MM")),
                    guided     = buckets.Select(w => w.Guided),
                    practice   = buckets.Select(w => w.FreePractice),
                    assessment = buckets.Select(w => w.Assessment)
                } : null
            });
        }

        // Una guía de "¿Quién está listo?": la barra apilada de estados y las tres cifras con su
        // detalle ("22 de 28 con Assessment").
        private static SummaryGuideRow GuideRow(GuideSummary s, DashboardGuide guide)
        {
            int total = s.Passed + s.Failed + s.InTraining;
            return new SummaryGuideRow
            {
                GuideName          = guide.AreaName,
                GuideTitle         = guide.Title,
                Passed             = s.Passed,
                Failed             = s.Failed,
                InTraining         = s.InTraining,
                TotalText          = total == 1 ? "1 operador" : total + " operadores",
                PassedWidth        = ReportText.BarWidth(Share(s.Passed, total)),
                FailedWidth        = ReportText.BarWidth(Share(s.Failed, total)),
                InTrainingWidth    = ReportText.BarWidth(Share(s.InTraining, total)),
                StatesAria         = string.Format("{0}: {1} aprobados, {2} reprobados, {3} en entrenamiento",
                                         guide.AreaName, s.Passed, s.Failed, s.InTraining),
                PassRateText       = ReportText.Percent(s.PassRate),
                PassRateDetail     = s.AssessedOperators > 0
                    ? s.PassedOperators + " de " + s.AssessedOperators + " con Assessment"
                    : "Sin Assessment en el período",
                FirstAttemptText   = ReportText.Percent(s.FirstAttemptRate),
                FirstAttemptDetail = s.FirstAttemptOperators > 0
                    ? s.FirstAttemptPassed + " de " + s.FirstAttemptOperators
                    : "Sin primeros intentos",
                AverageText        = ReportText.Score(s.AverageScore)
            };
        }

        private static decimal Share(int part, int total) => total > 0 ? part * 100m / total : 0m;

        private string BuildUrl(string page, bool allPeriod, int? guideId)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (allPeriod) query["period"] = ReportFilters.PeriodAll;
            if (guideId.HasValue) query["guide"] = guideId.Value.ToString();
            var url = ResolveUrl(page);
            return query.Count == 0 ? url : url + "?" + query;
        }
    }
}
