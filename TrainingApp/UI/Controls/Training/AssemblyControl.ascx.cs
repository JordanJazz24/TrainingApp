using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class AssemblyControl : UserControl, TrainingApp.Pages.Training.Models.IStepControl
    {
        public StepViewModel ViewModel { get; set; }

        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && ViewModel != null)
            {
                int expected = GetExpectedCount();
                HfExpectedCount.Value = expected.ToString();
                HfScannedCount.Value  = "0";

                var components = BuildComponentList(expected);
                RptComponents.DataSource = components;
                RptComponents.DataBind();
                LitTotal.Text   = components.Count.ToString();
                LitTotalOf.Text = components.Count.ToString();
            }
        }

        protected void BtnAdd_Click(object sender, EventArgs e)
        {
            int scanned  = int.Parse(HfScannedCount.Value) + 1;
            int expected = int.Parse(HfExpectedCount.Value);

            string barcode = HfBarcodeData.Value;

            // Populate component details for display
            LitComponentCode.Text    = "COMP-" + barcode.Substring(0, Math.Min(6, barcode.Length)).ToUpper();
            LitComponentVersion.Text = "A";
            LitComponentDesc.Text    = "Simulated Component";
            LitComponentQty.Text     = "1";
            LitBatchNumber.Text      = "LOT-" + barcode.Substring(0, Math.Min(4, barcode.Length)).ToUpper();
            PnlComponentDetails.Visible = true;
            PnlComponentEmpty.Visible   = false;
            PnlAssemblyData.Visible     = true;
            PnlAssemblyEmpty.Visible    = false;

            HfScannedCount.Value = scanned.ToString();
            TxtBarcode.Text      = string.Empty;

            if (scanned >= expected)
            {
                OnStepSubmit?.Invoke(this,
                    new StepSubmitEventArgs { Answer = "{\"scannedCount\":" + scanned + "}" });
            }
            else
            {
                // More components to scan — keep modal open
                Page.ClientScript.RegisterStartupScript(GetType(), "reopenAssemble",
                    "$(document).ready(function(){var m=document.getElementById('modalAssemble');if(m)new bootstrap.Modal(m).show();});", true);
            }
        }

        protected void BtnReturnPOD_Click(object sender, EventArgs e)
        {
            // User cancelled — do nothing, code-behind hides control
        }

        private int GetExpectedCount()
        {
            if (ViewModel?.ExtraData?.ContainsKey("ExpectedCount") == true)
                return Convert.ToInt32(ViewModel.ExtraData["ExpectedCount"]);
            return 1;
        }

        private static IList<object> BuildComponentList(int count)
        {
            var list = new List<object>();
            for (int i = 1; i <= count; i++)
            {
                list.Add(new
                {
                    Code        = "COMP-" + i.ToString("D3"),
                    Description = "Component " + i,
                    Qty         = 1,
                    RemQty      = 1
                });
            }
            return list;
        }
    }
}
