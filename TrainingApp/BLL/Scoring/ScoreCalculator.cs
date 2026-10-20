using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Scoring
{
    public class ScoreCalculator
    {
        // Assessment: puntaje final mínimo (0..1) para aprobar.
        public const decimal PassingScore = 0.70m;

        // Assessment: último intento con puntaje; el siguiente fallo fuerza el avance.
        public const int MaxScoredAttempts = ModePolicy.MaxScoredAttempts;

        public static bool IsPassed(decimal finalScore)
        {
            return finalScore >= PassingScore;
        }

        public decimal CalculateSessionScore(
            IList<TrainingStep> steps,
            IList<TrainingSessionStepAttempt> attempts,
            SessionMode mode)
        {
            decimal total = 0m;
            foreach (var step in steps)
            {
                var firstCorrect = attempts
                    .Where(a => a.StepId == step.Id && a.IsCorrect)
                    .OrderBy(a => a.AttemptNumber)
                    .FirstOrDefault();

                if (firstCorrect != null)
                    total += step.Weight * GetPenaltyFactor(firstCorrect.AttemptNumber, mode);
                // Attempt 4+ forced: IsCorrect=false on all attempts → 0 contribution
            }
            return total;
        }

        // Qué modo penaliza y cuándo fuerza el avance lo decide ModePolicy (spec 012, P-12;
        // AUD-07, D-2).
        public static decimal GetPenaltyFactor(int attemptNumber, SessionMode mode)
            => ModePolicy.For(mode).PenaltyFactor(attemptNumber);

        public static bool ShouldForceAdvance(int attemptNumber, SessionMode mode)
            => ModePolicy.For(mode).ForcesAdvanceAt(attemptNumber);
    }
}
