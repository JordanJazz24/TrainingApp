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
