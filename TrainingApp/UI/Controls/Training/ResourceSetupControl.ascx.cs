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
            // Restore disabled state from HfRetrieved (PostBack cycle only)
            // Uses Attributes["disabled"] — NOT Enabled=false — so ASP.NET keeps the button
            // registered in EventValidation and BtnSave_Click fires correctly.
            if (HfRetrieved.Value == "true")
                BtnSave.Attributes.Remove("disabled");
            else
                BtnSave.Attributes["disabled"] = "disabled";

            if (!IsPostBack)
                Initialize();
            else if (HfRetrieved.Value == "true")
                Page.ClientScript.RegisterStartupScript(GetType(), "rsRestoreUI",
                    "rsRestoreRetrievedState();", true);
        }

        public void Initialize()
        {
            if (ViewModel == null) return;
            LitSite.Text     = Page.Server.HtmlEncode(
                ViewModel.ExtraData.ContainsKey("Site") ? ViewModel.ExtraData["Site"]?.ToString() : "CR9A");
            LitResource.Text = Page.Server.HtmlEncode(
                ViewModel.ExtraData.ContainsKey("ResourceName") ? ViewModel.ExtraData["ResourceName"]?.ToString() : "");

            // Disable Save until Retrieve is clicked (also covers first dynamic load
            // where Page_Load doesn't fire because the control is added in an event handler)
            BtnSave.Attributes["disabled"] = "disabled";
        }

        protected void BtnSave_Click(object sender, EventArgs e)
        {
            string setupState = HfSetupState.Value;
            string rowsJson   = HfRows.Value;

            // Always re-open the modal FIRST so it stays visible regardless of outcome.
            // This prevents the modal from disappearing on PostBack when validation fails.
            Page.ClientScript.RegisterStartupScript(GetType(), "rsKeepOpen",
                "showModalSafe('modalResourceSetup'); rsRestoreRetrievedState();", true);

            // Server-side validation (primary gate: client rsPrepareSubmit() should catch
            // these first, but we validate here as a reliable fallback)
            if (setupState != "Set Up")
            {
                Page.ClientScript.RegisterStartupScript(GetType(), "swalRS",
                    "Swal.fire({icon:'warning',title:'Setup State incorrecto'," +
                    "text:'El Setup State debe estar en \"Set Up\" antes de guardar.'});", true);
                return;
            }

            if (string.IsNullOrEmpty(rowsJson) || rowsJson == "[]")
            {
                Page.ClientScript.RegisterStartupScript(GetType(), "swalRS",
                    "Swal.fire({icon:'warning',title:'Sin recursos'," +
                    "text:'No existen recursos creados.'});", true);
                return;
            }

            // Build the answer JSON and submit the step
            string answer = string.Format("{{\"setupState\":\"{0}\",\"rows\":{1}}}",
                setupState?.Replace("\"", "\\\"") ?? "",
                rowsJson);

            // Show Record Change banner + restore rows (BUG 3: rows are DOM-only, lost on PostBack)
            PnlRecordChange.Visible = true;
            Page.ClientScript.RegisterStartupScript(GetType(), "rsAfterSave",
                "rsRestoreRows(" + rowsJson + ");" +
                "setTimeout(function(){" +
                "  var b=document.querySelector('.record-change-banner');" +
                "  if(b) b.style.display='none';" +
                "},5000);", true);

            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = answer });
        }

        protected void BtnClear_Click(object sender, EventArgs e)
        {
            HfRows.Value       = "[]";
            HfSetupState.Value = "Open";
            HfRetrieved.Value  = "false";
            BtnSave.Attributes["disabled"] = "disabled";
        }
    }
}
