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

    // Sesiones de una semana de la planta (de lunes a domingo), por modo.
    public class WeekActivity
    {
        // El lunes de la semana, en la hora de la planta.
        public DateTime WeekStart { get; set; }
        public int Guided { get; set; }
        public int Assessment { get; set; }
        public int FreePractice { get; set; }
    }

    // Reglas del Resumen (spec 018 → Domain Model, "Tasa de aprobación"). Funciones puras: lo que
    // es aprobado lo decide ReadinessPolicy. El período filtra por la fecha de inicio de la sesión.
    public static class SummaryRules
    {
        // sessions: las de todos los operadores, con todo el historial (el estado y el primer
        // Assessment lo necesitan); fromUtc: inicio del período, null = todo.
        public static GuideSummary ForGuide(int trainingGuideId, IEnumerable<DashboardSession> sessions, DateTime? fromUtc)
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
                var inPeriod = assessments.Where(s => InPeriod(s, fromUtc)).ToList();
                if (inPeriod.Count > 0)
                {
                    summary.AssessedOperators++;
                    if (inPeriod.Any(ReadinessPolicy.IsPassed)) summary.PassedOperators++;
                }

                // El primero por inicio, la misma fecha con la que el período filtra.
                var first = assessments.OrderBy(s => s.StartedAt).ThenBy(s => s.SessionId).FirstOrDefault();
                if (first != null && InPeriod(first, fromUtc))
                {
                    summary.FirstAttemptOperators++;
                    if (ReadinessPolicy.IsPassed(first)) summary.FirstAttemptPassed++;
                }
            }

            var scores = byUser.SelectMany(u => u)
                .Where(s => ReadinessPolicy.CountsAsAssessment(s) && InPeriod(s, fromUtc))
                .Select(s => s.FinalScore)
                .ToList();
            summary.CompletedAssessments = scores.Count;
            summary.AverageScore = scores.Count > 0 ? scores.Average() : (decimal?)null;
            return summary;
        }

        // El lunes de la semana de una fecha local.
        public static DateTime WeekStart(DateTime local)
            => local.Date.AddDays(-(((int)local.DayOfWeek + 6) % 7));

        // Sesiones por semana de la planta y por modo, de la semana de inicio del período (o de
        // la primera sesión, con todo) hasta la de nowUtc, con las semanas vacías en 0. Sin
        // sesiones en el período, vacío ("Sin datos").
        public static IList<WeekActivity> Weeks(IEnumerable<DashboardSession> sessions, DateTime? fromUtc, DateTime nowUtc)
        {
            var inPeriod = (sessions ?? Enumerable.Empty<DashboardSession>())
                .Where(s => InPeriod(s, fromUtc))
                .ToList();
            if (inPeriod.Count == 0) return new List<WeekActivity>();

            var byWeek = inPeriod.ToLookup(s => WeekStart(PlantTime.ToLocal(s.StartedAt)));
            var first = WeekStart(PlantTime.ToLocal(fromUtc ?? inPeriod.Min(s => s.StartedAt)));
            var last  = WeekStart(PlantTime.ToLocal(nowUtc));
            if (byWeek.Any()) last = new[] { last, byWeek.Max(g => g.Key) }.Max();

            var weeks = new List<WeekActivity>();
            for (var week = first; week <= last; week = week.AddDays(7))
            {
                var these = byWeek[week];
                weeks.Add(new WeekActivity
                {
                    WeekStart    = week,
                    Guided       = these.Count(s => s.Mode == SessionMode.Guided),
                    Assessment   = these.Count(s => s.Mode == SessionMode.Assessment),
                    FreePractice = these.Count(s => s.Mode == SessionMode.FreePractice)
                });
            }
            return weeks;
        }

        private static bool InPeriod(DashboardSession session, DateTime? fromUtc)
            => !fromUtc.HasValue || session.StartedAt >= fromUtc.Value;
    }
}
