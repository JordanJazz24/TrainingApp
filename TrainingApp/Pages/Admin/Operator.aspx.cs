using System;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using TrainingApp.BLL.Auth;
using TrainingApp.BLL.Reports;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Admin
{
    // Detalle de un Operador (spec 018, AC-14). Solo UI: el estado lo decide ReadinessPolicy y
    // los pasos a reforzar ReinforcementRule, por DashboardService.
    public partial class Operator : Page
    {
        private const string OperatorsUrl = "~/Pages/Admin/Operators.aspx";

        private readonly DashboardService _dashboard = new DashboardService();

        // Usuario autenticado (Session escrita solo por el servidor tras el login).
        private int UserId => (int)Session[Constants.SessionKeys.UserId];

        // Datos de las curvas para el <script>, en el orden de RptCurves (JavaScriptSerializer
        // escapa < y >).
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

            // Un id inexistente, de un administrador o manipulado: vuelta a Operadores con un
            // aviso, sin datos de otro usuario.
            var detail = int.TryParse(Request.QueryString["id"], out int operatorId)
                ? _dashboard.GetOperator(UserId, operatorId) : null;
            if (detail == null)
            {
                Response.Redirect(OperatorsUrl + "?missing=1");
                return;
            }

            Bind(detail);
        }

        private void Bind(OperatorDetail detail)
        {
            Title = detail.Name;
            LnkBack.NavigateUrl = OperatorsUrl;
            LitName.Text  = HttpUtility.HtmlEncode(detail.Name);
            LitCode1.Text = HttpUtility.HtmlEncode(detail.Code1);
            LitArea.Text  = HttpUtility.HtmlEncode(detail.AreaText);
            LitPrinted.Text = HttpUtility.HtmlEncode(ReportText.Date(DateTime.UtcNow));

            var guides     = detail.Guides.ToDictionary(g => g.Id);
            var guideNames = detail.Guides.ToDictionary(g => g.Id, g => g.AreaName);

            // Su área (spec 024, D-269): sus guías, también Sin iniciar; sin guías que entrenar,
            // el texto atenuado (D-267, P-5).
            RptGuides.DataSource = detail.AreaGuides.Select(r => Card(guides[r.TrainingGuideId], r)).ToList();
            RptGuides.DataBind();
            LblAreaNote.Text    = HttpUtility.HtmlEncode(ReportText.AreaNote(detail.AreaStatus) ?? string.Empty);
            LblAreaNote.Visible = detail.AreaGuides.Count == 0;

            // Otras áreas (entrenamiento cruzado): en pantalla, un desplegable cerrado (abierto sin
            // área, P-8); en la ficha, una línea (D-270, P-9).
            PhOtherGuides.Visible = detail.OtherGuides.Count > 0;
            if (detail.AreaStatus == OperatorAreaStatus.NoArea)
                DetOtherGuides.Attributes["open"] = "open";
            RptOtherGuides.DataSource = detail.OtherGuides.Select(r => Card(guides[r.TrainingGuideId], r)).ToList();
            RptOtherGuides.DataBind();
            LitAlsoTrained.Text = HttpUtility.HtmlEncode("También entrenó: " + string.Join("; ", detail.OtherGuides.Select(r =>
                AreaLabel.Of(guides[r.TrainingGuideId].ProductName, guides[r.TrainingGuideId].AreaName)
                + " — " + ReportText.State(r.State))) + ".");

            RptReinforcement.DataSource = detail.Reinforcement.Select(r => new OperatorReinforcementGroup
            {
                GuideName = guideNames[r.TrainingGuideId],
                Steps     = r.Steps.Select(s => new OperatorReinforcementRow
                {
                    StepName            = s.StepName,
                    TimesWithoutSuccess = s.TimesWithoutSuccess,
                    FailedAttempts      = s.FailedAttempts,
                    MainReasonText      = ReportText.Reason(s.MainReason)
                }).ToList()
            }).ToList();
            RptReinforcement.DataBind();
            PnlNoReinforcement.Visible = detail.Reinforcement.Count == 0;

            RptSessions.DataSource = detail.Sessions.Select(row => new OperatorSessionListRow
            {
                StartedText    = ReportText.Date(row.Session.StartedAt),
                AreaText       = row.AreaText,
                GuideTitle     = guides[row.Session.TrainingGuideId].Title,
                ModeText       = ReportText.Mode(row.Session.Mode),
                StatusText     = ReportText.Status(row.Session.Status),
                ScoreText      = ReportText.Score(row.Score),
                DurationText   = ReportText.Duration(row.Duration),
                FailedAttempts = row.FailedAttempts
            }).ToList();
            RptSessions.DataBind();
            PhSessions.Visible    = detail.Sessions.Count > 0;
            LblNoSessions.Visible = detail.Sessions.Count == 0;

            BindCurves(detail);
        }

        // La tarjeta de una guía (su área u otra).
        private static OperatorGuideCard Card(DashboardGuide guide, GuideReadiness readiness)
        {
            return new OperatorGuideCard
            {
                GuideName        = guide.AreaName,
                GuideTitle       = guide.Title,
                StateText        = ReportText.State(readiness.State),
                StateCss         = ReportText.StateCss(readiness.State),
                StateIcon        = ReportText.StateIcon(readiness.State),
                BestText         = ReportText.Score(readiness.BestScore),
                LastText         = ReportText.Score(readiness.LastScore),
                LastActivityText = ReportText.Date(readiness.LastActivity)
            };
        }

        // Curva de aprendizaje (AC-15): una tarjeta por guía que entrenó, con su gráfica.
        private void BindCurves(OperatorDetail detail)
        {
            var guides = detail.Guides.ToDictionary(g => g.Id);
            PhCurves.Visible = detail.Curves.Count > 0;
            RptCurves.DataSource = detail.Curves.Select(c => new OperatorCurveCard
            {
                GuideName    = guides[c.TrainingGuideId].AreaName,
                GuideTitle   = guides[c.TrainingGuideId].Title,
                HasPoints    = c.Points.Count > 0,
                PracticeText = ReportText.PracticeBeforeFirstPass(c.PracticeBeforeFirstPass),
                ScoresText   = c.Points.Any(p => p.Score.HasValue)
                    ? "Puntajes de Assessment: " + string.Join(" · ", c.Points.Where(p => p.Score.HasValue).Select(p => ReportText.Score(p.Score)))
                    : string.Empty
            }).ToList();
            RptCurves.DataBind();

            ChartJson = new JavaScriptSerializer().Serialize(detail.Curves.Select(c => c.Points.Count == 0 ? null : new
            {
                // "#1 · 05/10": el número de la sesión en la curva y su fecha en la planta.
                labels     = c.Points.Select((p, i) => "#" + (i + 1) + " · " + SessionHelper.ToLocal(p.StartedAt).ToString("dd/MM")),
                practice   = c.Points.Select(p => p.Mode == SessionMode.FreePractice ? (int?)p.FailedAttempts : null),
                assessment = c.Points.Select(p => p.Mode == SessionMode.Assessment ? (int?)p.FailedAttempts : null),
                scores     = c.Points.Select(p => p.Score.HasValue ? ReportText.Score(p.Score) : null)
            }));
        }
    }
}
