using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Models;
// DataCollectionParameter.DataType is a string: "Numeric" | "Boolean" | "Date" | "Text"

namespace TrainingApp.UI.Controls.Training
{
    public partial class DataCollectionControl : UserControl, TrainingApp.Pages.Training.Models.IStepControl
    {
        public StepViewModel ViewModel { get; set; }

        // Parte del contrato IStepControl; DC guarda grupo por grupo (ver OnGroupSubmit), así
        // que el evento no guarda suscriptores.
        public event EventHandler<StepSubmitEventArgs> OnStepSubmit { add { } remove { } }

        // Save de un DC Group para la pieza actual (spec 008).
        public event EventHandler<DataCollectionGroupSubmitEventArgs> OnGroupSubmit;

        // El operador sale del Data Collection List sin responder: el host cierra el control.
        public event EventHandler OnReturnToPod;

        // Active group stored in ViewState for PostBack persistence
        private string ActiveGroup
        {
            get { return ViewState["DC_ActiveGroup"] as string; }
            set { ViewState["DC_ActiveGroup"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Control estático: corre en cada request. Invisible = otro paso activo.
            if (!Visible) return;

            // Los inputs de parámetros son dinámicos: se recrean en cada postback
            // mientras hay un grupo abierto, para que Save lea sus valores.
            if (ActiveGroup != null) RenderParams(ActiveGroup);
        }

        public void Initialize()
        {
            ActiveGroup         = null;
            HfActiveGroup.Value = string.Empty;
            HfAnswers.Value     = string.Empty;
            LitActiveGroup.Text = string.Empty;
            PhParameters.Controls.Clear();

            if (ViewModel == null) return;
            BindGroupList();
            UpdatePieceIndicator();
        }

        // Después de un Save correcto que no completa el paso: vuelve a la Data Collection
        // List con el indicador de pieza actualizado (spec 008).
        public void ShowGroupList()
        {
            ActiveGroup         = null;
            HfActiveGroup.Value = string.Empty;
            HfAnswers.Value     = string.Empty;
            LitActiveGroup.Text = string.Empty;
            PhParameters.Controls.Clear();

            if (ViewModel == null) return;
            BindGroupList();
            UpdatePieceIndicator();
        }

        // "SFC: 000123-001 (1/3)" debajo del header de Data Collection Entry (spec 008).
        private void UpdatePieceIndicator()
        {
            string label = ViewModel.ExtraData.ContainsKey("PieceLabel")
                ? ViewModel.ExtraData["PieceLabel"]?.ToString() : "—";
            LitSFCCounter.Text = Page.Server.HtmlEncode(label);
        }

        protected void BtnCollect_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            string groupCode = btn.CommandArgument;
            ActiveGroup          = groupCode;
            HfActiveGroup.Value  = groupCode;
            LitActiveGroup.Text  = Page.Server.HtmlEncode(groupCode);

            RenderParams(groupCode);

            Page.ClientScript.RegisterStartupScript(GetType(), "openDCEntry",
                "showModalSafe('modalDCEntry');", true);
        }

        protected void BtnSave_Click(object sender, EventArgs e)
        {
            // ASP.NET dispara el click aunque el control ya esté cerrado (spec 006, E12).
            if (!Visible || string.IsNullOrEmpty(ActiveGroup)) return;

            string answersJson = HfAnswers.Value;
            if (string.IsNullOrEmpty(answersJson)) answersJson = "{}";
            OnGroupSubmit?.Invoke(this, new DataCollectionGroupSubmitEventArgs
            {
                GroupCode   = ActiveGroup,
                AnswersJson = answersJson
            });
        }

        protected void BtnReturnPOD_Click(object sender, EventArgs e)
        {
            OnReturnToPod?.Invoke(this, EventArgs.Empty);
        }

        private void BindGroupList()
        {
            // DC Groups del catálogo (spec 008), ya ordenados por DisplayOrder. La lista no
            // indica cuáles son obligatorios: saberlo es parte de lo que se entrena.
            var groups = GetGroups()
                .Select(g => new
                {
                    GroupCode     = g.Code,
                    Description   = g.Description,
                    CollectDataAt = g.CollectDataAt
                })
                .ToList();

            RptGroups.DataSource = groups;
            RptGroups.DataBind();
        }

        private void RenderParams(string groupCode)
        {
            PhParameters.Controls.Clear();
            var group = GetGroups().FirstOrDefault(g => g.Code == groupCode);
            if (group == null) return;

            var parameters = GetParameters()
                .Where(p => p.DataCollectionGroupId == group.Id)
                .OrderBy(p => p.DisplayOrder);

            foreach (var param in parameters)
            {
                var wrapper = new Panel();
                wrapper.CssClass = "mb-3";

                var label = new Label();
                label.Text = "<label class=\"form-label small\"><span class=\"required-star\">★</span> " +
                    Page.Server.HtmlEncode(param.Name) + "</label>" +
                    "<div class=\"text-muted small mb-1\">Data Type: " + param.DataType + " &nbsp; 0 files attached" +
                    " &nbsp; <button type=\"button\" class=\"btn btn-link btn-sm p-0\">Comments</button></div>";
                label.ID = "lbl_" + param.Id;
                wrapper.Controls.Add(label);

                if (param.DataType == "Boolean")
                {
                    var ddl = new DropDownList();
                    ddl.CssClass = "form-select form-select-sm";
                    ddl.Attributes["data-paramid"] = param.Id.ToString();
                    ddl.Items.Add(new ListItem("( Select )", ""));
                    ddl.Items.Add(new ListItem("NO", "false"));
                    ddl.Items.Add(new ListItem("Si", "true"));
                    wrapper.Controls.Add(ddl);
                }
                else
                {
                    var txt = new TextBox();
                    txt.CssClass = "form-control form-control-sm";
                    txt.Attributes["data-paramid"] = param.Id.ToString();
                    if (param.DataType == "Numeric")
                        txt.Attributes["type"] = "number";
                    else if (param.DataType == "Date")
                        txt.Attributes["type"] = "date";
                    wrapper.Controls.Add(txt);
                }

                PhParameters.Controls.Add(wrapper);
            }
        }

        private IList<DataCollectionGroup> GetGroups()
        {
            if (ViewModel?.ExtraData?.ContainsKey("Groups") == true)
                return ViewModel.ExtraData["Groups"] as IList<DataCollectionGroup>
                    ?? new List<DataCollectionGroup>();
            return new List<DataCollectionGroup>();
        }

        private IList<DataCollectionParameter> GetParameters()
        {
            if (ViewModel?.ExtraData?.ContainsKey("Parameters") == true)
                return ViewModel.ExtraData["Parameters"] as IList<DataCollectionParameter>
                    ?? new List<DataCollectionParameter>();
            return new List<DataCollectionParameter>();
        }
    }
}
