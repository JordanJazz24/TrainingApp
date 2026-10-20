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

    // Sesiones completas con los pesos reales de cada guía. Copia de la BD: si cambian los
    // scripts de origen, hay que actualizar estos valores.
    [TestFixture]
    public class ScoreCalculatorGuideTests
    {
        private readonly ScoreCalculator _calc = new ScoreCalculator();

        // Database/StoredProcedures/07_RCMoldeoGuide.sql — NonSerialized, sin Assembly, con SFC Split.
        internal static List<TrainingStep> MoldeoGuide()
        {
            return Guide(
                (StepType.MesLogin,       0.05m),
                (StepType.ResourceSetup,  0.20m),
                (StepType.LineClearance,  0.05m),
                (StepType.Search,         0.15m),
                (StepType.DataCollection, 0.25m),
                (StepType.LineClearance,  0.05m),
                (StepType.SFC_Split,      0.05m),
                (StepType.LogNC,          0.10m),
                (StepType.Complete,       0.05m),
                (StepType.Logout,         0.05m));
        }

        // Database/StoredProcedures/05_SeedData.sql (datos de ejemplo) — Serialized, con
        // Assembly, sin SFC Split. LoFlo (09) tiene la misma forma y los mismos pesos.
        internal static List<TrainingStep> RedefinityGuide()
        {
            return Guide(
                (StepType.MesLogin,            0.05m),
                (StepType.ResourceSetup,       0.20m),
                (StepType.LineClearance,       0.05m),
                (StepType.Search,              0.05m),
                (StepType.Assembly,            0.15m),
                (StepType.DataCollection,      0.20m),
                (StepType.LineClearance,       0.05m),
                (StepType.LogNC,               0.05m),
                (StepType.LogNCFastRepair,     0.05m),
                (StepType.LogNCExtendedRepair, 0.05m),
                (StepType.Complete,            0.05m),
                (StepType.Logout,              0.05m));
        }

        // Pasos en el orden de la guía; Id = Order = posición.
        private static List<TrainingStep> Guide(params (StepType Type, decimal Weight)[] steps)
        {
            return steps
                .Select((s, i) => new TrainingStep { Id = i + 1, Order = i + 1, StepType = s.Type, Weight = s.Weight })
                .ToList();
        }

        private static IEnumerable<TestCaseData> RealGuides()
        {
            yield return new TestCaseData(MoldeoGuide()).SetArgDisplayNames("Moldeo");
            yield return new TestCaseData(RedefinityGuide()).SetArgDisplayNames("Redefinity");
        }

        private const int Forced = 0;   // en las tablas de resultados: el paso se forzó sin acierto

        // Un resultado por paso, en orden: el intento en que acertó, o Forced.
        private static List<TrainingSessionStepAttempt> Run(IList<TrainingStep> steps, params int[] outcomes)
        {
            var attempts = new List<TrainingSessionStepAttempt>();
            for (int i = 0; i < steps.Count; i++)
            {
                attempts.AddRange(outcomes[i] == Forced
                    ? ScoreData.FailedTimes(steps[i].Id, ScoreCalculator.MaxScoredAttempts + 1)
                    : ScoreData.CorrectOn(steps[i].Id, outcomes[i]));
            }
            return attempts;
        }

        private static int[] All(int outcome, int count)
        {
            return Enumerable.Repeat(outcome, count).ToArray();
        }

        [Test]
        public void GuideCopies_WeightsSumOne()
        {
            Assert.That(MoldeoGuide().Sum(s => s.Weight), Is.EqualTo(1.00m));
            Assert.That(RedefinityGuide().Sum(s => s.Weight), Is.EqualTo(1.00m));
        }

        // Spec 016 (AC-15): las copias son las de los scripts. Si un script cambia sus pasos o
        // sus pesos sin actualizar la copia, este test falla.
        private static IEnumerable<TestCaseData> GuideScripts()
        {
            yield return new TestCaseData("07_RCMoldeoGuide.sql", MoldeoGuide()).SetArgDisplayNames("07 Moldeo");
            yield return new TestCaseData("05_SeedData.sql", RedefinityGuide()).SetArgDisplayNames("05 Redefinity");
            yield return new TestCaseData("09_SensoresLoFloGuide.sql", RedefinityGuide()).SetArgDisplayNames("09 LoFlo");
        }

        [TestCaseSource(nameof(GuideScripts))]
        public void GuideScript_MatchesCopy(string script, List<TrainingStep> copy)
        {
            var steps = GuideScriptReader.Read(script);

            Assert.That(steps.Select(s => (s.Order, s.StepType, s.Weight)),
                Is.EqualTo(copy.Select(s => (s.Order, s.StepType, s.Weight))));
        }

        // Spec 016 (D-64, D-66): Ingreso a MES es el primer paso, con 0.05, en las tres guías.
        [TestCaseSource(nameof(GuideScripts))]
        public void GuideScript_StartsWithMesLogin(string script, List<TrainingStep> copy)
        {
            var first = GuideScriptReader.Read(script).First();

            Assert.That(first.StepType, Is.EqualTo(StepType.MesLogin));
            Assert.That(first.Weight, Is.EqualTo(0.05m));
        }

        private static IEnumerable<TestCaseData> GuidesWithAbsentStep()
        {
            yield return new TestCaseData(MoldeoGuide(), StepType.Assembly).SetArgDisplayNames("Moldeo sin Assembly");
            yield return new TestCaseData(RedefinityGuide(), StepType.SFC_Split).SetArgDisplayNames("Redefinity sin SFC Split");
        }

        // Pasos opcionales ausentes: la guía no tiene el paso y sus pesos ya lo absorben, así
        // que todo correcto da 1.00 (el cálculo no completa ni descuenta nada por el ausente).
        [TestCaseSource(nameof(GuidesWithAbsentStep))]
        public void PerfectRun_ScoresOne_WithOptionalStepAbsent(List<TrainingStep> guide, StepType absent)
        {
            Assert.That(guide.Select(s => s.StepType), Has.None.EqualTo(absent));

            var attempts = Run(guide, All(1, guide.Count));

            Assert.That(_calc.CalculateSessionScore(guide, attempts, SessionMode.Assessment), Is.EqualTo(1.00m));
        }

        [TestCaseSource(nameof(RealGuides))]
        public void Assessment_AllStepsForced_ScoresZero(List<TrainingStep> guide)
        {
            var attempts = Run(guide, All(Forced, guide.Count));

            Assert.That(_calc.CalculateSessionScore(guide, attempts, SessionMode.Assessment), Is.EqualTo(0m));
        }

        [TestCaseSource(nameof(RealGuides))]
        public void SameAttempts_GuidedVsAssessment(List<TrainingStep> guide)
        {
            var attempts = Run(guide, All(3, guide.Count));

            Assert.That(_calc.CalculateSessionScore(guide, attempts, SessionMode.Guided), Is.EqualTo(1.00m));
            Assert.That(_calc.CalculateSessionScore(guide, attempts, SessionMode.Assessment), Is.EqualTo(0.60m));
        }

        // Moldeo sin Ingreso a MES ni Data Collection (forzados, −0.05 − 0.25) queda justo en
        // el mínimo y aprueba.
        [Test]
        public void Moldeo_LosingMesLoginAndDataCollection_PassesAtSeventy()
        {
            var guide = MoldeoGuide();
            //                     In      RS LC Se DC      LC SS NC Co Lo
            var attempts = Run(guide, Forced, 1, 1, 1, Forced, 1, 1, 1, 1, 1);
            decimal score = _calc.CalculateSessionScore(guide, attempts, SessionMode.Assessment);

            Assert.That(score, Is.EqualTo(0.70m));
            Assert.That(ScoreCalculator.IsPassed(score), Is.True);
        }

        // Además, un reintento en Line Clearance (0.05 × 0.20 = −0.01) deja 0.69: no aprueba.
        [Test]
        public void Moldeo_LosingMesLoginDataCollectionAndOneRetry_FailsAtSixtyNine()
        {
            var guide = MoldeoGuide();
            //                     In      RS LC Se DC      LC SS NC Co Lo
            var attempts = Run(guide, Forced, 1, 2, 1, Forced, 1, 1, 1, 1, 1);
            decimal score = _calc.CalculateSessionScore(guide, attempts, SessionMode.Assessment);

            Assert.That(score, Is.EqualTo(0.69m));
            Assert.That(ScoreCalculator.IsPassed(score), Is.False);
        }

        // El cálculo no depende del estado de la sesión: una abandonada a mitad de la guía
        // vale la suma de los pasos resueltos. (Que FinalScore solo se guarde al completar lo
        // decide spStepAttemptRecord, no ScoreCalculator.)
        [Test]
        public void AbandonedMidway_ScoresResolvedStepsOnly()
        {
            var guide = MoldeoGuide();
            // Ingreso al 1.º, RS al 1.º, LC al 1.º, Search al 2.º, DC forzado; el resto sin intentos.
            var attempts = ScoreData.CorrectOn(1, 1)
                .Concat(ScoreData.CorrectOn(2, 1))
                .Concat(ScoreData.CorrectOn(3, 1))
                .Concat(ScoreData.CorrectOn(4, 2))
                .Concat(ScoreData.FailedTimes(5, ScoreCalculator.MaxScoredAttempts + 1))
                .ToList();

            Assert.That(_calc.CalculateSessionScore(guide, attempts, SessionMode.Assessment),
                        Is.EqualTo(0.42m));   // 0.05 + 0.20 + 0.05 + 0.15 × 0.80 + 0
        }

        // Barrido: cada paso resuelto en el intento 1, 2, 3 o forzado (4^10 combinaciones). El
        // puntaje está entre 0 y 1, tiene a lo sumo 2 decimales (FinalScore es DECIMAL(5,2): el
        // redondeo de la BD no lo cambia) y es Σ peso × factor, con los factores de CLAUDE.md
        // escritos aquí, no tomados de GetPenaltyFactor. Solo Moldeo: con los 12 pasos de
        // Redefinity serían 4^12 (16,7 millones); los pesos de Redefinity no agregan casos
        // (todos son múltiplos de 0.05, como los de Moldeo).
        [Test]
        public void Assessment_EveryOutcomeCombination_IsExactAndInRange()
        {
            var guide = MoldeoGuide();
            int[] outcomes = { 1, 2, 3, Forced };
            decimal[] factors = { 1.00m, 0.80m, 0.60m, 0.00m };
            int combinations = (int)System.Math.Pow(outcomes.Length, guide.Count);
            var failures = new List<string>();

            for (int combo = 0; combo < combinations && failures.Count < 5; combo++)
            {
                var stepOutcomes = new int[guide.Count];
                decimal expected = 0m;
                int rest = combo;
                for (int i = 0; i < guide.Count; i++)
                {
                    int pick = rest % outcomes.Length;
                    rest /= outcomes.Length;
                    stepOutcomes[i] = outcomes[pick];
                    expected += guide[i].Weight * factors[pick];
                }

                decimal score = _calc.CalculateSessionScore(guide, Run(guide, stepOutcomes), SessionMode.Assessment);

                if (score != expected || score < 0m || score > 1m || decimal.Round(score, 2) != score)
                    failures.Add("[" + string.Join(",", stepOutcomes) + "] → " + score + " (esperado " + expected + ")");
            }

            Assert.That(failures, Is.Empty);
        }
    }
}
