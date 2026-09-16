using System.Collections.Generic;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Models
{
    public class StepViewModel
    {
        public int      StepId         { get; set; }
        public StepType StepType       { get; set; }
        public int      StepOrder      { get; set; }
        public string   Hint           { get; set; }
        public bool?    IsCorrect      { get; set; }
        public int      AttemptCount   { get; set; }
        public Dictionary<string, object> ExtraData { get; set; }

        public StepViewModel()
        {
            ExtraData = new Dictionary<string, object>();
        }
    }

    public class StepSubmitEventArgs : System.EventArgs
    {
        public string Answer { get; set; }
    }
}
