using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Scoring
{
    public class StepValidator
    {
        private readonly StepValidationContext _ctx;

        public StepValidator(StepValidationContext context)
        {
            _ctx = context;
        }

        // submittedAnswer formats by StepType:
        //   ResourceSetup                   : JSON {"setupState":"...","rows":[...]} — correct only with
        //                                     setupState "Set Up" and no preloaded equipment left in
        //                                     the rows (spec 015); the rest of the rows/Shop Order is
        //                                     format, validated by the control
        //   Search                          : áreas con RequiresStartChecklist: JSON de SearchControl — correcto
        //                                     solo si las respuestas LC Moldeo (verif*) y la verificación
        //                                     final son true. Sin checklist (Start directo, spec 008):
        //                                     solo secuencia, cualquier Answer es correcto.
        //   Complete                        : "complete"      (any other value = incorrect, e.g. no row selected — spec 007)
        //   Logout                          : "logout"        (último paso: el clic en el ícono Logout, spec 015)
        //   LineClearance                   : "true" | "false"
        //   Assembly                        : no se valida por Answer: el paso se completa en
        //                                     PodService.AssembleComponent al quedar
        //                                     ensamblado todo el BOM (spec 008)
        //   DataCollection                  : no se valida por Answer: se califica por DC Group en
        //                                     ValidateDataCollectionGroup (spec 008)
        //   SFC_Split                       : "split-completed"
        //   LogNC                           : NC code string  (e.g. "F_FS_MANCHAS"); a value outside the
        //                                     catalog is incorrect (wrong button / invalid row — spec 007)
        public bool Validate(TrainingStep step, string submittedAnswer)
        {
            switch (step.StepType)
            {
                case StepType.ResourceSetup:
                    return ValidateResourceSetup(submittedAnswer);

                case StepType.Search:
                    // Sin contexto de área se asume el checklist (comportamiento de Moldeo).
                    if (_ctx.Area != null && !_ctx.Area.RequiresStartChecklist)
                        return true;
                    return ValidateSearch(submittedAnswer);

                case StepType.Complete:
                    return submittedAnswer == "complete";

                case StepType.Logout:
                    return submittedAnswer == "logout";

                case StepType.LineClearance:
                    return submittedAnswer == "true";

                case StepType.Assembly:
                case StepType.DataCollection:
                    return false;

                case StepType.SFC_Split:
                    return submittedAnswer == "split-completed";

                case StepType.LogNC:
                    return ValidateLogNC(submittedAnswer);

                default:
                    return false;
            }
        }

        private static readonly string[] SearchRequiredConfirmations =
            { "verifCalibracion", "verifD0128682", "verifD0128035", "verificacionFinal" };

        private bool ValidateResourceSetup(string submittedAnswer)
        {
            var answer = TryParseObject(submittedAnswer);
            object setupState;
            return answer != null
                && answer.TryGetValue("setupState", out setupState)
                && setupState as string == "Set Up"
                && !HasPreloadedValue(submittedAnswer);
        }

        // Spec 015: alguna fila del Save de Resource Setup trae un equipo precargado (por valor,
        // sin espacios, con cualquier tipo de fila). Es una decisión equivocada, no formato.
        public bool HasPreloadedValue(string submittedAnswer)
        {
            var preloaded = _ctx.PreloadedValues;
            if (preloaded == null || preloaded.Count == 0) return false;

            ResourceSetupAnswer answer;
            try
            {
                answer = string.IsNullOrWhiteSpace(submittedAnswer)
                    ? null : JsonConvert.DeserializeObject<ResourceSetupAnswer>(submittedAnswer);
            }
            catch (JsonException)
            {
                return false;
            }
            if (answer?.Rows == null) return false;

            return answer.Rows.Any(row =>
            {
                string value;
                return row != null && row.TryGetValue("value", out value)
                    && PreloadedEquipmentRule.IsPreloaded(preloaded, value);
            });
        }

        private sealed class ResourceSetupAnswer
        {
            [JsonProperty("rows")]
            public List<Dictionary<string, string>> Rows { get; set; }
        }

        private static bool ValidateSearch(string submittedAnswer)
        {
            var answer = TryParseObject(submittedAnswer);
            if (answer == null) return false;

            foreach (var key in SearchRequiredConfirmations)
            {
                object value;
                if (!answer.TryGetValue(key, out value) || !(value is bool) || !(bool)value)
                    return false;
            }
            return true;
        }

        private static Dictionary<string, object> TryParseObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // Save de un DC Group (spec 008): el Answer tiene que traer todos los parámetros del
        // grupo, solo esos, y con valores válidos. Un campo vacío es intento fallido (regla de
        // docs/domain.md para Data Collection), salvo el nombre del grupo Trainer (spec 015).
        public bool ValidateDataCollectionGroup(int groupId, string submittedAnswer)
        {
            if (string.IsNullOrWhiteSpace(submittedAnswer))
                return false;

            Dictionary<string, string> submitted;
            try
            {
                submitted = JsonConvert.DeserializeObject<Dictionary<string, string>>(submittedAnswer);
            }
            catch (JsonException)
            {
                return false;
            }
            if (submitted == null || submitted.Count == 0)
                return false;

            var groupParams = _ctx.DataCollectionParams
                .Where(p => p.DataCollectionGroupId == groupId)
                .ToList();
            if (groupParams.Count == 0 || submitted.Count != groupParams.Count)
                return false;

            foreach (var param in groupParams)
            {
                string value;
                if (!submitted.TryGetValue(param.Id.ToString(CultureInfo.InvariantCulture), out value))
                    return false;
                if (!ValidateParamValue(param, value))
                    return false;
            }
            return true;
        }

        // Registro de actividad (spec 013): un Save de DC incorrecto por un valor vacío o que
        // falta (EmptyRequiredField) y no por un valor fuera de rango (WrongDecision).
        public bool HasMissingDataCollectionValue(int groupId, string submittedAnswer)
        {
            Dictionary<string, string> submitted = null;
            if (!string.IsNullOrWhiteSpace(submittedAnswer))
            {
                try
                {
                    submitted = JsonConvert.DeserializeObject<Dictionary<string, string>>(submittedAnswer);
                }
                catch (JsonException)
                {
                    return false;
                }
            }

            // El nombre del grupo Trainer vacío no falta: es opcional (spec 015).
            return _ctx.DataCollectionParams
                .Where(p => p.DataCollectionGroupId == groupId && p.DataType != PersonNameRule.DataType)
                .Any(p =>
                {
                    string value;
                    return submitted == null
                        || !submitted.TryGetValue(p.Id.ToString(CultureInfo.InvariantCulture), out value)
                        || string.IsNullOrWhiteSpace(value);
                });
        }

        // Spec 015: un nombre del grupo Trainer que no cumple PersonNameRule. El control lo bloquea
        // como formato, así que si llega es una manipulación: no se califica.
        public bool HasInvalidPersonName(int groupId, string submittedAnswer)
            => PersonNameRule.HasInvalid(
                _ctx.DataCollectionParams.Where(p => p.DataCollectionGroupId == groupId), submittedAnswer);

        private static bool ValidateParamValue(DataCollectionParameter param, string value)
        {
            // El nombre del grupo Trainer es opcional (spec 015): vacío es válido.
            if (param.DataType == PersonNameRule.DataType)
                return PersonNameRule.IsValid(value);

            if (string.IsNullOrWhiteSpace(value))
                return false;

            switch (param.DataType)
            {
                case "Boolean":
                    return value == "true";

                case "Date":
                    DateTime dt;
                    return DateTime.TryParse(value, out dt);

                case "Numeric":
                    decimal numeric;
                    if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out numeric))
                        return false;
                    if (param.MinValue.HasValue && numeric < param.MinValue.Value)
                        return false;
                    if (param.MaxValue.HasValue && numeric > param.MaxValue.Value)
                        return false;
                    return true;

                default:
                    return false;
            }
        }

        private bool ValidateLogNC(string submittedAnswer)
        {
            if (string.IsNullOrWhiteSpace(submittedAnswer))
                return false;

            return _ctx.ValidNCCodes.Any(nc =>
                string.Equals(nc.Code, submittedAnswer, StringComparison.OrdinalIgnoreCase));
        }
    }
}
