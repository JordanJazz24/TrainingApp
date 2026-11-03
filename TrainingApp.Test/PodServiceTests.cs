using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // AUD-74 E4 (D-15, D-259): el núcleo de PodService con repositorios y servicios en memoria.
    // Cubre LoadContext (solo una sesión propia y en curso), el registro del intento y el
    // avance (RecordAttempt, vía SubmitStep) en los tres modos, Skip, el avance forzado y
    // Complete. Split, Log NC, Assembly y Data Collection quedan fuera (D-259).
    [TestFixture]
    public class PodServiceTests
    {
        private const int UserId      = 7;
        private const int OtherUserId = 8;
        private const int SessionId   = 100;
        private const int GuideId     = 5;
        private const int AreaId      = 2;

        // Guía de cuatro pasos de igual peso: tres Line Clearance (se califican con "true") y
        // Complete al final.
        private static readonly StepType[] Standard =
            { StepType.LineClearance, StepType.LineClearance, StepType.LineClearance, StepType.Complete };

        private InMemoryPodStateStore _store;
        private FakeSessions _sessions;
        private FakeAttempts _attempts;
        private FakeCatalog _catalog;
        private PodService _pod;
        private TrainingGuide _guide;

        [SetUp]
        public void SetUp()
        {
            _store    = new InMemoryPodStateStore();
            _sessions = new FakeSessions();
            _attempts = new FakeAttempts(_sessions);
            _catalog  = new FakeCatalog();
            var users = new FakeUsers();
            users.Add(new User { Id = UserId, Code1 = "E0001" });
            _pod = new PodService(_store, _sessions, _attempts, users, _catalog,
                new FakeSessionService(), new ScoreCalculator());
        }

        // ── LoadContext: solo una sesión propia y en curso ───────────────

        [Test]
        public void LoadContext_OwnSessionInProgress_ReturnsItsContext()
        {
            var ctx = Arrange(SessionMode.Guided, currentStep: 2);

            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.UserId, Is.EqualTo(UserId));
            Assert.That(ctx.Session.Id, Is.EqualTo(SessionId));
            Assert.That(ctx.Guide.Id, Is.EqualTo(GuideId));
            Assert.That(ctx.Area.Id, Is.EqualTo(AreaId));
            Assert.That(ctx.CurrentStep.Order, Is.EqualTo(2));
            Assert.That(ctx.Score, Is.EqualTo(0m));
        }

        [Test]
        public void LoadContext_SessionOfAnotherUser_ReturnsNullWithoutReadingAnythingElse()
        {
            Arrange(SessionMode.Guided);
            _store.Remove(SessionId);
            int guideReads = _catalog.Reads;
            int attemptReads = _attempts.Reads;

            var ctx = _pod.LoadContext(SessionId, OtherUserId);

            Assert.That(ctx, Is.Null);
            Assert.That(_store.Load(SessionId), Is.Null, "No crea ni guarda estado para una sesión ajena.");
            Assert.That(_catalog.Reads, Is.EqualTo(guideReads), "No lee la guía de una sesión ajena.");
            Assert.That(_attempts.Reads, Is.EqualTo(attemptReads), "No lee los intentos de una sesión ajena.");
        }

        [TestCase(SessionStatus.Completed)]
        [TestCase(SessionStatus.Abandoned)]
        public void LoadContext_SessionNotInProgress_ReturnsNull(SessionStatus status)
        {
            Arrange(SessionMode.Guided);
            _sessions.Stored(SessionId).Status = status;

            Assert.That(_pod.LoadContext(SessionId, UserId), Is.Null);
        }

        [Test]
        public void LoadContext_UnknownSession_ReturnsNull()
        {
            Arrange(SessionMode.Guided);

            Assert.That(_pod.LoadContext(SessionId + 1, UserId), Is.Null);
        }

        [Test]
        public void LoadContext_WithoutStoredState_CreatesAndSavesIt()
        {
            var ctx = Arrange(SessionMode.Guided);

            Assert.That(ctx.State, Is.Not.Null);
            Assert.That(_store.Load(SessionId), Is.SameAs(ctx.State));
        }

        [Test]
        public void LoadContext_ScoreComesFromTheAttemptsAlreadyRecorded()
        {
            _attempts.SeedCorrect(SessionId, stepId: 11);
            var ctx = Arrange(SessionMode.Guided, currentStep: 2);

            Assert.That(ctx.Score, Is.EqualTo(Weight));
        }

        // ── SubmitStep → RecordAttempt: el intento y el avance, juntos ───

        [Test]
        public void SubmitStep_Correct_RecordsTheAttemptAndTheAdvanceInOneCall()
        {
            var ctx = Arrange(SessionMode.Guided);

            var result = _pod.SubmitStep(ctx, "true");

            var call = _attempts.Calls.Single();
            Assert.That(result.Outcome, Is.EqualTo(StepSubmitOutcome.Correct));
            Assert.That(call.StepOrder, Is.EqualTo(1));
            Assert.That(call.NextStepOrder, Is.EqualTo(2));
            Assert.That(call.FinalScore, Is.Null);
            Assert.That(call.Attempt.IsCorrect, Is.True);
            Assert.That(call.Attempt.StepId, Is.EqualTo(11));
            Assert.That(call.Attempt.AttemptNumber, Is.EqualTo(1));
            Assert.That(call.Attempt.Reason, Is.EqualTo(AttemptReason.None));
            Assert.That(call.Attempt.Action, Is.EqualTo(AttemptActions.ForSubmit(StepType.LineClearance, "true")));
            Assert.That(ctx.Session.CurrentStepOrder, Is.EqualTo(2));
            Assert.That(ctx.CurrentStep.Order, Is.EqualTo(2));
            Assert.That(ctx.Score, Is.EqualTo(Weight));
        }

        [Test]
        public void SubmitStep_TheServerDecidesTheStep_NotThePage()
        {
            var ctx = Arrange(SessionMode.Guided, currentStep: 3);

            _pod.SubmitStep(ctx, "true");

            var call = _attempts.Calls.Single();
            Assert.That(call.StepOrder, Is.EqualTo(3));
            Assert.That(call.Attempt.StepId, Is.EqualTo(13));
            Assert.That(call.NextStepOrder, Is.EqualTo(4));
        }

        [Test]
        public void SubmitStep_UpdatesTheContextOnlyAfterTheRecordIsSaved()
        {
            var ctx = Arrange(SessionMode.Guided);
            int stepAtRecord = -1;
            decimal scoreAtRecord = -1m;
            _attempts.OnRecord = () => { stepAtRecord = ctx.Session.CurrentStepOrder; scoreAtRecord = ctx.Score; };

            _pod.SubmitStep(ctx, "true");

            Assert.That(stepAtRecord, Is.EqualTo(1), "Al guardar, el contexto sigue en el paso de antes.");
            Assert.That(scoreAtRecord, Is.EqualTo(0m));
            Assert.That(ctx.Session.CurrentStepOrder, Is.EqualTo(2));
            Assert.That(ctx.Score, Is.EqualTo(Weight));
        }

        [Test]
        public void SubmitStep_WhenTheRecordFails_TheContextIsLeftUntouched()
        {
            var ctx = Arrange(SessionMode.Guided);
            _attempts.FailWith = new InvalidOperationException("la BD no respondió");

            Assert.Throws<InvalidOperationException>((Action)(() => _pod.SubmitStep(ctx, "true")));

            Assert.That(ctx.Session.CurrentStepOrder, Is.EqualTo(1));
            Assert.That(ctx.Score, Is.EqualTo(0m));
            Assert.That(_sessions.Stored(SessionId).CurrentStepOrder, Is.EqualTo(1));

            // Sin el fallo, el mismo envío cuenta como primer intento: no quedó nada a medias.
            _attempts.FailWith = null;
            Assert.That(_pod.SubmitStep(ctx, "true").AttemptNumber, Is.EqualTo(1));
        }

        [Test]
        public void SubmitStep_OnASessionThatIsNotInProgress_ThrowsAndRecordsNothing()
        {
            var ctx = Arrange(SessionMode.Guided);
            ctx.Session.Status = SessionStatus.Completed;

            Assert.Throws<InvalidOperationException>((Action)(() => _pod.SubmitStep(ctx, "true")));
            Assert.That(_attempts.Calls, Is.Empty);
        }

        [Test]
        public void SubmitStep_InvalidAction_IsRecordedAsInvalidAction()
        {
            var ctx = Arrange(SessionMode.Guided);

            _pod.SubmitStep(ctx, StepAnswers.Invalid);

            Assert.That(_attempts.Calls.Single().Attempt.Reason, Is.EqualTo(AttemptReason.InvalidAction));
        }

        // ── SubmitStep por modo ──────────────────────────────────────────

        [Test]
        public void Guided_Incorrect_RecordsOnlyTheAttemptAndNeverForcesTheAdvance()
        {
            var ctx = Arrange(SessionMode.Guided);

            StepSubmitResult last = null;
            for (int i = 1; i <= 5; i++)
            {
                last = _pod.SubmitStep(ctx, "false");
                Assert.That(last.Outcome, Is.EqualTo(StepSubmitOutcome.Incorrect));
                Assert.That(last.AttemptNumber, Is.EqualTo(i));
            }

            Assert.That(_attempts.Calls.Count, Is.EqualTo(5));
            Assert.That(_attempts.Calls.All(c => c.NextStepOrder == null && c.FinalScore == null), Is.True);
            Assert.That(_attempts.Calls[0].Attempt.IsCorrect, Is.False);
            Assert.That(_attempts.Calls[0].Attempt.Reason, Is.EqualTo(AttemptReason.WrongDecision));
            Assert.That(ctx.Session.CurrentStepOrder, Is.EqualTo(1));
            Assert.That(ctx.Score, Is.EqualTo(0m));
        }

        [Test]
        public void Guided_CorrectAfterFailures_KeepsTheFullWeight()
        {
            var ctx = Arrange(SessionMode.Guided);
            _pod.SubmitStep(ctx, "false");
            _pod.SubmitStep(ctx, "false");

            var result = _pod.SubmitStep(ctx, "true");

            Assert.That(result.Outcome, Is.EqualTo(StepSubmitOutcome.Correct));
            Assert.That(result.AttemptNumber, Is.EqualTo(3));
            Assert.That(ctx.Score, Is.EqualTo(Weight));
        }

        [Test]
        public void Assessment_RetriesArePenalized()
        {
            var ctx = Arrange(SessionMode.Assessment);

            _pod.SubmitStep(ctx, "false");
            var result = _pod.SubmitStep(ctx, "true");

            Assert.That(result.AttemptNumber, Is.EqualTo(2));
            Assert.That(ctx.Score, Is.EqualTo(Weight * 0.80m));
        }

        [Test]
        public void Assessment_TheFourthFailureForcesTheAdvanceWithoutScore()
        {
            var ctx = Arrange(SessionMode.Assessment);
            for (int i = 1; i <= 3; i++)
                Assert.That(_pod.SubmitStep(ctx, "false").Outcome, Is.EqualTo(StepSubmitOutcome.Incorrect));

            var forced = _pod.SubmitStep(ctx, "false");

            var call = _attempts.Calls.Last();
            Assert.That(forced.Outcome, Is.EqualTo(StepSubmitOutcome.FailedForced));
            Assert.That(forced.AttemptNumber, Is.EqualTo(4));
            Assert.That(call.Attempt.IsCorrect, Is.False);
            Assert.That(call.NextStepOrder, Is.EqualTo(2));
            Assert.That(ctx.Session.CurrentStepOrder, Is.EqualTo(2));
            Assert.That(ctx.Score, Is.EqualTo(0m));
        }

        [Test]
        public void FreePractice_NeverPenalizesNorForcesTheAdvance()
        {
            var ctx = Arrange(SessionMode.FreePractice);
            for (int i = 1; i <= 5; i++)
                Assert.That(_pod.SubmitStep(ctx, "false").Outcome, Is.EqualTo(StepSubmitOutcome.Incorrect));

            var result = _pod.SubmitStep(ctx, "true");

            Assert.That(result.Outcome, Is.EqualTo(StepSubmitOutcome.Correct));
            Assert.That(result.AttemptNumber, Is.EqualTo(6));
            Assert.That(ctx.Score, Is.EqualTo(Weight));
        }

        // ── Último paso: cierre de la sesión y puntaje final ─────────────

        [Test]
        public void SubmitStep_CorrectOnTheLastStep_ClosesTheSessionWithTheFinalScore()
        {
            for (int stepId = 11; stepId <= 13; stepId++) _attempts.SeedCorrect(SessionId, stepId);
            var ctx = Arrange(SessionMode.Assessment, currentStep: 4);

            var result = _pod.SubmitStep(ctx, StepAnswers.Complete);

            var call = _attempts.Calls.Single();
            Assert.That(result.Outcome, Is.EqualTo(StepSubmitOutcome.SessionCompleted));
            Assert.That(call.NextStepOrder, Is.Null);
            Assert.That(call.FinalScore, Is.EqualTo(Weight * 4));
            Assert.That(result.SessionScore, Is.EqualTo(Weight * 4));
            Assert.That(ctx.Session.Status, Is.EqualTo(SessionStatus.Completed));
            Assert.That(ctx.Session.FinalScore, Is.EqualTo(Weight * 4));
            Assert.That(ctx.Session.CompletedAt, Is.Not.Null);
            Assert.That(_sessions.Stored(SessionId).Status, Is.EqualTo(SessionStatus.Completed));
        }

        [Test]
        public void SubmitStep_ClosingScore_IsTheOneTheDatabaseStored()
        {
            // Tres pasos de peso 1/3: el puntaje calculado (0.9999…) se guarda con 2 decimales
            // (DECIMAL(5,2)) y la sesión muestra el guardado, no el calculado.
            _attempts.SeedCorrect(SessionId, 11);
            _attempts.SeedCorrect(SessionId, 12);
            var ctx = Arrange(SessionMode.Assessment, currentStep: 3,
                stepTypes: new[] { StepType.LineClearance, StepType.LineClearance, StepType.Complete });

            _pod.SubmitStep(ctx, StepAnswers.Complete);

            Assert.That(_attempts.Calls.Single().FinalScore, Is.Not.EqualTo(1.00m), "El caso necesita más de 2 decimales.");
            Assert.That(ctx.Session.FinalScore, Is.EqualTo(1.00m));
        }

        [Test]
        public void Skip_WhenTheRecordFailsOnAStepWithProgress_KeepsTheProgress()
        {
            var ctx = Arrange(SessionMode.FreePractice,
                stepTypes: new[] { StepType.ResourceSetup, StepType.LineClearance, StepType.LineClearance, StepType.Complete });
            ctx.State.PreloadedEquipment = new PreloadedEquipment { ShopOrder = "1" };
            _attempts.FailWith = new InvalidOperationException("la BD no respondió");

            Assert.Throws<InvalidOperationException>((Action)(() => _pod.Skip(ctx, 2)));

            Assert.That(ctx.State.PreloadedEquipment, Is.Not.Null, "El avance parcial se descarta solo después de guardar.");
            Assert.That(ctx.State.Order, Is.Null, "La orden por defecto no se crea si el intento no se guardó.");
            Assert.That(ctx.Session.CurrentStepOrder, Is.EqualTo(1));
        }

        // ── Complete ─────────────────────────────────────────────────────

        [Test]
        public void Complete_WithTheLotRowSelected_ClosesTheSessionAndRemovesTheRow()
        {
            for (int stepId = 11; stepId <= 13; stepId++) _attempts.SeedCorrect(SessionId, stepId);
            var ctx = Arrange(SessionMode.Assessment, currentStep: 4, withOrder: true);
            string rowId = ctx.State.Order.Rows[0].RowId;

            var result = _pod.Complete(ctx, new[] { rowId });

            Assert.That(result.Step.Outcome, Is.EqualTo(StepSubmitOutcome.SessionCompleted));
            Assert.That(_attempts.Calls.Single().FinalScore, Is.EqualTo(Weight * 4));
            Assert.That(ctx.Session.Status, Is.EqualTo(SessionStatus.Completed));
            Assert.That(ctx.Session.FinalScore, Is.EqualTo(Weight * 4));
            Assert.That(result.Completed.Count, Is.EqualTo(1));
            Assert.That(ctx.State.Order.ActiveRows(), Is.Empty);
        }

        [Test]
        public void Complete_WithoutASelectedRow_IsAFailedAttemptAndKeepsTheRow()
        {
            var ctx = Arrange(SessionMode.Assessment, currentStep: 4, withOrder: true);

            var result = _pod.Complete(ctx, new string[0]);

            var call = _attempts.Calls.Single();
            Assert.That(result.Step.Outcome, Is.EqualTo(StepSubmitOutcome.Incorrect));
            Assert.That(result.FeedbackReason, Is.EqualTo(PodAttemptReason.NoRowToComplete));
            Assert.That(call.Attempt.IsCorrect, Is.False);
            Assert.That(call.Attempt.Reason, Is.EqualTo(AttemptReason.MissingSelection));
            Assert.That(call.Attempt.Action, Is.EqualTo(AttemptActions.Complete));
            Assert.That(call.NextStepOrder, Is.Null);
            Assert.That(ctx.State.Order.ActiveRows().Count, Is.EqualTo(1));
        }

        [Test]
        public void Complete_ClickedOutOfSequence_InAssessment_IsAFailedAttemptOfTheCurrentStep()
        {
            var ctx = Arrange(SessionMode.Assessment, withOrder: true);

            var result = _pod.Complete(ctx, new[] { ctx.State.Order.Rows[0].RowId });

            var call = _attempts.Calls.Single();
            Assert.That(result.OutOfSequence, Is.True);
            Assert.That(result.Step.Outcome, Is.EqualTo(StepSubmitOutcome.Incorrect));
            Assert.That(call.StepOrder, Is.EqualTo(1));
            Assert.That(call.Attempt.Reason, Is.EqualTo(AttemptReason.OutOfSequence));
            Assert.That(call.Attempt.IsCorrect, Is.False);
        }

        [TestCase(SessionMode.Guided)]
        [TestCase(SessionMode.FreePractice)]
        public void Complete_ClickedOutOfSequence_InGuidedAndFreePractice_RecordsNothing(SessionMode mode)
        {
            var ctx = Arrange(mode, withOrder: true);

            var result = _pod.Complete(ctx, new[] { ctx.State.Order.Rows[0].RowId });

            Assert.That(result.OutOfSequence, Is.True);
            Assert.That(_attempts.Calls, Is.Empty);
            Assert.That(ctx.Session.CurrentStepOrder, Is.EqualTo(1));
        }

        [Test]
        public void Complete_AssessmentFourthFailure_ForcesTheAdvanceAndClearsThePod()
        {
            // Complete como primer paso para que el avance forzado no cierre la sesión.
            var ctx = Arrange(SessionMode.Assessment, withOrder: true,
                stepTypes: new[] { StepType.Complete, StepType.LineClearance, StepType.LineClearance, StepType.LineClearance });
            for (int i = 1; i <= 3; i++)
                Assert.That(_pod.Complete(ctx, new string[0]).Step.Outcome, Is.EqualTo(StepSubmitOutcome.Incorrect));

            var forced = _pod.Complete(ctx, new string[0]);

            Assert.That(forced.Step.Outcome, Is.EqualTo(StepSubmitOutcome.FailedForced));
            Assert.That(_attempts.Calls.Last().NextStepOrder, Is.EqualTo(2));
            Assert.That(ctx.Session.CurrentStepOrder, Is.EqualTo(2));
            Assert.That(ctx.State.Order.ActiveRows(), Is.Empty, "El POD queda como tras un Complete hecho.");
            Assert.That(forced.Completed, Is.Not.Empty);
        }

        [Test]
        public void OutOfSequence_AssessmentFourthWrongClick_ForcesTheAdvanceAndRestoresThePod()
        {
            // Resource Setup como paso actual y cuatro Complete: el cuarto fuerza el avance y el
            // POD queda con la orden por defecto, como si Resource Setup se hubiera hecho.
            var ctx = Arrange(SessionMode.Assessment,
                stepTypes: new[] { StepType.ResourceSetup, StepType.LineClearance, StepType.LineClearance, StepType.Complete });
            for (int i = 1; i <= 3; i++)
                Assert.That(_pod.Complete(ctx, new string[0]).Step.Outcome, Is.EqualTo(StepSubmitOutcome.Incorrect));
            Assert.That(ctx.State.Order, Is.Null);

            var forced = _pod.Complete(ctx, new string[0]);

            Assert.That(forced.OutOfSequence, Is.True);
            Assert.That(forced.Step.Outcome, Is.EqualTo(StepSubmitOutcome.FailedForced));
            Assert.That(forced.PodChanged, Is.True);
            Assert.That(ctx.Session.CurrentStepOrder, Is.EqualTo(2));
            Assert.That(ctx.State.Order.ShopOrderInput, Is.EqualTo(PodDefaults.DefaultShopOrder));
        }

        // ── Skip y avance forzado (Práctica libre) ───────────────────────

        [Test]
        public void Skip_AdvancesEachStepAsAForcedAttemptWithReasonSkipped()
        {
            var ctx = Arrange(SessionMode.FreePractice);

            var result = _pod.Skip(ctx, targetOrder: 3);

            Assert.That(_attempts.Calls.Count, Is.EqualTo(2));
            Assert.That(_attempts.Calls.Select(c => c.StepOrder), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(_attempts.Calls.Select(c => c.NextStepOrder), Is.EqualTo(new int?[] { 2, 3 }));
            Assert.That(_attempts.Calls.All(c => !c.Attempt.IsCorrect), Is.True);
            Assert.That(_attempts.Calls.All(c => c.Attempt.Reason == AttemptReason.Skipped), Is.True);
            Assert.That(_attempts.Calls.All(c => c.Attempt.Action == AttemptActions.Skip), Is.True);
            Assert.That(result.Skipped, Is.True);
            Assert.That(result.Step.Outcome, Is.EqualTo(StepSubmitOutcome.FailedForced));
            Assert.That(ctx.Session.CurrentStepOrder, Is.EqualTo(3));
            Assert.That(ctx.Score, Is.EqualTo(0m), "Un paso saltado no suma.");
        }

        [Test]
        public void Skip_ToOnePastTheLastStep_ClosesTheSession()
        {
            var ctx = Arrange(SessionMode.FreePractice, currentStep: 4);

            _pod.Skip(ctx, targetOrder: 5);

            var call = _attempts.Calls.Single();
            Assert.That(call.NextStepOrder, Is.Null);
            Assert.That(call.FinalScore, Is.EqualTo(0m));
            Assert.That(ctx.Session.Status, Is.EqualTo(SessionStatus.Completed));
        }

        [Test]
        public void Skip_ToAStepThatIsNotAhead_IsIgnored()
        {
            var ctx = Arrange(SessionMode.FreePractice, currentStep: 2);

            var result = _pod.Skip(ctx, targetOrder: 2);

            Assert.That(result.Skipped, Is.False);
            Assert.That(_attempts.Calls, Is.Empty);
            Assert.That(ctx.Session.CurrentStepOrder, Is.EqualTo(2));
        }

        [TestCase(0)]
        [TestCase(6)]
        public void Skip_ToAStepOutsideTheGuide_ThrowsAndRecordsNothing(int target)
        {
            var ctx = Arrange(SessionMode.FreePractice);

            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => _pod.Skip(ctx, target)));
            Assert.That(_attempts.Calls, Is.Empty);
        }

        [TestCase(SessionMode.Guided)]
        [TestCase(SessionMode.Assessment)]
        public void Skip_InAModeThatDoesNotAllowIt_ThrowsAndRecordsNothing(SessionMode mode)
        {
            var ctx = Arrange(mode);

            Assert.Throws<InvalidOperationException>((Action)(() => _pod.Skip(ctx, 3)));
            Assert.That(_attempts.Calls, Is.Empty);
        }

        [Test]
        public void Skip_ThroughResourceSetup_LeavesTheDefaultOrderInThePod()
        {
            var ctx = Arrange(SessionMode.FreePractice,
                stepTypes: new[] { StepType.ResourceSetup, StepType.LineClearance, StepType.LineClearance, StepType.Complete });
            Assert.That(ctx.State.Order, Is.Null);

            _pod.Skip(ctx, targetOrder: 2);

            Assert.That(ctx.State.Order, Is.Not.Null);
            Assert.That(ctx.State.Order.ShopOrderInput, Is.EqualTo(PodDefaults.DefaultShopOrder));
            Assert.That(_store.Load(SessionId).Order, Is.SameAs(ctx.State.Order), "El estado se guardó.");
        }

        [Test]
        public void Skip_ThroughComplete_RemovesTheRowsFromThePod()
        {
            var ctx = Arrange(SessionMode.FreePractice, currentStep: 4, withOrder: true);
            Assert.That(ctx.State.Order.ActiveRows(), Is.Not.Empty);

            _pod.Skip(ctx, targetOrder: 5);

            Assert.That(ctx.State.Order.ActiveRows(), Is.Empty);
            Assert.That(ctx.Session.Status, Is.EqualTo(SessionStatus.Completed));
        }

        // ── Armado ───────────────────────────────────────────────────────

        private decimal Weight => _guide.Steps[0].Weight;

        // Deja la sesión SessionId del operador UserId en el paso currentStep, con la guía de
        // stepTypes (Standard por omisión), y devuelve su contexto como lo carga la página.
        private PodContext Arrange(SessionMode mode, int currentStep = 1, bool withOrder = false,
            StepType[] stepTypes = null)
        {
            stepTypes = stepTypes ?? Standard;
            decimal weight = 1m / stepTypes.Length;
            _guide = new TrainingGuide
            {
                Id = GuideId,
                AreaId = AreaId,
                Title = "Guía de prueba",
                IsPublished = true,
                Steps = stepTypes.Select((type, i) => new TrainingStep
                {
                    Id = 11 + i,
                    TrainingGuideId = GuideId,
                    StepType = type,
                    Order = i + 1,
                    Weight = weight
                }).ToList()
            };
            _catalog.Guide = _guide;
            _catalog.Area = new Area
            {
                Id = AreaId,
                ProductId = 1,
                Name = "Moldeo",
                SerializationType = SerializationType.NonSerialized
            };
            _sessions.Add(new TrainingSession
            {
                Id = SessionId,
                UserId = UserId,
                TrainingGuideId = GuideId,
                Mode = mode,
                Status = SessionStatus.InProgress,
                CurrentStepOrder = currentStep,
                StartedAt = DateTime.UtcNow
            });
            if (withOrder)
                _store.Save(SessionId, new PodState
                {
                    Order = SimulatedOrder.Create(PodDefaults.DefaultShopOrder, "300000002", "Moldeo",
                        SerializationType.NonSerialized)
                });
            return _pod.LoadContext(SessionId, UserId);
        }

        // ── Fakes ────────────────────────────────────────────────────────

        private sealed class FakeSessions : ITrainingSessionRepository
        {
            private readonly Dictionary<int, TrainingSession> _byId = new Dictionary<int, TrainingSession>();

            public void Add(TrainingSession session) => _byId[session.Id] = session;

            // Lo guardado en la "BD", para comprobar lo que persistió.
            public TrainingSession Stored(int id) => _byId[id];

            // Como el repositorio real: una copia nueva en cada lectura, nunca la misma instancia
            // que el PodContext modifica.
            public TrainingSession GetById(int id)
            {
                if (!_byId.TryGetValue(id, out var s)) return null;
                return new TrainingSession
                {
                    Id = s.Id, UserId = s.UserId, TrainingGuideId = s.TrainingGuideId, Mode = s.Mode,
                    Status = s.Status, CurrentStepOrder = s.CurrentStepOrder, FinalScore = s.FinalScore,
                    StartedAt = s.StartedAt, CompletedAt = s.CompletedAt, ResourceSetupType = s.ResourceSetupType
                };
            }
        }

        private sealed class RecordedAttempt
        {
            public TrainingSessionStepAttempt Attempt { get; set; }
            public int StepOrder { get; set; }
            public int? NextStepOrder { get; set; }
            public decimal? FinalScore { get; set; }
        }

        // Como spStepAttemptRecord: guarda el intento y, en lo mismo, avanza la sesión o la
        // cierra con el puntaje final (el contrato de IStepAttemptRepository).
        private sealed class FakeAttempts : IStepAttemptRepository
        {
            private readonly FakeSessions _sessions;
            private readonly List<TrainingSessionStepAttempt> _stored = new List<TrainingSessionStepAttempt>();

            public FakeAttempts(FakeSessions sessions) { _sessions = sessions; }

            public List<RecordedAttempt> Calls { get; } = new List<RecordedAttempt>();
            public int Reads { get; private set; }

            // Se ejecuta al empezar Record, antes de guardar: para ver el estado del contexto en
            // ese momento.
            public Action OnRecord { get; set; }
            public Exception FailWith { get; set; }

            public void SeedCorrect(int sessionId, int stepId)
                => _stored.Add(new TrainingSessionStepAttempt
                {
                    SessionId = sessionId, StepId = stepId, AttemptNumber = 1, IsCorrect = true,
                    Timestamp = DateTime.UtcNow, Reason = AttemptReason.None, Action = "seed"
                });

            public void Record(TrainingSessionStepAttempt attempt, int stepOrder, int? nextStepOrder,
                decimal? finalScore)
            {
                OnRecord?.Invoke();
                if (FailWith != null) throw FailWith;

                // Las mismas precondiciones de spStepAttemptRecord (50001 y 50002): avanzar y
                // cerrar son excluyentes, y la sesión tiene que seguir en curso en ese paso y con
                // ese número de intento.
                if (nextStepOrder.HasValue && finalScore.HasValue)
                    throw new InvalidOperationException("avanzar y cerrar la sesión son excluyentes.");
                var session = _sessions.Stored(attempt.SessionId);
                int previous = _stored.Count(a => a.SessionId == attempt.SessionId && a.StepId == attempt.StepId);
                if (session.Status != SessionStatus.InProgress || session.CurrentStepOrder != stepOrder ||
                    previous != attempt.AttemptNumber - 1)
                    throw new InvalidOperationException("la sesión cambió mientras se registraba el intento.");

                Calls.Add(new RecordedAttempt
                {
                    Attempt = attempt, StepOrder = stepOrder, NextStepOrder = nextStepOrder, FinalScore = finalScore
                });
                _stored.Add(attempt);

                if (finalScore.HasValue)
                {
                    session.Status      = SessionStatus.Completed;
                    // La columna es DECIMAL(5,2): guarda el puntaje redondeado a 2 decimales.
                    session.FinalScore  = Math.Round(finalScore.Value, 2, MidpointRounding.AwayFromZero);
                    session.CompletedAt = DateTime.UtcNow;
                }
                else if (nextStepOrder.HasValue)
                {
                    session.CurrentStepOrder = nextStepOrder.Value;
                }
            }

            // Una lista nueva y modificable, como la del repositorio real (PodService le agrega
            // el intento nuevo).
            public IList<TrainingSessionStepAttempt> GetBySessionId(int sessionId)
            {
                Reads++;
                return _stored.Where(a => a.SessionId == sessionId).ToList();
            }
        }

        private sealed class FakeCatalog : IGuideCatalog
        {
            public TrainingGuide Guide { get; set; }
            public Area Area { get; set; }
            public int Reads { get; private set; }

            public TrainingGuide GetById(int id) { Reads++; return Guide != null && Guide.Id == id ? Guide : null; }
            public Area GetArea(int areaId) { Reads++; return Area != null && Area.Id == areaId ? Area : null; }
            public IList<Product> GetAllProducts() => new List<Product>();
            public IList<DataCollectionGroup> GetDataCollectionGroups(int areaId) => new List<DataCollectionGroup>();
            public IList<DataCollectionParameter> GetDataCollectionParameters(int areaId) => new List<DataCollectionParameter>();
            public IList<AssemblyComponent> GetAssemblyComponents(int areaId) => new List<AssemblyComponent>();
            public IList<NCCode> GetNCCodes(int areaId) => new List<NCCode>();
            public IList<Operation> GetOperations(int areaId) => new List<Operation>();
        }

        // El núcleo no inicia ni abandona sesiones: si lo hiciera, el test falla.
        private sealed class FakeSessionService : ITrainingSessionService
        {
            public TrainingSession Start(int userId, TrainingGuide guide, SessionMode mode) => throw new NotSupportedException();
            public void Abandon(int sessionId, int userId) => throw new NotSupportedException();
        }

        private sealed class InMemoryPodStateStore : IPodStateStore
        {
            private readonly Dictionary<int, PodState> _states = new Dictionary<int, PodState>();

            public PodState Load(int trainingSessionId)
                => _states.TryGetValue(trainingSessionId, out var state) ? state : null;
            public void Save(int trainingSessionId, PodState state) => _states[trainingSessionId] = state;
            public void Remove(int trainingSessionId) => _states.Remove(trainingSessionId);
            public void RemoveAll() => _states.Clear();
        }

        private sealed class FakeUsers : IUserRepository
        {
            private readonly List<User> _users = new List<User>();
            public void Add(User user) => _users.Add(user);
            public User GetById(int userId) => _users.FirstOrDefault(u => u.Id == userId);
            public User GetByCode1(string code1) => throw new NotSupportedException();
            public int Insert(User user) => throw new NotSupportedException();
            public bool TryInsert(User user) => throw new NotSupportedException();
            public IList<UserSummary> Search(string text, int offset, int pageSize, out int total) => throw new NotSupportedException();
            public void ResetPassword(int userId, string passwordHash) => throw new NotSupportedException();
            public void Unlock(int userId) => throw new NotSupportedException();
            public bool Delete(int callerId, int userId) => throw new NotSupportedException();
            public bool ChooseArea(int userId, int areaId) => throw new NotSupportedException();
            public bool SetArea(int userId, int? areaId) => throw new NotSupportedException();
        }
    }
}
