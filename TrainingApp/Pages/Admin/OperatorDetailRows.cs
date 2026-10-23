using System.Collections.Generic;

namespace TrainingApp.Pages.Admin
{
    // Filas del detalle del Operador (spec 018, AC-14), con los textos ya armados. Solo
    // presentación.
    public class OperatorGuideCard
    {
        public string GuideName { get; set; }
        public string GuideTitle { get; set; }
        public string StateText { get; set; }
        public string StateCss { get; set; }
        public string BestText { get; set; }
        public string LastText { get; set; }
        public string LastActivityText { get; set; }
    }

    public class OperatorSessionListRow
    {
        public string StartedText { get; set; }
        public string GuideName { get; set; }
        public string ModeText { get; set; }
        public string StatusText { get; set; }
        public string ScoreText { get; set; }
        public string DurationText { get; set; }
        public int FailedAttempts { get; set; }
    }

    public class OperatorReinforcementGroup
    {
        public string GuideName { get; set; }
        public IList<OperatorReinforcementRow> Steps { get; set; }
    }

    public class OperatorCurveCard
    {
        public string GuideName { get; set; }
        public string GuideTitle { get; set; }
        public bool HasPoints { get; set; }
        public string PracticeText { get; set; }
        public string ScoresText { get; set; }
    }

    public class OperatorReinforcementRow
    {
        public string StepName { get; set; }
        public int TimesWithoutSuccess { get; set; }
        public int FailedAttempts { get; set; }
        public string MainReasonText { get; set; }
    }
}
