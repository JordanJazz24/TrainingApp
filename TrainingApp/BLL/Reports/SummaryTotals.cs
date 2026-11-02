using System;
using System.Collections.Generic;
using System.Linq;

namespace TrainingApp.BLL.Reports
{
    // Los totales de las guías mostradas en el Resumen (spec 022, E8b, D-252): el "46 de 90
    // aprobados" y la aprobación global del período con su cambio. Solo suma lo que GetSummary ya
    // carga (GuideSummary); no hay consulta nueva. Los operadores se cuentan POR GUÍA: quien
    // entrena en dos guías suma dos veces, y así lo dice la pantalla. Métrica derivada de
    // presentación, sin test (como D-235, D-283, D-293, D-297, D-304 y D-318).
    public sealed class SummaryTotals
    {
        // Operadores por estado con todo el historial, sumados por guía.
        public int Passed { get; private set; }
        public int Failed { get; private set; }
        public int InTraining { get; private set; }
        public int Total => Passed + Failed + InTraining;

        // Del período, sumados por guía: evaluados y, de ellos, los que aprobaron alguno.
        public int AssessedOperators { get; private set; }
        public int PassedOperators { get; private set; }

        // De 0 a 1; null sin evaluados ("—", nunca 0 %, AC-11).
        public decimal? PassRate => AssessedOperators > 0 ? (decimal)PassedOperators / AssessedOperators : (decimal?)null;

        // El cambio de la aprobación global contra el período anterior, en puntos; null con Todo
        // (spec 023), como el de cada guía.
        public ReportChange PassRateChange { get; private set; }

        // compares: si el período se compara con el anterior (ReportRange.Compares).
        public static SummaryTotals Of(IEnumerable<GuideSummary> guides, bool compares)
        {
            var list = (guides ?? Enumerable.Empty<GuideSummary>()).ToList();
            var totals = new SummaryTotals
            {
                Passed            = list.Sum(g => g.Passed),
                Failed            = list.Sum(g => g.Failed),
                InTraining        = list.Sum(g => g.InTraining),
                AssessedOperators = list.Sum(g => g.AssessedOperators),
                PassedOperators   = list.Sum(g => g.PassedOperators)
            };
            if (!compares) return totals;

            // El anterior = Σ(tasa anterior × evaluados anteriores) / Σ(evaluados anteriores), de
            // las guías con comparación: los aprobados anteriores son enteros (se redondea la
            // multiplicación de la tasa por su base).
            var before = list.Where(g => g.PassRateChange != null && g.PassRateChange.Previous.HasValue).ToList();
            int previousBase = before.Sum(g => g.PassRateChange.PreviousBase);
            decimal? previous = previousBase > 0
                ? before.Sum(g => Math.Round(g.PassRateChange.Previous.Value * g.PassRateChange.PreviousBase)) / previousBase
                : (decimal?)null;
            totals.PassRateChange = ReportChange.Points(totals.PassRate, totals.AssessedOperators,
                previous, previousBase, higherIsBetter: true);
            return totals;
        }
    }
}
