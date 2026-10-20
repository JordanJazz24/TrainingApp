using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 012 (D-85): nombres de los pasos en Ir a… y en el resumen, con su variante cuando
    // el tipo se repite (docs/domain.md → "Ponderación de pasos").
    [TestFixture]
    public class StepNamesTests
    {
        private static List<string> Names(List<TrainingStep> steps)
            => steps.Select(s => StepNames.Display(steps, s)).ToList();

        [Test]
        public void Moldeo_AllSteps()
        {
            Assert.That(Names(ScoreCalculatorGuideTests.MoldeoGuide()), Is.EqualTo(new[]
            {
                "Ingreso a MES", "Resource Setup", "Line Clearance (apertura)", "Search / Start",
                "Data Collection", "Line Clearance (cierre)", "SFC Split", "Log NC · desecho rápido",
                "Complete", "Logout"
            }));
        }

        // LoFlo (09) tiene la misma forma que Redefinity (05).
        [Test]
        public void RedefinityAndLoFlo_AllSteps()
        {
            Assert.That(Names(ScoreCalculatorGuideTests.RedefinityGuide()), Is.EqualTo(new[]
            {
                "Ingreso a MES", "Resource Setup", "Line Clearance (apertura)", "Search / Start",
                "Assembly", "Data Collection", "Line Clearance (cierre)", "Log NC · desecho rápido",
                "Log NC · reparación rápida", "Log NC · reparación extendida", "Complete", "Logout"
            }));
        }

        [Test]
        public void EveryStepType_HasAName()
        {
            foreach (StepType type in System.Enum.GetValues(typeof(StepType)))
            {
                var step = new TrainingStep { Id = 1, Order = 1, StepType = type };
                Assert.That(StepNames.Display(new List<TrainingStep> { step }, step), Is.Not.Empty, type.ToString());
            }
        }
    }
}
