using System;
using System.Web.UI;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    // Declarado en GuidedTest por completitud del contrato (spec 006). Todavía sin UI:
    // BtnComplete hace el submit directo con Answer "complete".
    public partial class CompleteControl : UserControl, IStepControl
    {
        public StepViewModel ViewModel { get; set; }
        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        public void Initialize() { }
    }
}
