using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Web.UI;
using TrainingApp.BLL.Training;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class ResourceSetupControl : UserControl, TrainingApp.Pages.Training.Models.IStepControl
    {
        public StepViewModel ViewModel { get; set; }

        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Control estático: corre en cada request. Invisible = otro paso activo,
            // no debe registrar scripts contra un DOM que no se renderiza.
            if (!Visible) return;

            // Restore disabled state from HfRetrieved (PostBack cycle only)
            // Uses Attributes["disabled"] — NOT Enabled=false — so ASP.NET keeps the button
            // registered in EventValidation and BtnSave_Click fires correctly.
            if (HfRetrieved.Value == "true")
                BtnSave.Attributes.Remove("disabled");
            else
                BtnSave.Attributes["disabled"] = "disabled";

            if (IsPostBack && HfRetrieved.Value == "true")
                Page.ClientScript.RegisterStartupScript(GetType(), "rsRestoreUI",
                    "rsRestoreRetrievedState();", true);
        }

        public void Initialize()
        {
            HfRows.Value            = string.Empty;
            HfSetupState.Value      = string.Empty;
            HfRetrieved.Value       = "false";
            DdlSetupState.ClearSelection();
            PnlRecordChange.Visible = false;

            if (ViewModel == null) return;
            LitSite.Text     = Page.Server.HtmlEncode(
                ViewModel.ExtraData.ContainsKey("Site") ? ViewModel.ExtraData["Site"]?.ToString() : PodDefaults.Site);
            LitResource.Text = Page.Server.HtmlEncode(
                ViewModel.ExtraData.ContainsKey("ResourceName") ? ViewModel.ExtraData["ResourceName"]?.ToString() : "");

            // Disable Save until Retrieve is clicked (Page_Load ya corrió cuando el
            // control se abre desde el handler del lanzador)
            BtnSave.Attributes["disabled"] = "disabled";
        }

        protected void BtnSave_Click(object sender, EventArgs e)
        {
            // ASP.NET dispara el click aunque el control ya esté cerrado (su modal de
            // confirmación se renderizó en la respuesta anterior): un Save repetido no debe
            // reemplazar la orden ni registrar otro intento (spec 006, E12).
            if (!Visible) return;

            string setupState = HfSetupState.Value;
            string rowsJson   = HfRows.Value;

            // Always re-open the modal FIRST so it stays visible regardless of outcome.
            // This prevents the modal from disappearing on PostBack when validation fails.
            Page.ClientScript.RegisterStartupScript(GetType(), "rsKeepOpen",
                "showModalSafe('modalResourceSetup'); rsRestoreRetrievedState();", true);

            // Server-side validation (primary gate: client rsPrepareSubmit() should catch
            // these first, but we validate here as a reliable fallback). Solo formato: el
            // Setup State es una decisión del operador y lo califica StepValidator.
            // HfRows llega del cliente: se parsea una vez y lo que vuelve al script o a la
            // respuesta se re-serializa (escapa < > ' &). JSON inválido = formato, sin intento
            // (auditoría AUD-11).
            var rows = TryParseRows(rowsJson);
            if (rows == null || rows.Count == 0)
            {
                Page.ClientScript.RegisterStartupScript(GetType(), "swalRS",
                    "Swal.fire({icon:'warning',title:'Sin recursos'," +
                    "text:'No existen recursos creados.'});", true);
                return;
            }

            // Server-side fallback: extraer y validar el Shop Order numérico (spec 005).
            // rsPrepareSubmit() ya valida esto en cliente; aquí re-validamos igual que
            // el resto de las reglas de este método (belt-and-suspenders).
            string shopOrderInput = ExtractShopOrderValue(rows);
            if (!SimulatedOrder.IsValidShopOrder(shopOrderInput))
            {
                Page.ClientScript.RegisterStartupScript(GetType(), "swalRS",
                    "Swal.fire({icon:'warning',title:'Shop Order inválido'," +
                    "text:'El valor de Shop Order debe ser numérico.'});", true);
                return;
            }

            // Build the answer JSON and submit the step
            var serializer = new JavaScriptSerializer();
            string answer  = serializer.Serialize(new { setupState = setupState ?? "", rows });

            // Restore rows (BUG 3: rows are DOM-only, lost on PostBack)
            Page.ClientScript.RegisterStartupScript(GetType(), "rsAfterSave",
                "rsRestoreRows(" + serializer.Serialize(rows) + ");", true);

            // La orden simulada la crea la BLL con esta Shop Order (spec 009).
            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = answer, ShopOrder = shopOrderInput });
        }

        // Banner "Record change": lo muestra GuidedTest solo cuando el servidor aceptó el paso.
        // Se oculta a los 5 s, salvo con la guía activa (spec 010, R9).
        public void ShowSaveConfirmation()
        {
            PnlRecordChange.Visible = true;
            Page.ClientScript.RegisterStartupScript(GetType(), "rsSavedBanner",
                "podAutoHide('.record-change-banner', 5000);", true);
        }

        protected void BtnClear_Click(object sender, EventArgs e)
        {
            HfRows.Value       = "[]";
            HfSetupState.Value = "Open";
            HfRetrieved.Value  = "false";
            BtnSave.Attributes["disabled"] = "disabled";
        }

        // ── Spec 005: captura dinámica de la orden ─────────────────────────

        // Filas {type, value} que arma rsPrepareSubmit(). Devuelve null si el JSON está mal
        // formado o no es una lista de objetos con valores de texto.
        private static List<Dictionary<string, string>> TryParseRows(string rowsJson)
        {
            if (string.IsNullOrWhiteSpace(rowsJson)) return null;

            object parsed;
            try
            {
                parsed = new JavaScriptSerializer().DeserializeObject(rowsJson);
            }
            catch (ArgumentException)
            {
                return null;
            }

            var items = parsed as object[];
            if (items == null) return null;

            var rows = new List<Dictionary<string, string>>();
            foreach (var item in items)
            {
                var fields = item as IDictionary<string, object>;
                if (fields == null) return null;

                var row = new Dictionary<string, string>();
                foreach (var field in fields)
                {
                    var text = field.Value as string;
                    if (text == null) return null;
                    row[field.Key] = text;
                }
                rows.Add(row);
            }
            return rows;
        }

        private static string ExtractShopOrderValue(IEnumerable<Dictionary<string, string>> rows)
        {
            foreach (var row in rows)
            {
                if (row.TryGetValue("type", out var type) && type == "Shop Order")
                    return row.TryGetValue("value", out var value) ? value?.Trim() : null;
            }
            return null;
        }
    }
}
