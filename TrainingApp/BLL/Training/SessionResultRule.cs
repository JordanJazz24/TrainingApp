using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Reports;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Cómo terminó un paso de la sesión, para el operador (spec 025, P-3).
    public enum StepResultOutcome
    {
        AtFirst,              // acertó sin ningún fallo
        WithFailures,         // acertó después de N fallos
        WithoutSuccess,       // terminó sin acierto (avance forzado de Assessment)
        Skipped,              // se saltó sin fallos (Práctica libre)
        SkippedWithFailures   // se saltó después de N fallos
    }

    // Cuántos fallos de un motivo tuvo un paso (solo el tipo y el conteo, nunca el detalle de
    // la decisión: P-6).
    public class FailureReasonCount
    {
        public AttemptReason Reason { get; set; }
        public int Count { get; set; }
    }

    // Un renglón del fin de sesión: un paso de la guía.
    public class StepResultRow
    {
        public int Order { get; set; }
        public string StepName { get; set; }

        // Intentos del paso, sin contar el salto de Práctica libre.
        public int Attempts { get; set; }

        // Intentos fallidos (la misma regla que Pasos: StepRuns.IsFailure).
        public int Failures { get; set; }

        // Los motivos de los fallos que lo tienen registrado, por motivo (los intentos
        // anteriores a la spec 013 no lo traen).
        public IList<FailureReasonCount> Reasons { get; set; } = new List<FailureReasonCount>();

        public StepResultOutcome Outcome { get; set; }

        // Puntos como fracción del puntaje (0 a 1, como FinalScore); null si el modo no muestra
        // puntaje (Práctica libre).
        public decimal? PointsEarned { get; set; }
        public decimal? PointsPossible { get; set; }
        public decimal? PointsLost => PointsPossible.HasValue && PointsEarned.HasValue
            ? PointsPossible.Value - PointsEarned.Value : (decimal?)null;

        // Qué reforzar (P-7): con algún fallo o sin acierto. Un saltado sin fallos no entra.
        public bool NeedsReinforcement => Failures > 0 || Outcome == StepResultOutcome.WithoutSuccess;
    }

    // El detalle de una sesión completada: resumen, todos los pasos y los que reforzar.
    public class SessionResultDetail
    {
        public int SessionId { get; set; }
        public SessionMode Mode { get; set; }
        public ModePolicy Policy { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // De inicio a cierre (P-12).
        public TimeSpan? Duration { get; set; }

        // El puntaje final guardado (0 a 1); null donde el modo no lo muestra (D-90, D-334).
        public decimal? Score { get; set; }

        // Aprobado o reprobado contra el mínimo; null donde el modo no da veredicto (solo
        // Assessment, Policy.Summary == ScoreAndResult).
        public bool? Passed { get; set; }
        public decimal PassingScore => ScoreCalculator.PassingScore;

        // Todos los pasos de la guía, en orden, y los que hay que reforzar (subconjunto).
        public IList<StepResultRow> Steps { get; set; } = new List<StepResultRow>();
        public IList<StepResultRow> Reinforce { get; set; } = new List<StepResultRow>();

        // "N de M a la primera" (Práctica libre).
        public int AtFirstCount { get; set; }
        public int StepCount { get; set; }

        // Salió bien (P-8): Assessment aprobado; en los demás, ni fallos ni saltos.
        public bool WentWell { get; set; }
    }

    // Reglas del fin de sesión (spec 025, P-3, P-6, P-7, P-13), puras y sin repositorios para
    // probarlas con datos en memoria. No repite la fórmula del puntaje ni la definición de
    // fallo: usa ScoreCalculator (puntos por paso) y StepRuns (fallo, sin acierto, saltado), las
    // mismas del cierre de la sesión y de Pasos. Reemplaza a StepAttemptSummary.
    public static class SessionResultRule
    {
        // session: la sesión (completada); steps: los pasos de su guía con su Weight; attempts: los
        // intentos de la sesión.
        public static SessionResultDetail Build(TrainingSession session, IList<TrainingStep> steps,
            IEnumerable<TrainingSessionStepAttempt> attempts)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (steps == null) throw new ArgumentNullException(nameof(steps));

            var policy  = ModePolicy.For(session.Mode);
            var ordered = steps.OrderBy(s => s.Order).ToList();
            var own     = (attempts ?? Enumerable.Empty<TrainingSessionStepAttempt>())
                .Where(a => a.SessionId == session.Id).ToList();
            var byStep  = own.ToLookup(a => a.StepId);

            // StepRuns trabaja con los intentos del dashboard: el mismo dato, con el orden del paso.
            var orderOf = ordered.ToDictionary(s => s.Id, s => s.Order);
            var runs = StepRuns.ForSession(
                new DashboardSession
                {
                    SessionId       = session.Id,
                    UserId          = session.UserId,
                    TrainingGuideId = session.TrainingGuideId,
                    Mode            = session.Mode,
                    Status          = session.Status,
                    FinalScore      = session.FinalScore,
                    StartedAt       = session.StartedAt,
                    CompletedAt     = session.CompletedAt
                },
                own.Where(a => orderOf.ContainsKey(a.StepId)).Select(a => new DashboardAttempt
                {
                    SessionId     = a.SessionId,
                    StepId        = a.StepId,
                    StepOrder     = orderOf[a.StepId],
                    AttemptNumber = a.AttemptNumber,
                    IsCorrect     = a.IsCorrect,
                    Timestamp     = a.Timestamp,
                    Reason        = a.Reason,
                    Action        = a.Action
                }).ToList())
                .ToDictionary(r => r.StepId);

            var calculator = new ScoreCalculator();
            var rows = new List<StepResultRow>();
            foreach (var step in ordered)
            {
                runs.TryGetValue(step.Id, out StepRun run);
                int failures = run?.FailedAttempts ?? 0;
                var row = new StepResultRow
                {
                    Order    = step.Order,
                    StepName = StepNames.Display(ordered, step),
                    Attempts = byStep[step.Id].Count(a => a.Reason != AttemptReason.Skipped),
                    Failures = failures,
                    Reasons  = run == null
                        ? new List<FailureReasonCount>()
                        : run.FailureReasons.GroupBy(r => r).OrderBy(g => (int)g.Key)
                            .Select(g => new FailureReasonCount { Reason = g.Key, Count = g.Count() }).ToList(),
                    Outcome  = OutcomeOf(run)
                };
                if (policy.ShowsScore)
                {
                    // El mismo cálculo del cierre, paso por paso: la suma da el FinalScore guardado.
                    row.PointsEarned   = calculator.CalculateSessionScore(new[] { step }, byStep[step.Id].ToList(), session.Mode);
                    row.PointsPossible = step.Weight;
                }
                rows.Add(row);
            }

            bool? passed = policy.Summary == SummaryKind.ScoreAndResult
                ? ScoreCalculator.IsPassed(session.FinalScore) : (bool?)null;
            return new SessionResultDetail
            {
                SessionId    = session.Id,
                Mode         = session.Mode,
                Policy       = policy,
                StartedAt    = session.StartedAt,
                CompletedAt  = session.CompletedAt,
                Duration     = session.CompletedAt.HasValue ? session.CompletedAt.Value - session.StartedAt : (TimeSpan?)null,
                Score        = policy.ShowsScore ? session.FinalScore : (decimal?)null,
                Passed       = passed,
                Steps        = rows,
                Reinforce    = rows.Where(r => r.NeedsReinforcement).ToList(),
                AtFirstCount = rows.Count(r => r.Outcome == StepResultOutcome.AtFirst),
                StepCount    = rows.Count,
                WentWell     = passed ?? rows.All(r => r.Outcome == StepResultOutcome.AtFirst)
            };
        }

        // Un paso sin ningún intento (guía cambiada después, D-56) no cuenta como fallo.
        private static StepResultOutcome OutcomeOf(StepRun run)
        {
            if (run == null) return StepResultOutcome.Skipped;
            if (run.Skipped)
                return run.FailedAttempts > 0 ? StepResultOutcome.SkippedWithFailures : StepResultOutcome.Skipped;
            if (run.HasCorrectAttempt)
                return run.FailedAttempts > 0 ? StepResultOutcome.WithFailures : StepResultOutcome.AtFirst;
            return StepResultOutcome.WithoutSuccess;
        }
    }
}
