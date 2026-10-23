using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Reports
{
    // Cómo le fue a una sesión en uno de sus pasos (spec 018).
    public class StepRun
    {
        public int StepId { get; set; }
        public int StepOrder { get; set; }

        // Intentos fallidos, sin contar el salto (StepRuns.IsFailure).
        public int FailedAttempts { get; set; }

        // Motivos de los intentos fallidos, uno por intento.
        public IList<AttemptReason> FailureReasons { get; set; }

        public bool HasCorrectAttempt { get; set; }
        public bool Skipped { get; set; }

        // El paso quedó atrás: la sesión se completó, tiene intentos en un paso posterior o el
        // modo forzó el avance (ModePolicy.ForcesAdvanceAt), aunque después la abandonara.
        public bool Resolved { get; set; }

        // Terminó sin acierto (spec 018): avance forzado en Assessment, o saltado con fallos en
        // Práctica libre. Un salto sin fallos no cuenta: saltar nunca es un fallo.
        public bool EndedWithoutSuccess => Resolved && !HasCorrectAttempt && (!Skipped || FailedAttempts > 0);

        // Hora de su último intento y duración (DA-2).
        public DateTime LastAttempt { get; set; }
        public TimeSpan Duration { get; set; }
    }

    // Los pasos de una sesión a partir de sus intentos (spec 018). Funciones puras: las usan el
    // detalle del Operador (E2), Pasos (E3) y las descargas, siempre con la misma regla.
    public static class StepRuns
    {
        // Un intento fallido: incorrecto y que no sea el salto de Práctica libre (que se cuenta
        // aparte y nunca como fallo).
        public static bool IsFailure(DashboardAttempt attempt)
            => !attempt.IsCorrect && attempt.Reason != AttemptReason.Skipped;

        // Un StepRun por paso con intentos, en orden. La duración de un paso va desde el último
        // intento del paso anterior (o desde el inicio de la sesión en el primero) hasta su
        // último intento (DA-2).
        public static IList<StepRun> ForSession(DashboardSession session, IEnumerable<DashboardAttempt> attempts)
        {
            var steps = (attempts ?? Enumerable.Empty<DashboardAttempt>())
                .Where(a => a.SessionId == session.SessionId)
                .GroupBy(a => new { a.StepOrder, a.StepId })
                .OrderBy(g => g.Key.StepOrder)
                .ToList();

            var policy = ModePolicy.For(session.Mode);
            var runs = new List<StepRun>();
            var previous = session.StartedAt;
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                var last = step.Max(a => a.Timestamp);
                var failures = step.Where(IsFailure).ToList();
                runs.Add(new StepRun
                {
                    StepId            = step.Key.StepId,
                    StepOrder         = step.Key.StepOrder,
                    FailedAttempts    = failures.Count,
                    FailureReasons    = failures.Where(a => a.Reason.HasValue).Select(a => a.Reason.Value).ToList(),
                    HasCorrectAttempt = step.Any(a => a.IsCorrect),
                    Skipped           = step.Any(a => a.Reason == AttemptReason.Skipped),
                    Resolved          = session.Status == SessionStatus.Completed || i < steps.Count - 1
                                        || policy.ForcesAdvanceAt(failures.Count),
                    LastAttempt       = last,
                    Duration          = last > previous ? last - previous : TimeSpan.Zero
                });
                previous = last;
            }
            return runs;
        }

        // Duración de la sesión: de inicio a cierre; null si sigue en curso (o no tiene cierre).
        public static TimeSpan? SessionDuration(DashboardSession session)
            => session.Status == SessionStatus.InProgress || !session.CompletedAt.HasValue
                ? (TimeSpan?)null
                : session.CompletedAt.Value - session.StartedAt;
    }
}
