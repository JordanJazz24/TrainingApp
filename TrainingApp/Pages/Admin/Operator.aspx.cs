using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using TrainingApp.BLL.Auth;
using TrainingApp.BLL.Reports;
using TrainingApp.BLL.Scoring;
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

            var guideNames = detail.Guides.ToDictionary(g => g.Id, g => g.AreaName);

            RptGuides.DataSource = detail.Guides.Zip(detail.Readiness, (g, r) => new OperatorGuideCard
            {
                GuideName        = g.AreaName,
                GuideTitle       = g.Title,
                StateText        = ReportText.State(r.State),
                StateCss         = ReportText.StateCss(r.State),
                BestText         = ReportText.Score(r.BestScore),
                LastText         = ReportText.Score(r.LastScore),
                LastActivityText = ReportText.Date(r.LastActivity)
            }).ToList();
            RptGuides.DataBind();

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
            LblNoReinforcement.Visible = detail.Reinforcement.Count == 0;

            RptSessions.DataSource = detail.Sessions.Select(row => new OperatorSessionListRow
            {
                StartedText    = ReportText.Date(row.Session.StartedAt),
                GuideName      = guideNames[row.Session.TrainingGuideId],
                ModeText       = ReportText.Mode(row.Session.Mode),
                StatusText     = ReportText.Status(row.Session.Status),
                ScoreText      = ReportText.Score(row.Score),
                DurationText   = ReportText.Duration(row.Duration),
                FailedAttempts = row.FailedAttempts
            }).ToList();
            RptSessions.DataBind();
            LblNoSessions.Visible = detail.Sessions.Count == 0;
        }
    }
}
