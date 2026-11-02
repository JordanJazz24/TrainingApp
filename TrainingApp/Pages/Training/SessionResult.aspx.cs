using System;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Admin;
using TrainingApp.Shared;
using ResultModel = TrainingApp.BLL.Training.SessionResult;

namespace TrainingApp.Pages.Training
{
    // Fin de sesión (spec 025, E3): el resultado de una sesión propia y completada. Solo UI: lo
    // calcula SessionResultService (dueño y estado validados, P-2); la página arma los textos. Un
    // sessionId que no sirve (no numérico, ajeno, en curso, abandonado o inexistente) lleva al
    // Inicio sin decir por qué.
    public partial class SessionResult : Page
    {
        private readonly SessionResultService _service = new SessionResultService();

        protected string GuideText { get; private set; } = string.Empty;
        protected string GuideTitle { get; private set; } = string.Empty;

        // Tarjeta de resultado.
        protected string VerdictClass { get; private set; } = string.Empty;
        protected string VerdictIcon { get; private set; } = string.Empty;
        protected string VerdictText { get; private set; } = string.Empty;
        protected string FigureText { get; private set; } = string.Empty;
        protected string FigureLabel { get; private set; } = string.Empty;
        protected string BarWidth { get; private set; } = "0";
        protected string MarkLeft { get; private set; } = "0";
        protected string MinText { get; private set; } = string.Empty;
        protected string BarAria { get; private set; } = string.Empty;
        protected string ModeIcon { get; private set; } = string.Empty;
        protected string ModeText { get; private set; } = string.Empty;
        protected string DateText { get; private set; } = string.Empty;
        protected string DurationText { get; private set; } = string.Empty;

        // Pasos.
        protected int RefCount { get; private set; }
        protected int AllCount { get; private set; }

        // Qué hacer después.
        protected string NextWhy { get; private set; } = string.Empty;
        protected string PrimaryUrl { get; private set; } = string.Empty;
        protected string PrimaryLabel { get; private set; } = string.Empty;
        protected string PrimaryIcon { get; private set; } = string.Empty;
        protected string RepeatUrl { get; private set; } = string.Empty;
        protected string RepeatLabel { get; private set; } = string.Empty;
        protected string HomeUrl { get; private set; } = string.Empty;

        // Quien perdió la capacidad con el RoleCache vigente vuelve a su inicio, nunca al login
        // (AC-2), como en el Resumen.
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

            // Spec 013, P-15: solo entrena quien tiene la capacidad; un admin va a su módulo.
            if (!SessionHelper.CanTrain())
            {
                SessionHelper.RedirectHome();
                return;
            }

            if (SessionHelper.MustChangePassword())
            {
                Response.Redirect(SessionHelper.ChangePasswordUrl);
                return;
            }

            if (SessionHelper.MustChooseArea())
            {
                Response.Redirect(SessionHelper.ChooseAreaUrl);
                return;
            }

            if (!IsPostBack)
                Bind();
        }

        private void Bind()
        {
            int userId = (int)Session[Constants.SessionKeys.UserId];

            // El sessionId de la URL nunca se usa sin la validación del servicio (P-2).
            ResultModel result = null;
            if (int.TryParse(Request.QueryString["sessionId"], NumberStyles.None, CultureInfo.InvariantCulture, out int sessionId))
                result = _service.Get(userId, sessionId);
            if (result == null)
            {
                Response.Redirect(SessionResultService.StartUrl);
                return;
            }

            var detail = result.Detail;
            (Master as SiteMaster)?.ShowArea(result.UserAreaText);

            GuideText  = result.GuideText ?? string.Empty;
            GuideTitle = result.GuideTitle ?? string.Empty;
            ModeIcon   = SessionResultText.ModeIcon(detail.Mode);
            ModeText   = ReportText.Mode(detail.Mode);
            DateText   = ReportText.Date(detail.CompletedAt);
            DurationText = ReportText.Duration(detail.Duration);

            // Veredicto y barra solo en Assessment; Práctica libre, sin puntaje (D-334).
            if (detail.Passed.HasValue)
            {
                bool passed = detail.Passed.Value;
                VerdictClass = passed ? "ok" : "bad";
                VerdictIcon  = passed ? "bi-check-circle-fill" : "bi-x-circle-fill";
                VerdictText  = passed ? "Aprobado" : "Reprobado";
                MinText      = SessionResultText.Points(detail.PassingScore) + " %";
                MarkLeft     = ReportText.BarWidth(detail.PassingScore * 100m);
                BarWidth     = ReportText.BarWidth((detail.Score ?? 0m) * 100m);
                BarAria      = "Puntaje " + SessionResultText.Points(detail.Score ?? 0m) + " % de un mínimo de " + MinText;
                PhVerdict.Visible = true;
                PhBar.Visible     = true;
            }
            if (detail.Score.HasValue)
            {
                FigureText  = SessionResultText.Points(detail.Score.Value) + " %";
                FigureLabel = "Puntaje de la sesión";
            }
            else
            {
                FigureText  = detail.AtFirstCount + " de " + detail.StepCount;
                FigureLabel = "pasos a la primera · Práctica libre no tiene puntaje";
            }

            RefCount = detail.Reinforce.Count;
            AllCount = detail.Steps.Count;
            PhClean.Visible = RefCount == 0;
            RptSteps.DataSource = detail.Steps.Select(SessionResultText.Step).ToList();
            RptSteps.DataBind();

            var next = SessionResultText.Next(result);
            NextWhy      = next.Why;
            PrimaryLabel = next.Label;
            PrimaryIcon  = next.Icon;
            PrimaryUrl   = ResolveUrl(result.Primary.Url);
            HomeUrl      = ResolveUrl(result.Home.Url);
            if (result.Repeat != null)
            {
                RepeatUrl   = ResolveUrl(result.Repeat.Url);
                RepeatLabel = "Repetir " + ReportText.Mode(detail.Mode);
                PhRepeat.Visible = true;
            }
            // Ir al Inicio secundaria solo si la principal no lo es (si no, saldría dos veces).
            PhHome.Visible = !result.Primary.IsHome;
        }
    }
}
