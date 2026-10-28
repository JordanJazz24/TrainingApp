using System;

namespace TrainingApp.BLL.Reports
{
    public enum ChangeDirection
    {
        Same = 0,
        Up   = 1,
        Down = 2
    }

    // Cómo se mide el cambio: en puntos (tasas) o en % (conteos).
    public enum ChangeUnit
    {
        Points  = 0,
        Percent = 1
    }

    // El cambio de un número contra el período anterior (spec 023, P-4, P-5 y P-16), con la base
    // de los dos (evaluados o sesiones). Sin datos en el anterior, sin cambio: "sin datos para
    // comparar". Con Todo no hay ReportChange (null).
    public sealed class ReportChange
    {
        private ReportChange(ChangeUnit unit, decimal? current, decimal? previous,
            int currentBase, int previousBase, decimal? change, bool higherIsBetter)
        {
            Unit         = unit;
            Current      = current;
            Previous     = previous;
            CurrentBase  = currentBase;
            PreviousBase = previousBase;
            Change       = change;
            Direction    = !change.HasValue || change.Value == 0m ? ChangeDirection.Same
                         : change.Value > 0m ? ChangeDirection.Up : ChangeDirection.Down;
            // Una tasa de aprobación o las sesiones mejoran al subir; un % con fallo, al bajar.
            IsImprovement = Direction == ChangeDirection.Same ? (bool?)null
                          : (Direction == ChangeDirection.Up) == higherIsBetter;
        }

        public ChangeUnit Unit { get; }

        // Tasas de 0 a 1 o conteos; null sin denominador.
        public decimal? Current { get; }
        public decimal? Previous { get; }

        // Evaluados o sesiones de cada período.
        public int CurrentBase { get; }
        public int PreviousBase { get; }

        // En puntos o en %, con un decimal (como se muestran las tasas); null sin datos para
        // comparar.
        public decimal? Change { get; }

        public bool HasComparison => Change.HasValue;
        public ChangeDirection Direction { get; }

        // null si no cambió o no hay con qué comparar.
        public bool? IsImprovement { get; }

        // Una tasa (de 0 a 1) con su denominador en cada período; el cambio en puntos.
        public static ReportChange Points(decimal? current, int currentBase, decimal? previous, int previousBase,
            bool higherIsBetter)
        {
            decimal? change = current.HasValue && previous.HasValue
                ? Round((current.Value - previous.Value) * 100m) : (decimal?)null;
            return new ReportChange(ChangeUnit.Points, current, previous, currentBase, previousBase, change, higherIsBetter);
        }

        // Un conteo (sus sesiones son su base); el cambio en %. Sin nada en el anterior no hay %.
        public static ReportChange Percent(int current, int previous, bool higherIsBetter)
        {
            decimal? change = previous > 0 ? Round((current - previous) * 100m / previous) : (decimal?)null;
            return new ReportChange(ChangeUnit.Percent, current, previous, current, previous, change, higherIsBetter);
        }

        private static decimal Round(decimal value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
    }
}
