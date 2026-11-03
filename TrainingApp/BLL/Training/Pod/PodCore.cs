using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training.Pod
{
    // Núcleo del intento (AUD-74, E5): registrar el intento y el avance o cierre juntos
    // (spStepAttemptRecord, AUD-06), el estado del POD (SaveState es el único que lo persiste),
    // el validador del paso y las guardas de invariantes. Lo usan todos los comandos.
    internal sealed class PodCore
    {
        private readonly IPodStateStore             _store;
        private readonly IStepAttemptRepository     _attemptRepo;
        private readonly ITrainingSessionRepository _sessionRepo;
        private readonly ScoreCalculator            _scoreCalc;

        public PodCore(IPodStateStore store, IStepAttemptRepository attemptRepo,
            ITrainingSessionRepository sessionRepo, ScoreCalculator scoreCalc)
        {
            _store       = store;
            _attemptRepo = attemptRepo;
            _sessionRepo = sessionRepo;
            _scoreCalc   = scoreCalc;
        }

        // Califica la respuesta del paso actual (Line Clearance, y la base del resto). El paso lo
        // define el servidor, no la página: un paso viejo haría retroceder la sesión al avanzar
        // (auditoría AUD-05).
        // Resource Setup entra solo por SubmitResourceSetup, que valida sus invariantes (spec 020).
        internal StepSubmitResult SubmitStep(PodContext ctx, string submittedAnswer)
        {
            if (ctx.CurrentStepOrDefault?.StepType == StepType.ResourceSetup)
                throw new InvalidOperationException("Resource Setup se envía con SubmitResourceSetup.");
            return SubmitStep(ctx, submittedAnswer, null);
        }

        // selected: filas del POD seleccionadas al enviar (Log NC las califica, spec 014).
        internal StepSubmitResult SubmitStep(PodContext ctx, string submittedAnswer,
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

        internal static void EnsureInProgress(PodContext ctx)
        {
            if (ctx.Session.Status != SessionStatus.InProgress)
                throw new InvalidOperationException("La sesión no está en curso.");
        }

        // Validador del paso con el catálogo que ese paso usa: códigos NC en Log NC y
        // parámetros en Data Collection, del contexto del request (AUD-39). Sin caché entre
        // requests: un script de catálogo se ve en el siguiente click (AUD-13). Assembly no
        // pasa por el validador: AssembleComponent lo resuelve contra el BOM (spec 008).
        // Log NC recibe además el hallazgo del paso y las filas enviadas (spec 014).
        internal static StepValidator BuildValidator(PodContext ctx, StepType stepType,
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
                    ? ctx.State.PreloadedEquipment?.AllValues() : null,
                ResourceSetupType    = ctx.ResourceSetupType
            });
        }

        // Cada comando califica su propio paso: llamarlo en otro paso es un error de
        // programación o una manipulación, no un intento (auditoría AUD-05).
        internal static void EnsureCurrentStep(PodContext ctx, StepType expected)
        {
            if (ctx.CurrentStep.StepType != expected)
                throw new InvalidOperationException("El comando no corresponde al paso actual.");
        }

        // Acción equivocada del operador en el paso actual (sin fila válida): intento fallido.
        internal PodCommandResult FailedAttempt(PodContext ctx, PodAttemptReason reason, string action)
        {
            EnsureInProgress(ctx);
            return new PodCommandResult
            {
                Step          = RecordAttempt(ctx, ctx.CurrentStep, isCorrect: false,
                    AttemptReason.MissingSelection, action),
                FeedbackReason = reason
            };
        }

        internal void SaveState(PodContext ctx)
        {
            _store.Save(ctx.Session.Id, ctx.State);
        }

        // Assembly resuelto (el último Add, forzado o reconstruido): todo el BOM queda
        // registrado, en su orden, con la Assembly Data de hoy (Batch Number = Process Lot de la
        // orden; la de cada material llega con W13, E4).
        internal static void CompleteAssembled(PodContext ctx)
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
        // resolve: un intento no correcto que igual resuelve el paso (el salto de Práctica libre,
        // D-83); sigue el camino del avance forzado.
        internal StepSubmitResult RecordAttempt(PodContext ctx, TrainingStep step, bool isCorrect,
            AttemptReason failureReason, string action, bool resolve = false)
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

            bool forceAdvance = resolve || ctx.Policy.ForcesAdvanceAt(attemptNumber);

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
