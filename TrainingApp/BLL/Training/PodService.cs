using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

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
    public class PodService
    {
        private readonly IPodStateStore             _store;
        private readonly TrainingSessionRepository  _sessionRepo    = new TrainingSessionRepository();
        private readonly StepAttemptRepository      _attemptRepo    = new StepAttemptRepository();
        private readonly UserRepository             _userRepo       = new UserRepository();
        private readonly TrainingGuideService       _guideService   = new TrainingGuideService();
        private readonly TrainingSessionService     _sessionService = new TrainingSessionService();
        private readonly ScoreCalculator            _scoreCalc      = new ScoreCalculator();

        public PodService(IPodStateStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
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

            var guide    = _guideService.GetById(session.TrainingGuideId);
            var area     = _guideService.GetArea(guide.AreaId);
            var attempts = _attemptRepo.GetBySessionId(sessionId);

            var state = _store.Load(sessionId);
            if (state == null)
            {
                state = new PodState();
                _store.Save(sessionId, state);
            }

            decimal score = _scoreCalc.CalculateSessionScore(guide.Steps, attempts, session.Mode);
            var ctx = new PodContext(userId, session, guide, area, attempts, state, score, _guideService,
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

        private static bool HasLogNC(TrainingGuide guide)
            => guide.Steps.Any(s => StepTypes.IsLogNC(s.StepType));

        // Reconstrucción (D-10): la orden por defecto con los efectos de los pasos ya
        // resueltos, como si cada uno hubiera avanzado forzado. Sin aviso, sin intento y sin
        // tocar el puntaje. Assembly/DC a medias empiezan de cero (su avance se perdió).
        private void RebuildState(PodContext ctx)
        {
            CreateOrder(ctx, PodDefaults.DefaultShopOrder);

            var resolved = ctx.Guide.Steps
                .Where(s => s.Order < ctx.Session.CurrentStepOrder)
                .OrderBy(s => s.Order);
            foreach (var step in resolved)
            {
                if (step.StepType == StepType.Search)
                {
                    StartPieces(ctx, null, startAll: true);
                }
                else if (step.StepType == StepType.SFC_Split)
                {
                    ApplyDefaultSplit(ctx);
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
                    ApplyFindingDisposition(ctx, ctx.NcFindingFor(step), null);
                }
                else if (step.StepType == StepType.Complete)
                {
                    // Spec 015 (D-22): las filas completadas ya no están en el POD.
                    RemoveRows(ctx, ctx.State.Order.ActiveRows());
                }
            }

            ctx.Rebuilt = true;
        }

        // Inicia una guía: la BD abandona las otras sesiones en curso del operador (D-9) y aquí
        // se descarta el estado del POD de las sesiones anteriores de este login, abandonadas
        // o completadas (auditoría AUD-41).
        public TrainingSession StartSession(int userId, int guideId, SessionMode mode)
        {
            var session = _sessionService.Start(userId, guideId, mode);
            _store.RemoveAll();
            return session;
        }

        public void Abandon(PodContext ctx)
        {
            _sessionService.Abandon(ctx.Session.Id, ctx.UserId);
            _store.Remove(ctx.Session.Id);
        }

        // Califica la respuesta del paso actual (Line Clearance, y la base del resto). El paso lo
        // define el servidor, no la página: un paso viejo haría retroceder la sesión al avanzar
        // (auditoría AUD-05).
        public StepSubmitResult SubmitStep(PodContext ctx, string submittedAnswer)
            => SubmitStep(ctx, submittedAnswer, null);

        // selected: filas del POD seleccionadas al enviar (Log NC las califica, spec 014).
        private StepSubmitResult SubmitStep(PodContext ctx, string submittedAnswer,
            IList<SimulatedOrderRow> selected)
        {
            EnsureInProgress(ctx);
            var step = ctx.CurrentStep;

            // Assembly y Data Collection se califican en AssembleComponent y
            // SubmitDataCollectionGroup: un envío por respuesta en esos pasos es un error de
            // programación, no un intento fallido (auditoría AUD-21). El Ingreso a MES se
            // califica por pantalla en SubmitMesAction (spec 016).
            if (step.StepType == StepType.Assembly || step.StepType == StepType.DataCollection ||
                step.StepType == StepType.MesLogin || step.StepType == StepType.Logout)
                throw new InvalidOperationException("El paso actual no se califica por respuesta.");

            bool isCorrect = BuildValidator(ctx, step.StepType, selected).Validate(step, submittedAnswer);
            // Registro de actividad (spec 013): un botón equivocado dentro del paso es una acción
            // inválida; el resto, una decisión incorrecta.
            var reason = submittedAnswer == StepAnswers.Invalid
                ? AttemptReason.InvalidAction : AttemptReason.WrongDecision;
            return RecordAttempt(ctx, step, isCorrect, reason, AttemptActions.ForSubmit(step.StepType, submittedAnswer));
        }

        // ── Ingreso a MES (spec 016) ─────────────────────────────────────

        // Cada pantalla del Ingreso se califica al enviarla (D-68, como el Split de Moldeo):
        // MesLoginRule decide y aquí se aplica. Un fallo registra el intento y dice a qué
        // pantalla volver (P-4); el 4.º en Assessment fuerza el avance y aparece el POD (P-5).
        // Abrir el Repositorio antes del script es de secuencia: en Guided se ignora, como un
        // botón fuera de secuencia (S2). Un envío fuera del paso (doble envío tras Log On) se
        // ignora. Uno que no corresponde al avance guardado (la Session se perdió, otra pestaña
        // o un envío manipulado) no registra nada y vuelve a la pantalla del avance. El Code1
        // contra el que se califica es el del usuario (D-79).
        public PodCommandResult SubmitMesAction(PodContext ctx, MesAction action,
            string code1 = null, string resource = null, string password = null)
        {
            if (ctx.IsLogoutCleanup)
                return SubmitLogoutCleanup(ctx, action, code1);

            if (ctx.Session.Status != SessionStatus.InProgress ||
                ctx.CurrentStepOrDefault?.StepType != StepType.MesLogin)
                return new PodCommandResult();

            var progress = ctx.MesLogin;
            var outcome  = MesLoginRule.Evaluate(progress, action, ctx.Code1, code1, resource, password);

            switch (outcome.Kind)
            {
                case MesOutcomeKind.Ignored:
                case MesOutcomeKind.Invalid:
                    return new PodCommandResult { MesScreen = progress.ResumeScreen() };

                case MesOutcomeKind.Failed:
                    return MesFailure(ctx, outcome, AttemptActions.ForMesAction(action));
            }

            if (action == MesAction.LogOn)
                return new PodCommandResult
                {
                    Step = RecordAttempt(ctx, ctx.CurrentStep, isCorrect: true, AttemptReason.None,
                        AttemptActions.ForMesAction(action))
                };

            progress.Apply(action);
            ctx.State.MesLogin = progress;
            SaveState(ctx);
            return new PodCommandResult { MesScreen = outcome.Screen };
        }

        // Fallo de una pantalla del Ingreso o de la limpieza: uno de secuencia (el Repositorio
        // fuera de su turno) en Guided se ignora sin intento, como un botón fuera de secuencia
        // (S2); el resto registra el intento y dice a qué pantalla volver (P-4).
        private PodCommandResult MesFailure(PodContext ctx, MesOutcome outcome, string action)
        {
            bool outOfSequence = outcome.Reason == AttemptReason.OutOfSequence;
            if (outOfSequence && ctx.Policy.OutOfSequence != OutOfSequenceHandling.RecordAttempt)
                return new PodCommandResult { OutOfSequence = true, MesScreen = outcome.Screen };

            return new PodCommandResult
            {
                OutOfSequence = outOfSequence,
                Step          = RecordAttempt(ctx, ctx.CurrentStep, isCorrect: false, outcome.Reason, action),
                MesScreen     = outcome.Screen
            };
        }

        // Limpieza al salir (spec 016, P-6), dentro del paso Logout: cerrar Edge lleva al
        // escritorio; el script con el Code1 propio cierra el paso y la sesión (el resumen sale
        // al cerrar la consola, P-12). Abrir el Repositorio (en Guided se ignora, como en el
        // Ingreso) o un Code1 ajeno es intento fallido de Logout; el 4.º en Assessment cierra la
        // sesión con su resumen, como antes. Aplica a toda sesión, sea cual sea la versión de
        // su guía: es del código.
        private PodCommandResult SubmitLogoutCleanup(PodContext ctx, MesAction action, string code1)
        {
            var progress = ctx.State.Logout;
            var outcome  = LogoutCleanupRule.Evaluate(progress, action, ctx.Code1, code1);

            switch (outcome.Kind)
            {
                case MesOutcomeKind.Ignored:
                case MesOutcomeKind.Invalid:
                    return new PodCommandResult { MesScreen = progress.ResumeScreen() };

                case MesOutcomeKind.Failed:
                    return MesFailure(ctx, outcome, AttemptActions.ForLogoutCleanup(action));
            }

            if (action == MesAction.RunScript)
                return new PodCommandResult
                {
                    Step      = RecordAttempt(ctx, ctx.CurrentStep, isCorrect: true, AttemptReason.None,
                        AttemptActions.ForLogoutCleanup(action)),
                    MesScreen = outcome.Screen
                };

            progress.Apply(action);
            SaveState(ctx);
            return new PodCommandResult { MesScreen = outcome.Screen };
        }

        // ── Resource Setup y Line Clearance ──────────────────────────────

        // Cada apertura genera otros equipos precargados (spec 015): Retrieve los muestra y el
        // Save no puede dejar ninguno. Viajan con el control, sin otro postback.
        public PodCommandResult OpenResourceSetup(PodContext ctx)
        {
            var sequence = CheckSequence(ctx, StepType.ResourceSetup, AttemptActions.ResourceSetup);
            if (sequence != null) return sequence;

            ctx.State.PreloadedEquipment = PreloadedEquipmentGenerator.Generate(new Random());
            SaveState(ctx);
            return new PodCommandResult();
        }

        // Line Clearance se abre desde Activities ("Data Collection Standalone").
        public PodCommandResult OpenLineClearance(PodContext ctx)
            => CheckSequence(ctx, StepType.LineClearance, AttemptActions.DCStandalone) ?? new PodCommandResult();

        // Resource Setup: califica el Setup State y, ya registrado el intento (AUD-06), crea la
        // orden con la Shop Order capturada, también en un intento fallido ("Open"), como
        // antes (spec 005). Si el paso avanzó forzado, la orden sale de la Shop Order genérica
        // e ignora lo capturado (S2 / spec 008, E8).
        // Con un equipo precargado en las filas, el control no aplica el formato (D-26, spec
        // 015): la Shop Order puede faltar y el intento se registra igual. Sin una Shop Order
        // válida y propia (no la del lote anterior), la orden no se crea: el paso sigue pendiente.
        public StepSubmitResult SubmitResourceSetup(PodContext ctx, string answer, string shopOrderInput)
        {
            EnsureCurrentStep(ctx, StepType.ResourceSetup);
            bool validShopOrder = SimulatedOrder.IsValidShopOrder(shopOrderInput) &&
                ctx.State.PreloadedEquipment?.Contains(shopOrderInput) != true;
            // El control ya lo avisa como formato; si llega mal es una manipulación o un error
            // de programación: no se registra nada (auditoría AUD-36).
            if (!validShopOrder && !BuildValidator(ctx, StepType.ResourceSetup).HasPreloadedValue(answer))
                throw new ArgumentException("La Shop Order debe ser numérica.", nameof(shopOrderInput));

            var result = SubmitStep(ctx, answer);
            if (result.Outcome == StepSubmitOutcome.FailedForced)
                CreateOrder(ctx, PodDefaults.DefaultShopOrder);
            else if (validShopOrder)
                CreateOrder(ctx, shopOrderInput);
            return result;
        }

        // Orden nueva de la sesión, que reemplaza a la anterior (spec 005). El avance de
        // Assembly y DC apuntaba a filas de la orden vieja: se descarta con ella.
        private void CreateOrder(PodContext ctx, string shopOrderInput)
        {
            ctx.State.Order = SimulatedOrder.Create(shopOrderInput,
                PodDefaults.MaterialDescription(ctx.Product, ctx.Area), ctx.Area.SerializationType);
            ctx.State.Assembly       = null;
            ctx.State.DataCollection = null;
            ctx.State.NcFindings     = null;
            ctx.State.AssembledComponents = null;
            SaveState(ctx);
        }

        // ── Start (Search/Start) ─────────────────────────────────────────

        // Click en Start. Avisos de formato, sin intento: sin orden (Resource Setup pendiente o
        // Session perdida); Serialized sin todas las piezas elegidas en Browse Work List (spec
        // 008); NonSerialized sin SFC o con uno que no es del lote (spec 011). Con checklist (Moldeo) pide abrirlo; sin checklist, el
        // Start se califica solo por secuencia y las piezas pasan a Active.
        public PodCommandResult ClickStart(PodContext ctx, ICollection<string> browseRowIds, string scannedSfc)
        {
            var sequence = CheckSequence(ctx, StepType.Search, AttemptActions.Start);
            if (sequence != null) return sequence;

            var order = ctx.State.Order;
            if (order == null)
                return PodCommandResult.WithNotice(PodNotice.ResourceSetupRequired);

            if (ctx.Area.SerializationType == SerializationType.Serialized)
            {
                if (!IsBrowseSelectionValid(order, browseRowIds))
                    return PodCommandResult.WithNotice(PodNotice.StartWholeOrder);
            }
            else if (string.IsNullOrWhiteSpace(scannedSfc))
            {
                return PodCommandResult.WithNotice(PodNotice.SfcRequired);
            }
            else if (!order.IsLotReference(scannedSfc))
            {
                // Spec 011 (enmienda E-2): TxtSFC es editable; el cliente ya lo avisa, esto
                // cubre una manipulación. Ningún lote ajeno se inicia.
                return PodCommandResult.WithNotice(PodNotice.SfcNotInOrder);
            }

            if (ctx.Area.RequiresStartChecklist)
                return new PodCommandResult { OpenChecklist = true };

            return new PodCommandResult { Step = SubmitStart(ctx, StepAnswers.Start, browseRowIds) };
        }

        // Checklist de Start (Moldeo, 4 modales): califica las respuestas y, resuelto el paso,
        // inicia las piezas. Solo se llega desde ClickStart, que ya validó la orden y la
        // selección; sin checklist, sin orden o con otra selección es una manipulación, no un
        // intento. La selección se vuelve a validar porque es la que se inicia (AUD-37).
        public StepSubmitResult SubmitStartChecklist(PodContext ctx, string answerJson,
            ICollection<string> browseRowIds)
        {
            EnsureCurrentStep(ctx, StepType.Search);
            var order = ctx.State.Order;
            if (!ctx.Area.RequiresStartChecklist || order == null)
                throw new InvalidOperationException("El Start de esta área no tiene checklist pendiente.");
            if (ctx.Area.SerializationType == SerializationType.Serialized &&
                !IsBrowseSelectionValid(order, browseRowIds))
                throw new InvalidOperationException("La selección de piezas no es la que validó el Start.");

            return SubmitStart(ctx, answerJson, browseRowIds);
        }

        // Serialized (spec 008): el Start exige todas las piezas elegidas en Browse Work List.
        private static bool IsBrowseSelectionValid(SimulatedOrder order, ICollection<string> browseRowIds)
            => PieceSelectionRule.Evaluate(StepType.Search,
                   order.FindRows(browseRowIds).Count, order.Rows.Count) == PieceSelectionResult.Valid;

        private StepSubmitResult SubmitStart(PodContext ctx, string answer, ICollection<string> browseRowIds)
        {
            var result = SubmitStep(ctx, answer);
            if (result.Outcome != StepSubmitOutcome.Incorrect)
                StartPieces(ctx, browseRowIds, startAll: result.Outcome == StepSubmitOutcome.FailedForced);
            return result;
        }

        // Piezas iniciadas con el Start. Serialized: las elegidas en Browse Work List (todas si
        // el paso avanzó forzado, E9 de la spec 008). NonSerialized: el lote ya nace Active.
        // Spec 014: con el Start se generan los hallazgos de los pasos de Log NC.
        private void StartPieces(PodContext ctx, ICollection<string> browseRowIds, bool startAll)
        {
            var order = ctx.State.Order;
            if (order == null) return;

            bool serialized = ctx.Area.SerializationType == SerializationType.Serialized;
            bool logNC = HasLogNC(ctx.Guide);
            if (!serialized && !logNC) return;

            if (serialized)
                order.Activate(startAll || browseRowIds == null ? order.Rows : order.FindRows(browseRowIds));

            if (logNC)
            {
                ctx.State.NcFindings = NcFindingGenerator.Generate(ctx.Guide.Steps, order.ActiveRows(),
                    ctx.Area.SerializationType, ctx.NCCodes,
                    ctx.Guide.Steps.Any(s => s.StepType == StepType.LogNCFastRepair) ? ctx.AssemblyComponents : null,
                    PodDefaults.NonSerializedLotQty, new Random());
                if (!serialized)
                    foreach (var finding in ctx.State.NcFindings)
                        finding.SFCCode = order.SFCCode;
            }
            SaveState(ctx);
        }

        // ── Assembly ─────────────────────────────────────────────────────

        // Abrir la Component List exige la selección del paso (aviso de formato, sin intento).
        public PodCommandResult OpenAssembly(PodContext ctx, ICollection<string> selectedRowIds)
        {
            return CheckSequence(ctx, StepType.Assembly, AttemptActions.Assemble)
                ?? PodCommandResult.WithNotice(CheckPodSelection(ctx, StepType.Assembly, selectedRowIds));
        }

        // Assembly (spec 008): ensambla un componente del BOM para todas las piezas
        // seleccionadas. El paso se completa (intento correcto) cuando todo el BOM quedó
        // ensamblado. No tiene camino incorrecto: se califica solo por secuencia (S2).
        public AssemblySubmitResult AssembleComponent(PodContext ctx, int componentId)
        {
            var result = new AssemblySubmitResult();
            if (ctx.Session.Status != SessionStatus.InProgress) return result;

            var step = ctx.CurrentStep;
            if (step.StepType != StepType.Assembly) return result;

            var bom = ctx.AssemblyComponents;
            if (!bom.Any(c => c.Id == componentId)) return result;

            // Con el último componente, el intento se guarda antes de tocar el avance: si el SP
            // falla, la Session no queda adelantada y el Add se puede repetir (AUD-06).
            // RecordAttempt descarta el avance al resolver el paso.
            var progress = ctx.State.Assembly ?? new AssemblyProgress();
            if (progress.CoversWith(componentId, bom))
            {
                result.StepResult = RecordAttempt(ctx, step, isCorrect: true,
                    AttemptReason.None, AttemptActions.AssemblyAdd);
            }
            else
            {
                progress.AssembledComponentIds.Add(componentId);
                ctx.State.Assembly = progress;
                SaveState(ctx);
            }

            result.ComponentAssembled = true;
            return result;
        }

        // ── Data Collection ──────────────────────────────────────────────

        // Abrir Data Collection exige la selección del paso (aviso de formato, sin intento). La
        // primera apertura fija las piezas a recorrer; al reabrir se sigue donde quedó (AC-9,
        // spec 008).
        public PodCommandResult OpenDataCollection(PodContext ctx, ICollection<string> selectedRowIds)
        {
            var sequence = CheckSequence(ctx, StepType.DataCollection, AttemptActions.DCCollect);
            if (sequence != null) return sequence;

            var notice = CheckPodSelection(ctx, StepType.DataCollection, selectedRowIds);
            if (notice == PodNotice.None && ctx.State.DataCollection == null)
                StartDataCollection(ctx, selectedRowIds);
            return PodCommandResult.WithNotice(notice);
        }

        // Data Collection (spec 008): guarda un DC Group para la pieza actual del avance.
        //   - Valores incorrectos: intento fallido (penalización y force-advance normales); el
        //     avance se conserva salvo que el paso avance forzado (E5).
        //   - Correcto: la pieza se completa según DataCollectionCompletionRule y el avance
        //     pasa a la siguiente; con la última, se registra el intento correcto del paso.
        // Sin avance (DC no se abrió con OpenDataCollection) el Save no aplica.
        public DataCollectionSubmitResult SubmitDataCollectionGroup(PodContext ctx,
            string groupCode, string answersJson)
        {
            var result = new DataCollectionSubmitResult { Outcome = DataCollectionOutcome.Ignored };
            if (ctx.Session.Status != SessionStatus.InProgress) return result;

            var step = ctx.CurrentStep;
            if (step.StepType != StepType.DataCollection) return result;

            var progress = ctx.State.DataCollection;
            if (progress == null || progress.IsFinished) return result;

            var groups = ctx.DataCollectionGroups;
            var group  = groups.FirstOrDefault(g => g.Code == groupCode);
            if (group == null) return result;

            // Grupo ya guardado por la pieza actual: su Collect está deshabilitado, así que un
            // segundo Save solo llega manipulando el cliente y no cuenta (E3, enmienda 2026-09-29).
            if (progress.CurrentPieceSavedGroupIds.Contains(group.Id)) return result;

            var piece = progress.CurrentPiece;
            result.PieceSfcCode = piece.SFCCode;

            var validator = BuildValidator(ctx, StepType.DataCollection);
            // Nombre del grupo Trainer inválido (spec 015): el control lo bloquea como formato;
            // si llega es una manipulación y no cuenta (auditoría AUD-36).
            if (validator.HasInvalidPersonName(group.Id, answersJson))
                return new DataCollectionSubmitResult { Outcome = DataCollectionOutcome.Ignored };

            if (!validator.ValidateDataCollectionGroup(group.Id, answersJson))
            {
                // Avance forzado (E5): RecordAttempt descarta el avance parcial. Registro de
                // actividad: un valor vacío se distingue de uno fuera de rango (spec 013).
                var reason = validator.HasMissingDataCollectionValue(group.Id, answersJson)
                    ? AttemptReason.EmptyRequiredField : AttemptReason.WrongDecision;
                result.StepResult = RecordAttempt(ctx, step, isCorrect: false,
                    reason, AttemptActions.DataCollectionSave);
                result.Outcome = result.StepResult.Outcome == StepSubmitOutcome.Incorrect
                    ? DataCollectionOutcome.Incorrect
                    : DataCollectionOutcome.StepResolved;
                return result;
            }

            var savedAfter = new HashSet<int>(progress.SavedGroupIds(piece.RowId)) { group.Id };
            if (!DataCollectionCompletionRule.IsPieceComplete(savedAfter, groups))
            {
                progress.MarkSaved(piece.RowId, group.Id);
                SaveState(ctx);
                result.Outcome = DataCollectionOutcome.GroupSaved;
                return result;
            }

            // Con la última pieza, el intento se guarda antes de tocar el avance: si el SP falla,
            // la Session no queda adelantada y el Save se puede repetir (AUD-06). RecordAttempt
            // descarta el avance al resolver el paso.
            if (progress.CurrentIndex == progress.Pieces.Count - 1)
            {
                result.StepResult = RecordAttempt(ctx, step, isCorrect: true,
                    AttemptReason.None, AttemptActions.DataCollectionSave);
                result.Outcome = DataCollectionOutcome.StepResolved;
                return result;
            }

            progress.MarkSaved(piece.RowId, group.Id);
            progress.CurrentIndex++;
            SaveState(ctx);
            result.Outcome = DataCollectionOutcome.PieceCompleted;
            return result;
        }

        // Las piezas de DC son las filas seleccionadas del POD, en su orden (NonSerialized: el lote).
        private void StartDataCollection(PodContext ctx, ICollection<string> selectedRowIds)
        {
            var progress = new DataCollectionProgress();
            foreach (var row in SelectedRows(ctx, selectedRowIds))
                progress.Pieces.Add(new DataCollectionPiece { RowId = row.RowId, SFCCode = row.SFCCode });

            ctx.State.DataCollection = progress;
            SaveState(ctx);
        }

        // ── SFC Split ────────────────────────────────────────────────────

        // Abrir SFC Split exige una fila seleccionada del POD (aviso de formato, sin intento).
        public PodCommandResult OpenSplit(PodContext ctx, ICollection<string> selectedRowIds)
        {
            return CheckSequence(ctx, StepType.SFC_Split, AttemptActions.SFCSplit)
                ?? PodCommandResult.WithNotice(IsSingleRow(SelectedRows(ctx, selectedRowIds))
                    ? PodNotice.None
                    : PodNotice.SelectRowToSplit);
        }

        // Divide una fila; la cantidad tiene que dejar algo en la original. Crea un SFC nuevo de
        // la misma Shop Order (enmienda 2026-10-01 de la spec 005). El paso se resuelve al
        // cerrar. Con hallazgo (enmienda 3 de la spec 014), la cantidad es una decisión: otra que
        // no sea la del hallazgo es intento fallido y el split no se aplica (forzado: el split
        // de N); un segundo split es un aviso.
        public PodCommandResult Split(PodContext ctx, string rowId, int qty)
        {
            if (ctx.CurrentStep.StepType != StepType.SFC_Split)
                return PodCommandResult.WithNotice(PodNotice.SplitNotActive);

            // Solo una fila del POD (Active): el id llega del cliente.
            var order = ctx.State.Order;
            var row   = order?.FindActiveRows(new[] { rowId }).FirstOrDefault();
            if (row == null || qty <= 0 || qty >= row.Qty)
                return PodCommandResult.WithNotice(PodNotice.InvalidSplit);

            var finding = ctx.SplitFinding;
            if (finding != null)
            {
                if (order.HasSplit)
                    return PodCommandResult.WithNotice(PodNotice.SplitAlreadyDone);
                if (qty != finding.UnitCount)
                {
                    EnsureInProgress(ctx);
                    var result = new PodCommandResult
                    {
                        Step = RecordAttempt(ctx, ctx.CurrentStep, isCorrect: false,
                            AttemptReason.WrongDecision, AttemptActions.SFCSplitSplit)
                    };
                    if (result.Step.Outcome == StepSubmitOutcome.FailedForced)
                        result.PodChanged = ApplyDefaultSplit(ctx);
                    return result;
                }
            }

            string newSfc = order.Split(row, qty);
            SaveState(ctx);
            return new PodCommandResult { NewSfcCode = newSfc, RemainingQty = row.Qty };
        }

        // Close del SFC Split: hace falta al menos un split; sin él es un aviso, sin intento.
        public PodCommandResult CloseSplit(PodContext ctx, string answer)
        {
            EnsureCurrentStep(ctx, StepType.SFC_Split);
            if (ctx.State.Order == null || !ctx.State.Order.HasSplit)
                return PodCommandResult.WithNotice(PodNotice.SplitRequired);

            return new PodCommandResult { Step = SubmitStep(ctx, answer) };
        }

        // ── Log NC ───────────────────────────────────────────────────────

        // Abrir Log NC (el botón vale en cualquier paso de Log NC, spec 014 P-7). Serialized:
        // scrapear todas las piezas es un aviso (tiene que quedar una para Complete, spec 008).
        // Sin una fila scrapeable es un intento fallido del paso (spec 007): no hay gate en el
        // cliente, el click tiene que llegar acá para contar. La pieza no se califica al abrir
        // (P-3): abrir con otra pieza y salir con Return to POD no cuenta.
        public PodCommandResult OpenLogNC(PodContext ctx, ICollection<string> selectedRowIds)
        {
            var sequence = CheckSequence(ctx, StepType.LogNC, AttemptActions.LogNC);
            if (sequence != null) return sequence;

            var finding = EnsureFinding(ctx);
            var scrappable = ScrappableRows(ctx, selectedRowIds, finding.Method, out bool noPieceLeft);
            if (noPieceLeft)
                return PodCommandResult.WithNotice(PodNotice.NoPieceLeft);
            if (scrappable.Count == 0)
            {
                var failed = FailedAttempt(ctx, PodAttemptReason.NoScrappableRow, AttemptActions.LogNC);
                // P-5: el paso forzado aplica la disposición del hallazgo.
                if (failed.Step.Outcome == StepSubmitOutcome.FailedForced)
                    ResolveFinding(ctx, finding, null, failed);
                return failed;
            }

            return new PodCommandResult();
        }

        // Add-Done de NC Data Entry (o un botón equivocado del modal). Spec 014: califica la
        // pieza y el código contra el hallazgo del paso (StepValidator). Resuelto (correcto o
        // forzado, P-5) se aplica la disposición del hallazgo.
        public PodCommandResult SubmitLogNC(PodContext ctx, string answer, ICollection<string> selectedRowIds)
        {
            EnsureInProgress(ctx);
            if (!StepTypes.IsLogNC(ctx.CurrentStep.StepType))
                throw new InvalidOperationException("El comando no corresponde al paso actual.");
            var finding = EnsureFinding(ctx);

            var selected = SelectedRows(ctx, selectedRowIds);
            var result = new PodCommandResult { Step = SubmitStep(ctx, answer, selected) };
            if (result.Step.Outcome == StepSubmitOutcome.Incorrect) return result;

            ResolveFinding(ctx, finding, selected, result);
            return result;
        }

        // Paso de Log NC resuelto: aplica la disposición del hallazgo y deja en el resultado lo
        // que la página muestra. Desecho: las filas que salieron. Reparación rápida (D-47): la
        // pieza sigue. Extendida (D-54): la operación del banner; Return to any previous ya sacó
        // la pieza del POD.
        private void ResolveFinding(PodContext ctx, NcFinding finding, IList<SimulatedOrderRow> selected,
            PodCommandResult result)
        {
            var removed = ApplyFindingDisposition(ctx, finding, selected);
            switch (finding.Method)
            {
                case NCMethod.FastScrap:
                    result.Scrapped = removed;
                    break;
                case NCMethod.FastRepair:
                    result.Repaired = finding;
                    break;
                case NCMethod.ExtendedRepair:
                    result.Reworked        = finding;
                    result.ReworkOperation = finding.Disposition == NcDisposition.ReturnToPrevious
                        ? ctx.PreviousOperationName : ctx.CurrentOperationName;
                    break;
            }
        }

        // El hallazgo del paso actual de Log NC, con su pieza en el POD. Faltar es un error de
        // invariante (spec 014, Edge Cases): los hallazgos son de piezas distintas y Complete
        // va después. No es un intento.
        private static NcFinding EnsureFinding(PodContext ctx)
        {
            var finding = ctx.NcFindingFor(ctx.CurrentStep);
            if (finding == null)
                throw new InvalidOperationException("El paso de Log NC no tiene hallazgo.");
            if (finding.RowId != null && ctx.State.Order?.FindActiveRows(new[] { finding.RowId }).Count != 1)
                throw new InvalidOperationException("La pieza del hallazgo ya no está en el POD.");
            return finding;
        }

        // Disposición del hallazgo (spec 014, P-4): el desecho rápido y Return to any previous
        // sacan su pieza del POD (D-54); la reparación rápida y el Local Rework la dejan (D-47).
        // La pieza es la enviada si es la del hallazgo (StepValidator.IsFindingPiece); si no
        // (avance forzado y reconstrucción), la del hallazgo o, en NonSerialized, la primera fila
        // del split. Devuelve las filas que salieron.
        private IList<SimulatedOrderRow> ApplyFindingDisposition(PodContext ctx, NcFinding finding,
            IList<SimulatedOrderRow> selected)
        {
            var order = ctx.State.Order;
            if (finding == null || order == null || !RemovesPiece(finding))
                return new List<SimulatedOrderRow>();

            var piece = StepValidator.IsFindingPiece(finding, selected)
                ? selected[0]
                : finding.RowId != null
                    ? order.FindActiveRows(new[] { finding.RowId }).FirstOrDefault()
                    : order.ActiveRows().FirstOrDefault(r => r.IsFromSplit);

            return piece == null
                ? new List<SimulatedOrderRow>()
                : RemoveRows(ctx, new List<SimulatedOrderRow> { piece });
        }

        private static bool RemovesPiece(NcFinding finding)
            => finding.Method == NCMethod.FastScrap ||
               (finding.Method == NCMethod.ExtendedRepair && finding.Disposition == NcDisposition.ReturnToPrevious);

        // NonSerialized: exactamente una fila, creada por el SFC Split; la fila original del
        // lote no se scrapea (spec 007). Serialized: las filas seleccionadas, siempre que quede
        // al menos una pieza para Complete (spec 008); noPieceLeft avisa que no quedaría ninguna.
        // Spec 014: la regla de dejar una pieza para Complete es del desecho. Las reparaciones
        // califican exactamente la pieza del hallazgo, y aunque Return to any previous la saque
        // del POD, NcFindingGenerator deja siempre una pieza para Complete.
        private static IList<SimulatedOrderRow> ScrappableRows(PodContext ctx,
            ICollection<string> selectedRowIds, NCMethod method, out bool noPieceLeft)
        {
            var selected = SelectedRows(ctx, selectedRowIds);
            noPieceLeft  = false;

            if (ctx.Area.SerializationType == SerializationType.Serialized)
            {
                if (method != NCMethod.FastScrap)
                    return selected;
                var selection = PieceSelectionRule.Evaluate(StepType.LogNC, selected.Count, ActiveCount(ctx));
                noPieceLeft = selection == PieceSelectionResult.NoPieceLeft;
                return selection == PieceSelectionResult.Valid ? selected : new List<SimulatedOrderRow>();
            }

            return IsSingleRow(selected) && selected[0].IsFromSplit
                ? selected
                : new List<SimulatedOrderRow>();
        }

        // ── Complete ─────────────────────────────────────────────────────

        // Complete cierra las filas que quedan en el POD. Serialized (spec 008): algunas piezas
        // es un aviso, sin intento; ninguna es un intento fallido. NonSerialized: sin la fila
        // seleccionada es un intento fallido (spec 007).
        public PodCommandResult Complete(PodContext ctx, ICollection<string> selectedRowIds)
        {
            var sequence = CheckSequence(ctx, StepType.Complete, AttemptActions.Complete);
            if (sequence != null) return sequence;

            var selected = SelectedRows(ctx, selectedRowIds);
            if (ctx.Area.SerializationType == SerializationType.Serialized)
            {
                var selection = PieceSelectionRule.Evaluate(StepType.Complete, selected.Count, ActiveCount(ctx));
                if (selection == PieceSelectionResult.MissingPieces)
                    return PodCommandResult.WithNotice(PodNotice.SelectAllPieces);
                if (PieceSelectionRule.CountsAsFailedAttempt(StepType.Complete, selection))
                    return FailedComplete(ctx);
            }
            else if (!IsSingleRow(selected))
            {
                return FailedComplete(ctx);
            }

            // Spec 015 (D-22): Complete ya no cierra la sesión (sigue Logout); las filas
            // completadas salen del POD, como el SFC que pasa a la siguiente operación.
            var result = new PodCommandResult { Step = SubmitStep(ctx, StepAnswers.Complete) };
            if (result.Step.Outcome != StepSubmitOutcome.Incorrect)
                result.Completed = RemoveRows(ctx, selected);
            return result;
        }

        // Complete sin la selección del paso: intento fallido. Si avanza forzado, el POD queda
        // como tras un Complete hecho (D-22): sin filas.
        private PodCommandResult FailedComplete(PodContext ctx)
        {
            var result = FailedAttempt(ctx, PodAttemptReason.NoRowToComplete, AttemptActions.Complete);
            if (result.Step.Outcome == StepSubmitOutcome.FailedForced)
                result.Completed = RemoveRows(ctx, ctx.State.Order?.ActiveRows() ?? new List<SimulatedOrderRow>());
            return result;
        }

        private IList<SimulatedOrderRow> RemoveRows(PodContext ctx, IList<SimulatedOrderRow> rows)
        {
            if (rows.Count == 0) return rows;
            ctx.State.Order.Remove(rows);
            SaveState(ctx);
            return rows;
        }

        // ── Logout ───────────────────────────────────────────────────────

        // Ícono Logout del header (spec 015): antes del último paso es un botón más del POD (S2):
        // intento fallido del paso actual en Assessment, ignorado en Guided. En el último paso
        // ya no cierra la sesión (spec 016, D-65): empieza la limpieza ("You have logged off"),
        // sin intento; el paso lo cierra el script (SubmitMesAction). Un segundo clic no cambia
        // nada.
        public PodCommandResult ClickLogout(PodContext ctx)
        {
            var sequence = CheckSequence(ctx, StepType.Logout, AttemptActions.Logout);
            if (sequence != null) return sequence;

            if (ctx.State.Logout == null)
            {
                ctx.State.Logout = new LogoutProgress();
                SaveState(ctx);
            }
            return new PodCommandResult { MesScreen = ctx.State.Logout.ResumeScreen() };
        }

        // ── Acciones del MES que no son un paso ──────────────────────────

        // Work Instructions, Reports, Place Back in Queue, Tool List y las opciones de relleno
        // de Activities no tienen lógica: en Assessment el click es un intento fallido del paso
        // actual (S2); en Guided esos botones están deshabilitados. action: el botón (catálogo
        // de AttemptActions), para el registro de actividad.
        public PodCommandResult ClickNonStepAction(PodContext ctx, string action)
            => OutOfSequence(ctx, action, ctx.Policy.NoLogicButtons == NoLogicButtonHandling.RecordAttempt);

        // ── Secuencia y avance forzado ───────────────────────────────────

        // null si el botón es el del paso actual. Si no: según ModePolicy, intento fallido del
        // paso actual (Assessment) o se ignora (Guided).
        // action: el botón presionado, para el registro de actividad (spec 013, AC-20).
        private PodCommandResult CheckSequence(PodContext ctx, StepType clickedStepType, string action)
        {
            var step = ctx.CurrentStep;
            if (step.StepType == clickedStepType) return null;
            // Spec 014 (P-7): el botón [Log NC] abre cualquiera de los pasos de Log NC.
            if (StepTypes.IsLogNC(clickedStepType) && StepTypes.IsLogNC(step.StepType))
                return null;

            return OutOfSequence(ctx, action, ctx.Policy.OutOfSequence == OutOfSequenceHandling.RecordAttempt);
        }

        // Click que no es el del paso actual. Con recordAttempt, intento fallido del paso actual
        // y, si avanzó forzado, el POD queda como si el paso se hubiera hecho; si no (o con la
        // sesión ya cerrada) el click se ignora.
        private PodCommandResult OutOfSequence(PodContext ctx, string action, bool recordAttempt)
        {
            var step   = ctx.CurrentStep;
            var result = new PodCommandResult { OutOfSequence = true };
            if (!recordAttempt || ctx.Session.Status != SessionStatus.InProgress)
                return result;

            result.Step = RecordAttempt(ctx, step, isCorrect: false, AttemptReason.OutOfSequence, action);
            if (result.Step.Outcome == StepSubmitOutcome.FailedForced)
                result.PodChanged = RestoreAfterForcedAdvance(ctx, step);
            return result;
        }

        // Un paso forzado por errores de secuencia nunca abrió su control: el POD queda como si
        // el paso se hubiera hecho, para que el resto de la evaluación se pueda completar.
        // Assembly y DC no necesitan nada: RecordAttempt ya descartó su avance parcial.
        // Devuelve true si la página tiene que volver a pintar el POD (Resource Setup y Start
        // siempre; SFC Split solo si se aplicó el split por defecto; Complete si quitó filas).
        private bool RestoreAfterForcedAdvance(PodContext ctx, TrainingStep forced)
        {
            // Spec 014 (P-5): Log NC forzado aplica la disposición de su hallazgo.
            if (StepTypes.IsLogNC(forced.StepType))
                return ApplyFindingDisposition(ctx, ctx.NcFindingFor(forced), null).Count > 0;

            switch (forced.StepType)
            {
                case StepType.ResourceSetup:
                    // La orden sale de la Shop Order genérica (S2 / spec 008, E8).
                    CreateOrder(ctx, PodDefaults.DefaultShopOrder);
                    return true;

                case StepType.Search:
                    // E9 (spec 008): Start forzado inicia todas las piezas.
                    StartPieces(ctx, null, startAll: true);
                    return true;

                case StepType.SFC_Split:
                    return ApplyDefaultSplit(ctx);

                case StepType.Complete:
                    // Spec 015 (D-22): como un Complete hecho, las filas salen del POD.
                    var active = ctx.State.Order?.ActiveRows() ?? new List<SimulatedOrderRow>();
                    return RemoveRows(ctx, active).Count > 0;

                default:
                    return false;
            }
        }

        // SFC Split forzado sin ningún split hecho (auditoría AUD-03, decisión D-1): queda una
        // fila de split por defecto, porque en NonSerialized Log NC exige scrapear una.
        // Enmienda 2 de la spec 014: con hallazgo, la cantidad es la suya (también al reconstruir).
        private bool ApplyDefaultSplit(PodContext ctx)
        {
            var order = ctx.State.Order;
            if (order == null || order.HasSplit) return false;

            int qty = ctx.SplitFinding?.UnitCount > 0 ? ctx.SplitFinding.UnitCount : PodDefaults.DefaultSplitQty;
            var lot = order.Rows.FirstOrDefault(r => !r.IsFromSplit && r.Qty > qty);
            if (lot == null) return false;

            order.Split(lot, qty);
            SaveState(ctx);
            return true;
        }

        // ── Selección ────────────────────────────────────────────────────

        // Filas del POD que mandó el cliente: solo cuentan las que existen y están Active.
        private static IList<SimulatedOrderRow> SelectedRows(PodContext ctx, ICollection<string> rowIds)
            => ctx.State.Order?.FindActiveRows(rowIds) ?? new List<SimulatedOrderRow>();

        private static int ActiveCount(PodContext ctx) => ctx.State.Order?.ActiveRows().Count ?? 0;

        // Pasos de una sola fila (NonSerialized): exactamente una fila seleccionada.
        private static bool IsSingleRow(IList<SimulatedOrderRow> selected) => selected.Count == 1;

        // Assembly y DC trabajan la selección del POD (spec 008, punto 6): en Serialized todas
        // las piezas juntas; en NonSerialized, la fila del lote.
        private static PodNotice CheckPodSelection(PodContext ctx, StepType stepType,
            ICollection<string> selectedRowIds)
        {
            var selected = SelectedRows(ctx, selectedRowIds);
            if (ctx.Area.SerializationType == SerializationType.Serialized)
                return PieceSelectionRule.Evaluate(stepType, selected.Count, ActiveCount(ctx)) == PieceSelectionResult.Valid
                    ? PodNotice.None
                    : PodNotice.SelectAllPieces;

            return IsSingleRow(selected) ? PodNotice.None : PodNotice.SelectOneRow;
        }

        // ── Registro de intentos y estado ────────────────────────────────

        private static void EnsureInProgress(PodContext ctx)
        {
            if (ctx.Session.Status != SessionStatus.InProgress)
                throw new InvalidOperationException("La sesión no está en curso.");
        }

        // Validador del paso con el catálogo que ese paso usa: códigos NC en Log NC y
        // parámetros en Data Collection, del contexto del request (AUD-39). Sin caché entre
        // requests: un script de catálogo se ve en el siguiente click (AUD-13). Assembly no
        // pasa por el validador: AssembleComponent lo resuelve contra el BOM (spec 008).
        // Log NC recibe además el hallazgo del paso y las filas enviadas (spec 014).
        private static StepValidator BuildValidator(PodContext ctx, StepType stepType,
            IList<SimulatedOrderRow> selected = null)
        {
            bool logNC = StepTypes.IsLogNC(stepType);
            return new StepValidator(new StepValidationContext
            {
                Area                 = ctx.Area,
                ValidNCCodes         = logNC ? ctx.NCCodes : new List<NCCode>(),
                NcFinding            = logNC ? ctx.NcFindingFor(ctx.CurrentStep) : null,
                SelectedRows         = logNC ? selected : null,
                DataCollectionParams = stepType == StepType.DataCollection
                    ? ctx.DataCollectionParameters : new List<DataCollectionParameter>(),
                PreloadedValues      = stepType == StepType.ResourceSetup
                    ? ctx.State.PreloadedEquipment?.AllValues() : null
            });
        }

        // Cada comando califica su propio paso: llamarlo en otro paso es un error de
        // programación o una manipulación, no un intento (auditoría AUD-05).
        private static void EnsureCurrentStep(PodContext ctx, StepType expected)
        {
            if (ctx.CurrentStep.StepType != expected)
                throw new InvalidOperationException("El comando no corresponde al paso actual.");
        }

        // Acción equivocada del operador en el paso actual (sin fila válida): intento fallido.
        private PodCommandResult FailedAttempt(PodContext ctx, PodAttemptReason reason, string action)
        {
            EnsureInProgress(ctx);
            return new PodCommandResult
            {
                Step          = RecordAttempt(ctx, ctx.CurrentStep, isCorrect: false,
                    AttemptReason.MissingSelection, action),
                FeedbackReason = reason
            };
        }

        private void SaveState(PodContext ctx)
        {
            _store.Save(ctx.Session.Id, ctx.State);
        }

        // Assembly resuelto (el último Add, forzado o reconstruido): todo el BOM queda
        // registrado, en su orden, con la Assembly Data de hoy (Batch Number = Process Lot de la
        // orden; la de cada material llega con W13, E4).
        private static void CompleteAssembled(PodContext ctx)
        {
            ctx.State.AssembledComponents = AssembledComponent.Complete(ctx.State.AssembledComponents,
                ctx.AssemblyComponents, ctx.State.Order?.ProcessLot);
        }

        private void DiscardProgress(PodContext ctx, StepType resolvedStep)
        {
            if (resolvedStep == StepType.ResourceSetup)
                ctx.State.PreloadedEquipment = null;
            else if (resolvedStep == StepType.MesLogin)
                ctx.State.MesLogin = null;
            else if (resolvedStep == StepType.Logout)
                ctx.State.Logout = null;
            else if (resolvedStep == StepType.Assembly)
            {
                ctx.State.Assembly = null;
                CompleteAssembled(ctx);
            }
            else if (resolvedStep == StepType.DataCollection)
                ctx.State.DataCollection = null;
            else
                return;
            SaveState(ctx);
        }

        // El intento y el avance (o el cierre) se guardan juntos en una transacción
        // (spStepAttemptRecord, auditoría AUD-06): si falla, no queda un intento sin avance que
        // penalice el siguiente envío. Por eso el puntaje se calcula antes de guardar, con el
        // intento nuevo incluido. El contexto se actualiza solo después de guardar.
        // Registro de actividad (spec 013): cada intento guarda su motivo (None si es correcto)
        // y la acción del operador, en la misma transacción.
        private StepSubmitResult RecordAttempt(PodContext ctx, TrainingStep step, bool isCorrect,
            AttemptReason failureReason, string action)
        {
            // Un error de programación, no un intento: sin acción el intento se confundiría con
            // los anteriores a la spec 013, y una de más de 50 haría fallar el SP.
            if (string.IsNullOrEmpty(action) || action.Length > AttemptActions.MaxLength)
                throw new ArgumentException("Acción del registro de actividad inválida.", nameof(action));

            var session       = ctx.Session;
            int attemptNumber = ctx.Attempts.Count(a => a.StepId == step.Id) + 1;

            var attempt = new TrainingSessionStepAttempt
            {
                SessionId     = session.Id,
                StepId        = step.Id,
                AttemptNumber = attemptNumber,
                IsCorrect     = isCorrect,
                Timestamp     = DateTime.UtcNow,
                Reason        = isCorrect ? AttemptReason.None : failureReason,
                Action        = action
            };
            var attempts = new List<TrainingSessionStepAttempt>(ctx.Attempts) { attempt };
            decimal score = _scoreCalc.CalculateSessionScore(ctx.Guide.Steps, attempts, session.Mode);

            bool forceAdvance = ctx.Policy.ForcesAdvanceAt(attemptNumber);

            StepSubmitResult result;
            if (!isCorrect && !forceAdvance)
            {
                _attemptRepo.Record(attempt, step.Order, nextStepOrder: null, finalScore: null);
                result = new StepSubmitResult
                {
                    Outcome       = StepSubmitOutcome.Incorrect,
                    AttemptNumber = attemptNumber,
                    CurrentScore  = score
                };
            }
            else if (step.Order + 1 > ctx.Guide.Steps.Count)
            {
                _attemptRepo.Record(attempt, step.Order, nextStepOrder: null, finalScore: score);
                result = new StepSubmitResult
                {
                    Outcome       = StepSubmitOutcome.SessionCompleted,
                    AttemptNumber = attemptNumber,
                    CurrentScore  = score,
                    SessionScore  = score
                };
            }
            else
            {
                int nextOrder = step.Order + 1;
                _attemptRepo.Record(attempt, step.Order, nextStepOrder: nextOrder, finalScore: null);
                result = new StepSubmitResult
                {
                    Outcome       = forceAdvance ? StepSubmitOutcome.FailedForced : StepSubmitOutcome.Correct,
                    AttemptNumber = attemptNumber,
                    NextStepOrder = nextOrder,
                    CurrentScore  = score
                };
            }

            ctx.Attempts.Add(attempt);
            ctx.Score = score;

            // Paso resuelto (correcto, forzado o fin de la sesión): su avance parcial de Assembly
            // o DC, o los equipos precargados de Resource Setup, ya no se usan.
            if (result.Outcome != StepSubmitOutcome.Incorrect)
                DiscardProgress(ctx, step.StepType);

            if (result.Outcome == StepSubmitOutcome.SessionCompleted)
            {
                // El puntaje final se lee de la BD: la columna lo redondea a 2 decimales y la
                // pantalla final muestra el valor guardado.
                var stored = _sessionRepo.GetById(session.Id);
                session.Status      = stored.Status;
                session.FinalScore  = stored.FinalScore;
                session.CompletedAt = stored.CompletedAt;
            }
            else if (result.NextStepOrder > 0)
            {
                session.CurrentStepOrder = result.NextStepOrder;
            }

            return result;
        }
    }
}
