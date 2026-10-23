using System.Collections.Generic;

namespace TrainingApp.Pages.Admin
{
    // Filas de Pasos (spec 018, AC-17 y AC-18), con los textos ya armados. Solo presentación.
    public class StepGuideGroup
    {
        public string GuideName { get; set; }
        public string GuideTitle { get; set; }
        public IList<StepListRow> Rows { get; set; }
    }

    public class StepListRow
    {
        public string StepName { get; set; }
        public int Sessions { get; set; }
        public string FailureRateText { get; set; }
        public int FailedAttempts { get; set; }
        public int TimesWithoutSuccess { get; set; }
        public int Skips { get; set; }
        public string MedianText { get; set; }

        // "Motivo · acción (veces)", del más repetido al menos.
        public IList<string> Reasons { get; set; }
    }
}
