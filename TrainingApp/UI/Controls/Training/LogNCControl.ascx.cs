using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class LogNCControl : UserControl, TrainingApp.Pages.Training.Models.IStepControl
    {
        public StepViewModel ViewModel { get; set; }

        // "split" | "nc"
        public string Mode
        {
            get { return ViewState["LogNC_Mode"] as string ?? "nc"; }
            set { ViewState["LogNC_Mode"] = value; }
        }

        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && ViewModel != null)
                Initialize();
        }

        private void Initialize()
        {
            string sfcCode = ViewModel.ExtraData.ContainsKey("SFCCode")
                ? ViewModel.ExtraData["SFCCode"]?.ToString() : "—";

            if (Mode == "split")
            {
                LitSFCToSplit.Text  = Page.Server.HtmlEncode(sfcCode);
                LitAvailableQty.Text = "250";
                PnlSFCSplit.Visible  = true;
            }
            else
            {
                LitNCEntrySFC.Text   = Page.Server.HtmlEncode(sfcCode);
                PnlNCSelection.Visible = true;
                BindNCCodes();
            }
        }

        private void BindNCCodes()
        {
            var codes = ViewModel.ExtraData.ContainsKey("NCCodes")
                ? ViewModel.ExtraData["NCCodes"] as IList<NCCode>
                : new List<NCCode>();

            RptNCCodes.DataSource = codes ?? new List<NCCode>();
            RptNCCodes.DataBind();
        }

        // ── SFC Split handlers ───────────────────────────────────────────
        protected void BtnSplit_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            string qty     = TxtQtyToSplit.Text.Trim();
            string sfcCode = LitSFCToSplit.Text;

            LitNewSFC.Text  = sfcCode + "-S1";
            LitNewQty.Text  = qty;
            PnlSplitConfirm.Visible = true;
            BtnSplit.Enabled = false;
        }

        protected void BtnClearSplit_Click(object sender, EventArgs e)
        {
            TxtQtyToSplit.Text = string.Empty;
        }

        protected void BtnCloseSplit_Click(object sender, EventArgs e)
        {
            // User confirmed split and closed — now show NC Selection
            string qty = TxtQtyToSplit.Text.Trim();
            string splitAnswer = "{\"sfcSplit\":{\"qty\":" + (string.IsNullOrEmpty(qty) ? "0" : qty) + "}}";
            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = splitAnswer });
        }

        // ── NC Selection handler ─────────────────────────────────────────
        protected void BtnLogNCCode_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            string[] parts  = btn.CommandArgument.Split('|');
            HfNCCodeId.Value = parts[0];
            TxtNCCode.Text   = parts.Length > 1 ? parts[1] : parts[0];

            PnlNCSelection.Visible = false;
            PnlNCEntry.Visible     = true;
        }

        // ── NC Data Entry handlers ───────────────────────────────────────
        protected void BtnAdd_Click(object sender, EventArgs e)
        {
            // Register NC without closing — keep modal open
            PnlNCEntry.Visible = true;
        }

        protected void BtnAddDone_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            EmitNCAnswer();
        }

        protected void BtnReturnPOD_Click(object sender, EventArgs e)
        {
            PnlNCEntry.Visible     = false;
            PnlNCSelection.Visible = false;
        }

        protected void BtnDone_Click(object sender, EventArgs e)
        {
            EmitNCAnswer();
        }

        private void EmitNCAnswer()
        {
            string ncCodeId   = HfNCCodeId.Value;
            string component  = TxtComponent.Text.Trim().Replace("\"", "\\\"");
            string comments   = TxtComments.Text.Trim().Replace("\"", "\\\"");

            string answer = "{\"ncEntries\":[{" +
                "\"ncCodeId\":\"" + ncCodeId + "\"," +
                "\"component\":\"" + component + "\"," +
                "\"comments\":\"" + comments + "\"}]}";

            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = answer });
        }
    }
}
