using System;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Training;

namespace TrainingApp.Test
{
    // Spec 017: el catálogo de mensajes del MES. Los textos esperados son los de la tabla
    // "Catálogo de mensajes" de la spec, literales.
    [TestFixture]
    public class MesMessageTests
    {
        private static readonly (string Name, string Value)[] NoArgs = new (string, string)[0];

        private static object[] Cases =
        {
            new object[] { MesMessageId.LineClearanceNotDone, NoArgs,
                "Line Clearance has not been done since the last Resource Setup, please perform Line Clearance (Message 22501)" },
            new object[] { MesMessageId.LineClearanceStatusNo, NoArgs,
                "Line Clearance status is NO, please perform Line Clearance (Message 22501)" },
            new object[] { MesMessageId.SfcMissingStart, NoArgs, "SFC is missing (Message 22506)" },
            new object[] { MesMessageId.SfcMissing, NoArgs, "SFC is missing (Message 12906)" },
            new object[] { MesMessageId.InvalidSfc, NoArgs, "Invalid SFC (Message 22507)" },
            new object[] { MesMessageId.MissingDataCollection,
                new[] { ("list", "TEMP (DCG_A) , PRESSURE (DCG_B)"), ("sfc", "300014142061-42812") },
                "There are missing required values for data collection parameter(s) TEMP (DCG_A) , PRESSURE (DCG_B) for SFC \"300014142061-42812\" (Message 15246)" },
            new object[] { MesMessageId.BarcodeNotCompliant, NoArgs,
                "2D barcode is not compliant with ISO 15434 format (Message 17150)" },
            new object[] { MesMessageId.RequiredValueMissing, new[] { ("param", "TEMP") },
                "Required value \"TEMP\" missing; enter a value in the TEMP field (Message 10015)" },
            new object[] { MesMessageId.FieldMustBeNumber, new[] { ("param", "DC_WELDING_MACHINE") },
                "Field DC_WELDING_MACHINE must contain a number (Message 12205)" },
            new object[] { MesMessageId.ValueAboveUpperLimit, new[] { ("value", "99999") },
                "Value 99999 is above the upper limit (Message 15233)" },
            new object[] { MesMessageId.ValueBelowLowerLimit, new[] { ("value", "-5") },
                "Value -5 is below the lower limit (Message 15232)" },
            new object[] { MesMessageId.SplitQtyMissing, NoArgs, "Define Qty to Split (Message 13875)" },
            new object[] { MesMessageId.SplitQtyAboveAvailable, new[] { ("n", "30"), ("m", "20") },
                "Requested qty to split 30 is greater than the available SFC qty 20 (Message 13885)" },
            new object[] { MesMessageId.SplitQtyNotPositive, NoArgs, "Quantity must be greater than 0 (Message 13213)" }
        };

        [TestCaseSource(nameof(Cases))]
        public void Format_WithNumber_EndsWithMessageNumberAndReplacesMarkers(
            MesMessageId id, (string Name, string Value)[] args, string expected)
        {
            var message = MesMessage.Get(id);

            Assert.That(message.Number, Is.Not.Null);
            Assert.That(message.Format(args), Is.EqualTo(expected));
            Assert.That(message.Format(args), Does.EndWith("(Message " + message.Number + ")"));
        }

        // C-10: provisional y sin número.
        [Test]
        public void ResourceSetupMissing_HasNoNumber()
        {
            var message = MesMessage.Get(MesMessageId.ResourceSetupMissing);

            Assert.That(message.Number, Is.Null);
            Assert.That(message.Format(),
                Is.EqualTo("Resource Setup has not been done, please perform Resource Setup"));
        }

        // M-1 a M-16 sin M-10 (D-118); M-17 y M-18 tampoco (D-120, D-118): solo lo que el
        // simulador puede mostrar, cada uno con su id.
        [Test]
        public void Catalog_HasEveryMessageTheSimulatorCanShow()
        {
            var ids = MesMessage.All.Select(m => (int)m.Id).OrderBy(n => n).ToArray();

            Assert.That(ids, Is.EqualTo(Enumerable.Range(1, 16).Where(n => n != 10).ToArray()));
            Assert.That(Enum.GetValues(typeof(MesMessageId)).Length, Is.EqualTo(15));
        }

        [Test]
        public void Surfaces_MatchTheSpec()
        {
            Assert.Multiple(new Action(() =>
            {
                foreach (var id in new[] { MesMessageId.ResourceSetupMissing, MesMessageId.LineClearanceNotDone,
                    MesMessageId.LineClearanceStatusNo, MesMessageId.SfcMissingStart, MesMessageId.SfcMissing,
                    MesMessageId.InvalidSfc, MesMessageId.MissingDataCollection })
                    Assert.That(MesMessage.Get(id).Surface, Is.EqualTo(MesSurface.PodBanner), id.ToString());

                foreach (var id in new[] { MesMessageId.BarcodeNotCompliant, MesMessageId.RequiredValueMissing,
                    MesMessageId.FieldMustBeNumber, MesMessageId.ValueAboveUpperLimit,
                    MesMessageId.ValueBelowLowerLimit })
                    Assert.That(MesMessage.Get(id).Surface, Is.EqualTo(MesSurface.WindowStrip), id.ToString());

                foreach (var id in new[] { MesMessageId.SplitQtyMissing, MesMessageId.SplitQtyAboveAvailable,
                    MesMessageId.SplitQtyNotPositive })
                    Assert.That(MesMessage.Get(id).Surface, Is.EqualTo(MesSurface.SplitForm), id.ToString());
            }));
        }

        // M-9: las comillas del texto del MES y las del valor quedan tal cual (lo codifica quien
        // lo pinta).
        [Test]
        public void Format_KeepsQuotesIntact()
        {
            string text = MesMessage.Get(MesMessageId.RequiredValueMissing).Format(("param", "Peso \"bruto\""));

            Assert.That(text, Is.EqualTo(
                "Required value \"Peso \"bruto\"\" missing; enter a value in the Peso \"bruto\" field (Message 10015)"));
        }

        // Un valor con algo que parece un marcador no se vuelve a reemplazar.
        [Test]
        public void Format_DoesNotReplaceMarkersInsideValues()
        {
            string text = MesMessage.Get(MesMessageId.ValueAboveUpperLimit).Format(("value", "{param}"));

            Assert.That(text, Is.EqualTo("Value {param} is above the upper limit (Message 15233)"));
        }

        [Test]
        public void Format_WithoutAMarkerValue_Throws()
        {
            Assert.Throws<ArgumentException>(new Action(
                () => MesMessage.Get(MesMessageId.SplitQtyAboveAvailable).Format(("n", "3"))));
        }

        [Test]
        public void Error_CarriesIdTextAndSurface()
        {
            var error = MesMessage.Error(MesMessageId.SfcMissing);

            Assert.That(error.Id, Is.EqualTo(MesMessageId.SfcMissing));
            Assert.That(error.Text, Is.EqualTo("SFC is missing (Message 12906)"));
            Assert.That(error.Surface, Is.EqualTo(MesSurface.PodBanner));
        }
    }
}
