using NUnit.Framework;
using TrainingApp.BLL.Auth;
using TrainingApp.Shared;

namespace TrainingApp.Test
{
    // AUD-76: el largo mínimo de la contraseña nueva lo exige la BLL, no solo el validador del cliente.
    [TestFixture]
    public class AuthPasswordRuleTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("1234567")]
        [TestCase("         ")]
        public void CheckNewPassword_TooShort(string password)
        {
            Assert.That(AuthService.CheckNewPassword(password), Is.EqualTo(ChangePasswordStatus.NewPasswordTooShort));
        }

        [Test]
        public void CheckNewPassword_EightCharacters_Accepted()
        {
            Assert.That(AuthService.CheckNewPassword("12345678"), Is.EqualTo(ChangePasswordStatus.Success));
        }

        [Test]
        public void CheckNewPassword_DefaultPassword_Rejected()
        {
            Assert.That(AuthService.CheckNewPassword(Constants.DEFAULT_PASSWORD), Is.EqualTo(ChangePasswordStatus.NewPasswordIsDefault));
        }
    }
}
