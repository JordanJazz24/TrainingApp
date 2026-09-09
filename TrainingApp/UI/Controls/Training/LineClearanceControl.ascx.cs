using System;
using System.Web.UI;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class LineClearanceControl : UserControl, TrainingApp.Pages.Training.Models.IStepControl
    {
        public StepViewModel ViewModel { get; set; }

        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && ViewModel != null)
            {
                LitOperation.Text     = Page.Server.HtmlEncode(ViewModel.ExtraData.ContainsKey("OperationName")
                    ? ViewModel.ExtraData["OperationName"]?.ToString() : "");
                LitResource.Text      = Page.Server.HtmlEncode(ViewModel.ExtraData.ContainsKey("ResourceName")
                    ? ViewModel.ExtraData["ResourceName"]?.ToString() : "");
                LitResourceEntry.Text = LitResource.Text;

                string question = ViewModel.ExtraData.ContainsKey("QuestionText")
                    ? ViewModel.ExtraData["QuestionText"]?.ToString()
                    : "¿La limpieza de linea se ejecuto?";
                LitQuestion.Text = Page.Server.HtmlEncode(question);
            }
        }

        protected void BtnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string answer = DdlAnswer.SelectedValue;
            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = answer });
        }
    }
}
