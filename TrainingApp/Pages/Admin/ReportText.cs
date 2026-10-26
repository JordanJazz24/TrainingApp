using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Admin
{
    // Textos del dashboard (spec 018), iguales en todas sus vistas. Solo presentación: el
    // estado lo decide ReadinessPolicy.
    public static class ReportText
    {
        // Sin dato (sin denominador, sin Assessment, sesión en curso): nunca 0 (AC-11).
        public const string None = "—";

        public static string State(ReadinessState state)
        {
            switch (state)
            {
                case ReadinessState.Passed:     return "Aprobado";
                case ReadinessState.Failed:     return "Reprobado";
                case ReadinessState.InTraining: return "En entrenamiento";
                default:                        return "Sin iniciar";
            }
        }

        // Insignia de estado (brief 019, P-2): el color va siempre con el texto y su ícono.
        public static string StateCss(ReadinessState state)
        {
            switch (state)
            {
                case ReadinessState.Passed:     return "mt-badge mt-badge-ok";
                case ReadinessState.Failed:     return "mt-badge mt-badge-bad";
                case ReadinessState.InTraining: return "mt-badge mt-badge-warn";
                default:                        return "mt-badge mt-badge-idle";
            }
        }

        public static string StateIcon(ReadinessState state)
        {
            switch (state)
            {
                case ReadinessState.Passed:     return "bi bi-check-circle";
                case ReadinessState.Failed:     return "bi bi-x-circle";
                case ReadinessState.InTraining: return "bi bi-hourglass-split";
                default:                        return "bi bi-dash-circle";
            }
        }

        // Puntaje de 0 a 1 como en el resumen final del operador ("85.0%").
        public static string Score(decimal? score)
            => Percent(score);

        // Una tasa de 0 a 1 ("45.0%"); "—" sin denominador (AC-11).
        public static string Percent(decimal? rate)
            => rate.HasValue ? (rate.Value * 100m).ToString("F1") + "%" : None;

        // El período de una vista, para decir a qué corresponde cada bloque.
        public static string Period(ReportPeriod period)
            => period == ReportPeriod.All ? "Todo el historial" : "Últimos " + ReportPeriods.RecentDays + " días";

        // Fecha UTC de la BD en la hora de la planta (DA-8).
        public static string Date(DateTime? utc)
            => utc.HasValue ? SessionHelper.ToLocal(utc.Value).ToString("dd/MM/yyyy HH:mm") : None;

        // Los nombres de modo de la pantalla de inicio del operador.
        public static string Mode(SessionMode mode)
        {
            switch (mode)
            {
                case SessionMode.Assessment:   return "Assessment";
                case SessionMode.FreePractice: return "Práctica libre";
                default:                       return "Guided";
            }
        }

        public static string Status(SessionStatus status)
        {
            switch (status)
            {
                case SessionStatus.Completed: return "Completada";
                case SessionStatus.Abandoned: return "Abandonada";
                default:                      return "En curso";
            }
        }

        // Motivo de un intento fallido (AttemptReason, spec 013).
        public static string Reason(AttemptReason? reason)
        {
            switch (reason)
            {
                case AttemptReason.OutOfSequence:      return "Fuera de secuencia";
                case AttemptReason.WrongDecision:      return "Decisión incorrecta";
                case AttemptReason.InvalidAction:      return "Acción inválida";
                case AttemptReason.MissingSelection:   return "Falta selección";
                case AttemptReason.EmptyRequiredField: return "Campo vacío";
                case AttemptReason.Skipped:            return "Saltado";
                default:                               return None;
            }
        }

        // Un motivo con su acción y cuántas veces se registró (Pasos y su Excel). La acción va
        // con su código, como se registra (AttemptActions).
        public static string ReasonCount(ReasonCount reason)
            => Reason(reason.Reason) + " · " + (reason.Action ?? None) + " (" + reason.Count + ")";

        // El dato de la curva (AC-15): "4 sesiones de práctica antes del primer aprobado".
        public static string PracticeBeforeFirstPass(int? sessions)
        {
            if (!sessions.HasValue) return "Todavía no aprueba esta guía.";
            return sessions.Value == 1
                ? "1 sesión de práctica antes del primer aprobado"
                : sessions.Value + " sesiones de práctica antes del primer aprobado";
        }

        // Cómo terminó un paso de una sesión (hoja Intentos por paso).
        public static string Outcome(StepOutcome outcome)
        {
            switch (outcome)
            {
                case StepOutcome.Correct:        return "Acierto";
                case StepOutcome.WithoutSuccess: return "Sin acierto";
                case StepOutcome.Skipped:        return "Saltado";
                default:                         return "Sin resolver";
            }
        }

        // Una línea por regla de Seguimiento; S-3, una por paso (página y Excel).
        public static IEnumerable<string> FollowUpLines(FollowUpReason reason)
        {
            switch (reason.Rule)
            {
                case FollowUpRule.RepeatedFailures:
                    return new[] { string.Format("S-1 · Reprobó {0} Assessment sin aprobar ninguno", reason.FailedAssessments) };
                case FollowUpRule.Inactive:
                    return new[] { string.Format("S-2 · {0} días sin sesiones de esta guía", reason.DaysInactive) };
                case FollowUpRule.StuckStep:
                    return reason.Steps.Select(s => string.Format("S-3 · {0}: sin acierto en {1} Assessment", s.StepName, s.Times));
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason.Rule, "Regla de seguimiento sin texto.");
            }
        }

        // "45 s", "25 min 13 s", "1 h 05 min"; "—" sin duración (sesión en curso).
        public static string Duration(TimeSpan? duration)
        {
            if (!duration.HasValue) return None;
            var d = duration.Value;
            if (d.TotalHours >= 1) return string.Format("{0} h {1:00} min", (int)d.TotalHours, d.Minutes);
            if (d.TotalMinutes >= 1) return string.Format("{0} min {1:00} s", d.Minutes, d.Seconds);
            return d.Seconds + " s";
        }
    }
}
