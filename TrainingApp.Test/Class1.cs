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
    public class AssemblyProgressTests
    {
        [Test]
        public void Covers_OnlyWhenEveryBomComponentIsAssembled()
        {
            var bom = new[] { new AssemblyComponent { Id = 1 }, new AssemblyComponent { Id = 2 } };
            var progress = new TrainingApp.BLL.Training.AssemblyProgress();

            Assert.That(progress.Covers(bom), Is.False);
            progress.AssembledComponentIds.Add(1);
            Assert.That(progress.Covers(bom), Is.False);
            progress.AssembledComponentIds.Add(2);
            Assert.That(progress.Covers(bom), Is.True);
        }

        [Test]
        public void Covers_EmptyBomNeverCompletes()
        {
            var progress = new TrainingApp.BLL.Training.AssemblyProgress();
            Assert.That(progress.Covers(new AssemblyComponent[0]), Is.False);
        }
    }

    [TestFixture]
    public class DataCollectionCompletionRuleTests
    {
        private static readonly DataCollectionGroup Required1 = new DataCollectionGroup { Id = 1, IsRequired = true };
        private static readonly DataCollectionGroup Required2 = new DataCollectionGroup { Id = 2, IsRequired = true };
        private static readonly DataCollectionGroup Optional3 = new DataCollectionGroup { Id = 3, IsRequired = false };

        [Test]
        public void WithRequiredGroups_PieceNeedsAllOfThem()
        {
            var groups = new[] { Required1, Required2, Optional3 };

            Assert.That(DataCollectionCompletionRule.IsPieceComplete(new[] { 1 }, groups), Is.False);
            Assert.That(DataCollectionCompletionRule.IsPieceComplete(new[] { 3 }, groups), Is.False);
            Assert.That(DataCollectionCompletionRule.IsPieceComplete(new[] { 1, 3 }, groups), Is.False);
            Assert.That(DataCollectionCompletionRule.IsPieceComplete(new[] { 1, 2 }, groups), Is.True);
        }

        [Test]
        public void WithoutRequiredGroups_AnyGroupCompletesThePiece()
        {
            var groups = new[] { Optional3, new DataCollectionGroup { Id = 4 } };

            Assert.That(DataCollectionCompletionRule.IsPieceComplete(new int[0], groups), Is.False);
            Assert.That(DataCollectionCompletionRule.IsPieceComplete(new[] { 4 }, groups), Is.True);
        }
    }

    [TestFixture]
    public class DataCollectionProgressTests
    {
        [Test]
        public void SavingTheSameGroupTwice_CountsOnce_AndPiecesAdvanceInOrder()
        {
            var progress = new TrainingApp.BLL.Training.DataCollectionProgress();
            progress.Pieces.Add(new TrainingApp.BLL.Training.DataCollectionPiece { RowId = "a", SFCCode = "000123-7" });
            progress.Pieces.Add(new TrainingApp.BLL.Training.DataCollectionPiece { RowId = "b", SFCCode = "000123-430" });

            progress.MarkSaved("a", 1);
            progress.MarkSaved("a", 1);
            Assert.That(progress.SavedGroupIds("a").Count, Is.EqualTo(1));
            Assert.That(progress.SavedGroupIds("b").Count, Is.EqualTo(0));

            Assert.That(progress.CurrentPiece.SFCCode, Is.EqualTo("000123-7"));
            progress.CurrentIndex++;
            Assert.That(progress.CurrentPiece.SFCCode, Is.EqualTo("000123-430"));
            progress.CurrentIndex++;
            Assert.That(progress.IsFinished, Is.True);
        }
    }

    [TestFixture]
    public class DataCollectionGroupValidationTests
    {
        private static StepValidator BuildValidator()
        {
            return new StepValidator(new StepValidationContext
            {
                DataCollectionParams = new[]
                {
                    new DataCollectionParameter { Id = 10, DataCollectionGroupId = 1, DataType = "Numeric", MinValue = 10m, MaxValue = 15m },
                    new DataCollectionParameter { Id = 11, DataCollectionGroupId = 1, DataType = "Boolean" },
                    new DataCollectionParameter { Id = 20, DataCollectionGroupId = 2, DataType = "Numeric" }
                }
            });
        }

        [Test]
        public void AllGroupParamsWithValidValues_IsCorrect()
        {
            Assert.That(BuildValidator().ValidateDataCollectionGroup(1, "{\"10\":\"12\",\"11\":\"true\"}"), Is.True);
        }

        [Test]
        public void MissingEmptyOutOfRangeOrForeignParams_AreIncorrect()
        {
            var validator = BuildValidator();

            Assert.That(validator.ValidateDataCollectionGroup(1, "{\"10\":\"12\"}"), Is.False);                    // falta 11
            Assert.That(validator.ValidateDataCollectionGroup(1, "{\"10\":\"\",\"11\":\"true\"}"), Is.False);      // vacío
            Assert.That(validator.ValidateDataCollectionGroup(1, "{\"10\":\"20\",\"11\":\"true\"}"), Is.False);    // fuera de rango
            Assert.That(validator.ValidateDataCollectionGroup(1, "{\"10\":\"12\",\"20\":\"true\"}"), Is.False);    // param de otro grupo
            Assert.That(validator.ValidateDataCollectionGroup(1, "not json"), Is.False);
        }
    }
}
