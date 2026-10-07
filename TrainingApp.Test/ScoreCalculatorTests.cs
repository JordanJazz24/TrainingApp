using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Fase 5 (docs/specs/fase-5-plan.md). Reglas: CLAUDE.md → "Modos de sesión" y
    // "Ponderación de pasos". Un test que falla aquí es una decisión para el usuario, no un
    // ajuste del algoritmo.

    [TestFixture]
    public class ScoreCalculatorPenaltyTests
    {
        // NUnit no acepta decimal en atributos: el esperado llega como double.
        [TestCase(1, 1.00)]
        [TestCase(2, 0.80)]
        [TestCase(3, 0.60)]
        [TestCase(4, 0.00)]
        [TestCase(5, 0.00)]
        [TestCase(10, 0.00)]
        public void GetPenaltyFactor_Assessment_PenalizesEachRetry(int attemptNumber, double expected)
        {
            Assert.That(ScoreCalculator.GetPenaltyFactor(attemptNumber, SessionMode.Assessment),
                        Is.EqualTo((decimal)expected));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(10)]
        public void GetPenaltyFactor_Guided_NeverPenalizes(int attemptNumber)
        {
            Assert.That(ScoreCalculator.GetPenaltyFactor(attemptNumber, SessionMode.Guided),
                        Is.EqualTo(1.00m));
        }
    }

    [TestFixture]
    public class ScoreCalculatorForceAdvanceTests
    {
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(4, true)]
        [TestCase(5, true)]
        [TestCase(10, true)]
        public void ShouldForceAdvance_Assessment_FromFourthAttempt(int attemptNumber, bool expected)
        {
            Assert.That(ScoreCalculator.ShouldForceAdvance(attemptNumber, SessionMode.Assessment),
                        Is.EqualTo(expected));
        }

        [TestCase(1)]
        [TestCase(4)]
        [TestCase(10)]
        public void ShouldForceAdvance_Guided_Never(int attemptNumber)
        {
            Assert.That(ScoreCalculator.ShouldForceAdvance(attemptNumber, SessionMode.Guided), Is.False);
        }

        // El intento que fuerza el avance es el primero que ya no puntúa: los últimos que
        // puntúan son los que no fuerzan.
        [Test]
        public void ShouldForceAdvance_Assessment_StartsWhereScoringEnds()
        {
            int lastScored = ScoreCalculator.MaxScoredAttempts;
            int firstForced = lastScored + 1;

            Assert.That(ScoreCalculator.ShouldForceAdvance(lastScored, SessionMode.Assessment), Is.False);
            Assert.That(ScoreCalculator.GetPenaltyFactor(lastScored, SessionMode.Assessment), Is.GreaterThan(0m));
            Assert.That(ScoreCalculator.ShouldForceAdvance(firstForced, SessionMode.Assessment), Is.True);
            Assert.That(ScoreCalculator.GetPenaltyFactor(firstForced, SessionMode.Assessment), Is.EqualTo(0m));
        }
    }

    [TestFixture]
    public class ScoreCalculatorPassingTests
    {
        [TestCase(0.70, true)]
        [TestCase(1.00, true)]
        [TestCase(0.6999, false)]
        [TestCase(0.69, false)]
        [TestCase(0.00, false)]
        public void IsPassed_UsesSeventyPercentThreshold(double score, bool expected)
        {
            Assert.That(ScoreCalculator.IsPassed((decimal)score), Is.EqualTo(expected));
        }

        // Si alguien cambia estas reglas de negocio, un test lo avisa.
        [Test]
        public void Constants_MatchBusinessRules()
        {
            Assert.That(ScoreCalculator.PassingScore, Is.EqualTo(0.70m));
            Assert.That(ScoreCalculator.MaxScoredAttempts, Is.EqualTo(3));
        }
    }

    // Datos en memoria para CalculateSessionScore: pasos con su peso e intentos por paso.
    internal static class ScoreData
    {
        public static TrainingStep Step(int id, decimal weight)
        {
            return new TrainingStep { Id = id, Order = id, Weight = weight };
        }

        public static TrainingSessionStepAttempt Attempt(int stepId, int attemptNumber, bool isCorrect)
        {
            return new TrainingSessionStepAttempt
            {
                StepId        = stepId,
                AttemptNumber = attemptNumber,
                IsCorrect     = isCorrect
            };
        }

        // Fallos en los intentos 1..correctOn-1 y acierto en correctOn.
        public static IEnumerable<TrainingSessionStepAttempt> CorrectOn(int stepId, int correctOn)
        {
            for (int n = 1; n < correctOn; n++)
                yield return Attempt(stepId, n, isCorrect: false);
            yield return Attempt(stepId, correctOn, isCorrect: true);
        }

        // Fallos en los intentos 1..failures, sin acierto (avance forzado en Assessment).
        public static IEnumerable<TrainingSessionStepAttempt> FailedTimes(int stepId, int failures)
        {
            return Enumerable.Range(1, failures).Select(n => Attempt(stepId, n, isCorrect: false));
        }
    }

    [TestFixture]
    public class ScoreCalculatorStepScoreTests
    {
        private readonly ScoreCalculator _calc = new ScoreCalculator();

        private decimal Score(IList<TrainingStep> steps,
                              IEnumerable<TrainingSessionStepAttempt> attempts,
                              SessionMode mode = SessionMode.Assessment)
        {
            return _calc.CalculateSessionScore(steps, attempts.ToList(), mode);
        }

        [Test]
        public void NoAttempts_ScoresZero()
        {
            var steps = new List<TrainingStep> { ScoreData.Step(1, 0.50m), ScoreData.Step(2, 0.50m) };

            Assert.That(Score(steps, new TrainingSessionStepAttempt[0]), Is.EqualTo(0m));
        }

        // StepScore = StepWeight × PenaltyFactor(intento en que acertó).
        [TestCase(1, 0.30)]
        [TestCase(2, 0.24)]
        [TestCase(3, 0.18)]
        [TestCase(4, 0.00)]   // acierta tarde: el paso ya cuenta como fallido
        public void Assessment_StepScoreIsWeightTimesPenalty(int correctOn, double expected)
        {
            var steps = new List<TrainingStep> { ScoreData.Step(1, 0.30m) };

            Assert.That(Score(steps, ScoreData.CorrectOn(1, correctOn)), Is.EqualTo((decimal)expected));
        }

        [Test]
        public void Assessment_ForcedWithoutCorrectAttempt_ScoresZero()
        {
            var steps = new List<TrainingStep> { ScoreData.Step(1, 0.30m) };

            Assert.That(Score(steps, ScoreData.FailedTimes(1, ScoreCalculator.MaxScoredAttempts + 1)),
                        Is.EqualTo(0m));
        }

        [Test]
        public void Guided_CorrectOnFifthAttempt_ScoresFullWeight()
        {
            var steps = new List<TrainingStep> { ScoreData.Step(1, 0.30m) };

            Assert.That(Score(steps, ScoreData.CorrectOn(1, 5), SessionMode.Guided), Is.EqualTo(0.30m));
        }

        // Vale el primer acierto (menor AttemptNumber), aunque la lista llegue desordenada.
        [Test]
        public void FirstCorrectAttemptCounts_EvenIfUnordered()
        {
            var steps = new List<TrainingStep> { ScoreData.Step(1, 0.50m) };
            var attempts = new[]
            {
                ScoreData.Attempt(1, 3, isCorrect: true),
                ScoreData.Attempt(1, 1, isCorrect: false),
                ScoreData.Attempt(1, 2, isCorrect: true)
            };

            Assert.That(Score(steps, attempts), Is.EqualTo(0.40m));   // 0.50 × 0.80
        }

        [Test]
        public void AttemptsOfStepNotInGuide_AreIgnored()
        {
            var steps = new List<TrainingStep> { ScoreData.Step(1, 0.50m) };
            var attempts = ScoreData.CorrectOn(1, 1).Concat(ScoreData.CorrectOn(99, 1));

            Assert.That(Score(steps, attempts), Is.EqualTo(0.50m));
        }

        // Sesión en curso: los pasos sin intentos suman 0; es el puntaje en vivo del POD.
        [Test]
        public void InProgress_ScoresOnlyResolvedSteps()
        {
            var steps = new List<TrainingStep>
            {
                ScoreData.Step(1, 0.20m), ScoreData.Step(2, 0.30m), ScoreData.Step(3, 0.50m)
            };
            var attempts = ScoreData.CorrectOn(1, 1).Concat(ScoreData.CorrectOn(2, 2));

            Assert.That(Score(steps, attempts), Is.EqualTo(0.44m));   // 0.20 + 0.30 × 0.80
        }

        [Test]
        public void RetriesOfOneStep_DoNotAffectAnother()
        {
            var steps = new List<TrainingStep> { ScoreData.Step(1, 0.50m), ScoreData.Step(2, 0.50m) };
            var attempts = ScoreData.CorrectOn(1, 3).Concat(ScoreData.CorrectOn(2, 1));

            Assert.That(Score(steps, attempts), Is.EqualTo(0.80m));   // 0.50 × 0.60 + 0.50
        }

        // El cálculo no normaliza: si los pesos de la guía suman 0.90, el máximo es 0.90.
        // Riesgo de configuración al cargar una guía (CLAUDE.md → "Ponderación de pasos").
        [Test]
        public void WeightsNotSummingOne_AreNotNormalized()
        {
            var steps = new List<TrainingStep>
            {
                ScoreData.Step(1, 0.30m), ScoreData.Step(2, 0.30m), ScoreData.Step(3, 0.30m)
            };
            var attempts = steps.SelectMany(s => ScoreData.CorrectOn(s.Id, 1));

            Assert.That(Score(steps, attempts), Is.EqualTo(0.90m));
        }
    }
}
