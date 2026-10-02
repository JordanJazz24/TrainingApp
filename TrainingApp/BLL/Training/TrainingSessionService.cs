using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    public class TrainingSessionService
    {
        private readonly TrainingSessionRepository _sessionRepo;
        private readonly TrainingGuideService _guideService;
        private readonly StepAttemptRepository _attemptRepo;
        private readonly ScoreCalculator _scoreCalc;

        public TrainingSessionService()
        {
            _sessionRepo  = new TrainingSessionRepository();
            _guideService = new TrainingGuideService();
            _attemptRepo  = new StepAttemptRepository();
            _scoreCalc    = new ScoreCalculator();
        }

        public TrainingSession Start(int operatorId, int guideId, SessionMode mode)
        {
            // La sesión que quedó a medias en esta guía se marca Abandoned y conserva sus
            // intentos para el instructor (auditoría AUD-01, decisión D-4).
            _sessionRepo.AbandonActiveByOperator(operatorId, guideId);

            var session = new TrainingSession
            {
                OperatorId       = operatorId,
                TrainingGuideId  = guideId,
                Mode             = mode,
                Status           = SessionStatus.InProgress,
                CurrentStepOrder = 1,
                FinalScore       = 0m,
                StartedAt        = DateTime.UtcNow
            };
            session.Id = _sessionRepo.Insert(session);
            return session;
        }

        public TrainingSession GetById(int sessionId)
        {
            return _sessionRepo.GetById(sessionId);
        }

        public void Abandon(int sessionId, int operatorId)
        {
            LoadOwnedSession(sessionId, operatorId);
            _sessionRepo.Abandon(sessionId);
        }

        public decimal GetCurrentScore(int sessionId)
        {
            var session  = _sessionRepo.GetById(sessionId);
            var guide    = _guideService.GetById(session.TrainingGuideId);
            var attempts = _attemptRepo.GetBySessionId(sessionId);
            return _scoreCalc.CalculateSessionScore(guide.Steps, attempts, session.Mode);
        }

        // Califica la respuesta del paso actual de la sesión. El paso lo define el servidor, no
        // la página: un paso viejo haría retroceder la sesión al avanzar (auditoría AUD-05).
        // LoadAndValidateSession ya descarta las sesiones que no están en curso.
        public StepSubmitResult SubmitStep(int sessionId, int operatorId, string submittedAnswer)
        {
            var session = LoadOwnedSession(sessionId, operatorId);
            if (session.Status != SessionStatus.InProgress)
                throw new InvalidOperationException("La sesión no está en curso.");

            var guide = _guideService.GetById(session.TrainingGuideId);
            var step  = guide.Steps.First(s => s.Order == session.CurrentStepOrder);

            var validator = _guideService.BuildValidatorForGuide(guide);
            bool isCorrect = validator.Validate(step, submittedAnswer);

            return RecordAttempt(session, guide, step, isCorrect);
        }

        // Assessment: un click en un botón del POD que no corresponde al paso actual
        // es un intento fallido del paso actual (misma penalización y force-advance).
        // clickedStepType null = acción de MES que no es ningún paso (Work Instructions, Reports…).
        // Devuelve null si no es un error de secuencia (Guided, o el botón es el del paso actual).
        public StepSubmitResult RegisterOutOfSequence(int sessionId, int operatorId, StepType? clickedStepType)
        {
            var session = LoadOwnedSession(sessionId, operatorId);
            if (session.Mode != SessionMode.Assessment || session.Status != SessionStatus.InProgress)
                return null;

            var guide = _guideService.GetById(session.TrainingGuideId);
            var step  = guide.Steps.First(s => s.Order == session.CurrentStepOrder);
            if (step.StepType == clickedStepType)
                return null;

            return RecordAttempt(session, guide, step, isCorrect: false);
        }

        // Assembly (spec 008): ensambla un componente del BOM para todas las piezas
        // seleccionadas. El paso se completa (intento correcto) cuando todo el BOM quedó
        // ensamblado. No tiene camino incorrecto: se califica solo por secuencia (S2).
        public AssemblySubmitResult AssembleComponent(int sessionId, int operatorId, int componentId,
            AssemblyProgress progress)
        {
            var result  = new AssemblySubmitResult();
            var session = LoadOwnedSession(sessionId, operatorId);
            if (session.Status != SessionStatus.InProgress) return result;

            var guide = _guideService.GetById(session.TrainingGuideId);
            var step  = guide.Steps.First(s => s.Order == session.CurrentStepOrder);
            if (step.StepType != StepType.Assembly) return result;

            var bom = _guideService.GetAssemblyComponents(guide.AreaId);
            if (!bom.Any(c => c.Id == componentId)) return result;

            // Con el último componente, el intento se guarda antes de actualizar el avance: si el
            // SP falla, la Session no queda adelantada y el Add se puede repetir (AUD-06).
            if (progress.CoversWith(componentId, bom))
                result.StepResult = RecordAttempt(session, guide, step, isCorrect: true);

            progress.AssembledComponentIds.Add(componentId);
            result.ComponentAssembled = true;
            return result;
        }

        // Data Collection (spec 008): guarda un DC Group para la pieza actual del avance.
        //   - Valores incorrectos: intento fallido (penalización y force-advance normales); el
        //     avance se conserva salvo que el paso avance forzado (E5).
        //   - Correcto: la pieza se completa según DataCollectionCompletionRule y el avance
        //     pasa a la siguiente; con la última, se registra el intento correcto del paso.
        public DataCollectionSubmitResult SubmitDataCollectionGroup(int sessionId, int operatorId,
            string groupCode, string answersJson, DataCollectionProgress progress)
        {
            var result  = new DataCollectionSubmitResult { Outcome = DataCollectionOutcome.Ignored };
            var session = LoadOwnedSession(sessionId, operatorId);
            if (session.Status != SessionStatus.InProgress || progress == null || progress.IsFinished)
                return result;

            var guide = _guideService.GetById(session.TrainingGuideId);
            var step  = guide.Steps.First(s => s.Order == session.CurrentStepOrder);
            if (step.StepType != StepType.DataCollection) return result;

            var groups = _guideService.GetDataCollectionGroups(guide.AreaId);
            var group  = groups.FirstOrDefault(g => g.Code == groupCode);
            if (group == null) return result;

            // Grupo ya guardado por la pieza actual: su Collect está deshabilitado, así que un
            // segundo Save solo llega manipulando el cliente y no cuenta (E3, enmienda 2026-09-29).
            if (progress.CurrentPieceSavedGroupIds.Contains(group.Id)) return result;

            if (!_guideService.BuildValidatorForGuide(guide).ValidateDataCollectionGroup(group.Id, answersJson))
            {
                result.StepResult = RecordAttempt(session, guide, step, isCorrect: false);
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
                result.StepResult = RecordAttempt(session, guide, step, isCorrect: true);

            progress.MarkSaved(piece.RowId, group.Id);
            progress.CurrentIndex++;
            result.Outcome = isLastPiece
                ? DataCollectionOutcome.StepResolved
                : DataCollectionOutcome.PieceCompleted;
            return result;
        }

        // Dueño de la sesión (auditoría AUD-05): la página ya lo valida, pero la BLL no confía
        // en ella. Si no se cumple es una manipulación o un error de programación, no un
        // intento, así que no se registra nada.
        private TrainingSession LoadOwnedSession(int sessionId, int operatorId)
        {
            var session = _sessionRepo.GetById(sessionId);
            if (session == null || session.OperatorId != operatorId)
                throw new InvalidOperationException("La sesión no existe o no pertenece al operador.");
            return session;
        }

        // El intento y el avance (o el cierre) se guardan juntos en una transacción
        // (spStepAttemptRecord, auditoría AUD-06): si falla, no queda un intento sin avance que
        // penalice el siguiente envío. Por eso el puntaje se calcula antes de guardar, con el
        // intento nuevo incluido.
        private StepSubmitResult RecordAttempt(TrainingSession session, TrainingGuide guide,
            TrainingStep step, bool isCorrect)
        {
            var attempts      = _attemptRepo.GetBySessionId(session.Id);
            int attemptNumber = attempts.Count(a => a.StepId == step.Id) + 1;

            var attempt = new TrainingSessionStepAttempt
            {
                SessionId     = session.Id,
                StepId        = step.Id,
                AttemptNumber = attemptNumber,
                IsCorrect     = isCorrect,
                Timestamp     = DateTime.UtcNow
            };
            attempts.Add(attempt);
            decimal score = _scoreCalc.CalculateSessionScore(guide.Steps, attempts, session.Mode);

            bool forceAdvance = ScoreCalculator.ShouldForceAdvance(attemptNumber, session.Mode);

            if (!isCorrect && !forceAdvance)
            {
                _attemptRepo.Record(attempt, step.Order, nextStepOrder: null, finalScore: null);
                return new StepSubmitResult
                {
                    Outcome       = StepSubmitOutcome.Incorrect,
                    AttemptNumber = attemptNumber,
                    CurrentScore  = score
                };
            }

            int nextOrder   = step.Order + 1;
            bool isLastStep = nextOrder > guide.Steps.Count;

            if (isLastStep)
            {
                _attemptRepo.Record(attempt, step.Order, nextStepOrder: null, finalScore: score);
                return new StepSubmitResult
                {
                    Outcome       = StepSubmitOutcome.SessionCompleted,
                    AttemptNumber = attemptNumber,
                    CurrentScore  = score,
                    SessionScore  = score
                };
            }

            _attemptRepo.Record(attempt, step.Order, nextStepOrder: nextOrder, finalScore: null);
            return new StepSubmitResult
            {
                Outcome       = forceAdvance ? StepSubmitOutcome.FailedForced : StepSubmitOutcome.Correct,
                AttemptNumber = attemptNumber,
                NextStepOrder = nextOrder,
                CurrentScore  = score
            };
        }
    }
}
