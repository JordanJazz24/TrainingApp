namespace TrainingApp.Pages.Admin
{
    // Filas del Resumen (spec 018, AC-9; diseño de la Fase 11), con los textos ya armados. Solo
    // presentación: los números son los de GuideSummary y StepStats.
    public class SummaryGuideRow
    {
        public string GuideName { get; set; }
        public string GuideTitle { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int InTraining { get; set; }
        public string TotalText { get; set; }

        // Anchos de la barra apilada en % del total de la guía (cultura invariante: van al CSS).
        public string PassedWidth { get; set; }
        public string FailedWidth { get; set; }
        public string InTrainingWidth { get; set; }
        public string StatesAria { get; set; }

        public string PassRateText { get; set; }
        public string PassRateDetail { get; set; }

        // El cambio de la tasa de aprobación contra el período anterior (spec 023).
        public ChangeView PassRateChange { get; set; }
        public string FirstAttemptText { get; set; }
        public string FirstAttemptDetail { get; set; }
        public string AverageText { get; set; }
    }

    public class SummaryTopRow
    {
        public string StepName { get; set; }
        public string FailureRateText { get; set; }
        public string FailureWidth { get; set; }
        public string Detail { get; set; }

        // El cambio del % con fallo contra el período anterior (spec 023).
        public ChangeView FailureRateChange { get; set; }
    }
}
