using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Admin;
using TrainingApp.BLL.Auth;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 020: Custom Resource Setup. Datos simulados (D-184), calificación (D-183, D-191),
    // invariantes del Save (D-191, D-196), tipo por sesión (D-188) y configuración por guía.

    // Assert.Throws con una lambda que devuelve valor es ambiguo en NUnit 4 (TestDelegate/Action).
    internal static class Throwing
    {
        public static void Expect<T>(Func<object> call) where T : Exception
            => Assert.Throws<T>(new Action(() => call()));
    }

    [TestFixture]
    public class CustomResourceSetupProviderTests
    {
        [Test]
        public void ToolGroups_BetweenTwoAndFour_FromThePool_NoRepeats_Sorted()
        {
            var pool = CustomResourceSetupProvider.Pool.Select(g => g.Code).ToList();
            for (int sessionId = 1; sessionId <= 500; sessionId++)
            {
                var codes = CustomResourceSetupProvider.ToolGroupsFor(sessionId).Select(g => g.Code).ToList();
                Assert.That(codes.Count, Is.InRange(CustomResourceSetupProvider.MinToolGroups,
                                                    CustomResourceSetupProvider.MaxToolGroups));
                Assert.That(codes, Is.SubsetOf(pool));
                Assert.That(codes, Is.Unique);
                Assert.That(codes, Is.EqualTo(codes.OrderBy(c => c, StringComparer.Ordinal).ToList()));
            }
        }

        [Test]
        public void ToolGroups_SameSession_SameSet()
        {
            var first  = CustomResourceSetupProvider.ToolGroupsFor(42).Select(g => g.Code).ToList();
            var second = CustomResourceSetupProvider.ToolGroupsFor(42).Select(g => g.Code).ToList();
            Assert.That(second, Is.EqualTo(first));
        }

        // Con 500 sesiones salen las tres cantidades y casi todos los grupos del pool.
        [Test]
        public void ToolGroups_VaryAcrossSessions()
        {
            var sets = Enumerable.Range(1, 500).Select(CustomResourceSetupProvider.ToolGroupsFor).ToList();
            Assert.That(sets.Select(s => s.Count).Distinct().OrderBy(c => c), Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(sets.SelectMany(s => s.Select(g => g.Code)).Distinct().Count(),
                Is.EqualTo(CustomResourceSetupProvider.Pool.Count));
        }

        [Test]
        public void ToolGroups_AreCopies_PoolNotMutable()
        {
            var group = CustomResourceSetupProvider.ToolGroupsFor(7).First();
            string code = group.Code;
            group.Description = "cambiada";
            Assert.That(CustomResourceSetupProvider.Pool.First(g => g.Code == code).Description,
                Is.Not.EqualTo("cambiada"));
        }
    }

    [TestFixture]
    public class CustomResourceSetupAnswerTests
    {
        private static readonly TrainingStep Step = new TrainingStep { StepType = StepType.ResourceSetup, Order = 1 };

        internal static string Custom(string setupState = "Set Up", bool associated = true,
            string shopOrder = "303627317", params string[] tools)
        {
            if (tools.Length == 0) tools = new[] { "T-1" };
            string toolsJson = string.Join(",", tools.Select(t => "{\"group\":\"TG_RULER\",\"value\":\"" + t + "\"}"));
            return "{\"kind\":\"custom\",\"setupState\":\"" + setupState + "\",\"shopOrderAssociated\":" +
                   (associated ? "true" : "false") + ",\"shopOrder\":\"" + shopOrder + "\",\"tools\":[" + toolsJson + "]}";
        }

        private const string Standard = "{\"setupState\":\"Set Up\",\"rows\":[{\"type\":\"Shop Order\",\"value\":\"1\"}]}";

        private static bool Validate(ResourceSetupType type, string answer)
            => new StepValidator(new StepValidationContext { ResourceSetupType = type }).Validate(Step, answer);

        [Test]
        public void Validator_Custom_SetUpAndAssociated_IsCorrect()
            => Assert.That(Validate(ResourceSetupType.Custom, Custom()), Is.True);

        [Test]
        public void Validator_Custom_Open_IsWrong()
            => Assert.That(Validate(ResourceSetupType.Custom, Custom(setupState: "Open")), Is.False);

        [Test]
        public void Validator_Custom_NotAssociated_IsWrong()
            => Assert.That(Validate(ResourceSetupType.Custom, Custom(associated: false, shopOrder: "")), Is.False);

        // Los Tool Numbers no se califican (D-182): cualquier valor vale.
        [Test]
        public void Validator_Custom_AnyToolNumber_IsCorrect()
            => Assert.That(Validate(ResourceSetupType.Custom, Custom(tools: new[] { "x", "" })), Is.True);

        [Test]
        public void Validator_Custom_StandardAnswer_IsWrong()
            => Assert.That(Validate(ResourceSetupType.Custom, Standard), Is.False);

        [Test]
        public void Validator_Standard_Unchanged()
            => Assert.That(Validate(ResourceSetupType.Standard, Standard), Is.True);

        [Test]
        public void Require_StandardSession_StandardAnswer_ReturnsNull()
            => Assert.That(CustomResourceSetupAnswer.RequireForSession(ResourceSetupType.Standard, Standard), Is.Null);

        // D-196: el Save de la otra pantalla es una manipulación, no un intento.
        [Test]
        public void Require_StandardSession_CustomAnswer_Throws()
            => Throwing.Expect<ArgumentException>(() =>
                CustomResourceSetupAnswer.RequireForSession(ResourceSetupType.Standard, Custom()));

        [Test]
        public void Require_CustomSession_StandardAnswer_Throws()
            => Throwing.Expect<ArgumentException>(() =>
                CustomResourceSetupAnswer.RequireForSession(ResourceSetupType.Custom, Standard));

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{\"kind\":5}")]
        [TestCase("{\"kind\":\"custom\",\"shopOrderAssociated\":\"si\"}")]
        public void Require_CustomSession_Malformed_Throws(string answer)
            => Throwing.Expect<ArgumentException>(() =>
                CustomResourceSetupAnswer.RequireForSession(ResourceSetupType.Custom, answer));

        // D-191: formato que el control ya avisa; si llega, no se registra nada.
        [TestCase("")]
        [TestCase("30A")]
        public void Require_Associated_NonNumericShopOrder_Throws(string shopOrder)
            => Throwing.Expect<ArgumentException>(() =>
                CustomResourceSetupAnswer.RequireForSession(ResourceSetupType.Custom, Custom(shopOrder: shopOrder)));

        [Test]
        public void Require_Associated_NoToolNumber_Throws()
            => Throwing.Expect<ArgumentException>(() =>
                CustomResourceSetupAnswer.RequireForSession(ResourceSetupType.Custom, Custom(tools: new[] { " ", "" })));

        // Sin asociar es un intento fallido (D-191), no formato: pasa al validador.
        [Test]
        public void Require_NotAssociated_IsAccepted()
        {
            var answer = CustomResourceSetupAnswer.RequireForSession(ResourceSetupType.Custom,
                Custom(associated: false, shopOrder: "", tools: new[] { "" }));
            Assert.That(answer, Is.Not.Null);
            Assert.That(answer.ShopOrderAssociated, Is.False);
        }

        [Test]
        public void Require_Associated_Valid_ReturnsAnswer()
        {
            var answer = CustomResourceSetupAnswer.RequireForSession(ResourceSetupType.Custom, Custom(setupState: "Open"));
            Assert.That(answer.ShopOrder, Is.EqualTo("303627317"));
            Assert.That(answer.SetupState, Is.EqualTo("Open"));
        }

        private static CustomResourceSetupAnswer Parsed(bool associated, string shopOrder = " 303627317 ")
            => new CustomResourceSetupAnswer { ShopOrderAssociated = associated, ShopOrder = shopOrder };

        [TestCase(StepSubmitOutcome.Correct)]
        [TestCase(StepSubmitOutcome.Incorrect)]
        public void OrderToCreate_Associated_IsTheAssociatedOne_EvenWhenWrong(StepSubmitOutcome outcome)
            => Assert.That(CustomResourceSetupAnswer.ShopOrderToCreate(outcome, Parsed(true)), Is.EqualTo("303627317"));

        [Test]
        public void OrderToCreate_NotAssociated_None()
            => Assert.That(CustomResourceSetupAnswer.ShopOrderToCreate(StepSubmitOutcome.Incorrect, Parsed(false, null)),
                Is.Null);

        [TestCase(true)]
        [TestCase(false)]
        public void OrderToCreate_Forced_IsTheDefault(bool associated)
            => Assert.That(CustomResourceSetupAnswer.ShopOrderToCreate(StepSubmitOutcome.FailedForced, Parsed(associated, null)),
                Is.EqualTo(PodDefaults.DefaultShopOrder));
    }

    [TestFixture]
    public class ResourceSetupTypeSessionTests
    {
        // D-188: la sesión copia el tipo de la guía al empezar.
        [TestCase(ResourceSetupType.Standard)]
        [TestCase(ResourceSetupType.Custom)]
        public void NewSession_CopiesGuideType(ResourceSetupType type)
        {
            var guide   = new TrainingGuide { Id = 3, ResourceSetupType = type };
            var session = TrainingSessionService.NewSession(10, guide, SessionMode.Assessment, DateTime.UtcNow);
            Assert.That(session.ResourceSetupType, Is.EqualTo(type));
            Assert.That(session.TrainingGuideId, Is.EqualTo(3));
            Assert.That(session.Status, Is.EqualTo(SessionStatus.InProgress));
        }

        [Test]
        public void ManageGuides_OnlyAdministrator()
        {
            Assert.That(AccessPolicy.CanManageGuides(UserRole.Administrator), Is.True);
            Assert.That(AccessPolicy.CanManageGuides(UserRole.Operator), Is.False);
        }
    }

    [TestFixture]
    public class GuideAdminServiceTests
    {
        private const int AdminId    = 1;
        private const int OperatorId = 2;

        private readonly List<GuideSummary> _guides = new List<GuideSummary>();
        private GuideAdminService _service;

        [SetUp]
        public void SetUp()
        {
            _guides.Clear();
            _guides.Add(new GuideSummary { Id = 5, Title = "Guía A", IsPublished = true });
            var users = new UsersStub(
                new User { Id = AdminId,    Role = UserRole.Administrator },
                new User { Id = OperatorId, Role = UserRole.Operator });
            _service = new GuideAdminService(users, () => _guides,
                (id, type) =>
                {
                    var guide = _guides.FirstOrDefault(g => g.Id == id);
                    if (guide == null) return false;
                    guide.ResourceSetupType = type;
                    return true;
                });
        }

        [Test]
        public void Admin_ListsAndSetsType()
        {
            Assert.That(_service.GetPublishedGuides(AdminId), Has.Count.EqualTo(1));
            Assert.That(_service.SetResourceSetupType(AdminId, 5, ResourceSetupType.Custom), Is.EqualTo(GuideAdminResult.Ok));
            Assert.That(_guides[0].ResourceSetupType, Is.EqualTo(ResourceSetupType.Custom));
        }

        [Test]
        public void UnknownGuide_NotFound()
            => Assert.That(_service.SetResourceSetupType(AdminId, 99, ResourceSetupType.Custom),
                Is.EqualTo(GuideAdminResult.GuideNotFound));

        [Test]
        public void UndefinedType_Throws()
            => Throwing.Expect<ArgumentOutOfRangeException>(() =>
                _service.SetResourceSetupType(AdminId, 5, (ResourceSetupType)7));

        [TestCase(OperatorId)]
        [TestCase(99)]
        public void WithoutCapability_Throws_AndChangesNothing(int userId)
        {
            Throwing.Expect<UnauthorizedAccessException>(() => _service.GetPublishedGuides(userId));
            Throwing.Expect<UnauthorizedAccessException>(() =>
                _service.SetResourceSetupType(userId, 5, ResourceSetupType.Custom));
            Assert.That(_guides[0].ResourceSetupType, Is.EqualTo(ResourceSetupType.Standard));
        }

        private sealed class UsersStub : IUserRepository
        {
            private readonly List<User> _users;
            public UsersStub(params User[] users) { _users = users.ToList(); }

            public User GetById(int userId) => _users.FirstOrDefault(u => u.Id == userId);
            public User GetByCode1(string code1) => throw new NotSupportedException();
            public int Insert(User user) => throw new NotSupportedException();
            public bool TryInsert(User user) => throw new NotSupportedException();
            public IList<UserSummary> Search(string text, int offset, int pageSize, out int total)
                => throw new NotSupportedException();
            public void ResetPassword(int userId, string passwordHash) => throw new NotSupportedException();
            public void Unlock(int userId) => throw new NotSupportedException();
            public bool Delete(int callerId, int userId) => throw new NotSupportedException();
        }
    }
}
