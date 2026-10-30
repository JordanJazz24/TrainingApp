using System;
using System.Collections.Generic;
using System.Linq;

namespace TrainingApp.BLL.Reports
{
    // El resumen del encabezado de Operadores (spec 022, brief 6: pestañas por estado con la barra
    // proporcional). Métricas derivadas (D-205): solo cuentan a los operadores que la consulta ya
    // cargó, sin test, como D-235 y D-283. El estado de cada uno lo sigue decidiendo OperatorArea.
    public class OperatorSummary
    {
        // "Sin actividad reciente": los mismos días que S-2 de Seguimiento.
        public const int InactiveDays = FollowUpRules.InactiveDays;

        // Con la búsqueda y el área, sin el filtro de estado: cuántos hay en cada pestaña. Con
        // "Sin área" elegida, en todas las áreas.
        public int Passed { get; private set; }
        public int Failed { get; private set; }
        public int InTraining { get; private set; }
        public int NotStarted { get; private set; }

        // "Todos": los mismos operadores que cuentan arriba, también los Sin área y los de un
        // área sin guía publicada.
        public int All { get; private set; }

        // Con la búsqueda, en cualquier área.
        public int WithoutArea { get; private set; }

        // De la lista ya filtrada (todas las páginas).
        public int Inactive { get; private set; }

        public int CountOf(ReadinessState state)
        {
            switch (state)
            {
                case ReadinessState.Passed:     return Passed;
                case ReadinessState.Failed:     return Failed;
                case ReadinessState.InTraining: return InTraining;
                default:                        return NotStarted;
            }
        }

        // byState: los operadores de las pestañas; withoutArea: los Sin área con la búsqueda;
        // listed: la lista con todos los filtros.
        public static OperatorSummary For(IList<OperatorRow> byState, int withoutArea, IList<OperatorRow> listed)
        {
            Func<ReadinessState, int> count = st => byState.Count(r => OperatorArea.Matches(r.AreaStatus, r.AreaGuides, st));
            return new OperatorSummary
            {
                Passed      = count(ReadinessState.Passed),
                Failed      = count(ReadinessState.Failed),
                InTraining  = count(ReadinessState.InTraining),
                NotStarted  = count(ReadinessState.NotStarted),
                All         = byState.Count,
                WithoutArea = withoutArea,
                Inactive    = listed.Count(r => r.Inactive)
            };
        }

        // Días desde su última actividad y si lleva InactiveDays o más: solo quien tiene guía en
        // su área y ya empezó (Sin área y Sin iniciar tienen su propia pestaña).
        public static void MarkActivity(OperatorRow row, DateTime nowUtc)
        {
            row.DaysSinceActivity = row.LastActivity.HasValue
                ? (int)Math.Floor((nowUtc - row.LastActivity.Value).TotalDays)
                : (int?)null;
            row.Inactive = row.DaysSinceActivity >= InactiveDays
                && row.AreaStatus == OperatorAreaStatus.HasGuides
                && !row.NotStarted;
        }
    }
}
