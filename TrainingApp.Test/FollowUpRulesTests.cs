using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 018, Seguimiento: S-1 (AC-4), S-2 por guía (AC-19), S-3 por paso de la guía y la
    // salida al aprobar (AC-20), sin período (DA-3).
    [TestFixture]
    public class FollowUpRulesTests
    {
        private const int Moldeo = 10;
        private const int LoFlo = 20;
        private const int Redefinity = 30;
        private static readonly DateTime Now = new DateTime(2026, 10, 23, 18, 0, 0);

        private static DashboardSession Session(int guideId, SessionMode mode, decimal score, int daysAgo,
            SessionStatus status = SessionStatus.Completed)
            => new DashboardSession
            {
                SessionId = 1, UserId = 1, TrainingGuideId = guideId, Mode = mode, Status = status,
                FinalScore = score, StartedAt = Now.AddDays(-daysAgo), CompletedAt = Now.AddDays(-daysAgo).AddMinutes(20)
            };

        private static IList<StepRun> Ended(params int[] stepIds)
            => stepIds.Select(id => new StepRun { StepId = id, StepOrder = id % 100, Resolved = true, HasCorrectAttempt = false }).ToList();

        private static IList<FollowUpRule> Rules(int guideId, IList<DashboardSession> sessions,
            IEnumerable<IList<StepRun>> runs = null)
            => FollowUpRules.ForGuide(guideId, sessions, runs, Now).Select(r => r.Rule).ToList();

        // AC-4: dos Assessment completados de Moldeo con 0.55 y 0.65 → S-1.
        [Test]
        public void TwoFailedAssessments_S1()
        {
            var sessions = new List<DashboardSession>
            {
                Session(Moldeo, SessionMode.Assessment, 0.55m, 3),
                Session(Moldeo, SessionMode.Assessment, 0.65m, 2)
            };

            var reasons = FollowUpRules.ForGuide(Moldeo, sessions, null, Now);

            Assert.That(reasons.Single().Rule, Is.EqualTo(FollowUpRule.RepeatedFailures));
            Assert.That(reasons.Single().FailedAssessments, Is.EqualTo(2));
            // Uno solo no alcanza; los abandonados no cuentan.
            Assert.That(Rules(Moldeo, new List<DashboardSession>
            {
                Session(Moldeo, SessionMode.Assessment, 0.55m, 3),
                Session(Moldeo, SessionMode.Assessment, 0m, 2, SessionStatus.Abandoned)
            }), Is.Empty);
        }

        // AC-5: aprobó y después reprobó: sigue aprobado y no está en Seguimiento.
        [Test]
        public void PassedGuide_NoRules()
        {
            var sessions = new List<DashboardSession>
            {
                Session(Moldeo, SessionMode.Assessment, 0.80m, 40),
                Session(Moldeo, SessionMode.Assessment, 0.60m, 30),
                Session(Moldeo, SessionMode.Assessment, 0.50m, 20)
            };

            Assert.That(Rules(Moldeo, sessions, new[] { Ended(101), Ended(101) }), Is.Empty);
        }

        // AC-19: 15 días sin sesiones de Redefinity, aunque practicó Moldeo ayer → S-2 en
        // Redefinity; con 13 días todavía no.
        [Test]
        public void InactiveInTheGuide_S2EvenIfPracticingOthers()
        {
            var sessions = new List<DashboardSession>
            {
                Session(Redefinity, SessionMode.Guided, 0m, 15),
                Session(Moldeo, SessionMode.FreePractice, 0m, 1)
            };

            var reasons = FollowUpRules.ForGuide(Redefinity, sessions, null, Now);

            Assert.That(reasons.Single().Rule, Is.EqualTo(FollowUpRule.Inactive));
            Assert.That(reasons.Single().DaysInactive, Is.EqualTo(15));
            Assert.That(Rules(Moldeo, sessions), Is.Empty);
            Assert.That(Rules(Redefinity, new List<DashboardSession> { Session(Redefinity, SessionMode.Guided, 0m, 13) }), Is.Empty);
            Assert.That(Rules(Redefinity, new List<DashboardSession> { Session(Redefinity, SessionMode.Guided, 0m, 14) }),
                Is.EqualTo(new[] { FollowUpRule.Inactive }));
        }

        // Una sesión en curso cuenta desde su inicio (spec → Edge Cases).
        [Test]
        public void InProgressSession_CountsFromItsStart()
        {
            var sessions = new List<DashboardSession>
            {
                Session(LoFlo, SessionMode.Guided, 0m, 20),
                Session(LoFlo, SessionMode.FreePractice, 0m, 16, SessionStatus.InProgress)
            };

            Assert.That(FollowUpRules.ForGuide(LoFlo, sessions, null, Now).Single().DaysInactive, Is.EqualTo(16));
        }

        // AC-20: el mismo paso de LoFlo sin acierto en 2 Assessment → S-3 con ese paso; al aprobar
        // LoFlo sale. Dos pasos distintos (aunque se llamen igual) no se juntan (DA-3b).
        [Test]
        public void SameStepWithoutSuccess_S3AndLeavesOnPass()
        {
            var sessions = new List<DashboardSession>
            {
                Session(LoFlo, SessionMode.Assessment, 0.50m, 2)
            };
            var runs = new[] { Ended(203, 207), Ended(203), Ended(207 + 100) };

            var reasons = FollowUpRules.ForGuide(LoFlo, sessions, runs, Now);

            var stuck = reasons.Single(r => r.Rule == FollowUpRule.StuckStep).Steps;
            Assert.That(stuck.Select(s => s.StepId), Is.EqualTo(new[] { 203 }));
            Assert.That(stuck.Single().Times, Is.EqualTo(2));

            sessions.Add(Session(LoFlo, SessionMode.Assessment, 0.90m, 1));
            Assert.That(Rules(LoFlo, sessions, runs), Is.Empty);
        }

        // Un paso que no quedó atrás (sesión abandonada sin avance forzado) no terminó sin acierto.
        [Test]
        public void UnresolvedStep_IsNotWithoutSuccess()
        {
            var sessions = new List<DashboardSession> { Session(LoFlo, SessionMode.Assessment, 0.50m, 2) };
            var unresolved = new List<StepRun> { new StepRun { StepId = 203, StepOrder = 3, Resolved = false } };

            Assert.That(Rules(LoFlo, sessions, new[] { unresolved, unresolved }), Is.Empty);
        }

        [Test]
        public void NoSessionsInTheGuide_NoRules()
        {
            Assert.That(Rules(LoFlo, new List<DashboardSession>(), new[] { Ended(203), Ended(203) }), Is.Empty);
        }
    }
}
