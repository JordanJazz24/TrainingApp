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
    }
}
