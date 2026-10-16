using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 014 (AC-8, AC-10, AC-11, AC-23): Log NC se califica con la pieza y el cÃ³digo exacto
    // del hallazgo.
    [TestFixture]
    public class StepValidatorLogNCTests
    {
        private static readonly IList<NCCode> Catalog = new List<NCCode>
        {
            new NCCode { Code = "F_FS_MANCHAS", Description = "Manchas",          Method = NCMethod.FastScrap },
            new NCCode { Code = "F_FS_RAYAS",   Description = "Rayas",            Method = NCMethod.FastScrap },
            new NCCode { Code = "F_FR_GENERAL", Description = "Fast Repair",      Method = NCMethod.FastRepair },
            new NCCode { Code = "F_FX_GENERAL", Description = "Fast Extended",    Method = NCMethod.ExtendedRepair, RequiresComments = true }
        };

        private static readonly TrainingStep Step = new TrainingStep { StepType = StepType.LogNC, Order = 8 };

        private static SimulatedOrderRow Row(string id, bool fromSplit = false)
            => new SimulatedOrderRow { RowId = id, SFCCode = "SFC-" + id, Qty = 1, Status = SimulatedOrderRow.StatusActive, IsFromSplit = fromSplit };

        private static string Answer(string code) => "{\"code\":\"" + code + "\"}";

        private static bool Validate(NcFinding finding, string answer, params SimulatedOrderRow[] selected)
            => new StepValidator(new StepValidationContext
            {
                ValidNCCodes = Catalog,
                NcFinding    = finding,
                SelectedRows = selected
            }).Validate(Step, answer);

        private static NcFinding SerializedFinding()
            => new NcFinding { StepOrder = 8, RowId = "b", SFCCode = "SFC-b", Code = "F_FS_MANCHAS", Method = NCMethod.FastScrap };

        private static NcFinding LotFinding()
            => new NcFinding { StepOrder = 8, RowId = null, SFCCode = "CR9A00001234", UnitCount = 12, Code = "F_FS_RAYAS", Method = NCMethod.FastScrap };

        [Test]
        public void Serialized_FindingPieceAndExactCode_IsCorrect()
        {
            Assert.That(Validate(SerializedFinding(), Answer("F_FS_MANCHAS"), Row("b")), Is.True);
            // El cÃ³digo no distingue mayÃºsculas, como antes.
            Assert.That(Validate(SerializedFinding(), Answer("f_fs_manchas"), Row("b")), Is.True);
        }

        [Test]
        public void Serialized_OtherPiece_IsIncorrect()
        {
            Assert.That(Validate(SerializedFinding(), Answer("F_FS_MANCHAS"), Row("a")), Is.False);
        }

        [Test]
        public void Serialized_FindingPiecePlusAnother_IsIncorrect()
        {
            Assert.That(Validate(SerializedFinding(), Answer("F_FS_MANCHAS"), Row("b"), Row("c")), Is.False);
        }

        [Test]
        public void Serialized_NoSelection_IsIncorrect()
        {
            Assert.That(Validate(SerializedFinding(), Answer("F_FS_MANCHAS")), Is.False);
        }

        [Test]
        public void OtherCodeOfSameMethod_IsIncorrect()
        {
            Assert.That(Validate(SerializedFinding(), Answer("F_FS_RAYAS"), Row("b")), Is.False);
        }

        [Test]
        public void CodeOfOtherMethod_IsIncorrect()
        {
            // AC-23: en Moldeo, F_FR_GENERAL o F_FX_GENERAL en el desecho son intento fallido.
            Assert.That(Validate(SerializedFinding(), Answer("F_FR_GENERAL"), Row("b")), Is.False);
            Assert.That(Validate(SerializedFinding(), Answer("F_FX_GENERAL"), Row("b")), Is.False);
        }

        [Test]
        public void NonSerialized_SplitRowAndExactCode_IsCorrect()
        {
            Assert.That(Validate(LotFinding(), Answer("F_FS_RAYAS"), Row("split", fromSplit: true)), Is.True);
        }

        [Test]
        public void NonSerialized_LotRow_IsIncorrect()
        {
            // P-9: el SFC del lote es intento fallido.
            Assert.That(Validate(LotFinding(), Answer("F_FS_RAYAS"), Row("lot")), Is.False);
        }

        [Test]
        public void WrongButtonOrNoFinding_IsIncorrect()
        {
            Assert.That(Validate(SerializedFinding(), StepAnswers.Invalid, Row("b")), Is.False);
            Assert.That(Validate(SerializedFinding(), "F_FS_MANCHAS", Row("b")), Is.False);
            Assert.That(Validate(null, Answer("F_FS_MANCHAS"), Row("b")), Is.False);
        }
    }

    // Spec 014 (Scope punto 3): un hallazgo por paso de Log NC, en piezas distintas, con un
    // cÃ³digo del mÃ©todo del paso.
    [TestFixture]
    public class NcFindingGeneratorTests
    {
        private static readonly IList<NCCode> Catalog = new List<NCCode>
        {
            new NCCode { Code = "F_FS_A", Description = "A", Method = NCMethod.FastScrap },
            new NCCode { Code = "F_FS_B", Description = "B", Method = NCMethod.FastScrap },
            new NCCode { Code = "F_FR_A", Description = "C", Method = NCMethod.FastRepair },
            new NCCode { Code = "F_FX_A", Description = "D", Method = NCMethod.ExtendedRepair }
        };

        private static IList<TrainingStep> Guide(params StepType[] types)
            => types.Select((t, i) => new TrainingStep { StepType = t, Order = i + 1 }).ToList();

        private static IList<SimulatedOrderRow> Pieces(int count)
            => Enumerable.Range(1, count).Select(i => new SimulatedOrderRow
            {
                RowId = "r" + i, SFCCode = "000123-" + i, Qty = 1, Status = SimulatedOrderRow.StatusActive
            }).ToList();

        [Test]
        public void Serialized_OneFindingPerLogNCStep_OnDistinctPieces()
        {
            // Tres pasos de Log NC (los tres mÃ©todos llegan en E2/E3; hoy, el desecho repetido).
            var guide = Guide(StepType.Search, StepType.LogNC, StepType.LogNC, StepType.LogNC, StepType.Complete);
            for (int seed = 0; seed < 200; seed++)
            {
                var findings = NcFindingGenerator.Generate(guide, Pieces(4), SerializationType.Serialized,
                    Catalog, 250, new Random(seed));

                Assert.That(findings.Select(f => f.StepOrder), Is.EqualTo(new[] { 2, 3, 4 }));
                Assert.That(findings.Select(f => f.RowId), Is.Unique);
                Assert.That(findings.Select(f => f.Index), Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(findings.All(f => f.Total == 3), Is.True);
                Assert.That(findings.All(f => f.RowId != null && f.SFCCode == "000123-" + f.RowId.Substring(1)), Is.True);
            }
        }

        [Test]
        public void CodeIsOfTheStepMethod()
        {
            var guide = Guide(StepType.LogNC);
            for (int seed = 0; seed < 200; seed++)
            {
                var finding = NcFindingGenerator.Generate(guide, Pieces(4), SerializationType.Serialized,
                    Catalog, 250, new Random(seed)).Single();

                Assert.That(finding.Method, Is.EqualTo(NCMethod.FastScrap));
                Assert.That(finding.Code, Is.AnyOf("F_FS_A", "F_FS_B"));
                Assert.That(finding.Description, Is.EqualTo(Catalog.Single(c => c.Code == finding.Code).Description));
            }
        }

        [Test]
        public void NonSerialized_UnitsBelowLotQty_WithoutPiece()
        {
            var guide = Guide(StepType.SFC_Split, StepType.LogNC);
            for (int seed = 0; seed < 200; seed++)
            {
                var finding = NcFindingGenerator.Generate(guide, null, SerializationType.NonSerialized,
                    Catalog, 250, new Random(seed)).Single();

                Assert.That(finding.RowId, Is.Null);
                Assert.That(finding.UnitCount, Is.InRange(1, 249));
            }
        }

        [Test]
        public void NonSerialized_NeverAsksForTheWholeLot()
        {
            // Enmienda 3: N va de 1 a lote âˆ’ 1, tambiÃ©n con lotes chicos.
            var guide = Guide(StepType.SFC_Split, StepType.LogNC);
            foreach (int lot in new[] { 2, 3, 250 })
                for (int seed = 0; seed < 200; seed++)
                {
                    var finding = NcFindingGenerator.Generate(guide, null, SerializationType.NonSerialized,
                        Catalog, lot, new Random(seed)).Single();
                    Assert.That(finding.UnitCount, Is.InRange(1, lot - 1));
                }

            Assert.Throws<ArgumentOutOfRangeException>(new Action(() => NcFindingGenerator.Generate(guide, null,
                SerializationType.NonSerialized, Catalog, 1, new Random(1))));
        }

        [Test]
        public void GuideWithoutLogNC_HasNoFindings()
        {
            var findings = NcFindingGenerator.Generate(Guide(StepType.Search, StepType.Complete), Pieces(4),
                SerializationType.Serialized, Catalog, 250, new Random(1));
            Assert.That(findings, Is.Empty);
        }

        [Test]
        public void MissingCodesOfTheMethod_Throws()
        {
            var onlyRepair = Catalog.Where(c => c.Method != NCMethod.FastScrap).ToList();
            Assert.Throws<InvalidOperationException>(new Action(() => NcFindingGenerator.Generate(Guide(StepType.LogNC),
                Pieces(4), SerializationType.Serialized, onlyRepair, 250, new Random(1))));
        }
    }
}
