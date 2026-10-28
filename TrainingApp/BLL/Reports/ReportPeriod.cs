using System;
using TrainingApp.Shared;

namespace TrainingApp.BLL.Reports
{
    // Período de Pasos y del Resumen (spec 023, P-3): cinco atajos, Todo y Personalizado. Al
    // entrar, Este mes (D-215), que también es el valor por defecto. Filtra por la fecha de inicio
    // de la sesión (P-1).
    public enum ReportPeriod
    {
        ThisMonth  = 0,     // día 1 a hoy
        Today      = 1,
        ThisWeek   = 2,     // lunes a hoy
        PrevMonth  = 3,
        Last30Days = 4,     // hoy y los 29 días anteriores
        All        = 5,
        Custom     = 6      // Inicio y Fin
    }

    // Un período ya resuelto: sus límites en UTC (el inicio incluido, el fin excluido, así
    // ninguna sesión cae en dos períodos) y en días de la planta, y el período anterior con el
    // que se compara (P-4). Con Todo, sin límites ni anterior.
    public sealed class ReportRange
    {
        internal ReportRange(ReportPeriod period, DateTime? fromLocal, DateTime? toLocal,
            DateTime? prevFromLocal, DateTime? prevToLocal, bool invalid)
        {
            Period        = period;
            FromLocal     = fromLocal;
            ToLocal       = toLocal;
            PrevFromLocal = prevFromLocal;
            PrevToLocal   = prevToLocal;
            Invalid       = invalid;
            FromUtc       = StartUtc(fromLocal);
            ToUtc         = EndUtc(toLocal);
            PrevFromUtc   = StartUtc(prevFromLocal);
            PrevToUtc     = EndUtc(prevToLocal);
        }

        public ReportPeriod Period { get; }

        // Se pidió un período inválido (P-6): vale Este mes y la página muestra el aviso.
        public bool Invalid { get; }

        // Primer y último día del período, en la planta (los dos incluidos); null con Todo.
        public DateTime? FromLocal { get; }
        public DateTime? ToLocal { get; }

        // Los mismos límites en UTC: desde la medianoche de FromLocal hasta la del día después
        // de ToLocal (excluida; P-2).
        public DateTime? FromUtc { get; }
        public DateTime? ToUtc { get; }

        // El período anterior (P-4), igual; null con Todo.
        public DateTime? PrevFromLocal { get; }
        public DateTime? PrevToLocal { get; }
        public DateTime? PrevFromUtc { get; }
        public DateTime? PrevToUtc { get; }

        // Largo en días; null con Todo (la gráfica usa el largo real, SummaryRules).
        public int? Days => FromLocal.HasValue && ToLocal.HasValue
            ? (ToLocal.Value - FromLocal.Value).Days + 1 : (int?)null;

        public bool Compares => PrevFromUtc.HasValue;

        // Desde dónde leer para tener el período y el anterior en una sola lectura (P-8).
        public DateTime? ReadFromUtc => PrevFromUtc ?? FromUtc;

        public bool Contains(DateTime startedAtUtc) => Between(startedAtUtc, FromUtc, ToUtc);

        public bool InPrevious(DateTime startedAtUtc)
            => Compares && Between(startedAtUtc, PrevFromUtc, PrevToUtc);

        private static bool Between(DateTime utc, DateTime? fromUtc, DateTime? toUtc)
            => (!fromUtc.HasValue || utc >= fromUtc.Value) && (!toUtc.HasValue || utc < toUtc.Value);

        private static DateTime? StartUtc(DateTime? local)
            => local.HasValue ? PlantTime.FromLocal(local.Value) : (DateTime?)null;

        private static DateTime? EndUtc(DateTime? local)
            => local.HasValue ? PlantTime.FromLocal(local.Value.AddDays(1)) : (DateTime?)null;
    }

    public static class ReportPeriods
    {
        // El de inicio (D-215) y el que vale un período inválido (P-6).
        public const ReportPeriod Default = ReportPeriod.ThisMonth;

        // Inicio más antiguo que se acepta: la app no tiene datos anteriores, y una fecha absurda
        // de la URL (año 1) desbordaría el período anterior.
        public static readonly DateTime MinCustomFrom = new DateTime(2000, 1, 1);

        // Resuelve el período con el "hoy" de la planta. from y to (días de la planta) solo
        // cuentan con Personalizado; ahí son obligatorios, Fin ≥ Inicio, Fin ≤ hoy e Inicio desde
        // MinCustomFrom. Lo que no cumple vale Este mes con Invalid: lo que llega de la URL puede
        // venir manipulado.
        public static ReportRange Resolve(ReportPeriod period, DateTime? from, DateTime? to, DateTime nowUtc)
        {
            var today = PlantTime.Today(nowUtc);

            switch (period)
            {
                case ReportPeriod.Today:
                    return Range(period, today, today, today.AddDays(-1), today.AddDays(-1));

                case ReportPeriod.ThisWeek:
                {
                    var monday = SummaryRules.WeekStart(today);
                    return SameDaysBefore(period, monday, today, monday.AddDays(-7));
                }

                case ReportPeriod.ThisMonth:
                    return ThisMonth(today, invalid: false);

                case ReportPeriod.PrevMonth:
                {
                    var first = FirstOfMonth(today).AddMonths(-1);
                    var prev  = first.AddMonths(-1);
                    return Range(period, first, first.AddMonths(1).AddDays(-1), prev, first.AddDays(-1));
                }

                case ReportPeriod.Last30Days:
                    return Range(period, today.AddDays(-29), today, today.AddDays(-59), today.AddDays(-30));

                case ReportPeriod.All:
                    return new ReportRange(period, null, null, null, null, invalid: false);

                case ReportPeriod.Custom:
                    if (!from.HasValue || !to.HasValue || from.Value.Date < MinCustomFrom
                        || to.Value.Date < from.Value.Date || to.Value.Date > today)
                        return ThisMonth(today, invalid: true);
                    var start = from.Value.Date;
                    var end   = to.Value.Date;
                    return SameDaysBefore(period, start, end, start.AddDays(-((end - start).Days + 1)));

                default:
                    return ThisMonth(today, invalid: true);
            }
        }

        // Del día 1 a hoy, contra los mismos días del mes anterior; si es más corto, hasta su
        // último día (1–31 oct. vs. 1–30 sep.).
        private static ReportRange ThisMonth(DateTime today, bool invalid)
        {
            var first   = FirstOfMonth(today);
            var prev    = first.AddMonths(-1);
            var prevEnd = prev.AddDays(today.Day - 1);
            if (prevEnd >= first) prevEnd = first.AddDays(-1);
            return new ReportRange(ReportPeriod.ThisMonth, first, today, prev, prevEnd, invalid);
        }

        // Los mismos días, empezando en prevStart.
        private static ReportRange SameDaysBefore(ReportPeriod period, DateTime start, DateTime end, DateTime prevStart)
            => Range(period, start, end, prevStart, prevStart.AddDays((end - start).Days));

        private static ReportRange Range(ReportPeriod period, DateTime from, DateTime to, DateTime prevFrom, DateTime prevTo)
            => new ReportRange(period, from, to, prevFrom, prevTo, invalid: false);

        private static DateTime FirstOfMonth(DateTime day) => new DateTime(day.Year, day.Month, 1);
    }
}
