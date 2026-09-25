using NUnit.Framework;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    [TestFixture]
    public class StepValidatorTests
    {
        [Test]
        public void SFCSplit_AcceptsOnlyCompletedToken()
        {
            var validator = new StepValidator(new StepValidationContext());
            var step = new TrainingStep { StepType = StepType.SFC_Split };

            Assert.That(validator.Validate(step, "split-completed"), Is.True);
            Assert.That(validator.Validate(step, ""), Is.False);
        }

        [Test]
        public void ResourceSetup_RequiresSetUpState()
        {
            var validator = new StepValidator(new StepValidationContext());
            var step = new TrainingStep { StepType = StepType.ResourceSetup };

            Assert.That(validator.Validate(step, "{\"setupState\":\"Set Up\",\"rows\":[]}"), Is.True);
            Assert.That(validator.Validate(step, "{\"setupState\":\"Open\",\"rows\":[]}"), Is.False);
            Assert.That(validator.Validate(step, ""), Is.False);
        }

        [Test]
        public void Search_RequiresAllConfirmations()
        {
            var validator = new StepValidator(new StepValidationContext());
            var step = new TrainingStep { StepType = StepType.Search };
            const string ok = "{\"moldeId\":\"1\",\"verifCalibracion\":true,\"verifD0128682\":true," +
                              "\"verifD0128035\":true,\"verificacionFinal\":true}";

            Assert.That(validator.Validate(step, ok), Is.True);
            Assert.That(validator.Validate(step, ok.Replace("\"verifD0128682\":true", "\"verifD0128682\":false")), Is.False);
            Assert.That(validator.Validate(step, ok.Replace("\"verificacionFinal\":true", "\"verificacionFinal\":false")), Is.False);
            Assert.That(validator.Validate(step, "not json"), Is.False);
        }
    }

    [TestFixture]
    public class ScoreCalculatorPassingTests
    {
        [Test]
        public void IsPassed_UsesSeventyPercentThreshold()
        {
            Assert.That(ScoreCalculator.IsPassed(0.70m), Is.True);
            Assert.That(ScoreCalculator.IsPassed(0.699m), Is.False);
        }
    }
}
