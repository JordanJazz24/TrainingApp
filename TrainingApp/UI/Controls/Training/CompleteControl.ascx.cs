using System;
using System.Web.UI;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class CompleteControl : UserControl
    {
        public StepViewModel ViewModel { get; set; }
        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        public void Initialize() { }  // No visual setup — Complete has no modal
    }
}
