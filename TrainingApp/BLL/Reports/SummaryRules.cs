using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.BLL.Reports
{
    // Una guía en el Resumen (spec 018, AC-9 a AC-11).
    public class GuideSummary
    {
        public int TrainingGuideId { get; set; }

        // Operadores por estado en la guía, con todo el historial (DA-1).
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int InTraining { get; set; }

        // Del período: operadores con algún Assessment completado y, de ellos, los que aprobaron
        // alguno.
        public int AssessedOperators { get; set; }
        public int PassedOperators { get; set; }
        public decimal? PassRate => Rate(PassedOperators, AssessedOperators);

        // El cambio de la tasa de aprobación contra el período anterior, en puntos, con los
        // evaluados de los dos (spec 023); null con Todo. Lo pone el servicio.
        public ReportChange PassRateChange { get; set; }

        // Operadores cuyo primer Assessment completado de la guía (en todo su historial) cae en
        // el período y, de ellos, los que lo aprobaron.
        public int FirstAttemptOperators { get; set; }
        public int FirstAttemptPassed { get; set; }
        public decimal? FirstAttemptRate => Rate(FirstAttemptPassed, FirstAttemptOperators);

        // Promedio de FinalScore de los Assessment completados del período; null sin ninguno.
        public int CompletedAssessments { get; set; }
        public decimal? AverageScore { get; set; }

        // Sin denominador, null ("—", nunca 0 %, AC-11).
        private static decimal? Rate(int part, int total) => total > 0 ? (decimal)part / total : (decimal?)null;
    }

    // Cómo se agrupa la gráfica de actividad, según el largo del período (spec 023, P-9).
    public enum ActivityGrain
    {
        Day   = 0,
        Week  = 1,      // de lunes a domingo
        Month = 2
    }

    // Sesiones de un día, una semana o un mes de la planta, por modo.
    public class ActivityBucket
    {
        // El día, el lunes de la semana o el día 1 del mes, en la hora de la planta.
        public DateTime Start { get; set; }
        public int Guided { get; set; }
        public int Assessment { get; set; }
        public int FreePractice { get; set; }
    }

    // La gráfica de actividad del Resumen.
    public class ActivityChart
    {
        public ActivityGrain Grain { get; set; }

        // En orden, con los vacíos en 0; vacío sin sesiones en el período ("Sin datos").
        public IList<ActivityBucket> Buckets { get; set; }
    }

    // Reglas del Resumen (spec 018 → Domain Model, "Tasa de aprobación"). Funciones puras: lo que
    // es aprobado lo decide ReadinessPolicy. El período filtra por la fecha de inicio de la sesión.
    public static class SummaryRules
    {
        // sessions: las de todos los operadores, con todo el historial (el estado y el primer
        // Assessment lo necesitan); fromUtc y toUtc (excluido): el período, null = sin límite.
        public static GuideSummary ForGuide(int trainingGuideId, IEnumerable<DashboardSession> sessions,
            DateTime? fromUtc, DateTime? toUtc = null)
        {
            var byUser = (sessions ?? Enumerable.Empty<DashboardSession>())
                .Where(s => s.TrainingGuideId == trainingGuideId)
                .GroupBy(s => s.UserId)
                .ToList();
            var summary = new GuideSummary { TrainingGuideId = trainingGuideId };

            foreach (var user in byUser)
            {
                switch (ReadinessPolicy.ForGuide(trainingGuideId, user).State)
                {
                    case ReadinessState.Passed:     summary.Passed++;     break;
                    case ReadinessState.Failed:     summary.Failed++;     break;
                    case ReadinessState.InTraining: summary.InTraining++; break;
                }

                var assessments = user.Where(ReadinessPolicy.CountsAsAssessment).ToList();
                var inPeriod = assessments.Where(s => InPeriod(s, fromUtc, toUtc)).ToList();
                if (inPeriod.Count > 0)
                {
                    summary.AssessedOperators++;
                    if (inPeriod.Any(ReadinessPolicy.IsPassed)) summary.PassedOperators++;
                }

                // El primero por inicio, la misma fecha con la que el período filtra.
                var first = assessments.OrderBy(s => s.StartedAt).ThenBy(s => s.SessionId).FirstOrDefault();
                if (first != null && InPeriod(first, fromUtc, toUtc))
                {
                    summary.FirstAttemptOperators++;
                    if (ReadinessPolicy.IsPassed(first)) summary.FirstAttemptPassed++;
                }
            }

            var scores = byUser.SelectMany(u => u)
                .Where(s => ReadinessPolicy.CountsAsAssessment(s) && InPeriod(s, fromUtc, toUtc))
                .Select(s => s.FinalScore)
                .ToList();
            summary.CompletedAssessments = scores.Count;
            summary.AverageScore = scores.Count > 0 ? scores.Average() : (decimal?)null;
            return summary;
        }

        // El lunes de la semana de una fecha local.
        public static DateTime WeekStart(DateTime local)
            => local.Date.AddDays(-(((int)local.DayOfWeek + 6) % 7));

        // Hasta cuántos días la gráfica va por día y por semana; más largo, por mes (P-9).
        public const int MaxDailyDays  = 31;
        public const int MaxWeeklyDays = 366;

        public static ActivityGrain GrainFor(int days)
            => days <= MaxDailyDays ? ActivityGrain.Day
             : days <= MaxWeeklyDays ? ActivityGrain.Week : ActivityGrain.Month;

        // Sesiones del período por día, semana o mes de la planta y por modo, del inicio del
        // período (o de la primera sesión, con Todo) a su último día (o hoy), con los vacíos en
        // 0. Con Todo, la granularidad sale del largo real desde la primera sesión (D-214).
        public static ActivityChart Activity(IEnumerable<DashboardSession> sessions, ReportRange range, DateTime nowUtc)
        {
            var inPeriod = (sessions ?? Enumerable.Empty<DashboardSession>())
                .Where(s => range.Contains(s.StartedAt))
                .ToList();

            var firstDay = range.FromLocal ?? (inPeriod.Count > 0
                ? PlantTime.ToLocal(inPeriod.Min(s => s.StartedAt)).Date
                : PlantTime.Today(nowUtc));
            var lastDay  = range.ToLocal ?? PlantTime.Today(nowUtc);
            if (inPeriod.Count > 0)
                lastDay = new[] { lastDay, PlantTime.ToLocal(inPeriod.Max(s => s.StartedAt)).Date }.Max();

            var grain = GrainFor(range.Days ?? (lastDay - firstDay).Days + 1);
            var chart = new ActivityChart { Grain = grain, Buckets = new List<ActivityBucket>() };
            if (inPeriod.Count == 0) return chart;

            var byBucket = inPeriod.ToLookup(s => BucketStart(PlantTime.ToLocal(s.StartedAt), grain));
            for (var start = BucketStart(firstDay, grain); start <= lastDay; start = Next(start, grain))
            {
                var these = byBucket[start];
                chart.Buckets.Add(new ActivityBucket
                {
                    Start        = start,
                    Guided       = these.Count(s => s.Mode == SessionMode.Guided),
                    Assessment   = these.Count(s => s.Mode == SessionMode.Assessment),
                    FreePractice = these.Count(s => s.Mode == SessionMode.FreePractice)
                });
            }
            return chart;
        }

        // El día, el lunes o el día 1 de una fecha local.
        private static DateTime BucketStart(DateTime local, ActivityGrain grain)
        {
            switch (grain)
            {
                case ActivityGrain.Week:  return WeekStart(local);
                case ActivityGrain.Month: return new DateTime(local.Year, local.Month, 1);
                default:                  return local.Date;
            }
        }

        private static DateTime Next(DateTime start, ActivityGrain grain)
        {
            switch (grain)
            {
                case ActivityGrain.Week:  return start.AddDays(7);
                case ActivityGrain.Month: return start.AddMonths(1);
                default:                  return start.AddDays(1);
            }
        }

        private static bool InPeriod(DashboardSession session, DateTime? fromUtc, DateTime? toUtc)
            => (!fromUtc.HasValue || session.StartedAt >= fromUtc.Value)
               && (!toUtc.HasValue || session.StartedAt < toUtc.Value);
    }
}
