using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Reports
{
    // Lo que una guía junta en el "Historial por guía" del detalle del Operador (spec 022, brief 7,
    // alternativa H).
    public class GuideHistory
    {
        public int TrainingGuideId { get; set; }

        // Sus sesiones de cualquier modo y estado.
        public int Sessions { get; set; }

        // Los Assessment que cuentan (ReadinessPolicy.CountsAsAssessment) y cuántos aprobó.
        public int Assessments { get; set; }
        public int PassedAssessments { get; set; }

        // Intentos fallidos de todas sus sesiones (StepRuns, como en la tabla).
        public int FailedAttempts { get; set; }

        // La sesión con el mejor puntaje de Assessment (la más vieja si empatan); null sin
        // Assessment que cuenten.
        public int? BestSessionId { get; set; }

        // Último menos mejor puntaje (de 0 a 1: cero o negativo); null sin Assessment.
        public decimal? LastMinusBest { get; set; }

        // Días completos desde su última actividad en la guía; null sin sesiones.
        public int? DaysSinceActivity { get; set; }
    }

    // El esfuerzo de un operador en el detalle (spec 022, brief 7, alternativa H: "¿Qué ha
    // hecho?" y el historial de cada guía). Métricas derivadas (D-205): solo cuentan las sesiones
    // que GetOperator ya cargó, sin test, como D-235, D-283 y D-297. Qué es aprobado lo sigue
    // decidiendo ReadinessPolicy; los días, como OperatorSummary.MarkActivity.
    public class OperatorActivity
    {
        // Todas sus sesiones, de todas las áreas, y cuántas de cada modo.
        public int Sessions { get; private set; }
        public int Guided { get; private set; }
        public int FreePractice { get; private set; }
        public int Assessment { get; private set; }

        // Inicio de su sesión más reciente y los días desde entonces; null sin sesiones.
        public DateTime? LastSession { get; private set; }
        public int? DaysSinceLastSession { get; private set; }

        // Una por guía mostrada (su área y las otras), en el mismo orden.
        public IList<GuideHistory> Guides { get; private set; }

        // sessions: las filas del detalle (todas las áreas); shown: las guías de su área y las
        // otras con sesiones (OperatorArea), en ese orden.
        public static OperatorActivity For(IList<OperatorSessionRow> sessions, IEnumerable<GuideReadiness> shown, DateTime nowUtc)
        {
            var last = sessions.Count == 0 ? (DateTime?)null : sessions.Max(r => r.Session.StartedAt);
            return new OperatorActivity
            {
                Sessions     = sessions.Count,
                Guided       = sessions.Count(r => r.Session.Mode == SessionMode.Guided),
                FreePractice = sessions.Count(r => r.Session.Mode == SessionMode.FreePractice),
                Assessment   = sessions.Count(r => r.Session.Mode == SessionMode.Assessment),
                LastSession  = last,
                DaysSinceLastSession = DaysSince(last, nowUtc),
                Guides = shown.Select(g => ForGuide(g, sessions.Where(r => r.Session.TrainingGuideId == g.TrainingGuideId).ToList(), nowUtc)).ToList()
            };
        }

        private static GuideHistory ForGuide(GuideReadiness readiness, IList<OperatorSessionRow> rows, DateTime nowUtc)
        {
            var counted = rows.Where(r => ReadinessPolicy.CountsAsAssessment(r.Session)).ToList();
            var best = counted
                .OrderByDescending(r => r.Session.FinalScore)
                .ThenBy(r => r.Session.StartedAt)
                .FirstOrDefault();
            return new GuideHistory
            {
                TrainingGuideId   = readiness.TrainingGuideId,
                Sessions          = rows.Count,
                Assessments       = counted.Count,
                PassedAssessments = counted.Count(r => ReadinessPolicy.IsPassed(r.Session)),
                FailedAttempts    = rows.Sum(r => r.FailedAttempts),
                BestSessionId     = best?.Session.SessionId,
                LastMinusBest     = readiness.BestScore.HasValue && readiness.LastScore.HasValue
                    ? readiness.LastScore.Value - readiness.BestScore.Value : (decimal?)null,
                DaysSinceActivity = DaysSince(readiness.LastActivity, nowUtc)
            };
        }

        private static int? DaysSince(DateTime? utc, DateTime nowUtc)
            => utc.HasValue ? (int)Math.Floor((nowUtc - utc.Value).TotalDays) : (int?)null;
    }
}
