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

        // Semanas de la planta (UTC-6): el lunes 26 a las 03:00 UTC todavía es domingo 25 en la
        // planta y cuenta en la semana del 19. Las semanas vacías van en 0.
        [Test]
        public void Weeks_InPlantTimeWithEmptyWeeks()
        {
            DashboardSession At(DateTime utc, SessionMode mode) => new DashboardSession
            {
                SessionId = ++_nextId, UserId = 1, TrainingGuideId = LoFlo, Mode = mode, StartedAt = utc
            };
            var sessions = new List<DashboardSession>
            {
                At(new DateTime(2026, 10, 26, 3, 0, 0), SessionMode.Guided),
                At(new DateTime(2026, 10, 20, 15, 0, 0), SessionMode.Assessment),
                At(new DateTime(2026, 11, 10, 15, 0, 0), SessionMode.FreePractice)
            };

            var weeks = SummaryRules.Weeks(sessions, new DateTime(2026, 10, 20, 6, 0, 0), new DateTime(2026, 11, 12, 12, 0, 0));

            Assert.That(weeks.Select(w => w.WeekStart), Is.EqualTo(new[]
            {
                new DateTime(2026, 10, 19), new DateTime(2026, 10, 26), new DateTime(2026, 11, 2), new DateTime(2026, 11, 9)
            }));
            Assert.That(weeks[0].Guided, Is.EqualTo(1));
            Assert.That(weeks[0].Assessment, Is.EqualTo(1));
            Assert.That(weeks[1].Guided + weeks[1].Assessment + weeks[1].FreePractice, Is.EqualTo(0));
            Assert.That(weeks[3].FreePractice, Is.EqualTo(1));
            Assert.That(SummaryRules.Weeks(new List<DashboardSession>(), null, DateTime.UtcNow), Is.Empty);
        }
    }
}
