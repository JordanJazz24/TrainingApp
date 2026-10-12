using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Guide;
using TrainingApp.Pages.Training.Models;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Training
{
    public partial class GuidedTest : Page
    {
        // Motor del POD (spec 009). Se crea al primer uso: necesita la Session del request.
        private PodService _pod;
        private PodService Pod => _pod ?? (_pod = new PodService(new SessionPodStateStore(Session)));

        // Contexto del request (spec 009, AUD-25): sesión, guía, área, intentos y estado del
        // POD, cargados una vez en Page_Load. Los handlers de eventos corren después y lo usan.
        private PodContext _ctx;

        // Orden simulada de la sesión. Sin orden (antes de Resource Setup) el POD y el Browse
        // Work List quedan vacíos (spec 005).
        private SimulatedOrder Order => _ctx?.State.Order;

        // Guía visual de Guided (spec 010, P-2): qué dejó la página en pantalla en este request.
        // Lo anotan los handlers; OnPreRender arma el guion con la entrada que corresponde.
        private GuideMoment _guideMoment = GuideMoment.Load;


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
            var step = _ctx.CurrentStep;
            var ctrl = GetStepControl(stepType);
            ctrl.ViewModel = BuildStepViewModel(step, area, session, guide);
            ctrl.Initialize();

            OpenStepType = stepType;
            ApplyStepControlVisibility(stepType);
            _guideMoment = GuideMoment.Opened(step);

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

            // El logout no depende de la sesión de entrenamiento (auditoría AUD-33): se atiende
            // antes de cargar el contexto, que redirige a StartSession si la guía ya se
            // completó o se abandonó (panel final, pestaña vieja tras D-9).
            if (IsPostBack && IsPostBackFrom(BtnLogout))
            {
                SessionHelper.Logout();
                return;
            }

            _ctx = LoadContext();
            if (_ctx == null) return;

            // P-1 (spec 009): la Session se perdió y la BLL reconstruyó el estado del POD. El
            // click de este postback apuntaba a filas de la orden perdida: no se ejecuta (ni la
            // página ni los controles), para no contar como intento algo que el operador no
            // hizo. Se recarga la página como un F5 y el operador repite el click. Abandon no
            // usa filas de la orden y sí se ejecuta (auditoría AUD-34).
            if (IsPostBack && _ctx.Rebuilt && !IsPostBackFrom(BtnAbandon))
            {
                Response.Redirect(Request.RawUrl);
                return;
            }

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
                if (_ctx.IsResolved(StepType.Search))
                {
                    var order = Order;
                    if (order != null) TxtSFC.Text = order.SFCCode;
                    BindWorkList(area, session);
                }
            }

            // Corre antes del Page_Load de los controles: cada uno ya ve su Visible
            // definitivo y el abierto tiene su ViewModel. Solo se arma el ViewModel del
            // control abierto — BuildStepViewModel lee los catálogos del área (del
            // contexto, una vez por request: AUD-39).
            var open = OpenStepType;
            ApplyStepControlVisibility(open);
            if (open.HasValue)
                GetStepControl(open.Value).ViewModel = BuildStepViewModel(step, area, session, guide);

            UpdateScoreWidget(session);
            SetActiveButton(step.StepType, area, session.Mode);
            BindSearchHeader();
        }

        // Corre después de los handlers: el momento del request ya está anotado.
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            RegisterGuide();
        }

        // Guía visual (spec 010): solo en Guided. Assessment no recibe el motor, el guion, los
        // textos ni el estado (AC-2). Pasos sin guion todavía: la guía no se muestra.
        private void RegisterGuide()
        {
            // Visible persiste en ViewState: se decide en cada request.
            PhGuideAssets.Visible = false;
            if (_ctx == null || _ctx.Session.Mode != SessionMode.Guided) return;

            var script = new GuideScriptBuilder().Build(_ctx, _guideMoment);
            if (script.IsEmpty) return;

            PhGuideAssets.Visible = true;
            // JavaScriptSerializer escapa < > & ' : el HTML de los textos no puede cerrar el <script>.
            ClientScript.RegisterStartupScript(GetType(), "podGuide",
                "PodGuide.start(" + new JavaScriptSerializer().Serialize(script.ToClient()) + ");", true);
        }

        // POD Search: Shop Order y Status (SFC) de la orden cargada. Antes del Start, las
        // piezas elegidas en Browse Work List (In Queue); con el Start, Active. Se recalcula
        // en cada request porque el OK de Browse solo lo adelanta en el cliente.
        private void BindSearchHeader()
        {
            var order = Order;
            if (order != null && _ctx.IsResolved(StepType.Search))
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
            IList<SimulatedOrderRow> scrapped = null;
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

                case StepType.LogNC:
                    // Resuelto el paso, la BLL scrapea las filas seleccionadas (spec 007).
                    var logNC = Pod.SubmitLogNC(_ctx, e.Answer, SelectedIds());
                    result   = logNC.Step;
                    scrapped = logNC.Scrapped;
                    break;

                default:
                    result = Pod.SubmitStep(_ctx, e.Answer);
                    break;
            }

            ApplySubmitResult(result, session, guide, area, step, e.IncorrectFeedback);
            var outcome = result.Outcome;

            // Spec 006 (E3): respuesta incorrecta → el control queda abierto en el modal
            // donde se respondió y solo se ve el SweetAlert con el hint (de ApplySubmitResult).
            if (outcome == StepSubmitOutcome.Incorrect)
            {
                // Search tiene 4 modales: el control sabe cuál contiene la respuesta a corregir.
                if (step.StepType == StepType.Search)
                {
                    _guideMoment = GuideMoment.Incorrect(step, CtrlSearch.ReopenForRetry());
                    return;
                }

                string answerModalId = GetAnswerModalId(step.StepType);
                if (answerModalId != null) ReopenModal(answerModalId);
                return;
            }

            ContinueAfterStepResolved(session, guide, area, step, outcome, scrapped);
        }

        // Paso resuelto (Correct, FailedForced o sesión completada) desde un control: cierra el
        // control, habilita el botón del paso siguiente y aplica lo propio de cada paso. Lo
        // usan el submit de los controles y el ensamble del último componente (spec 008).
        // scrapped: filas que la BLL sacó de la orden al resolver Log NC.
        private void ContinueAfterStepResolved(TrainingSession session, TrainingGuide guide,
            Area area, TrainingStep step, StepSubmitOutcome outcome,
            IList<SimulatedOrderRow> scrapped = null)
        {
            // Sesión completada: ShowCompletionPanel ya cerró todos los controles. La sesión ya
            // refleja el intento registrado (PodService actualiza el contexto).
            if (session.Status != SessionStatus.InProgress) return;

            // Modal-overlay: se renderiza en esta respuesta para mostrar su confirmación y
            // queda cerrado para el request siguiente. El resto vuelve al POD de inmediato.
            CloseStepControl(IsModalOverlay(step.StepType) ? step.StepType : (StepType?)null);

            if (ClearSelectionAroundSplit(step.StepType))
                BindWorkList(area, session);

            var nextStep = _ctx.CurrentStepOrDefault;
            if (nextStep != null)
                SetActiveButton(nextStep.StepType, area, session.Mode);

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
                // La Component List reemplazaba el POD: vuelve sin selección, como el real
                // (2026-10-07). Mensaje del MES real al terminar el BOM (Fase 6.5).
                ClearPodSelection(area);
                BindWorkList(area, session);
                ShowBanner("All components processed at this operation");
            }
            else if (step.StepType == StepType.DataCollection)
            {
                // La Data Collection List reemplazaba el POD: vuelve sin selección (2026-10-07).
                ClearPodSelection(area);
                BindWorkList(area, session);
            }
            else if (step.StepType == StepType.SFC_Split)
                ShowBanner("SFC Split completed successfully");
            else if (step.StepType == StepType.LogNC)
                ShowScrapped(area, session, scrapped);
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

            var result = Pod.AssembleComponent(_ctx, e.ComponentId);
            if (result.StepResult == null)
            {
                CtrlAssembly.ViewModel = BuildStepViewModel(step, area, session, guide);
                CtrlAssembly.BindComponents();
                // La confirmación solo si la BLL aceptó el componente (pertenece al BOM y el
                // paso está activo); si no, un aviso para que no parezca un click perdido.
                if (result.ComponentAssembled)
                {
                    CtrlAssembly.ShowAddConfirmation(e.ComponentId);
                    // Spec 010: la guía entra en la confirmación de este componente.
                    _guideMoment = GuideMoment.Progress(step, e.ComponentId.ToString(CultureInfo.InvariantCulture));
                }
                else
                    ShowSweetAlert("warning", "Componente no válido",
                        "El componente no pertenece a la Component List de esta orden.");
                return;
            }

            ApplySubmitResult(result.StepResult, session, guide, area, step, incorrectFeedback: null);
            ContinueAfterStepResolved(session, guide, area, step, result.StepResult.Outcome);
        }

        // Spec 007: el scrap elimina la fila de la orden simulada, tanto en Correct como en
        // FailedForced (lo hace la BLL). Sin fila válida no se tocó nada. Spec 008: en
        // Serialized se scrapean todas las filas seleccionadas de una vez.
        private void ShowScrapped(Area area, TrainingSession session, IList<SimulatedOrderRow> rows)
        {
            if (rows == null || rows.Count == 0) return;

            string scrapped = rows.Count == 1
                ? "All of " + (rows[0].SFCCode ?? Order.SFCCode)
                : rows.Count + " SFCs";

            ClearRowSelection();
            // NonSerialized: TxtSFC es el SFC del lote y no cambia (spec 007).
            if (area.SerializationType == SerializationType.Serialized)
                TxtSFC.Text = string.Empty;
            BindWorkList(area, session);
            ShowBanner(scrapped + " scrapped", hideAfterMs: 5000);
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
            // Spec 010: la guía entra en el mensaje "New SFC … created".
            _guideMoment = GuideMoment.Progress(step, "split");
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

        private void ApplySubmitResult(StepSubmitResult result, TrainingSession session, TrainingGuide guide,
            Area area, TrainingStep step, string incorrectFeedback)
        {
            UpdateScoreWidget(session);

            switch (result.Outcome)
            {
                case StepSubmitOutcome.Correct:
                    _guideMoment = GuideMoment.StepResolved(step);
                    HandleCorrectSubmit(session, guide, area, step.StepType);
                    break;

                case StepSubmitOutcome.Incorrect:
                    _guideMoment = GuideMoment.Incorrect(step);
                    ShowSweetAlert("warning", "Incorrect",
                        BuildIncorrectFeedback(session.Mode, step.StepType, area, result.RemainingScoredAttempts, incorrectFeedback));
                    break;

                case StepSubmitOutcome.FailedForced:
                    _guideMoment = GuideMoment.StepResolved(step);
                    ShowSweetAlert("error", "Step Failed",
                        "Paso fallido — el sistema avanza automáticamente.");
                    HandleCorrectSubmit(session, guide, area, step.StepType);
                    break;

                case StepSubmitOutcome.SessionCompleted:
                    _guideMoment = GuideMoment.SessionCompleted(step);
                    // PodService ya dejó en la sesión el puntaje final guardado.
                    ShowCompletionPanel(session, area, guide);
                    break;
            }
        }

        // Assessment (S2): todos los botones del POD están habilitados y un click en uno que
        // no es el del paso actual es un intento fallido de ese paso (lo decide la BLL antes que
        // cualquier aviso de formato). Devuelve true si el click fue fuera de secuencia; en
        // Guided esos botones están deshabilitados y el click se ignora.
        private bool HandleOutOfSequence(PodCommandResult result, TrainingSession session,
            TrainingGuide guide, Area area, TrainingStep step)
        {
            if (!result.OutOfSequence) return false;
            if (result.Step == null) return true;

            ApplySubmitResult(result.Step, session, guide, area, step, incorrectFeedback: null);
            if (result.Step.Outcome == StepSubmitOutcome.FailedForced)
                RepaintAfterForcedAdvance(step.StepType, session, guide, area, result.PodChanged);
            return true;
        }

        // Paso forzado por errores de secuencia: la BLL ya dejó el POD como si el paso se
        // hubiera hecho (orden por defecto, piezas iniciadas, split por defecto). La página lo
        // vuelve a pintar como en el submit normal (B-1): cierra el control que seguía abierto
        // (Component List o Data Collection List) y, si el paso forzado o el siguiente es SFC
        // Split, limpia la selección.
        private void RepaintAfterForcedAdvance(StepType forcedStep, TrainingSession session,
            TrainingGuide guide, Area area, bool podChanged)
        {
            bool rebind = false;
            if (OpenStepType.HasValue)
            {
                CloseStepControl();
                rebind = true;
            }

            if (podChanged && forcedStep == StepType.ResourceSetup)
                BindBrowseList(area, session);

            if (ClearSelectionAroundSplit(forcedStep))
                rebind = true;

            // Como en el submit normal: tras Assembly y DC el POD vuelve sin selección.
            if (forcedStep == StepType.Assembly || forcedStep == StepType.DataCollection)
            {
                ClearPodSelection(area);
                rebind = true;
            }

            // ShowStartedOrder ya vuelve a pintar el POD.
            if (podChanged && forcedStep == StepType.Search)
                ShowStartedOrder(area, session);
            else if (rebind)
                BindWorkList(area, session);
        }

        // Si el paso resuelto o el siguiente es SFC Split, se limpia la selección del POD: el
        // split pide elegir la fila de nuevo. Devuelve true si hay que volver a pintar el POD.
        private bool ClearSelectionAroundSplit(StepType resolvedStep)
        {
            var nextStep = _ctx.CurrentStepOrDefault;
            if (resolvedStep != StepType.SFC_Split && nextStep?.StepType != StepType.SFC_Split)
                return false;

            ClearRowSelection();
            return true;
        }

        // Texto del intento fallido que decidió la BLL sin pasar por el validador (Guided lo
        // muestra en lugar del hint; Assessment muestra los intentos restantes).
        private static string AttemptFeedback(PodAttemptReason reason, Area area)
        {
            bool serialized = area.SerializationType == SerializationType.Serialized;
            switch (reason)
            {
                // En español y de usted, como los hints de Guided (spec 010, P-6 y P-8).
                case PodAttemptReason.NoScrappableRow:
                    return serialized
                        ? "Seleccione una fila para scrapear"
                        : "Seleccione una fila creada por el split para scrapear";
                case PodAttemptReason.NoRowToComplete:
                    return serialized ? "Seleccione todas las filas para completar" : "Seleccione 1 fila para completar";
                default:
                    return null;
            }
        }

        // Guided: hint del paso para que el operador aprenda. Assessment: no se revela la
        // respuesta, solo cuántos intentos con puntaje le quedan antes del avance forzado.
        private static string BuildIncorrectFeedback(SessionMode mode, StepType stepType, Area area,
            int remainingScoredAttempts, string incorrectFeedback)
        {
            if (mode == SessionMode.Assessment)
            {
                return remainingScoredAttempts > 0
                    ? string.Format("Respuesta incorrecta. Te quedan {0} intento(s) con puntaje.", remainingScoredAttempts)
                    : "Respuesta incorrecta. Si vuelves a fallar, el paso se marca fallido y avanza.";
            }

            if (!string.IsNullOrEmpty(incorrectFeedback)) return incorrectFeedback;
            string hint = GetHint(stepType, area);
            return !string.IsNullOrEmpty(hint) ? hint : "Intente de nuevo.";
        }

        private void HandleCorrectSubmit(TrainingSession session, TrainingGuide guide,
            Area area, StepType submittedStepType)
        {
            if (session.Status == SessionStatus.Completed)
            {
                ShowCompletionPanel(session, area, guide);
                return;
            }

            var nextStep = _ctx.CurrentStepOrDefault;
            if (nextStep == null) return;

            SetActiveButton(nextStep.StepType, area, session.Mode);
        }

        // ── Button ServerClick handlers ───────────────────────────────────

        protected void BtnResourceSetup_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(Pod.OpenResourceSetup(_ctx), session, guide, area, step)) return;
            OpenStepControl(StepType.ResourceSetup, area, session, guide, "modalResourceSetup");
        }

        protected void BtnActivities_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(Pod.OpenLineClearance(_ctx), session, guide, area, step)) return;
            // LineClearance se abre como "Data Collection Standalone" desde Activities.
            OpenStepControl(StepType.LineClearance, area, session, guide, "modalDCStandalone");
        }

        protected void BtnStart_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            var result = Pod.ClickStart(_ctx, BrowseSelectedIds(), ScannedSfc());
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
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

        // SFC capturado para el Start de NonSerialized: escaneado en TxtSFC o elegido en Browse.
        private string ScannedSfc()
        {
            string sfcValue = TxtSFC.Text.Trim();
            return string.IsNullOrEmpty(sfcValue) ? HfSelectedSFC.Value.Trim() : sfcValue;
        }

        // Start completado (directo, con checklist o forzado): la BLL ya pasó las piezas a
        // Active y el POD las carga sin seleccionar, como el POD real: el operador las
        // selecciona para la actividad siguiente (corrección del usuario, 2026-10-07).
        // NonSerialized: el SFC del lote queda fijo en el header, tomado de la orden (servidor).
        private void ShowStartedOrder(Area area, TrainingSession session)
        {
            var order = Order;
            ClearPodSelection(area);
            if (order != null)
            {
                if (area.SerializationType == SerializationType.NonSerialized)
                    TxtSFC.Text = order.SFCCode;

                SetSearchHeader(order.ShopOrderInput, SimulatedOrderRow.StatusActive);
            }

            BindWorkList(area, session);
        }

        // El POD vuelve sin selección (tras Start, Assembly y DC, como el real). Serialized: el
        // campo SFC refleja la selección, así que queda vacío. NonSerialized: es el SFC del lote
        // y no cambia (spec 007).
        private void ClearPodSelection(Area area)
        {
            ClearRowSelection();
            if (area.SerializationType == SerializationType.Serialized)
                TxtSFC.Text = string.Empty;
        }

        protected void BtnAssemble_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            var result = Pod.OpenAssembly(_ctx, SelectedIds());
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
            if (result.Notice != PodNotice.None)
            {
                ShowSelectionNotice(result.Notice, area, session);
                return;
            }

            // Abre la Component List; el modal Assemble se abre desde cada componente.
            OpenStepControl(StepType.Assembly, area, session, guide, modalId: null);
        }

        // Aviso de selección al abrir Assembly, DC o SFC Split (lo decide la BLL, sin intento).
        // Si el paso pedía una fila, se limpia la selección del POD. oneRowMessage: texto propio
        // del paso en lugar del genérico.
        private void ShowSelectionNotice(PodNotice notice, Area area, TrainingSession session,
            string oneRowMessage = null)
        {
            if (notice == PodNotice.SelectOneRow || notice == PodNotice.SelectRowToSplit)
            {
                ClearRowSelection();
                BindWorkList(area, session);
            }

            var (title, text) = NoticeText(notice);
            if (notice == PodNotice.SelectOneRow && oneRowMessage != null)
                text = oneRowMessage;
            ShowSweetAlert("warning", title, text);
        }

        protected void BtnDCCollection_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            // Defensa en servidor: el HiddenField puede faltar, estar obsoleto o ser
            // manipulado aunque la validación JavaScript haya sido omitida. La BLL decide la
            // selección y, en la primera apertura, fija las piezas a recorrer (spec 008).
            var result = Pod.OpenDataCollection(_ctx, SelectedIds());
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
            if (result.Notice != PodNotice.None)
            {
                ShowSelectionNotice(result.Notice, area, session,
                    "Debes seleccionar una fila de la orden en la tabla antes de iniciar Data Collection.");
                return;
            }

            OpenStepControl(StepType.DataCollection, area, session, guide, modalId: null);
        }

        // Spec 010: se eligió un código NC y se abrió NC Data Entry; la guía entra en el
        // formulario. Mismo guard de sender que los demás handlers del control.
        protected void HandleNCCodeSelected(object sender, EventArgs e)
        {
            if (_ctx == null || !ReferenceEquals(sender, CtrlLogNC)) return;
            _guideMoment = GuideMoment.Opened(_ctx.CurrentStep, "entry");
        }

        // Spec 010: Collect abrió el Data Collection Entry de un grupo; la guía entra en sus
        // parámetros. Mismo guard de sender que los demás handlers del control.
        protected void HandleGroupOpened(object sender, EventArgs e)
        {
            if (_ctx == null || !ReferenceEquals(sender, CtrlDataCollection)) return;
            _guideMoment = GuideMoment.Opened(_ctx.CurrentStep, CtrlDataCollection.OpenGroupCode);
        }

        // Data Collection (spec 008): Save de un DC Group para la pieza actual.
        protected void HandleGroupSubmit(object sender, DataCollectionGroupSubmitEventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            // Mismo guard de sender que HandleControlSubmit (spec 006, E12).
            if (!ReferenceEquals(sender, GetStepControl(step.StepType))) return;

            // Pieza para la que se guarda el grupo (la guía entra en su banner, spec 010).
            string pieceRowId = GetDataCollectionProgress()?.CurrentPiece?.RowId;
            var result = Pod.SubmitDataCollectionGroup(_ctx, e.GroupCode, e.AnswersJson);
            switch (result.Outcome)
            {
                case DataCollectionOutcome.Incorrect:
                    // El control queda abierto en el modal donde se respondió (spec 006, E3);
                    // grupos y piezas ya guardados se conservan (AC-8).
                    ApplySubmitResult(result.StepResult, session, guide, area, step, incorrectFeedback: null);
                    _guideMoment = GuideMoment.Incorrect(step, e.GroupCode);
                    ReopenModal("modalDCEntry");
                    return;

                case DataCollectionOutcome.GroupSaved:
                    RefreshDataCollectionControl(session, guide, area, step);
                    _guideMoment = GuideMoment.Progress(step, pieceRowId + "|" + e.GroupCode);
                    ShowBanner("All data for DC group " + e.GroupCode + " has been collected", hideAfterMs: 4000);
                    return;

                case DataCollectionOutcome.PieceCompleted:
                    RefreshDataCollectionControl(session, guide, area, step);
                    _guideMoment = GuideMoment.Progress(step, pieceRowId + "|" + e.GroupCode);
                    ShowBanner(string.Format("SFC {0} completed. Next: {1}",
                        result.PieceSfcCode, DescribeCurrentPiece(GetDataCollectionProgress())), hideAfterMs: 5000);
                    return;

                case DataCollectionOutcome.StepResolved:
                    // Paso completo, forzado (E5: la BLL descarta el avance parcial) o sesión terminada.
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

        // Avance de DC (estado del POD, lo maneja la BLL): sobrevive a Return to POD y a F5.
        private DataCollectionProgress GetDataCollectionProgress() => _ctx.State.DataCollection;

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
            var result = Pod.OpenSplit(_ctx, SelectedIds());
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
            if (result.Notice != PodNotice.None)
            {
                ShowSelectionNotice(result.Notice, area, session);
                return;
            }

            OpenStepControl(StepType.SFC_Split, area, session, guide, "modalSFCSplit");
        }

        protected void BtnLogNC_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            // La BLL decide: aviso si en Serialized no queda pieza para Complete; intento
            // fallido si no hay una fila scrapeable (spec 007/008).
            var result = Pod.OpenLogNC(_ctx, SelectedIds());
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
            if (result.Notice != PodNotice.None)
            {
                ShowNotice(result.Notice);
                return;
            }
            if (result.Step != null)
            {
                ApplySubmitResult(result.Step, session, guide, area, step,
                    AttemptFeedback(result.AttemptReason, area));
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
            HandleOutOfSequence(Pod.ClickNonStepAction(_ctx), session, guide, area, step);
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
            // La BLL decide: aviso por selección parcial (Serialized), intento fallido sin la
            // selección del paso (spec 007/008) o el Complete.
            var result = Pod.Complete(_ctx, SelectedIds());
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
            if (result.Notice != PodNotice.None)
            {
                ShowNotice(result.Notice);
                return;
            }

            ApplySubmitResult(result.Step, session, guide, area, step,
                AttemptFeedback(result.AttemptReason, area));
        }

        protected void BtnAbandon_Click(object sender, EventArgs e)
        {
            if (_ctx == null) return;
            // Abandona la sesión y borra su estado del POD. Al completar, el estado queda en la
            // Session por diseño (Deferred Decisions de la spec 005).
            Pod.Abandon(_ctx);
            Response.Redirect("~/Pages/Training/StartSession.aspx");
        }

        // Page_Load atiende el logout antes de cargar el contexto (AUD-33), así que este
        // handler no llega a correr; el botón lo necesita para que ASP.NET le genere el
        // __doPostBack.
        protected void BtnLogout_ServerClick(object sender, EventArgs e)
        {
            SessionHelper.Logout();
        }

        // Los botones HTML del POD (Logout, Abandon) postean con __doPostBack.
        private bool IsPostBackFrom(Control control)
            => Request.Form["__EVENTTARGET"] == control.UniqueID;

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

            LitFinalArea.Text    = Page.Server.HtmlEncode(area.Name);
            LitFinalProduct.Text = Page.Server.HtmlEncode(_ctx.Product?.Name ?? "—");

            if (session.Mode == SessionMode.Assessment)
            {
                PnlFinalResult.Visible = true;
                LitFinalResult.Text    = _ctx.IsPassed
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
                    vm.ExtraData["Parameters"] = _ctx.DataCollectionParameters;
                    vm.ExtraData["Groups"]     = _ctx.DataCollectionGroups;
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
                    vm.ExtraData["NCCodes"] = _ctx.NCCodes;
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
                    vm.ExtraData["Components"]         = _ctx.AssemblyComponents;
                    vm.ExtraData["AssembledIds"]       = _ctx.State.Assembly?.AssembledComponentIds
                                                         ?? new HashSet<int>();
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
            if (!SessionHelper.EnsureUserSession())
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
            var ctx = Pod.LoadContext(sessionId, CurrentUserId);
            if (ctx == null)
            {
                Response.Redirect("~/Pages/Training/StartSession.aspx");
                return null;
            }

            return ctx;
        }

        // Operador autenticado (Session escrita solo por el servidor tras el login; ya
        // re-hidratada por EnsureUserSession). La BLL verifica con él que la sesión le
        // pertenece (auditoría AUD-05).
        private int CurrentUserId => (int)Session[Constants.SessionKeys.UserId];

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
        // Ocupa su propia franja y desaparece solo después de hideAfterMs, salvo que la guía de
        // Guided lo esté mostrando (spec 010, R9).
        private void ShowBanner(string message, int hideAfterMs = 3000)
        {
            PnlBannerZone.Visible = true;
            LitBanner.Text        = HttpUtility.HtmlEncode(message);
            ClientScript.RegisterStartupScript(GetType(), "hideBanner",
                "podAutoHide('.pod-banner', " + hideAfterMs + ");", true);
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
                    return ("Selecciona todas las piezas",
                        "En el simulador se trabajan todas las piezas juntas: selecciona todas las filas.");
                case PodNotice.SfcRequired:
                    return ("Campo requerido", "Debes ingresar o escanear un SFC antes de iniciar.");
                case PodNotice.SelectOneRow:
                    return ("Selecciona una fila", "Debes seleccionar una fila de la orden en la tabla antes de continuar.");
                case PodNotice.SelectRowToSplit:
                    return ("Selecciona una fila para dividir",
                        "Debes seleccionar una fila válida de la orden antes de iniciar SFC Split.");
                case PodNotice.SplitRequired:
                    return ("Split requerido", "Debes completar al menos un split antes de cerrar el paso.");
                case PodNotice.SplitNotActive:
                    return ("SFC Split", "SFC Split no es el paso activo.");
                case PodNotice.InvalidSplit:
                    return ("SFC Split", "La fila o cantidad seleccionada ya no es válida.");
                case PodNotice.NoPieceLeft:
                    return ("Selección inválida", "Debe quedar al menos una pieza para Complete.");
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

        // Hint de Guided tras un intento fallido: en español, con los nombres del MES en inglés
        // (spec 010, R7). Log NC solo nombra el Component si el área lo pide.
        private static string GetHint(StepType stepType, Area area)
        {
            switch (stepType)
            {
                case StepType.LogNC when !area.RequiresNCComponent:
                    return "Seleccione un código NC y presione Add-Done.";
                case StepType.ResourceSetup:  return "Presione Retrieve, verifique que Setup State esté en 'Set Up', agregue las filas y presione Save.";
                case StepType.LineClearance:  return "Responda 'Si' para confirmar la limpieza de línea.";
                case StepType.Search:         return "Responda 'Sí' a las verificaciones y presione Save.";
                case StepType.DataCollection: return "Complete todos los parámetros dentro de su rango y presione Save (no Save Draft).";
                case StepType.SFC_Split:      return "Seleccione una fila del POD, divida una cantidad válida y presione Close.";
                case StepType.LogNC:          return "Seleccione un código NC, ingrese el Component y presione Add-Done.";
                case StepType.Assembly:       return "Ensamble cada componente de la Component List: escanee su código y presione Add.";
                case StepType.Complete:       return "Presione Complete para cerrar la orden.";
                default:                      return string.Empty;
            }
        }
    }
}
