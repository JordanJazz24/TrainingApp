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

        // Guided es práctica: acertar en cualquier intento vale el peso completo del paso. La
        // penalización por reintento es solo de Assessment (CLAUDE.md → Modos de sesión;
        // auditoría AUD-07, decisión D-2).
        public static decimal GetPenaltyFactor(int attemptNumber, SessionMode mode)
        {
            if (mode == SessionMode.Guided) return 1.00m;

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
