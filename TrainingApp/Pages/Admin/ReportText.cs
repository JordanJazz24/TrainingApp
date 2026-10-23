using System;
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

        public static string StateCss(ReadinessState state)
        {
            switch (state)
            {
                case ReadinessState.Passed:     return "badge bg-success";
                case ReadinessState.Failed:     return "badge bg-danger";
                case ReadinessState.InTraining: return "badge bg-warning text-dark";
                default:                        return "badge bg-light text-secondary border";
            }
        }

        // Puntaje de 0 a 1 como en el resumen final del operador ("85.0%").
        public static string Score(decimal? score)
            => score.HasValue ? (score.Value * 100m).ToString("F1") + "%" : None;

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
