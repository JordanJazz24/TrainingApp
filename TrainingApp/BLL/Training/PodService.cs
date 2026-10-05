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
        public PodContext LoadContext(int sessionId, int operatorId)
        {
            var session = _sessionRepo.GetById(sessionId);
            if (session == null || session.OperatorId != operatorId ||
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
            return new PodContext(operatorId, session, guide, area, attempts, state, score);
        }

        // Resource Setup: califica el Setup State y, ya registrado el intento (AUD-06), crea la
        // orden con la Shop Order capturada, también en un intento fallido ("Open"), como
        // antes (spec 005). Si el paso avanzó forzado, la orden sale de la Shop Order genérica
        // e ignora lo capturado (S2 / spec 008, E8).
        public StepSubmitResult SubmitResourceSetup(PodContext ctx, string answer, string shopOrderInput)
        {
            var result = SubmitStep(ctx, answer);
            CreateOrder(ctx, result.Outcome == StepSubmitOutcome.FailedForced
                ? PodDefaults.DefaultShopOrder
                : shopOrderInput);
            return result;
        }

        // Orden nueva de la sesión, que reemplaza a la anterior (spec 005). El avance de
        // Assembly y DC apuntaba a filas de la orden vieja: se descarta con ella.
        public void CreateOrder(PodContext ctx, string shopOrderInput)
        {
            var product = _guideService.GetAllProducts().FirstOrDefault(p => p.Id == ctx.Area.ProductId);
            ctx.State.Order = SimulatedOrder.Create(shopOrderInput,
                PodDefaults.MaterialDescription(product, ctx.Area), ctx.Area.SerializationType);
            ctx.State.Assembly       = null;
            ctx.State.DataCollection = null;
            SaveState(ctx);
        }

        public void Abandon(PodContext ctx)
        {
            _sessionService.Abandon(ctx.Session.Id, ctx.OperatorId);
            _store.Remove(ctx.Session.Id);
        }

        // Califica la respuesta del paso actual. El paso lo define el servidor, no la página:
        // un paso viejo haría retroceder la sesión al avanzar (auditoría AUD-05).
        public StepSubmitResult SubmitStep(PodContext ctx, string submittedAnswer)
        {
            EnsureInProgress(ctx);
            var step = ctx.CurrentStep;

            // Assembly y Data Collection se califican en AssembleComponent y
            // SubmitDataCollectionGroup: un envío por respuesta en esos pasos es un error de
            // programación, no un intento fallido (auditoría AUD-21).
            if (step.StepType == StepType.Assembly || step.StepType == StepType.DataCollection)
                throw new InvalidOperationException("El paso actual no se califica por respuesta.");

            bool isCorrect = _guideService.BuildValidator(ctx.Area, step.StepType).Validate(step, submittedAnswer);
            return RecordAttempt(ctx, step, isCorrect);
        }

        // Assessment: un click en un botón del POD que no corresponde al paso actual
        // es un intento fallido del paso actual (misma penalización y force-advance).
        // clickedStepType null = acción de MES que no es ningún paso (Work Instructions, Reports…).
        // Devuelve null si no es un error de secuencia (Guided, o el botón es el del paso actual).
        public StepSubmitResult RegisterOutOfSequence(PodContext ctx, StepType? clickedStepType)
        {
            if (ctx.Session.Mode != SessionMode.Assessment || ctx.Session.Status != SessionStatus.InProgress)
                return null;

            var step = ctx.CurrentStep;
            if (step.StepType == clickedStepType)
                return null;

            return RecordAttempt(ctx, step, isCorrect: false);
        }

        // Assembly (spec 008): ensambla un componente del BOM para todas las piezas
        // seleccionadas. El paso se completa (intento correcto) cuando todo el BOM quedó
        // ensamblado. No tiene camino incorrecto: se califica solo por secuencia (S2).
        public AssemblySubmitResult AssembleComponent(PodContext ctx, int componentId,
            AssemblyProgress progress)
        {
            var result = new AssemblySubmitResult();
            if (ctx.Session.Status != SessionStatus.InProgress) return result;

            var step = ctx.CurrentStep;
            if (step.StepType != StepType.Assembly) return result;

            var bom = _guideService.GetAssemblyComponents(ctx.Area.Id);
            if (!bom.Any(c => c.Id == componentId)) return result;

            // Con el último componente, el intento se guarda antes de actualizar el avance: si el
            // SP falla, la Session no queda adelantada y el Add se puede repetir (AUD-06).
            if (progress.CoversWith(componentId, bom))
                result.StepResult = RecordAttempt(ctx, step, isCorrect: true);

            progress.AssembledComponentIds.Add(componentId);
            result.ComponentAssembled = true;
            return result;
        }

        // Data Collection (spec 008): guarda un DC Group para la pieza actual del avance.
        //   - Valores incorrectos: intento fallido (penalización y force-advance normales); el
        //     avance se conserva salvo que el paso avance forzado (E5).
        //   - Correcto: la pieza se completa según DataCollectionCompletionRule y el avance
        //     pasa a la siguiente; con la última, se registra el intento correcto del paso.
        public DataCollectionSubmitResult SubmitDataCollectionGroup(PodContext ctx,
            string groupCode, string answersJson, DataCollectionProgress progress)
        {
            var result = new DataCollectionSubmitResult { Outcome = DataCollectionOutcome.Ignored };
            if (ctx.Session.Status != SessionStatus.InProgress || progress == null || progress.IsFinished)
                return result;

            var step = ctx.CurrentStep;
            if (step.StepType != StepType.DataCollection) return result;

            var groups = _guideService.GetDataCollectionGroups(ctx.Area.Id);
            var group  = groups.FirstOrDefault(g => g.Code == groupCode);
            if (group == null) return result;

            // Grupo ya guardado por la pieza actual: su Collect está deshabilitado, así que un
            // segundo Save solo llega manipulando el cliente y no cuenta (E3, enmienda 2026-09-29).
            if (progress.CurrentPieceSavedGroupIds.Contains(group.Id)) return result;

            if (!_guideService.BuildValidator(ctx.Area, StepType.DataCollection)
                    .ValidateDataCollectionGroup(group.Id, answersJson))
            {
                result.StepResult = RecordAttempt(ctx, step, isCorrect: false);
                result.Outcome = result.StepResult.Outcome == StepSubmitOutcome.Incorrect
                    ? DataCollectionOutcome.Incorrect
                    : DataCollectionOutcome.StepResolved;
                return result;
            }

            var piece      = progress.CurrentPiece;
            var savedAfter = new HashSet<int>(progress.SavedGroupIds(piece.RowId)) { group.Id };
            if (!DataCollectionCompletionRule.IsPieceComplete(savedAfter, groups))
            {
                progress.MarkSaved(piece.RowId, group.Id);
                result.Outcome = DataCollectionOutcome.GroupSaved;
                return result;
            }

            // Con la última pieza, el intento se guarda antes de actualizar el avance: si el SP
            // falla, la Session no queda adelantada y el Save se puede repetir (AUD-06).
            bool isLastPiece = progress.CurrentIndex == progress.Pieces.Count - 1;
            if (isLastPiece)
                result.StepResult = RecordAttempt(ctx, step, isCorrect: true);

            progress.MarkSaved(piece.RowId, group.Id);
            progress.CurrentIndex++;
            result.Outcome = isLastPiece
                ? DataCollectionOutcome.StepResolved
                : DataCollectionOutcome.PieceCompleted;
            return result;
        }

        private static void EnsureInProgress(PodContext ctx)
        {
            if (ctx.Session.Status != SessionStatus.InProgress)
                throw new InvalidOperationException("La sesión no está en curso.");
        }

        private void SaveState(PodContext ctx)
        {
            _store.Save(ctx.Session.Id, ctx.State);
        }

        // El intento y el avance (o el cierre) se guardan juntos en una transacción
        // (spStepAttemptRecord, auditoría AUD-06): si falla, no queda un intento sin avance que
        // penalice el siguiente envío. Por eso el puntaje se calcula antes de guardar, con el
        // intento nuevo incluido. El contexto se actualiza solo después de guardar.
        private StepSubmitResult RecordAttempt(PodContext ctx, TrainingStep step, bool isCorrect)
        {
            var session       = ctx.Session;
            int attemptNumber = ctx.Attempts.Count(a => a.StepId == step.Id) + 1;

            var attempt = new TrainingSessionStepAttempt
            {
                SessionId     = session.Id,
                StepId        = step.Id,
                AttemptNumber = attemptNumber,
                IsCorrect     = isCorrect,
                Timestamp     = DateTime.UtcNow
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
