using System;
using System.Web;
using System.Web.UI;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class SFCSplitControl : UserControl, IStepControl
    {
        public StepViewModel ViewModel { get; set; }
        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;
        public event EventHandler<SFCSplitRequestedEventArgs> OnSplitRequested;

        private bool HasSuccessfulSplit
        {
            get { return ViewState["SFCSplit_HasSuccessfulSplit"] as bool? ?? false; }
            set { ViewState["SFCSplit_HasSuccessfulSplit"] = value; }
        }

        public void Initialize()
        {
            TxtQtyToSplit.Text      = string.Empty;
            LitNewQty.Text          = string.Empty;
            PnlSplitConfirm.Visible = false;

            if (ViewModel == null) return;

            string sfcCode = GetExtraData("SFCCode", "—");
            int availableQty = GetExtraData("AvailableQty", 0);
            bool hasSuccessfulSplit = GetExtraData("HasSuccessfulSplit", false);

            LitSFCToSplit.Text = Page.Server.HtmlEncode(sfcCode);
            LitAvailableQty.Text = availableQty.ToString();
            HasSuccessfulSplit = hasSuccessfulSplit;
            BtnCloseSplit.Enabled = hasSuccessfulSplit;
        }

        protected void BtnSplit_Click(object sender, EventArgs e)
        {
            // Formato (sin intento): sin validadores en el markup, el aviso es un SweetAlert.
            int availableQty = GetExtraData("AvailableQty", 0);
            if (!int.TryParse(TxtQtyToSplit.Text.Trim(), out int qtyToSplit) ||
                qtyToSplit <= 0 || qtyToSplit >= availableQty)
            {
                ReopenModal();
                ShowWarning("Qty inválida",
                    "La cantidad a dividir debe ser mayor a 0 y menor que la Qty disponible.");
                return;
            }

            var args = new SFCSplitRequestedEventArgs
            {
                RowId = GetExtraData<string>("SelectedRowId", null),
                Qty = qtyToSplit
            };
            OnSplitRequested?.Invoke(this, args);

            // Spec 014: el paso avanzó forzado (la página ya lo cerró) o la página ya mostró el
            // intento fallido.
            if (args.StepResolved) return;
            ReopenModal();
            if (args.Handled) return;

            if (!args.IsApplied)
            {
                ShowWarning("No se pudo dividir", string.IsNullOrEmpty(args.ErrorMessage)
                    ? "La fila seleccionada ya no es válida. Selecciónala nuevamente en el POD."
                    : args.ErrorMessage);
                return;
            }

            LitAvailableQty.Text = args.RemainingQty.ToString();
            LitNewSfc.Text = Page.Server.HtmlEncode(args.NewSfcCode ?? string.Empty);
            LitNewQty.Text = qtyToSplit.ToString();
            PnlSplitConfirm.Visible = true;
            HasSuccessfulSplit = true;
            BtnCloseSplit.Enabled = true;
            TxtQtyToSplit.Text = string.Empty;
        }

        protected void BtnCloseSplit_Click(object sender, EventArgs e)
        {
            if (!HasSuccessfulSplit)
            {
                ReopenModal();
                ShowWarning("Split requerido", "Debes completar al menos un split antes de cerrar el paso.");
                return;
            }

            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = "split-completed" });
        }

        private T GetExtraData<T>(string key, T defaultValue)
        {
            if (ViewModel == null || !ViewModel.ExtraData.ContainsKey(key) ||
                ViewModel.ExtraData[key] == null)
                return defaultValue;

            return (T)Convert.ChangeType(ViewModel.ExtraData[key], typeof(T));
        }

        private void ReopenModal()
        {
            Page.ClientScript.RegisterStartupScript(GetType(), "keepSFCSplitOpen",
                "showModalSafe('modalSFCSplit');", true);
        }

        private void ShowWarning(string title, string text)
        {
            Page.ClientScript.RegisterStartupScript(GetType(), "swalSFCSplit",
                "Swal.fire({icon:'warning',title:'" + HttpUtility.JavaScriptStringEncode(title ?? string.Empty) +
                "',text:'" + HttpUtility.JavaScriptStringEncode(text ?? string.Empty) + "'});", true);
        }
    }
}
