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
    public class StartWithoutChecklistTests
    {
        [Test]
        public void Search_WithoutChecklist_IsGradedBySequenceOnly()
        {
            var area = new Area { RequiresStartChecklist = false };
            var validator = new StepValidator(new StepValidationContext { Area = area });
            var step = new TrainingStep { StepType = StepType.Search };

            Assert.That(validator.Validate(step, "start"), Is.True);
        }

        [Test]
        public void Search_WithChecklist_StillRequiresConfirmations()
        {
            var area = new Area { RequiresStartChecklist = true };
            var validator = new StepValidator(new StepValidationContext { Area = area });
            var step = new TrainingStep { StepType = StepType.Search };

            Assert.That(validator.Validate(step, "start"), Is.False);
        }
    }

    [TestFixture]
    public class SerialSfcGeneratorTests
    {
        [Test]
        public void Generate_Returns3To5UniquePiecesWithNumericSuffixOf1To6Digits()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var sfcs = TrainingApp.BLL.Training.SerialSfcGenerator.Generate("000123", new System.Random(seed));

                Assert.That(sfcs.Count, Is.InRange(3, 5));
                Assert.That(sfcs, Is.Unique);
                foreach (var sfc in sfcs)
                    Assert.That(sfc, Does.Match(@"^000123-\d{1,6}$"));
            }
        }
    }

    [TestFixture]
    public class PieceSelectionRuleTests
    {
        private static TrainingApp.BLL.Training.PieceSelectionResult Eval(StepType step, int selected, int available)
            => TrainingApp.BLL.Training.PieceSelectionRule.Evaluate(step, selected, available);

        [Test]
        public void StartAssemblyDcAndComplete_RequireAllPieces()
        {
            foreach (var step in new[] { StepType.Search, StepType.Assembly, StepType.DataCollection, StepType.Complete })
            {
                Assert.That(Eval(step, 3, 3), Is.EqualTo(TrainingApp.BLL.Training.PieceSelectionResult.Valid));
                Assert.That(Eval(step, 2, 3), Is.EqualTo(TrainingApp.BLL.Training.PieceSelectionResult.MissingPieces));
                Assert.That(Eval(step, 0, 3), Is.EqualTo(TrainingApp.BLL.Training.PieceSelectionResult.NothingSelected));
            }
        }

        [Test]
        public void LogNC_NeedsAtLeastOneAndMustLeaveOnePiece()
        {
            Assert.That(Eval(StepType.LogNC, 2, 3), Is.EqualTo(TrainingApp.BLL.Training.PieceSelectionResult.Valid));
            Assert.That(Eval(StepType.LogNC, 3, 3), Is.EqualTo(TrainingApp.BLL.Training.PieceSelectionResult.NoPieceLeft));
            Assert.That(Eval(StepType.LogNC, 0, 3), Is.EqualTo(TrainingApp.BLL.Training.PieceSelectionResult.NothingSelected));
        }

        [Test]
        public void OnlyEmptySelectionInLogNcOrComplete_IsAFailedAttempt()
        {
            var none = TrainingApp.BLL.Training.PieceSelectionResult.NothingSelected;
            var missing = TrainingApp.BLL.Training.PieceSelectionResult.MissingPieces;

            Assert.That(TrainingApp.BLL.Training.PieceSelectionRule.CountsAsFailedAttempt(StepType.LogNC, none), Is.True);
            Assert.That(TrainingApp.BLL.Training.PieceSelectionRule.CountsAsFailedAttempt(StepType.Complete, none), Is.True);
            Assert.That(TrainingApp.BLL.Training.PieceSelectionRule.CountsAsFailedAttempt(StepType.DataCollection, none), Is.False);
            Assert.That(TrainingApp.BLL.Training.PieceSelectionRule.CountsAsFailedAttempt(StepType.Complete, missing), Is.False);
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
