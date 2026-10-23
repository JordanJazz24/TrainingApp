using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 018: pasos de una sesión (DA-2, duración), fallos sin contar saltos, pasos sin acierto
    // y pasos a reforzar.
    [TestFixture]
    public class StepRunsTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 1, 14, 0, 0, DateTimeKind.Utc);

        private static DashboardSession Session(SessionStatus status = SessionStatus.Completed, int id = 1)
            => new DashboardSession { SessionId = id, Mode = SessionMode.Assessment, Status = status, StartedAt = Start };

        private static DashboardAttempt Attempt(int order, int seconds, bool correct,
            AttemptReason? reason = null, int sessionId = 1)
            => new DashboardAttempt
            {
                SessionId = sessionId, StepId = 100 + order, StepOrder = order, IsCorrect = correct,
                Timestamp = Start.AddSeconds(seconds),
                Reason    = correct ? AttemptReason.None : reason ?? AttemptReason.WrongDecision
            };

        // DA-2: el primer paso desde el inicio; los demás desde el último intento del anterior.
        [Test]
        public void Duration_FromPreviousStepLastAttempt()
        {
            var runs = StepRuns.ForSession(Session(), new[]
            {
                Attempt(1, 30, false), Attempt(1, 50, true),
                Attempt(2, 120, true),
                Attempt(3, 150, false), Attempt(3, 200, true)
            });

            Assert.That(runs.Select(r => r.StepOrder), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(runs.Select(r => r.Duration.TotalSeconds), Is.EqualTo(new[] { 50d, 70d, 80d }));
            Assert.That(runs.Select(r => r.FailedAttempts), Is.EqualTo(new[] { 1, 0, 1 }));
        }

        // Los intentos de otra sesión no cuentan.
        [Test]
        public void IgnoresOtherSessions()
        {
            var runs = StepRuns.ForSession(Session(), new[] { Attempt(1, 10, true), Attempt(1, 20, false, sessionId: 2) });

            Assert.That(runs.Single().FailedAttempts, Is.EqualTo(0));
        }

        // El salto de Práctica libre no es un fallo, pero el paso terminó sin acierto.
        [Test]
        public void Skip_IsNotAFailure_ButEndsWithoutSuccess()
        {
            var runs = StepRuns.ForSession(Session(), new[]
            {
                Attempt(1, 10, false, AttemptReason.InvalidAction),
                Attempt(1, 20, false, AttemptReason.Skipped),
                Attempt(2, 40, true)
            });

            Assert.That(runs[0].FailedAttempts, Is.EqualTo(1));
            Assert.That(runs[0].Skipped, Is.True);
            Assert.That(runs[0].EndedWithoutSuccess, Is.True);
            Assert.That(StepRuns.IsFailure(Attempt(1, 0, false, AttemptReason.Skipped)), Is.False);
        }

        // Avance forzado: 3 fallos y el paso siguiente con intentos.
        [Test]
        public void ForcedAdvance_EndsWithoutSuccess()
        {
            var runs = StepRuns.ForSession(Session(SessionStatus.InProgress), new[]
            {
                Attempt(1, 10, false), Attempt(1, 20, false), Attempt(1, 30, false),
                Attempt(2, 40, false)
            });

            Assert.That(runs[0].EndedWithoutSuccess, Is.True);
            // El paso actual de una sesión en curso todavía no terminó.
            Assert.That(runs[1].Resolved, Is.False);
            Assert.That(runs[1].EndedWithoutSuccess, Is.False);
        }

        // Un salto sin fallos no terminó "sin acierto": saltar nunca es un fallo (spec 018).
        [Test]
        public void SkipWithoutFailures_IsNotEndedWithoutSuccess()
        {
            var session = Session(); session.Mode = SessionMode.FreePractice;
            var runs = StepRuns.ForSession(session, new[] { Attempt(1, 10, false, AttemptReason.Skipped), Attempt(2, 20, true) });

            Assert.That(runs[0].Skipped, Is.True);
            Assert.That(runs[0].EndedWithoutSuccess, Is.False);
        }

        // Avance forzado en el último paso de un Assessment abandonado: el 4.º fallo fuerza el
        // avance (ModePolicy.ForcesAdvanceAt) aunque no haya intentos en un paso posterior.
        [Test]
        public void ForcedAdvance_OnLastStepOfAbandonedAssessment()
        {
            var four = StepRuns.ForSession(Session(SessionStatus.Abandoned), new[]
            {
                Attempt(1, 10, false), Attempt(1, 20, false), Attempt(1, 30, false), Attempt(1, 40, false)
            });
            var three = StepRuns.ForSession(Session(SessionStatus.Abandoned), new[]
            {
                Attempt(1, 10, false), Attempt(1, 20, false), Attempt(1, 30, false)
            });

            Assert.That(four.Single().EndedWithoutSuccess, Is.True);
            Assert.That(three.Single().EndedWithoutSuccess, Is.False);
        }

        // En una sesión completada todos sus pasos quedaron atrás.
        [Test]
        public void CompletedSession_LastStepIsResolved()
        {
            var runs = StepRuns.ForSession(Session(), new[] { Attempt(1, 10, false), Attempt(1, 20, false), Attempt(1, 30, false) });

            Assert.That(runs.Single().EndedWithoutSuccess, Is.True);
        }

        // La duración de la sesión: "—" en curso.
        [Test]
        public void SessionDuration()
        {
            var done = Session(); done.CompletedAt = Start.AddMinutes(25);
            var running = Session(SessionStatus.InProgress);

            Assert.That(StepRuns.SessionDuration(done), Is.EqualTo(TimeSpan.FromMinutes(25)));
            Assert.That(StepRuns.SessionDuration(running), Is.Null);
        }
    }

    [TestFixture]
    public class ReinforcementRuleTests
    {
        private static StepRun Run(int order, int failed, bool correct, bool resolved = true,
            params AttemptReason[] reasons)
            => new StepRun
            {
                StepId = 100 + order, StepOrder = order, FailedAttempts = failed,
                Failures = (reasons.Length > 0 ? reasons : Enumerable.Repeat(AttemptReason.WrongDecision, failed))
                    .Select(r => new DashboardAttempt { Reason = r }).ToList(),
                HasCorrectAttempt = correct, Resolved = resolved
            };

        // Solo entran los pasos sin acierto o con 2 o más fallos en una sesión.
        [Test]
        public void Threshold()
        {
            var steps = ReinforcementRule.ForGuide(new List<IList<StepRun>>
            {
                new List<StepRun> { Run(1, 1, true), Run(2, 2, true), Run(3, 3, false) },
                new List<StepRun> { Run(1, 1, true) }
            });

            Assert.That(steps.Select(s => s.StepOrder), Is.EqualTo(new[] { 3, 2 }));
        }

        // Orden: veces sin acierto y después intentos fallidos; hasta 5.
        [Test]
        public void Order_AndTopFive()
        {
            var sessionA = new List<StepRun> { Run(1, 2, true), Run(2, 3, false), Run(3, 2, true), Run(4, 5, true), Run(5, 2, true), Run(6, 2, true) };
            var sessionB = new List<StepRun> { Run(2, 1, true), Run(4, 3, false) };

            var steps = ReinforcementRule.ForGuide(new List<IList<StepRun>> { sessionA, sessionB });

            Assert.That(steps.Count, Is.EqualTo(5));
            // 2 y 4 terminaron sin acierto una vez; 4 tiene más fallos (8 contra 4).
            Assert.That(steps.Select(s => s.StepOrder).Take(2), Is.EqualTo(new[] { 4, 2 }));
            Assert.That(steps[0].TimesWithoutSuccess, Is.EqualTo(1));
            Assert.That(steps[0].FailedAttempts, Is.EqualTo(8));
        }

        // El motivo más repetido entre los fallos del paso.
        [Test]
        public void MainReason_IsTheMostFrequent()
        {
            var steps = ReinforcementRule.ForGuide(new List<IList<StepRun>>
            {
                new List<StepRun> { Run(1, 3, true, true, AttemptReason.EmptyRequiredField, AttemptReason.WrongDecision, AttemptReason.EmptyRequiredField) }
            });

            Assert.That(steps.Single().MainReason, Is.EqualTo(AttemptReason.EmptyRequiredField));
        }

        // Sin Assessment, nada que reforzar.
        [Test]
        public void NoSessions_Empty()
        {
            Assert.That(ReinforcementRule.ForGuide(new List<IList<StepRun>>()), Is.Empty);
        }
    }
}
