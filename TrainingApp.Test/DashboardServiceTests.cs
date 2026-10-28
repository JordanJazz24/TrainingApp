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
    // estado y área (spec 024), 50 por página y la capacidad CanViewReports.
    [TestFixture]
    public class DashboardServiceTests
    {
        private const int AdminId    = 1;
        private const int OperatorId = 2;
        private const int LoFlo      = 10;
        private const int Moldeo     = 20;

        // Áreas de las dos guías (spec 024): los operadores de los tests están en LoFlo salvo que
        // se diga otra cosa.
        private const int LoFloArea  = 1;
        private const int MoldeoArea = 2;

        // Miércoles 28 de octubre de 2026, 12:00 en la planta: Este mes es del 1 al 28 de oct. y
        // su anterior, del 1 al 28 de sep. (spec 023).
        private static readonly DateTime Now = new DateTime(2026, 10, 28, 18, 0, 0);

        private FakeDashboardRepository _dashboard;
        private DashboardService _service;

        [SetUp]
        public void SetUp()
        {
            _dashboard = new FakeDashboardRepository();
            _dashboard.Guides.Add(new DashboardGuide { Id = LoFlo,  Title = "LoFlo",  AreaId = LoFloArea,  ProductName = "Sensores", AreaName = "LoFlo" });
            _dashboard.Guides.Add(new DashboardGuide { Id = Moldeo, Title = "Moldeo", AreaId = MoldeoArea, ProductName = "S&RC",     AreaName = "Moldeo" });

            var users = new FakeUsers();
            users.Add(new User { Id = AdminId,    Code1 = "A1", Role = UserRole.Administrator });
            users.Add(new User { Id = OperatorId, Code1 = "O1", Role = UserRole.Operator });
            _service = new DashboardService(_dashboard, users, () => Now);
        }

        private void Operator(int id, string name, string code1 = null, int? areaId = LoFloArea)
            => _dashboard.Operators.Add(new DashboardOperator { UserId = id, Name = name, Code1 = code1 ?? "C" + id, AreaId = areaId });

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

        // Spec 024: el estado de su área (aunque entrene otra), con mejor, último y la última
        // actividad de cualquier guía.
        [Test]
        public void Row_HasStateInHisArea()
        {
            Operator(100, "Ana", areaId: MoldeoArea);
            Session(100, Moldeo, SessionMode.Assessment, 0.80m, 0);
            Session(100, Moldeo, SessionMode.Assessment, 0.60m, 2);
            Session(100, LoFlo, SessionMode.FreePractice, 0.90m, 5);

            var row = _service.SearchOperators(AdminId, new OperatorFilter()).Rows.Single();

            Assert.That(row.AreaStatus, Is.EqualTo(OperatorAreaStatus.HasGuides));
            Assert.That(row.AreaGuides.Select(g => g.TrainingGuideId), Is.EqualTo(new[] { Moldeo }));
            Assert.That(row.AreaGuides[0].State, Is.EqualTo(ReadinessState.Passed));
            Assert.That(row.AreaGuides[0].BestScore, Is.EqualTo(0.80m));
            Assert.That(row.AreaGuides[0].LastScore, Is.EqualTo(0.60m));
            Assert.That(row.LastActivity, Is.EqualTo(new DateTime(2026, 10, 6)));
        }

        // Spec 024 (D-263, AC-3, AC-4): Estado se evalúa en la guía de su área. Sin iniciar incluye
        // a quien solo entrenó otra área; aprobar otra área no lo hace Aprobado; sin área nunca es
        // Sin iniciar.
        [Test]
        public void Filters_ByStateInHisArea()
        {
            Operator(100, "Ana");                           // LoFlo aprobado
            Operator(101, "Beto");                          // LoFlo reprobado, Moldeo aprobado (cruzado)
            Operator(102, "Carla", areaId: MoldeoArea);     // Moldeo en entrenamiento
            Operator(103, "Dani");                          // sin iniciar
            Operator(104, "Eva",   areaId: MoldeoArea);     // solo LoFlo (cruzado): sin iniciar en Moldeo
            Operator(105, "Fede",  areaId: null);           // sin área
            Session(100, LoFlo,  SessionMode.Assessment, 0.90m, 0);
            Session(101, LoFlo,  SessionMode.Assessment, 0.40m, 0);
            Session(101, Moldeo, SessionMode.Assessment, 0.75m, 1);
            Session(102, Moldeo, SessionMode.Guided,     1.00m, 0);
            Session(104, LoFlo,  SessionMode.Assessment, 0.95m, 0);
            Session(105, LoFlo,  SessionMode.Guided,     1.00m, 0);

            Assert.That(Names(new OperatorFilter { State = ReadinessState.Passed }), Is.EqualTo(new[] { "Ana" }));
            Assert.That(Names(new OperatorFilter { State = ReadinessState.Failed }), Is.EqualTo(new[] { "Beto" }));
            Assert.That(Names(new OperatorFilter { State = ReadinessState.InTraining }), Is.EqualTo(new[] { "Carla" }));
            Assert.That(Names(new OperatorFilter { State = ReadinessState.NotStarted }), Is.EqualTo(new[] { "Dani", "Eva" }));
            Assert.That(Names(new OperatorFilter()), Has.Member("Fede"));
        }

        // Spec 024 (D-262, D-267, AC-2): el filtro Área, Sin área y combinado con Estado.
        [Test]
        public void Filters_ByArea()
        {
            Operator(100, "Ana");
            Operator(101, "Beto",  areaId: MoldeoArea);
            Operator(102, "Carla", areaId: null);
            Session(101, Moldeo, SessionMode.Guided, 1m, 0);

            Assert.That(Names(new OperatorFilter { AreaId = LoFloArea }), Is.EqualTo(new[] { "Ana" }));
            Assert.That(Names(new OperatorFilter { AreaId = MoldeoArea }), Is.EqualTo(new[] { "Beto" }));
            Assert.That(Names(new OperatorFilter { WithoutArea = true }), Is.EqualTo(new[] { "Carla" }));
            Assert.That(Names(new OperatorFilter { WithoutArea = true, AreaId = LoFloArea }), Is.EqualTo(new[] { "Carla" }));
            Assert.That(Names(new OperatorFilter { AreaId = MoldeoArea, State = ReadinessState.NotStarted }), Is.Empty);

            var page = _service.SearchOperators(AdminId, new OperatorFilter());
            Assert.That(page.Areas.Select(a => a.AreaId), Is.EqualTo(new[] { LoFloArea, MoldeoArea }));
            Assert.That(page.Rows.Single(r => r.Name == "Carla").AreaStatus, Is.EqualTo(OperatorAreaStatus.NoArea));
        }

        // Spec 024 (P-5): un operador cuya área no tiene guía publicada no es Sin iniciar y solo
        // sale con todos los estados.
        [Test]
        public void AreaWithoutPublishedGuide_IsNotNotStarted()
        {
            Operator(100, "Ana", areaId: 77);

            var row = _service.SearchOperators(AdminId, new OperatorFilter()).Rows.Single();

            Assert.That(row.AreaStatus, Is.EqualTo(OperatorAreaStatus.NoPublishedGuide));
            Assert.That(row.NotStarted, Is.False);
            Assert.That(row.AreaGuides, Is.Empty);
            Assert.That(Names(new OperatorFilter { State = ReadinessState.NotStarted }), Is.Empty);
        }

        // Un área sin guía publicada o un estado inválido (URL manipulada) valen "todas" y "todos".
        [Test]
        public void InvalidFilters_FallBackToAll()
        {
            Operator(100, "Ana");
            Operator(101, "Beto", areaId: MoldeoArea);
            Session(100, LoFlo, SessionMode.Guided, 1m, 0);

            Assert.That(Names(new OperatorFilter { AreaId = 999 }), Is.EqualTo(new[] { "Ana", "Beto" }));
            Assert.That(Names(new OperatorFilter { State = (ReadinessState)42 }), Is.EqualTo(new[] { "Ana", "Beto" }));

            // La página muestra los filtros aplicados, no los que llegaron.
            var applied = _service.SearchOperators(AdminId,
                new OperatorFilter { Text = "  an ", AreaId = 999, State = (ReadinessState)42, Page = 7 }).Applied;
            Assert.That(applied.Text, Is.EqualTo("an"));
            Assert.That(applied.AreaId, Is.Null);
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

            // Sin área: lo que entrenó va en las otras áreas (P-8).
            Assert.That(detail.AreaStatus, Is.EqualTo(OperatorAreaStatus.NoArea));
            Assert.That(detail.AreaGuides, Is.Empty);
            Assert.That(detail.OtherGuides.Select(r => (r.TrainingGuideId, r.State)), Is.EqualTo(new[] { (LoFlo, ReadinessState.Passed) }));
            Assert.That(detail.Sessions[0].AreaText, Is.EqualTo("Sensores · LoFlo"));
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

        // AC-15: la curva del detalle, solo de las guías que entrenó, con los intentos fallidos de
        // StepRuns (sin saltos) y el puntaje visible.
        [Test]
        public void GetOperator_LearningCurve()
        {
            var start = new DateTime(2026, 10, 1, 14, 0, 0);
            _dashboard.Sessions.Add(new DashboardSession
            {
                SessionId = 1, UserId = OperatorId, TrainingGuideId = LoFlo, Mode = SessionMode.Guided,
                Status = SessionStatus.Completed, StartedAt = start, CompletedAt = start.AddMinutes(10)
            });
            _dashboard.Sessions.Add(new DashboardSession
            {
                SessionId = 2, UserId = OperatorId, TrainingGuideId = LoFlo, Mode = SessionMode.FreePractice,
                Status = SessionStatus.Completed, StartedAt = start.AddDays(1), CompletedAt = start.AddDays(1).AddMinutes(10)
            });
            _dashboard.Sessions.Add(new DashboardSession
            {
                SessionId = 3, UserId = OperatorId, TrainingGuideId = LoFlo, Mode = SessionMode.Assessment,
                Status = SessionStatus.Completed, FinalScore = 0.9m, StartedAt = start.AddDays(2), CompletedAt = start.AddDays(2).AddMinutes(10)
            });
            _dashboard.Attempts.Add(Attempt(2, 1, false, AttemptReason.WrongDecision, start.AddDays(1).AddMinutes(1)));
            _dashboard.Attempts.Add(Attempt(2, 2, false, AttemptReason.Skipped, start.AddDays(1).AddMinutes(2)));

            var curve = _service.GetOperator(AdminId, OperatorId).Curves.Single();

            Assert.That(curve.TrainingGuideId, Is.EqualTo(LoFlo));
            Assert.That(curve.Points.Select(p => p.FailedAttempts), Is.EqualTo(new[] { 1, 0 }));
            Assert.That(curve.Points.Select(p => p.Score), Is.EqualTo(new decimal?[] { null, 0.9m }));
            Assert.That(curve.PracticeBeforeFirstPass, Is.EqualTo(2));
        }

        // ---- Pasos (E3, AC-17 y AC-18) ----

        // Una sesión de LoFlo con sus intentos: (orden del paso, correcto, motivo, segundos desde
        // el inicio).
        private void StepSession(int id, SessionMode mode, SessionStatus status,
            params (int order, bool correct, AttemptReason reason, int second)[] attempts)
            => StepSessionIn(LoFlo, id, mode, status, attempts);

        // Los pasos de LoFlo son 501, 502…; los de Moldeo, 601, 602… Empieza el 1 de oct. (Este
        // mes).
        private void StepSessionIn(int guideId, int id, SessionMode mode, SessionStatus status,
            params (int order, bool correct, AttemptReason reason, int second)[] attempts)
            => StepSessionAt(guideId, id, new DateTime(2026, 10, 1, 14, 0, 0), mode, status, attempts);

        private void StepSessionAt(int guideId, int id, DateTime start, SessionMode mode, SessionStatus status,
            params (int order, bool correct, AttemptReason reason, int second)[] attempts)
        {
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

        // Filtros manipulados: Guided vale Assessment, una guía no publicada vale "todas" y un
        // período fuera de las opciones, Este mes con el aviso; una sola lectura llega al
        // repositorio, del inicio del anterior (1 de sep.) al fin del período (29 de oct., 00:00
        // de la planta, excluido). Sin la capacidad, excepción.
        [Test]
        public void GetSteps_ValidatesFilters()
        {
            var report = _service.GetSteps(AdminId, new StepFilter
            {
                Mode = SessionMode.Guided, GuideId = 999, Period = (ReportPeriod)7
            });

            Assert.That(report.Applied.Mode, Is.EqualTo(SessionMode.Assessment));
            Assert.That(report.Applied.GuideId, Is.Null);
            Assert.That(report.Applied.Period, Is.EqualTo(ReportPeriod.ThisMonth));
            Assert.That(report.Range.Invalid, Is.True);
            Assert.That(_dashboard.LastStepMode, Is.EqualTo(SessionMode.Assessment));
            Assert.That(_dashboard.LastStepFrom, Is.EqualTo(new DateTime(2026, 9, 1, 6, 0, 0)));
            Assert.That(_dashboard.LastStepTo, Is.EqualTo(new DateTime(2026, 10, 29, 6, 0, 0)));
            Assert.That(report.Rankings, Is.Empty);

            Assert.That(_service.GetSteps(AdminId, new StepFilter()).Range.Invalid, Is.False);

            _service.GetSteps(AdminId, new StepFilter { Period = ReportPeriod.All, GuideId = Moldeo });
            Assert.That(_dashboard.LastStepFrom, Is.Null);
            Assert.That(_dashboard.LastStepTo, Is.Null);
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
                Status = SessionStatus.Completed, StartedAt = Now.AddDays(-1)
            });

            var summary = _service.GetSummary(AdminId, new SummaryFilter { GuideId = 999, Period = (ReportPeriod)9 });
            var steps   = _service.GetSteps(AdminId, new StepFilter()).Rankings.Single().Steps;

            Assert.That(summary.Applied.GuideId, Is.Null);
            Assert.That(summary.Applied.Period, Is.EqualTo(ReportPeriod.ThisMonth));
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

            Assert.That(summary.Activity.Buckets, Is.Empty);
            Assert.That(summary.TopSteps, Is.Empty);
            Assert.That(summary.GuideSummaries.All(g => g.PassRate == null && g.FirstAttemptRate == null), Is.True);
        }

        // ---- Período y comparación (spec 023) ----

        private void AssessmentAt(int id, int userId, decimal score, DateTime startUtc, SessionMode mode = SessionMode.Assessment)
            => _dashboard.Sessions.Add(new DashboardSession
            {
                SessionId = id, UserId = userId, TrainingGuideId = LoFlo, Mode = mode, Status = SessionStatus.Completed,
                FinalScore = score, StartedAt = startUtc, CompletedAt = startUtc.AddMinutes(20)
            });

        // AC-4: la tasa de aprobación cambia en puntos con los evaluados de los dos períodos, y
        // las sesiones del período (todos los modos) en %. Lo de antes del anterior no cuenta.
        [Test]
        public void GetSummary_ComparesWithPreviousPeriod()
        {
            AssessmentAt(1, 100, 0.5m, new DateTime(2026, 9, 10, 15, 0, 0));    // anterior, reprobó
            AssessmentAt(2, 100, 1m,   new DateTime(2026, 10, 10, 15, 0, 0));   // período, aprobó
            AssessmentAt(3, 101, 1m,   new DateTime(2026, 9, 12, 15, 0, 0));    // anterior, aprobó
            AssessmentAt(4, 101, 0m,   new DateTime(2026, 10, 11, 15, 0, 0), SessionMode.Guided);
            AssessmentAt(5, 101, 0m,   new DateTime(2026, 10, 12, 15, 0, 0), SessionMode.Guided);
            AssessmentAt(6, 101, 0m,   new DateTime(2026, 8, 20, 15, 0, 0), SessionMode.Guided);  // fuera de los dos

            var summary = _service.GetSummary(AdminId, new SummaryFilter());
            var loflo   = summary.GuideSummaries.Single(g => g.TrainingGuideId == LoFlo);

            Assert.That(loflo.PassRate, Is.EqualTo(1m));
            Assert.That(loflo.PassRateChange.Change, Is.EqualTo(50m));
            Assert.That(loflo.PassRateChange.IsImprovement, Is.True);
            Assert.That((loflo.PassRateChange.CurrentBase, loflo.PassRateChange.PreviousBase), Is.EqualTo((1, 2)));
            Assert.That(summary.Sessions, Is.EqualTo(3));
            Assert.That(summary.SessionsChange.Change, Is.EqualTo(50m));
            Assert.That((summary.SessionsChange.CurrentBase, summary.SessionsChange.PreviousBase), Is.EqualTo((3, 2)));
            // Moldeo no tiene evaluados en ninguno: sin datos para comparar.
            Assert.That(summary.GuideSummaries.Single(g => g.TrainingGuideId == Moldeo).PassRateChange.HasComparison, Is.False);
        }

        // AC-4: el % con fallo de cada paso, en puntos, con las sesiones de los dos períodos; igual
        // con una guía y con todas (por nombre) y en el top 5 del Resumen. Subir es empeorar.
        [Test]
        public void GetSteps_FailureRateChange()
        {
            LoFloSteps();
            StepSessionAt(LoFlo, 1, new DateTime(2026, 10, 2, 15, 0, 0), SessionMode.Assessment, SessionStatus.Completed,
                (1, false, AttemptReason.WrongDecision, 10), (1, true, AttemptReason.None, 20), (2, true, AttemptReason.None, 30));
            StepSessionAt(LoFlo, 2, new DateTime(2026, 9, 5, 15, 0, 0), SessionMode.Assessment, SessionStatus.Completed,
                (1, true, AttemptReason.None, 10), (2, true, AttemptReason.None, 20));
            StepSessionAt(LoFlo, 3, new DateTime(2026, 9, 6, 15, 0, 0), SessionMode.Assessment, SessionStatus.Completed,
                (1, false, AttemptReason.WrongDecision, 10), (1, true, AttemptReason.None, 20), (2, true, AttemptReason.None, 30));
            StepSessionAt(LoFlo, 4, new DateTime(2026, 8, 1, 15, 0, 0), SessionMode.Assessment, SessionStatus.Completed,
                (1, false, AttemptReason.WrongDecision, 10));   // antes del anterior

            foreach (var guide in new int?[] { LoFlo, null })
            {
                var steps = _service.GetSteps(AdminId, new StepFilter { GuideId = guide }).Rankings.Single().Steps;
                var first = steps.Single(s => s.StepOrder == 1).FailureRateChange;

                Assert.That(first.Change, Is.EqualTo(50m), "guía " + guide);
                Assert.That(first.IsImprovement, Is.False);
                Assert.That((first.CurrentBase, first.PreviousBase), Is.EqualTo((1, 2)));
                Assert.That(steps.Single(s => s.StepOrder == 2).FailureRateChange.Direction, Is.EqualTo(ChangeDirection.Same));
            }

            var top = _service.GetSummary(AdminId, new SummaryFilter()).TopSteps.Single();
            Assert.That(top.FailureRateChange.Change, Is.EqualTo(50m));
        }

        // AC-5: sin nada en el anterior, "sin datos para comparar".
        [Test]
        public void EmptyPreviousPeriod_HasNoComparison()
        {
            LoFloSteps();
            StepSession(1, SessionMode.Assessment, SessionStatus.Completed, (1, false, AttemptReason.WrongDecision, 10));

            var step = _service.GetSteps(AdminId, new StepFilter()).Rankings.Single().Steps.Single();
            var summary = _service.GetSummary(AdminId, new SummaryFilter());

            Assert.That(step.FailureRateChange.HasComparison, Is.False);
            Assert.That(summary.SessionsChange.HasComparison, Is.False);
            Assert.That(summary.Sessions, Is.EqualTo(1));
        }

        // AC-4: con Todo no hay comparación.
        [Test]
        public void All_HasNoComparison()
        {
            LoFloSteps();
            StepSession(1, SessionMode.Assessment, SessionStatus.Completed, (1, false, AttemptReason.WrongDecision, 10));

            var step = _service.GetSteps(AdminId, new StepFilter { Period = ReportPeriod.All }).Rankings.Single().Steps.Single();
            var summary = _service.GetSummary(AdminId, new SummaryFilter { Period = ReportPeriod.All });

            Assert.That(step.FailureRateChange, Is.Null);
            Assert.That(summary.SessionsChange, Is.Null);
            Assert.That(summary.GuideSummaries.All(g => g.PassRateChange == null), Is.True);
        }

        // Personalizado: el filtro aplicado conserva Inicio y Fin y la descarga trae solo ese
        // rango; uno inválido vale Este mes, sin fechas y con el aviso (P-6).
        [Test]
        public void Custom_AppliedAndExport()
        {
            Operator(OperatorId, "Ana");
            AssessmentAt(1, OperatorId, 1m, new DateTime(2026, 10, 3, 15, 0, 0));
            AssessmentAt(2, OperatorId, 1m, new DateTime(2026, 10, 6, 15, 0, 0));
            var custom = new SummaryFilter { Period = ReportPeriod.Custom, From = new DateTime(2026, 10, 5), To = new DateTime(2026, 10, 14) };

            var export = _service.GetSessionExport(AdminId, custom);
            var summary = _service.GetSummary(AdminId, custom);

            Assert.That(export.Rows.Select(r => r.Session.SessionId), Is.EqualTo(new[] { 2 }));
            Assert.That((summary.Applied.From, summary.Applied.To), Is.EqualTo(((DateTime?)new DateTime(2026, 10, 5), (DateTime?)new DateTime(2026, 10, 14))));
            Assert.That(summary.Sessions, Is.EqualTo(1));

            var invalid = _service.GetSummary(AdminId, new SummaryFilter { Period = ReportPeriod.Custom, From = new DateTime(2026, 10, 14), To = new DateTime(2026, 10, 5) });
            Assert.That(invalid.Applied.Period, Is.EqualTo(ReportPeriod.ThisMonth));
            Assert.That(invalid.Applied.From, Is.Null);
            Assert.That(invalid.Range.Invalid, Is.True);
            Assert.That(invalid.Sessions, Is.EqualTo(2));
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

        // Fase 11 (E5): primero quien cumple más reglas; a igual número, por guía y nombre.
        [Test]
        public void GetFollowUp_MoreRulesFirst()
        {
            Operator(100, "Ana");
            Operator(101, "Beto");
            Session(100, LoFlo, SessionMode.Guided, 1m, 0);           // S-2
            Session(101, LoFlo, SessionMode.Assessment, 0.40m, 0);    // S-1 y S-2
            Session(101, LoFlo, SessionMode.Assessment, 0.40m, 1);

            var rows = _service.GetFollowUp(AdminId, new FollowUpFilter()).Rows;

            Assert.That(rows.Select(r => r.Name), Is.EqualTo(new[] { "Beto", "Ana" }));
            Assert.That(rows.Select(r => r.Reasons.Count), Is.EqualTo(new[] { 2, 1 }));
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
                Status = SessionStatus.InProgress, StartedAt = Now.AddDays(-1)
            });
            foreach (var s in _dashboard.Sessions) s.StartedAt = Now.AddDays(-2);

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

            public IList<DashboardSession> GetSessions(int? userId, bool includeFailedAttempts,
                DateTime? fromUtc = null, DateTime? toUtc = null, int? guideId = null)
                => Sessions.Where(s => (!userId.HasValue || s.UserId == userId.Value)
                                       && InRange(s, fromUtc, toUtc)
                                       && (!guideId.HasValue || s.TrainingGuideId == guideId.Value)).ToList();

            public SessionMode? LastStepMode { get; private set; }
            public DateTime? LastStepFrom { get; private set; }
            public DateTime? LastStepTo { get; private set; }
            public int? LastStepGuide { get; private set; }

            // Como el SP: las sesiones del modo (null = todos), desde fromUtc, antes de toUtc y de
            // la guía, con sus intentos.
            public DashboardAttemptSet GetStepAttempts(SessionMode? mode, DateTime? fromUtc, DateTime? toUtc, int? guideId)
            {
                LastStepMode = mode; LastStepFrom = fromUtc; LastStepTo = toUtc; LastStepGuide = guideId;
                var sessions = Sessions
                    .Where(s => (!mode.HasValue || s.Mode == mode.Value) && InRange(s, fromUtc, toUtc)
                                && (!guideId.HasValue || s.TrainingGuideId == guideId.Value))
                    .ToList();
                var ids = new HashSet<int>(sessions.Select(s => s.SessionId));
                return new DashboardAttemptSet
                {
                    Sessions = sessions,
                    Attempts = Attempts.Where(a => ids.Contains(a.SessionId)).ToList()
                };
            }

            private static bool InRange(DashboardSession s, DateTime? fromUtc, DateTime? toUtc)
                => (!fromUtc.HasValue || s.StartedAt >= fromUtc.Value) && (!toUtc.HasValue || s.StartedAt < toUtc.Value);
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
