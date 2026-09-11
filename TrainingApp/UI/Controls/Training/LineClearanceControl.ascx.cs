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
            if (!IsPostBack)
                Initialize();
            else if (HfRetrieved.Value == "true")
                Page.ClientScript.RegisterStartupScript(GetType(), "lcRestoreUI",
                    "lcRestoreRetrievedState();", true);
        }

        public void Initialize()
        {
            if (ViewModel == null) return;
            PnlSavedBanner.Visible = false;
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

        protected void BtnSave_Click(object sender, EventArgs e)
        {
            string answer = DdlAnswer.SelectedValue;

            // Always re-open main modal FIRST so it stays visible regardless of outcome.
            Page.ClientScript.RegisterStartupScript(GetType(), "lcKeepOpen",
                "showModalSafe('modalDCStandalone'); lcRestoreRetrievedState();", true);

            // Server-side validation (belt-and-suspenders — lcValidateAnswer() should catch these client-side)
            if (string.IsNullOrEmpty(answer))
            {
                Page.ClientScript.RegisterStartupScript(GetType(), "swalLC",
                    "Swal.fire({icon:'warning',title:'Selección requerida'," +
                    "text:'Debes seleccionar una respuesta antes de guardar.'});" +
                    "showModalSafe('modalDCEntry');", true);
                return;
            }

            if (answer != "true")
            {
                Page.ClientScript.RegisterStartupScript(GetType(), "swalLC",
                    "Swal.fire({icon:'warning',title:'Respuesta incorrecta'," +
                    "text:'La respuesta debe ser \\u0027S\\u00ed\\u0027 para continuar.'});" +
                    "showModalSafe('modalDCEntry');", true);
                return;
            }

            // Success: show banner, register auto-hide, fire submit
            PnlSavedBanner.Visible = true;
            Page.ClientScript.RegisterStartupScript(GetType(), "lcAfterSave",
                "setTimeout(function(){" +
                "  var b=document.querySelector('.lc-save-banner');" +
                "  if(b) b.style.display='none';" +
                "},5000);", true);

            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = answer });
        }
    }
}
