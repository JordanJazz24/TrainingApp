using System;

namespace TrainingApp.Pages.Training.Models
{
    public interface IStepControl
    {
        StepViewModel ViewModel { get; set; }
        event EventHandler<StepSubmitEventArgs> OnStepSubmit;
        // Called by the host (GuidedTest) after setting ViewModel — runs visual setup
        // even when the control is added during an event handler (after Page_Load).
        void Initialize();
    }
}
