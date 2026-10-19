using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class LogNCControl : UserControl, IStepControl
    {
        // Feedback de Guided tras un intento (spec 010, P-6: en español).
        private const string WrongButtonFeedback = "Ese no es el botón correcto";

        public StepViewModel ViewModel { get; set; }
        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        // El operador sale al POD sin responder: el host cierra el control.
        public event EventHandler OnReturnToPod;

        // Se eligió un código en NC Selection y se abrió NC Data Entry (la guía de Guided entra
        // en el formulario, spec 010).
        public event EventHandler OnCodeSelected;

        public void Initialize()
        {
            PnlNCSelection.Visible = false;
            PnlNCEntry.Visible     = false;
            HfNCPage.Value         = "0";
            HfNCCodeId.Value       = string.Empty;
            TxtNCCode.Text         = string.Empty;
            TxtComponent.Text      = string.Empty;
            TxtComments.Text       = string.Empty;
            HfRepairFlow.Value     = "0";
            HfRepairDecision.Value = string.Empty;

            if (ViewModel == null) return;

            // "{primer SFC} (1/N)" como Assemble Components (Fase 6.5, entrega 8): el NC se
            // registra en todas las piezas seleccionadas a la vez (spec 008), así que con más de
            // una se muestra "Apply to all SFCs" marcado y fijo.
            int pieces = ViewModel.ExtraData.ContainsKey("SelectedPieceCount")
                ? Math.Max(1, Convert.ToInt32(ViewModel.ExtraData["SelectedPieceCount"])) : 1;
            string firstSfc = ViewModel.ExtraData.ContainsKey("FirstSelectedSFC")
                ? ViewModel.ExtraData["FirstSelectedSFC"]?.ToString() : null;
            if (string.IsNullOrEmpty(firstSfc))
                firstSfc = ViewModel.ExtraData.ContainsKey("SFCCode")
                    ? ViewModel.ExtraData["SFCCode"]?.ToString() : "—";
            LitNCEntrySFC.Text   = Page.Server.HtmlEncode(string.Format("{0} (1/{1})", firstSfc, pieces));
            PnlNCApplyAll.Visible = pieces > 1;

            // Enmienda 4 de la spec 014: Component solo con un código de reparación (al elegirlo).
            ShowComponent(false);

            PnlNCSelection.Visible = true;
            BindNCCodes();
            // Browse (D-39): lo ensamblado en Assembly, para cualquier código.
            RptBrowse.DataSource = Assembled;
            RptBrowse.DataBind();
        }

        private IList<AssembledComponent> Assembled
            => (ViewModel != null && ViewModel.ExtraData.ContainsKey("Assembled")
                ? ViewModel.ExtraData["Assembled"] as IList<AssembledComponent> : null)
               ?? new List<AssembledComponent>();

        // Invisible + deshabilitado: el validador no se evalúa en Add-Done.
        private void ShowComponent(bool visible)
        {
            PnlComponent.Visible = visible;
            RfvComponent.Enabled = visible;
        }

        // Datos de As-Built Configuration de Remove component y Add Component (P-11), para el
        // script del .ascx. JavaScriptSerializer escapa < > &: no puede cerrar el <script>.
        protected string RepairInfoJson
            => new JavaScriptSerializer().Serialize(ViewModel != null && ViewModel.ExtraData.ContainsKey("RepairInfo")
                ? ViewModel.ExtraData["RepairInfo"] : new Dictionary<string, string>());

        // Qty del BOM con punto decimal, como la muestra el MES.
        protected static string FormatQty(object qty)
            => qty is decimal d ? d.ToString("0.######", CultureInfo.InvariantCulture) : Convert.ToString(qty, CultureInfo.InvariantCulture);

        private void BindNCCodes()
        {
            var codes = (ViewModel.ExtraData.ContainsKey("NCCodes")
                ? ViewModel.ExtraData["NCCodes"] as IList<NCCode>
                : null) ?? new List<NCCode>();
            RptNCCodes.DataSource = codes;
            RptNCCodes.DataBind();

            // Guided (spec 014): la lista abre en la página del código que señala la guía. Solo
            // llega en Guided: en Assessment la página inicial no revela nada.
            string guideCode = ViewModel.ExtraData.ContainsKey("GuideNCCode")
                ? ViewModel.ExtraData["GuideNCCode"] as string : null;
            int index = guideCode == null ? -1
                : codes.ToList().FindIndex(c => string.Equals(c.Code, guideCode, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                HfNCPage.Value = (index / NCPageSize).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        // Códigos por página de NC Selection: el script del .ascx lo lee de aquí.
        protected const int NCPageSize = 8;

        protected void BtnLogNCCode_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            string[] parts = btn.CommandArgument.Split('|');
            HfNCCodeId.Value = parts[0];
            TxtNCCode.Text = parts.Length > 1 ? parts[1] : parts[0];

            // Spec 014: un código de reparación rápida sigue con Remove component y Add
            // Component si hay algo ensamblado (Moldeo no tiene BOM: su Add-Done es el normal y
            // el código se califica como cualquier otro, AC-23).
            var codes = ViewModel?.ExtraData.ContainsKey("NCCodes") == true
                ? ViewModel.ExtraData["NCCodes"] as IList<NCCode> : null;
            var code = codes?.FirstOrDefault(c => c.Id.ToString(CultureInfo.InvariantCulture) == parts[0]);
            bool repairFlow = code != null && code.Method == NCMethod.FastRepair && Assembled.Count > 0;
            HfRepairFlow.Value     = repairFlow ? "1" : "0";
            HfRepairDecision.Value = string.Empty;
            // Obligatorio solo en la reparación rápida: el desecho no lo pide (2026-10-19).
            // Enmienda 4: el desecho no muestra Component en ningún área; la rápida lo pide.
            ShowComponent(repairFlow);

            // La NC Selection List queda visible detrás del modal (spec 007).
            PnlNCEntry.Visible = true;
            OnCodeSelected?.Invoke(this, EventArgs.Empty);
        }

        protected void BtnCloseEntry_Click(object sender, EventArgs e)
        {
            PnlNCEntry.Visible = false;
        }

        protected void BtnAddDone_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            // Spec 014 (D-59): la respuesta es un JSON; las filas seleccionadas llegan aparte.
            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs
            {
                Answer = LogNCAnswer.Serialize(TxtNCCode.Text.Trim(), TxtComponent.Text.Trim())
            });
        }

        // Fin del flujo de la reparación rápida (un solo postback, spec 014 punto 12): Add en
        // Add Component, o la decisión equivocada (Return, Send to Routing, No a agregar).
        protected void BtnRepairSubmit_Click(object sender, EventArgs e)
        {
            if (HfRepairFlow.Value != "1") return;

            var decision = LogNCAnswer.Parse(HfRepairDecision.Value);
            if (decision == null || !LogNCAnswer.IsRemoveAction(decision.RemoveAction)) return;

            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs
            {
                Answer = LogNCAnswer.Serialize(TxtNCCode.Text.Trim(), TxtComponent.Text.Trim(),
                    decision.RemoveAction, decision.AddComponent)
            });
        }

        // Add y Done no son el camino correcto del paso: registran un intento fallido.
        protected void BtnWrongButton_Click(object sender, EventArgs e)
        {
            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs
            {
                Answer            = StepAnswers.Invalid,
                IncorrectFeedback = WrongButtonFeedback
            });
        }

        protected void BtnReturnPOD_Click(object sender, EventArgs e)
        {
            OnReturnToPod?.Invoke(this, EventArgs.Empty);
        }
    }
}
