using System.Collections.Generic;
using Newtonsoft.Json;

namespace TrainingApp.BLL.Training
{
    // Respuesta de Log NC que arma LogNCControl (spec 014, D-59): un JSON con el código y las
    // decisiones de cada método. Las filas seleccionadas llegan aparte. Único lugar que lee
    // el contrato: el validador, el registro de actividad y el control.
    public sealed class LogNCAnswer
    {
        // removeAction de Remove component (reparación rápida).
        public const string RemoveScrap         = "scrap";
        public const string RemoveReturn        = "return";
        public const string RemoveSendToRouting = "sendToRouting";

        public string Code         { get; private set; }
        public string Component    { get; private set; }
        public string RemoveAction { get; private set; }
        public bool   AddComponent { get; private set; }

        public static bool IsRemoveAction(string value)
            => value == RemoveScrap || value == RemoveReturn || value == RemoveSendToRouting;

        // null si no es un JSON (ej. StepAnswers.Invalid de un botón equivocado).
        public static LogNCAnswer Parse(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return null;
            Dictionary<string, object> values;
            try
            {
                values = JsonConvert.DeserializeObject<Dictionary<string, object>>(answer);
            }
            catch (JsonException)
            {
                return null;
            }
            if (values == null) return null;

            object value;
            return new LogNCAnswer
            {
                Code         = (values.TryGetValue("code", out value) ? value as string : null)?.Trim(),
                Component    = (values.TryGetValue("component", out value) ? value as string : null)?.Trim(),
                RemoveAction = values.TryGetValue("removeAction", out value) ? value as string : null,
                AddComponent = values.TryGetValue("addComponent", out value) && value is bool && (bool)value
            };
        }

        public static string Serialize(string code, string component, string removeAction = null, bool? addComponent = null)
            => JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                ["code"]         = code,
                ["component"]    = component,
                ["removeAction"] = removeAction,
                ["addComponent"] = addComponent
            }, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
    }
}
