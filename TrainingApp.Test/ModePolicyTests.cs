using System;
using NUnit.Framework;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 012 (P-12): la tabla de capacidades por modo. Guided y Assessment tienen que quedar
    // exactamente como antes de ModePolicy (AC-1).
    [TestFixture]
    public class ModePolicyTests
    {
        [Test]
        public void For_ReturnsThePolicyOfEachMode()
        {
            Assert.That(ModePolicy.For(SessionMode.Guided), Is.SameAs(ModePolicy.Guided));
            Assert.That(ModePolicy.For(SessionMode.Assessment), Is.SameAs(ModePolicy.Assessment));
            Assert.That(ModePolicy.For(SessionMode.FreePractice), Is.SameAs(ModePolicy.FreePractice));
            Assert.That(ModePolicy.Guided.Mode, Is.EqualTo(SessionMode.Guided));
            Assert.That(ModePolicy.Assessment.Mode, Is.EqualTo(SessionMode.Assessment));
            Assert.That(ModePolicy.FreePractice.Mode, Is.EqualTo(SessionMode.FreePractice));
        }

        // P-1: el valor que se guarda en TrainingSessions.Mode.
        [Test]
        public void FreePractice_IsStoredAsTwo()
        {
            Assert.That((int)SessionMode.FreePractice, Is.EqualTo(2));
        }

        [Test]
        public void FreePractice_Capabilities()
        {
            var p = ModePolicy.FreePractice;
            Assert.Multiple(new Action(() =>
            {
                Assert.That(p.ShowsGuide, Is.False);
                Assert.That(p.EnablesAllButtons, Is.True);
                Assert.That(p.OutOfSequence, Is.EqualTo(OutOfSequenceHandling.Notice));
                Assert.That(p.NoLogicButtons, Is.EqualTo(NoLogicButtonHandling.Inert));
                Assert.That(p.PenalizesRetries, Is.False);
                Assert.That(p.ForcesAdvance, Is.False);
                Assert.That(p.FailureFeedback, Is.EqualTo(FailureFeedbackKind.Incorrect));
                Assert.That(p.HintOnDemand, Is.True);
                Assert.That(p.AllowsSkip, Is.True);
                Assert.That(p.Summary, Is.EqualTo(SummaryKind.AttemptsPerStep));
                Assert.That(p.ShowsScore, Is.False);
            }));
        }

        [Test]
        public void GuidedAndAssessment_ShowScore()
        {
            Assert.That(ModePolicy.Guided.ShowsScore, Is.True);
            Assert.That(ModePolicy.Assessment.ShowsScore, Is.True);
        }

        // P-7: sin penalización y sin avance forzado, en cualquier intento.
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(10)]
        public void FreePractice_NeverPenalizesNorForcesAdvance(int attemptNumber)
        {
            Assert.That(ModePolicy.FreePractice.PenaltyFactor(attemptNumber), Is.EqualTo(1.00m));
            Assert.That(ModePolicy.FreePractice.ForcesAdvanceAt(attemptNumber), Is.False);
            Assert.That(ScoreCalculator.GetPenaltyFactor(attemptNumber, SessionMode.FreePractice), Is.EqualTo(1.00m));
            Assert.That(ScoreCalculator.ShouldForceAdvance(attemptNumber, SessionMode.FreePractice), Is.False);
        }

        [Test]
        public void For_UnknownMode_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(new Action(() => ModePolicy.For((SessionMode)99)));
        }

        [Test]
        public void Guided_Capabilities()
        {
            var p = ModePolicy.Guided;
            Assert.Multiple(new Action(() =>
            {
                Assert.That(p.ShowsGuide, Is.True);
                Assert.That(p.EnablesAllButtons, Is.False);
                Assert.That(p.OutOfSequence, Is.EqualTo(OutOfSequenceHandling.Ignore));
                Assert.That(p.NoLogicButtons, Is.EqualTo(NoLogicButtonHandling.Disabled));
                Assert.That(p.PenalizesRetries, Is.False);
                Assert.That(p.ForcesAdvance, Is.False);
                Assert.That(p.FailureFeedback, Is.EqualTo(FailureFeedbackKind.Hint));
                Assert.That(p.HintOnDemand, Is.False);
                Assert.That(p.AllowsSkip, Is.False);
                Assert.That(p.Summary, Is.EqualTo(SummaryKind.Score));
            }));
        }

        [Test]
        public void Assessment_Capabilities()
        {
            var p = ModePolicy.Assessment;
            Assert.Multiple(new Action(() =>
            {
                Assert.That(p.ShowsGuide, Is.False);
                Assert.That(p.EnablesAllButtons, Is.True);
                Assert.That(p.OutOfSequence, Is.EqualTo(OutOfSequenceHandling.RecordAttempt));
                Assert.That(p.NoLogicButtons, Is.EqualTo(NoLogicButtonHandling.RecordAttempt));
                Assert.That(p.PenalizesRetries, Is.True);
                Assert.That(p.ForcesAdvance, Is.True);
                Assert.That(p.FailureFeedback, Is.EqualTo(FailureFeedbackKind.RemainingAttempts));
                Assert.That(p.HintOnDemand, Is.False);
                Assert.That(p.AllowsSkip, Is.False);
                Assert.That(p.Summary, Is.EqualTo(SummaryKind.ScoreAndResult));
            }));
        }

        // Los valores de siempre, escritos aquí (no tomados de ScoreCalculator).
        [TestCase(1, 1.00)]
        [TestCase(2, 0.80)]
        [TestCase(3, 0.60)]
        [TestCase(4, 0.00)]
        [TestCase(10, 0.00)]
        public void Assessment_PenaltyFactor(int attemptNumber, double expected)
        {
            Assert.That(ModePolicy.Assessment.PenaltyFactor(attemptNumber), Is.EqualTo((decimal)expected));
        }

        [TestCase(1)]
        [TestCase(4)]
        [TestCase(10)]
        public void Guided_PenaltyFactor_AlwaysFull(int attemptNumber)
        {
            Assert.That(ModePolicy.Guided.PenaltyFactor(attemptNumber), Is.EqualTo(1.00m));
        }

        [TestCase(3, false)]
        [TestCase(4, true)]
        [TestCase(10, true)]
        public void Assessment_ForcesAdvanceFromFourthAttempt(int attemptNumber, bool expected)
        {
            Assert.That(ModePolicy.Assessment.ForcesAdvanceAt(attemptNumber), Is.EqualTo(expected));
        }

        [TestCase(1)]
        [TestCase(4)]
        [TestCase(10)]
        public void Guided_NeverForcesAdvance(int attemptNumber)
        {
            Assert.That(ModePolicy.Guided.ForcesAdvanceAt(attemptNumber), Is.False);
        }
    }
}
