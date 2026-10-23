using System;

namespace TrainingApp.BLL.Reports
{
    // Período de las vistas del dashboard (spec 018 → Filtros): los últimos 30 días al entrar, o
    // todo. Filtra por la fecha de inicio de la sesión.
    public enum ReportPeriod
    {
        Last30Days = 0,
        All        = 1
    }

    public static class ReportPeriods
    {
        public const int RecentDays = 30;

        // Desde cuándo (UTC) empiezan las sesiones del período; null = todo.
        public static DateTime? FromUtc(ReportPeriod period, DateTime nowUtc)
            => period == ReportPeriod.All ? (DateTime?)null : nowUtc.AddDays(-RecentDays);
    }
}
