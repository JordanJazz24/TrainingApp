using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Guide;
using TrainingApp.Pages.Training.Models;
using TrainingApp.Shared;
using TrainingApp.UI.Controls.Training;

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

        // Texto propio del control en el fallo de este request (ej. el botón equivocado de Log
        // NC): Ver pista lo muestra antes de la pista del paso. No se guarda entre requests (D-87).
        private string _lastFailureFeedback;


        // ── Controles de paso estáticos (spec 006) ───────────────────────
        // Los 8 controles están declarados en el .aspx, así que el árbol es idéntico en
        // todos los requests. Qué paso está abierto vive en el ViewState de la página
        // (firmado con MAC) y ApplyStepControlVisibility es el único que toca Visible.
        private const string OpenStepTypeKey = "GDT_OpenStepType";

        // Pasos con control propio: Logout es el ícono del header (spec 015). Los pasos de Log
        // NC comparten un control (spec 014, D-60). Ingreso a MES no se abre como un control
        // de paso: es una pantalla completa mientras es el paso actual (spec 016, P-1).
        private static readonly StepType[] AllStepTypes =
            ((StepType[])Enum.GetValues(typeof(StepType)))
                .Where(t => t != StepType.Logout && t != StepType.MesLogin).ToArray();

        // Ingreso a MES (spec 016): la pantalla completa reemplaza al POD en esta respuesta.
        protected bool MesScreenActive { get; private set; }

        // Pantalla que decidió la BLL en el handler del Ingreso (la que falló o la que sigue);
        // null: la del avance guardado (F5, D-78).
        private MesScreen? _mesScreen;

        // La limpieza al salir cerró la sesión en este request con el script (spec 016, P-12):
        // la consola muestra su salida y el resumen se ve al cerrarla.
        private bool _cleanupCompleted;

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
            if (StepTypes.IsLogNC(stepType)) return CtrlLogNC;
            switch (stepType)
            {
                case StepType.ResourceSetup:  return CtrlResourceSetup;
                case StepType.LineClearance:  return CtrlLineClearance;
                case StepType.Search:         return CtrlSearch;
                case StepType.Assembly:       return CtrlAssembly;
                case StepType.DataCollection: return CtrlDataCollection;
                case StepType.SFC_Split:      return CtrlSFCSplit;
                case StepType.Complete:       return CtrlComplete;
                // Sin control: los guards de sender comparan contra null y no aceptan nada.
                // MesLoginControl tiene su propio evento (HandleMesAction).
                case StepType.Logout:         return null;
                case StepType.MesLogin:       return null;
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
            var shownControl = shown.HasValue ? GetStepControl(shown.Value) : null;
            foreach (var type in AllStepTypes)
            {
                var control = GetStepControl(type);
                ((Control)control).Visible = ReferenceEquals(control, shownControl);
            }

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

            // Logout (spec 015): con la sesión de entrenamiento en curso es un botón del POD
            // (BtnLogout_ServerClick); sin ella (resumen final, pestaña vieja tras D-9) LoadContext
            // cierra la sesión de la app, como antes (AUD-33, D-21).
            _ctx = LoadContext();
            if (_ctx == null) return;

            // P-1 (spec 009): la Session se perdió y la BLL reconstruyó el estado del POD. El
            // click de este postback apuntaba a filas de la orden perdida: no se ejecuta (ni la
            // página ni los controles), para no contar como intento algo que el operador no
            // hizo. Se recarga la página como un F5 y el operador repite el click. Abandon no
            // usa filas de la orden y sí se ejecuta (auditoría AUD-34).
            if (IsPostBack && _ctx.Rebuilt && !IsPostBackFrom(BtnAbandon) && !IsPostBackFrom(BtnMesAbandon))
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
                    // Tras Complete el POD queda vacío (spec 015, D-22).
                    if (order != null && !_ctx.IsResolved(StepType.Complete)) TxtSFC.Text = order.SFCCode;
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
            SetActiveButton(step.StepType, area, _ctx.Policy);
            BindSearchHeader();
        }

        // Corre después de los handlers: el momento del request ya está anotado.
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            ApplyMesLoginScreen();
            ApplySfcFieldMode();
            ApplyOrderLabel();
            ApplyNcFindingNote();
            ApplyPracticeBar();
            RegisterGuide();
        }

        // Barra de práctica (spec 012, D-87 a D-89): Ver pista, Saltar paso e Ir a… según la
        // política, en el pie del POD y en la barra del escritorio. Se decide después de los
        // handlers (el paso pudo cambiar en este request); Visible persiste en ViewState: se
        // asigna siempre. La pista y los pasos llegan con la página (JavaScriptSerializer
        // escapa < > & ').
        private void ApplyPracticeBar()
        {
            var current = _ctx != null && _ctx.Session.Status == SessionStatus.InProgress && !PnlCompleted.Visible
                ? _ctx.CurrentStepOrDefault
                : null;
            bool hint = current != null && _ctx.Policy.HintOnDemand;
            bool skip = current != null && _ctx.Policy.AllowsSkip;
            BtnHint.Visible = BtnMesHint.Visible = hint;
            BtnSkip.Visible = BtnMesSkip.Visible = BtnGoTo.Visible = BtnMesGoTo.Visible = skip;
            PnlPractice.Visible = PnlMesPractice.Visible = hint || skip;
            if (!hint && !skip) return;

            var steps = _ctx.Guide.Steps;
            var next  = steps.Where(s => s.Order > current.Order).OrderBy(s => s.Order)
                .Select(s => new { order = s.Order, name = StepNames.Display(steps, s) })
                .ToList();
            BtnGoTo.Disabled = BtnMesGoTo.Disabled = next.Count == 0;
            BtnSkip.Attributes["onclick"]    = "return practiceSkip('" + BtnSkip.UniqueID + "');";
            BtnMesSkip.Attributes["onclick"] = "return practiceSkip('" + BtnMesSkip.UniqueID + "');";
            BtnGoTo.Attributes["onclick"]    = "return practiceGoTo('" + BtnSkip.UniqueID + "');";
            BtnMesGoTo.Attributes["onclick"] = "return practiceGoTo('" + BtnMesSkip.UniqueID + "');";

            var data = new
            {
                hint     = StepHints.For(_ctx, current.StepType),
                failure  = _lastFailureFeedback,
                current  = new { order = current.Order, name = StepNames.Display(steps, current) },
                next,
                hiddenId = HfSkipTarget.ClientID
            };
            ClientScript.RegisterStartupScript(GetType(), "podPractice",
                "window.podPractice = " + new JavaScriptSerializer().Serialize(data) + ";", true);
        }

        // Saltar paso / Ir a… (spec 012, D-83, D-84 enmendada): el destino viaja en
        // HfSkipTarget. La BLL ignora uno que ya no está adelante (doble clic, F5). Si el salto
        // cierra la sesión, el resumen; si no (también si se ignoró), el POD se vuelve a armar
        // desde su estado como en un F5 (sin selección ni control abierto): los pasos saltados
        // pueden ser varios y de tipos distintos, y el Ingreso a MES saltado muestra el POD.
        protected void BtnSkip_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, _) = GetContext();
            if (session == null) return;

            string raw = HfSkipTarget.Value;
            HfSkipTarget.Value = string.Empty;
            if (int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int target) &&
                Pod.Skip(_ctx, target).Step?.Outcome == StepSubmitOutcome.SessionCompleted)
            {
                ShowCompletionPanel(session, area, guide);
                return;
            }
            Response.Redirect(Request.RawUrl);
        }

        // Ingreso a MES (spec 016, P-1): mientras es el paso actual, el escritorio simulado en
        // pantalla completa en lugar del POD. Se decide después de los handlers (Log On o el
        // avance forzado de este request lo cierran); Visible persiste en ViewState: se asigna
        // siempre.
        private void ApplyMesLoginScreen()
        {
            bool login   = _ctx != null && _ctx.Session.Status == SessionStatus.InProgress &&
                           _ctx.CurrentStepOrDefault?.StepType == StepType.MesLogin;
            bool cleanup = _ctx != null && (_ctx.IsLogoutCleanup || _cleanupCompleted);
            MesScreenActive = login || cleanup;
            PnlMesScreen.Visible = MesScreenActive;
            // El resumen (fijo, por encima del escritorio) espera a que se cierre la consola de
            // la limpieza (P-12). CssClass persiste en ViewState: se asigna siempre.
            PnlCompleted.CssClass = _cleanupCompleted ? "pod-completion d-none" : "pod-completion";
            if (!MesScreenActive) return;

            if (login)
            {
                var progress = _ctx.MesLogin;
                var screen   = _mesScreen ?? progress.ResumeScreen();
                CtrlMesLogin.View = new MesLoginView
                {
                    Mode         = MesLoginControl.ModeLogin,
                    ScriptDone   = progress.ScriptDone,
                    EdgeOpen     = progress.EdgeOpen,
                    HoraHoraDone = progress.HoraHoraDone,
                    RecursosDone = progress.RecursosDone,
                    Screen       = MesLoginControl.ScreenFor(screen),
                    ResourceName = _ctx.ResourceName,
                    ScriptCode1  = screen == MesScreen.ScriptOutput ? _ctx.Code1 : null
                };
            }
            else
            {
                // Limpieza al salir (P-6): "You have logged off" hasta cerrar Edge; después el
                // escritorio y el script. Si el script cerró la sesión en este request, la
                // consola muestra su salida y al cerrarla se ve el resumen (P-12).
                var screen = _cleanupCompleted ? MesScreen.ScriptOutput
                    : _mesScreen ?? _ctx.State.Logout.ResumeScreen();
                CtrlMesLogin.View = new MesLoginView
                {
                    Mode           = MesLoginControl.ModeLogout,
                    EdgeOpen       = !_cleanupCompleted && !_ctx.State.Logout.EdgeClosed,
                    Screen         = MesLoginControl.ScreenFor(screen),
                    ResourceName   = _ctx.ResourceName,
                    ScriptCode1    = screen == MesScreen.ScriptOutput ? _ctx.Code1 : null,
                    CloseToSummary = _cleanupCompleted
                };
            }
            LitMesArea.Text = HttpUtility.HtmlEncode(_ctx.Area.Name);
            LitMesMode.Text = ModeLabel(_ctx.Policy).Label;
            BtnMesAbandon.Attributes["onclick"] = "return confirmAbandon('" + BtnMesAbandon.UniqueID + "');";
        }

        // Una pantalla del Ingreso enviada por MesLoginControl (D-77): la califica la BLL y dice
        // qué pantalla sigue o a cuál vuelve un fallo (P-4). Log On correcto o el avance forzado
        // cierran el paso y aparece el POD (P-5).
        protected void HandleMesAction(object sender, MesActionEventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            bool cleanup = _ctx.IsLogoutCleanup;
            var result = Pod.SubmitMesAction(_ctx, e.Action, e.Code1, e.Resource, e.Password);
            _mesScreen = result.MesScreen;
            if (result.Step != null)
            {
                ApplySubmitResult(result.Step, session, guide, area, step, incorrectFeedback: null);
                // La guía retoma en la pantalla que falló (spec 016, P-4).
                if (result.Step.Outcome == StepSubmitOutcome.Incorrect)
                    _guideMoment = GuideMoment.Incorrect(step, result.MesScreen?.ToString());
                // Limpieza terminada con el script (no forzada): primero la salida de la consola.
                _cleanupCompleted = cleanup && result.Step.Outcome == StepSubmitOutcome.SessionCompleted &&
                                    result.MesScreen == MesScreen.ScriptOutput;
                if (_cleanupCompleted)
                    _guideMoment = GuideMoment.SessionCompleted(step, GuideMoment.CleanupDetail);
                return;
            }

            // Guided: abrir el Repositorio fuera de su turno (antes del script del Ingreso o en
            // la limpieza) se ignora, sin intento, como un botón fuera de secuencia; la guía (E4)
            // no lo deja. Mientras tanto, un aviso que no parece un error calificado.
            if (result.NotCurrentStepNotice)
                ShowNotCurrentStepNotice();
            else if (result.OutOfSequence)
                ShowSweetAlert("info", cleanup ? "Limpieza al salir" : "Antes del Repositorio",
                    cleanup
                        ? "Después del Logout no se abre el Repositorio: ejecute el script de limpieza (MES Limpiar historial y cache) con su Code1."
                        : "Primero ejecute el script de limpieza (MES Limpiar historial y cache) con su Code1.");
        }

        // Nota de hallazgo (spec 014, D-49): solo el hallazgo del paso actual de Log NC, en los
        // dos modos, sin nombrar el código. Se decide después de los handlers (el paso pudo
        // cambiar en este request); Visible persiste en ViewState: se asigna siempre.
        private void ApplyNcFindingNote()
        {
            var finding = _ctx?.CurrentNcFinding;
            PnlNcFinding.Visible = finding != null && !PnlCompleted.Visible;
            if (!PnlNcFinding.Visible) return;

            // Guided: la guía la muestra al llegar al paso y la deja resaltada (pod-guide.js);
            // Assessment no tiene guía y la ve de inmediato. Los atributos persisten en ViewState.
            if (_ctx.Policy.ShowsGuide)
                PnlNcFinding.Attributes["data-guide-reveal"] = string.Empty;
            else
                PnlNcFinding.Attributes.Remove("data-guide-reveal");

            bool serialized = finding.RowId != null;
            LitFindingTitle.Text = string.Format("Hallazgo {0} de {1}", finding.Index, finding.Total);
            LitFindingPiece.Text = serialized
                ? "SFC <strong>" + HttpUtility.HtmlEncode(finding.SFCCode) + "</strong>"
                : "Lote <strong>" + HttpUtility.HtmlEncode(finding.SFCCode) + "</strong>: " +
                  finding.UnitCount.ToString(CultureInfo.InvariantCulture) + " unidades";
            LitFindingDefect.Text      = HttpUtility.HtmlEncode(finding.Description);
            LitFindingDisposition.Text = HttpUtility.HtmlEncode(FindingDisposition(finding));
        }

        // Disposición del hallazgo como la dictaría el MPI o el NCR (D-28).
        private static string FindingDisposition(NcFinding finding)
        {
            switch (finding.Method)
            {
                case NCMethod.FastScrap:
                    return finding.RowId != null ? "desechar la pieza (scrap)" : "desechar las unidades (scrap)";
                case NCMethod.FastRepair:
                    return "reemplazar el componente " + finding.ComponentPartNumber + " (se desecha)";
                case NCMethod.ExtendedRepair:
                    // P-13: con un código que exige Comments, el MPI que se anota.
                    string disposition = finding.Disposition == NcDisposition.ReturnToPrevious
                        ? "regresar la pieza a una operación anterior (Return to any previous)"
                        : "retrabajar la pieza en esta operación (Local Rework)";
                    return string.IsNullOrEmpty(finding.Mpi) ? disposition : disposition + ", según " + finding.Mpi;
                default:
                    return string.Empty;
            }
        }

        // Etiqueta de la orden (spec 011, enmienda 2026-10-13): visible desde que hay orden
        // hasta el Start, con su Shop Order (la por defecto si Resource Setup avanzó forzado:
        // AC-3). Se decide después de los handlers porque Resource Setup o el Start de este
        // mismo request la cambian. Visible persiste en ViewState: se asigna en cada request.
        private void ApplyOrderLabel()
        {
            var order = Order;
            PnlOrderLabel.Visible = order != null && !PodOrderStarted && !PnlCompleted.Visible;
            LabelShopOrder = order?.ShopOrderInput ?? string.Empty;
        }

        protected string LabelShopOrder { get; private set; } = string.Empty;

        // Spec 011: TxtSFC se escribe o se escanea hasta el Start (la carga la resuelve el
        // cliente, como Browse Work List); con el Start ya ejecutado vuelve a ser de solo
        // lectura, como antes. Atributo HTML y no la propiedad ReadOnly: con ReadOnly=true
        // ASP.NET ignora el valor posteado y lo cargado se perdería en cada postback. Se decide
        // en PreRender porque el Start de este mismo request ya lo cambia (los atributos
        // persisten en ViewState).
        private void ApplySfcFieldMode()
        {
            if (PodOrderStarted)
                TxtSFC.Attributes["readonly"] = "readonly";
            else
                TxtSFC.Attributes.Remove("readonly");
        }

        // Mensajes del MES de los chequeos de formato del cliente (spec 017), los mismos del
        // catálogo de la BLL, ya codificados para un string de JS: un número que no es de la
        // orden (22507, spec 011 punto 5) y DC Collect o SFC Split sin fila (12906).
        protected string InvalidSfcMesErrorJs => MesErrorJs(MesMessageId.InvalidSfc);
        protected string SfcMissingMesErrorJs => MesErrorJs(MesMessageId.SfcMissing);

        private static string MesErrorJs(MesMessageId id)
            => HttpUtility.JavaScriptStringEncode(MesMessage.Get(id).Format());

        // Guía visual (spec 010): solo en Guided. Assessment no recibe el motor, el guion, los
        // textos ni el estado (AC-2). Pasos sin guion todavía: la guía no se muestra.
        private void RegisterGuide()
        {
            // Visible persiste en ViewState: se decide en cada request.
            PhGuideAssets.Visible = false;
            if (_ctx == null || !_ctx.Policy.ShowsGuide) return;

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
            if (order != null && _ctx.IsResolved(StepType.Complete))
            {
                // Spec 015 (D-22): la orden ya salió del POD. Sin header; el campo SFC sigue de
                // solo lectura y la etiqueta de la orden no vuelve (PodOrderStarted).
                SetSearchHeader(string.Empty, string.Empty);
                PodOrderStarted = true;
            }
            else if (order != null && _ctx.IsResolved(StepType.Search))
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
            PodCommandResult logNC = null;
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
                    if (StepTypes.IsLogNC(step.StepType))
                    {
                        // Spec 014: la BLL califica pieza y código contra el hallazgo y, resuelto
                        // el paso, aplica su disposición.
                        logNC  = Pod.SubmitLogNC(_ctx, e.Answer, SelectedIds());
                        result = logNC.Step;
                        break;
                    }
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

            ContinueAfterStepResolved(session, guide, area, step, outcome, logNC);
        }

        // Paso resuelto (Correct, FailedForced o sesión completada) desde un control: cierra el
        // control, habilita el botón del paso siguiente y aplica lo propio de cada paso. Lo
        // usan el submit de los controles y el ensamble del último componente (spec 008).
        // logNC: el resultado de Log NC resuelto (lo que la BLL hizo con la pieza del hallazgo).
        private void ContinueAfterStepResolved(TrainingSession session, TrainingGuide guide,
            Area area, TrainingStep step, StepSubmitOutcome outcome, PodCommandResult logNC = null)
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
                SetActiveButton(nextStep.StepType, area, _ctx.Policy);

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
            else if (StepTypes.IsLogNC(step.StepType) && logNC != null)
                ShowLogNCResolved(area, session, logNC);
        }

        // Log NC resuelto (spec 014): el banner de cada método.
        private void ShowLogNCResolved(Area area, TrainingSession session, PodCommandResult logNC)
        {
            if (logNC.Repaired != null)
                ShowRepaired(area, session, logNC.Repaired);
            else if (logNC.Reworked != null)
                ShowReworked(area, session, logNC.Reworked, logNC.ReworkOperation);
            else
                ShowScrapped(area, session, logNC.Scrapped);
        }

        // Reparación extendida resuelta (D-54): Return to any previous ya sacó la pieza del POD;
        // Local Rework la deja, con la operación actual en el banner (V-5, confirmado por el
        // usuario el 2026-10-19).
        private void ShowReworked(Area area, TrainingSession session, NcFinding reworked, string operation)
            => ShowLogNCBanner(area, session,
                string.Format("All of {0} will be processed at {1}", reworked.SFCCode, operation));

        // Vuelta al POD tras Log NC: sin selección, el SFC vacío en Serialized (NonSerialized:
        // TxtSFC es el SFC del lote y no cambia, spec 007), la orden al día y el banner.
        private void ShowLogNCBanner(Area area, TrainingSession session, string message)
        {
            ClearRowSelection();
            if (area.SerializationType == SerializationType.Serialized)
                TxtSFC.Text = string.Empty;
            BindWorkList(area, session);
            ShowBanner(message, hideAfterMs: 5000);
        }

        // Reparación rápida resuelta (spec 014): la pieza sigue en el POD (D-47). Banner de
        // D-55, confirmado por el usuario el 2026-10-19 (V-3).
        private void ShowRepaired(Area area, TrainingSession session, NcFinding repaired)
            => ShowLogNCBanner(area, session,
                string.Format("Component {0} replaced on {1}", repaired.ComponentPartNumber, repaired.SFCCode));

        // Assembly (spec 008): el operador ensambló un componente del BOM. Mientras falten
        // componentes la Component List se actualiza y el modal queda abierto con la
        // confirmación del Add (Fase 6.5); con el último, el paso se resuelve y vuelve el POD.
        protected void HandleComponentAssembled(object sender, ComponentAssembledEventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            // Mismo guard de sender que HandleControlSubmit (spec 006, E12).
            if (!ReferenceEquals(sender, GetStepControl(step.StepType))) return;

            var result = Pod.AssembleComponent(_ctx, e.ComponentId, e.Barcode);
            // Spec 017 (M-8): el 17150 en la franja de la ventana, que queda abierta.
            if (result.MesError != null)
            {
                CtrlAssembly.ShowMesError(e.ComponentId, result.MesError);
                return;
            }
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

            ShowLogNCBanner(area, session, scrapped + " scrapped");
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
            // Spec 014 (enmienda 3): una cantidad distinta de la del hallazgo es un intento. El
            // control queda abierto en el modal del split; forzado, el paso se resuelve con el
            // split de N que dejó la BLL.
            if (result.Step != null)
            {
                e.Handled = true;
                ApplySubmitResult(result.Step, session, guide, area, step, incorrectFeedback: null);
                if (result.Step.Outcome != StepSubmitOutcome.Incorrect)
                {
                    e.StepResolved = true;
                    ContinueAfterStepResolved(session, guide, area, step, result.Step.Outcome);
                }
                return;
            }
            // El modal del split muestra el mensaje del MES en lugar del formulario (spec 017) o
            // el aviso del simulador en su propio mensaje de error.
            if (result.MesError != null)
            {
                e.MesError = result.MesError;
                return;
            }
            if (result.Notice != PodNotice.None)
            {
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
                    _lastFailureFeedback = incorrectFeedback;
                    ShowSweetAlert("warning", "Incorrect",
                        BuildIncorrectFeedback(_ctx, step.StepType, result.RemainingScoredAttempts, incorrectFeedback));
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

        // Botón de otro paso sin intento (D-88).
        private void ShowNotCurrentStepNotice()
            => ShowSweetAlert("info", "Otro paso", "Ese no es el paso actual.");

        // Assessment (S2): todos los botones del POD están habilitados y un click en uno que
        // no es el del paso actual es un intento fallido de ese paso (lo decide la BLL antes que
        // cualquier aviso de formato). Devuelve true si el click fue fuera de secuencia; en
        // Guided esos botones están deshabilitados y el click se ignora; en Práctica libre, el
        // aviso sin intento (D-88).
        private bool HandleOutOfSequence(PodCommandResult result, TrainingSession session,
            TrainingGuide guide, Area area, TrainingStep step)
        {
            if (!result.OutOfSequence) return false;
            if (result.Step == null)
            {
                if (result.NotCurrentStepNotice) ShowNotCurrentStepNotice();
                return true;
            }

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

            // Complete forzado (spec 015, D-22): la BLL quitó las filas del POD.
            if (podChanged && forcedStep == StepType.Complete)
            {
                RepaintCompletedPod(area, session);
                return;
            }

            // Log NC forzado (spec 014, P-5): la BLL desechó la pieza del hallazgo.
            if (podChanged && StepTypes.IsLogNC(forcedStep))
            {
                ClearRowSelection();
                if (area.SerializationType == SerializationType.Serialized)
                    TxtSFC.Text = string.Empty;
                rebind = true;
            }

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
        private static string BuildIncorrectFeedback(PodContext ctx, StepType stepType,
            int remainingScoredAttempts, string incorrectFeedback)
        {
            var policy = ctx.Policy;
            if (policy.FailureFeedback == FailureFeedbackKind.RemainingAttempts)
            {
                return remainingScoredAttempts > 0
                    ? string.Format("Respuesta incorrecta. Te quedan {0} intento(s) con puntaje.", remainingScoredAttempts)
                    : "Respuesta incorrecta. Si vuelves a fallar, el paso se marca fallido y avanza.";
            }

            // Práctica libre (P-6): sin intentos restantes; la pista se pide con Ver pista.
            if (policy.FailureFeedback == FailureFeedbackKind.Incorrect)
                return "Respuesta incorrecta.";

            if (!string.IsNullOrEmpty(incorrectFeedback)) return incorrectFeedback;
            string hint = StepHints.For(ctx, stepType);
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

            SetActiveButton(nextStep.StepType, area, _ctx.Policy);
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
            if (result.MesError != null)
            {
                ShowMesError(result.MesError);
                return;
            }
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
            if (ShowSelectionError(result, area, session)) return;

            // Abre la Component List; el modal Assemble se abre desde cada componente.
            OpenStepControl(StepType.Assembly, area, session, guide, modalId: null);
        }

        // Selección inválida al abrir Assembly, DC o SFC Split (lo decide la BLL, sin intento):
        // sin la fila del lote, el 12906 del MES y la selección del POD se limpia; en Serialized,
        // el aviso del simulador. Devuelve si hubo alguno.
        private bool ShowSelectionError(PodCommandResult result, Area area, TrainingSession session)
        {
            if (result.MesError != null)
            {
                ClearRowSelection();
                BindWorkList(area, session);
                ShowMesError(result.MesError);
                return true;
            }
            if (result.Notice != PodNotice.None)
            {
                ShowNotice(result.Notice);
                return true;
            }
            return false;
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
            if (ShowSelectionError(result, area, session)) return;

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
            if (ShowSelectionError(result, area, session)) return;

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
                    AttemptFeedback(result.FeedbackReason, area));
                // Avance forzado (spec 014, P-5): la BLL aplicó la disposición del hallazgo.
                if (result.Step.Outcome == StepSubmitOutcome.FailedForced)
                    ShowLogNCResolved(area, session, result);
                return;
            }

            // El paso de Log NC actual (spec 014: el botón abre cualquiera de los tres).
            OpenStepControl(step.StepType, area, session, guide, modalId: null);
        }

        // Acciones de MES que nunca son un paso de la guía (Work Instructions, Reports, Place
        // Back in Queue, Tool List y las opciones de relleno de Activities). No tienen lógica:
        // en Assessment el click es un intento fallido del paso actual (S2).
        protected void BtnNonStepAction_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            HandleOutOfSequence(Pod.ClickNonStepAction(_ctx, NonStepAction(sender)), session, guide, area, step);
        }

        // Botón de relleno → su acción en el registro de actividad (spec 013). Es también la lista
        // de botones de relleno (SetActiveButton): un botón nuevo se agrega aquí una sola vez.
        private Dictionary<System.Web.UI.HtmlControls.HtmlButton, string> NonStepActions =>
            new Dictionary<System.Web.UI.HtmlControls.HtmlButton, string>
            {
                { BtnWorkInstructions,      AttemptActions.WorkInstruction },
                { BtnReports,               AttemptActions.Reports },
                { BtnPlaceBackInQueue,      AttemptActions.PlaceBackInQueue },
                { BtnToolList,              AttemptActions.ToolList },
                { BtnAsBuiltConfig,         AttemptActions.AsBuiltConfig },
                { BtnLogComment,            AttemptActions.LogComment },
                { BtnSfcPlaceHold,          AttemptActions.SfcPlaceHold },
                { BtnSfcReleaseHold,        AttemptActions.SfcReleaseHold },
                { BtnRemoveFutureHold,      AttemptActions.RemoveFutureHold },
                { BtnChangeEquipmentStatus, AttemptActions.ChangeEquipmentStatus },
                { BtnTimeSensitiveMaterial, AttemptActions.TimeSensitiveMaterial }
            };

        private string NonStepAction(object sender)
        {
            var button = sender as System.Web.UI.HtmlControls.HtmlButton;
            if (button == null || !NonStepActions.TryGetValue(button, out string action))
                throw new InvalidOperationException("Botón de relleno sin acción en el registro de actividad.");
            return action;
        }

        private IEnumerable<System.Web.UI.HtmlControls.HtmlButton> NonStepActionButtons => NonStepActions.Keys;

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
                AttemptFeedback(result.FeedbackReason, area));
            ShowCompleted(area, session, result.Completed, banner: result.Step.Outcome == StepSubmitOutcome.Correct);
        }

        // Spec 015 (D-22): las filas completadas salen del POD (las quitó la BLL). Banner
        // genérico PROVISIONAL hasta tener la captura real (general-flow → Paso 9).
        private void ShowCompleted(Area area, TrainingSession session, IList<SimulatedOrderRow> rows, bool banner)
        {
            if (rows == null || rows.Count == 0 || session.Status != SessionStatus.InProgress) return;

            RepaintCompletedPod(area, session);
            if (banner)
                ShowBanner(area.SerializationType == SerializationType.Serialized
                    ? rows.Count + " SFCs completed"
                    : "SFC " + rows[0].SFCCode + " completed", hideAfterMs: 5000);
        }

        // El POD después de Complete (normal o forzado): sin selección, sin SFC ni header.
        private void RepaintCompletedPod(Area area, TrainingSession session)
        {
            ClearRowSelection();
            TxtSFC.Text = string.Empty;
            BindWorkList(area, session);
            BindSearchHeader();
        }

        protected void BtnAbandon_Click(object sender, EventArgs e)
        {
            if (_ctx == null) return;
            // Abandona la sesión y borra su estado del POD. Al completar, el estado queda en la
            // Session por diseño (Deferred Decisions de la spec 005).
            Pod.Abandon(_ctx);
            Response.Redirect("~/Pages/Training/StartSession.aspx");
        }

        // Logout con la sesión de entrenamiento en curso (spec 015): antes del último paso es un
        // botón más del POD (S2). En el último paso empieza la limpieza (spec 016, P-6): "You
        // have logged off" en Edge, sin intento; el script la termina (HandleMesAction). Sin
        // sesión en curso no llega acá: LoadContext ya salió (D-21).
        protected void BtnLogout_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            var result = Pod.ClickLogout(_ctx);
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
            _mesScreen = result.MesScreen;
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
            // Con la sesión cerrada, Logout sale de la app (D-21, spec 015), también con el globo
            // final de la guía en pantalla.
            BtnLogout.Disabled   = false;
            BtnLogout.Attributes["data-guide-free"] = string.Empty;

            // P-11: Práctica libre calcula el puntaje pero no lo muestra.
            PnlFinalScore.Visible = _ctx.Policy.ShowsScore;
            decimal finalScore = session.FinalScore * 100m;
            LitFinalScore.Text   = finalScore.ToString("F1") + "%";
            LitFinalMode.Text    = ModeLabel(_ctx.Policy).Label;

            LitFinalArea.Text    = Page.Server.HtmlEncode(area.Name);
            LitFinalProduct.Text = Page.Server.HtmlEncode(_ctx.Product?.Name ?? "—");

            if (_ctx.Policy.Summary == SummaryKind.ScoreAndResult)
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

            // Práctica libre (P-10): los pasos con sus intentos, de la sesión que se cerró en este
            // request (el resumen no se vuelve a ver después de cerrarlo).
            PnlAttemptSummary.Visible = _ctx.Policy.Summary == SummaryKind.AttemptsPerStep;
            if (PnlAttemptSummary.Visible)
            {
                RptAttemptSummary.DataSource = Pod.AttemptSummary(_ctx);
                RptAttemptSummary.DataBind();
            }
        }

        // Fila del resumen: resaltada si tuvo fallos (más de un intento, o fallos antes de
        // saltarse); atenuada si se saltó.
        protected static string SummaryRowCss(StepAttemptSummaryRow row)
        {
            bool retried = row.Skipped ? row.Failures > 0 : row.Attempts > 1;
            return string.Join(" ", new[] { retried ? "summary-retried" : null, row.Skipped ? "summary-skipped" : null }
                .Where(c => c != null));
        }

        // "Saltado" o "Saltado (N fallos)" (punto confirmado 4); si no, cuántos intentos.
        protected static string SummaryAttemptsText(StepAttemptSummaryRow row)
        {
            if (!row.Skipped) return row.Attempts.ToString(CultureInfo.InvariantCulture);
            if (row.Failures == 0) return "Saltado";
            return row.Failures == 1
                ? "Saltado (1 fallo)"
                : string.Format(CultureInfo.InvariantCulture, "Saltado ({0} fallos)", row.Failures);
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

            if (StepTypes.IsLogNC(step.StepType))
            {
                vm.ExtraData["NCCodes"] = _ctx.NCCodes;
                // "SFC (1/N)" y "Apply to all SFCs" de NC Data Entry (Fase 6.5, entrega 8).
                var ncPieces = FindSelectedOrderRows();
                vm.ExtraData["SelectedPieceCount"] = ncPieces.Count;
                vm.ExtraData["FirstSelectedSFC"]   = ncPieces.Count > 0
                    ? ncPieces[0].SFCCode ?? Order?.SFCCode
                    : null;
                // Guided (spec 014): NC Selection abre en la página del código del hallazgo, que
                // señala la guía. Assessment no lo recibe.
                if (_ctx.Policy.ShowsGuide)
                    vm.ExtraData["GuideNCCode"] = _ctx.NcFindingFor(step)?.Code;
                // Browse y reparaciones (spec 014): lo ensamblado y As-Built Configuration (P-11:
                // de la orden simulada; Routing y BOM son simulados; la operación, la actual del
                // área, D-58).
                vm.ExtraData["Assembled"] = _ctx.State.AssembledComponents ?? new List<AssembledComponent>();
                vm.ExtraData["RepairInfo"] = new Dictionary<string, string>
                {
                    ["sfc"]         = vm.ExtraData["FirstSelectedSFC"] as string ?? sfcCode,
                    ["material"]    = PodDefaults.MaterialVersion(Order),
                    ["description"] = Order?.MaterialDescription ?? string.Empty,
                    ["routing"]     = PodDefaults.SimRouting(area),
                    ["bom"]         = PodDefaults.SimBom(Order),
                    ["status"]      = SimulatedOrderRow.StatusActive,
                    ["operation"]   = _ctx.CurrentOperationName,
                    ["resource"]    = resourceName
                };
                return vm;
            }

            switch (step.StepType)
            {
                case StepType.ResourceSetup:
                    vm.ExtraData["Preloaded"] = _ctx.State.PreloadedEquipment;
                    break;

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
        // Sin un contexto de entrenamiento en curso, el ícono Logout sale de la app, como antes
        // (AUD-33, D-21 de la spec 015): no depende de la sesión de entrenamiento ni de los
        // otros chequeos del usuario. Cualquier otro request va a su página.
        private PodContext LoadContext()
        {
            bool logoutClick = IsPostBack && IsPostBackFrom(BtnLogout);

            // Re-hidrata sesión si se perdió pero el usuario sigue autenticado
            if (!SessionHelper.EnsureUserSession())
                return Leave(logoutClick, () => Response.Redirect(SessionHelper.LoginUrl));

            // Spec 013: solo entrena quien tiene la capacidad; un admin vuelve a su módulo.
            if (!SessionHelper.CanTrain())
                return Leave(logoutClick, SessionHelper.RedirectHome);

            // Primer acceso sin contraseña personal: no puede entrenar (auditoría AUD-08).
            if (SessionHelper.MustChangePassword())
                return Leave(logoutClick, () => Response.Redirect(SessionHelper.ChangePasswordUrl));

            // null: la sesión no existe, es de otro operador o ya no está en curso.
            var ctx = int.TryParse(Request.QueryString["sessionId"], out int sessionId)
                ? Pod.LoadContext(sessionId, CurrentUserId) : null;
            if (ctx == null)
                return Leave(logoutClick, () => Response.Redirect("~/Pages/Training/StartSession.aspx"));

            return ctx;
        }

        private static PodContext Leave(bool logoutClick, Action redirect)
        {
            if (logoutClick)
                SessionHelper.Logout();
            else
                redirect();
            return null;
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
            PnlScore.Visible = _ctx.Policy.ShowsScore;

            var (label, badgeClass) = ModeLabel(_ctx.Policy);
            LitModeBadge.Text = string.Format(
                "<span class=\"{0}\">{1}</span>", badgeClass, label);
        }

        // Nombre del modo en el header, la barra del simulador y el resumen, con su badge.
        private static (string Label, string BadgeClass) ModeLabel(ModePolicy policy)
        {
            switch (policy.Mode)
            {
                case SessionMode.Guided:     return ("Guided", "badge badge-guided");
                case SessionMode.Assessment: return ("Assessment", "badge badge-assessment");
                case SessionMode.FreePractice: return ("Práctica libre", "badge badge-practice");
                default: throw new ArgumentOutOfRangeException(nameof(policy), policy.Mode, "Modo sin etiqueta.");
            }
        }

        private void SetActiveButton(StepType stepType, Area area, ModePolicy policy)
        {
            // Assessment (S2): todo habilitado — elegir el botón correcto es parte de la evaluación.
            bool enableAll = policy.EnablesAllButtons;

            BtnResourceSetup.Disabled = !enableAll;
            BtnStart.Disabled         = !enableAll;
            BtnAssemble.Disabled      = !enableAll;
            BtnDCCollection.Disabled  = !enableAll;
            BtnComplete.Disabled      = !enableAll;
            BtnActivities.Disabled    = !enableAll;
            BtnSFCSplit.Disabled      = !enableAll;
            BtnLogNC.Disabled         = !enableAll;
            BtnLogout.Disabled        = !enableAll;
            foreach (var decoy in NonStepActionButtons)
                decoy.Disabled = !enableAll;

            // La validación de fila en cliente (formato) solo aplica al botón del paso actual:
            // un click fuera de secuencia tiene que llegar al servidor para contar.
            SetClientRowCheck(BtnDCCollection, "podValidateDCCollect", stepType == StepType.DataCollection);
            SetClientRowCheck(BtnSFCSplit, "podValidateSFCSplit", stepType == StepType.SFC_Split);
            // Spec 011 (enmienda E-1): con Search como paso actual, Start carga primero lo escrito
            // en TxtSFC sin Enter. Fuera de turno no hay chequeo: el click cuenta en Assessment (S2).
            SetClientRowCheck(BtnStart, "podLoadBeforeStart", stepType == StepType.Search);

            if (enableAll)
            {
                EnableActivitiesToggle();
                return;
            }

            // Spec 014 (P-7): los tres pasos de Log NC se abren con [Log NC].
            if (StepTypes.IsLogNC(stepType))
            {
                BtnLogNC.Disabled = false;
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
                case StepType.Complete:
                    BtnComplete.Disabled = false;
                    break;
                case StepType.Logout:
                    BtnLogout.Disabled = false;
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

        // Mensaje del MES del banner del POD (spec 017, superficie B), elegido por la BLL. Lo
        // pinta podShowMesErrors como texto plano; queda hasta el siguiente postback (D-112).
        private void ShowMesError(MesError error)
        {
            ClientScript.RegisterStartupScript(GetType(), "podMesError",
                "podShowMesErrors(" + new JavaScriptSerializer().Serialize(new[] { error.Text }) + ");", true);
        }

        // Avisos de formato propios del simulador que decide la BLL (no cuentan como intento).
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
                // Spec 011, punto 6: un solo aviso para el escaneo y Browse Work List.
                case PodNotice.StartWholeOrder:
                    return ("Inicia la orden completa",
                        "En esta guía se trabaja la orden completa: escanea la Shop Order o selecciona todas las piezas en Browse Work List.");
                case PodNotice.SplitRequired:
                    return ("Split requerido", "Debes completar al menos un split antes de cerrar el paso.");
                case PodNotice.SplitNotActive:
                    return ("SFC Split", "SFC Split no es el paso activo.");
                case PodNotice.InvalidSplit:
                    return ("SFC Split", "La fila seleccionada ya no es válida.");
                case PodNotice.InvalidSplitQty:
                    return ("SFC Split", "La cantidad a dividir debe ser un número entero menor que la Qty disponible.");
                case PodNotice.NoPieceLeft:
                    return ("Selección inválida", "Debe quedar al menos una pieza para Complete.");
                case PodNotice.SplitAlreadyDone:
                    return ("SFC Split", "Ya se separó la cantidad del hallazgo.");
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
    }
}
