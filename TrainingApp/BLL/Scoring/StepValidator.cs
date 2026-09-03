using TrainingApp.Domain;

namespace TrainingApp.BLL.Scoring
{
    public class StepValidator
    {
        // Validates whether the operator's submitted answer for a step is correct.
        // Each StepType has its own validation rule — see CLAUDE.md "Flujo estándar".
        //
        // submittedAnswer is a string serialized by the page code-behind:
        //   LineClearance:    "true" or "false"
        //   Assembly:         scanned component count as string (e.g. "3")
        //   DataCollection:   JSON of parameter key-value pairs
        //   LogNC:            selected NC code string
        //   ResourceSetup,
        //   Search, Complete: sequence-only steps — answer is always correct if the
        //                     operator reached this step; pass "ok"
        public bool Validate(TrainingStep step, string submittedAnswer)
        {
            switch (step.StepType)
            {
                case StepType.ResourceSetup:
                case StepType.Search:
                case StepType.Complete:
                    return true; // sequence-only steps; reaching them is the validation

                case StepType.LineClearance:
                    return submittedAnswer == "true";

                case StepType.Assembly:
                case StepType.DataCollection:
                case StepType.LogNC:
                    throw new System.NotImplementedException(
                        $"Validation for {step.StepType} requires catalog data — implement in a future iteration.");

                default:
                    return false;
            }
        }
    }
}
