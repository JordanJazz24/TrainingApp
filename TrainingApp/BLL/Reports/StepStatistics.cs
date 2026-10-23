using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Reports
{
    // Veces que se registró un motivo con una acción en los intentos fallidos de un paso.
    public class ReasonCount
    {
        // null en un intento sin motivo registrado.
        public AttemptReason? Reason { get; set; }
        public string Action { get; set; }
        public int Count { get; set; }
    }

    // Un paso en el ranking de Pasos (spec 018, AC-17 y AC-18). Con todas las guías, el renglón
    // junta el mismo paso de cada guía: StepId es el de una de ellas y StepOrder el menor.
    public class StepStats
    {
        public int StepId { get; set; }
        public int StepOrder { get; set; }
        public string StepName { get; set; }

        // Sesiones con algún intento en el paso y, de ellas, las que tienen algún intento fallido
        // (los saltos no son fallos).
        public int Sessions { get; set; }
        public int SessionsWithFailure { get; set; }

        // % de sesiones con fallo, de 0 a 1; null sin sesiones ("—", nunca 0).
        public decimal? FailureRate => Sessions > 0 ? (decimal)SessionsWithFailure / Sessions : (decimal?)null;

        public int FailedAttempts { get; set; }
        public int TimesWithoutSuccess { get; set; }
        public int Skips { get; set; }

        // Mediana de la duración del paso (DA-2), de las veces que se resolvió sin saltarlo; null
        // si no hay ninguna ("—").
        public TimeSpan? MedianDuration { get; set; }

        // De la más repetida a la menos.
        public IList<ReasonCount> Reasons { get; set; }
    }

    // Ranking de Pasos (spec 018): agrega los StepRun de las sesiones de una guía, siempre con
    // la regla de StepRuns (D-146). Funciones puras.
    public static class StepStatistics
    {
        // Los pasos de una guía con algún intento, por % de sesiones con fallo, después por
        // intentos fallidos y después en el orden de la guía. Sin nombre: lo pone el servicio.
        public static IList<StepStats> Rank(IEnumerable<IList<StepRun>> sessionRuns)
            => Build(Flatten(sessionRuns).GroupBy(r => r.StepId), null);

        // Todas las guías (usuario, 2026-10-23): el mismo paso de cada guía (mismo nombre de
        // StepNames, p. ej. "Line Clearance (apertura)") en un solo renglón. Los conteos y la
        // mediana salen de todas sus sesiones juntas, no de promediar guías.
        public static IList<StepStats> RankByName(IEnumerable<IList<StepRun>> sessionRuns, Func<StepRun, string> nameOf)
            => Build(Flatten(sessionRuns).GroupBy(nameOf), nameOf);

        private static IEnumerable<StepRun> Flatten(IEnumerable<IList<StepRun>> sessionRuns)
            => (sessionRuns ?? Enumerable.Empty<IList<StepRun>>()).SelectMany(r => r);

        // nameOf: el nombre del renglón; null = lo pone el servicio.
        private static IList<StepStats> Build<TKey>(IEnumerable<IGrouping<TKey, StepRun>> groups, Func<StepRun, string> nameOf)
        {
            return groups
                .Select(g => new StepStats
                {
                    StepId              = g.First().StepId,
                    StepOrder           = g.Min(r => r.StepOrder),
                    StepName            = nameOf?.Invoke(g.First()),
                    Sessions            = g.Count(),
                    SessionsWithFailure = g.Count(r => r.FailedAttempts > 0),
                    FailedAttempts      = g.Sum(r => r.FailedAttempts),
                    TimesWithoutSuccess = g.Count(r => r.EndedWithoutSuccess),
                    Skips               = g.Count(r => r.Skipped),
                    // Un salto o el paso a medias de una sesión en curso o abandonada no
                    // terminaron el paso: su duración bajaría la mediana.
                    MedianDuration      = Median(g.Where(r => r.Resolved && !r.Skipped).Select(r => r.Duration)),
                    Reasons             = g.SelectMany(r => r.Failures ?? Enumerable.Empty<DashboardAttempt>())
                        .GroupBy(a => new { a.Reason, a.Action })
                        .Select(x => new ReasonCount { Reason = x.Key.Reason, Action = x.Key.Action, Count = x.Count() })
                        .OrderByDescending(x => x.Count).ThenBy(x => x.Reason).ThenBy(x => x.Action, StringComparer.Ordinal)
                        .ToList()
                })
                .OrderByDescending(s => s.FailureRate)
                .ThenByDescending(s => s.FailedAttempts)
                .ThenBy(s => s.StepOrder)
                .ToList();
        }

        // La mediana: con un número par de valores, el promedio de los dos del medio. Sin
        // valores, null.
        public static TimeSpan? Median(IEnumerable<TimeSpan> values)
        {
            var sorted = (values ?? Enumerable.Empty<TimeSpan>()).OrderBy(v => v).ToList();
            if (sorted.Count == 0) return null;
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 1
                ? sorted[mid]
                : TimeSpan.FromTicks((sorted[mid - 1].Ticks + sorted[mid].Ticks) / 2);
        }
    }
}
