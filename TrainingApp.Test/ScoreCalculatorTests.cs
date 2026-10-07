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
}
