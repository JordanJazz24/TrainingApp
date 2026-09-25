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
        public const int MaxScoredAttempts = 3;

        public static bool IsPassed(decimal finalScore)
        {
            return finalScore >= PassingScore;
        }

        public decimal CalculateSessionScore(
            IList<TrainingStep> steps,
            IList<TrainingSessionStepAttempt> attempts)
        {
            decimal total = 0m;
            foreach (var step in steps)
            {
                var firstCorrect = attempts
                    .Where(a => a.StepId == step.Id && a.IsCorrect)
                    .OrderBy(a => a.AttemptNumber)
                    .FirstOrDefault();

                if (firstCorrect != null)
                    total += step.Weight * GetPenaltyFactor(firstCorrect.AttemptNumber);
                // Attempt 4+ forced: IsCorrect=false on all attempts → 0 contribution
            }
            return total;
        }

        public decimal CalculateStepScore(decimal stepWeight, int attemptNumber)
        {
            return stepWeight * GetPenaltyFactor(attemptNumber);
        }

        public static decimal GetPenaltyFactor(int attemptNumber)
        {
            switch (attemptNumber)
            {
                case 1: return 1.00m;
                case 2: return 0.80m;
                case 3: return 0.60m;
                default: return 0.00m;
            }
        }

        public static bool ShouldForceAdvance(int attemptNumber, SessionMode mode)
        {
            return mode == SessionMode.Assessment && attemptNumber > MaxScoredAttempts;
        }
    }
}
