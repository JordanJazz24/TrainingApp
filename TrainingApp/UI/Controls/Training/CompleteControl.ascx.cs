using System;
using System.Web.UI;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class CompleteControl : UserControl
    {
        public StepViewModel ViewModel { get; set; }
        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        // Complete step has no modal — submit is triggered directly from BtnComplete_ServerClick
        // in GuidedTest.aspx.cs without loading this control into the DOM.
    }
}
