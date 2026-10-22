using System;
using System.Collections.Generic;
using NUnit.Framework;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 018 (D-136): estado de un operador en una guía, AC-3 a AC-6 y AC-8.
    [TestFixture]
    public class ReadinessPolicyTests
    {
        private const int LoFlo  = 1;
        private const int Moldeo = 2;

        private static readonly DateTime Day = new DateTime(2026, 10, 1, 14, 0, 0, DateTimeKind.Utc);

        private static DashboardSession Session(int guideId, SessionMode mode, SessionStatus status,
            decimal score, int day)
            => new DashboardSession
            {
                TrainingGuideId = guideId,
                Mode            = mode,
                Status          = status,
                FinalScore      = score,
                StartedAt       = Day.AddDays(day),
                CompletedAt     = status == SessionStatus.Completed ? Day.AddDays(day).AddMinutes(20) : (DateTime?)null
            };

        private static DashboardSession Assessment(int guideId, decimal score, int day)
            => Session(guideId, SessionMode.Assessment, SessionStatus.Completed, score, day);

        // AC-3: 0.70 exacto aprueba.
        [Test]
        public void CompletedAssessmentAtPassingScore_IsPassed()
        {
            var readiness = ReadinessPolicy.ForGuide(LoFlo, new[] { Assessment(LoFlo, 0.70m, 0) });

            Assert.That(readiness.State, Is.EqualTo(ReadinessState.Passed));
            Assert.That(readiness.BestScore, Is.EqualTo(0.70m));
        }

        // AC-4: dos Assessment completados por debajo de 0.70.
        [Test]
        public void CompletedAssessmentsBelowPassingScore_IsFailed()
        {
            var readiness = ReadinessPolicy.ForGuide(Moldeo, new[]
            {
                Assessment(Moldeo, 0.55m, 0), Assessment(Moldeo, 0.65m, 1)
            });

            Assert.That(readiness.State, Is.EqualTo(ReadinessState.Failed));
            Assert.That(readiness.BestScore, Is.EqualTo(0.65m));
            Assert.That(readiness.LastScore, Is.EqualTo(0.65m));
        }

        // AC-5: aprobó y después reprobó: sigue aprobado; último 0.60 y mejor 0.80.
        [Test]
        public void PassedThenFailed_StaysPassed()
        {
            var readiness = ReadinessPolicy.ForGuide(Moldeo, new[]
            {
                Assessment(Moldeo, 0.80m, 0), Assessment(Moldeo, 0.60m, 3)
            });

            Assert.That(readiness.State, Is.EqualTo(ReadinessState.Passed));
            Assert.That(readiness.BestScore, Is.EqualTo(0.80m));
            Assert.That(readiness.LastScore, Is.EqualTo(0.60m));
        }

        // El último es el último que se completó, no el último de la lista.
        [Test]
        public void LastScore_IsTheLatestCompleted()
        {
            var readiness = ReadinessPolicy.ForGuide(LoFlo, new[]
            {
                Assessment(LoFlo, 0.60m, 5), Assessment(LoFlo, 0.90m, 2)
            });

            Assert.That(readiness.LastScore, Is.EqualTo(0.60m));
        }

        // AC-6: solo Guided, o un Assessment abandonado o en curso, es En entrenamiento.
        [TestCase(SessionMode.Guided, SessionStatus.Completed)]
        [TestCase(SessionMode.FreePractice, SessionStatus.Completed)]
        [TestCase(SessionMode.Assessment, SessionStatus.Abandoned)]
        [TestCase(SessionMode.Assessment, SessionStatus.InProgress)]
        public void WithoutCompletedAssessment_IsInTraining(SessionMode mode, SessionStatus status)
        {
            var readiness = ReadinessPolicy.ForGuide(LoFlo, new[] { Session(LoFlo, mode, status, 0.95m, 0) });

            Assert.That(readiness.State, Is.EqualTo(ReadinessState.InTraining));
            Assert.That(readiness.BestScore, Is.Null);
            Assert.That(readiness.LastScore, Is.Null);
            Assert.That(ReadinessPolicy.CountsAsAssessment(Session(LoFlo, mode, status, 0.95m, 0)), Is.False);
        }

        // Un Assessment abandonado no cuenta como reprobado junto a uno aprobado ni a uno reprobado.
        [Test]
        public void AbandonedAssessment_DoesNotCount()
        {
            var readiness = ReadinessPolicy.ForGuide(LoFlo, new[]
            {
                Session(LoFlo, SessionMode.Assessment, SessionStatus.Abandoned, 0.10m, 0),
                Assessment(LoFlo, 0.65m, 1)
            });

            Assert.That(readiness.State, Is.EqualTo(ReadinessState.Failed));
            Assert.That(readiness.BestScore, Is.EqualTo(0.65m));
        }

        // Sin sesiones de la guía (aunque tenga de otra): NotStarted en esa guía.
        [Test]
        public void NoSessionsInGuide_IsNotStarted()
        {
            var readiness = ReadinessPolicy.ForGuide(LoFlo, new[] { Assessment(Moldeo, 0.90m, 0) });

            Assert.That(readiness.State, Is.EqualTo(ReadinessState.NotStarted));
            Assert.That(readiness.LastActivity, Is.Null);
            Assert.That(ReadinessPolicy.ForGuide(LoFlo, new List<DashboardSession>()).State,
                Is.EqualTo(ReadinessState.NotStarted));
        }

        // La última actividad es el inicio de la sesión más reciente de la guía, de cualquier modo.
        [Test]
        public void LastActivity_IsTheLatestStart()
        {
            var readiness = ReadinessPolicy.ForGuide(LoFlo, new[]
            {
                Assessment(LoFlo, 0.80m, 1),
                Session(LoFlo, SessionMode.Guided, SessionStatus.InProgress, 0m, 4)
            });

            Assert.That(readiness.LastActivity, Is.EqualTo(Day.AddDays(4)));
        }
    }
}
