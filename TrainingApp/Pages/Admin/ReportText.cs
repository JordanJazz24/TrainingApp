using System;
using TrainingApp.BLL.Reports;

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
    }
}
