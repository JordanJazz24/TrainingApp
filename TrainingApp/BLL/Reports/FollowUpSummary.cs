using System.Collections.Generic;
using System.Linq;

namespace TrainingApp.BLL.Reports
{
    // El resumen de la cola de Seguimiento (spec 022, brief 5: alternativa B con el encabezado P1).
    // Métricas derivadas (D-205): solo cuentan las filas que la vista ya carga, sin test, como
    // D-235 y D-283. No cambian qué reglas aplican (FollowUpRules) ni el orden de las filas.
    public class FollowUpSummary
    {
        // "Llamar primero": los casos (operador y guía) que cumplen 2 reglas o más; el resto va
        // en "Después".
        public const int CallFirstMinRules = 2;

        public int Operators { get; private set; }
        public int Cases { get; private set; }
        public int CallFirst { get; private set; }
        public int Later { get; private set; }

        // Casos que cumplen cada regla; un caso puede contar en varias.
        public int RepeatedFailures { get; private set; }   // S-1
        public int Inactive { get; private set; }           // S-2
        public int StuckStep { get; private set; }          // S-3

        // Cada FollowUpReason es una regla distinta (S-3 junta todos sus pasos en una).
        public static bool IsCallFirst(FollowUpRow row) => row.Reasons.Count >= CallFirstMinRules;

        public static FollowUpSummary For(IList<FollowUpRow> rows)
        {
            rows = rows ?? new List<FollowUpRow>();
            int callFirst = rows.Count(IsCallFirst);
            return new FollowUpSummary
            {
                Operators        = rows.Select(r => r.UserId).Distinct().Count(),
                Cases            = rows.Count,
                CallFirst        = callFirst,
                Later            = rows.Count - callFirst,
                RepeatedFailures = rows.Count(r => Has(r, FollowUpRule.RepeatedFailures)),
                Inactive         = rows.Count(r => Has(r, FollowUpRule.Inactive)),
                StuckStep        = rows.Count(r => Has(r, FollowUpRule.StuckStep))
            };
        }

        private static bool Has(FollowUpRow row, FollowUpRule rule) => row.Reasons.Any(x => x.Rule == rule);
    }
}
