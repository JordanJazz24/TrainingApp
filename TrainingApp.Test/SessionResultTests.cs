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
    // Spec 025, E1 (P-14): el fin de sesión, con datos en memoria. La regla del detalle en los
    // tres modos (la suma de los puntos por paso es el FinalScore), qué reforzar, la sugerencia
    // por modo, el invariante del dueño y el estado (P-2) y la preselección del Inicio (P-9).
    // El SP de las últimas sesiones y las páginas se prueban en el navegador.

    internal static class SessionResultData
    {
        public const int SessionId = 100;
        public const int UserId    = 1;
        public const int GuideId   = 10;
        public const int AreaId    = 3;
        public static readonly DateTime Start = new DateTime(2026, 11, 2, 15, 0, 0, DateTimeKind.Utc);

        // 5 pasos, pesos que suman 1.00: Resource Setup .30, Line Clearance .20, Assembly .20,
        // Complete .20, Logout .10.
        public static List<TrainingStep> Steps() => new List<TrainingStep>
        {
            new TrainingStep { Id = 1, TrainingGuideId = GuideId, Order = 1, StepType = StepType.ResourceSetup, Weight = 0.30m },
            new TrainingStep { Id = 2, TrainingGuideId = GuideId, Order = 2, StepType = StepType.LineClearance, Weight = 0.20m },
            new TrainingStep { Id = 3, TrainingGuideId = GuideId, Order = 3, StepType = StepType.Assembly,      Weight = 0.20m },
            new TrainingStep { Id = 4, TrainingGuideId = GuideId, Order = 4, StepType = StepType.Complete,      Weight = 0.20m },
            new TrainingStep { Id = 5, TrainingGuideId = GuideId, Order = 5, StepType = StepType.Logout,        Weight = 0.10m }
        };

        public static TrainingGuide Guide(bool published = true, int areaId = AreaId) => new TrainingGuide
        {
            Id = GuideId, AreaId = areaId, Title = "Guía de prueba", IsPublished = published, Steps = Steps()
        };

        // Un intento del paso step; el motivo por defecto de un fallo es una decisión incorrecta.
        public static TrainingSessionStepAttempt Attempt(int step, int number, bool correct, AttemptReason? reason = null)
            => new TrainingSessionStepAttempt
            {
                Id            = step * 100 + number,
                SessionId     = SessionId,
                StepId        = step,
                AttemptNumber = number,
                IsCorrect     = correct,
                Timestamp     = Start.AddMinutes(step * 10 + number),
                Reason        = reason ?? (correct ? AttemptReason.None : AttemptReason.WrongDecision)
            };

        public static TrainingSessionStepAttempt Skip(int step, int number)
            => Attempt(step, number, false, AttemptReason.Skipped);

        // Una sesión completada; FinalScore = el que guardaría el cierre (el mismo ScoreCalculator).
        public static TrainingSession Completed(SessionMode mode, IList<TrainingSessionStepAttempt> attempts,
            SessionStatus status = SessionStatus.Completed, int userId = UserId, int sessionId = SessionId)
            => new TrainingSession
            {
                Id               = sessionId,
                UserId           = userId,
                TrainingGuideId  = GuideId,
                Mode             = mode,
                Status           = status,
                CurrentStepOrder = 5,
                FinalScore       = new ScoreCalculator().CalculateSessionScore(Steps(), attempts, mode),
                StartedAt        = Start,
                CompletedAt      = Start.AddMinutes(25)
            };

        // Todos los pasos a la primera.
        public static List<TrainingSessionStepAttempt> AllAtFirst()
            => Steps().Select(s => Attempt(s.Id, 1, true)).ToList();
    }

    [TestFixture]
    public class SessionResultRuleTests
    {
        private static SessionResultDetail Build(SessionMode mode, IList<TrainingSessionStepAttempt> attempts)
            => SessionResultRule.Build(SessionResultData.Completed(mode, attempts), SessionResultData.Steps(), attempts);

        // Assessment con aciertos al 1.º, 2.º y 3.º intento y un avance forzado al 4.º.
        private static List<TrainingSessionStepAttempt> MixedAssessment() => new List<TrainingSessionStepAttempt>
        {
            SessionResultData.Attempt(1, 1, true),
            SessionResultData.Attempt(2, 1, false), SessionResultData.Attempt(2, 2, true),
            SessionResultData.Attempt(3, 1, false), SessionResultData.Attempt(3, 2, false), SessionResultData.Attempt(3, 3, true),
            SessionResultData.Attempt(4, 1, false), SessionResultData.Attempt(4, 2, false),
            SessionResultData.Attempt(4, 3, false), SessionResultData.Attempt(4, 4, false),
            SessionResultData.Attempt(5, 1, true)
        };

        [Test]
        public void Assessment_PointsPerStepSumToTheStoredFinalScore()
        {
            var attempts = MixedAssessment();
            var detail = Build(SessionMode.Assessment, attempts);

            // .30 + .20 × .80 + .20 × .60 + 0 (avance forzado) + .10 = .68
            Assert.That(detail.Steps.Sum(r => r.PointsEarned.Value), Is.EqualTo(0.68m));
            Assert.That(detail.Steps.Sum(r => r.PointsEarned.Value),
                Is.EqualTo(SessionResultData.Completed(SessionMode.Assessment, attempts).FinalScore));
            Assert.That(detail.Steps.Sum(r => r.PointsPossible.Value), Is.EqualTo(1.00m));
            Assert.That(detail.Score, Is.EqualTo(0.68m));
        }

        [Test]
        public void Assessment_FinalScoreBelowThePassMark_IsFailed()
        {
            var detail = Build(SessionMode.Assessment, MixedAssessment());

            Assert.That(detail.Passed, Is.False);
            Assert.That(detail.WentWell, Is.False);
            Assert.That(detail.PassingScore, Is.EqualTo(0.70m));
        }

        [Test]
        public void Assessment_OutcomesAndAttemptsPerStep()
        {
            var detail = Build(SessionMode.Assessment, MixedAssessment());

            Assert.That(detail.Steps.Select(r => r.Outcome), Is.EqualTo(new[]
            {
                StepResultOutcome.AtFirst, StepResultOutcome.WithFailures, StepResultOutcome.WithFailures,
                StepResultOutcome.WithoutSuccess, StepResultOutcome.AtFirst
            }));
            Assert.That(detail.Steps.Select(r => r.Attempts), Is.EqualTo(new[] { 1, 2, 3, 4, 1 }));
            Assert.That(detail.Steps.Select(r => r.Failures), Is.EqualTo(new[] { 0, 1, 2, 4, 0 }));
        }

        [Test]
        public void Assessment_Reinforce_ListsTheStepsWithFailuresWithTheirLostPoints()
        {
            var detail = Build(SessionMode.Assessment, MixedAssessment());

            Assert.That(detail.Reinforce.Select(r => r.Order), Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(detail.Reinforce.Select(r => r.PointsLost), Is.EqualTo(new decimal?[] { 0.04m, 0.08m, 0.20m }));
        }

        [Test]
        public void Assessment_AllAtFirst_IsPassedAndNothingToReinforce()
        {
            var detail = Build(SessionMode.Assessment, SessionResultData.AllAtFirst());

            Assert.That(detail.Passed, Is.True);
            Assert.That(detail.WentWell, Is.True);
            Assert.That(detail.Score, Is.EqualTo(1.00m));
            Assert.That(detail.Reinforce, Is.Empty);
            Assert.That(detail.Steps, Has.Count.EqualTo(5));
        }

        [Test]
        public void Assessment_ExactlyThePassMark_IsPassed()
        {
            // Justo el mínimo: .30 + .20 + .20 = .70 con los dos últimos pasos sin acierto
            // (avance forzado). "70 % o más" aprueba.
            var attempts = new List<TrainingSessionStepAttempt>
            {
                SessionResultData.Attempt(1, 1, true), SessionResultData.Attempt(2, 1, true), SessionResultData.Attempt(3, 1, true),
                SessionResultData.Attempt(4, 1, false), SessionResultData.Attempt(4, 2, false),
                SessionResultData.Attempt(4, 3, false), SessionResultData.Attempt(4, 4, false),
                SessionResultData.Attempt(5, 1, false), SessionResultData.Attempt(5, 2, false),
                SessionResultData.Attempt(5, 3, false), SessionResultData.Attempt(5, 4, false)
            };
            var detail = Build(SessionMode.Assessment, attempts);

            Assert.That(detail.Score, Is.EqualTo(0.70m));
            Assert.That(detail.Passed, Is.True);
        }

        [Test]
        public void Guided_ShowsTheScoreWithoutAVerdict_AndTheSumIsTheFinalScore()
        {
            var attempts = SessionResultData.AllAtFirst();
            var detail = Build(SessionMode.Guided, attempts);

            Assert.That(detail.Passed, Is.Null);
            Assert.That(detail.Score, Is.EqualTo(1.00m));
            Assert.That(detail.Steps.Sum(r => r.PointsEarned.Value), Is.EqualTo(detail.Score));
            Assert.That(detail.Reinforce, Is.Empty);
            Assert.That(detail.WentWell, Is.True);
        }

        // Guided no penaliza: un acierto tardío vale el peso entero y la suma sigue siendo el FinalScore.
        [Test]
        public void Guided_LateSuccess_ScoresFullWeight_AndSumsToTheFinalScore()
        {
            var attempts = SessionResultData.AllAtFirst();
            attempts.Add(SessionResultData.Attempt(2, 2, true));
            attempts.RemoveAll(a => a.StepId == 2 && a.AttemptNumber == 1);
            attempts.Add(SessionResultData.Attempt(2, 1, false));
            var session = SessionResultData.Completed(SessionMode.Guided, attempts);
            var detail = SessionResultRule.Build(session, SessionResultData.Steps(), attempts);

            Assert.That(detail.Steps.Sum(r => r.PointsEarned.Value), Is.EqualTo(session.FinalScore));
            Assert.That(detail.Steps[1].PointsEarned, Is.EqualTo(0.20m));
        }

        [Test]
        public void FreePractice_HasNoScoreAndNoPoints()
        {
            var detail = Build(SessionMode.FreePractice, SessionResultData.AllAtFirst());

            Assert.That(detail.Score, Is.Null);
            Assert.That(detail.Passed, Is.Null);
            Assert.That(detail.Steps.All(r => r.PointsEarned == null && r.PointsPossible == null && r.PointsLost == null), Is.True);
            Assert.That(detail.AtFirstCount, Is.EqualTo(5));
            Assert.That(detail.StepCount, Is.EqualTo(5));
            Assert.That(detail.WentWell, Is.True);
        }

        // Saltado sin fallos, saltado con fallos y acierto tras un fallo.
        private static List<TrainingSessionStepAttempt> PracticeWithSkips() => new List<TrainingSessionStepAttempt>
        {
            SessionResultData.Attempt(1, 1, true),
            SessionResultData.Skip(2, 1),                                                   // saltado limpio
            SessionResultData.Attempt(3, 1, false), SessionResultData.Skip(3, 2),            // saltado con 1 fallo
            SessionResultData.Attempt(4, 1, false), SessionResultData.Attempt(4, 2, true),  // acierto tras 1 fallo
            SessionResultData.Attempt(5, 1, true)
        };

        [Test]
        public void FreePractice_SkippedAndSkippedWithFailures_AreTheirOwnOutcomes()
        {
            var detail = Build(SessionMode.FreePractice, PracticeWithSkips());

            Assert.That(detail.Steps.Select(r => r.Outcome), Is.EqualTo(new[]
            {
                StepResultOutcome.AtFirst, StepResultOutcome.Skipped, StepResultOutcome.SkippedWithFailures,
                StepResultOutcome.WithFailures, StepResultOutcome.AtFirst
            }));
            // El salto no cuenta como intento ni como fallo.
            Assert.That(detail.Steps[1].Attempts, Is.EqualTo(0));
            Assert.That(detail.Steps[1].Failures, Is.EqualTo(0));
            Assert.That(detail.Steps[2].Attempts, Is.EqualTo(1));
            Assert.That(detail.Steps[2].Failures, Is.EqualTo(1));
            Assert.That(detail.AtFirstCount, Is.EqualTo(2));
        }

        // P-7: un saltado sin fallos NO entra; uno saltado con fallos sí.
        [Test]
        public void FreePractice_Reinforce_ExcludesACleanSkip()
        {
            var detail = Build(SessionMode.FreePractice, PracticeWithSkips());

            Assert.That(detail.Reinforce.Select(r => r.Order), Is.EqualTo(new[] { 3, 4 }));
            Assert.That(detail.WentWell, Is.False);
        }

        // Un salto limpio basta para que no "salga bien" (sin fallos ni saltos, P-8).
        [Test]
        public void FreePractice_ACleanSkipAlone_IsNotWentWell_ButNothingToReinforce()
        {
            var attempts = SessionResultData.AllAtFirst();
            attempts.RemoveAll(a => a.StepId == 2);
            attempts.Add(SessionResultData.Skip(2, 1));
            var detail = Build(SessionMode.FreePractice, attempts);

            Assert.That(detail.Reinforce, Is.Empty);
            Assert.That(detail.WentWell, Is.False);
        }

        [Test]
        public void Reasons_AreCountedPerType_AndNeverRevealTheAnswer()
        {
            var attempts = new List<TrainingSessionStepAttempt>
            {
                SessionResultData.Attempt(1, 1, false, AttemptReason.OutOfSequence),
                SessionResultData.Attempt(1, 2, false, AttemptReason.WrongDecision),
                SessionResultData.Attempt(1, 3, false, AttemptReason.OutOfSequence),
                SessionResultData.Attempt(1, 4, true),
                SessionResultData.Attempt(2, 1, true), SessionResultData.Attempt(3, 1, true),
                SessionResultData.Attempt(4, 1, true), SessionResultData.Attempt(5, 1, true)
            };
            var row = Build(SessionMode.FreePractice, attempts).Steps[0];

            Assert.That(row.Failures, Is.EqualTo(3));
            Assert.That(row.Reasons.Select(r => (r.Reason, r.Count)),
                Is.EqualTo(new[] { (AttemptReason.OutOfSequence, 2), (AttemptReason.WrongDecision, 1) }));
        }

        // Los intentos anteriores a la spec 013 no traen motivo: cuentan como fallo, sin tipo.
        [Test]
        public void AFailureWithoutAReason_CountsAsAFailure_ButHasNoReasonRow()
        {
            var attempts = SessionResultData.AllAtFirst();
            attempts.RemoveAll(a => a.StepId == 1);
            attempts.Add(new TrainingSessionStepAttempt
            {
                SessionId = SessionResultData.SessionId, StepId = 1, AttemptNumber = 1, IsCorrect = false,
                Timestamp = SessionResultData.Start.AddMinutes(11), Reason = null
            });
            attempts.Add(SessionResultData.Attempt(1, 2, true));
            var row = Build(SessionMode.FreePractice, attempts).Steps[0];

            Assert.That(row.Failures, Is.EqualTo(1));
            Assert.That(row.Reasons, Is.Empty);
            Assert.That(row.NeedsReinforcement, Is.True);
        }

        [Test]
        public void Duration_IsCompletedMinusStarted()
        {
            var detail = Build(SessionMode.Assessment, SessionResultData.AllAtFirst());

            Assert.That(detail.Duration, Is.EqualTo(TimeSpan.FromMinutes(25)));
        }

        // La guía se muestra entera: un paso sin ningún intento sigue teniendo su renglón.
        [Test]
        public void EveryStepOfTheGuideHasARow_EvenWithoutAttempts()
        {
            var attempts = SessionResultData.AllAtFirst().Where(a => a.StepId != 3).ToList();
            var detail = Build(SessionMode.FreePractice, attempts);

            Assert.That(detail.Steps, Has.Count.EqualTo(5));
            Assert.That(detail.Steps[2].Attempts, Is.EqualTo(0));
            Assert.That(detail.Steps[2].NeedsReinforcement, Is.False);
        }
    }

    [TestFixture]
    public class ModePolicySuggestionTests
    {
        // D-332 / P-8: la progresión de modos. Assessment aprobado no sugiere modo.
        [TestCase(SessionMode.Guided,       true,  SessionMode.FreePractice)]
        [TestCase(SessionMode.Guided,       false, SessionMode.FreePractice)]
        [TestCase(SessionMode.FreePractice, true,  SessionMode.Assessment)]
        [TestCase(SessionMode.FreePractice, false, SessionMode.FreePractice)]
        [TestCase(SessionMode.Assessment,   false, SessionMode.FreePractice)]
        public void SuggestedNextMode_FollowsTheProgression(SessionMode mode, bool wentWell, SessionMode expected)
        {
            Assert.That(ModePolicy.For(mode).SuggestedNextMode(wentWell), Is.EqualTo(expected));
        }

        [Test]
        public void ApprovedAssessment_SuggestsNoMode()
        {
            Assert.That(ModePolicy.Assessment.SuggestedNextMode(true), Is.Null);
        }
    }

    [TestFixture]
    public class SessionResultServiceTests
    {
        private const string Start = SessionResultService.StartUrl;

        private User _operator;
        private FakeUsers _users;
        private FakeSource _source;
        private SessionResultService _service;

        [SetUp]
        public void SetUp()
        {
            _operator = new User { Id = SessionResultData.UserId, Role = UserRole.Operator, AreaId = SessionResultData.AreaId };
            _users    = new FakeUsers(_operator, new User { Id = 99, Role = UserRole.Administrator });
            _source   = new FakeSource { Guide = SessionResultData.Guide() };
            _service  = new SessionResultService(_users, _source);
        }

        private SessionResult Get(SessionMode mode, IList<TrainingSessionStepAttempt> attempts)
        {
            _source.Add(SessionResultData.Completed(mode, attempts), attempts);
            return _service.Get(SessionResultData.UserId, SessionResultData.SessionId);
        }

        private static List<TrainingSessionStepAttempt> Failed() => new List<TrainingSessionStepAttempt>
        {
            SessionResultData.Attempt(1, 1, false), SessionResultData.Attempt(1, 2, false),
            SessionResultData.Attempt(1, 3, false), SessionResultData.Attempt(1, 4, false),
            SessionResultData.Attempt(2, 1, false), SessionResultData.Attempt(2, 2, false),
            SessionResultData.Attempt(2, 3, false), SessionResultData.Attempt(2, 4, false),
            SessionResultData.Attempt(3, 1, true), SessionResultData.Attempt(4, 1, true),
            SessionResultData.Attempt(5, 1, true)
        };

        // ---- AC-4: la acción sugerida de cada fila de la tabla, hacia el Inicio con modo y guía ----

        [Test]
        public void Guided_SuggestsFreePractice_AndRepeatsGuided()
        {
            var result = Get(SessionMode.Guided, SessionResultData.AllAtFirst());

            Assert.That(result.Primary.Mode, Is.EqualTo(SessionMode.FreePractice));
            Assert.That(result.Primary.Url, Is.EqualTo(Start + "?mode=2&guide=10"));
            Assert.That(result.Repeat.Mode, Is.EqualTo(SessionMode.Guided));
            Assert.That(result.Repeat.Url, Is.EqualTo(Start + "?mode=0&guide=10"));
            Assert.That(result.Home.IsHome, Is.True);
        }

        [Test]
        public void FreePracticeWithoutFailuresOrSkips_SuggestsAssessment()
        {
            var result = Get(SessionMode.FreePractice, SessionResultData.AllAtFirst());

            Assert.That(result.Primary.Mode, Is.EqualTo(SessionMode.Assessment));
            Assert.That(result.Primary.Url, Is.EqualTo(Start + "?mode=1&guide=10"));
            Assert.That(result.Repeat.Mode, Is.EqualTo(SessionMode.FreePractice));
        }

        // D-342: con fallos o saltos la sugerida es Práctica libre de nuevo; Repetir sería lo mismo.
        [Test]
        public void FreePracticeWithFailures_SuggestsFreePracticeAgain_WithoutRepeat()
        {
            var attempts = SessionResultData.AllAtFirst();
            attempts.RemoveAll(a => a.StepId == 2);
            attempts.Add(SessionResultData.Attempt(2, 1, false));
            attempts.Add(SessionResultData.Attempt(2, 2, true));
            var result = Get(SessionMode.FreePractice, attempts);

            Assert.That(result.Primary.Mode, Is.EqualTo(SessionMode.FreePractice));
            Assert.That(result.Repeat, Is.Null);
        }

        [Test]
        public void FreePracticeWithASkip_SuggestsFreePracticeAgain_WithoutRepeat()
        {
            var attempts = SessionResultData.AllAtFirst();
            attempts.RemoveAll(a => a.StepId == 2);
            attempts.Add(SessionResultData.Skip(2, 1));
            var result = Get(SessionMode.FreePractice, attempts);

            Assert.That(result.Primary.Mode, Is.EqualTo(SessionMode.FreePractice));
            Assert.That(result.Repeat, Is.Null);
        }

        [Test]
        public void FailedAssessment_SuggestsFreePractice_AndRepeatsAssessment()
        {
            var result = Get(SessionMode.Assessment, Failed());

            Assert.That(result.Detail.Passed, Is.False);
            Assert.That(result.Primary.Mode, Is.EqualTo(SessionMode.FreePractice));
            Assert.That(result.Repeat.Mode, Is.EqualTo(SessionMode.Assessment));
        }

        [Test]
        public void PassedAssessment_SuggestsGoingHome_AndRepeatsAssessment()
        {
            var result = Get(SessionMode.Assessment, SessionResultData.AllAtFirst());

            Assert.That(result.Detail.Passed, Is.True);
            Assert.That(result.Primary.IsHome, Is.True);
            Assert.That(result.Primary.Url, Is.EqualTo(Start));
            Assert.That(result.Repeat.Mode, Is.EqualTo(SessionMode.Assessment));
        }

        // P-8: una guía que ya no se puede iniciar (no publicada o de otra área) → solo Ir al Inicio.
        [Test]
        public void AnUnpublishedGuide_OnlyOffersGoingHome()
        {
            _source.Guide = SessionResultData.Guide(published: false);
            var result = Get(SessionMode.Guided, SessionResultData.AllAtFirst());

            Assert.That(result.Primary.IsHome, Is.True);
            Assert.That(result.Repeat, Is.Null);
        }

        [Test]
        public void AGuideOfAnotherArea_OnlyOffersGoingHome()
        {
            _source.Guide = SessionResultData.Guide(areaId: SessionResultData.AreaId + 1);
            var result = Get(SessionMode.FreePractice, SessionResultData.AllAtFirst());

            Assert.That(result.Primary.IsHome, Is.True);
            Assert.That(result.Repeat, Is.Null);
        }

        [Test]
        public void TheResultCarriesTheGuideAndItsAreaText()
        {
            _source.AreaTexts[SessionResultData.AreaId] = "Sensores · LoFlo";
            var result = Get(SessionMode.Guided, SessionResultData.AllAtFirst());

            Assert.That(result.GuideId, Is.EqualTo(SessionResultData.GuideId));
            Assert.That(result.GuideTitle, Is.EqualTo("Guía de prueba"));
            Assert.That(result.GuideText, Is.EqualTo("Sensores · LoFlo"));

            _operator.ProductName = "Sensores";
            _operator.AreaName    = "LoFlo";
            Assert.That(_service.Get(SessionResultData.UserId, SessionResultData.SessionId).UserAreaText,
                Is.EqualTo("Sensores · LoFlo"));
        }

        // ---- P-2: el dueño y el estado ----

        [Test]
        public void ASessionOfAnotherOperator_IsNull()
        {
            var attempts = SessionResultData.AllAtFirst();
            _source.Add(SessionResultData.Completed(SessionMode.Guided, attempts, userId: 2), attempts);

            Assert.That(_service.Get(SessionResultData.UserId, SessionResultData.SessionId), Is.Null);
        }

        [TestCase(SessionStatus.InProgress)]
        [TestCase(SessionStatus.Abandoned)]
        public void ASessionThatIsNotCompleted_IsNull(SessionStatus status)
        {
            var attempts = SessionResultData.AllAtFirst();
            _source.Add(SessionResultData.Completed(SessionMode.Guided, attempts, status), attempts);

            Assert.That(_service.Get(SessionResultData.UserId, SessionResultData.SessionId), Is.Null);
        }

        [Test]
        public void ASessionThatDoesNotExist_IsNull()
        {
            Assert.That(_service.Get(SessionResultData.UserId, 12345), Is.Null);
        }

        // Quien no entrena (un admin) no es un intento: excepción, la página lo manda a su inicio.
        [Test]
        public void AnAdministrator_Throws()
        {
            var attempts = SessionResultData.AllAtFirst();
            _source.Add(SessionResultData.Completed(SessionMode.Guided, attempts, userId: 99), attempts);

            Assert.Throws<UnauthorizedAccessException>((Action)(() => _service.Get(99, SessionResultData.SessionId)));
            Assert.Throws<UnauthorizedAccessException>((Action)(() => _service.GetRecent(99)));
        }

        [Test]
        public void AnUnknownUser_Throws()
        {
            Assert.Throws<UnauthorizedAccessException>((Action)(() => _service.Get(555, SessionResultData.SessionId)));
        }

        // ---- D-335, P-5: las últimas 5 ----

        private static CompletedSessionItem Item(int id, int minutes, int areaId = SessionResultData.AreaId, string area = "LoFlo")
            => new CompletedSessionItem
            {
                SessionId = id, TrainingGuideId = SessionResultData.GuideId, Mode = SessionMode.Guided,
                CompletedAt = SessionResultData.Start.AddMinutes(minutes), AreaId = areaId, AreaName = area, ProductName = "Sensores"
            };

        [Test]
        public void GetRecent_ReturnsAtMostFive_MostRecentFirst()
        {
            for (int i = 1; i <= 7; i++) _source.Recent.Add(Item(i, i));

            var recent = _service.GetRecent(SessionResultData.UserId);

            Assert.That(SessionResultService.RecentCount, Is.EqualTo(5));
            Assert.That(recent.Select(r => r.SessionId), Is.EqualTo(new[] { 7, 6, 5, 4, 3 }));
        }

        [Test]
        public void GetRecent_WithoutCompletedSessions_IsEmpty()
        {
            Assert.That(_service.GetRecent(SessionResultData.UserId), Is.Empty);
        }

        [Test]
        public void GetRecent_ShowsProductAndAreaOnlyForAnotherArea()
        {
            _source.Recent.Add(Item(1, 1));
            _source.Recent.Add(Item(2, 2, areaId: 4, area: "Moldeo"));

            var recent = _service.GetRecent(SessionResultData.UserId);

            Assert.That(recent.Single(r => r.SessionId == 1).AreaText, Is.Null);
            Assert.That(recent.Single(r => r.SessionId == 2).AreaText, Is.EqualTo("Sensores · Moldeo"));
        }

        // ---- Fakes ----

        private sealed class FakeUsers : IUserRepository
        {
            private readonly List<User> _all;
            public FakeUsers(params User[] users) { _all = users.ToList(); }

            public User GetById(int userId) => _all.FirstOrDefault(u => u.Id == userId);
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

        private sealed class FakeSource : ISessionResultSource
        {
            private readonly Dictionary<int, TrainingSession> _sessions = new Dictionary<int, TrainingSession>();
            private readonly Dictionary<int, IList<TrainingSessionStepAttempt>> _attempts = new Dictionary<int, IList<TrainingSessionStepAttempt>>();
            public TrainingGuide Guide { get; set; }
            public Dictionary<int, string> AreaTexts { get; } = new Dictionary<int, string>();
            public List<CompletedSessionItem> Recent { get; } = new List<CompletedSessionItem>();

            public void Add(TrainingSession session, IList<TrainingSessionStepAttempt> attempts)
            {
                _sessions[session.Id] = session;
                _attempts[session.Id] = attempts;
            }

            public TrainingSession GetSession(int sessionId) => _sessions.TryGetValue(sessionId, out var s) ? s : null;
            public IList<TrainingSessionStepAttempt> GetAttempts(int sessionId) => _attempts[sessionId];
            public TrainingGuide GetGuide(int guideId) => Guide;
            public string AreaText(int areaId) => AreaTexts.TryGetValue(areaId, out var t) ? t : null;
            public IList<CompletedSessionItem> GetRecentCompleted(int userId, int top)
                => Recent.OrderByDescending(i => i.CompletedAt).ThenByDescending(i => i.SessionId).Take(top).ToList();
        }
    }

    // P-9: lo que el Inicio deja elegido por la URL, cada valor por separado.
    [TestFixture]
    public class StartPreselectionTests
    {
        private static OperatorStartOptions Options() => new OperatorStartOptions
        {
            HasArea = true,
            Guides  = new List<TrainingGuide>
            {
                new TrainingGuide { Id = 10, AreaId = 3, IsPublished = true },
                new TrainingGuide { Id = 11, AreaId = 3, IsPublished = true },
                new TrainingGuide { Id = 12, AreaId = 3, IsPublished = false }
            }
        };

        [Test]
        public void AValidModeAndGuide_AreBothPreselected()
        {
            var pre = Options().Preselect("2", "11");

            Assert.That(pre.Mode, Is.EqualTo(SessionMode.FreePractice));
            Assert.That(pre.GuideId, Is.EqualTo(11));
        }

        [TestCase("3")]
        [TestCase("-1")]
        [TestCase("abc")]
        [TestCase("1.5")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("99999999999999999999")]
        public void AnInvalidMode_IsIgnored(string mode)
        {
            Assert.That(Options().Preselect(mode, "10").Mode, Is.Null);
            Assert.That(Options().Preselect(mode, "10").GuideId, Is.EqualTo(10));
        }

        [Test]
        public void AGuideOfAnotherArea_IsIgnored_AndTheModeStays()
        {
            var pre = Options().Preselect("1", "77");

            Assert.That(pre.Mode, Is.EqualTo(SessionMode.Assessment));
            Assert.That(pre.GuideId, Is.Null);
        }

        [Test]
        public void AnUnpublishedGuide_IsIgnored()
        {
            Assert.That(Options().Preselect("1", "12").GuideId, Is.Null);
        }

        [TestCase("abc")]
        [TestCase("-10")]
        [TestCase("")]
        [TestCase(null)]
        public void AnInvalidGuide_IsIgnored_AndTheModeStays(string guide)
        {
            var pre = Options().Preselect("0", guide);

            Assert.That(pre.Mode, Is.EqualTo(SessionMode.Guided));
            Assert.That(pre.GuideId, Is.Null);
        }

        [Test]
        public void WithoutGuides_NothingIsPreselectedButTheMode()
        {
            var pre = new OperatorStartOptions().Preselect("1", "10");

            Assert.That(pre.Mode, Is.EqualTo(SessionMode.Assessment));
            Assert.That(pre.GuideId, Is.Null);
        }
    }
}
