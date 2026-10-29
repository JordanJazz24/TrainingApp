using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;
using TrainingApp.Shared;
using TrainingApp.UI.Master;

namespace TrainingApp.Pages.Admin
{
    // Pasos (spec 018, AC-17 y AC-18; brief 022, alternativa 4a). Solo UI: el ranking y el
    // cambio de cada paso contra el período anterior los arma DashboardService con StepRuns. Los
    // filtros viven en la URL (?period=…&from=…&to=…&guide=…&mode=practice, spec 023): nada se
    // guarda en Session.
    public partial class Steps : Page
    {
        private const string PageUrl = "~/Pages/Admin/Steps.aspx";

        // Los motivos que se ven debajo de cada paso; "Ver todos" muestra el resto (D-160).
        private const int VisibleReasons = 2;

        private readonly DashboardService _dashboard = new DashboardService();

        // Usuario autenticado (Session escrita solo por el servidor tras el login).
        private int UserId => (int)Session[Constants.SessionKeys.UserId];

        // Los saltos (AC-18), para el markup.
        protected bool ShowsSkips { get; private set; }

        // El ranking a la vista: su guía (o "Todas las guías"), el título de la guía y cuántos
        // pasos tiene (las columnas).
        protected string GroupName { get; private set; }
        protected string GroupTitle { get; private set; }
        protected int StepCount { get; private set; }

        // Franja de lado a lado bajo la barra superior (brief 022, 4a, D-281).
        protected void Page_Init(object sender, EventArgs e)
        {
            ((AdminMaster)Master).WideMain = true;
        }

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
            Response.Redirect(BuildUrl(Period.Period, Period.From, Period.To, guideId > 0 ? guideId : (int?)null,
                RblMode.SelectedValue == ReportFilters.ModePractice));
        }

        private void Bind(StepFilter filter)
        {
            var report = _dashboard.GetSteps(UserId, filter);

            // Los controles muestran los filtros que de verdad se aplicaron.
            var applied = report.Applied;
            var range   = report.Range;
            Period.Bind(range);
            LblInvalidPeriod.Visible = range.Invalid;
            RblMode.SelectedValue   = applied.Mode == SessionMode.FreePractice ? ReportFilters.ModePractice : string.Empty;
            DdlGuide.Items.Clear();
            DdlGuide.Items.Add(new ListItem("Todas", string.Empty));
            foreach (var g in report.Guides)
                DdlGuide.Items.Add(new ListItem(g.AreaName, g.Id.ToString()) { Selected = g.Id == applied.GuideId });

            // "Qué pasos fallan más en Moldeo, en Assessment." y, debajo, el rango aplicado y el
            // anterior (el control de período no lo pinta aquí).
            var guide = applied.GuideId.HasValue ? report.Guides.FirstOrDefault(g => g.Id == applied.GuideId.Value) : null;
            LitScope.Text     = HttpUtility.HtmlEncode((guide?.AreaName ?? "todas las guías") + ", en " + ReportText.Mode(applied.Mode));
            LitRange.Text     = HttpUtility.HtmlEncode(ReportText.Range(range));
            LitPrevRange.Text = range.Compares
                ? HttpUtility.HtmlEncode(" · se compara con " + ReportText.PreviousRange(range))
                : string.Empty;
            ShowsSkips = report.ShowsSkips;
            PhSkipsNote.Visible = report.ShowsSkips;
            PhAllGuidesNote.Visible = !applied.GuideId.HasValue;

            // Con una guía, su ranking; con todas, un solo resumen (nunca más de uno).
            var ranking = report.Rankings.FirstOrDefault();
            PhRanking.Visible = ranking != null;
            PnlEmpty.Visible  = ranking == null;
            if (ranking != null)
            {
                var named  = ranking.TrainingGuideId.HasValue ? report.Guides.First(g => g.Id == ranking.TrainingGuideId.Value) : null;
                GroupName  = named?.AreaName ?? "Todas las guías";
                GroupTitle = named?.Title ?? string.Empty;
                var rows   = ranking.Steps.Select((s, i) => Row(s, i + 1, range)).ToList();
                StepCount  = rows.Count;
                foreach (var repeater in new[] { RptColumns, RptLabels, RptDetails, RptList })
                {
                    repeater.DataSource = rows;
                    repeater.DataBind();
                }
            }
            LnkExport.NavigateUrl = ReportFilters.ExportUrl(ReportFilters.ViewSteps, Request.QueryString);
        }

        private static StepListRow Row(StepStats s, int rank, ReportRange range)
        {
            var change = s.FailureRateChange;
            var view   = ChangeView.Of(change, range, "sesión", "sesiones");
            bool hasPrevious = change != null && change.HasComparison && change.Previous.HasValue;

            // La barra de cada motivo, contra el más repetido; su parte del total la trae la BLL
            // (ReasonCount.Share, D-283).
            int most = s.Reasons.Count > 0 ? s.Reasons.Max(r => r.Count) : 0;
            return new StepListRow
            {
                Rank                = rank,
                StepName            = s.StepName,
                FailureRateText     = ReportText.Percent(s.FailureRate),
                FailureWidth        = ReportText.BarWidth((s.FailureRate ?? 0m) * 100m),
                SessionsText        = ReportText.SessionsWithFailure(s.SessionsWithFailure, s.Sessions),
                FailureRateChange   = view,
                ChangeVerdict       = string.IsNullOrEmpty(view.Verdict) ? string.Empty
                                      : char.ToUpperInvariant(view.Verdict[0]) + view.Verdict.Substring(1),
                HasPrevious         = hasPrevious,
                PreviousRateText    = hasPrevious ? ReportText.Percent(change.Previous) : string.Empty,
                PreviousWidth       = hasPrevious ? ReportText.BarWidth(change.Previous.Value * 100m) : "0",
                FailedAttempts      = s.FailedAttempts,
                TimesWithoutSuccess = s.TimesWithoutSuccess,
                Skips               = s.Skips,
                MedianText          = ReportText.Duration(s.MedianDuration),
                TopReasons          = s.Reasons.Take(VisibleReasons).Select(r => new StepReasonView
                {
                    Reason    = ReportText.Reason(r.Reason),
                    Action    = r.Action ?? ReportText.None,
                    CountText = r.Count + (r.Count == 1 ? " vez" : " veces"),
                    ShareText = ReportText.Percent(r.Share),
                    Width     = ReportText.BarWidth(100m * r.Count / most)
                }).ToList(),
                MoreReasons         = s.Reasons.Skip(VisibleReasons).Select(ReportText.ReasonCount).ToList(),
                FirstReason         = s.Reasons.Select(ReportText.ReasonCount).FirstOrDefault() ?? string.Empty
            };
        }

        private string BuildUrl(ReportPeriod period, DateTime? from, DateTime? to, int? guideId, bool practice)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            ReportFilters.WritePeriod(query, period, from, to);
            if (guideId.HasValue) query["guide"] = guideId.Value.ToString();
            if (practice) query["mode"] = ReportFilters.ModePractice;
            var url = ResolveUrl(PageUrl);
            return query.Count == 0 ? url : url + "?" + query;
        }
    }
}
