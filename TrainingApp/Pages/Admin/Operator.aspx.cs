using System;
using System.Collections.Generic;
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
    // Detalle de un Operador (spec 018, AC-14; spec 022, brief 7, alternativa H). Solo UI: el
    // estado lo decide ReadinessPolicy, los pasos a reforzar ReinforcementRule y las métricas
    // derivadas OperatorActivity, por DashboardService.
    public partial class Operator : Page
    {
        private const string OperatorsUrl = "~/Pages/Admin/Operators.aspx";

        // Ancho máximo, en px, de la barrita de intentos fallidos de la tabla.
        private const int FailBarWidth = 44;

        private readonly DashboardService _dashboard = new DashboardService();

        // Usuario autenticado (Session escrita solo por el servidor tras el login).
        private int UserId => (int)Session[Constants.SessionKeys.UserId];

        // Datos de las curvas para el <script>, en el orden de RptHistory (JavaScriptSerializer
        // escapa < y >).
        protected string ChartJson { get; private set; } = "null";

        // Encabezado y "¿Qué ha hecho?".
        protected bool HasArea { get; private set; }
        protected int GuidedCount { get; private set; }
        protected int PracticeCount { get; private set; }
        protected int AssessmentCount { get; private set; }
        protected string MixLabel { get; private set; } = string.Empty;

        // "Ver también otras áreas (N pasos)".
        protected string OtherStepsShowText { get; private set; } = string.Empty;

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
            HasArea = detail.AreaStatus != OperatorAreaStatus.NoArea;

            var guides    = detail.Guides.ToDictionary(g => g.Id);
            var history   = detail.Activity.Guides.ToDictionary(h => h.TrainingGuideId);
            var readiness = detail.AreaGuides.Concat(detail.OtherGuides).ToDictionary(r => r.TrainingGuideId);
            var areaIds   = new HashSet<int>(detail.AreaGuides.Select(r => r.TrainingGuideId));

            BindReady(detail, guides, history);
            BindReinforcement(detail, guides, areaIds);
            BindActivity(detail, guides);

            // Las sesiones de cada bloque y la tabla única de la ficha; "Mejor", en la sesión con
            // el mejor puntaje de su guía.
            var best    = new HashSet<int>(detail.Activity.Guides.Where(h => h.BestSessionId.HasValue).Select(h => h.BestSessionId.Value));
            int maxFail = Math.Max(1, detail.Sessions.Count == 0 ? 0 : detail.Sessions.Max(r => r.FailedAttempts));
            var rows    = detail.Sessions.ToDictionary(r => r.Session.SessionId, r => SessionRow(r, guides, best, maxFail));

            RptHistory.DataSource = detail.Curves.Select((c, i) => HistoryBlock(detail, c, i, guides, readiness[c.TrainingGuideId],
                history[c.TrainingGuideId], areaIds.Contains(c.TrainingGuideId),
                detail.Sessions.Where(r => r.Session.TrainingGuideId == c.TrainingGuideId).Select(r => rows[r.Session.SessionId]).ToList())).ToList();
            RptHistory.DataBind();
            PhHistory.Visible = detail.Curves.Count > 0;

            RptSessions.DataSource = detail.Sessions.Select(r => rows[r.Session.SessionId]).ToList();
            RptSessions.DataBind();
            PhPrintSessions.Visible = detail.Sessions.Count > 0;
            LblNoSessions.Visible   = detail.Sessions.Count == 0;

            BindCurves(detail);
        }

        // ¿Está listo en su área? (spec 024, D-269): sus guías, también Sin iniciar, cada una con
        // la línea de qué hacer de Operadores; sin guías que entrenar, el texto (D-267, P-5).
        private void BindReady(OperatorDetail detail, Dictionary<int, DashboardGuide> guides, Dictionary<int, GuideHistory> history)
        {
            RptGuides.DataSource = detail.AreaGuides.Select(r => Card(guides[r.TrainingGuideId], r, history[r.TrainingGuideId])).ToList();
            RptGuides.DataBind();

            PhAreaNote.Visible = detail.AreaGuides.Count == 0;
            LitAreaNote.Text   = HttpUtility.HtmlEncode(ReportText.AreaNote(detail.AreaStatus) ?? string.Empty);
            var hint = ReportText.StateHint(null, true);
            PhAreaHint.Visible    = detail.AreaStatus == OperatorAreaStatus.NoArea;
            LitAreaHintTitle.Text = HttpUtility.HtmlEncode(hint.Title);
            LitAreaHintText.Text  = HttpUtility.HtmlEncode(hint.Text);
        }

        // ¿Qué le falla?: primero su área (D-269); los grupos de otras áreas arrancan ocultos si
        // hay de su área, y se muestran en el cliente.
        private void BindReinforcement(OperatorDetail detail, Dictionary<int, DashboardGuide> guides, HashSet<int> areaIds)
        {
            bool hasMine = detail.Reinforcement.Any(r => areaIds.Contains(r.TrainingGuideId));
            var groups = detail.Reinforcement.Select(r =>
            {
                bool mine = areaIds.Contains(r.TrainingGuideId);
                return new OperatorReinforcementGroup
                {
                    GuideName = guides[r.TrainingGuideId].AreaName,
                    Tag       = mine ? "Su área" : "Otra área",
                    TagCss    = mine ? "operator-tag is-mine" : "operator-tag",
                    Collapsed = !mine && hasMine,
                    Steps     = r.Steps.Select(s => new OperatorReinforcementRow
                    {
                        StepName            = s.StepName,
                        TimesWithoutSuccess = s.TimesWithoutSuccess,
                        FailedAttempts      = s.FailedAttempts,
                        MainReasonText      = ReportText.Reason(s.MainReason)
                    }).ToList()
                };
            }).ToList();

            RptReinforcement.DataSource = groups;
            RptReinforcement.DataBind();
            PnlNoReinforcement.Visible = groups.Count == 0;

            int hidden = groups.Where(g => g.Collapsed).Sum(g => g.Steps.Count);
            PhOtherSteps.Visible = hidden > 0;
            OtherStepsShowText   = "Ver también otras áreas (" + (hidden == 1 ? "1 paso" : hidden + " pasos") + ")";
        }

        // ¿Qué ha hecho?: sus sesiones por modo y "También entrenó" (D-270, P-9).
        private void BindActivity(OperatorDetail detail, Dictionary<int, DashboardGuide> guides)
        {
            var activity = detail.Activity;
            LitSessions.Text = HttpUtility.HtmlEncode(ReportText.Sessions(activity.Sessions));
            PhLastSession.Visible = activity.LastSession.HasValue;
            string ago = ReportText.DaysAgo(activity.DaysSinceLastSession);
            LitLastSession.Text = HttpUtility.HtmlEncode(ReportText.Date(activity.LastSession) + (ago.Length > 0 ? " · " + ago : string.Empty));

            GuidedCount     = activity.Guided;
            PracticeCount   = activity.FreePractice;
            AssessmentCount = activity.Assessment;
            PhMix.Visible   = activity.Sessions > 0;
            MixLabel = "Sesiones por modo: Guided " + GuidedCount + ", Práctica libre " + PracticeCount + ", Assessment " + AssessmentCount;

            PhAlsoTrained.Visible = detail.OtherGuides.Count > 0;
            LitAlsoTrained.Text = HttpUtility.HtmlEncode("También entrenó: " + string.Join("; ", detail.OtherGuides.Select(r =>
                AreaLabel.Of(guides[r.TrainingGuideId].ProductName, guides[r.TrainingGuideId].AreaName)
                + " — " + ReportText.State(r.State))) + ".");
        }

        // La tarjeta de una guía de su área.
        private static OperatorGuideCard Card(DashboardGuide guide, GuideReadiness readiness, GuideHistory history)
        {
            var hint = ReportText.StateHint(readiness.State, false);
            return new OperatorGuideCard
            {
                GuideName        = guide.AreaName,
                GuideTitle       = guide.Title,
                StateText        = ReportText.State(readiness.State),
                StateCss         = ReportText.StateCss(readiness.State),
                StateIcon        = ReportText.StateIcon(readiness.State),
                BestText         = ReportText.Score(readiness.BestScore),
                LastText         = ReportText.Score(readiness.LastScore),
                LastActivityText = ReportText.Date(readiness.LastActivity),
                AgoText          = ReportText.DaysAgo(history.DaysSinceActivity),
                GapText          = ReportText.ScoreGap(history.LastMinusBest),
                HintTitle        = hint.Title,
                HintText         = hint.Text,
                HintIcon         = hint.Icon,
                HintTone         = hint.Tone
            };
        }

        // Un bloque del historial: la guía con su curva (canvas curve-<index>), su lectura y sus
        // sesiones. Abierto: su área, o todos si no tiene área (P-8).
        private static OperatorHistoryBlock HistoryBlock(OperatorDetail detail, GuideCurve curve, int index,
            Dictionary<int, DashboardGuide> guides, GuideReadiness readiness, GuideHistory history, bool mine,
            IList<OperatorSessionListRow> rows)
        {
            var guide  = guides[curve.TrainingGuideId];
            var scores = curve.Points.Where(p => p.Mode == SessionMode.Assessment && p.Score.HasValue).ToList();
            return new OperatorHistoryBlock
            {
                CurveIndex        = index,
                AreaText          = AreaLabel.Of(guide.ProductName, guide.AreaName),
                GuideTitle        = guide.Title,
                Tag               = mine ? "Su área" : "Otra área",
                TagCss            = mine ? "operator-tag is-mine" : "operator-tag",
                Open              = mine || detail.AreaStatus == OperatorAreaStatus.NoArea,
                StateText         = ReportText.State(readiness.State),
                StateCss          = ReportText.StateCss(readiness.State),
                StateIcon         = ReportText.StateIcon(readiness.State),
                BestText          = ReportText.Score(readiness.BestScore),
                LastText          = ReportText.Score(readiness.LastScore),
                Sessions          = history.Sessions,
                LastActivityText  = ReportText.Date(readiness.LastActivity),
                AgoText           = ReportText.DaysAgo(history.DaysSinceActivity),
                HasPoints         = curve.Points.Count > 0,
                PracticeText      = ReportText.PracticeBeforeFirstPass(curve.PracticeBeforeFirstPass),
                ScoresText        = scores.Count > 0
                    ? "Puntajes de Assessment: " + string.Join(" → ", scores.Select(p => ReportText.Score(p.Score)))
                    : string.Empty,
                Assessments       = history.Assessments,
                PassedAssessments = history.PassedAssessments,
                FailedAttempts    = history.FailedAttempts,
                Rows              = rows
            };
        }

        // Una sesión: el medidor y "Aprobó / Sin aprobar" solo en los Assessment con puntaje.
        private static OperatorSessionListRow SessionRow(OperatorSessionRow row, Dictionary<int, DashboardGuide> guides,
            HashSet<int> best, int maxFail)
        {
            var session = row.Session;
            bool meter  = session.Mode == SessionMode.Assessment && row.Score.HasValue;
            return new OperatorSessionListRow
            {
                StartedText    = ReportText.Date(session.StartedAt),
                DayText        = ReportText.Day(session.StartedAt),
                TimeText       = ReportText.Time(session.StartedAt),
                AreaText       = row.AreaText,
                GuideTitle     = guides[session.TrainingGuideId].Title,
                ModeText       = ReportText.Mode(session.Mode),
                ModeCss        = ReportText.ModeCss(session.Mode),
                StatusText     = ReportText.Status(session.Status),
                StatusIcon     = ReportText.StatusIcon(session.Status),
                HasScore       = meter,
                ScoreText      = ReportText.Score(row.Score),
                ScoreWidth     = meter ? ReportText.BarWidth(row.Score.Value * 100m) : "0",
                Passed         = row.Passed,
                IsBest         = meter && best.Contains(session.SessionId),
                DurationText   = ReportText.Duration(row.Duration),
                FailedAttempts = row.FailedAttempts,
                FailWidth      = (int)Math.Round(row.FailedAttempts * (double)FailBarWidth / maxFail)
            };
        }

        // Curvas (AC-15) en el orden de RptHistory: por sesión de Práctica libre o Assessment, su
        // etiqueta (#n, día y modo), los intentos fallidos de cada modo y el puntaje de los
        // Assessment con si aprobó; el mínimo para aprobar, de ScoreCalculator.
        private void BindCurves(OperatorDetail detail)
        {
            ChartJson = new JavaScriptSerializer().Serialize(detail.Curves.Select(c => c.Points.Count == 0 ? null : new
            {
                labels     = c.Points.Select((p, i) => new[]
                {
                    "#" + (i + 1),
                    SessionHelper.ToLocal(p.StartedAt).ToString("dd/MM"),
                    p.Mode == SessionMode.Assessment ? "Assessment" : "Práctica"
                }),
                practice   = c.Points.Select(p => p.Mode == SessionMode.FreePractice ? (int?)p.FailedAttempts : null),
                assessment = c.Points.Select(p => p.Mode == SessionMode.Assessment ? (int?)p.FailedAttempts : null),
                scores     = c.Points.Select(p => p.Mode == SessionMode.Assessment && p.Score.HasValue ? (decimal?)Math.Round(p.Score.Value * 100m, 1) : null),
                passed     = c.Points.Select(p => p.Mode == SessionMode.Assessment && p.Score.HasValue && ScoreCalculator.IsPassed(p.Score.Value)),
                passing    = ScoreCalculator.PassingScore * 100m
            }));
        }
    }
}
