using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
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

        private static readonly MemoryCache _validatorCache = MemoryCache.Default;
        private const int ValidatorCacheTtlMinutes = 60;

        public TrainingSessionService()
        {
            _sessionRepo  = new TrainingSessionRepository();
            _guideService = new TrainingGuideService();
            _attemptRepo  = new StepAttemptRepository();
            _scoreCalc    = new ScoreCalculator();
        }

        public TrainingSession Start(int operatorId, int guideId, SessionMode mode)
        {
            _sessionRepo.DeleteActiveByOperator(operatorId, guideId);

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

        public void Abandon(int sessionId)
        {
            _sessionRepo.Abandon(sessionId);
        }

        public decimal GetCurrentScore(int sessionId)
        {
            var session  = _sessionRepo.GetById(sessionId);
            var guide    = _guideService.GetById(session.TrainingGuideId);
            var attempts = _attemptRepo.GetBySessionId(sessionId);
            return _scoreCalc.CalculateSessionScore(guide.Steps, attempts);
        }

        public StepSubmitResult SubmitStep(int sessionId, int stepId, string submittedAnswer)
        {
            var session = _sessionRepo.GetById(sessionId);
            var guide   = _guideService.GetById(session.TrainingGuideId);
            var step    = guide.Steps.First(s => s.Id == stepId);

            var validator = GetOrBuildValidator(guide);
            bool isCorrect = validator.Validate(step, submittedAnswer);

            return RecordAttempt(session, guide, step, isCorrect);
        }

        // Assessment: un click en un botón del POD que no corresponde al paso actual
        // es un intento fallido del paso actual (misma penalización y force-advance).
        // clickedStepType null = acción de MES que no es ningún paso (Work Instructions, Reports…).
        // Devuelve null si no es un error de secuencia (Guided, o el botón es el del paso actual).
        public StepSubmitResult RegisterOutOfSequence(int sessionId, StepType? clickedStepType)
        {
            var session = _sessionRepo.GetById(sessionId);
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
        public AssemblySubmitResult AssembleComponent(int sessionId, int componentId, AssemblyProgress progress)
        {
            var result  = new AssemblySubmitResult();
            var session = _sessionRepo.GetById(sessionId);
            if (session.Status != SessionStatus.InProgress) return result;

            var guide = _guideService.GetById(session.TrainingGuideId);
            var step  = guide.Steps.First(s => s.Order == session.CurrentStepOrder);
            if (step.StepType != StepType.Assembly) return result;

            var bom = _guideService.GetAssemblyComponents(guide.AreaId);
            if (!bom.Any(c => c.Id == componentId)) return result;

            progress.AssembledComponentIds.Add(componentId);
            result.ComponentAssembled = true;

            if (progress.Covers(bom))
                result.StepResult = RecordAttempt(session, guide, step, isCorrect: true);
            return result;
        }

        private StepSubmitResult RecordAttempt(TrainingSession session, TrainingGuide guide,
            TrainingStep step, bool isCorrect)
        {
            int sessionId = session.Id;
            int stepId    = step.Id;

            var previousAttempts = _attemptRepo.GetBySessionAndStep(sessionId, stepId);
            int attemptNumber    = previousAttempts.Count + 1;

            _attemptRepo.Insert(new TrainingSessionStepAttempt
            {
                SessionId     = sessionId,
                StepId        = stepId,
                AttemptNumber = attemptNumber,
                IsCorrect     = isCorrect,
                Timestamp     = DateTime.UtcNow
            });

            bool forceAdvance = ScoreCalculator.ShouldForceAdvance(attemptNumber, session.Mode);

            if (!isCorrect && !forceAdvance)
            {
                var allAttemptsForScore = _attemptRepo.GetBySessionId(sessionId);
                return new StepSubmitResult
                {
                    Outcome      = StepSubmitOutcome.Incorrect,
                    AttemptNumber = attemptNumber,
                    CurrentScore = _scoreCalc.CalculateSessionScore(guide.Steps, allAttemptsForScore)
                };
            }

            int nextOrder   = step.Order + 1;
            bool isLastStep = nextOrder > guide.Steps.Count;

            if (isLastStep)
            {
                var allAttempts = _attemptRepo.GetBySessionId(sessionId);
                decimal finalScore = _scoreCalc.CalculateSessionScore(guide.Steps, allAttempts);
                _sessionRepo.Complete(sessionId, finalScore);
                return new StepSubmitResult
                {
                    Outcome      = StepSubmitOutcome.SessionCompleted,
                    AttemptNumber = attemptNumber,
                    CurrentScore = finalScore,
                    SessionScore = finalScore
                };
            }

            _sessionRepo.UpdateCurrentStep(sessionId, nextOrder);

            var attemptsForScore = _attemptRepo.GetBySessionId(sessionId);
            decimal currentScore = _scoreCalc.CalculateSessionScore(guide.Steps, attemptsForScore);

            return new StepSubmitResult
            {
                Outcome       = forceAdvance ? StepSubmitOutcome.FailedForced : StepSubmitOutcome.Correct,
                AttemptNumber = attemptNumber,
                NextStepOrder = nextOrder,
                CurrentScore  = currentScore
            };
        }

        private StepValidator GetOrBuildValidator(TrainingGuide guide)
        {
            string cacheKey = "validator_area_" + guide.AreaId;
            var cached = _validatorCache[cacheKey] as StepValidator;
            if (cached != null)
                return cached;

            var validator = _guideService.BuildValidatorForGuide(guide);
            _validatorCache.Set(cacheKey, validator,
                DateTimeOffset.UtcNow.AddMinutes(ValidatorCacheTtlMinutes));
            return validator;
        }
    }
}
