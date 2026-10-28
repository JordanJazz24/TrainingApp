using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 018, Resumen: tasas por guía (AC-10, AC-11), estados con todo el historial y las
    // semanas de la planta desde el lunes.
    [TestFixture]
    public class SummaryRulesTests
    {
        private const int LoFlo = 10;
        private static readonly DateTime From = new DateTime(2026, 10, 1, 6, 0, 0); // UTC

        private static int _nextId;

        private static DashboardSession Assessment(int userId, decimal score, int day,
            SessionStatus status = SessionStatus.Completed, int guideId = LoFlo)
        {
            var start = From.AddDays(day);
            return new DashboardSession
            {
                SessionId = ++_nextId, UserId = userId, TrainingGuideId = guideId, Mode = SessionMode.Assessment,
                Status = status, FinalScore = score, StartedAt = start,
                CompletedAt = status == SessionStatus.Completed ? start.AddMinutes(20) : (DateTime?)null
            };
        }

        // AC-10: 3 operadores cuyo primer Assessment cae en el período, 2 lo aprobaron = 67 %. El
        // operador 4 tuvo su primer Assessment antes del período: no entra, aunque apruebe en él.
        [Test]
        public void FirstAttemptRate_OnlyFirstAssessmentInPeriod()
        {
            var sessions = new List<DashboardSession>
            {
                Assessment(1, 0.90m, 1),
                Assessment(2, 0.75m, 2),
                Assessment(3, 0.50m, 3), Assessment(3, 0.95m, 4),
                Assessment(4, 0.40m, -5), Assessment(4, 0.80m, 5)
            };

            var summary = SummaryRules.ForGuide(LoFlo, sessions, From);

            Assert.That(summary.FirstAttemptOperators, Is.EqualTo(3));
            Assert.That(summary.FirstAttemptPassed, Is.EqualTo(2));
            Assert.That(Math.Round(summary.FirstAttemptRate.Value * 100m), Is.EqualTo(67m));
            // % aprobados del período: los 4 tienen un Assessment completado en él y todos aprueban alguno.
            Assert.That(summary.AssessedOperators, Is.EqualTo(4));
            Assert.That(summary.PassRate, Is.EqualTo(1m));
            // Puntaje promedio solo del período: 0.90, 0.75, 0.50, 0.95, 0.80.
            Assert.That(summary.AverageScore, Is.EqualTo(0.78m));
            // Estados con todo el historial: todos aprobaron alguno.
            Assert.That(summary.Passed, Is.EqualTo(4));
        }

        // AC-11: sin denominador, null ("—"), nunca 0; los abandonados y en curso no cuentan.
        [Test]
        public void NoDenominator_IsNull()
        {
            var sessions = new List<DashboardSession>
            {
                Assessment(1, 0m, 1, SessionStatus.Abandoned),
                Assessment(2, 0m, 2, SessionStatus.InProgress)
            };

            var summary = SummaryRules.ForGuide(LoFlo, sessions, From);

            Assert.That(summary.PassRate, Is.Null);
            Assert.That(summary.FirstAttemptRate, Is.Null);
            Assert.That(summary.AverageScore, Is.Null);
            Assert.That(summary.InTraining, Is.EqualTo(2));
            Assert.That(SummaryRules.ForGuide(LoFlo, new List<DashboardSession>(), From).PassRate, Is.Null);
        }

        // Estados: reprobado y en entrenamiento, por operador, de esa guía nada más.
        [Test]
        public void States_ByOperatorInTheGuide()
        {
            var sessions = new List<DashboardSession>
            {
                Assessment(1, 0.50m, 1),
                Assessment(2, 0.90m, 1, guideId: 99),
                new DashboardSession { SessionId = ++_nextId, UserId = 3, TrainingGuideId = LoFlo, Mode = SessionMode.Guided,
                                       Status = SessionStatus.Completed, StartedAt = From }
            };

            var summary = SummaryRules.ForGuide(LoFlo, sessions, null);

            Assert.That(new[] { summary.Passed, summary.Failed, summary.InTraining }, Is.EqualTo(new[] { 0, 1, 1 }));
        }

        [Test]
        public void WeekStart_IsMonday()
        {
            Assert.That(SummaryRules.WeekStart(new DateTime(2026, 10, 19, 8, 0, 0)), Is.EqualTo(new DateTime(2026, 10, 19)));
            Assert.That(SummaryRules.WeekStart(new DateTime(2026, 10, 25, 23, 0, 0)), Is.EqualTo(new DateTime(2026, 10, 19)));
            Assert.That(SummaryRules.WeekStart(new DateTime(2026, 10, 21, 0, 0, 0)), Is.EqualTo(new DateTime(2026, 10, 19)));
        }

        private static DashboardSession At(DateTime utc, SessionMode mode) => new DashboardSession
        {
            SessionId = ++_nextId, UserId = 1, TrainingGuideId = LoFlo, Mode = mode, StartedAt = utc
        };

        // Semanas de la planta (UTC-6): el lunes 26 a las 03:00 UTC todavía es domingo 25 en la
        // planta y cuenta en la semana del 19. Las semanas vacías van en 0. Con 43 días, por
        // semana (spec 023, P-9).
        [Test]
        public void Activity_WeeksInPlantTimeWithEmptyWeeks()
        {
            var sessions = new List<DashboardSession>
            {
                At(new DateTime(2026, 10, 26, 3, 0, 0), SessionMode.Guided),
                At(new DateTime(2026, 10, 20, 15, 0, 0), SessionMode.Assessment),
                At(new DateTime(2026, 11, 10, 15, 0, 0), SessionMode.FreePractice),
                At(new DateTime(2026, 9, 30, 15, 0, 0), SessionMode.Guided)    // antes del período
            };
            var now   = new DateTime(2026, 11, 12, 18, 0, 0);
            var range = ReportPeriods.Resolve(ReportPeriod.Custom, new DateTime(2026, 10, 1), new DateTime(2026, 11, 12), now);

            var chart = SummaryRules.Activity(sessions, range, now);

            Assert.That(chart.Grain, Is.EqualTo(ActivityGrain.Week));
            Assert.That(chart.Buckets.Select(w => w.Start), Is.EqualTo(new[]
            {
                new DateTime(2026, 9, 28), new DateTime(2026, 10, 5), new DateTime(2026, 10, 12), new DateTime(2026, 10, 19),
                new DateTime(2026, 10, 26), new DateTime(2026, 11, 2), new DateTime(2026, 11, 9)
            }));
            Assert.That(chart.Buckets[0].Guided, Is.EqualTo(0), "la del 30 de sep. no es del período");
            Assert.That(chart.Buckets[3].Guided, Is.EqualTo(1));
            Assert.That(chart.Buckets[3].Assessment, Is.EqualTo(1));
            Assert.That(chart.Buckets[4].Guided + chart.Buckets[4].Assessment + chart.Buckets[4].FreePractice, Is.EqualTo(0));
            Assert.That(chart.Buckets[6].FreePractice, Is.EqualTo(1));
        }

        // Por día, del día 1 a hoy, con los días vacíos; sin sesiones en el período, vacío.
        [Test]
        public void Activity_DaysOfThisMonth()
        {
            var now   = new DateTime(2026, 10, 28, 18, 0, 0);
            var range = ReportPeriods.Resolve(ReportPeriod.ThisMonth, null, null, now);

            var chart = SummaryRules.Activity(new List<DashboardSession> { At(new DateTime(2026, 10, 3, 15, 0, 0), SessionMode.Guided) }, range, now);

            Assert.That(chart.Grain, Is.EqualTo(ActivityGrain.Day));
            Assert.That(chart.Buckets.Count, Is.EqualTo(28));
            Assert.That(chart.Buckets[2].Start, Is.EqualTo(new DateTime(2026, 10, 3)));
            Assert.That(chart.Buckets[2].Guided, Is.EqualTo(1));
            Assert.That(SummaryRules.Activity(new List<DashboardSession>(), range, now).Buckets, Is.Empty);
        }

        // Con Todo, el largo real desde la primera sesión: más de un año va por mes.
        [Test]
        public void Activity_AllByRealLength()
        {
            var now   = new DateTime(2026, 10, 28, 18, 0, 0);
            var range = ReportPeriods.Resolve(ReportPeriod.All, null, null, now);
            var sessions = new List<DashboardSession>
            {
                At(new DateTime(2025, 6, 10, 15, 0, 0), SessionMode.Assessment),
                At(new DateTime(2026, 10, 2, 15, 0, 0), SessionMode.Guided)
            };

            var chart = SummaryRules.Activity(sessions, range, now);

            Assert.That(chart.Grain, Is.EqualTo(ActivityGrain.Month));
            Assert.That(chart.Buckets.First().Start, Is.EqualTo(new DateTime(2025, 6, 1)));
            Assert.That(chart.Buckets.Last().Start, Is.EqualTo(new DateTime(2026, 10, 1)));
            Assert.That(chart.Buckets.Count, Is.EqualTo(17));
        }

        // AC-6: 1, 7 y 31 días por día; 90 por semana; 400 por mes.
        [TestCase(1, ActivityGrain.Day)]
        [TestCase(7, ActivityGrain.Day)]
        [TestCase(31, ActivityGrain.Day)]
        [TestCase(32, ActivityGrain.Week)]
        [TestCase(90, ActivityGrain.Week)]
        [TestCase(366, ActivityGrain.Week)]
        [TestCase(400, ActivityGrain.Month)]
        public void GrainFor_ByLength(int days, ActivityGrain grain)
            => Assert.That(SummaryRules.GrainFor(days), Is.EqualTo(grain));

        // ForGuide con el fin del período: lo que empieza en el fin (excluido) no cuenta.
        [Test]
        public void ForGuide_EndIsExclusive()
        {
            var sessions = new List<DashboardSession> { Assessment(1, 1m, 0), Assessment(2, 0.5m, 5) };

            var summary = SummaryRules.ForGuide(LoFlo, sessions, From, From.AddDays(5));

            Assert.That(summary.AssessedOperators, Is.EqualTo(1));
            Assert.That(summary.PassRate, Is.EqualTo(1m));
        }
    }
}
