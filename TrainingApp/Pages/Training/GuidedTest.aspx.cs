using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Models;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Training
{
    public partial class GuidedTest : Page
    {
        // El POD lee catálogos y sesión solo a través de la BLL (auditoría AUD-14).
        private readonly TrainingGuideService _guideService = new TrainingGuideService();

        // Motor del POD (spec 009). Se crea al primer uso: necesita la Session del request.
        private PodService _pod;
        private PodService Pod => _pod ?? (_pod = new PodService(new SessionPodStateStore(Session)));

        // Contexto del request (spec 009, AUD-25): sesión, guía, área, intentos y estado del
        // POD, cargados una vez en Page_Load. Los handlers de eventos corren después y lo usan.
        private PodContext _ctx;

        // Orden simulada de la sesión. Sin orden (antes de Resource Setup) el POD y el Browse
        // Work List quedan vacíos (spec 005).
        private SimulatedOrder Order => _ctx?.State.Order;


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

            _ctx = LoadContext();
            if (_ctx == null) return;

            var (session, guide, area, step) = GetContext();

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
                    var order = Order;
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
            var order = Order;
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
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            // Guard de sender (spec 006, E12): ASP.NET dispara eventos también en controles
            // invisibles, y el Answer se evalúa contra el paso actual. Un click en el modal
            // de confirmación de un paso ya completado penalizaría el paso siguiente.
            if (!ReferenceEquals(sender, GetStepControl(step.StepType))) return;

            StepSubmitResult result;
            switch (step.StepType)
            {
                case StepType.ResourceSetup:
                    // La BLL crea la orden simulada con la Shop Order capturada (spec 005).
                    result = Pod.SubmitResourceSetup(_ctx, e.Answer, e.ShopOrder);
                    break;

                case StepType.Search:
                    // Checklist de Start (Moldeo): resuelto el paso, la BLL inicia las piezas.
                    result = Pod.SubmitStartChecklist(_ctx, e.Answer, BrowseSelectedIds());
                    break;

                case StepType.SFC_Split:
                    var close = Pod.CloseSplit(_ctx, e.Answer);
                    if (close.Notice != PodNotice.None)
                    {
                        ShowNotice(close.Notice);
                        ReopenModal("modalSFCSplit");
                        return;
                    }
                    result = close.Step;
                    break;

                default:
                    result = Pod.SubmitStep(_ctx, e.Answer);
                    break;
            }

            ApplySubmitResult(result, session, guide, area, step, e.IncorrectFeedback);
            var outcome = result.Outcome;

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
            // Sesión completada: ShowCompletionPanel ya cerró todos los controles. La sesión ya
            // refleja el intento registrado (PodService actualiza el contexto).
            if (session.Status != SessionStatus.InProgress) return;

            // Modal-overlay: se renderiza en esta respuesta para mostrar su confirmación y
            // queda cerrado para el request siguiente. El resto vuelve al POD de inmediato.
            CloseStepControl(IsModalOverlay(step.StepType) ? step.StepType : (StepType?)null);

            var nextStep = guide.Steps.FirstOrDefault(s => s.Order == session.CurrentStepOrder);
            if (nextStep != null)
            {
                if (step.StepType == StepType.SFC_Split ||
                    (step.StepType != StepType.SFC_Split && nextStep.StepType == StepType.SFC_Split))
                {
                    ClearRowSelection();
                    BindWorkList(area, session);
                }

                SetActiveButton(nextStep.StepType, area, session.Mode);
            }

            // El banner de guardado confirma una respuesta correcta; no se muestra en FailedForced.
            bool accepted = outcome == StepSubmitOutcome.Correct;

            if (step.StepType == StepType.ResourceSetup)
            {
                // Con el avance forzado, la BLL ya dejó la orden por defecto.
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
                ShowStartedOrder(area, session);
                ShowBanner("Start process completed successfully");
            }
            else if (step.StepType == StepType.Assembly)
            {
                // La Component List reemplazaba el POD: se vuelve a pintar con la selección.
                // Mensaje del MES real al terminar el BOM (Fase 6.5).
                BindWorkList(area, session);
                ShowBanner("All components processed at this operation");
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
        // componentes la Component List se actualiza y el modal queda abierto con la
        // confirmación del Add (Fase 6.5); con el último, el paso se resuelve y vuelve el POD.
        protected void HandleComponentAssembled(object sender, ComponentAssembledEventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            // Mismo guard de sender que HandleControlSubmit (spec 006, E12).
            if (!ReferenceEquals(sender, GetStepControl(step.StepType))) return;

            var result = Pod.AssembleComponent(_ctx, e.ComponentId, GetAssemblyProgress());
            if (result.StepResult == null)
            {
                CtrlAssembly.ViewModel = BuildStepViewModel(step, area, session, guide);
                CtrlAssembly.BindComponents();
                // La confirmación solo si la BLL aceptó el componente (pertenece al BOM y el
                // paso está activo); si no, un aviso para que no parezca un click perdido.
                if (result.ComponentAssembled)
                    CtrlAssembly.ShowAddConfirmation(e.ComponentId);
                else
                    ShowSweetAlert("warning", "Componente no válido",
                        "El componente no pertenece a la Component List de esta orden.");
                return;
            }

            ClearAssemblyProgress();
            ApplySubmitResult(result.StepResult, session, guide, area, step, incorrectFeedback: null);
            ContinueAfterStepResolved(session, guide, area, step, result.StepResult.Outcome);
        }

        // Avance de Assembly en el estado del POD (Session, servidor): sobrevive a Return to
        // POD y a F5.
        private AssemblyProgress GetAssemblyProgress()
            => _ctx.State.Assembly ?? (_ctx.State.Assembly = new AssemblyProgress());

        private void ClearAssemblyProgress()
        {
            _ctx.State.Assembly = null;
        }

        // Spec 007: el scrap elimina la fila de la orden simulada, tanto en Correct como en
        // FailedForced. Sin fila válida (force-advance por el gate de LogNC) no se toca nada.
        // Spec 008: en Serialized se scrapean todas las filas seleccionadas de una vez.
        private void ScrapSelectedRows(Area area, TrainingSession session)
        {
            var rows = FindScrappableRows(area);
            if (rows.Count == 0) return;

            var order = Order;
            order.Remove(rows);

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
        private IList<SimulatedOrderRow> FindScrappableRows(Area area)
        {
            var selected = FindSelectedOrderRows();

            if (area.SerializationType == SerializationType.Serialized)
                return PieceSelectionRule.Evaluate(StepType.LogNC, selected.Count, ActiveRowCount())
                       == PieceSelectionResult.Valid
                    ? selected
                    : new List<SimulatedOrderRow>();

            return selected.Count == 1 && selected[0].IsFromSplit
                ? selected
                : new List<SimulatedOrderRow>();
        }

        private int ActiveRowCount() => Order?.ActiveRows().Count ?? 0;

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
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            var result = Pod.Split(_ctx, e.RowId, e.Qty);
            if (result.Notice != PodNotice.None)
            {
                // El modal del split muestra el texto en su propio mensaje de error.
                e.ErrorMessage = NoticeText(result.Notice).Text;
                return;
            }

            e.IsApplied = true;
            e.RemainingQty = result.RemainingQty;
            e.NewSfcCode = result.NewSfcCode;
            BindWorkList(area, session);
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
            var result = Pod.SubmitStep(_ctx, answer);
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
                    HandleCorrectSubmit(session, guide, area, step.StepType);
                    break;

                case StepSubmitOutcome.SessionCompleted:
                    // PodService ya dejó en la sesión el puntaje final guardado.
                    ShowCompletionPanel(session, area, guide);
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

            var result = Pod.RegisterOutOfSequence(_ctx, clicked);
            if (result == null) return true;

            ApplySubmitResult(result, session, guide, area, step, incorrectFeedback: null);
            if (result.Outcome == StepSubmitOutcome.FailedForced)
                RestorePodAfterForcedAdvance(step.StepType, session, area);
            return true;
        }

        // Un paso forzado por errores de secuencia nunca abrió su control: la BLL deja el POD
        // como si el paso se hubiera hecho (orden por defecto, piezas iniciadas, split por
        // defecto) y la página lo vuelve a pintar.
        private void RestorePodAfterForcedAdvance(StepType forcedStep, TrainingSession session, Area area)
        {
            bool orderChanged = Pod.RestoreAfterForcedAdvance(_ctx, forcedStep);

            if (forcedStep == StepType.ResourceSetup)
            {
                BindBrowseList(area, session);
            }
            else if (forcedStep == StepType.Search)
            {
                ShowStartedOrder(area, session);
            }
            else if (forcedStep == StepType.SFC_Split && orderChanged)
            {
                ClearRowSelection();
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
            if (session.Status == SessionStatus.Completed)
            {
                ShowCompletionPanel(session, area, guide);
                return;
            }

            var nextStep = guide.Steps.FirstOrDefault(s => s.Order == session.CurrentStepOrder);
            if (nextStep == null) return;

            SetActiveButton(nextStep.StepType, area, session.Mode);
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

            var result = Pod.ClickStart(_ctx, BrowseSelectedIds(), ScannedSfc());
            if (result.Notice != PodNotice.None)
            {
                ShowNotice(result.Notice);
                return;
            }

            // Checklist (Moldeo): SearchControl abre su propio Modal 1 desde Initialize()
            // (secuencia de 4 modales); el Start se completa en HandleControlSubmit.
            if (result.OpenChecklist)
            {
                OpenStepControl(StepType.Search, area, session, guide, modalId: null);
                return;
            }

            // Start directo (spec 008): sin modales, se califica solo por secuencia.
            ApplySubmitResult(result.Step, session, guide, area, step, incorrectFeedback: null);
            if (result.Step.Outcome == StepSubmitOutcome.Incorrect) return;
            ShowStartedOrder(area, session);
            ShowBanner("Start process completed successfully");
        }

        private const string AllPiecesTitle   = "Selecciona todas las piezas";
        private const string AllPiecesMessage =
            "En el simulador se trabajan todas las piezas juntas: selecciona todas las filas.";

        // SFC capturado para el Start de NonSerialized: escaneado en TxtSFC o elegido en Browse.
        private string ScannedSfc()
        {
            string sfcValue = TxtSFC.Text.Trim();
            return string.IsNullOrEmpty(sfcValue) ? HfSelectedSFC.Value.Trim() : sfcValue;
        }

        // Start completado (directo, con checklist o forzado): la BLL ya pasó las piezas a
        // Active y el POD las carga. Serialized: quedan seleccionadas, como en el POD real.
        // NonSerialized: el SFC del lote queda fijo en el header, tomado de la orden (servidor).
        private void ShowStartedOrder(Area area, TrainingSession session)
        {
            ClearRowSelection();

            var order = Order;
            if (order != null)
            {
                if (area.SerializationType == SerializationType.Serialized)
                {
                    var active = order.ActiveRows();
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
                int active = ActiveRowCount();
                var result = PieceSelectionRule.Evaluate(stepType, FindSelectedOrderRows().Count, active);
                if (result == PieceSelectionResult.Valid) return true;

                ShowSweetAlert("warning", AllPiecesTitle, AllPiecesMessage);
                return false;
            }

            if (FindSelectedOrderRow() != null) return true;

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
            if (GetDataCollectionProgress() == null)
                StartDataCollectionProgress();

            OpenStepControl(StepType.DataCollection, area, session, guide, modalId: null);
        }

        // Data Collection (spec 008): Save de un DC Group para la pieza actual.
        protected void HandleGroupSubmit(object sender, DataCollectionGroupSubmitEventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            // Mismo guard de sender que HandleControlSubmit (spec 006, E12).
            if (!ReferenceEquals(sender, GetStepControl(step.StepType))) return;

            var progress = GetDataCollectionProgress() ?? StartDataCollectionProgress();
            string pieceSfc = progress.CurrentPiece?.SFCCode;

            var result = Pod.SubmitDataCollectionGroup(_ctx, e.GroupCode, e.AnswersJson, progress);
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
                    ClearDataCollectionProgress();
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

        // Avance de DC en el estado del POD (Session, servidor): sobrevive a Return to POD y a F5.
        private DataCollectionProgress GetDataCollectionProgress() => _ctx.State.DataCollection;

        // Las piezas son las filas seleccionadas del POD, en su orden (NonSerialized: el lote).
        private DataCollectionProgress StartDataCollectionProgress()
        {
            var progress = new DataCollectionProgress();
            foreach (var row in FindSelectedOrderRows())
                progress.Pieces.Add(new DataCollectionPiece { RowId = row.RowId, SFCCode = row.SFCCode });

            _ctx.State.DataCollection = progress;
            return progress;
        }

        private void ClearDataCollectionProgress()
        {
            _ctx.State.DataCollection = null;
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

            var notice = Pod.OpenSplit(_ctx, SelectedIds());
            if (notice != PodNotice.None)
            {
                ClearRowSelection();
                BindWorkList(area, session);
                ShowNotice(notice);
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
                PieceSelectionRule.Evaluate(StepType.LogNC, FindSelectedOrderRows().Count,
                    ActiveRowCount()) == PieceSelectionResult.NoPieceLeft)
            {
                ShowSweetAlert("warning", "Selección inválida", "Debe quedar al menos una pieza para Complete.");
                return;
            }

            // Spec 007: abrir Log NC sin una fila scrapeable es un intento fallido del paso.
            // No hay gate en el cliente: el click tiene que llegar acá para registrarse.
            if (FindScrappableRows(area).Count == 0)
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
                    FindSelectedOrderRows().Count, ActiveRowCount());
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
            else if (FindSelectedOrderRow() == null)
            {
                // Spec 007: Complete sin fila seleccionada es un intento fallido del paso.
                ProcessSubmit(session, guide, area, step, StepAnswers.Invalid, "Select 1 row to complete");
                return;
            }

            ProcessSubmit(session, guide, area, step, StepAnswers.Complete);
        }

        protected void BtnAbandon_Click(object sender, EventArgs e)
        {
            if (_ctx == null) return;
            // Abandona la sesión y borra su estado del POD. Al completar, el estado queda en la
            // Session por diseño (Deferred Decisions de la spec 005).
            Pod.Abandon(_ctx);
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

            var products = _guideService.GetAllProducts();
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
            var vm = new StepViewModel();

            // Spec 005: BuildSimulatedSFCCode ya no existe — el SFC real vive en la
            // SimulatedOrder de sesión. Antes de Resource Setup no hay orden aún,
            // así que se usa un placeholder en vez de fallar.
            // NonSerialized: el SFC del lote. Serialized: el SFC o "[N SELECTED]" de la selección
            // (ej. NC Data Entry de varias piezas, spec 008).
            string sfcCode = Order?.SFCCode;
            if (string.IsNullOrEmpty(sfcCode))
                sfcCode = DescribeSelection(FindSelectedOrderRows());
            if (string.IsNullOrEmpty(sfcCode))
                sfcCode = "—";
            string resourceName = PodDefaults.ResourceName(area);
            string opName     = guide.Title;

            vm.ExtraData["SFCCode"]      = sfcCode;
            vm.ExtraData["ResourceName"] = resourceName;
            vm.ExtraData["OperationName"] = opName;
            vm.ExtraData["Site"]         = PodDefaults.Site;

            switch (step.StepType)
            {
                case StepType.LineClearance:
                    vm.ExtraData["QuestionText"] = PodDefaults.LineClearanceQuestion;
                    break;

                case StepType.DataCollection:
                    var dcParams = _guideService.GetDataCollectionParameters(area.Id);
                    vm.ExtraData["Parameters"] = dcParams;
                    vm.ExtraData["Groups"]     = _guideService.GetDataCollectionGroups(area.Id);
                    var dcProgress = GetDataCollectionProgress();
                    vm.ExtraData["PieceLabel"] = DescribeCurrentPiece(dcProgress);
                    // Grupos ya guardados por la pieza actual: su Collect se deshabilita.
                    vm.ExtraData["SavedGroupIds"] = dcProgress?.CurrentPieceSavedGroupIds ?? new HashSet<int>();
                    break;

                case StepType.SFC_Split:
                    var selectedRow = FindSelectedOrderRow();
                    // "SFC to Split" es el SFC de la fila elegida: una fila que ya salió de otro
                    // split tiene su propio SFC, distinto del lote.
                    if (selectedRow?.SFCCode != null)
                        vm.ExtraData["SFCCode"] = selectedRow.SFCCode;
                    vm.ExtraData["AvailableQty"] = selectedRow?.Qty ?? 0;
                    vm.ExtraData["SelectedRowId"] = selectedRow?.RowId;
                    vm.ExtraData["HasSuccessfulSplit"] = Order != null && Order.HasSplit;
                    break;

                case StepType.LogNC:
                    vm.ExtraData["NCCodes"] = _guideService.GetNCCodes(area.Id);
                    vm.ExtraData["RequiresNCComponent"] = area.RequiresNCComponent;
                    // "SFC (1/N)" y "Apply to all SFCs" de NC Data Entry (Fase 6.5, entrega 8).
                    var ncPieces = FindSelectedOrderRows();
                    vm.ExtraData["SelectedPieceCount"] = ncPieces.Count;
                    vm.ExtraData["FirstSelectedSFC"]   = ncPieces.Count > 0
                        ? ncPieces[0].SFCCode ?? Order?.SFCCode
                        : null;
                    break;

                case StepType.Assembly:
                    // BOM del área y componentes ya ensamblados (spec 008).
                    vm.ExtraData["Components"]         = _guideService.GetAssemblyComponents(area.Id);
                    vm.ExtraData["AssembledIds"]       = GetAssemblyProgress().AssembledComponentIds;
                    vm.ExtraData["SelectedPieceCount"] = FindSelectedOrderRows().Count;
                    vm.ExtraData["ProcessLot"]         = Order?.ProcessLot;
                    break;

                case StepType.Search:
                    // SearchControl owns its own 4-modal flow (labels hardcoded in the .ascx);
                    // it only needs ResourceName, already populated above.
                    break;
            }

            return vm;
        }

        // ── Session validation ───────────────────────────────────────────
        private PodContext LoadContext()
        {
            // Re-hidrata sesión si se perdió pero el usuario sigue autenticado
            if (!SessionHelper.EnsureOperatorSession())
            {
                Response.Redirect("~/Pages/Auth/Login.aspx");
                return null;
            }

            // Primer acceso sin contraseña personal: no puede entrenar (auditoría AUD-08).
            if (SessionHelper.MustChangePassword())
            {
                Response.Redirect("~/Pages/Auth/ChangePassword.aspx");
                return null;
            }

            if (!int.TryParse(Request.QueryString["sessionId"], out int sessionId))
            {
                Response.Redirect("~/Pages/Training/StartSession.aspx");
                return null;
            }

            // null: la sesión no existe, es de otro operador o ya no está en curso.
            var ctx = Pod.LoadContext(sessionId, CurrentOperatorId);
            if (ctx == null)
            {
                Response.Redirect("~/Pages/Training/StartSession.aspx");
                return null;
            }

            return ctx;
        }

        // Operador autenticado (Session escrita solo por el servidor tras el login; ya
        // re-hidratada por EnsureOperatorSession). La BLL verifica con él que la sesión le
        // pertenece (auditoría AUD-05).
        private int CurrentOperatorId => (int)Session[Constants.SessionKeys.OperatorId];

        // ── UI helpers ───────────────────────────────────────────────────
        private void PopulateHeader(TrainingSession session, Area area, TrainingGuide guide)
        {
            // El MES real muestra el Code1 del empleado, no su nombre (Fase 6.5).
            LitUserName.Text      = HttpUtility.HtmlEncode(
                Session[Constants.SessionKeys.Code1]?.ToString() ?? "");
            LitOperationName.Text = HttpUtility.HtmlEncode(guide.Title);
            LitResourceName.Text  = HttpUtility.HtmlEncode(PodDefaults.ResourceName(area));
            LitBrowseContext.Text = string.Format("{0}, {1}, {2}",
                area.Name, PodDefaults.ResourceName(area),
                Session[Constants.SessionKeys.Code1]?.ToString() ?? "");
        }

        // El POD solo muestra las filas iniciadas (Active). En NonSerialized el lote ya
        // nace Active; en Serialized cada pieza pasa a Active con el Start (spec 008).
        private void BindWorkList(Area area, TrainingSession session)
        {
            var rows = BuildPodWorkList(onlyActive: true);
            RptWorkList.DataSource = rows;
            RptWorkList.DataBind();
        }

        // Browse Work List muestra todas las filas de la orden (sin columna Status).
        private void BindBrowseList(Area area, TrainingSession session)
        {
            var rows = BuildPodWorkList(onlyActive: false);
            RptBrowseList.DataSource = rows;
            RptBrowseList.DataBind();
        }

        // Filas del POD que el operador seleccionó (HfSelectedRowIds, llenado en cliente por
        // selectWorkListRow). El cliente puede mandar cualquier cosa: solo cuentan los ids que
        // existen en la orden y están Active (spec 008).
        private IList<SimulatedOrderRow> FindSelectedOrderRows()
        {
            var order = Order;
            if (order == null) return new List<SimulatedOrderRow>();
            return order.FindActiveRows(ParseRowIds(HfSelectedRowIds.Value));
        }

        // Pasos de una sola fila (NonSerialized: DC, SFC Split, Log NC, Complete): la fila
        // seleccionada solo si hay exactamente una.
        private SimulatedOrderRow FindSelectedOrderRow()
        {
            var rows = FindSelectedOrderRows();
            return rows.Count == 1 ? rows[0] : null;
        }

        // Piezas elegidas en Browse Work List para el Start (HfBrowseRowIds).
        private IList<SimulatedOrderRow> FindBrowseSelectedRows(SimulatedOrder order)
        {
            return order.FindRows(ParseRowIds(HfBrowseRowIds.Value));
        }

        // Ids de filas que manda el cliente; la BLL descarta los que no son de la orden.
        private HashSet<string> SelectedIds() => ParseRowIds(HfSelectedRowIds.Value);
        private HashSet<string> BrowseSelectedIds() => ParseRowIds(HfBrowseRowIds.Value);

        private static HashSet<string> ParseRowIds(string value)
        {
            return new HashSet<string>((value ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim()));
        }

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

        private IList<PodWorkListRow> BuildPodWorkList(bool onlyActive)
        {
            var order = Order;
            if (order == null) return new List<PodWorkListRow>();

            return order.Rows.Where(r => !onlyActive || r.IsActive).Select(r => new PodWorkListRow
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
            // Puntaje del contexto: el de la carga o el del último intento registrado (AUD-25).
            LitScore.Text = FormatScore(_ctx.Score);

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
        // Lo que cargó Page_Load en este request; vacío si redirigió.
        private (TrainingSession, TrainingGuide, Area, TrainingStep) GetContext()
        {
            if (_ctx == null) return (null, null, null, null);
            return (_ctx.Session, _ctx.Guide, _ctx.Area, _ctx.CurrentStep);
        }

        // ── Modal re-open & SweetAlert helpers ───────────────────────────

        // Avisos de formato que decide la BLL (no cuentan como intento).
        private void ShowNotice(PodNotice notice)
        {
            var (title, text) = NoticeText(notice);
            ShowSweetAlert("warning", title, text);
        }

        private static (string Title, string Text) NoticeText(PodNotice notice)
        {
            switch (notice)
            {
                case PodNotice.ResourceSetupRequired:
                    return ("Resource Setup requerido", "Debes completar Resource Setup antes de iniciar.");
                case PodNotice.SelectAllPieces:
                    return (AllPiecesTitle, AllPiecesMessage);
                case PodNotice.SfcRequired:
                    return ("Campo requerido", "Debes ingresar o escanear un SFC antes de iniciar.");
                case PodNotice.SelectRowToSplit:
                    return ("Selecciona una fila para dividir",
                        "Debes seleccionar una fila válida de la orden antes de iniciar SFC Split.");
                case PodNotice.SplitRequired:
                    return ("Split requerido", "Debes completar al menos un split antes de cerrar el paso.");
                case PodNotice.SplitNotActive:
                    return ("SFC Split", "SFC Split no es el paso activo.");
                case PodNotice.InvalidSplit:
                    return ("SFC Split", "La fila o cantidad seleccionada ya no es válida.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(notice), notice, null);
            }
        }

        protected void ShowSweetAlert(string type, string title, string text)
        {
            string script = string.Format(
                "Swal.fire({{title:'{0}',text:'{1}',icon:'{2}'}});",
                HttpUtility.JavaScriptStringEncode(title ?? string.Empty),
                HttpUtility.JavaScriptStringEncode(text ?? string.Empty), type);
            ClientScript.RegisterStartupScript(GetType(), "swal_" + type, script, true);
        }

        protected void ReopenModal(string modalId)
        {
            string script = string.Format("showModalSafe('{0}');", modalId);
            ClientScript.RegisterStartupScript(GetType(), "modal_" + modalId, script, true);
        }

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
