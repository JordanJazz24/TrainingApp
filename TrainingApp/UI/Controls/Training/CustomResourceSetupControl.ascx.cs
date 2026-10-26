using System;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web.UI;
using TrainingApp.BLL.Training;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    // Custom Resource Setup (spec 020). Solo UI: arma la pantalla con los datos de la BLL y envía
    // el Save; lo califica PodService.SubmitResourceSetup.
    public partial class CustomResourceSetupControl : UserControl, IStepControl
    {
        public StepViewModel ViewModel { get; set; }

        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Control estático: corre en cada request. Invisible = otro paso activo.
            if (!Visible) return;

            if (IsPostBack)
                Page.ClientScript.RegisterStartupScript(GetType(), "crsRestore", "crsRestore();", true);
        }

        public void Initialize()
        {
            HfAnswer.Value      = string.Empty;
            HfToolGroups.Value  = string.Empty;
            PnlSuccess.Visible  = false;

            if (ViewModel == null ||
                !ViewModel.ExtraData.TryGetValue("CustomResourceSetup", out var raw) ||
                !(raw is CustomResourceSetupData data))
                return;

            // HiddenField codifica el atributo; JavaScriptSerializer escapa < > ' & en el JSON.
            HfToolGroups.Value = new JavaScriptSerializer().Serialize(
                data.ToolGroups.Select(g => new { g.Code, g.Description }));
            LitSite.Text      = Page.Server.HtmlEncode(data.Site);
            LitResource.Text  = Page.Server.HtmlEncode(data.Resource);
            LitOperation.Text = Page.Server.HtmlEncode(data.Operation);
            LitCode1.Text     = Page.Server.HtmlEncode(
                ViewModel.ExtraData.TryGetValue("Code1", out var code1) ? code1 as string : string.Empty);

            Page.ClientScript.RegisterStartupScript(GetType(), "crsInit", "crsClear();", true);
        }

        protected void BtnConfirm_Click(object sender, EventArgs e)
        {
            // ASP.NET dispara el click aunque el control ya esté cerrado (spec 006, E12).
            if (!Visible) return;

            // Respaldo del formato que el cliente ya avisa (D-191): sin intento. GuidedTest reabre
            // el modal en el resto de los caminos.
            var answer = CustomResourceSetupAnswer.TryParse(HfAnswer.Value);
            if (answer == null || !answer.IsFormatValid)
            {
                Page.ClientScript.RegisterStartupScript(GetType(), "crsFormat",
                    "showModalSafe('modalCustomResourceSetup');" +
                    "Swal.fire({icon:'warning',title:'Datos incompletos'," +
                    "text:'Revise la ShopOrder asociada y los Tool Numbers.'});", true);
                return;
            }

            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = HfAnswer.Value });
        }

        // "Resource setup saved successfully.": lo muestra GuidedTest solo cuando el servidor
        // aceptó el paso; el OK cierra el modal (D-190).
        public void ShowSaveConfirmation()
        {
            PnlSuccess.Visible = true;
        }
    }
}
