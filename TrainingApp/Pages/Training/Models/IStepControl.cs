using System;

namespace TrainingApp.Pages.Training.Models
{
    public interface IStepControl
    {
        StepViewModel ViewModel { get; set; }
        event EventHandler<StepSubmitEventArgs> OnStepSubmit;
        // Único punto de reinicio: el host (GuidedTest) lo llama al abrir el paso, después
        // de asignar ViewModel. Debe dejar el control igual que una instancia nueva — el
        // control es estático y su ViewState sobrevive entre aperturas (spec 006).
        void Initialize();
    }
}
