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

        // Active group stored in ViewState for PostBack persistence
        private string ActiveGroup
        {
            get { return ViewState["DC_ActiveGroup"] as string; }
            set { ViewState["DC_ActiveGroup"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) Initialize();
            else if (ActiveGroup != null) RenderParams(ActiveGroup);
        }

        public void Initialize()
        {
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

        protected void BtnSaveDraft_Click(object sender, EventArgs e)
        {
            // Save Draft — does NOT complete the step; keep modal open
            Page.ClientScript.RegisterStartupScript(GetType(), "reopenDCEntry",
                "showModalSafe('modalDCEntry');", true);
        }

        private void BindGroupList()
        {
            var parameters = GetParameters();
            // Group by DC Group (use first param's group or area name as group code)
            string areaName = ViewModel.ExtraData.ContainsKey("AreaName")
                ? ViewModel.ExtraData["AreaName"]?.ToString() : "AREA";

            var groups = new List<object>
            {
                new { GroupCode = areaName + "/A", Description = "Data Collection - " + areaName, CollectDataAt = "Anytime" }
            };

            RptGroups.DataSource = groups;
            RptGroups.DataBind();
        }

        private void RenderParams(string groupCode)
        {
            PhParameters.Controls.Clear();
            var parameters = GetParameters();

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
