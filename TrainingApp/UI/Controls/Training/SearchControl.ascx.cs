using System;
using System.Collections.Generic;
using System.Web.UI;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class SearchControl : UserControl, TrainingApp.Pages.Training.Models.IStepControl
    {
        public StepViewModel ViewModel { get; set; }

        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (ViewModel == null || ViewModel.CurrentSubStep == 0) return;

            PnlSubStep.Visible = true;

            if (!IsPostBack)
                SetupSubStep(ViewModel.CurrentSubStep);
        }

        private void SetupSubStep(int subStep)
        {
            LitResource.Text = Page.Server.HtmlEncode(
                ViewModel.ExtraData.ContainsKey("ResourceName")
                    ? ViewModel.ExtraData["ResourceName"]?.ToString() : "");

            switch (subStep)
            {
                case 1:
                    LitDCGroup.Text    = "DCGTM_ID_MOLDE/A";
                    LitParam1Label.Text = Page.Server.HtmlEncode(
                        GetExtraString("Param1Label", "ID del molde"));
                    PnlSubStep1.Visible = true;
                    break;

                case 2:
                    LitDCGroup.Text = "DCGTM_LC_MOLDEO/A";
                    var questions   = GetExtraList("LCQuestions");
                    RptBoolQuestions.DataSource = questions;
                    RptBoolQuestions.DataBind();
                    PnlSubStep2.Visible = true;
                    break;

                case 3:
                    LitDCGroup.Text    = "DCGTM_LC_MOLDEO/A";
                    LitParam3Label.Text = Page.Server.HtmlEncode(
                        GetExtraString("Param3Label", "¿Se verificó el IDMold & IDIMM según MSS?"));
                    PnlSubStep3.Visible = true;
                    break;
            }
        }

        protected void BtnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string answer;
            switch (ViewModel.CurrentSubStep)
            {
                case 1:
                    answer = TxtParam1.Text.Trim();
                    break;
                case 2:
                    answer = HfBoolAnswers.Value;
                    break;
                case 3:
                    answer = DdlParam3.SelectedValue;
                    break;
                default:
                    answer = string.Empty;
                    break;
            }

            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = answer });
        }

        private string GetExtraString(string key, string defaultValue)
        {
            return ViewModel.ExtraData.ContainsKey(key)
                ? ViewModel.ExtraData[key]?.ToString() ?? defaultValue
                : defaultValue;
        }

        private IList<object> GetExtraList(string key)
        {
            if (!ViewModel.ExtraData.ContainsKey(key)) return new List<object>();
            return ViewModel.ExtraData[key] as IList<object> ?? new List<object>();
        }
    }
}
