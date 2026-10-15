using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Admin;
using TrainingApp.BLL.Auth;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 013: mapa rol → capacidades (AccessPolicy) y gestión de usuarios (UserAdminService)
    // con datos en memoria. AC-5: sin la capacidad, cada método lanza excepción.

    [TestFixture]
    public class AccessPolicyTests
    {
        [Test]
        public void Operator_TrainsAndDoesNotManageUsers()
        {
            Assert.That(AccessPolicy.CanTrain(UserRole.Operator), Is.True);
            Assert.That(AccessPolicy.CanManageUsers(UserRole.Operator), Is.False);
        }

        [Test]
        public void Administrator_ManagesUsersAndDoesNotTrain()
        {
            Assert.That(AccessPolicy.CanManageUsers(UserRole.Administrator), Is.True);
            Assert.That(AccessPolicy.CanTrain(UserRole.Administrator), Is.False);
        }

        [TestCase(UserRole.Operator,      LoginPortal.Training, true)]
        [TestCase(UserRole.Operator,      LoginPortal.Admin,    false)]
        [TestCase(UserRole.Administrator, LoginPortal.Admin,    true)]
        [TestCase(UserRole.Administrator, LoginPortal.Training, false)]
        public void EachPortal_AcceptsOnlyItsRole(UserRole role, LoginPortal portal, bool expected)
        {
            Assert.That(AccessPolicy.CanUsePortal(role, portal), Is.EqualTo(expected));
        }
    }

    [TestFixture]
    public class UserAdminServiceTests
    {
        private const int AdminId    = 1;
        private const int OperatorId = 2;
        private const int UnknownId  = 99;

        private FakeUserRepository _users;
        private UserAdminService _service;

        [SetUp]
        public void SetUp()
        {
            _users = new FakeUserRepository();
            _users.Add(new User { Id = AdminId,    Code1 = "A1", Name = "Admin",    Role = UserRole.Administrator });
            _users.Add(new User { Id = OperatorId, Code1 = "O1", Name = "Operador", Role = UserRole.Operator });

            _service = new UserAdminService(_users,
                code1 => code1 == "NEW" ? new Employee { Code1 = "NEW", Nombre = "Nuevo", Apellidos = "Empleado" } : null);
        }

        // AC-5: un operador (o un Id que no existe) no puede usar ningún método.
        private static IEnumerable<TestCaseData> AdminActions()
        {
            yield return new TestCaseData(new Action<UserAdminService, int>((s, caller) => s.Search(caller, null, 1))).SetName("Search");
            yield return new TestCaseData(new Action<UserAdminService, int>((s, caller) => s.Create(caller, "NEW", UserRole.Operator))).SetName("Create");
            yield return new TestCaseData(new Action<UserAdminService, int>((s, caller) => s.ResetPassword(caller, AdminId))).SetName("ResetPassword");
            yield return new TestCaseData(new Action<UserAdminService, int>((s, caller) => s.Unlock(caller, AdminId))).SetName("Unlock");
            yield return new TestCaseData(new Action<UserAdminService, int>((s, caller) => s.Delete(caller, AdminId))).SetName("Delete");
        }

        [TestCaseSource(nameof(AdminActions))]
        public void Operator_CannotUseAdminActions(Action<UserAdminService, int> action)
        {
            Assert.Throws<UnauthorizedAccessException>((Action)(() => action(_service, OperatorId)));
            Assert.That(_users.Changes, Is.Empty, "Sin la capacidad no se cambia nada.");
        }

        [TestCaseSource(nameof(AdminActions))]
        public void UnknownCaller_CannotUseAdminActions(Action<UserAdminService, int> action)
        {
            Assert.Throws<UnauthorizedAccessException>((Action)(() => action(_service, UnknownId)));
            Assert.That(_users.Changes, Is.Empty);
        }

        // AC-13b: nadie se borra a sí mismo (así nunca quedan cero admins).
        [Test]
        public void Delete_OwnAccount_IsRejected()
        {
            Assert.That(_service.Delete(AdminId, AdminId), Is.EqualTo(UserAdminResult.OwnAccount));
            Assert.That(_users.Changes, Is.Empty);
        }

        [Test]
        public void Delete_Operator_RemovesTheAccount()
        {
            Assert.That(_service.Delete(AdminId, OperatorId), Is.EqualTo(UserAdminResult.Ok));
            Assert.That(_users.GetById(OperatorId), Is.Null);
            Assert.That(_service.Delete(AdminId, OperatorId), Is.EqualTo(UserAdminResult.UserNotFound));
        }

        // Otro admin borró o degradó a quien llama entre la verificación y el SP.
        [Test]
        public void Delete_CallerNoLongerAdminInTheSp_Throws()
        {
            _users.RejectDeletes = true;

            Assert.Throws<UnauthorizedAccessException>((Action)(() => _service.Delete(AdminId, OperatorId)));
        }

        [Test]
        public void Create_UndefinedRole_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => _service.Create(AdminId, "NEW", (UserRole)3)));
            Assert.That(_users.Changes, Is.Empty);
        }

        [Test]
        public void Create_RequiresEmployeeInApiAndNoAccount()
        {
            Assert.That(_service.Create(AdminId, "NOPE", UserRole.Operator), Is.EqualTo(UserAdminResult.NotInEmployeeApi));
            Assert.That(_service.Create(AdminId, "NEW", UserRole.Administrator), Is.EqualTo(UserAdminResult.Ok));
            Assert.That(_service.Create(AdminId, "NEW", UserRole.Operator), Is.EqualTo(UserAdminResult.AlreadyExists));

            var created = _users.GetByCode1("NEW");
            Assert.That(created.Role, Is.EqualTo(UserRole.Administrator));
            Assert.That(created.IsFirstLogin, Is.True);
            Assert.That(created.Name, Is.EqualTo("Nuevo Empleado"));
        }

        // Repositorio en memoria: registra los cambios para verificar que sin la capacidad no
        // se toca nada.
        private sealed class FakeUserRepository : IUserRepository
        {
            private readonly List<User> _users = new List<User>();
            public List<string> Changes { get; } = new List<string>();
            public bool RejectDeletes { get; set; }

            public void Add(User user) => _users.Add(user);

            public User GetById(int userId) => _users.FirstOrDefault(u => u.Id == userId);
            public User GetByCode1(string code1) => _users.FirstOrDefault(u => u.Code1 == code1);

            public int Insert(User user)
            {
                user.Id = _users.Max(u => u.Id) + 1;
                _users.Add(user);
                Changes.Add("Insert");
                return user.Id;
            }

            public bool TryInsert(User user)
            {
                if (GetByCode1(user.Code1) != null) return false;
                Insert(user);
                return true;
            }

            public IList<UserSummary> Search(string text, int offset, int pageSize, out int total)
            {
                total = _users.Count;
                return _users.Skip(offset).Take(pageSize)
                    .Select(u => new UserSummary { Id = u.Id, Code1 = u.Code1, Name = u.Name, Role = u.Role })
                    .ToList();
            }

            public void ResetPassword(int userId, string passwordHash) => Changes.Add("ResetPassword");
            public void Unlock(int userId) => Changes.Add("Unlock");

            public bool Delete(int callerId, int userId)
            {
                if (RejectDeletes) return false;
                _users.RemoveAll(u => u.Id == userId);
                Changes.Add("Delete");
                return true;
            }
        }
    }
}
