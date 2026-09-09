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
        // Sub-paso activo para secuencias de múltiples modales (ej. Moldeo Start: 1|2|3; 0 = sin sub-paso)
        public int      CurrentSubStep { get; set; }
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
