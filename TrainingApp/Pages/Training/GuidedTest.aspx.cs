using System;
using System.Globalization;
using System.Web.UI;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Guide;
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

        // Texto propio del control en el fallo de este request (ej. el botón equivocado de Log
        // NC): Ver pista lo muestra antes de la pista del paso. No se guarda entre requests (D-87).
        private string _lastFailureFeedback;

        // Mensaje del MES que la página mostró en este request (spec 017, D-102): la guía de
        // Guided pone su nota en español y Ver pista lo muestra como último fallo, con la
        // explicación. Assessment no recibe ninguna de las dos. Solo los del servidor: los
        // chequeos del cliente no pasan por aquí.
        private MesError _lastMesError;

        // Anota el mensaje que se va a pintar y lo devuelve (el primero del request).
        private MesError NoteMesError(MesError error)
        {
            if (_lastMesError == null) _lastMesError = error;
            return error;
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
            // (BtnLogout_ServerClick); sin ella (pestaña vieja tras D-9) LoadContext cierra la
            // sesión de la app, como antes (AUD-33). Una sesión propia ya completada (F5, pestaña
            // vieja) lleva a su resultado (spec 025, D-338).
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
            RegisterClientMesNotes();
        }

        // Los botones HTML del POD (Logout, Abandon) postean con __doPostBack.
        private bool IsPostBackFrom(Control control)
            => Request.Form["__EVENTTARGET"] == control.UniqueID;

        // ── Session validation ───────────────────────────────────────────
        // Sin un contexto de entrenamiento en curso, el ícono Logout sale de la app, como antes
        // (AUD-33, spec 015): no depende de la sesión de entrenamiento ni de los otros chequeos
        // del usuario. Una sesión propia ya completada (F5, pestaña vieja) lleva a su resultado
        // (spec 025, D-338); cualquier otro request va al Inicio.
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

            // Spec 021 (D-169): sin área, primero la elige. Una sesión en curso de otra área
            // (el admin cambió el área) sigue: esto solo mira si tiene alguna (D-174).
            if (SessionHelper.MustChooseArea())
                return Leave(logoutClick, () => Response.Redirect(SessionHelper.ChooseAreaUrl));

            // null: la sesión no existe, es de otro operador o ya no está en curso.
            bool hasId = int.TryParse(Request.QueryString["sessionId"], NumberStyles.None,
                CultureInfo.InvariantCulture, out int sessionId);
            var ctx = hasId ? Pod.LoadContext(sessionId, CurrentUserId) : null;
            if (ctx == null)
            {
                // Propia y completada (SessionResultService lo valida: el sessionId de la URL
                // nunca se usa sin eso): el resultado, no el Inicio.
                if (hasId && OwnCompleted(sessionId))
                    return Leave(false, () => Response.Redirect(SessionResultService.ResultUrl(sessionId)));
                return Leave(logoutClick, () => Response.Redirect("~/Pages/Training/StartSession.aspx"));
            }

            return ctx;
        }

        // Sesión propia y completada. Quien perdió la capacidad de entrenar desde el último chequeo
        // (el servicio lo verifica con la BD) no tiene resultado que ver: va al Inicio.
        private bool OwnCompleted(int sessionId)
        {
            try { return _results.Get(CurrentUserId, sessionId) != null; }
            catch (UnauthorizedAccessException) { return false; }
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

        // ── Context helper ───────────────────────────────────────────────
        // Lo que cargó Page_Load en este request; vacío si redirigió.
        private (TrainingSession, TrainingGuide, Area, TrainingStep) GetContext()
        {
            if (_ctx == null) return (null, null, null, null);
            return (_ctx.Session, _ctx.Guide, _ctx.Area, _ctx.CurrentStep);
        }
    }
}
