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
            var ctx = new PodContext(userId, session, guide, area, attempts, state, score, _guideService);

            // Después de Resource Setup siempre hay orden: si falta, se perdió la Session.
            if (state.Order == null && ctx.IsResolved(StepType.ResourceSetup))
                RebuildState(ctx);
            return ctx;
        }

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
                else if (step.StepType == StepType.LogNC &&
                         ctx.Area.SerializationType == SerializationType.NonSerialized)
                {
                    // Log NC scrapea una sola fila, la del split (aquí, la del split por defecto);
                    // queda el lote con su cantidad restante. En Serialized no se sabe qué piezas
                    // se scrapearon: quedan todas para Complete.
                    var splitRow = ctx.State.Order.Rows.FirstOrDefault(r => r.IsFromSplit);
                    if (splitRow != null)
                    {
                        ctx.State.Order.Remove(new[] { splitRow });
                        SaveState(ctx);
                    }
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
        {
            EnsureInProgress(ctx);
            var step = ctx.CurrentStep;

            // Assembly y Data Collection se califican en AssembleComponent y
            // SubmitDataCollectionGroup: un envío por respuesta en esos pasos es un error de
            // programación, no un intento fallido (auditoría AUD-21).
            if (step.StepType == StepType.Assembly || step.StepType == StepType.DataCollection)
                throw new InvalidOperationException("El paso actual no se califica por respuesta.");

            bool isCorrect = BuildValidator(ctx, step.StepType).Validate(step, submittedAnswer);
            // Registro de actividad (spec 013): un botón equivocado dentro del paso es una acción
            // inválida; el resto, una decisión incorrecta.
            var reason = submittedAnswer == StepAnswers.Invalid
                ? AttemptReason.InvalidAction : AttemptReason.WrongDecision;
            return RecordAttempt(ctx, step, isCorrect, reason, AttemptActions.ForSubmit(step.StepType, submittedAnswer));
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
        private void StartPieces(PodContext ctx, ICollection<string> browseRowIds, bool startAll)
        {
            var order = ctx.State.Order;
            if (order == null || ctx.Area.SerializationType != SerializationType.Serialized) return;

            order.Activate(startAll || browseRowIds == null ? order.Rows : order.FindRows(browseRowIds));
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
        // la misma Shop Order (enmienda 2026-10-01 de la spec 005). No es un intento: el paso
        // se califica al cerrar.
        public PodCommandResult Split(PodContext ctx, string rowId, int qty)
        {
            if (ctx.CurrentStep.StepType != StepType.SFC_Split)
                return PodCommandResult.WithNotice(PodNotice.SplitNotActive);

            // Solo una fila del POD (Active): el id llega del cliente.
            var order = ctx.State.Order;
            var row   = order?.FindActiveRows(new[] { rowId }).FirstOrDefault();
            if (row == null || qty <= 0 || qty >= row.Qty)
                return PodCommandResult.WithNotice(PodNotice.InvalidSplit);

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

        // Abrir Log NC. Serialized: scrapear todas las piezas es un aviso (tiene que quedar una
        // para Complete, spec 008). Sin una fila scrapeable es un intento fallido del paso (spec
        // 007): no hay gate en el cliente, el click tiene que llegar acá para contar.
        public PodCommandResult OpenLogNC(PodContext ctx, ICollection<string> selectedRowIds)
        {
            var sequence = CheckSequence(ctx, StepType.LogNC, AttemptActions.LogNC);
            if (sequence != null) return sequence;

            var scrappable = ScrappableRows(ctx, selectedRowIds, out bool noPieceLeft);
            if (noPieceLeft)
                return PodCommandResult.WithNotice(PodNotice.NoPieceLeft);
            if (scrappable.Count == 0)
                return FailedAttempt(ctx, PodAttemptReason.NoScrappableRow, AttemptActions.LogNC);

            return new PodCommandResult();
        }

        // Add-Done de NC Data Entry (o un botón equivocado del modal): califica el código y,
        // resuelto el paso (correcto o forzado), scrapea las filas seleccionadas (spec 007).
        public PodCommandResult SubmitLogNC(PodContext ctx, string answer, ICollection<string> selectedRowIds)
        {
            EnsureCurrentStep(ctx, StepType.LogNC);
            var result = new PodCommandResult { Step = SubmitStep(ctx, answer) };
            if (result.Step.Outcome == StepSubmitOutcome.Incorrect) return result;

            var rows = ScrappableRows(ctx, selectedRowIds, out _);
            if (rows.Count > 0)
            {
                ctx.State.Order.Remove(rows);
                SaveState(ctx);
                result.Scrapped = rows;
            }
            return result;
        }

        // NonSerialized: exactamente una fila, creada por el SFC Split; la fila original del
        // lote no se scrapea (spec 007). Serialized: las filas seleccionadas, siempre que quede
        // al menos una pieza para Complete (spec 008); noPieceLeft avisa que no quedaría ninguna.
        private static IList<SimulatedOrderRow> ScrappableRows(PodContext ctx,
            ICollection<string> selectedRowIds, out bool noPieceLeft)
        {
            var selected = SelectedRows(ctx, selectedRowIds);
            noPieceLeft  = false;

            if (ctx.Area.SerializationType == SerializationType.Serialized)
            {
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

        // Ícono Logout del header (spec 015): en el último paso es su respuesta correcta y cierra
        // la sesión de entrenamiento con su resumen (simulado: la sesión de la app sigue).
        // Antes, es un botón más del POD (S2): intento fallido del paso actual en Assessment,
        // ignorado en Guided.
        public PodCommandResult ClickLogout(PodContext ctx)
            => CheckSequence(ctx, StepType.Logout, AttemptActions.Logout)
               ?? new PodCommandResult { Step = SubmitStep(ctx, StepAnswers.Logout) };

        // ── Acciones del MES que no son un paso ──────────────────────────

        // Work Instructions, Reports, Place Back in Queue, Tool List y las opciones de relleno
        // de Activities no tienen lógica: en Assessment el click es un intento fallido del paso
        // actual (S2); en Guided esos botones están deshabilitados. action: el botón (catálogo
        // de AttemptActions), para el registro de actividad.
        public PodCommandResult ClickNonStepAction(PodContext ctx, string action)
            => CheckSequence(ctx, null, action) ?? new PodCommandResult();

        // ── Secuencia y avance forzado ───────────────────────────────────

        // null si el botón es el del paso actual. Si no: en Assessment, intento fallido del paso
        // actual y, si avanzó forzado, el POD queda como si el paso se hubiera hecho; en
        // Guided (o con la sesión ya cerrada) el click se ignora.
        // action: el botón presionado, para el registro de actividad (spec 013, AC-20).
        private PodCommandResult CheckSequence(PodContext ctx, StepType? clickedStepType, string action)
        {
            var step = ctx.CurrentStep;
            if (step.StepType == clickedStepType) return null;

            var result = new PodCommandResult { OutOfSequence = true };
            if (ctx.Session.Mode != SessionMode.Assessment || ctx.Session.Status != SessionStatus.InProgress)
                return result;

            result.Step = RecordAttempt(ctx, step, isCorrect: false, AttemptReason.OutOfSequence, action);
            if (result.Step.Outcome == StepSubmitOutcome.FailedForced)
                result.PodChanged = RestoreAfterForcedAdvance(ctx, step.StepType);
            return result;
        }

        // Un paso forzado por errores de secuencia nunca abrió su control: el POD queda como si
        // el paso se hubiera hecho, para que el resto de la evaluación se pueda completar.
        // Assembly y DC no necesitan nada: RecordAttempt ya descartó su avance parcial.
        // Devuelve true si la página tiene que volver a pintar el POD (Resource Setup y Start
        // siempre; SFC Split solo si se aplicó el split por defecto; Complete si quitó filas).
        private bool RestoreAfterForcedAdvance(PodContext ctx, StepType forcedStep)
        {
            switch (forcedStep)
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
        private bool ApplyDefaultSplit(PodContext ctx)
        {
            var order = ctx.State.Order;
            if (order == null || order.HasSplit) return false;

            var lot = order.Rows.FirstOrDefault(r => !r.IsFromSplit && r.Qty > PodDefaults.DefaultSplitQty);
            if (lot == null) return false;

            order.Split(lot, PodDefaults.DefaultSplitQty);
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
        private static StepValidator BuildValidator(PodContext ctx, StepType stepType)
        {
            return new StepValidator(new StepValidationContext
            {
                Area                 = ctx.Area,
                ValidNCCodes         = stepType == StepType.LogNC
                    ? ctx.NCCodes : new List<NCCode>(),
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

        private void DiscardProgress(PodContext ctx, StepType resolvedStep)
        {
            if (resolvedStep == StepType.ResourceSetup)
                ctx.State.PreloadedEquipment = null;
            else if (resolvedStep == StepType.Assembly)
                ctx.State.Assembly = null;
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

            bool forceAdvance = ScoreCalculator.ShouldForceAdvance(attemptNumber, session.Mode);

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
