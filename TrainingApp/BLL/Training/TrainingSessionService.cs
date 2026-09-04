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

            var previousAttempts = _attemptRepo.GetBySessionAndStep(sessionId, stepId);
            int attemptNumber    = previousAttempts.Count + 1;
            bool isCorrect       = validator.Validate(step, submittedAnswer);

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
