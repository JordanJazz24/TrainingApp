using System;

namespace TrainingApp.Pages.Training.Models
{
    public interface IStepControl
    {
        StepViewModel ViewModel { get; set; }
        event EventHandler<StepSubmitEventArgs> OnStepSubmit;
    }
}
