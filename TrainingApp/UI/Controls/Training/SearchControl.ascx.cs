using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;
using System.Web.UI;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class SearchControl : UserControl, IStepControl
    {
        public StepViewModel ViewModel { get; set; }

        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        public void Initialize()
        {
            foreach (var txt in new[] { TxtMoldeId, TxtMss1, TxtMss2, TxtMss3, TxtMss4, TxtMss5,
                                        TxtMss6, TxtMss7, TxtMss8, TxtMss9, TxtMss10 })
                txt.Text = string.Empty;
            foreach (var ddl in new[] { DdlVerif1, DdlVerif2, DdlVerif3, DdlFinal })
                ddl.ClearSelection();

            if (ViewModel == null) return;

            string resource = ViewModel.ExtraData.ContainsKey("ResourceName")
                ? Page.Server.HtmlEncode(ViewModel.ExtraData["ResourceName"]?.ToString() ?? "")
                : "";

            LitResource1.Text = LitResource2.Text = LitResource3.Text = LitResource4.Text = resource;

            // Abre el Modal 1. De aquí en adelante la navegación es 100% cliente
            // (_searchGo) — el único postback de la secuencia es el Save del Modal 4.
            Page.ClientScript.RegisterStartupScript(GetType(), "searchOpen1",
                "_searchOpenModalId='modalSearch1'; showModalSafe('modalSearch1');", true);
        }

        // ── Único postback de la secuencia (patrón 1 postback / paso) ──────
        // La navegación 1→2→3→4 y las validaciones simples ya ocurrieron en el
        // cliente (searchValidate1..4). Aquí solo se re-valida como fallback
        // (belt-and-suspenders) y se entrega el dato recolectado al servidor.
        protected void BtnSave4_Click(object sender, EventArgs e)
        {
            string failingModal = ValidateAll();
            if (failingModal != null)
            {
                Page.ClientScript.RegisterStartupScript(GetType(), "searchReopen",
                    "showModalSafe('" + failingModal + "');", true);
                Page.ClientScript.RegisterStartupScript(GetType(), "searchSwal",
                    "Swal.fire({icon:'warning',title:'Valor inválido'," +
                    "text:'Completa correctamente los campos del modal actual antes de guardar.'});", true);
                return;
            }

            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = BuildAnswerJson() });
        }

        // Devuelve el id del primer modal con datos inválidos, o null si todo OK.
        private string ValidateAll()
        {
            string molde = TxtMoldeId.Text.Trim();
            if (string.IsNullOrEmpty(molde) ||
                !decimal.TryParse(molde, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                return "modalSearch1";

            // Sí/No: solo se exige respuesta (formato). Si es correcta lo decide StepValidator.
            string[] verifs = { DdlVerif1.SelectedValue, DdlVerif2.SelectedValue, DdlVerif3.SelectedValue };
            foreach (var val in verifs)
                if (string.IsNullOrEmpty(val))
                    return "modalSearch2";

            string[] mss = {
                TxtMss1.Text.Trim(), TxtMss2.Text.Trim(), TxtMss3.Text.Trim(), TxtMss4.Text.Trim(),
                TxtMss5.Text.Trim(), TxtMss6.Text.Trim(), TxtMss7.Text.Trim(), TxtMss8.Text.Trim(),
                TxtMss9.Text.Trim(), TxtMss10.Text.Trim()
            };
            foreach (var v in mss)
                if (string.IsNullOrEmpty(v) ||
                    !decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                    return "modalSearch3";

            if (string.IsNullOrEmpty(DdlFinal.SelectedValue))
                return "modalSearch4";

            return null;
        }

        // Respuesta calificada incorrecta: reabre el primer modal con un "No" para que el
        // operador lo corrija; los valores ya capturados se conservan por ViewState.
        public void ReopenForRetry()
        {
            bool verifsConfirmed = DdlVerif1.SelectedValue == "true" &&
                                  DdlVerif2.SelectedValue == "true" &&
                                  DdlVerif3.SelectedValue == "true";
            string modalId = verifsConfirmed ? "modalSearch4" : "modalSearch2";

            Page.ClientScript.RegisterStartupScript(GetType(), "searchRetry",
                "_searchOpenModalId='" + modalId + "'; showModalSafe('" + modalId + "');", true);
        }

        // Datos recolectados de los 4 modales — quedan disponibles para el servidor
        // (podrían persistirse en el intento del paso Search en un futuro).
        private string BuildAnswerJson()
        {
            var dict = new Dictionary<string, object>();
            dict.Add("moldeId",           TxtMoldeId.Text.Trim());
            dict.Add("verifCalibracion",  DdlVerif1.SelectedValue == "true");
            dict.Add("verifD0128682",     DdlVerif2.SelectedValue == "true");
            dict.Add("verifD0128035",     DdlVerif3.SelectedValue == "true");
            dict.Add("mss1",  TxtMss1.Text.Trim());
            dict.Add("mss2",  TxtMss2.Text.Trim());
            dict.Add("mss3",  TxtMss3.Text.Trim());
            dict.Add("mss4",  TxtMss4.Text.Trim());
            dict.Add("mss5",  TxtMss5.Text.Trim());
            dict.Add("mss6",  TxtMss6.Text.Trim());
            dict.Add("mss7",  TxtMss7.Text.Trim());
            dict.Add("mss8",  TxtMss8.Text.Trim());
            dict.Add("mss9",  TxtMss9.Text.Trim());
            dict.Add("mss10", TxtMss10.Text.Trim());
            dict.Add("verificacionFinal", DdlFinal.SelectedValue == "true");
            return new JavaScriptSerializer().Serialize(dict);
        }
    }
}