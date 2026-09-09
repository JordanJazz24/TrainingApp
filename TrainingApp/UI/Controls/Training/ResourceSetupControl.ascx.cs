using System;
using System.Web.UI;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class ResourceSetupControl : UserControl, TrainingApp.Pages.Training.Models.IStepControl
    {
        public StepViewModel ViewModel { get; set; }

        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && ViewModel != null)
            {
                LitSite.Text     = Page.Server.HtmlEncode(
                    ViewModel.ExtraData.ContainsKey("Site") ? ViewModel.ExtraData["Site"]?.ToString() : "CR9A");
                LitResource.Text = Page.Server.HtmlEncode(
                    ViewModel.ExtraData.ContainsKey("ResourceName") ? ViewModel.ExtraData["ResourceName"]?.ToString() : "");
            }
        }

        protected void BtnSave_Click(object sender, EventArgs e)
        {
            string setupState = HfSetupState.Value;
            string rowsJson   = HfRows.Value;

            // Build the answer JSON: { "setupState": "...", "rows": [...] }
            string answer = string.Format("{{\"setupState\":\"{0}\",\"rows\":{1}}}",
                setupState?.Replace("\"", "\\\"") ?? "",
                string.IsNullOrEmpty(rowsJson) ? "[]" : rowsJson);

            PnlRecordChange.Visible = true;

            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = answer });
        }

        protected void BtnClear_Click(object sender, EventArgs e)
        {
            HfRows.Value       = "[]";
            HfSetupState.Value = "Open";
        }
    }
}
