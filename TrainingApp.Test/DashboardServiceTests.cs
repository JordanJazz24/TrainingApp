using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Reports;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 018: lista de Operadores (AC-7, AC-13) con datos en memoria: búsqueda, filtros por
    // estado y guía, 50 por página y la capacidad CanViewReports.
    [TestFixture]
    public class DashboardServiceTests
    {
        private const int AdminId    = 1;
        private const int OperatorId = 2;
        private const int LoFlo      = 10;
        private const int Moldeo     = 20;

        private FakeDashboardRepository _dashboard;
        private DashboardService _service;

        [SetUp]
        public void SetUp()
        {
            _dashboard = new FakeDashboardRepository();
            _dashboard.Guides.Add(new DashboardGuide { Id = LoFlo,  Title = "LoFlo" });
            _dashboard.Guides.Add(new DashboardGuide { Id = Moldeo, Title = "Moldeo" });

            var users = new FakeUsers();
            users.Add(new User { Id = AdminId,    Code1 = "A1", Role = UserRole.Administrator });
            users.Add(new User { Id = OperatorId, Code1 = "O1", Role = UserRole.Operator });
            _service = new DashboardService(_dashboard, users);
        }

        private void Operator(int id, string name, string code1 = null)
            => _dashboard.Operators.Add(new DashboardOperator { UserId = id, Name = name, Code1 = code1 ?? "C" + id });

        private void Session(int userId, int guideId, SessionMode mode, decimal score, int day,
            SessionStatus status = SessionStatus.Completed)
            => _dashboard.Sessions.Add(new DashboardSession
            {
                UserId = userId, TrainingGuideId = guideId, Mode = mode, Status = status, FinalScore = score,
                StartedAt = new DateTime(2026, 10, 1).AddDays(day)
            });

        private IList<string> Names(OperatorFilter filter)
            => _service.SearchOperators(AdminId, filter).Rows.Select(r => r.Name).ToList();

        // Un operador sin la capacidad recibe una excepción, no datos (AC-2).
        [Test]
        public void WithoutCanViewReports_Throws()
        {
            Assert.Throws<UnauthorizedAccessException>((Action)(() => _service.SearchOperators(OperatorId, new OperatorFilter())));
            Assert.Throws<UnauthorizedAccessException>((Action)(() => _service.SearchOperators(99, new OperatorFilter())));
            Assert.Throws<UnauthorizedAccessException>((Action)(() => _service.GetGuides(OperatorId)));
        }

        // AC-7: quien nunca entrenó aparece como Sin iniciar y el filtro lo muestra.
        [Test]
        public void OperatorWithoutSessions_IsNotStarted()
        {
            Operator(100, "Ana");
            Operator(101, "Beto");
            Session(101, LoFlo, SessionMode.Guided, 1m, 0);

            var page = _service.SearchOperators(AdminId, new OperatorFilter());
            var ana = page.Rows.Single(r => r.Name == "Ana");

            Assert.That(ana.NotStarted, Is.True);
            Assert.That(ana.LastActivity, Is.Null);
            Assert.That(page.Rows.Single(r => r.Name == "Beto").NotStarted, Is.False);
            Assert.That(Names(new OperatorFilter { State = ReadinessState.NotStarted }), Is.EqualTo(new[] { "Ana" }));
        }

        // Estado por guía, en el orden de las guías, con mejor, último y última actividad.
        [Test]
        public void Row_HasStatePerGuide()
        {
            Operator(100, "Ana");
            Session(100, Moldeo, SessionMode.Assessment, 0.80m, 0);
            Session(100, Moldeo, SessionMode.Assessment, 0.60m, 2);
            Session(100, LoFlo, SessionMode.FreePractice, 0.90m, 5);

            var row = _service.SearchOperators(AdminId, new OperatorFilter()).Rows.Single();

            Assert.That(row.Guides.Select(g => g.TrainingGuideId), Is.EqualTo(new[] { LoFlo, Moldeo }));
            Assert.That(row.Guides[0].State, Is.EqualTo(ReadinessState.InTraining));
            Assert.That(row.Guides[1].State, Is.EqualTo(ReadinessState.Passed));
            Assert.That(row.Guides[1].BestScore, Is.EqualTo(0.80m));
            Assert.That(row.Guides[1].LastScore, Is.EqualTo(0.60m));
            Assert.That(row.LastActivity, Is.EqualTo(new DateTime(2026, 10, 6)));
        }

        // Filtros: estado en alguna guía, guía entrenada, y estado en una guía.
        [Test]
        public void Filters_ByStateAndGuide()
        {
            Operator(100, "Ana");     // LoFlo aprobado
            Operator(101, "Beto");    // LoFlo reprobado, Moldeo aprobado
            Operator(102, "Carla");   // solo Moldeo en entrenamiento
            Operator(103, "Dani");    // sin iniciar
            Session(100, LoFlo,  SessionMode.Assessment, 0.90m, 0);
            Session(101, LoFlo,  SessionMode.Assessment, 0.40m, 0);
            Session(101, Moldeo, SessionMode.Assessment, 0.75m, 1);
            Session(102, Moldeo, SessionMode.Guided,     1.00m, 0);

            Assert.That(Names(new OperatorFilter { State = ReadinessState.Passed }), Is.EqualTo(new[] { "Ana", "Beto" }));
            Assert.That(Names(new OperatorFilter { GuideId = Moldeo }), Is.EqualTo(new[] { "Beto", "Carla" }));
            Assert.That(Names(new OperatorFilter { GuideId = LoFlo, State = ReadinessState.Passed }), Is.EqualTo(new[] { "Ana" }));
            Assert.That(Names(new OperatorFilter { GuideId = LoFlo, State = ReadinessState.Failed }), Is.EqualTo(new[] { "Beto" }));
            Assert.That(Names(new OperatorFilter { GuideId = LoFlo, State = ReadinessState.NotStarted }), Is.EqualTo(new[] { "Carla", "Dani" }));
        }

        // Una guía no publicada o un estado inválido (URL manipulada) valen "todas" y "todos".
        [Test]
        public void InvalidFilters_FallBackToAll()
        {
            Operator(100, "Ana");
            Operator(101, "Beto");
            Session(100, LoFlo, SessionMode.Guided, 1m, 0);

            Assert.That(Names(new OperatorFilter { GuideId = 999 }), Is.EqualTo(new[] { "Ana", "Beto" }));
            Assert.That(Names(new OperatorFilter { State = (ReadinessState)42 }), Is.EqualTo(new[] { "Ana", "Beto" }));

            // La página muestra los filtros aplicados, no los que llegaron.
            var applied = _service.SearchOperators(AdminId,
                new OperatorFilter { Text = "  an ", GuideId = 999, State = (ReadinessState)42, Page = 7 }).Applied;
            Assert.That(applied.Text, Is.EqualTo("an"));
            Assert.That(applied.GuideId, Is.Null);
            Assert.That(applied.State, Is.Null);
            Assert.That(applied.Page, Is.EqualTo(1));
        }

        // La búsqueda llega al repositorio recortada y sin espacios de más.
        [Test]
        public void Search_TrimsAndLimitsText()
        {
            _service.SearchOperators(AdminId, new OperatorFilter { Text = "  ana  " });
            Assert.That(_dashboard.LastText, Is.EqualTo("ana"));

            _service.SearchOperators(AdminId, new OperatorFilter { Text = new string('x', 150) });
            Assert.That(_dashboard.LastText.Length, Is.EqualTo(100));
        }

        // AC-13: 120 operadores, 50 por página; una página fuera de rango es la última.
        [Test]
        public void Paging_FiftyPerPage()
        {
            for (int i = 1; i <= 120; i++) Operator(1000 + i, "Op " + i.ToString("000"));

            var first = _service.SearchOperators(AdminId, new OperatorFilter { Page = 1 });
            var last  = _service.SearchOperators(AdminId, new OperatorFilter { Page = 9 });

            Assert.That(first.Total, Is.EqualTo(120));
            Assert.That(first.PageCount, Is.EqualTo(3));
            Assert.That(first.Rows.Count, Is.EqualTo(50));
            Assert.That(first.Rows[0].Name, Is.EqualTo("Op 001"));
            Assert.That(last.Page, Is.EqualTo(3));
            Assert.That(last.Rows.Count, Is.EqualTo(20));
            Assert.That(last.Rows[0].Name, Is.EqualTo("Op 101"));
        }

        // El filtro se aplica antes de paginar: el total y las páginas son de los filtrados.
        [Test]
        public void Paging_CountsOnlyFilteredRows()
        {
            for (int i = 1; i <= 120; i++)
            {
                Operator(1000 + i, "Op " + i.ToString("000"));
                if (i % 2 == 0) Session(1000 + i, LoFlo, SessionMode.Assessment, 0.30m, 0);
            }

            var page = _service.SearchOperators(AdminId, new OperatorFilter { State = ReadinessState.Failed, Page = 2 });

            Assert.That(page.Total, Is.EqualTo(60));
            Assert.That(page.PageCount, Is.EqualTo(2));
            Assert.That(page.Rows.Count, Is.EqualTo(10));
        }

        // Sin operadores: una página vacía, no un error.
        [Test]
        public void NoOperators_EmptyPage()
        {
            var page = _service.SearchOperators(AdminId, new OperatorFilter { Page = 3 });

            Assert.That(page.Rows, Is.Empty);
            Assert.That(page.Total, Is.EqualTo(0));
            Assert.That(page.Page, Is.EqualTo(1));
            Assert.That(page.PageCount, Is.EqualTo(1));
        }

        // ── Detalle del Operador (AC-14) ──────────────────────────────

        // Un id inexistente o de un administrador no tiene detalle (DA-9).
        [Test]
        public void GetOperator_UnknownOrAdmin_IsNull()
        {
            Assert.That(_service.GetOperator(AdminId, 999), Is.Null);
            Assert.That(_service.GetOperator(AdminId, AdminId), Is.Null);
            Assert.Throws<UnauthorizedAccessException>((Action)(() => _service.GetOperator(OperatorId, OperatorId)));
        }

        // Estado por guía, sesiones de la más reciente a la más vieja, duración ("—" en curso) e
        // intentos fallidos de cada una, sin contar los saltos.
        [Test]
        public void GetOperator_SessionsAndReadiness()
        {
            var start = new DateTime(2026, 10, 1, 14, 0, 0);
            _dashboard.Sessions.Add(new DashboardSession
            {
                SessionId = 1, UserId = OperatorId, TrainingGuideId = LoFlo, Mode = SessionMode.Assessment,
                Status = SessionStatus.Completed, FinalScore = 0.80m, StartedAt = start, CompletedAt = start.AddMinutes(20)
            });
            _dashboard.Sessions.Add(new DashboardSession
            {
                SessionId = 2, UserId = OperatorId, TrainingGuideId = LoFlo, Mode = SessionMode.FreePractice,
                Status = SessionStatus.InProgress, StartedAt = start.AddDays(1)
            });
            _dashboard.Attempts.Add(Attempt(1, 1, false, AttemptReason.WrongDecision, start.AddMinutes(1)));
            _dashboard.Attempts.Add(Attempt(1, 1, true, AttemptReason.None, start.AddMinutes(2)));
            _dashboard.Attempts.Add(Attempt(2, 1, false, AttemptReason.Skipped, start.AddDays(1).AddMinutes(1)));

            var detail = _service.GetOperator(AdminId, OperatorId);

            Assert.That(detail.Readiness.Select(r => r.State), Is.EqualTo(new[] { ReadinessState.Passed, ReadinessState.NotStarted }));
            Assert.That(detail.Sessions.Select(s => s.Session.SessionId), Is.EqualTo(new[] { 2, 1 }));
            Assert.That(detail.Sessions[0].Duration, Is.Null);
            Assert.That(detail.Sessions[0].FailedAttempts, Is.EqualTo(0));
            Assert.That(detail.Sessions[1].Duration, Is.EqualTo(TimeSpan.FromMinutes(20)));
            Assert.That(detail.Sessions[1].FailedAttempts, Is.EqualTo(1));
        }

        // Pasos a reforzar solo de los Assessment, con el nombre del paso.
        [Test]
        public void GetOperator_ReinforcementOnlyFromAssessment()
        {
            var start = new DateTime(2026, 10, 1, 14, 0, 0);
            _dashboard.Steps.Add(new TrainingStep { Id = 501, TrainingGuideId = LoFlo, StepType = StepType.ResourceSetup, Order = 1 });
            _dashboard.Steps.Add(new TrainingStep { Id = 502, TrainingGuideId = LoFlo, StepType = StepType.DataCollection, Order = 2 });
            foreach (var mode in new[] { SessionMode.Assessment, SessionMode.FreePractice })
            {
                int id = mode == SessionMode.Assessment ? 1 : 2;
                _dashboard.Sessions.Add(new DashboardSession
                {
                    SessionId = id, UserId = OperatorId, TrainingGuideId = LoFlo, Mode = mode,
                    Status = SessionStatus.Completed, StartedAt = start, CompletedAt = start.AddMinutes(10)
                });
                int stepId = mode == SessionMode.Assessment ? 502 : 501;
                int order  = mode == SessionMode.Assessment ? 2 : 1;
                for (int i = 0; i < 3; i++)
                    _dashboard.Attempts.Add(new DashboardAttempt
                    {
                        SessionId = id, StepId = stepId, StepOrder = order, IsCorrect = false,
                        Reason = AttemptReason.EmptyRequiredField, Timestamp = start.AddMinutes(1 + i)
                    });
            }

            var detail = _service.GetOperator(AdminId, OperatorId);

            var step = detail.Reinforcement.Single().Steps.Single();
            Assert.That(step.StepName, Is.EqualTo("Data Collection"));
            Assert.That(step.TimesWithoutSuccess, Is.EqualTo(1));
            Assert.That(step.FailedAttempts, Is.EqualTo(3));
            Assert.That(step.MainReason, Is.EqualTo(AttemptReason.EmptyRequiredField));
        }

        // ---- Pasos (E3, AC-17 y AC-18) ----

        // Una sesión de LoFlo con sus intentos: (orden del paso, correcto, motivo, segundos desde
        // el inicio).
        private void StepSession(int id, SessionMode mode, SessionStatus status,
            params (int order, bool correct, AttemptReason reason, int second)[] attempts)
            => StepSessionIn(LoFlo, id, mode, status, attempts);

        // Los pasos de LoFlo son 501, 502…; los de Moldeo, 601, 602…
        private void StepSessionIn(int guideId, int id, SessionMode mode, SessionStatus status,
            params (int order, bool correct, AttemptReason reason, int second)[] attempts)
        {
            var start = new DateTime(2026, 10, 1, 14, 0, 0);
            int firstStepId = guideId == LoFlo ? 500 : 600;
            _dashboard.Sessions.Add(new DashboardSession
            {
                SessionId = id, UserId = OperatorId, TrainingGuideId = guideId, Mode = mode, Status = status,
                StartedAt = start
            });
            foreach (var a in attempts)
                _dashboard.Attempts.Add(new DashboardAttempt
                {
                    SessionId = id, StepId = firstStepId + a.order, StepOrder = a.order, IsCorrect = a.correct,
                    Reason = a.reason, Action = a.correct ? null : "LC.Save", Timestamp = start.AddSeconds(a.second)
                });
        }

        private void LoFloSteps()
        {
            _dashboard.Steps.Add(new TrainingStep { Id = 501, TrainingGuideId = LoFlo, StepType = StepType.ResourceSetup, Order = 1 });
            _dashboard.Steps.Add(new TrainingStep { Id = 502, TrainingGuideId = LoFlo, StepType = StepType.LineClearance, Order = 2 });
        }

        // AC-18: en Práctica libre el salto se cuenta aparte y no suma al % ni a los intentos
        // fallidos; el salto con fallos sí terminó sin acierto.
        [Test]
        public void GetSteps_SkipsAreNotFailures()
        {
            LoFloSteps();
            StepSession(1, SessionMode.FreePractice, SessionStatus.Completed,
                (1, false, AttemptReason.WrongDecision, 30), (1, true, AttemptReason.None, 60));
            StepSession(2, SessionMode.FreePractice, SessionStatus.Completed,
                (1, false, AttemptReason.Skipped, 30));
            StepSession(3, SessionMode.FreePractice, SessionStatus.Completed,
                (1, false, AttemptReason.EmptyRequiredField, 30), (1, false, AttemptReason.Skipped, 60));
            StepSession(4, SessionMode.FreePractice, SessionStatus.Completed,
                (1, true, AttemptReason.None, 30));

            var report = _service.GetSteps(AdminId, new StepFilter { Mode = SessionMode.FreePractice });

            Assert.That(report.ShowsSkips, Is.True);
            var step = report.Rankings.Single().Steps.Single();
            Assert.That(step.StepName, Is.EqualTo("Resource Setup"));
            Assert.That(step.Sessions, Is.EqualTo(4));
            Assert.That(step.SessionsWithFailure, Is.EqualTo(2));
            Assert.That(step.FailureRate, Is.EqualTo(0.5m));
            Assert.That(step.FailedAttempts, Is.EqualTo(2));
            Assert.That(step.Skips, Is.EqualTo(2));
            Assert.That(step.TimesWithoutSuccess, Is.EqualTo(1));
            Assert.That(step.Reasons.Select(r => r.Reason), Is.EquivalentTo(new AttemptReason?[] { AttemptReason.WrongDecision, AttemptReason.EmptyRequiredField }));
        }

        // AC-17: en Assessment, sin columna de saltos; el avance forzado (4.º fallo) terminó sin
        // acierto; y el orden es por % de sesiones con fallo, después por intentos fallidos.
        [Test]
        public void GetSteps_AssessmentOrderAndForcedAdvance()
        {
            LoFloSteps();
            // Paso 1: 1 de 2 sesiones con fallo (50 %), 4 intentos fallidos y avance forzado.
            // Paso 2: 2 de 2 sesiones con fallo (100 %), 2 intentos fallidos.
            StepSession(1, SessionMode.Assessment, SessionStatus.Completed,
                (1, false, AttemptReason.WrongDecision, 10), (1, false, AttemptReason.WrongDecision, 20),
                (1, false, AttemptReason.WrongDecision, 30), (1, false, AttemptReason.WrongDecision, 40),
                (2, false, AttemptReason.MissingSelection, 50), (2, true, AttemptReason.None, 60));
            StepSession(2, SessionMode.Assessment, SessionStatus.Completed,
                (1, true, AttemptReason.None, 10),
                (2, false, AttemptReason.MissingSelection, 20), (2, true, AttemptReason.None, 30));

            var report = _service.GetSteps(AdminId, new StepFilter());

            Assert.That(report.ShowsSkips, Is.False);
            var steps = report.Rankings.Single().Steps;
            Assert.That(steps.Select(s => s.StepOrder), Is.EqualTo(new[] { 2, 1 }));
            Assert.That(steps[0].FailureRate, Is.EqualTo(1m));
            Assert.That(steps[1].FailureRate, Is.EqualTo(0.5m));
            Assert.That(steps[1].FailedAttempts, Is.EqualTo(4));
            Assert.That(steps[1].TimesWithoutSuccess, Is.EqualTo(1));
            Assert.That(steps[1].Reasons.Single().Count, Is.EqualTo(4));
        }

        // Con el mismo % gana el de más intentos fallidos.
        [Test]
        public void GetSteps_TieBreaksByFailedAttempts()
        {
            LoFloSteps();
            StepSession(1, SessionMode.Assessment, SessionStatus.Completed,
                (1, false, AttemptReason.WrongDecision, 10), (1, true, AttemptReason.None, 20),
                (2, false, AttemptReason.WrongDecision, 30), (2, false, AttemptReason.WrongDecision, 40),
                (2, true, AttemptReason.None, 50));

            var steps = _service.GetSteps(AdminId, new StepFilter()).Rankings.Single().Steps;

            Assert.That(steps.Select(s => s.StepOrder), Is.EqualTo(new[] { 2, 1 }));
        }

        // Con todas las guías (usuario, 2026-10-23): el mismo paso de cada guía en un solo
        // renglón, con los conteos de todas sus sesiones; con una guía, solo sus pasos.
        [Test]
        public void GetSteps_AllGuidesSumsTheSameStep()
        {
            LoFloSteps();
            _dashboard.Steps.Add(new TrainingStep { Id = 601, TrainingGuideId = Moldeo, StepType = StepType.ResourceSetup, Order = 1 });
            _dashboard.Steps.Add(new TrainingStep { Id = 602, TrainingGuideId = Moldeo, StepType = StepType.SFC_Split, Order = 2 });
            StepSessionIn(LoFlo, 1, SessionMode.Assessment, SessionStatus.Completed,
                (1, false, AttemptReason.WrongDecision, 10), (1, true, AttemptReason.None, 20));
            StepSessionIn(Moldeo, 2, SessionMode.Assessment, SessionStatus.Completed,
                (1, true, AttemptReason.None, 10),
                (2, false, AttemptReason.MissingSelection, 20), (2, true, AttemptReason.None, 30));

            var all = _service.GetSteps(AdminId, new StepFilter()).Rankings.Single();

            Assert.That(all.TrainingGuideId, Is.Null);
            Assert.That(all.Steps.Select(s => s.StepName), Is.EqualTo(new[] { "SFC Split", "Resource Setup" }));
            var setup = all.Steps.Single(s => s.StepName == "Resource Setup");
            Assert.That(setup.Sessions, Is.EqualTo(2));
            Assert.That(setup.FailureRate, Is.EqualTo(0.5m));

            var moldeo = _service.GetSteps(AdminId, new StepFilter { GuideId = Moldeo }).Rankings.Single();
            Assert.That(moldeo.TrainingGuideId, Is.EqualTo(Moldeo));
            Assert.That(moldeo.Steps.Single(s => s.StepName == "Resource Setup").Sessions, Is.EqualTo(1));
        }

        // La mediana de la duración no se mueve con un valor atípico (DA-2).
        [Test]
        public void GetSteps_MedianIgnoresOutlier()
        {
            LoFloSteps();
            int[] seconds = { 60, 70, 80, 5000 };
            for (int i = 0; i < seconds.Length; i++)
                StepSession(i + 1, SessionMode.Assessment, SessionStatus.Completed, (1, true, AttemptReason.None, seconds[i]));

            var step = _service.GetSteps(AdminId, new StepFilter()).Rankings.Single().Steps.Single();

            Assert.That(step.MedianDuration, Is.EqualTo(TimeSpan.FromSeconds(75)));
        }

        // La mediana solo usa los pasos resueltos sin saltar: ni un salto rápido ni el paso a
        // medias de una sesión abandonada la bajan; sin ninguno, null ("—").
        [Test]
        public void GetSteps_MedianOnlyFromResolvedSteps()
        {
            LoFloSteps();
            StepSession(1, SessionMode.FreePractice, SessionStatus.Completed, (1, true, AttemptReason.None, 120));
            StepSession(2, SessionMode.FreePractice, SessionStatus.Completed, (1, false, AttemptReason.Skipped, 3));
            StepSession(3, SessionMode.FreePractice, SessionStatus.Completed, (1, false, AttemptReason.Skipped, 4));
            StepSession(4, SessionMode.FreePractice, SessionStatus.Abandoned,
                (1, true, AttemptReason.None, 100), (2, false, AttemptReason.WrongDecision, 105));

            var steps = _service.GetSteps(AdminId, new StepFilter { Mode = SessionMode.FreePractice })
                .Rankings.Single().Steps;

            Assert.That(steps.Single(s => s.StepOrder == 1).MedianDuration, Is.EqualTo(TimeSpan.FromSeconds(110)));
            Assert.That(steps.Single(s => s.StepOrder == 2).MedianDuration, Is.Null);
        }

        // Filtros manipulados: Guided vale Assessment, una guía no publicada vale "todas", y el
        // período llega al repositorio (30 días o todo). Sin la capacidad, excepción.
        [Test]
        public void GetSteps_ValidatesFilters()
        {
            var report = _service.GetSteps(AdminId, new StepFilter
            {
                Mode = SessionMode.Guided, GuideId = 999, Period = (ReportPeriod)7
            });

            Assert.That(report.Applied.Mode, Is.EqualTo(SessionMode.Assessment));
            Assert.That(report.Applied.GuideId, Is.Null);
            Assert.That(report.Applied.Period, Is.EqualTo(ReportPeriod.Last30Days));
            Assert.That(_dashboard.LastStepMode, Is.EqualTo(SessionMode.Assessment));
            Assert.That(_dashboard.LastStepFrom, Is.EqualTo(DateTime.UtcNow.AddDays(-30)).Within(TimeSpan.FromMinutes(1)));
            Assert.That(report.Rankings, Is.Empty);

            _service.GetSteps(AdminId, new StepFilter { Period = ReportPeriod.All, GuideId = Moldeo });
            Assert.That(_dashboard.LastStepFrom, Is.Null);
            Assert.That(_dashboard.LastStepGuide, Is.EqualTo(Moldeo));

            Assert.Throws<UnauthorizedAccessException>((Action)(() => _service.GetSteps(OperatorId, new StepFilter())));
        }

        // ---- Resumen (E4, AC-9 y AC-12) ----

        // El top 5 es el principio de Pasos con los mismos filtros (D-147); con una guía, todo de
        // esa guía; los filtros se validan y sin la capacidad es una excepción.
        [Test]
        public void GetSummary_TopStepsMatchStepsAndGuideFilter()
        {
            LoFloSteps();
            StepSessionIn(LoFlo, 1, SessionMode.Assessment, SessionStatus.Completed,
                (1, false, AttemptReason.WrongDecision, 10), (1, true, AttemptReason.None, 20),
                (2, true, AttemptReason.None, 30));
            _dashboard.Sessions.Add(new DashboardSession
            {
                SessionId = 2, UserId = OperatorId, TrainingGuideId = Moldeo, Mode = SessionMode.Guided,
                Status = SessionStatus.Completed, StartedAt = DateTime.UtcNow.AddDays(-1)
            });

            var summary = _service.GetSummary(AdminId, new SummaryFilter { GuideId = 999, Period = (ReportPeriod)9 });
            var steps   = _service.GetSteps(AdminId, new StepFilter()).Rankings.Single().Steps;

            Assert.That(summary.Applied.GuideId, Is.Null);
            Assert.That(summary.Applied.Period, Is.EqualTo(ReportPeriod.Last30Days));
            Assert.That(summary.GuideSummaries.Select(g => g.TrainingGuideId), Is.EqualTo(new[] { LoFlo, Moldeo }));
            // Solo los pasos con algún fallo: el paso 2 (0 %) está en Pasos pero no en el top 5.
            Assert.That(summary.TopSteps.Select(s => s.StepName), Is.EqualTo(new[] { "Resource Setup" }));
            Assert.That(steps.Select(s => s.StepName), Is.EqualTo(new[] { "Resource Setup", "Line Clearance (apertura)" }));
            Assert.That(summary.TopSteps.First().FailureRate, Is.EqualTo(steps.First().FailureRate));

            var moldeo = _service.GetSummary(AdminId, new SummaryFilter { GuideId = Moldeo, Period = ReportPeriod.All });
            Assert.That(moldeo.GuideSummaries.Single().TrainingGuideId, Is.EqualTo(Moldeo));
            Assert.That(moldeo.GuideSummaries.Single().InTraining, Is.EqualTo(1));
            Assert.That(moldeo.TopSteps, Is.Empty);

            Assert.Throws<UnauthorizedAccessException>((Action)(() => _service.GetSummary(OperatorId, new SummaryFilter())));
        }

        // AC-11: una BD sin sesiones no rompe nada: sin semanas, sin top 5 y tasas en null.
        [Test]
        public void GetSummary_EmptyDatabase()
        {
            var summary = _service.GetSummary(AdminId, new SummaryFilter());

            Assert.That(summary.Weeks, Is.Empty);
            Assert.That(summary.TopSteps, Is.Empty);
            Assert.That(summary.GuideSummaries.All(g => g.PassRate == null && g.FirstAttemptRate == null), Is.True);
        }

        // ---- Seguimiento (E5, AC-20) ----

        // S-3 con el nombre del paso y el enlace al operador; con el filtro de otra guía no
        // aparece; sin la capacidad, excepción.
        [Test]
        public void GetFollowUp_StuckStepWithName()
        {
            LoFloSteps();
            Operator(OperatorId, "Ana");
            for (int id = 1; id <= 2; id++)
                StepSessionIn(LoFlo, id, SessionMode.Assessment, SessionStatus.Completed,
                    (1, false, AttemptReason.WrongDecision, 10), (1, false, AttemptReason.WrongDecision, 20),
                    (1, false, AttemptReason.WrongDecision, 30), (1, false, AttemptReason.WrongDecision, 40),
                    (2, true, AttemptReason.None, 50));
            foreach (var s in _dashboard.Sessions) s.FinalScore = 0.40m;

            var report = _service.GetFollowUp(AdminId, new FollowUpFilter { GuideId = 999 });

            Assert.That(report.Applied.GuideId, Is.Null);
            var row = report.Rows.Single();
            Assert.That(row.Name, Is.EqualTo("Ana"));
            Assert.That(row.TrainingGuideId, Is.EqualTo(LoFlo));
            Assert.That(row.State, Is.EqualTo(ReadinessState.Failed));
            Assert.That(row.Reasons.Select(r => r.Rule), Does.Contain(FollowUpRule.RepeatedFailures));
            var stuck = row.Reasons.Single(r => r.Rule == FollowUpRule.StuckStep).Steps.Single();
            Assert.That(stuck.StepName, Is.EqualTo("Resource Setup"));
            Assert.That(stuck.Times, Is.EqualTo(2));

            Assert.That(_service.GetFollowUp(AdminId, new FollowUpFilter { GuideId = Moldeo }).Rows, Is.Empty);
            Assert.Throws<UnauthorizedAccessException>((Action)(() => _service.GetFollowUp(OperatorId, new FollowUpFilter())));
        }

        // ---- Exportar (E6, DA-5) ----

        // La descarga de Operadores trae todas las filas con los mismos filtros, sin paginar.
        [Test]
        public void ExportOperators_AllRowsSameFilters()
        {
            for (int i = 0; i < 120; i++) Operator(100 + i, "Op " + i.ToString("000"));
            Operator(999, "Zeta");
            Session(999, LoFlo, SessionMode.Guided, 0m, 1);

            var all = _service.ExportOperators(AdminId, new OperatorFilter { Page = 3 });
            var notStarted = _service.ExportOperators(AdminId, new OperatorFilter { State = ReadinessState.NotStarted });

            Assert.That(all.Rows.Count, Is.EqualTo(121));
            Assert.That(all.Total, Is.EqualTo(121));
            Assert.That(notStarted.Rows.Count, Is.EqualTo(120));
            Assert.That(_service.SearchOperators(AdminId, new OperatorFilter()).Rows.Count, Is.EqualTo(DashboardService.PageSize));
        }

        // Las sesiones del Resumen con sus pasos: el resultado de StepRuns, el nombre del paso y
        // las sesiones sin intentos también; las de otro rol no (sin operador).
        [Test]
        public void GetSessionExport_SessionsWithSteps()
        {
            LoFloSteps();
            Operator(OperatorId, "Ana");
            StepSessionIn(LoFlo, 1, SessionMode.FreePractice, SessionStatus.Completed,
                (1, false, AttemptReason.Skipped, 10), (2, true, AttemptReason.None, 30));
            _dashboard.Sessions.Add(new DashboardSession
            {
                SessionId = 2, UserId = OperatorId, TrainingGuideId = LoFlo, Mode = SessionMode.Guided,
                Status = SessionStatus.InProgress, StartedAt = DateTime.UtcNow.AddDays(-1)
            });
            foreach (var s in _dashboard.Sessions) s.StartedAt = DateTime.UtcNow.AddDays(-2);

            var export = _service.GetSessionExport(AdminId, new SummaryFilter());

            Assert.That(export.Rows.Select(r => r.Session.SessionId), Is.EquivalentTo(new[] { 1, 2 }));
            var practice = export.Rows.Single(r => r.Session.SessionId == 1);
            Assert.That(practice.Name, Is.EqualTo("Ana"));
            Assert.That(practice.Steps.Select(s => s.StepName), Is.EqualTo(new[] { "Resource Setup", "Line Clearance (apertura)" }));
            Assert.That(practice.Steps.Select(s => s.Run.Outcome), Is.EqualTo(new[] { StepOutcome.Skipped, StepOutcome.Correct }));
            Assert.That(export.Rows.Single(r => r.Session.SessionId == 2).Steps, Is.Empty);
            Assert.Throws<UnauthorizedAccessException>((Action)(() => _service.GetSessionExport(OperatorId, new SummaryFilter())));
        }

        private static DashboardAttempt Attempt(int sessionId, int order, bool correct, AttemptReason reason, DateTime at)
            => new DashboardAttempt
            {
                SessionId = sessionId, StepId = 500 + order, StepOrder = order, IsCorrect = correct,
                Reason = reason, Timestamp = at
            };

        private sealed class FakeDashboardRepository : IDashboardRepository
        {
            public List<DashboardGuide> Guides { get; } = new List<DashboardGuide>();
            public List<DashboardOperator> Operators { get; } = new List<DashboardOperator>();
            public List<DashboardSession> Sessions { get; } = new List<DashboardSession>();
            public List<TrainingStep> Steps { get; } = new List<TrainingStep>();
            public List<DashboardAttempt> Attempts { get; } = new List<DashboardAttempt>();
            public string LastText { get; private set; }

            public IList<TrainingStep> GetGuideSteps() => Steps.ToList();

            public IList<DashboardAttempt> GetOperatorAttempts(int userId)
            {
                var mine = new HashSet<int>(Sessions.Where(s => s.UserId == userId).Select(s => s.SessionId));
                return Attempts.Where(a => mine.Contains(a.SessionId)).ToList();
            }

            public IList<DashboardGuide> GetGuides() => Guides.ToList();

            // Como el SP: contiene el texto en nombre o Code1, por nombre.
            public IList<DashboardOperator> GetOperators(string text)
            {
                LastText = text;
                return Operators
                    .Where(o => string.IsNullOrEmpty(text) ||
                                o.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                o.Code1.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(o => o.Name, StringComparer.Ordinal)
                    .ToList();
            }

            public IList<DashboardSession> GetSessions(int? userId, bool includeFailedAttempts)
                => Sessions.Where(s => !userId.HasValue || s.UserId == userId.Value).ToList();

            public SessionMode? LastStepMode { get; private set; }
            public DateTime? LastStepFrom { get; private set; }
            public int? LastStepGuide { get; private set; }

            // Como el SP: las sesiones del modo, desde fromUtc y de la guía, con sus intentos.
            public DashboardAttemptSet GetStepAttempts(SessionMode mode, DateTime? fromUtc, int? guideId)
            {
                LastStepMode = mode; LastStepFrom = fromUtc; LastStepGuide = guideId;
                var sessions = Sessions
                    .Where(s => s.Mode == mode && (!guideId.HasValue || s.TrainingGuideId == guideId.Value))
                    .ToList();
                var ids = new HashSet<int>(sessions.Select(s => s.SessionId));
                return new DashboardAttemptSet
                {
                    Sessions = sessions,
                    Attempts = Attempts.Where(a => ids.Contains(a.SessionId)).ToList()
                };
            }
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
        }
    }
}
