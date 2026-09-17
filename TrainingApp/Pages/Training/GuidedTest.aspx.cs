using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using TrainingApp.BLL.Training;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Models;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Training
{
    public partial class GuidedTest : Page
    {
        private readonly TrainingGuideService               _guideService   = new TrainingGuideService();
        private readonly TrainingSessionService             _sessionService = new TrainingSessionService();
        private readonly AreaRepository                     _areaRepo       = new AreaRepository();
        private readonly ProductRepository                  _productRepo    = new ProductRepository();
        private readonly NCCodeRepository                   _ncRepo         = new NCCodeRepository();
        private readonly DataCollectionParameterRepository  _dcParamRepo    = new DataCollectionParameterRepository();


        private const string ActiveCtrlPathKey = "GDT_ActiveCtrlPath";
        private const string ActiveCtrlModeKey = "GDT_ActiveCtrlMode";
        private const string PanelInstanceKey = "GDT_PanelInstance"; // Tracks which "generation" of the panel this is

        // ── Dynamic control restoration (must run before ViewState is applied) ─
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            // Si BtnStart fue clickeado, NO restaurar el control previo — el handler
            // creará uno fresco. Limpiar ViewState Y controles AQUÍ (antes de que ViewState se aplique).
            if (Request.Form[BtnStart.UniqueID] != null)
            {
                ViewState.Remove(ActiveCtrlPathKey);
                ViewState.Remove(ActiveCtrlModeKey);
                // CRÍTICO: limpiar AMBOS paneles para que ASP.NET no intente aplicar
                // ViewState de controles previos. Sin esto, el ViewState del request
                // anterior causa conflicto al agregar el nuevo SearchControl.
                PnlModalHost.Controls.Clear();
                PnlActiveControl.Controls.Clear();
                return;
            }

            string path = ViewState[ActiveCtrlPathKey] as string;
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var ctrl = LoadControl(path) as IStepControl;
                if (ctrl == null) return;

                // Apply mode for LogNCControl
                string mode = ViewState[ActiveCtrlModeKey] as string;
                if (ctrl is UI.Controls.Training.LogNCControl lnc && !string.IsNullOrEmpty(mode))
                    lnc.Mode = mode;

                ((UserControl)ctrl).ID = "StepCtrl";
                ctrl.OnStepSubmit += HandleControlSubmit;

                // Modal-overlay controls go to PnlModalHost (no positioning = no POD coverage).
                // View-replacement controls (Assembly, DC, LogNC) go to PnlActiveControl.
                if (IsModalOverlayPath(path))
                    PnlModalHost.Controls.Add((UserControl)ctrl);
                else
                    PnlActiveControl.Controls.Add((UserControl)ctrl);
            }
            catch { /* control path stale — ignore */ }
        }

        private static bool IsModalOverlayPath(string path) =>
            path.IndexOf("ResourceSetupControl", StringComparison.OrdinalIgnoreCase) >= 0 ||
            path.IndexOf("LineClearanceControl", StringComparison.OrdinalIgnoreCase) >= 0 ||
            path.IndexOf("SearchControl",        StringComparison.OrdinalIgnoreCase) >= 0;

        // ── Page_Load ────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            var session = LoadAndValidateSession();
            if (session == null) return;

            var guide = _guideService.GetById(session.TrainingGuideId);
            var area  = _areaRepo.GetById(guide.AreaId);
            var step  = guide.Steps.First(s => s.Order == session.CurrentStepOrder);

            if (!IsPostBack)
            {
                PopulateHeader(session, area, guide);
                BindWorkList(area, session);
                BindBrowseList(area, session);
            }

            // Set ViewModel on the active control (PostBack re-uses restored control).
            // Check both panels: view-replacement controls are in PnlActiveControl,
            // modal-overlay controls (ResourceSetup, LC) are in PnlModalHost.
            var activeCtrl = (PnlActiveControl.FindControl("StepCtrl")
                           ?? PnlModalHost.FindControl("StepCtrl")) as IStepControl;
            if (activeCtrl != null)
                activeCtrl.ViewModel = BuildStepViewModel(step, area, session, guide);

            UpdateScoreWidget(session);
            SetActiveButton(step.StepType, area);
        }

        // ── Step submit handler (wired in CreateChildControls) ────────────
        private void HandleControlSubmit(object sender, StepSubmitEventArgs e)
        {
            var session = LoadAndValidateSession();
            if (session == null) return;

            var guide = _guideService.GetById(session.TrainingGuideId);
            var area  = _areaRepo.GetById(guide.AreaId);
            var step  = guide.Steps.First(s => s.Order == session.CurrentStepOrder);

            // Modal-overlay controls (ResourceSetup, LineClearance, Search) must NOT be
            // restored on the next postback, but their HTML stays in this response so
            // the operator sees the confirmation banner before closing the modal with X.
            bool isModalOverlay = step.StepType == StepType.ResourceSetup ||
                                  step.StepType == StepType.LineClearance ||
                                  step.StepType == StepType.Search;

            if (isModalOverlay)
                ClearActiveControlViewState();
            else
                ClearActiveControl();

            ProcessSubmit(session, guide, area, step, e.Answer);

            if (step.StepType == StepType.ResourceSetup)
                ReopenModal("modalResourceSetup");
            else if (step.StepType == StepType.LineClearance)
                ReopenModal("modalDCStandalone");
            else if (step.StepType == StepType.Search)
                ShowBanner("✓ Start process completed successfully");
        }

        private void ProcessSubmit(TrainingSession session, TrainingGuide guide, Area area,
            TrainingStep step, string answer)
        {
            var result = _sessionService.SubmitStep(session.Id, step.Id, answer);

            UpdateScoreWidget(session);

            switch (result.Outcome)
            {
                case StepSubmitOutcome.Correct:
                    HandleCorrectSubmit(session, guide, area, step.StepType);
                    break;

                case StepSubmitOutcome.Incorrect:
                    ShowSweetAlert("warning", "Incorrect",
                        string.IsNullOrEmpty(GetHint(step.StepType)) ? "Try again." : GetHint(step.StepType));
                    break;

                case StepSubmitOutcome.FailedForced:
                    ShowSweetAlert("error", "Step Failed",
                        "Paso fallido — el sistema avanza automáticamente.");
                    HandleCorrectSubmit(_sessionService.GetById(session.Id), guide, area, step.StepType);
                    break;

                case StepSubmitOutcome.SessionCompleted:
                    ShowCompletionPanel(session, area, guide);
                    break;
            }
        }

        private void HandleCorrectSubmit(TrainingSession session, TrainingGuide guide,
            Area area, StepType submittedStepType)
        {
            var refreshedSession = _sessionService.GetById(session.Id);
            if (refreshedSession.Status == SessionStatus.Completed)
            {
                ShowCompletionPanel(refreshedSession, area, guide);
                return;
            }

            var nextStep = guide.Steps.FirstOrDefault(s => s.Order == refreshedSession.CurrentStepOrder);
            if (nextStep == null) return;

            SetActiveButton(nextStep.StepType, area);
        }

        // ── Button ServerClick handlers ───────────────────────────────────

        protected void BtnResourceSetup_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            // ResourceSetup is a modal overlay — POD work list must stay visible
            LoadAndShowControl("~/UI/Controls/Training/ResourceSetupControl.ascx",
                area, session, guide, "modalResourceSetup", hideWorkList: false);
        }

        protected void BtnActivities_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            // LineClearance (Data Collection Standalone) is a modal overlay — POD stays visible
            LoadAndShowControl("~/UI/Controls/Training/LineClearanceControl.ascx",
                area, session, guide, "modalDCStandalone", hideWorkList: false);
        }

        protected void BtnStart_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            string sfcValue = TxtSFC.Text.Trim();
            if (string.IsNullOrEmpty(sfcValue))
                sfcValue = HfSelectedSFC.Value.Trim();

            if (string.IsNullOrEmpty(sfcValue))
            {
                ShowSweetAlert("warning", "Campo requerido", "Debes ingresar o escanear un SFC antes de iniciar.");
                return;
            }

            // Search/Start launches the 4-modal sequence (SearchControl) as a modal
            // overlay — the POD work list must stay visible behind it.
            // CreateChildControls() detecta el click de [Start] y NO restaura el control
            // previo, limpiando ViewState antes de que ASP.NET lo aplique — solución
            // canónica para evitar conflictos de ViewState en controles dinámicos.
            LoadAndShowControl("~/UI/Controls/Training/SearchControl.ascx",
                area, session, guide, modalId: null, hideWorkList: false);
        }

        protected void BtnAssemble_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            LoadAndShowControl("~/UI/Controls/Training/AssemblyControl.ascx",
                area, session, guide, "modalAssemble");
        }

        protected void BtnDCCollection_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            PnlWorkList.Visible       = false;
            PnlActiveControl.Visible  = true;
            LoadAndShowControl("~/UI/Controls/Training/DataCollectionControl.ascx",
                area, session, guide, null);
        }

        protected void BtnSFCSplit_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            ViewState[ActiveCtrlModeKey] = "split";
            LoadAndShowControl("~/UI/Controls/Training/LogNCControl.ascx",
                area, session, guide, null);
        }

        protected void BtnLogNC_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            ViewState[ActiveCtrlModeKey] = "nc";
            LoadAndShowControl("~/UI/Controls/Training/LogNCControl.ascx",
                area, session, guide, null);
        }

        protected void BtnComplete_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            ProcessSubmit(session, guide, area, step, "complete");
        }

        protected void BtnAbandon_Click(object sender, EventArgs e)
        {
            var session = LoadAndValidateSession();
            if (session == null) return;
            _sessionService.Abandon(session.Id);
            Response.Redirect("~/Pages/Training/StartSession.aspx");
        }

        protected void BtnLogout_ServerClick(object sender, EventArgs e)
        {
            Session.Abandon();
            System.Web.Security.FormsAuthentication.SignOut();
            Response.Redirect("~/Pages/Auth/Login.aspx");
        }

        // ── Control loading ──────────────────────────────────────────────
        private void LoadAndShowControl(string path, Area area, TrainingSession session,
            TrainingGuide guide, string modalId, bool hideWorkList = true)
        {
            var step = guide.Steps.First(s => s.Order == session.CurrentStepOrder);
            ViewState[ActiveCtrlPathKey] = path;

            // Modal-overlay controls (ResourceSetup, LC) use PnlModalHost — a plain div
            // with no positioning — so the POD work list remains visible behind the Bootstrap modal.
            // View-replacement controls (Assembly, DC, LogNC) use PnlActiveControl which has
            // position:fixed and background:white to cover the work list area.
            System.Web.UI.WebControls.Panel targetPanel;
            if (hideWorkList)
            {
                PnlWorkList.Visible      = false;
                PnlActiveControl.Visible = true;
                targetPanel = PnlActiveControl;
            }
            else
            {
                PnlModalHost.Visible = true;
                targetPanel = PnlModalHost;
            }

            // Re-create the control for THIS request if not already in tree
            var existing = targetPanel.FindControl("StepCtrl");
            if (existing == null)
            {
                var ctrl = LoadControl(path) as IStepControl;
                if (ctrl == null) return;

                string mode = ViewState[ActiveCtrlModeKey] as string;
                if (ctrl is UI.Controls.Training.LogNCControl lnc && !string.IsNullOrEmpty(mode))
                    lnc.Mode = mode;

                ((UserControl)ctrl).ID = "StepCtrl";
                ctrl.OnStepSubmit += HandleControlSubmit;
                ctrl.ViewModel     = BuildStepViewModel(step, area, session, guide);

                targetPanel.Controls.Add((UserControl)ctrl);

                // Call Initialize() explicitly — Page_Load may have already passed
                // when this control is added during an event handler
                ctrl.Initialize();
            }

            if (!string.IsNullOrEmpty(modalId))
                ReopenModal(modalId);
        }

        private void ClearActiveControl()
        {
            ViewState.Remove(ActiveCtrlPathKey);
            ViewState.Remove(ActiveCtrlModeKey);
            PnlActiveControl.Controls.Clear();
            PnlActiveControl.Visible = false;
            PnlModalHost.Controls.Clear();
            PnlModalHost.Visible = false;
            PnlWorkList.Visible  = true;
        }

        // For modal-overlay controls: clears ViewState (control not restored next PostBack)
        // but keeps the modal HTML in the response so ReopenModal can show the confirmation banner.
        private void ClearActiveControlViewState()
        {
            ViewState.Remove(ActiveCtrlPathKey);
            ViewState.Remove(ActiveCtrlModeKey);
        }

        // ── Completion panel ─────────────────────────────────────────────
        private void ShowCompletionPanel(TrainingSession session, Area area, TrainingGuide guide)
        {
            ClearActiveControl();
            PnlWorkList.Visible  = false;
            PnlCompleted.Visible = true;
            PnlScore.Visible     = false;
            BtnAbandon.Disabled  = true;

            decimal finalScore = session.FinalScore * 100m;
            LitFinalScore.Text   = finalScore.ToString("F1") + "%";
            LitFinalMode.Text    = session.Mode.ToString();

            var products = _productRepo.GetAll();
            var product  = products.FirstOrDefault(p => p.Id == area.ProductId);
            LitFinalArea.Text    = Page.Server.HtmlEncode(area.Name);
            LitFinalProduct.Text = Page.Server.HtmlEncode(product?.Name ?? "—");

            if (session.Mode == SessionMode.Assessment)
            {
                PnlFinalResult.Visible = true;
                LitFinalResult.Text    = finalScore >= 70m
                    ? "<span class=\"text-success\">✓ APROBADO</span>"
                    : "<span class=\"text-danger\">✗ REPROBADO</span>";
            }
            else
            {
                PnlFinalResult.Visible = false;
            }
        }

        // ── StepViewModel builder ────────────────────────────────────────
        private StepViewModel BuildStepViewModel(TrainingStep step, Area area,
            TrainingSession session, TrainingGuide guide)
        {
            var vm = new StepViewModel
            {
                StepId       = step.Id,
                StepType     = step.StepType,
                StepOrder    = step.Order,
                Hint         = GetHint(step.StepType),
                AttemptCount = 0
            };

            string sfcCode    = BuildSimulatedSFCCode(session);
            string areaName   = area.Name;
            string resourceName = areaName + "-01";
            string opName     = guide.Title;

            vm.ExtraData["SFCCode"]      = sfcCode;
            vm.ExtraData["AreaName"]     = areaName;
            vm.ExtraData["ResourceName"] = resourceName;
            vm.ExtraData["OperationName"] = opName;
            vm.ExtraData["Site"]         = "CR9A";

            switch (step.StepType)
            {
                case StepType.LineClearance:
                    vm.ExtraData["QuestionText"] = "¿La limpieza de linea se ejecuto?";
                    break;

                case StepType.DataCollection:
                    var dcParams = _dcParamRepo.GetByArea(area.Id);
                    vm.ExtraData["Parameters"] = dcParams;
                    break;

                case StepType.LogNC:
                    var ncCodes = _ncRepo.GetByArea(area.Id);
                    vm.ExtraData["NCCodes"]         = ncCodes;
                    vm.ExtraData["IsNonSerialized"] = area.SerializationType == SerializationType.NonSerialized;
                    break;

                case StepType.Assembly:
                    // Moldeo has RequiresAssembly=false; this block runs for areas with assembly
                    vm.ExtraData["ExpectedCount"] = 3;
                    break;

                case StepType.Search:
                    // SearchControl owns its own 4-modal flow (labels hardcoded in the .ascx);
                    // it only needs ResourceName, already populated above.
                    break;
            }

            return vm;
        }

        private static IList<object> BuildMoldeoLCQuestions()
        {
            return new List<object>
            {
                new { QuestionText = "¿Se verificaron las etiquetas de calibración?" },
                new { QuestionText = "¿Se verificó el IDMold & IDIMM según MSS?" },
                new { QuestionText = "¿La línea está libre de materiales anteriores?" }
            };
        }

        // ── Session validation ───────────────────────────────────────────
        private TrainingSession LoadAndValidateSession()
        {
            // Re-hidrata sesión si se perdió pero el usuario sigue autenticado
            if (!SessionHelper.EnsureOperatorSession())
            {
                Response.Redirect("~/Pages/Auth/Login.aspx");
                return null;
            }

            if (!int.TryParse(Request.QueryString["sessionId"], out int sessionId))
            {
                Response.Redirect("~/Pages/Training/StartSession.aspx");
                return null;
            }

            var session    = _sessionService.GetById(sessionId);
            int operatorId = (int)Session[Constants.SessionKeys.OperatorId]; // Ahora seguro

            if (session == null ||
                session.OperatorId != operatorId ||
                session.Status != SessionStatus.InProgress)
            {
                Response.Redirect("~/Pages/Training/StartSession.aspx");
                return null;
            }

            return session;
        }

        // ── UI helpers ───────────────────────────────────────────────────
        private void PopulateHeader(TrainingSession session, Area area, TrainingGuide guide)
        {
            LitUserName.Text      = HttpUtility.HtmlEncode(
                Session[Constants.SessionKeys.Name]?.ToString() ?? "Operator");
            LitOperationName.Text = HttpUtility.HtmlEncode(guide.Title);
            LitResourceName.Text  = HttpUtility.HtmlEncode(area.Name + "-01");
            LitBrowseContext.Text = string.Format("{0}, {1}-01, {2}",
                area.Name, area.Name,
                Session[Constants.SessionKeys.Code1]?.ToString() ?? "");
        }

        private void BindWorkList(Area area, TrainingSession session)
        {
            var rows = BuildSimulatedWorkList(area, session);
            RptWorkList.DataSource = rows;
            RptWorkList.DataBind();
        }

        private void BindBrowseList(Area area, TrainingSession session)
        {
            var rows = BuildSimulatedWorkList(area, session);
            RptBrowseList.DataSource = rows;
            RptBrowseList.DataBind();
        }

        private IList<PodWorkListRow> BuildSimulatedWorkList(Area area, TrainingSession session)
        {
            var products = _productRepo.GetAll();
            var product  = products.FirstOrDefault(p => p.Id == area.ProductId);
            string shopOrder = "SO-" + session.Id.ToString("D6");
            string sfcCode   = BuildSimulatedSFCCode(session);
            string matDesc   = (product?.Name ?? "") + " / " + area.Name;
            int qty          = area.SerializationType == SerializationType.NonSerialized ? 250 : 1;

            return new List<PodWorkListRow>
            {
                new PodWorkListRow
                {
                    SFCCode             = sfcCode,
                    MaterialDescription = matDesc,
                    Qty                 = qty,
                    Status              = "Active",
                    ProcessLot          = "LOT-" + DateTime.Today.ToString("yyyyMMdd"),
                    ShopOrder           = shopOrder,
                    IsSelected          = true,
                    ScheduleStart       = DateTime.Today.ToString("MM/dd/yyyy"),
                    PlannedCompletion   = DateTime.Today.AddDays(1).ToString("MM/dd/yyyy")
                }
            };
        }

        private string BuildSimulatedSFCCode(TrainingSession session)
        {
            return "CR9A" + (session.Id * 1000 + 91).ToString("D8");
        }

        private void UpdateScoreWidget(TrainingSession session)
        {
            LitScore.Text = FormatScore(_sessionService.GetCurrentScore(session.Id));

            string modeLabel  = session.Mode == SessionMode.Assessment ? "Assessment" : "Guided";
            string badgeClass = session.Mode == SessionMode.Assessment
                ? "badge badge-assessment" : "badge badge-guided";
            LitModeBadge.Text = string.Format(
                "<span class=\"{0}\">{1}</span>", badgeClass, modeLabel);
        }

        private void SetActiveButton(StepType stepType, Area area)
        {
            BtnResourceSetup.Disabled = true;
            BtnStart.Disabled         = true;
            BtnAssemble.Disabled      = true;
            BtnDCCollection.Disabled  = true;
            BtnComplete.Disabled      = true;
            BtnActivities.Disabled    = true;
            BtnSFCSplit.Disabled      = true;
            BtnLogNC.Disabled         = true;

            switch (stepType)
            {
                case StepType.ResourceSetup:
                    BtnResourceSetup.Disabled = false;
                    break;
                case StepType.LineClearance:
                    BtnActivities.Disabled = false;
                    // Enable the dropdown toggle so the Activities menu can open
                    Page.ClientScript.RegisterStartupScript(GetType(), "enableActivitiesToggle",
                        "var t=document.getElementById('BtnActivitiesToggle');" +
                        "if(t) t.disabled=false;", true);
                    break;
                case StepType.Search:
                    BtnStart.Disabled = false;
                    break;
                case StepType.Assembly:
                    BtnAssemble.Disabled = false;
                    break;
                case StepType.DataCollection:
                    BtnDCCollection.Disabled = false;
                    break;
                case StepType.LogNC:
                    BtnLogNC.Disabled = false;
                    if (area.SerializationType == SerializationType.NonSerialized)
                        BtnSFCSplit.Disabled = false;
                    break;
                case StepType.Complete:
                    BtnComplete.Disabled = false;
                    break;
            }
        }

        private void ShowBanner(string message)
        {
            PnlBannerZone.Visible = true;
            LitBanner.Text        = HttpUtility.HtmlEncode(message);
            // Auto-hide after 3 seconds
            ClientScript.RegisterStartupScript(GetType(), "hideBanner",
                "setTimeout(function(){var b=document.querySelector('.pod-banner-zone');if(b)b.style.display='none';},3000);",
                true);
        }

        // ── Context helper ───────────────────────────────────────────────
        private (TrainingSession, TrainingGuide, Area, TrainingStep) GetContext()
        {
            var session = LoadAndValidateSession();
            if (session == null) return (null, null, null, null);
            var guide = _guideService.GetById(session.TrainingGuideId);
            var area  = _areaRepo.GetById(guide.AreaId);
            var step  = guide.Steps.First(s => s.Order == session.CurrentStepOrder);
            return (session, guide, area, step);
        }

        // ── Modal re-open & SweetAlert helpers ───────────────────────────
        protected void ShowSweetAlert(string type, string title, string text)
        {
            string script = string.Format(
                "Swal.fire({{title:'{0}',text:'{1}',icon:'{2}'}});",
                EscapeJs(title), EscapeJs(text), type);
            ClientScript.RegisterStartupScript(GetType(), "swal_" + type, script, true);
        }

        protected void ReopenModal(string modalId)
        {
            string script = string.Format("showModalSafe('{0}');", modalId);
            ClientScript.RegisterStartupScript(GetType(), "modal_" + modalId, script, true);
        }

        private static string EscapeJs(string s)
            => s?.Replace("'", "\\'").Replace("\n", "\\n") ?? string.Empty;

        private static string FormatScore(decimal score)
            => (score * 100m).ToString("F1") + "%";

        private static string GetHint(StepType stepType)
        {
            switch (stepType)
            {
                case StepType.ResourceSetup:  return "Complete Resource Setup: Retrieve, set state to 'Set Up', add rows, Save.";
                case StepType.LineClearance:  return "Answer 'Sí' to confirm line clearance.";
                case StepType.Search:         return "Enter or scan the SFC code, then press Start.";
                case StepType.DataCollection: return "Fill all required parameters and press Save (not Draft).";
                case StepType.LogNC:          return "Select an NC code and enter the Component, then Add-Done.";
                case StepType.Assembly:       return "Scan all required component barcodes and press Add for each.";
                case StepType.Complete:       return "Press Complete to close the manufacturing order.";
                default:                      return string.Empty;
            }
        }
    }
}
