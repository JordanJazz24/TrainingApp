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
        // Shop Order genérica de la orden por defecto cuando Resource Setup avanza forzado
        // (S2 / spec 008, E8). Dato de simulación, no de negocio.
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

        // Serialized (spec 008): el POD y Browse Work List permiten varias filas y TxtSFC
        // muestra "[N SELECTED]". NonSerialized: un click selecciona una fila (spec 007); el
        // ícono Select All selecciona todas en ambos tipos.
        protected bool PodMultiSelect { get; private set; }

        // Search/Start ya completado: el POD Search muestra la orden Active y Browse Work
        // List ya no la cambia a In Queue en el cliente.
        protected bool PodOrderStarted { get; private set; }

        // Status (SFC) de las piezas elegidas en Browse Work List antes del Start.
        private const string SfcStatusInQueue = "In Queue";

        // Ícono Select All del header del POD y de Browse Work List: cuadros apilados, como
        // en el MES real. Los colores salen del CSS (.select-all-btn).
        protected const string SelectAllIcon =
            "<svg width=\"15\" height=\"15\" viewBox=\"0 0 15 15\" aria-hidden=\"true\">" +
            "<rect class=\"sa-back\" x=\"0.5\" y=\"0.5\" width=\"9\" height=\"9\" />" +
            "<rect class=\"sa-back\" x=\"2.5\" y=\"2.5\" width=\"9\" height=\"9\" />" +
            "<rect class=\"sa-front\" x=\"5\" y=\"5\" width=\"9\" height=\"9\" />" +
            "</svg>";

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

            PodMultiSelect = area.SerializationType == SerializationType.Serialized;

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
            BindSearchHeader(session, guide);
        }

        // POD Search: Shop Order y Status (SFC) de la orden cargada. Antes del Start, las
        // piezas elegidas en Browse Work List (In Queue); con el Start, Active. Se recalcula
        // en cada request porque el OK de Browse solo lo adelanta en el cliente.
        private void BindSearchHeader(TrainingSession session, TrainingGuide guide)
        {
            var order = GetSimulatedOrder(session);
            if (order != null && IsSearchCompleted(guide, session))
                SetSearchHeader(order.ShopOrderInput, SimulatedOrderRow.StatusActive);
            else if (order != null && FindBrowseSelectedRows(order).Count > 0)
                SetSearchHeader(order.ShopOrderInput, SfcStatusInQueue);
            else
                SetSearchHeader(string.Empty, string.Empty);
        }

        private void SetSearchHeader(string shopOrder, string sfcStatus)
        {
            LitShopOrder.Text = HttpUtility.HtmlEncode(shopOrder);
            LitSFCStatus.Text = HttpUtility.HtmlEncode(sfcStatus);
            PodOrderStarted   = sfcStatus == SimulatedOrderRow.StatusActive;
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

            ContinueAfterStepResolved(session, guide, area, step, outcome);
        }

        // Paso resuelto (Correct, FailedForced o sesión completada) desde un control: cierra el
        // control, habilita el botón del paso siguiente y aplica lo propio de cada paso. Lo
        // usan el submit de los controles y el ensamble del último componente (spec 008).
        private void ContinueAfterStepResolved(TrainingSession session, TrainingGuide guide,
            Area area, TrainingStep step, StepSubmitOutcome outcome)
        {
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
                    ClearRowSelection();
                    BindWorkList(area, refreshedSession);
                }

                SetActiveButton(nextStep.StepType, area, refreshedSession.Mode);
            }

            // El banner de guardado confirma una respuesta correcta; no se muestra en FailedForced.
            bool accepted = outcome == StepSubmitOutcome.Correct;

            if (step.StepType == StepType.ResourceSetup)
            {
                if (outcome == StepSubmitOutcome.FailedForced)
                    ReplaceWithDefaultOrder(session, area);
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
                LoadStartedOrderOnPod(session, area);
                ShowBanner("Start process completed successfully");
            }
            else if (step.StepType == StepType.Assembly)
            {
                // La Component List reemplazaba el POD: se vuelve a pintar con la selección.
                BindWorkList(area, session);
                ShowBanner("Assembly completed successfully");
            }
            else if (step.StepType == StepType.DataCollection)
            {
                // La Data Collection List reemplazaba el POD.
                BindWorkList(area, session);
            }
            else if (step.StepType == StepType.SFC_Split)
                ShowBanner("SFC Split completed successfully");
            else if (step.StepType == StepType.LogNC)
                ScrapSelectedRows(area, session);
        }

        // Assembly (spec 008): el operador ensambló un componente del BOM. Mientras falten
        // componentes la Component List se actualiza; con el último, el paso se resuelve.
        protected void HandleComponentAssembled(object sender, ComponentAssembledEventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            // Mismo guard de sender que HandleControlSubmit (spec 006, E12).
            if (!ReferenceEquals(sender, GetStepControl(step.StepType))) return;

            var result = _sessionService.AssembleComponent(session.Id, e.ComponentId, GetAssemblyProgress(session));
            if (result.StepResult == null)
            {
                CtrlAssembly.ViewModel = BuildStepViewModel(step, area, session, guide);
                CtrlAssembly.BindComponents();
                return;
            }

            ClearAssemblyProgress(session);
            ApplySubmitResult(result.StepResult, session, guide, area, step, incorrectFeedback: null);
            ContinueAfterStepResolved(session, guide, area, step, result.StepResult.Outcome);
        }

        // Avance de Assembly en Session (servidor): sobrevive a Return to POD y a F5.
        private AssemblyProgress GetAssemblyProgress(TrainingSession session)
        {
            string key = Constants.SessionKeys.AssemblyProgressPrefix + session.Id;
            var progress = Session[key] as AssemblyProgress;
            if (progress == null)
            {
                progress = new AssemblyProgress();
                Session[key] = progress;
            }
            return progress;
        }

        private void ClearAssemblyProgress(TrainingSession session)
        {
            Session.Remove(Constants.SessionKeys.AssemblyProgressPrefix + session.Id);
        }

        // Spec 007: el scrap elimina la fila de la orden simulada, tanto en Correct como en
        // FailedForced. Sin fila válida (force-advance por el gate de LogNC) no se toca nada.
        // Spec 008: en Serialized se scrapean todas las filas seleccionadas de una vez.
        private void ScrapSelectedRows(Area area, TrainingSession session)
        {
            var rows = FindScrappableRows(session, area);
            if (rows.Count == 0) return;

            var order = GetSimulatedOrder(session);
            foreach (var row in rows)
                order.Rows.Remove(row);

            string scrapped = rows.Count == 1
                ? "All of " + (rows[0].SFCCode ?? order.SFCCode)
                : rows.Count + " SFCs";

            ClearRowSelection();
            // NonSerialized: TxtSFC es el SFC del lote y no cambia (spec 007).
            if (area.SerializationType == SerializationType.Serialized)
                TxtSFC.Text = string.Empty;
            BindWorkList(area, session);
            ShowBanner(scrapped + " scrapped", hideAfterMs: 5000);
        }

        // NonSerialized: exactamente una fila, creada por el SFC Split; la fila original del
        // lote no se scrapea (spec 007). Serialized: las filas seleccionadas, siempre que
        // quede al menos una pieza para Complete (spec 008).
        private IList<SimulatedOrderRow> FindScrappableRows(TrainingSession session, Area area)
        {
            var selected = FindSelectedOrderRows(session);

            if (area.SerializationType == SerializationType.Serialized)
                return PieceSelectionRule.Evaluate(StepType.LogNC, selected.Count, ActiveRowCount(session))
                       == PieceSelectionResult.Valid
                    ? selected
                    : new List<SimulatedOrderRow>();

            return selected.Count == 1 && selected[0].IsFromSplit
                ? selected
                : new List<SimulatedOrderRow>();
        }

        private int ActiveRowCount(TrainingSession session)
            => GetSimulatedOrder(session)?.Rows.Count(IsActive) ?? 0;

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
                SFCCode     = order.SFCCode,
                Qty         = e.Qty,
                Status      = SimulatedOrderRow.StatusActive,
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
            if (forcedStep == StepType.ResourceSetup)
            {
                ReplaceWithDefaultOrder(session, area);
            }
            else if (forcedStep == StepType.Search)
            {
                // E9 (spec 008): Start forzado inicia todas las piezas.
                LoadStartedOrderOnPod(session, area, startAllPieces: true);
            }
            else if (forcedStep == StepType.Assembly)
            {
                // El paso quedó fallido: el avance parcial ya no se usa.
                ClearAssemblyProgress(session);
            }
            else if (forcedStep == StepType.DataCollection)
            {
                ClearDataCollectionProgress(session);
            }
        }

        // Resource Setup forzado (por secuencia o por Setup State incorrecto): la orden sale
        // siempre de la Shop Order genérica, ignorando lo que el operador capturó en un
        // intento fallido. En Serialized también genera sus piezas (E8, spec 008).
        private void ReplaceWithDefaultOrder(TrainingSession session, Area area)
        {
            Session[Constants.SessionKeys.SimulatedOrderPrefix + session.Id] =
                SimulatedOrder.Create(DefaultShopOrder, BuildMaterialDescription(area),
                    GetInitialQty(area), area.SerializationType);
            BindBrowseList(area, session);
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
            var order = GetSimulatedOrder(session);
            if (order == null)
            {
                ShowSweetAlert("warning", "Resource Setup requerido",
                    "Debes completar Resource Setup antes de iniciar.");
                return;
            }

            if (!ValidateStartSelection(area, order)) return;

            // Checklist (Moldeo): SearchControl abre su propio Modal 1 desde Initialize()
            // (secuencia de 4 modales); el Start se completa en HandleControlSubmit.
            if (area.RequiresStartChecklist)
            {
                OpenStepControl(StepType.Search, area, session, guide, modalId: null);
                return;
            }

            // Start directo (spec 008): sin modales, se califica solo por secuencia.
            var outcome = ProcessSubmit(session, guide, area, step, StepAnswers.Start);
            if (outcome == StepSubmitOutcome.Incorrect) return;
            LoadStartedOrderOnPod(session, area);
            ShowBanner("Start process completed successfully");
        }

        private const string AllPiecesTitle   = "Selecciona todas las piezas";
        private const string AllPiecesMessage =
            "En el simulador se trabajan todas las piezas juntas: selecciona todas las filas.";

        // Aviso de formato, sin intento: Serialized exige todas las piezas de la orden en
        // Browse Work List (spec 008); NonSerialized, el SFC del lote.
        private bool ValidateStartSelection(Area area, SimulatedOrder order)
        {
            if (area.SerializationType == SerializationType.Serialized)
            {
                var result = PieceSelectionRule.Evaluate(StepType.Search,
                    FindBrowseSelectedRows(order).Count, order.Rows.Count);
                if (result == PieceSelectionResult.Valid) return true;

                ShowSweetAlert("warning", AllPiecesTitle, AllPiecesMessage);
                return false;
            }

            string sfcValue = TxtSFC.Text.Trim();
            if (string.IsNullOrEmpty(sfcValue))
                sfcValue = HfSelectedSFC.Value.Trim();

            if (string.IsNullOrEmpty(sfcValue))
            {
                ShowSweetAlert("warning", "Campo requerido", "Debes ingresar o escanear un SFC antes de iniciar.");
                return false;
            }
            return true;
        }

        // Start completado (directo, con checklist o forzado): las piezas iniciadas pasan a
        // Active y el POD las carga. Serialized: las elegidas en Browse Work List (todas si
        // el Start fue forzado, E9) y quedan seleccionadas, como en el POD real.
        // NonSerialized: el SFC del lote queda fijo en el header, tomado de la orden (servidor).
        private void LoadStartedOrderOnPod(TrainingSession session, Area area, bool startAllPieces = false)
        {
            ClearRowSelection();

            var order = GetSimulatedOrder(session);
            if (order != null)
            {
                if (area.SerializationType == SerializationType.Serialized)
                {
                    var started = startAllPieces ? order.Rows : FindBrowseSelectedRows(order);
                    foreach (var row in started)
                        row.Status = SimulatedOrderRow.StatusActive;

                    var active = order.Rows.Where(IsActive).ToList();
                    HfSelectedRowIds.Value = string.Join(",", active.Select(r => r.RowId));
                    TxtSFC.Text            = DescribeSelection(active);
                }
                else
                {
                    TxtSFC.Text = order.SFCCode;
                }

                SetSearchHeader(order.ShopOrderInput, SimulatedOrderRow.StatusActive);
            }

            BindWorkList(area, session);
        }

        protected void BtnAssemble_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(StepType.Assembly, session, guide, area, step)) return;
            if (!ValidatePodSelection(StepType.Assembly, session, area)) return;

            // Abre la Component List; el modal Assemble se abre desde cada componente.
            OpenStepControl(StepType.Assembly, area, session, guide, modalId: null);
        }

        // Aviso de formato, sin intento. Serialized: el paso exige todas las filas del POD
        // (spec 008, punto 6). NonSerialized: una fila seleccionada.
        private bool ValidatePodSelection(StepType stepType, TrainingSession session, Area area,
            string singleRowMessage = "Debes seleccionar una fila de la orden en la tabla antes de continuar.")
        {
            if (area.SerializationType == SerializationType.Serialized)
            {
                int active = GetSimulatedOrder(session)?.Rows.Count(IsActive) ?? 0;
                var result = PieceSelectionRule.Evaluate(stepType, FindSelectedOrderRows(session).Count, active);
                if (result == PieceSelectionResult.Valid) return true;

                ShowSweetAlert("warning", AllPiecesTitle, AllPiecesMessage);
                return false;
            }

            if (FindSelectedOrderRow(session) != null) return true;

            ClearRowSelection();
            BindWorkList(area, session);
            ShowSweetAlert("warning", "Selecciona una fila", singleRowMessage);
            return false;
        }

        protected void BtnDCCollection_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(StepType.DataCollection, session, guide, area, step)) return;

            // Defensa en servidor: el HiddenField puede faltar, estar obsoleto o ser
            // manipulado aunque la validación JavaScript haya sido omitida. Serialized exige
            // todas las filas del POD (spec 008).
            if (!ValidatePodSelection(StepType.DataCollection, session, area,
                    "Debes seleccionar una fila de la orden en la tabla antes de iniciar Data Collection."))
                return;

            // La primera apertura fija las piezas a recorrer; al reabrir se sigue donde quedó (AC-9).
            if (GetDataCollectionProgress(session) == null)
                StartDataCollectionProgress(session);

            OpenStepControl(StepType.DataCollection, area, session, guide, modalId: null);
        }

        // Data Collection (spec 008): Save de un DC Group para la pieza actual.
        protected void HandleGroupSubmit(object sender, DataCollectionGroupSubmitEventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            // Mismo guard de sender que HandleControlSubmit (spec 006, E12).
            if (!ReferenceEquals(sender, GetStepControl(step.StepType))) return;

            var progress = GetDataCollectionProgress(session) ?? StartDataCollectionProgress(session);
            string pieceSfc = progress.CurrentPiece?.SFCCode;

            var result = _sessionService.SubmitDataCollectionGroup(session.Id, e.GroupCode, e.AnswersJson, progress);
            switch (result.Outcome)
            {
                case DataCollectionOutcome.Incorrect:
                    // El control queda abierto en el modal donde se respondió (spec 006, E3);
                    // grupos y piezas ya guardados se conservan (AC-8).
                    ApplySubmitResult(result.StepResult, session, guide, area, step, incorrectFeedback: null);
                    ReopenModal("modalDCEntry");
                    return;

                case DataCollectionOutcome.GroupSaved:
                    RefreshDataCollectionControl(session, guide, area, step);
                    ShowBanner("All data for DC group " + e.GroupCode + " has been collected", hideAfterMs: 4000);
                    return;

                case DataCollectionOutcome.PieceCompleted:
                    RefreshDataCollectionControl(session, guide, area, step);
                    ShowBanner(string.Format("SFC {0} completed. Next: {1}",
                        pieceSfc, DescribeCurrentPiece(progress)), hideAfterMs: 5000);
                    return;

                case DataCollectionOutcome.StepResolved:
                    // Paso completo, forzado (E5: el avance parcial se descarta) o sesión terminada.
                    ClearDataCollectionProgress(session);
                    ApplySubmitResult(result.StepResult, session, guide, area, step, incorrectFeedback: null);
                    ContinueAfterStepResolved(session, guide, area, step, result.StepResult.Outcome);
                    return;
            }
        }

        private void RefreshDataCollectionControl(TrainingSession session, TrainingGuide guide,
            Area area, TrainingStep step)
        {
            CtrlDataCollection.ViewModel = BuildStepViewModel(step, area, session, guide);
            CtrlDataCollection.ShowGroupList();
        }

        // Avance de DC en Session (servidor): sobrevive a Return to POD y a F5.
        private DataCollectionProgress GetDataCollectionProgress(TrainingSession session)
        {
            return Session[Constants.SessionKeys.DataCollectionProgressPrefix + session.Id] as DataCollectionProgress;
        }

        // Las piezas son las filas seleccionadas del POD, en su orden (NonSerialized: el lote).
        private DataCollectionProgress StartDataCollectionProgress(TrainingSession session)
        {
            var progress = new DataCollectionProgress();
            foreach (var row in FindSelectedOrderRows(session))
                progress.Pieces.Add(new DataCollectionPiece { RowId = row.RowId, SFCCode = row.SFCCode });

            Session[Constants.SessionKeys.DataCollectionProgressPrefix + session.Id] = progress;
            return progress;
        }

        private void ClearDataCollectionProgress(TrainingSession session)
        {
            Session.Remove(Constants.SessionKeys.DataCollectionProgressPrefix + session.Id);
        }

        // "000123-430 (2/3)": pieza actual y posición dentro de las piezas del paso.
        private static string DescribeCurrentPiece(DataCollectionProgress progress)
        {
            if (progress == null || progress.CurrentPiece == null) return "—";
            return string.Format("{0} ({1}/{2})",
                progress.CurrentPiece.SFCCode, progress.CurrentIndex + 1, progress.Pieces.Count);
        }

        protected void BtnSFCSplit_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(StepType.SFC_Split, session, guide, area, step)) return;

            if (FindSelectedOrderRow(session) == null)
            {
                ClearRowSelection();
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

            // Spec 008: en Serialized scrapear todas las piezas es un aviso de formato, sin
            // intento — tiene que quedar al menos una para Complete.
            if (area.SerializationType == SerializationType.Serialized &&
                PieceSelectionRule.Evaluate(StepType.LogNC, FindSelectedOrderRows(session).Count,
                    ActiveRowCount(session)) == PieceSelectionResult.NoPieceLeft)
            {
                ShowSweetAlert("warning", "Selección inválida", "Debe quedar al menos una pieza para Complete.");
                return;
            }

            // Spec 007: abrir Log NC sin una fila scrapeable es un intento fallido del paso.
            // No hay gate en el cliente: el click tiene que llegar acá para registrarse.
            if (FindScrappableRows(session, area).Count == 0)
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

            if (area.SerializationType == SerializationType.Serialized)
            {
                // Spec 008: Complete cierra todas las filas que quedan en el POD. Algunas: aviso
                // de formato, sin intento. Ninguna: intento fallido (spec 007).
                var result = PieceSelectionRule.Evaluate(StepType.Complete,
                    FindSelectedOrderRows(session).Count, ActiveRowCount(session));
                if (result == PieceSelectionResult.MissingPieces)
                {
                    ShowSweetAlert("warning", AllPiecesTitle, AllPiecesMessage);
                    return;
                }
                if (PieceSelectionRule.CountsAsFailedAttempt(StepType.Complete, result))
                {
                    ProcessSubmit(session, guide, area, step, StepAnswers.Invalid, "Select all rows to complete");
                    return;
                }
            }
            else if (FindSelectedOrderRow(session) == null)
            {
                // Spec 007: Complete sin fila seleccionada es un intento fallido del paso.
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
            ClearAssemblyProgress(session);
            ClearDataCollectionProgress(session);
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
            // NonSerialized: el SFC del lote. Serialized: el SFC o "[N SELECTED]" de la selección
            // (ej. NC Data Entry de varias piezas, spec 008).
            string sfcCode = GetSimulatedOrder(session)?.SFCCode;
            if (string.IsNullOrEmpty(sfcCode))
                sfcCode = DescribeSelection(FindSelectedOrderRows(session));
            if (string.IsNullOrEmpty(sfcCode))
                sfcCode = "—";
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
                    vm.ExtraData["Groups"]     = _guideService.GetDataCollectionGroups(area.Id);
                    var dcProgress = GetDataCollectionProgress(session);
                    vm.ExtraData["PieceLabel"] = DescribeCurrentPiece(dcProgress);
                    // Grupos ya guardados por la pieza actual: su Collect se deshabilita.
                    vm.ExtraData["SavedGroupIds"] = dcProgress?.CurrentPieceSavedGroupIds ?? new HashSet<int>();
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
                    // BOM del área y componentes ya ensamblados (spec 008).
                    vm.ExtraData["Components"]         = _guideService.GetAssemblyComponents(area.Id);
                    vm.ExtraData["AssembledIds"]       = GetAssemblyProgress(session).AssembledComponentIds;
                    vm.ExtraData["SelectedPieceCount"] = FindSelectedOrderRows(session).Count;
                    vm.ExtraData["ProcessLot"]         = GetSimulatedOrder(session)?.ProcessLot;
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
                    vm.ExtraData["SerializationType"]    = area.SerializationType;
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
            // El MES real muestra el Code1 del empleado, no su nombre (Fase 6.5).
            LitUserName.Text      = HttpUtility.HtmlEncode(
                Session[Constants.SessionKeys.Code1]?.ToString() ?? "");
            LitOperationName.Text = HttpUtility.HtmlEncode(guide.Title);
            LitResourceName.Text  = HttpUtility.HtmlEncode(area.Name + "-01");
            LitBrowseContext.Text = string.Format("{0}, {1}-01, {2}",
                area.Name, area.Name,
                Session[Constants.SessionKeys.Code1]?.ToString() ?? "");
        }

        // El POD solo muestra las filas iniciadas (Active). En NonSerialized el lote ya
        // nace Active; en Serialized cada pieza pasa a Active con el Start (spec 008).
        private void BindWorkList(Area area, TrainingSession session)
        {
            var rows = BuildPodWorkList(session, onlyActive: true);
            RptWorkList.DataSource = rows;
            RptWorkList.DataBind();
        }

        // Browse Work List muestra todas las filas de la orden (sin columna Status).
        private void BindBrowseList(Area area, TrainingSession session)
        {
            var rows = BuildPodWorkList(session, onlyActive: false);
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

        // Filas del POD que el operador seleccionó (HfSelectedRowIds, llenado en cliente por
        // selectWorkListRow). El cliente puede mandar cualquier cosa: solo cuentan los ids que
        // existen en la orden y están Active (spec 008).
        private IList<SimulatedOrderRow> FindSelectedOrderRows(TrainingSession session)
        {
            var order = GetSimulatedOrder(session);
            if (order == null) return new List<SimulatedOrderRow>();
            var ids = ParseRowIds(HfSelectedRowIds.Value);
            return order.Rows.Where(r => IsActive(r) && ids.Contains(r.RowId)).ToList();
        }

        // Pasos de una sola fila (NonSerialized: DC, SFC Split, Log NC, Complete): la fila
        // seleccionada solo si hay exactamente una.
        private SimulatedOrderRow FindSelectedOrderRow(TrainingSession session)
        {
            var rows = FindSelectedOrderRows(session);
            return rows.Count == 1 ? rows[0] : null;
        }

        // Piezas elegidas en Browse Work List para el Start (HfBrowseRowIds).
        private IList<SimulatedOrderRow> FindBrowseSelectedRows(SimulatedOrder order)
        {
            var ids = ParseRowIds(HfBrowseRowIds.Value);
            return order.Rows.Where(r => ids.Contains(r.RowId)).ToList();
        }

        private static HashSet<string> ParseRowIds(string value)
        {
            return new HashSet<string>((value ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim()));
        }

        private static bool IsActive(SimulatedOrderRow row)
            => (row.Status ?? SimulatedOrderRow.StatusActive) == SimulatedOrderRow.StatusActive;

        private void ClearRowSelection()
        {
            HfSelectedRowIds.Value = string.Empty;
            HfSelectedSFC.Value    = string.Empty;
        }

        // Texto de TxtSFC para una selección (spec 008): el SFC si es una fila, "[N SELECTED]"
        // si son varias (igual que el POD real). selectWorkListRow hace lo mismo en el cliente.
        private static string DescribeSelection(IList<SimulatedOrderRow> rows)
        {
            if (rows.Count == 0) return string.Empty;
            return rows.Count == 1 ? rows[0].SFCCode : "[" + rows.Count + " SELECTED]";
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

        private IList<PodWorkListRow> BuildPodWorkList(TrainingSession session, bool onlyActive)
        {
            var order = GetSimulatedOrder(session);
            if (order == null) return new List<PodWorkListRow>();

            return order.Rows.Where(r => !onlyActive || IsActive(r)).Select(r => new PodWorkListRow
            {
                RowId               = r.RowId,
                SFCCode             = r.SFCCode ?? order.SFCCode,
                MaterialDescription = order.MaterialDescription,
                Qty                 = r.Qty,
                Status              = r.Status ?? order.Status,
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

        // Mensaje del MES entre el header y el POD Search (con ícono verde, como el real).
        // Ocupa su propia franja y desaparece solo después de hideAfterMs.
        private void ShowBanner(string message, int hideAfterMs = 3000)
        {
            PnlBannerZone.Visible = true;
            LitBanner.Text        = HttpUtility.HtmlEncode(message);
            ClientScript.RegisterStartupScript(GetType(), "hideBanner",
                "setTimeout(function(){var b=document.querySelector('.pod-banner');if(b)b.style.display='none';}," +
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
                case StepType.Assembly:       return "Assemble every component in the Component List: scan its barcode and press Add.";
                case StepType.Complete:       return "Press Complete to close the manufacturing order.";
                default:                      return string.Empty;
            }
        }
    }
}
