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
        //   Search                          : JSON from SearchControl — correct only if the LC Moldeo answers
        //                                     (verif*) and the final verification are all true
        //   Complete                        : "complete"      (any other value = incorrect, e.g. no row selected — spec 007)
        //   LineClearance                   : "true" | "false"
        //   Assembly                        : integer string  (e.g. "3")
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
                    return ValidateSearch(submittedAnswer);

                case StepType.Complete:
                    return submittedAnswer == "complete";

                case StepType.LineClearance:
                    return submittedAnswer == "true";

                case StepType.Assembly:
                    return ValidateAssembly(submittedAnswer);

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

        private bool ValidateAssembly(string submittedAnswer)
        {
            if (_ctx.ExpectedComponentCount == null)
                throw new InvalidOperationException(
                    "ExpectedComponentCount is required to validate an Assembly step.");

            int submitted;
            if (!int.TryParse(submittedAnswer, out submitted))
                return false;

            return submitted == _ctx.ExpectedComponentCount.Value;
        }

        private bool ValidateDataCollection(string submittedAnswer)
        {
            Dictionary<string, string> submitted;
            try
            {
                submitted = JsonConvert.DeserializeObject<Dictionary<string, string>>(submittedAnswer);
            }
            catch
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
