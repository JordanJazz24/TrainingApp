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
        private readonly TrainingGuideRepository _guideRepo;
        private readonly StepAttemptRepository _attemptRepo;
        private readonly ScoreCalculator _scoreCalc;
        private readonly StepValidator _validator;

        public TrainingSessionService()
        {
            _sessionRepo = new TrainingSessionRepository();
            _guideRepo = new TrainingGuideRepository();
            _attemptRepo = new StepAttemptRepository();
            _scoreCalc = new ScoreCalculator();
            _validator = new StepValidator();
        }

        public TrainingSession Start(int operatorId, int guideId, SessionMode mode)
        {
            var session = new TrainingSession
            {
                OperatorId = operatorId,
                TrainingGuideId = guideId,
                Mode = mode,
                Status = SessionStatus.InProgress,
                CurrentStepOrder = 1,
                FinalScore = 0m,
                StartedAt = DateTime.UtcNow
            };
            session.Id = _sessionRepo.Insert(session);
            return session;
        }

        public TrainingSession GetById(int sessionId)
        {
            return _sessionRepo.GetById(sessionId);
        }

        public StepSubmitResult SubmitStep(int sessionId, int stepId, string submittedAnswer)
        {
            var session = _sessionRepo.GetById(sessionId);
            var guide = _guideRepo.GetById(session.TrainingGuideId);
            var step = guide.Steps.First(s => s.Id == stepId);

            var previousAttempts = _attemptRepo.GetBySessionAndStep(sessionId, stepId);
            int attemptNumber = previousAttempts.Count + 1;
            bool isCorrect = _validator.Validate(step, submittedAnswer);

            _attemptRepo.Insert(new TrainingSessionStepAttempt
            {
                SessionId = sessionId,
                StepId = stepId,
                AttemptNumber = attemptNumber,
                IsCorrect = isCorrect,
                Timestamp = DateTime.UtcNow
            });

            bool forceAdvance = ScoreCalculator.ShouldForceAdvance(attemptNumber, session.Mode);

            if (!isCorrect && !forceAdvance)
                return new StepSubmitResult { Outcome = StepSubmitOutcome.Incorrect };

            // Advance: correct answer OR forced (Assessment 4+)
            int nextOrder = step.Order + 1;
            bool isLastStep = nextOrder > guide.Steps.Count;

            if (isLastStep)
            {
                var allAttempts = _attemptRepo.GetBySessionId(sessionId);
                decimal finalScore = _scoreCalc.CalculateSessionScore(guide.Steps, allAttempts);
                _sessionRepo.Complete(sessionId, finalScore);
                return new StepSubmitResult
                {
                    Outcome = StepSubmitOutcome.SessionCompleted,
                    SessionScore = finalScore
                };
            }

            _sessionRepo.UpdateCurrentStep(sessionId, nextOrder);
            return new StepSubmitResult
            {
                Outcome = forceAdvance ? StepSubmitOutcome.FailedForced : StepSubmitOutcome.Correct,
                NextStepOrder = nextOrder
            };
        }
    }
}
