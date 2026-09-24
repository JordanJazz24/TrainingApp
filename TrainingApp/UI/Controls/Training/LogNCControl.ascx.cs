using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    public partial class LogNCControl : UserControl, IStepControl
    {
        private const string WrongButtonFeedback = "Not the correct button";

        public StepViewModel ViewModel { get; set; }
        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

        // El operador sale al POD sin responder: el host cierra el control.
        public event EventHandler OnReturnToPod;

        public void Initialize()
        {
            PnlNCSelection.Visible = false;
            PnlNCEntry.Visible     = false;
            HfNCPage.Value         = "0";
            HfNCCodeId.Value       = string.Empty;
            TxtNCCode.Text         = string.Empty;
            TxtComponent.Text      = string.Empty;
            TxtComments.Text       = string.Empty;

            if (ViewModel == null) return;

            string sfcCode = ViewModel.ExtraData.ContainsKey("SFCCode")
                ? ViewModel.ExtraData["SFCCode"]?.ToString() : "—";
            LitNCEntrySFC.Text = Page.Server.HtmlEncode(sfcCode);

            // Spec 007: Component solo en áreas que lo usan (Area.RequiresNCComponent).
            // Invisible + deshabilitado: el validador no se evalúa en Add-Done.
            bool requiresComponent = !ViewModel.ExtraData.ContainsKey("RequiresNCComponent")
                || Convert.ToBoolean(ViewModel.ExtraData["RequiresNCComponent"]);
            PnlComponent.Visible = requiresComponent;
            RfvComponent.Enabled = requiresComponent;

            PnlNCSelection.Visible = true;
            BindNCCodes();
        }

        private void BindNCCodes()
        {
            var codes = ViewModel.ExtraData.ContainsKey("NCCodes")
                ? ViewModel.ExtraData["NCCodes"] as IList<NCCode>
                : new List<NCCode>();
            RptNCCodes.DataSource = codes ?? new List<NCCode>();
            RptNCCodes.DataBind();
        }

        protected void BtnLogNCCode_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            string[] parts = btn.CommandArgument.Split('|');
            HfNCCodeId.Value = parts[0];
            TxtNCCode.Text = parts.Length > 1 ? parts[1] : parts[0];

            // La NC Selection List queda visible detrás del modal (spec 007).
            PnlNCEntry.Visible = true;
        }

        protected void BtnCloseEntry_Click(object sender, EventArgs e)
        {
            PnlNCEntry.Visible = false;
        }

        protected void BtnAddDone_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = TxtNCCode.Text.Trim() });
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
