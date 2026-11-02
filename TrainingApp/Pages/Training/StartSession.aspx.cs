using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Training
{
    public partial class StartSession : Page
    {
        private readonly TrainingGuideService _guideService = new TrainingGuideService();
        private readonly SessionResultService _results = new SessionResultService();

        // La nota del bloque "Últimas sesiones" (spec 025, D-344).
        protected string RecentNote { get; private set; } = string.Empty;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Re-hidratar sesión si se perdió pero FormsAuth sigue válido
            if (!SessionHelper.EnsureUserSession())
            {
                Response.Redirect(SessionHelper.LoginUrl);
                return;
            }

            // Spec 013: solo entrena quien tiene la capacidad; un admin va a su módulo (AC-3).
            if (!SessionHelper.CanTrain())
            {
                SessionHelper.RedirectHome();
                return;
            }

            // Primer acceso sin contraseña personal: no puede entrenar (auditoría AUD-08).
            if (SessionHelper.MustChangePassword())
            {
                Response.Redirect(SessionHelper.ChangePasswordUrl);
                return;
            }

            // Spec 021 (D-169): sin área, primero la elige.
            if (SessionHelper.MustChooseArea())
            {
                Response.Redirect(SessionHelper.ChooseAreaUrl);
                return;
            }

            if (!IsPostBack)
            {
                LblLastAccess.Text = HttpUtility.HtmlEncode(SessionHelper.LastAccessText() ?? string.Empty);
                LoadStartOptions();
                LoadRecentSessions();
            }
        }

        protected void BtnStart_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(DdlGuide.SelectedValue, out int guideId) || guideId == 0)
            {
                ShowError("Selecciona una guía válida.");
                return;
            }

            var mode = RbAssessment.Checked   ? SessionMode.Assessment
                     : RbFreePractice.Checked ? SessionMode.FreePractice
                     : SessionMode.Guided;
            int userId = (int)Session[Constants.SessionKeys.UserId];

            // D-9 y AUD-41: deja una sola sesión en curso, también en el estado del POD.
            var session = new PodService(new SessionPodStateStore(Session)).StartSession(userId, guideId, mode);
            Response.Redirect("~/Pages/Training/GuidedTest.aspx?sessionId=" + session.Id);
        }

        // Spec 021 (D-167, D-168): el área de la BD en cada carga (P-3) y solo sus guías publicadas.
        private void LoadStartOptions()
        {
            var options = _guideService.GetStartOptions((int)Session[Constants.SessionKeys.UserId]);
            LblNoArea.Visible = !options.HasArea;
            PhStart.Visible   = options.HasArea;
            if (!options.HasArea) return;

            LitGuideCount.Text = options.GuideCountText;
            // El área, en en la barra superior ("Tu área", spec 022 brief 0).
            (Master as SiteMaster)?.ShowArea(options.AreaText);
            DdlGuide.Items.Clear();
            LblNoGuides.Visible = options.Guides.Count == 0;
            foreach (var g in options.Guides)
                DdlGuide.Items.Add(new ListItem(g.Title, g.Id.ToString()));

            // El Inicio con todo elegido (spec 025, P-9, D-337): ?mode=&guide= de las acciones del fin
            // de sesión, validados por separado por la BLL (uno inválido se ignora sin aviso). Solo en
            // la primera carga; Iniciar sigue siendo el único camino que crea la sesión.
            var chosen = options.Preselect(Request.QueryString["mode"], Request.QueryString["guide"]);
            if (chosen.Mode.HasValue)
            {
                RbGuided.Checked       = chosen.Mode == SessionMode.Guided;
                RbFreePractice.Checked = chosen.Mode == SessionMode.FreePractice;
                RbAssessment.Checked   = chosen.Mode == SessionMode.Assessment;
            }
            if (chosen.GuideId.HasValue)
                DdlGuide.SelectedValue = chosen.GuideId.Value.ToString();
        }

        // Spec 025 (D-335, D-344): las últimas 5 sesiones completadas del operador; sin ninguna,
        // el bloque no aparece.
        private void LoadRecentSessions()
        {
            var recent = _results.GetRecent((int)Session[Constants.SessionKeys.UserId]);
            PhRecent.Visible = recent.Count > 0;
            if (recent.Count == 0) return;

            RecentNote = SessionResultText.RecentNote(recent);
            RptRecent.DataSource = recent.Select(r => SessionResultText.Recent(r,
                ResolveUrl("~/Pages/Training/SessionResult.aspx?sessionId=" + r.SessionId))).ToList();
            RptRecent.DataBind();
        }

        private void ShowError(string message)
        {
            LblError.Text    = message;
            LblError.Visible = true;
        }
    }
}
