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

        public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

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
            string sfcCode = ViewModel.ExtraData.ContainsKey("SFCCode")
                ? ViewModel.ExtraData["SFCCode"]?.ToString() : "—";
            LitSFCCounter.Text = Page.Server.HtmlEncode(sfcCode + " (1/1)");
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
            string answersJson = HfAnswers.Value;
            if (string.IsNullOrEmpty(answersJson)) answersJson = "{}";
            OnStepSubmit?.Invoke(this, new StepSubmitEventArgs { Answer = answersJson });
        }

        protected void BtnReturnPOD_Click(object sender, EventArgs e)
        {
            OnReturnToPod?.Invoke(this, EventArgs.Empty);
        }

        private void BindGroupList()
        {
            var parameters = GetParameters();

            // Group by DC Group (real grouping from the catalog, not hardcoded per-area).
            // Ordered by each group's lowest DisplayOrder to preserve operational sequence.
            var groups = parameters
                .GroupBy(p => p.DCGroup)
                .OrderBy(g => g.Min(p => p.DisplayOrder))
                .Select(g => new
                {
                    GroupCode     = g.Key,
                    Description   = g.First().GroupDescription,
                    CollectDataAt = g.First().CollectDataAt
                })
                .ToList();

            RptGroups.DataSource = groups;
            RptGroups.DataBind();
        }

        private void RenderParams(string groupCode)
        {
            PhParameters.Controls.Clear();
            var parameters = GetParameters()
                .Where(p => p.DCGroup == groupCode)
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

        private IList<DataCollectionParameter> GetParameters()
        {
            if (ViewModel?.ExtraData?.ContainsKey("Parameters") == true)
                return ViewModel.ExtraData["Parameters"] as IList<DataCollectionParameter>
                    ?? new List<DataCollectionParameter>();
            return new List<DataCollectionParameter>();
        }
    }
}
