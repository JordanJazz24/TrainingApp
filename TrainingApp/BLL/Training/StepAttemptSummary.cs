using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Una fila del resumen de Práctica libre (spec 012, P-10): el paso, cuántos intentos tuvo
    // (1 = acertó a la primera) y si se saltó, con los fallos que hubo antes del salto.
    public class StepAttemptSummaryRow
    {
        public int    Order    { get; set; }
        public string StepName { get; set; }
        public int    Attempts { get; set; }
        public bool   Skipped  { get; set; }
        public int    Failures { get; set; }
    }

    // Arma el resumen con los pasos de la guía y los intentos que ya trae el PodContext del
    // cierre (D-86, enmienda 1: sin consulta nueva). Relación situacional: la BLL compone
    // sesión e intentos (CLAUDE.md → "Domain").
    public static class StepAttemptSummary
    {
        public static IList<StepAttemptSummaryRow> Build(IList<TrainingStep> steps,
            IEnumerable<TrainingSessionStepAttempt> attempts)
        {
            var byStep = attempts.ToLookup(a => a.StepId);
            return steps.OrderBy(s => s.Order).Select(s =>
            {
                var own = byStep[s.Id].ToList();
                return new StepAttemptSummaryRow
                {
                    Order    = s.Order,
                    StepName = StepNames.Display(steps, s),
                    Attempts = own.Count,
                    Skipped  = own.Any(a => a.Reason == AttemptReason.Skipped),
                    Failures = own.Count(a => !a.IsCorrect && a.Reason != AttemptReason.Skipped)
                };
            }).ToList();
        }
    }
}
