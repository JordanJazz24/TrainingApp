using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 018, AC-15: curva de aprendizaje (sin Guided) y sesiones de práctica antes del primer
    // aprobado (con Guided).
    [TestFixture]
    public class LearningCurveTests
    {
        private const int LoFlo = 10;
        private static readonly DateTime Start = new DateTime(2026, 10, 1, 14, 0, 0);
        private int _id;

        [SetUp]
        public void ResetIds() => _id = 0;

        private DashboardSession Session(SessionMode mode, int day, decimal score = 0m,
            SessionStatus status = SessionStatus.Completed, int guideId = LoFlo)
            => new DashboardSession
            {
                SessionId = ++_id, TrainingGuideId = guideId, Mode = mode, Status = status, FinalScore = score,
                StartedAt = Start.AddDays(day)
            };

        private static GuideCurve Curve(IEnumerable<DashboardSession> sessions)
            => LearningCurve.ForGuide(LoFlo, sessions, s => s.SessionId * 10,
                s => s.Mode == SessionMode.Assessment && s.Status == SessionStatus.Completed ? s.FinalScore : (decimal?)null);

        // AC-15: 3 Guided, 1 Práctica libre, 1 Assessment reprobado y 1 aprobado → 3 puntos en
        // orden y "4 sesiones de práctica antes del primer aprobado".
        [Test]
        public void ThreePoints_FourPracticeSessions()
        {
            var sessions = new List<DashboardSession>
            {
                Session(SessionMode.Guided, 1), Session(SessionMode.Guided, 2), Session(SessionMode.Guided, 3),
                Session(SessionMode.FreePractice, 4),
                Session(SessionMode.Assessment, 5, 0.55m),
                Session(SessionMode.Assessment, 6, 0.85m)
            };

            var curve = Curve(sessions.AsEnumerable().Reverse());

            Assert.That(curve.Points.Select(p => p.Mode),
                Is.EqualTo(new[] { SessionMode.FreePractice, SessionMode.Assessment, SessionMode.Assessment }));
            Assert.That(curve.Points.Select(p => p.FailedAttempts), Is.EqualTo(new[] { 40, 50, 60 }));
            Assert.That(curve.Points.Select(p => p.Score), Is.EqualTo(new decimal?[] { null, 0.55m, 0.85m }));
            Assert.That(curve.PracticeBeforeFirstPass, Is.EqualTo(4));
        }

        // Sin aprobar: null. Lo de después del primer aprobado y de otra guía no cuenta.
        [Test]
        public void PracticeCount_OnlyBeforeFirstPassInTheGuide()
        {
            Assert.That(Curve(new[] { Session(SessionMode.Guided, 1), Session(SessionMode.Assessment, 2, 0.5m) })
                .PracticeBeforeFirstPass, Is.Null);

            var curve = Curve(new[]
            {
                Session(SessionMode.FreePractice, 1),
                Session(SessionMode.Guided, 2, guideId: 99),
                Session(SessionMode.Assessment, 3, 0.9m),
                Session(SessionMode.FreePractice, 4)
            });

            Assert.That(curve.PracticeBeforeFirstPass, Is.EqualTo(1));
            Assert.That(curve.Points.Count, Is.EqualTo(3));
        }

        // Un Assessment abandonado no aprueba: la práctica sigue contando hasta el aprobado.
        [Test]
        public void AbandonedAssessment_IsNotAPass()
        {
            var curve = Curve(new[]
            {
                Session(SessionMode.Guided, 1),
                Session(SessionMode.Assessment, 2, 0.9m, SessionStatus.Abandoned),
                Session(SessionMode.FreePractice, 3),
                Session(SessionMode.Assessment, 4, 0.8m)
            });

            Assert.That(curve.PracticeBeforeFirstPass, Is.EqualTo(2));
            Assert.That(curve.Points[1].Score, Is.Null);
        }
    }
}
