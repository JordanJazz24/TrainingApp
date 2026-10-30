using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Reports
{
    // Una sesión en la curva de aprendizaje.
    public class CurvePoint
    {
        public int SessionId { get; set; }
        public DateTime StartedAt { get; set; }
        public SessionMode Mode { get; set; }

        // Intentos fallidos de la sesión, sin los saltos (StepRuns).
        public int FailedAttempts { get; set; }

        // El puntaje si se muestra (completada y de un modo que lo muestra, D-90); si no, null.
        public decimal? Score { get; set; }

        // Un Assessment completado con el puntaje mínimo (ReadinessPolicy.IsPassed).
        public bool Passed { get; set; }
    }

    // La curva de aprendizaje de un operador en una guía (spec 018, AC-15).
    public class GuideCurve
    {
        public int TrainingGuideId { get; set; }

        // Sus sesiones de Práctica libre y Assessment, de la más vieja a la más nueva.
        public IList<CurvePoint> Points { get; set; }

        // Sesiones de práctica (Guided y Práctica libre) antes de su primer Assessment aprobado;
        // null si todavía no aprueba la guía.
        public int? PracticeBeforeFirstPass { get; set; }
    }

    // Curva de aprendizaje (spec 018 → Domain Model, AC-15). Función pura: lo que es aprobado lo
    // decide ReadinessPolicy. Guided no entra en la gráfica (la guía evita los errores, casi
    // siempre mostraría 0), pero sí cuenta como práctica antes del primer aprobado.
    public static class LearningCurve
    {
        // sessions: las del operador en esa guía (todo el historial); failedAttempts y score: los
        // de cada sesión, como en el detalle (StepRuns y el puntaje visible).
        public static GuideCurve ForGuide(int trainingGuideId, IEnumerable<DashboardSession> sessions,
            Func<DashboardSession, int> failedAttempts, Func<DashboardSession, decimal?> score)
        {
            var ordered = (sessions ?? Enumerable.Empty<DashboardSession>())
                .Where(s => s.TrainingGuideId == trainingGuideId)
                .OrderBy(s => s.StartedAt).ThenBy(s => s.SessionId)
                .ToList();

            // El primer aprobado por inicio, la misma fecha que el Resumen usa para el primer
            // Assessment (E4).
            var firstPass = ordered.FirstOrDefault(ReadinessPolicy.IsPassed);

            return new GuideCurve
            {
                TrainingGuideId = trainingGuideId,
                Points = ordered
                    .Where(s => s.Mode == SessionMode.FreePractice || s.Mode == SessionMode.Assessment)
                    .Select(s => new CurvePoint
                    {
                        SessionId      = s.SessionId,
                        StartedAt      = s.StartedAt,
                        Mode           = s.Mode,
                        FailedAttempts = failedAttempts(s),
                        Score          = score(s),
                        Passed         = ReadinessPolicy.IsPassed(s)
                    })
                    .ToList(),
                PracticeBeforeFirstPass = firstPass == null ? (int?)null
                    : ordered.TakeWhile(s => s != firstPass)
                             .Count(s => s.Mode == SessionMode.Guided || s.Mode == SessionMode.FreePractice)
            };
        }
    }
}
