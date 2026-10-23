namespace TrainingApp.Pages.Admin
{
    // Filas del Resumen (spec 018, AC-9), con los textos ya armados. Solo presentación.
    public class SummaryGuideRow
    {
        public string GuideName { get; set; }
        public string GuideTitle { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int InTraining { get; set; }
        public int Assessed { get; set; }
        public string PassRateText { get; set; }
        public string FirstAttemptText { get; set; }
        public string AverageText { get; set; }
    }

    public class SummaryTopRow
    {
        public string StepName { get; set; }
        public int Sessions { get; set; }
        public string FailureRateText { get; set; }
    }
}
