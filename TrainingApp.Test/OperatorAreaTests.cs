using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Training;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 021: el área del operador. Quién puede iniciar qué guía (TrainingAccessRule, AC-8 y
    // B-20) y la elección de la propia área (OperatorAreaService, D-171), con datos en memoria
    // (P-8; PodService no admite repositorios inyectados, D-15).

    [TestFixture]
    public class TrainingAccessRuleTests
    {
        private const int LoFloId  = 3;
        private const int MoldeoId = 2;

        private static User Operator(int? areaId) =>
            new User { Id = 1, Role = UserRole.Operator, AreaId = areaId };

        private static TrainingGuide Guide(int areaId, bool published = true) =>
            new TrainingGuide { Id = 10, AreaId = areaId, IsPublished = published };

        [Test]
        public void OperatorWithArea_CanStartAPublishedGuideOfTheArea()
        {
            Assert.DoesNotThrow((Action)(() => TrainingAccessRule.EnsureCanStart(Operator(LoFloId), Guide(LoFloId), SessionMode.Guided)));
        }

        // B-20: antes solo la página lo impedía.
        [Test]
        public void GuideThatDoesNotExist_Throws()
        {
            Assert.Throws<ArgumentException>((Action)(() => TrainingAccessRule.EnsureCanStart(Operator(LoFloId), null, SessionMode.Guided)));
        }

        [Test]
        public void UnpublishedGuide_Throws()
        {
            Assert.Throws<InvalidOperationException>((Action)(() =>
                TrainingAccessRule.EnsureCanStart(Operator(LoFloId), Guide(LoFloId, published: false), SessionMode.Guided)));
        }

        [Test]
        public void GuideOfAnotherArea_Throws()
        {
            Assert.Throws<InvalidOperationException>((Action)(() =>
                TrainingAccessRule.EnsureCanStart(Operator(LoFloId), Guide(MoldeoId), SessionMode.Guided)));
        }

        [Test]
        public void OperatorWithoutArea_Throws()
        {
            Assert.Throws<InvalidOperationException>((Action)(() =>
                TrainingAccessRule.EnsureCanStart(Operator(null), Guide(LoFloId), SessionMode.Guided)));
        }

        // El administrador no entrena (spec 013), aunque tuviera un área en la BD.
        [Test]
        public void Administrator_Throws()
        {
            var admin = new User { Id = 2, Role = UserRole.Administrator, AreaId = LoFloId };
            Assert.Throws<UnauthorizedAccessException>((Action)(() => TrainingAccessRule.EnsureCanStart(admin, Guide(LoFloId), SessionMode.Guided)));
        }

        [Test]
        public void UnknownUser_Throws()
        {
            Assert.Throws<UnauthorizedAccessException>((Action)(() => TrainingAccessRule.EnsureCanStart(null, Guide(LoFloId), SessionMode.Guided)));
        }

        // B-4: un modo fuera del enum se rechaza antes de abandonar la sesión en curso.
        [Test]
        public void UndefinedMode_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
                TrainingAccessRule.EnsureCanStart(Operator(LoFloId), Guide(LoFloId), (SessionMode)99)));
        }

        [Test]
        public void IsEligible_OnlyAreasInTheList()
        {
            var eligible = new[] { new EligibleArea { AreaId = LoFloId } };
            Assert.That(TrainingAccessRule.IsEligible(LoFloId, eligible), Is.True);
            Assert.That(TrainingAccessRule.IsEligible(MoldeoId, eligible), Is.False);
            Assert.That(TrainingAccessRule.IsEligible(LoFloId, null), Is.False);
        }

        // P-9: "Producto · Área"; sin área, null.
        [Test]
        public void AreaLabel_ProductAndArea()
        {
            Assert.That(AreaLabel.Of("Sensores", "LoFlo"), Is.EqualTo("Sensores · LoFlo"));
            Assert.That(AreaLabel.Of(null, null), Is.Null);
        }
    }

    [TestFixture]
    public class OperatorAreaServiceTests
    {
        private const int OperatorId    = 1;
        private const int AdminId       = 2;
        private const int LoFloId       = 3;
        private const int NotEligibleId = 7;

        private FakeAreaUsers _users;
        private OperatorAreaService _service;

        [SetUp]
        public void SetUp()
        {
            _users = new FakeAreaUsers();
            _users.Add(new User { Id = OperatorId, Role = UserRole.Operator });
            _users.Add(new User { Id = AdminId,    Role = UserRole.Administrator });
            _service = new OperatorAreaService(_users,
                () => new List<EligibleArea> { new EligibleArea { AreaId = LoFloId } });
        }

        [Test]
        public void OperatorWithoutArea_ChoosesAnEligibleArea()
        {
            _service.ChooseOwnArea(OperatorId, LoFloId);
            Assert.That(_users.GetById(OperatorId).AreaId, Is.EqualTo(LoFloId));
        }

        // D-171: después solo un admin la cambia (AC-5).
        [Test]
        public void OperatorWithArea_CannotChooseAgain()
        {
            _users.GetById(OperatorId).AreaId = LoFloId;
            Assert.Throws<InvalidOperationException>((Action)(() => _service.ChooseOwnArea(OperatorId, LoFloId)));
            Assert.That(_users.Calls, Is.Zero);
        }

        [Test]
        public void NotEligibleArea_Throws()
        {
            Assert.Throws<ArgumentException>((Action)(() => _service.ChooseOwnArea(OperatorId, NotEligibleId)));
            Assert.That(_users.GetById(OperatorId).AreaId, Is.Null);
        }

        // P-2: el administrador no tiene área.
        [Test]
        public void Administrator_Throws()
        {
            Assert.Throws<UnauthorizedAccessException>((Action)(() => _service.ChooseOwnArea(AdminId, LoFloId)));
            Assert.That(_users.Calls, Is.Zero);
        }

        // P-4: otra pestaña eligió entre la lectura y el guardado; el SP no pisa la elección.
        [Test]
        public void Race_SpSavesNothing_Throws()
        {
            _users.ChooseAreaSavesNothing = true;
            Assert.Throws<InvalidOperationException>((Action)(() => _service.ChooseOwnArea(OperatorId, LoFloId)));
        }

        // Como spUserChooseArea: solo guarda si es Operator y sigue sin área.
        private sealed class FakeAreaUsers : IUserRepository
        {
            private readonly List<User> _users = new List<User>();
            public bool ChooseAreaSavesNothing { get; set; }
            public int Calls { get; private set; }

            public void Add(User user) => _users.Add(user);
            public User GetById(int userId) => _users.FirstOrDefault(u => u.Id == userId);

            public bool ChooseArea(int userId, int areaId)
            {
                Calls++;
                var user = _users.FirstOrDefault(u => u.Id == userId && u.Role == UserRole.Operator && u.AreaId == null);
                if (ChooseAreaSavesNothing || user == null) return false;
                user.AreaId = areaId;
                return true;
            }

            public User GetByCode1(string code1) => throw new NotSupportedException();
            public int Insert(User user) => throw new NotSupportedException();
            public bool TryInsert(User user) => throw new NotSupportedException();
            public IList<UserSummary> Search(string text, int offset, int pageSize, out int total) => throw new NotSupportedException();
            public void ResetPassword(int userId, string passwordHash) => throw new NotSupportedException();
            public void Unlock(int userId) => throw new NotSupportedException();
            public bool Delete(int callerId, int userId) => throw new NotSupportedException();
            public bool SetArea(int userId, int? areaId) => throw new NotSupportedException();
        }
    }
}
