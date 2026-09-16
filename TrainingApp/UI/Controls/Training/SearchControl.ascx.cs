using System;
using System.Globalization;
using System.Web.UI;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class SearchControl : UserControl, IStepControl
    {
        private const string SubStepKey = "SearchCtrl_SubStep";

        public StepViewModel ViewModel { get; set; }
        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        protected void Page_Load(object sender, EventArgs e) { }

        public void Initialize()
        {
            if (ViewModel == null) return;

            string resource = ViewModel.ExtraData.ContainsKey("ResourceName")
                ? Page.Server.HtmlEncode(ViewModel.ExtraData["ResourceName"]?.ToString() ?? "")
                : "";

            LitResource1.Text = LitResource2.Text = LitResource3.Text = LitResource4.Text = resource;

            ViewState[SubStepKey] = 1;
            Page.ClientScript.RegisterStartupScript(GetType(), "searchOpen1",
                "showModalSafe('modalSearch1');", true);
        }

        protected void BtnSave1_Click(object sender, EventArgs e)
        {
            string v = TxtMoldeId.Text.Trim();
            bool empty   = string.IsNullOrEmpty(v);
            bool notNum  = !empty && !decimal.TryParse(v, NumberStyles.Number,
                                CultureInfo.InvariantCulture, out _);

            if (empty || notNum)
            {
                string msg = empty
                    ? "Debes ingresar el ID del molde."
                    : "El ID del molde debe ser un valor num\u00e9rico.";
                // Reopen modal first so it's visible behind the SweetAlert
                Page.ClientScript.RegisterStartupScript(GetType(), "searchReopen",
                    "showModalSafe('modalSearch1');", true);
                Page.ClientScript.RegisterStartupScript(GetType(), "searchSwal",
                    "Swal.fire({icon:'warning',title:'Valor inv\u00e1lido',text:'" + EscJs(msg) + "'});", true);
                return;
            }

            ViewState[SubStepKey] = 2;
            Page.ClientScript.RegisterStartupScript(GetType(), "searchNext",
                "showModalSafe('modalSearch2');", true);
        }

        protected void BtnSave2_Click(object sender, EventArgs e)
        {
            string[] vals = { DdlVerif1.SelectedValue, DdlVerif2.SelectedValue, DdlVerif3.SelectedValue };
            foreach (var val in vals)
            {
                if (string.IsNullOrEmpty(val))
                {
                    Page.ClientScript.RegisterStartupScript(GetType(), "searchReopen",
                        "showModalSafe('modalSearch2');", true);
                    Page.ClientScript.RegisterStartupScript(GetType(), "searchSwal",
                        "Swal.fire({icon:'warning',title:'Campo requerido'," +
                        "text:'Debes responder todas las preguntas antes de guardar.'});", true);
                    return;
                }
                if (val != "true")
                {
                    Page.ClientScript.RegisterStartupScript(GetType(), "searchReopen",
                        "showModalSafe('modalSearch2');", true);
                    Page.ClientScript.RegisterStartupScript(GetType(), "searchSwal",
                        "Swal.fire({icon:'warning',title:'Respuesta incorrecta'," +
                        "text:'Todas las respuestas deben ser \\u0022S\\u00ed\\u0022 para continuar.'});", true);
                    return;
                }
            }

            ViewState[SubStepKey] = 3;
            Page.ClientScript.RegisterStartupScript(GetType(), "searchNext",
                "showModalSafe('modalSearch3');", true);
        }

        protected void BtnSave3_Click(object sender, EventArgs e)
        {
            System.Web.UI.WebControls.TextBox[] fields = {
                TxtMss1, TxtMss2, TxtMss3, TxtMss4, TxtMss5,
                TxtMss6, TxtMss7, TxtMss8, TxtMss9, TxtMss10
            };
            foreach (var f in fields)
            {
                string v = f.Text.Trim();
                if (string.IsNullOrEmpty(v))
                {
                    Page.ClientScript.RegisterStartupScript(GetType(), "searchReopen",
                        "showModalSafe('modalSearch3');", true);
                    Page.ClientScript.RegisterStartupScript(GetType(), "searchSwal",
                        "Swal.fire({icon:'warning',title:'Campos incompletos'," +
                        "text:'Debes completar todos los par\\u00e1metros antes de guardar.'});", true);
                    return;
                }
                if (!decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                {
                    Page.ClientScript.RegisterStartupScript(GetType(), "searchReopen",
                        "showModalSafe('modalSearch3');", true);
                    Page.ClientScript.RegisterStartupScript(GetType(), "searchSwal",
                        "Swal.fire({icon:'warning',title:'Valor inv\\u00e1lido'," +
                        "text:'Todos los par\\u00e1metros deben ser valores num\\u00e9ricos.'});", true);
                    return;
                }
            }

            ViewState[SubStepKey] = 4;
            Page.ClientScript.RegisterStartupScript(GetType(), "searchNext",
                "showModalSafe('modalSearch4');", true);
        }

        protected void BtnSave4_Click(object sender, EventArgs e)
        {
            string v = DdlFinal.SelectedValue;
            if (string.IsNullOrEmpty(v))
            {
                Page.ClientScript.RegisterStartupScript(GetType(), "searchReopen",
                    "showModalSafe('modalSearch4');", true);
                Page.ClientScript.RegisterStartupScript(GetType(), "searchSwal",
                    "Swal.fire({icon:'warning',title:'Campo requerido'," +
                    "text:'Debes seleccionar una respuesta.'});", true);
                return;
            }
            if (v != "true")
            {
                Page.ClientScript.RegisterStartupScript(GetType(), "searchReopen",
                    "showModalSafe('modalSearch4');", true);
                Page.ClientScript.RegisterStartupScript(GetType(), "searchSwal",
                    "Swal.fire({icon:'warning',title:'Respuesta incorrecta'," +
                    "text:'La respuesta debe ser \\u0022S\\u00ed\\u0022 para continuar.'});", true);
                return;
            }

            ViewState[SubStepKey] = 0;
            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = "true" });
        }

        protected void BtnCancel_Click(object sender, EventArgs e)
        {
            ViewState[SubStepKey] = 0;
            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = "CANCELLED" });
        }

        private static string EscJs(string s)
            => s?.Replace("'", "\\'").Replace("\n", "\\n") ?? string.Empty;
    }
}
