using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 017 (C-6, plan E3): el mensaje del MES de un Save de DC incorrecto. Uno solo, el del
    // primer parámetro con fallo en el orden de la pantalla. No cambia qué grupo es correcto.
    [TestFixture]
    public class StepValidatorDataCollectionTests
    {
        private const int GroupId = 7;

        // Ids desordenados respecto de DisplayOrder: el orden es el de la pantalla.
        private static readonly DataCollectionParameter Temp = new DataCollectionParameter
            { Id = 30, Name = "TEMP", DataType = "Numeric", MinValue = 10, MaxValue = 50, DisplayOrder = 1, DataCollectionGroupId = GroupId };
        private static readonly DataCollectionParameter Pressure = new DataCollectionParameter
            { Id = 10, Name = "PRESSURE", DataType = "Numeric", MinValue = 1, MaxValue = 5, DisplayOrder = 2, DataCollectionGroupId = GroupId };
        private static readonly DataCollectionParameter Ok = new DataCollectionParameter
            { Id = 20, Name = "VISUAL_OK", DataType = "Boolean", DisplayOrder = 3, DataCollectionGroupId = GroupId };
        private static readonly DataCollectionParameter Trainer = new DataCollectionParameter
            { Id = 40, Name = "TRAINER", DataType = PersonNameRule.DataType, DisplayOrder = 0, DataCollectionGroupId = GroupId };

        private static StepValidator Validator(params DataCollectionParameter[] parameters)
            => new StepValidator(new StepValidationContext { DataCollectionParams = parameters.ToList() });

        private static readonly StepValidator Group = Validator(Temp, Pressure, Ok);

        private static string Answer(string temp, string pressure, string ok)
            => Json(new Dictionary<string, string>
            {
                { Temp.Id.ToString(), temp }, { Pressure.Id.ToString(), pressure }, { Ok.Id.ToString(), ok }
            });

        // JSON del control: {"<id>":"<valor>",…} (los valores de los tests no llevan comillas).
        private static string Json(Dictionary<string, string> values)
            => "{" + string.Join(",", values.Select(v => "\"" + v.Key + "\":\"" + v.Value + "\"")) + "}";

        private static string Text(StepValidator validator, string answer)
            => validator.FirstDataCollectionError(GroupId, answer)?.Text;

        [Test]
        public void CorrectGroup_HasNoMessage()
        {
            string answer = Answer("20", "3", "true");

            Assert.That(Group.ValidateDataCollectionGroup(GroupId, answer), Is.True);
            Assert.That(Group.FirstDataCollectionError(GroupId, answer), Is.Null);
        }

        // Con dos vacíos, solo el primero de la pantalla.
        [Test]
        public void FirstParameterInScreenOrderWins()
        {
            Assert.That(Text(Group, Answer("", "", "true")),
                Is.EqualTo("Required value \"TEMP\" missing; enter a value in the TEMP field (Message 10015)"));
            Assert.That(Text(Group, Answer("99", "", "true")),
                Is.EqualTo("Value 99 is above the upper limit (Message 15233)"));
            Assert.That(Text(Group, Answer("20", "", "true")),
                Is.EqualTo("Required value \"PRESSURE\" missing; enter a value in the PRESSURE field (Message 10015)"));
        }

        [Test]
        public void MissingKey_IsARequiredValue()
        {
            string answer = Json(new Dictionary<string, string> { { Temp.Id.ToString(), "20" } });

            Assert.That(Group.FirstDataCollectionError(GroupId, answer).Id, Is.EqualTo(MesMessageId.RequiredValueMissing));
        }

        [Test]
        public void TextInANumeric_IsM11()
        {
            Assert.That(Text(Group, Answer("abc", "3", "true")),
                Is.EqualTo("Field TEMP must contain a number (Message 12205)"));
        }

        // El valor va tal cual lo escribió el operador, como el MES.
        [TestCase("99999", "Value 99999 is above the upper limit (Message 15233)")]
        [TestCase("50.5", "Value 50.5 is above the upper limit (Message 15233)")]
        [TestCase("9", "Value 9 is below the lower limit (Message 15232)")]
        [TestCase("-3", "Value -3 is below the lower limit (Message 15232)")]
        public void Limits_ShowTheValueAsWritten(string temp, string expected)
        {
            Assert.That(Text(Group, Answer(temp, "3", "true")), Is.EqualTo(expected));
        }

        // Los límites son inclusivos, como ValidateParamValue.
        [TestCase("10")]
        [TestCase("50")]
        public void LimitValues_HaveNoMessage(string temp)
        {
            Assert.That(Text(Group, Answer(temp, "3", "true")), Is.Null);
        }

        // El Boolean en NO es incorrecto, pero el MES no tiene texto: sin mensaje, y no tapa el
        // de un parámetro siguiente.
        [Test]
        public void InvalidBoolean_HasNoMessage()
        {
            Assert.That(Group.ValidateDataCollectionGroup(GroupId, Answer("20", "3", "false")), Is.False);
            Assert.That(Text(Group, Answer("20", "3", "false")), Is.Null);
        }

        [Test]
        public void EmptyBoolean_IsARequiredValue()
        {
            Assert.That(Group.FirstDataCollectionError(GroupId, Answer("20", "3", "")).Id,
                Is.EqualTo(MesMessageId.RequiredValueMissing));
        }

        // El nombre del grupo Trainer es opcional (spec 015): vacío no da mensaje.
        [Test]
        public void EmptyPersonName_HasNoMessage()
        {
            var validator = Validator(Trainer, Temp);
            string answer = Json(new Dictionary<string, string>
            {
                { Trainer.Id.ToString(), "" }, { Temp.Id.ToString(), "20" }
            });

            Assert.That(validator.ValidateDataCollectionGroup(GroupId, answer), Is.True);
            Assert.That(validator.FirstDataCollectionError(GroupId, answer), Is.Null);
        }

        // D-92: cada respuesta con mensaje sigue siendo un grupo incorrecto, como antes.
        [TestCase("", "3", "true")]
        [TestCase("abc", "3", "true")]
        [TestCase("99", "3", "true")]
        [TestCase("9", "3", "true")]
        [TestCase("20", "", "true")]
        [TestCase("20", "3", "")]
        public void EveryMessage_IsAnIncorrectGroup(string temp, string pressure, string ok)
        {
            string answer = Answer(temp, pressure, ok);

            Assert.That(Group.FirstDataCollectionError(GroupId, answer), Is.Not.Null);
            Assert.That(Group.ValidateDataCollectionGroup(GroupId, answer), Is.False);
        }

        [Test]
        public void MessagesGoToTheWindowStrip()
        {
            Assert.That(Group.FirstDataCollectionError(GroupId, Answer("", "3", "true")).Surface,
                Is.EqualTo(MesSurface.WindowStrip));
        }
    }
}
