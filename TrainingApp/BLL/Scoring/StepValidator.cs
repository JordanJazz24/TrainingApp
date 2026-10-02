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
        //                                     setupState "Set Up" (rows/Shop Order are format, validated by the control)
        //   Search                          : áreas con RequiresStartChecklist: JSON de SearchControl — correcto
        //                                     solo si las respuestas LC Moldeo (verif*) y la verificación
        //                                     final son true. Sin checklist (Start directo, spec 008):
        //                                     solo secuencia, cualquier Answer es correcto.
        //   Complete                        : "complete"      (any other value = incorrect, e.g. no row selected — spec 007)
        //   LineClearance                   : "true" | "false"
        //   Assembly                        : no se valida por Answer: el paso se completa en
        //                                     TrainingSessionService.AssembleComponent al quedar
        //                                     ensamblado todo el BOM (spec 008)
        //   DataCollection                  : JSON {"<paramId>":"<value>", ...}
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

                case StepType.LineClearance:
                    return submittedAnswer == "true";

                case StepType.Assembly:
                    return false;

                case StepType.DataCollection:
                    return ValidateDataCollection(submittedAnswer);

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

        private static bool ValidateResourceSetup(string submittedAnswer)
        {
            var answer = TryParseObject(submittedAnswer);
            object setupState;
            return answer != null
                && answer.TryGetValue("setupState", out setupState)
                && setupState as string == "Set Up";
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
        // grupo, solo esos, y con valores válidos. Un campo vacío es intento fallido (regla
        // de CLAUDE.md para Data Collection).
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

        private bool ValidateDataCollection(string submittedAnswer)
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

            foreach (var kvp in submitted)
            {
                int paramId;
                if (!int.TryParse(kvp.Key, out paramId))
                    return false;

                var param = _ctx.DataCollectionParams.FirstOrDefault(p => p.Id == paramId);
                if (param == null)
                    return false;

                if (!ValidateParamValue(param, kvp.Value))
                    return false;
            }

            return true;
        }

        private static bool ValidateParamValue(DataCollectionParameter param, string value)
        {
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
