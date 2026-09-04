using System;
using System.Linq;
using System.Web.UI;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Training
{
    public partial class GuidedTest : Page
    {
        private readonly TrainingGuideService   _guideService   = new TrainingGuideService();
        private readonly TrainingSessionService _sessionService = new TrainingSessionService();

        protected void Page_Load(object sender, EventArgs e)
        {
            var session = LoadAndValidateSession();
            if (session == null) return;

            if (!IsPostBack)
                RenderStep(session);
        }

        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            var session = LoadAndValidateSession();
            if (session == null) return;

            var guide = _guideService.GetById(session.TrainingGuideId);
            var step  = guide.Steps.First(s => s.Order == session.CurrentStepOrder);

            var result = _sessionService.SubmitStep(session.Id, step.Id, TxtAnswer.Text.Trim());

            LitScore.Text       = FormatScore(result.CurrentScore);
            PnlFeedback.Visible = true;

            switch (result.Outcome)
            {
                case StepSubmitOutcome.Incorrect:
                    LblFeedback.Text = "<div class=\"alert alert-warning mb-0\">Respuesta incorrecta. Intenta de nuevo.</div>";
                    PnlHint.Visible  = true;
                    LitHint.Text     = GetHint(step.StepType);
                    break;

                case StepSubmitOutcome.Correct:
                    Response.Redirect(Request.Url.PathAndQuery, false);
                    Context.ApplicationInstance.CompleteRequest();
                    break;

                case StepSubmitOutcome.FailedForced:
                    LblFeedback.Text    = "<div class=\"alert alert-danger mb-0\">Agotaste tus intentos para este paso. El paso fue marcado como fallido.</div>";
                    BtnSubmit.Visible   = false;
                    BtnContinue.Visible = true;
                    PnlHint.Visible     = false;
                    break;

                case StepSubmitOutcome.SessionCompleted:
                    PnlStep.Visible      = false;
                    BtnAbandon.Visible   = false;
                    PnlCompleted.Visible = true;
                    PnlFeedback.Visible  = false;
                    LitFinalScore.Text   = FormatScore(result.SessionScore);
                    break;
            }
        }

        protected void BtnContinue_Click(object sender, EventArgs e)
        {
            Response.Redirect(Request.Url.PathAndQuery, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void BtnAbandon_Click(object sender, EventArgs e)
        {
            var session = LoadAndValidateSession();
            if (session == null) return;

            _sessionService.Abandon(session.Id);
            Response.Redirect("~/Pages/Training/StartSession.aspx");
        }

        private TrainingSession LoadAndValidateSession()
        {
            if (!int.TryParse(Request.QueryString["sessionId"], out int sessionId))
            {
                Response.Redirect("~/Pages/Training/StartSession.aspx");
                return null;
            }

            var session    = _sessionService.GetById(sessionId);
            int operatorId = (int)Session[Constants.SessionKeys.OperatorId];

            if (session == null ||
                session.OperatorId != operatorId ||
                session.Status != SessionStatus.InProgress)
            {
                Response.Redirect("~/Pages/Training/StartSession.aspx");
                return null;
            }

            return session;
        }

        private void RenderStep(TrainingSession session)
        {
            var guide = _guideService.GetById(session.TrainingGuideId);
            var step  = guide.Steps.First(s => s.Order == session.CurrentStepOrder);

            LitGuideTitle.Text = guide.Title;
            LitMode.Text       = session.Mode == SessionMode.Assessment ? "Assessment" : "Guided";
            LitProgress.Text   = string.Format("Paso {0} de {1}", session.CurrentStepOrder, guide.Steps.Count);
            LitScore.Text      = FormatScore(_sessionService.GetCurrentScore(session.Id));
            LitStepType.Text   = step.StepType.ToString();
            TxtAnswer.Text     = string.Empty;

            PnlStep.Visible      = true;
            PnlCompleted.Visible = false;
            PnlFeedback.Visible  = false;
        }

        private static string GetHint(StepType stepType)
        {
            switch (stepType)
            {
                case StepType.ResourceSetup:  return "Escribe \"ok\" para confirmar la configuración del recurso.";
                case StepType.LineClearance:  return "Escribe \"true\" para confirmar que la línea está limpia.";
                case StepType.Search:         return "Escribe \"ok\" para confirmar que navegaste al Start Order.";
                case StepType.DataCollection: return "Envía JSON con los parámetros: {\"<id>\":\"<valor>\", ...}";
                case StepType.LogNC:          return "Escribe el código NC exacto (ej. F_FS_MANCHAS).";
                case StepType.Assembly:       return "Escribe el número de componentes escaneados.";
                case StepType.Complete:       return "Escribe \"ok\" para cerrar el proceso.";
                default:                      return string.Empty;
            }
        }

        private static string FormatScore(decimal score)
        {
            return (score * 100m).ToString("F1") + "%";
        }
    }
}
