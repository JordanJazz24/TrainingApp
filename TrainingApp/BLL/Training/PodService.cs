using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training.Pod;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;
using static TrainingApp.BLL.Training.Pod.PodCore;
using static TrainingApp.BLL.Training.Pod.PodOrderEffects;

namespace TrainingApp.BLL.Training
{
    // Motor del POD (spec 009): dueño del estado del POD (orden simulada y avance de Assembly
    // y DC) y del registro de intentos. Los comandos reciben el PodContext que LoadContext
    // cargó en el mismo request: no vuelven a leer la sesión, porque spStepAttemptRecord
    // rechaza en su transacción un intento sobre una sesión que cambió (AUD-06).
    //
    // Los comandos de botón (Open*, ClickStart, Complete, ClickNonStepAction) evalúan primero
    // la secuencia: un botón que no es el del paso actual es un intento fallido del paso
    // actual en Assessment y se ignora en Guided (S2). Después deciden los avisos de formato
    // (PodNotice, sin intento) y la acción del paso.
    //
    // PodService es la fachada (AUD-74, E5): conserva los mismos métodos públicos y delega cada
    // comando en su colaborador de BLL/Training/Pod/. El PodContext y el PodState se pasan, no se
    // copian.
    public class PodService
    {
        private readonly IPodStateStore             _store;
        private readonly ITrainingSessionRepository _sessionRepo;
        private readonly IStepAttemptRepository     _attemptRepo;
        private readonly IUserRepository            _userRepo;
        private readonly IGuideCatalog              _catalog;
        private readonly ITrainingSessionService    _sessionService;
        private readonly ScoreCalculator            _scoreCalc;
        private readonly PodCore                _core;
        private readonly PodOrderEffects        _effects;
        private readonly PodForcedAdvance       _forced;
        private readonly PodSetupCommands       _setup;
        private readonly PodAssemblyDcCommands  _assemblyDc;
        private readonly PodSplitCommands       _split;
        private readonly PodLogNcCommands       _logNc;
        private readonly PodCompletionCommands  _completion;

        // La página y StartSession: el servidor real (repositorios y servicios con su BD).
        public PodService(IPodStateStore store)
            : this(store, new TrainingSessionRepository(), new StepAttemptRepository(), new UserRepository(),
                   new TrainingGuideService(), new TrainingSessionService(), new ScoreCalculator())
        {
        }

        // Para los tests (AUD-74, D-15, D-259): todo en memoria. ScoreCalculator es puro.
        public PodService(IPodStateStore store, ITrainingSessionRepository sessionRepo,
            IStepAttemptRepository attemptRepo, IUserRepository userRepo, IGuideCatalog catalog,
            ITrainingSessionService sessionService, ScoreCalculator scoreCalc)
        {
            _store          = store ?? throw new ArgumentNullException(nameof(store));
            _sessionRepo    = sessionRepo ?? throw new ArgumentNullException(nameof(sessionRepo));
            _attemptRepo    = attemptRepo ?? throw new ArgumentNullException(nameof(attemptRepo));
            _userRepo       = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _catalog        = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _scoreCalc      = scoreCalc ?? throw new ArgumentNullException(nameof(scoreCalc));

            _core       = new PodCore(store, attemptRepo, sessionRepo, scoreCalc);
            _effects    = new PodOrderEffects(_core);
            _forced     = new PodForcedAdvance(_core, _effects);
            var sequence = new PodSequence(_core, _forced);
            _setup      = new PodSetupCommands(_core, _effects, sequence);
            _assemblyDc = new PodAssemblyDcCommands(_core, sequence);
            _split      = new PodSplitCommands(_core, _effects, sequence);
            _logNc      = new PodLogNcCommands(_core, _effects, sequence);
            _completion = new PodCompletionCommands(_core, _effects, sequence);
        }

        // Sesión, guía, área, intentos y estado del POD, una vez por request (AUD-25). null si
        // la sesión no existe, es de otro operador o ya no está en curso: la página vuelve a
        // StartSession, como antes.
        public PodContext LoadContext(int sessionId, int userId)
        {
            var session = _sessionRepo.GetById(sessionId);
            if (session == null || session.UserId != userId ||
                session.Status != SessionStatus.InProgress)
                return null;

            var guide    = _catalog.GetById(session.TrainingGuideId);
            var area     = _catalog.GetArea(guide.AreaId);
            var attempts = _attemptRepo.GetBySessionId(sessionId);

            var state = _store.Load(sessionId);
            if (state == null)
            {
                state = new PodState();
                _store.Save(sessionId, state);
            }

            decimal score = _scoreCalc.CalculateSessionScore(guide.Steps, attempts, session.Mode);
            var ctx = new PodContext(userId, session, guide, area, attempts, state, score, _catalog,
                () => _userRepo.GetById(userId)?.Code1);

            // Después de Resource Setup siempre hay orden: si falta, se perdió la Session. Después
            // del Start, con Log NC en la guía, siempre hay hallazgos (spec 014): si faltan, el
            // estado no es confiable y se reconstruye igual (D-10).
            if ((state.Order == null && ctx.IsResolved(StepType.ResourceSetup)) ||
                (state.Order != null && state.NcFindings == null && ctx.IsResolved(StepType.Search) &&
                 HasLogNC(guide)))
                RebuildState(ctx);
            return ctx;
        }

        // Reconstrucción (D-10): la orden por defecto con los efectos de los pasos ya
        // resueltos, como si cada uno hubiera avanzado forzado. Sin aviso, sin intento y sin
        // tocar el puntaje. Assembly/DC a medias empiezan de cero (su avance se perdió).
        private void RebuildState(PodContext ctx)
        {
            _effects.CreateOrder(ctx, PodDefaults.DefaultShopOrder);

            var resolved = ctx.Guide.Steps
                .Where(s => s.Order < ctx.Session.CurrentStepOrder)
                .OrderBy(s => s.Order);
            foreach (var step in resolved)
            {
                if (step.StepType == StepType.Search)
                {
                    _effects.StartPieces(ctx, null, startAll: true);
                }
                else if (step.StepType == StepType.SFC_Split)
                {
                    _effects.ApplyDefaultSplit(ctx);
                }
                else if (step.StepType == StepType.Assembly)
                {
                    // Spec 014: lo ensamblado, con la Assembly Data por defecto.
                    CompleteAssembled(ctx);
                }
                else if (StepTypes.IsLogNC(step.StepType))
                {
                    // Spec 014: los hallazgos se regeneraron con las piezas nuevas en el Start; se
                    // aplica la disposición de cada paso de Log NC ya resuelto (NonSerialized: la
                    // fila del split por defecto).
                    _effects.ApplyFindingDisposition(ctx, ctx.NcFindingFor(step), null);
                }
                else if (step.StepType == StepType.Complete)
                {
                    // Spec 015 (D-22): las filas completadas ya no están en el POD.
                    _effects.RemoveRows(ctx, ctx.State.Order.ActiveRows());
                }
            }

            ctx.Rebuilt = true;
        }

        // Inicia una guía: la BD abandona las otras sesiones en curso del operador (D-9) y aquí
        // se descarta el estado del POD de las sesiones anteriores de este login, abandonadas
        // o completadas (auditoría AUD-41).
        // Valida antes de Start, que abandona las sesiones en curso (D-9): un inicio rechazado no
        // las toca (spec 021, AC-8). Guía inexistente o no publicada (B-20), otra área u operador
        // sin área o modo no definido (B-4): excepción, no intento.
        public TrainingSession StartSession(int userId, int guideId, SessionMode mode)
        {
            var guide = _catalog.GetById(guideId);
            TrainingAccessRule.EnsureCanStart(_userRepo.GetById(userId), guide, mode);
            var session = _sessionService.Start(userId, guide, mode);
            _store.RemoveAll();
            return session;
        }

        public void Abandon(PodContext ctx)
        {
            _sessionService.Abandon(ctx.Session.Id, ctx.UserId);
            _store.Remove(ctx.Session.Id);
        }

        // ── Comandos: cada uno vive en su colaborador (BLL/Training/Pod/) ──

        // Califica la respuesta del paso actual (Line Clearance, y la base del resto).
        public StepSubmitResult SubmitStep(PodContext ctx, string submittedAnswer)
            => _core.SubmitStep(ctx, submittedAnswer);

        // Ingreso a MES y limpieza al salir.
        public PodCommandResult SubmitMesAction(PodContext ctx, MesAction action,
            string code1 = null, string resource = null, string password = null)
            => _completion.SubmitMesAction(ctx, action, code1, resource, password);

        // Resource Setup, Line Clearance y Start.
        public PodCommandResult OpenResourceSetup(PodContext ctx)
            => _setup.OpenResourceSetup(ctx);

        public PodCommandResult OpenLineClearance(PodContext ctx)
            => _setup.OpenLineClearance(ctx);

        public StepSubmitResult SubmitResourceSetup(PodContext ctx, string answer, string shopOrderInput)
            => _setup.SubmitResourceSetup(ctx, answer, shopOrderInput);

        public PodCommandResult ClickStart(PodContext ctx, ICollection<string> browseRowIds, string scannedSfc)
            => _setup.ClickStart(ctx, browseRowIds, scannedSfc);

        public StepSubmitResult SubmitStartChecklist(PodContext ctx, string answerJson,
            ICollection<string> browseRowIds)
            => _setup.SubmitStartChecklist(ctx, answerJson, browseRowIds);

        // Assembly y Data Collection.
        public PodCommandResult OpenAssembly(PodContext ctx, ICollection<string> selectedRowIds)
            => _assemblyDc.OpenAssembly(ctx, selectedRowIds);

        public AssemblySubmitResult AssembleComponent(PodContext ctx, int componentId, string barcode)
            => _assemblyDc.AssembleComponent(ctx, componentId, barcode);

        public PodCommandResult OpenDataCollection(PodContext ctx, ICollection<string> selectedRowIds)
            => _assemblyDc.OpenDataCollection(ctx, selectedRowIds);

        public DataCollectionSubmitResult SubmitDataCollectionGroup(PodContext ctx,
            string groupCode, string answersJson)
            => _assemblyDc.SubmitDataCollectionGroup(ctx, groupCode, answersJson);

        // SFC Split.
        public PodCommandResult OpenSplit(PodContext ctx, ICollection<string> selectedRowIds)
            => _split.OpenSplit(ctx, selectedRowIds);

        public PodCommandResult Split(PodContext ctx, string rowId, decimal? qty)
            => _split.Split(ctx, rowId, qty);

        public PodCommandResult CloseSplit(PodContext ctx, string answer)
            => _split.CloseSplit(ctx, answer);

        // Log NC.
        public PodCommandResult OpenLogNC(PodContext ctx, ICollection<string> selectedRowIds)
            => _logNc.OpenLogNC(ctx, selectedRowIds);

        public PodCommandResult SubmitLogNC(PodContext ctx, string answer, ICollection<string> selectedRowIds)
            => _logNc.SubmitLogNC(ctx, answer, selectedRowIds);

        // Complete, Logout y las acciones del MES que no son un paso.
        public PodCommandResult Complete(PodContext ctx, ICollection<string> selectedRowIds)
            => _completion.Complete(ctx, selectedRowIds);

        public PodCommandResult ClickLogout(PodContext ctx)
            => _completion.ClickLogout(ctx);

        public PodCommandResult ClickNonStepAction(PodContext ctx, string action)
            => _completion.ClickNonStepAction(ctx, action);

        // Saltar paso e Ir a… (Práctica libre).
        public PodCommandResult Skip(PodContext ctx, int targetOrder)
            => _forced.Skip(ctx, targetOrder);
    }
}
