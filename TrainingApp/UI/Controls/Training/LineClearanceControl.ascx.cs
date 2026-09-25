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
            // Control estático: corre en cada request. Invisible = otro paso activo,
            // no debe registrar scripts contra un DOM que no se renderiza.
            if (!Visible) return;

            if (IsPostBack && HfRetrieved.Value == "true")
                Page.ClientScript.RegisterStartupScript(GetType(), "lcRestoreUI",
                    "lcRestoreRetrievedState();", true);
        }

        public void Initialize()
        {
            PnlSavedBanner.Visible = false;
            HfRetrieved.Value      = "false";
            DdlAnswer.ClearSelection();

            if (ViewModel == null) return;
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

            // Server-side validation (belt-and-suspenders — lcValidateAnswer() should catch these client-side).
            // Solo el formato: 'No' se envía y lo califica StepValidator como intento fallido.
            if (string.IsNullOrEmpty(answer))
            {
                Page.ClientScript.RegisterStartupScript(GetType(), "swalLC",
                    "Swal.fire({icon:'warning',title:'Selección requerida'," +
                    "text:'Debes seleccionar una respuesta antes de guardar.'});" +
                    "showModalSafe('modalDCEntry');", true);
                return;
            }

            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = answer });
        }

        // Banner "DC group saved": lo muestra GuidedTest solo cuando el servidor aceptó el paso.
        public void ShowSaveConfirmation()
        {
            PnlSavedBanner.Visible = true;
            Page.ClientScript.RegisterStartupScript(GetType(), "lcAfterSave",
                "setTimeout(function(){" +
                "  var b=document.querySelector('.lc-save-banner');" +
                "  if(b) b.style.display='none';" +
                "},5000);", true);
        }
    }
}
