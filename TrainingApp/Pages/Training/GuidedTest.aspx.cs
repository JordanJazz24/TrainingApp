using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Models;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Training
{
    public partial class GuidedTest : Page
    {
        // Shop Order de ejemplo para la orden por defecto cuando Resource Setup se fuerza sin
        // haberse guardado nunca (S2). Dato de simulación, no de negocio.
        private const string DefaultShopOrder = "303406950";

        private readonly TrainingGuideService               _guideService   = new TrainingGuideService();
        private readonly TrainingSessionService             _sessionService = new TrainingSessionService();
        private readonly AreaRepository                     _areaRepo       = new AreaRepository();
        private readonly ProductRepository                  _productRepo    = new ProductRepository();
        private readonly NCCodeRepository                   _ncRepo         = new NCCodeRepository();
        private readonly DataCollectionParameterRepository  _dcParamRepo    = new DataCollectionParameterRepository();


        // ── Controles de paso estáticos (spec 006) ───────────────────────
        // Los 8 controles están declarados en el .aspx, así que el árbol es idéntico en
        // todos los requests. Qué paso está abierto vive en el ViewState de la página
        // (firmado con MAC) y ApplyStepControlVisibility es el único que toca Visible.
        private const string OpenStepTypeKey = "GDT_OpenStepType";

        private static readonly StepType[] AllStepTypes =
            (StepType[])Enum.GetValues(typeof(StepType));

        private StepType? OpenStepType
        {
            get { return ViewState[OpenStepTypeKey] as StepType?; }
            set { ViewState[OpenStepTypeKey] = value; }
        }

        private IStepControl GetStepControl(StepType stepType)
        {
            switch (stepType)
            {
                case StepType.ResourceSetup:  return CtrlResourceSetup;
                case StepType.LineClearance:  return CtrlLineClearance;
                case StepType.Search:         return CtrlSearch;
                case StepType.Assembly:       return CtrlAssembly;
                case StepType.DataCollection: return CtrlDataCollection;
                case StepType.SFC_Split:      return CtrlSFCSplit;
                case StepType.LogNC:          return CtrlLogNC;
                case StepType.Complete:       return CtrlComplete;
                default: throw new ArgumentOutOfRangeException(nameof(stepType), stepType, null);
            }
        }

        // Modal-overlay: el POD queda visible detrás del modal. El resto reemplaza el POD Work List.
        private static bool IsModalOverlay(StepType stepType) =>
            stepType == StepType.ResourceSetup || stepType == StepType.LineClearance ||
            stepType == StepType.Search        || stepType == StepType.SFC_Split;

        // renderOnce: control ya cerrado que igual se renderiza en esta respuesta para
        // mostrar su confirmación; en el request siguiente deja de estar visible.
        private void ApplyStepControlVisibility(StepType? open, StepType? renderOnce = null)
        {
            StepType? shown = open ?? renderOnce;
            foreach (var type in AllStepTypes)
                ((Control)GetStepControl(type)).Visible = type == shown;

            bool replacesWorkList = shown.HasValue && !IsModalOverlay(shown.Value);
            PnlModalHost.Visible     = shown.HasValue && IsModalOverlay(shown.Value);
            PnlActiveControl.Visible = replacesWorkList;
            PnlWorkList.Visible      = !replacesWorkList;
        }

        private void OpenStepControl(StepType stepType, Area area, TrainingSession session,
            TrainingGuide guide, string modalId)
        {
            var step = guide.Steps.First(s => s.Order == session.CurrentStepOrder);
            var ctrl = GetStepControl(stepType);
            ctrl.ViewModel = BuildStepViewModel(step, area, session, guide);
            ctrl.Initialize();

            OpenStepType = stepType;
            ApplyStepControlVisibility(stepType);

            if (!string.IsNullOrEmpty(modalId))
                ReopenModal(modalId);
        }

        private void CloseStepControl(StepType? renderOnce = null)
        {
            OpenStepType = null;
            ApplyStepControlVisibility(null, renderOnce);
        }

        // ── Page_Load ────────────────────────────────────────────────────
        protected void Page_Load(object sender, EventArgs e)
        {
            // El banner de éxito persiste en ViewState entre postbacks (Visible=true
            // queda grabado). Se resetea acá al inicio de cada request; si este mismo
            // request debe mostrarlo, ShowBanner() lo vuelve a activar más abajo
            // (en HandleControlSubmit, que corre después de Page_Load).
            PnlBannerZone.Visible = false;

            // Atributo HTML y no la propiedad ReadOnly: con ReadOnly=true ASP.NET ignora
            // el valor posteado y el SFC elegido en Browse se perdería en cada postback.
            TxtSFC.Attributes["readonly"] = "readonly";

            var session = LoadAndValidateSession();
            if (session == null) return;

            var guide = _guideService.GetById(session.TrainingGuideId);
            var area  = _areaRepo.GetById(guide.AreaId);
            var step  = guide.Steps.First(s => s.Order == session.CurrentStepOrder);

            if (!IsPostBack)
            {
                PopulateHeader(session, area, guide);
                // Spec 005: el POD arranca vacío — RptWorkList solo se llena tras
                // completar Search/Start (ver HandleControlSubmit). El Browse Work
                // List sí puede mostrar la orden si ya fue creada en Resource Setup.
                BindBrowseList(area, session);

                // Un F5 llega como GET (history.replaceState en el .aspx): si Start ya
                // se completó, el POD y el SFC se reconstruyen desde la orden simulada.
                // La selección de fila y el control abierto no se restauran.
                if (IsSearchCompleted(guide, session))
                {
                    var order = GetSimulatedOrder(session);
                    if (order != null) TxtSFC.Text = order.SFCCode;
                    BindWorkList(area, session);
                }
            }

            // Corre antes del Page_Load de los controles: cada uno ya ve su Visible
            // definitivo y el abierto tiene su ViewModel. Solo se arma el ViewModel del
            // control abierto — BuildStepViewModel consulta la BD.
            var open = OpenStepType;
            ApplyStepControlVisibility(open);
            if (open.HasValue)
                GetStepControl(open.Value).ViewModel = BuildStepViewModel(step, area, session, guide);

            UpdateScoreWidget(session);
            SetActiveButton(step.StepType, area, session.Mode);
        }

        // ── Step submit handler (cableado en el markup) ───────────────────
        protected void HandleControlSubmit(object sender, StepSubmitEventArgs e)
        {
            var session = LoadAndValidateSession();
            if (session == null) return;

            var guide = _guideService.GetById(session.TrainingGuideId);
            var area  = _areaRepo.GetById(guide.AreaId);
            var step  = guide.Steps.First(s => s.Order == session.CurrentStepOrder);

            // Guard de sender (spec 006, E12): ASP.NET dispara eventos también en controles
            // invisibles, y el Answer se evalúa contra el paso actual. Un click en el modal
            // de confirmación de un paso ya completado penalizaría el paso siguiente.
            if (!ReferenceEquals(sender, GetStepControl(step.StepType))) return;

            if (step.StepType == StepType.SFC_Split && !HasSuccessfulSplit(session))
            {
                ShowSweetAlert("warning", "Split requerido",
                    "Debes completar al menos un split antes de cerrar el paso.");
                ReopenModal("modalSFCSplit");
                return;
            }

            var outcome = ProcessSubmit(session, guide, area, step, e.Answer, e.IncorrectFeedback);

            // Spec 006 (E3): respuesta incorrecta → el control queda abierto en el modal
            // donde se respondió y solo se ve el SweetAlert con el hint (de ProcessSubmit).
            if (outcome == StepSubmitOutcome.Incorrect)
            {
                // Search tiene 4 modales: el control sabe cuál contiene la respuesta a corregir.
                if (step.StepType == StepType.Search)
                {
                    CtrlSearch.ReopenForRetry();
                    return;
                }

                string answerModalId = GetAnswerModalId(step.StepType);
                if (answerModalId != null) ReopenModal(answerModalId);
                return;
            }

            var refreshedSession = _sessionService.GetById(session.Id);

            // Sesión completada: ShowCompletionPanel ya cerró todos los controles.
            if (refreshedSession.Status != SessionStatus.InProgress) return;

            // Modal-overlay: se renderiza en esta respuesta para mostrar su confirmación y
            // queda cerrado para el request siguiente. El resto vuelve al POD de inmediato.
            CloseStepControl(IsModalOverlay(step.StepType) ? step.StepType : (StepType?)null);

            var nextStep = guide.Steps.FirstOrDefault(s => s.Order == refreshedSession.CurrentStepOrder);
            if (nextStep != null)
            {
                if (step.StepType == StepType.SFC_Split ||
                    (step.StepType != StepType.SFC_Split && nextStep.StepType == StepType.SFC_Split))
                {
                    HfSelectedRowId.Value = string.Empty;
                    BindWorkList(area, refreshedSession);
                }

                SetActiveButton(nextStep.StepType, area, refreshedSession.Mode);
            }

            // El banner de guardado confirma una respuesta correcta; no se muestra en FailedForced.
            bool accepted = outcome == StepSubmitOutcome.Correct;

            if (step.StepType == StepType.ResourceSetup)
            {
                BindBrowseList(area, session);
                if (accepted) CtrlResourceSetup.ShowSaveConfirmation();
                ReopenModal("modalResourceSetup");
            }
            else if (step.StepType == StepType.LineClearance)
            {
                if (accepted) CtrlLineClearance.ShowSaveConfirmation();
                ReopenModal("modalDCStandalone");
            }
            else if (step.StepType == StepType.Search)
            {
                // El SFC iniciado queda fijo en el header el resto de la sesión, tomado de
                // la orden simulada (servidor) y no del valor que escribió el cliente.
                var order = GetSimulatedOrder(session);
                if (order != null) TxtSFC.Text = order.SFCCode;

                HfSelectedRowId.Value = string.Empty;
                BindWorkList(area, session);
                ShowBanner("✓ Start process completed successfully");
            }
            else if (step.StepType == StepType.SFC_Split)
                ShowBanner("✓ SFC Split completed successfully");
            else if (step.StepType == StepType.LogNC)
                ScrapSelectedRow(area, session);
        }

        // Spec 007: el scrap elimina la fila de la orden simulada, tanto en Correct como en
        // FailedForced. Sin fila válida (force-advance por el gate de LogNC) no se toca nada.
        private void ScrapSelectedRow(Area area, TrainingSession session)
        {
            var row = FindScrappableRow(session, area);
            if (row == null) return;

            var order = GetSimulatedOrder(session);
            order.Rows.Remove(row);

            HfSelectedRowId.Value = string.Empty;
            HfSelectedSFC.Value   = string.Empty;
            BindWorkList(area, session);
            ShowBanner("✓ All of " + order.SFCCode + " scrapped", hideAfterMs: 5000);
        }

        // NonSerialized: solo las filas creadas por el SFC Split se pueden scrapear; la fila
        // original del lote no. Serialized: cualquier fila seleccionada.
        private SimulatedOrderRow FindScrappableRow(TrainingSession session, Area area)
        {
            var row = FindSelectedOrderRow(session);
            if (row == null) return null;
            if (area.SerializationType == SerializationType.NonSerialized && !row.IsFromSplit) return null;
            return row;
        }

        // El operador sale del control sin responder (no cuenta como intento). Mismo
        // criterio que el guard de sender: solo el control abierto puede cerrarse.
        protected void HandleReturnToPod(object sender, EventArgs e)
        {
            var open = OpenStepType;
            if (!open.HasValue || !ReferenceEquals(sender, GetStepControl(open.Value))) return;

            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            CloseStepControl();
            BindWorkList(area, session);
        }

        protected void HandleSplitRequested(object sender, SFCSplitRequestedEventArgs e)
        {
            var session = LoadAndValidateSession();
            if (session == null) return;

            var guide = _guideService.GetById(session.TrainingGuideId);
            var step = guide.Steps.First(s => s.Order == session.CurrentStepOrder);
            if (step.StepType != StepType.SFC_Split)
            {
                e.ErrorMessage = "SFC Split no es el paso activo.";
                return;
            }

            var order = GetSimulatedOrder(session);
            var row = order?.Rows.FirstOrDefault(r => r.RowId == e.RowId);
            if (row == null || e.Qty <= 0 || e.Qty >= row.Qty)
            {
                e.ErrorMessage = "La fila o cantidad seleccionada ya no es válida.";
                return;
            }

            row.Qty -= e.Qty;
            order.Rows.Add(new SimulatedOrderRow
            {
                RowId       = Guid.NewGuid().ToString(),
                Qty         = e.Qty,
                IsFromSplit = true
            });

            e.IsApplied = true;
            e.RemainingQty = row.Qty;
            BindWorkList(_areaRepo.GetById(guide.AreaId), session);
        }

        // Modal donde el operador ingresa la respuesta de cada paso. LineClearance responde
        // en modalDCEntry, apilado sobre modalDCStandalone (que el control ya reabre).
        // LogNC no usa uno: su entrada es un panel de servidor que conserva su estado.
        private static string GetAnswerModalId(StepType stepType)
        {
            switch (stepType)
            {
                case StepType.ResourceSetup:  return "modalResourceSetup";
                case StepType.LineClearance:  return "modalDCEntry";
                case StepType.Search:         return "modalSearch4";
                case StepType.Assembly:       return "modalAssemble";
                case StepType.DataCollection: return "modalDCEntry";
                case StepType.SFC_Split:      return "modalSFCSplit";
                default:                      return null;
            }
        }

        // incorrectFeedback: texto que reemplaza al hint genérico si el Answer resulta incorrecto.
        private StepSubmitOutcome ProcessSubmit(TrainingSession session, TrainingGuide guide, Area area,
            TrainingStep step, string answer, string incorrectFeedback = null)
        {
            var result = _sessionService.SubmitStep(session.Id, step.Id, answer);
            ApplySubmitResult(result, session, guide, area, step, incorrectFeedback);
            return result.Outcome;
        }

        private void ApplySubmitResult(StepSubmitResult result, TrainingSession session, TrainingGuide guide,
            Area area, TrainingStep step, string incorrectFeedback)
        {
            UpdateScoreWidget(session);

            switch (result.Outcome)
            {
                case StepSubmitOutcome.Correct:
                    HandleCorrectSubmit(session, guide, area, step.StepType);
                    break;

                case StepSubmitOutcome.Incorrect:
                    ShowSweetAlert("warning", "Incorrect",
                        BuildIncorrectFeedback(session.Mode, step.StepType, result.AttemptNumber, incorrectFeedback));
                    break;

                case StepSubmitOutcome.FailedForced:
                    ShowSweetAlert("error", "Step Failed",
                        "Paso fallido — el sistema avanza automáticamente.");
                    HandleCorrectSubmit(_sessionService.GetById(session.Id), guide, area, step.StepType);
                    break;

                case StepSubmitOutcome.SessionCompleted:
                    // Sesión recargada: la que llegó como parámetro es anterior al submit
                    // y todavía no tiene FinalScore.
                    ShowCompletionPanel(_sessionService.GetById(session.Id), area, guide);
                    break;
            }
        }

        // Assessment (S2): todos los botones del POD están habilitados y un click en uno que
        // no es el del paso actual es un intento fallido de ese paso (lo registra la BLL).
        // Se evalúa antes que cualquier aviso de formato del botón. Devuelve true si el click
        // fue fuera de secuencia; en Guided esos botones están deshabilitados y se ignora.
        private bool HandleOutOfSequence(StepType? clicked, TrainingSession session, TrainingGuide guide,
            Area area, TrainingStep step)
        {
            if (step.StepType == clicked) return false;

            var result = _sessionService.RegisterOutOfSequence(session.Id, clicked);
            if (result == null) return true;

            ApplySubmitResult(result, session, guide, area, step, incorrectFeedback: null);
            if (result.Outcome == StepSubmitOutcome.FailedForced)
                RestorePodAfterForcedAdvance(step.StepType, session, area);
            return true;
        }

        // Un paso forzado por errores de secuencia nunca abrió su control. Resource Setup sin
        // orden guardada recibe una orden por defecto, y Start forzado carga el POD como si se
        // hubiera ejecutado, para que el resto de la evaluación se pueda completar.
        private void RestorePodAfterForcedAdvance(StepType forcedStep, TrainingSession session, Area area)
        {
            if (forcedStep == StepType.ResourceSetup && GetSimulatedOrder(session) == null)
            {
                Session[Constants.SessionKeys.SimulatedOrderPrefix + session.Id] =
                    SimulatedOrder.Create(DefaultShopOrder, BuildMaterialDescription(area), GetInitialQty(area));
                BindBrowseList(area, session);
            }
            else if (forcedStep == StepType.Search)
            {
                var order = GetSimulatedOrder(session);
                if (order != null) TxtSFC.Text = order.SFCCode;
                HfSelectedRowId.Value = string.Empty;
                BindWorkList(area, session);
            }
        }

        // Guided: hint del paso para que el operador aprenda. Assessment: no se revela la
        // respuesta, solo cuántos intentos con puntaje le quedan antes del avance forzado.
        private static string BuildIncorrectFeedback(SessionMode mode, StepType stepType,
            int attemptNumber, string incorrectFeedback)
        {
            if (mode == SessionMode.Assessment)
            {
                int remaining = ScoreCalculator.MaxScoredAttempts - attemptNumber;
                return remaining > 0
                    ? string.Format("Respuesta incorrecta. Te quedan {0} intento(s) con puntaje.", remaining)
                    : "Respuesta incorrecta. Si vuelves a fallar, el paso se marca fallido y avanza.";
            }

            if (!string.IsNullOrEmpty(incorrectFeedback)) return incorrectFeedback;
            string hint = GetHint(stepType);
            return !string.IsNullOrEmpty(hint) ? hint : "Try again.";
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

            SetActiveButton(nextStep.StepType, area, refreshedSession.Mode);
        }

        // ── Button ServerClick handlers ───────────────────────────────────

        protected void BtnResourceSetup_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(StepType.ResourceSetup, session, guide, area, step)) return;
            OpenStepControl(StepType.ResourceSetup, area, session, guide, "modalResourceSetup");
        }

        protected void BtnActivities_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(StepType.LineClearance, session, guide, area, step)) return;
            // LineClearance se abre como "Data Collection Standalone" desde Activities.
            OpenStepControl(StepType.LineClearance, area, session, guide, "modalDCStandalone");
        }

        protected void BtnStart_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(StepType.Search, session, guide, area, step)) return;

            // Spec 005: sin SimulatedOrder guardada (Resource Setup nunca completado,
            // o Session expirada a mitad de flujo) no hay nada que buscar en Search/Start.
            // Defensivo — en el flujo normal Start ya requiere haber pasado ResourceSetup.
            if (GetSimulatedOrder(session) == null)
            {
                ShowSweetAlert("warning", "Resource Setup requerido",
                    "Debes completar Resource Setup antes de iniciar.");
                return;
            }

            string sfcValue = TxtSFC.Text.Trim();
            if (string.IsNullOrEmpty(sfcValue))
                sfcValue = HfSelectedSFC.Value.Trim();

            if (string.IsNullOrEmpty(sfcValue))
            {
                ShowSweetAlert("warning", "Campo requerido", "Debes ingresar o escanear un SFC antes de iniciar.");
                return;
            }

            // SearchControl abre su propio Modal 1 desde Initialize() (secuencia de 4 modales).
            OpenStepControl(StepType.Search, area, session, guide, modalId: null);
        }

        protected void BtnAssemble_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(StepType.Assembly, session, guide, area, step)) return;
            OpenStepControl(StepType.Assembly, area, session, guide, "modalAssemble");
        }

        protected void BtnDCCollection_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(StepType.DataCollection, session, guide, area, step)) return;

            // Defensa en servidor: el HiddenField puede faltar, estar obsoleto o ser
            // manipulado aunque la validación JavaScript haya sido omitida.
            if (FindSelectedOrderRow(session) == null)
            {
                HfSelectedRowId.Value = string.Empty;
                BindWorkList(area, session);
                ShowSweetAlert("warning", "Selecciona una fila",
                    "Debes seleccionar una fila de la orden en la tabla antes de iniciar Data Collection.");
                return;
            }

            OpenStepControl(StepType.DataCollection, area, session, guide, modalId: null);
        }

        protected void BtnSFCSplit_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(StepType.SFC_Split, session, guide, area, step)) return;

            if (FindSelectedOrderRow(session) == null)
            {
                HfSelectedRowId.Value = string.Empty;
                BindWorkList(area, session);
                ShowSweetAlert("warning", "Selecciona una fila para dividir",
                    "Debes seleccionar una fila válida de la orden antes de iniciar SFC Split.");
                return;
            }

            OpenStepControl(StepType.SFC_Split, area, session, guide, "modalSFCSplit");
        }

        protected void BtnLogNC_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(StepType.LogNC, session, guide, area, step)) return;

            // Spec 007: abrir Log NC sin una fila scrapeable es un intento fallido del paso.
            // No hay gate en el cliente: el click tiene que llegar acá para registrarse.
            if (FindScrappableRow(session, area) == null)
            {
                string feedback = area.SerializationType == SerializationType.NonSerialized
                    ? "Selecciona una fila creada por el split para scrapear"
                    : "Selecciona una fila para scrapear";
                ProcessSubmit(session, guide, area, step, StepAnswers.Invalid, feedback);
                return;
            }

            OpenStepControl(StepType.LogNC, area, session, guide, modalId: null);
        }

        // Acciones de MES que nunca son un paso de la guía (Work Instructions, Reports, Place
        // Back in Queue, Tool List y las opciones de relleno de Activities). No tienen lógica:
        // en Assessment el click es un intento fallido del paso actual (S2).
        protected void BtnNonStepAction_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            HandleOutOfSequence(null, session, guide, area, step);
        }

        private System.Web.UI.HtmlControls.HtmlButton[] NonStepActionButtons => new[]
        {
            BtnWorkInstructions, BtnReports, BtnPlaceBackInQueue, BtnToolList,
            BtnAsBuiltConfig, BtnLogComment, BtnSfcPlaceHold, BtnSfcReleaseHold,
            BtnRemoveFutureHold, BtnChangeEquipmentStatus, BtnTimeSensitiveMaterial
        };

        protected void BtnComplete_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(StepType.Complete, session, guide, area, step)) return;

            // Spec 007: Complete sin fila seleccionada es un intento fallido del paso.
            if (FindSelectedOrderRow(session) == null)
            {
                ProcessSubmit(session, guide, area, step, StepAnswers.Invalid, "Select 1 row to complete");
                return;
            }

            ProcessSubmit(session, guide, area, step, StepAnswers.Complete);
        }

        protected void BtnAbandon_Click(object sender, EventArgs e)
        {
            var session = LoadAndValidateSession();
            if (session == null) return;
            _sessionService.Abandon(session.Id);
            // Spec 005: la SimulatedOrder vive solo en Session — al abandonar,
            // se remueve explícitamente para no dejarla huérfana bajo su clave
            // (a diferencia de completar la guía normalmente, donde sí queda huérfana
            // por diseño, ver Deferred Decisions del spec 005).
            Session.Remove(Constants.SessionKeys.SimulatedOrderPrefix + session.Id);
            Response.Redirect("~/Pages/Training/StartSession.aspx");
        }

        protected void BtnLogout_ServerClick(object sender, EventArgs e)
        {
            Session.Abandon();
            System.Web.Security.FormsAuthentication.SignOut();
            Response.Redirect("~/Pages/Auth/Login.aspx");
        }

        // ── Completion panel ─────────────────────────────────────────────
        private void ShowCompletionPanel(TrainingSession session, Area area, TrainingGuide guide)
        {
            CloseStepControl();
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
                LitFinalResult.Text    = ScoreCalculator.IsPassed(session.FinalScore)
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

            // Spec 005: BuildSimulatedSFCCode ya no existe — el SFC real vive en la
            // SimulatedOrder de sesión. Antes de Resource Setup no hay orden aún,
            // así que se usa un placeholder en vez de fallar.
            string sfcCode    = GetSimulatedOrder(session)?.SFCCode ?? "—";
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

                case StepType.SFC_Split:
                    var selectedRow = FindSelectedOrderRow(session);
                    vm.ExtraData["AvailableQty"] = selectedRow?.Qty ?? 0;
                    vm.ExtraData["SelectedRowId"] = selectedRow?.RowId;
                    vm.ExtraData["HasSuccessfulSplit"] = HasSuccessfulSplit(session);
                    break;

                case StepType.LogNC:
                    vm.ExtraData["NCCodes"] = _ncRepo.GetByArea(area.Id);
                    vm.ExtraData["RequiresNCComponent"] = area.RequiresNCComponent;
                    break;

                case StepType.Assembly:
                    // Moldeo has RequiresAssembly=false; this block runs for areas with assembly
                    vm.ExtraData["ExpectedCount"] = 3;
                    break;

                case StepType.Search:
                    // SearchControl owns its own 4-modal flow (labels hardcoded in the .ascx);
                    // it only needs ResourceName, already populated above.
                    break;

                case StepType.ResourceSetup:
                    // Datos que ResourceSetupControl necesita para construir la
                    // SimulatedOrder en Session al guardar (spec 005).
                    vm.ExtraData["TrainingSessionId"]    = session.Id;
                    vm.ExtraData["MaterialDescription"]  = BuildMaterialDescription(area);
                    vm.ExtraData["InitialQty"]           = GetInitialQty(area);
                    break;
            }

            return vm;
        }

        private string BuildMaterialDescription(Area area)
        {
            var product = _productRepo.GetAll().FirstOrDefault(p => p.Id == area.ProductId);
            return (product?.Name ?? "") + " / " + area.Name;
        }

        private static int GetInitialQty(Area area)
            => area.SerializationType == SerializationType.NonSerialized ? 250 : 1;

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
            var rows = BuildPodWorkList(session);
            RptWorkList.DataSource = rows;
            RptWorkList.DataBind();
        }

        private void BindBrowseList(Area area, TrainingSession session)
        {
            var rows = BuildPodWorkList(session);
            RptBrowseList.DataSource = rows;
            RptBrowseList.DataBind();
        }

        // Spec 005: la orden ya no es hardcodeada — se lee de la SimulatedOrder
        // guardada en Session por ResourceSetupControl. Sin orden guardada, el
        // POD y el Browse Work List quedan vacíos (ver Deferred Decisions del spec).
        private SimulatedOrder GetSimulatedOrder(TrainingSession session)
        {
            return Session[Constants.SessionKeys.SimulatedOrderPrefix + session.Id] as SimulatedOrder;
        }

        // Spec 005: resuelve la fila del POD que el operador seleccionó (HfSelectedRowId,
        // llenado en cliente por selectWorkListRow/confirmBrowseSelection). Usada por
        // SFC Split para el tope de Qty y por el gating de DC Collect en el servidor.
        private SimulatedOrderRow FindSelectedOrderRow(TrainingSession session)
        {
            var order = GetSimulatedOrder(session);
            if (order == null) return null;
            string rowId = HfSelectedRowId.Value;
            if (string.IsNullOrEmpty(rowId)) return null;
            return order.Rows.FirstOrDefault(r => r.RowId == rowId);
        }

        private static bool IsSearchCompleted(TrainingGuide guide, TrainingSession session)
        {
            var search = guide.Steps.FirstOrDefault(s => s.StepType == StepType.Search);
            return search != null && session.CurrentStepOrder > search.Order;
        }

        private bool HasSuccessfulSplit(TrainingSession session)
        {
            var order = GetSimulatedOrder(session);
            return order != null && order.Rows.Count > 1;
        }

        private IList<PodWorkListRow> BuildPodWorkList(TrainingSession session)
        {
            var order = GetSimulatedOrder(session);
            if (order == null) return new List<PodWorkListRow>();

            return order.Rows.Select(r => new PodWorkListRow
            {
                RowId               = r.RowId,
                SFCCode             = order.SFCCode,
                MaterialDescription = order.MaterialDescription,
                Qty                 = r.Qty,
                Status              = order.Status,
                ProcessLot          = order.ProcessLot,
                ShopOrder           = order.ShopOrderInput,
                IsSelected          = false,
                ScheduleStart       = DateTime.Today.ToString("MM/dd/yyyy"),
                PlannedCompletion   = DateTime.Today.AddDays(1).ToString("MM/dd/yyyy")
            }).ToList();
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

        private void SetActiveButton(StepType stepType, Area area, SessionMode mode)
        {
            // Assessment (S2): todo habilitado — elegir el botón correcto es parte de la evaluación.
            bool enableAll = mode == SessionMode.Assessment;

            BtnResourceSetup.Disabled = !enableAll;
            BtnStart.Disabled         = !enableAll;
            BtnAssemble.Disabled      = !enableAll;
            BtnDCCollection.Disabled  = !enableAll;
            BtnComplete.Disabled      = !enableAll;
            BtnActivities.Disabled    = !enableAll;
            BtnSFCSplit.Disabled      = !enableAll;
            BtnLogNC.Disabled         = !enableAll;
            foreach (var decoy in NonStepActionButtons)
                decoy.Disabled = !enableAll;

            // La validación de fila en cliente (formato) solo aplica al botón del paso actual:
            // un click fuera de secuencia tiene que llegar al servidor para contar.
            SetClientRowCheck(BtnDCCollection, "podValidateDCCollect", stepType == StepType.DataCollection);
            SetClientRowCheck(BtnSFCSplit, "podValidateSFCSplit", stepType == StepType.SFC_Split);

            if (enableAll)
            {
                EnableActivitiesToggle();
                return;
            }

            switch (stepType)
            {
                case StepType.ResourceSetup:
                    BtnResourceSetup.Disabled = false;
                    break;
                case StepType.LineClearance:
                    BtnActivities.Disabled = false;
                    EnableActivitiesToggle();
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
                case StepType.SFC_Split:
                    if (area.SerializationType == SerializationType.NonSerialized)
                        BtnSFCSplit.Disabled = false;
                    break;
                case StepType.LogNC:
                    BtnLogNC.Disabled = false;
                    break;
                case StepType.Complete:
                    BtnComplete.Disabled = false;
                    break;
            }
        }

        // Enable the dropdown toggle so the Activities menu can open
        private void EnableActivitiesToggle()
        {
            Page.ClientScript.RegisterStartupScript(GetType(), "enableActivitiesToggle",
                "var t=document.getElementById('BtnActivitiesToggle');" +
                "if(t) t.disabled=false;", true);
        }

        // Se asigna en cada request: los atributos de un HtmlControl persisten en ViewState.
        private static void SetClientRowCheck(System.Web.UI.HtmlControls.HtmlButton button,
            string validatorFunction, bool isCurrentStep)
        {
            if (isCurrentStep)
                button.Attributes["onclick"] = "if (!" + validatorFunction + "()) return false;";
            else
                button.Attributes.Remove("onclick");
        }

        private void ShowBanner(string message, int hideAfterMs = 3000)
        {
            PnlBannerZone.Visible = true;
            LitBanner.Text        = HttpUtility.HtmlEncode(message);
            ClientScript.RegisterStartupScript(GetType(), "hideBanner",
                "setTimeout(function(){var b=document.querySelector('.pod-banner-zone');if(b)b.style.display='none';}," +
                hideAfterMs + ");",
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
                case StepType.Search:         return "Confirm the Line Clearance checks and the final verification with 'Sí', then Save.";
                case StepType.DataCollection: return "Fill all required parameters and press Save (not Draft).";
                case StepType.SFC_Split:      return "Select a POD row, split a valid quantity, then press Close.";
                case StepType.LogNC:          return "Select an NC code and enter the Component, then Add-Done.";
                case StepType.Assembly:       return "Scan all required component barcodes and press Add for each.";
                case StepType.Complete:       return "Press Complete to close the manufacturing order.";
                default:                      return string.Empty;
            }
        }
    }
}
