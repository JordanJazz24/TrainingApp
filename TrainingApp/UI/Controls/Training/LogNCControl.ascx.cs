using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class LogNCControl : UserControl, IStepControl
    {
        public StepViewModel ViewModel { get; set; }
        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) Initialize();
        }

        public void Initialize()
        {
            if (ViewModel == null) return;

            string sfcCode = ViewModel.ExtraData.ContainsKey("SFCCode")
                ? ViewModel.ExtraData["SFCCode"]?.ToString() : "—";
            LitNCEntrySFC.Text = Page.Server.HtmlEncode(sfcCode);
            PnlNCSelection.Visible = true;
            BindNCCodes();
        }

        private void BindNCCodes()
        {
            var codes = ViewModel.ExtraData.ContainsKey("NCCodes")
                ? ViewModel.ExtraData["NCCodes"] as IList<NCCode>
                : new List<NCCode>();
            RptNCCodes.DataSource = codes ?? new List<NCCode>();
            RptNCCodes.DataBind();
        }

        protected void BtnLogNCCode_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            string[] parts = btn.CommandArgument.Split('|');
            HfNCCodeId.Value = parts[0];
            TxtNCCode.Text = parts.Length > 1 ? parts[1] : parts[0];
            PnlNCSelection.Visible = false;
            PnlNCEntry.Visible = true;
        }

        protected void BtnAdd_Click(object sender, EventArgs e)
        {
            PnlNCEntry.Visible = true;
        }

        protected void BtnAddDone_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            EmitNCAnswer();
        }

        protected void BtnReturnPOD_Click(object sender, EventArgs e)
        {
            PnlNCEntry.Visible = false;
            PnlNCSelection.Visible = false;
        }

        protected void BtnDone_Click(object sender, EventArgs e)
        {
            EmitNCAnswer();
        }

        private void EmitNCAnswer()
        {
            string ncCode = TxtNCCode.Text.Trim();
            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = ncCode });
        }
    }
}
