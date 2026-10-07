using System;
using System.Collections.Generic;
using System.Linq;
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

        // Collect abrió el Data Collection Entry de un grupo (la guía de Guided entra en sus
        // parámetros, spec 010). El grupo está en OpenGroupCode.
        public event EventHandler OnGroupOpened;

        // Grupo abierto en el Data Collection Entry; null con la lista a la vista.
        public string OpenGroupCode => ActiveGroup;

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
            OnGroupOpened?.Invoke(this, EventArgs.Empty);
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
            // indica cuáles son obligatorios: saberlo es parte de lo que se entrena. Un grupo
            // ya guardado por la pieza actual queda con Collect deshabilitado (como Assemble).
            var saved = GetSavedGroupIds();
            var groups = GetGroups()
                .Select(g => new
                {
                    GroupCode     = g.Code,
                    Description   = g.Description,
                    CollectDataAt = g.CollectDataAt,
                    IsSaved       = saved.Contains(g.Id)
                })
                .ToList();

            RptGroups.DataSource = groups;
            RptGroups.DataBind();
            LitGroupsTotal.Text   = groups.Count.ToString();
            LitGroupsTotalOf.Text = groups.Count.ToString();
        }

        private void RenderParams(string groupCode)
        {
            PhParameters.Controls.Clear();
            var group = GetGroups().FirstOrDefault(g => g.Code == groupCode);
            if (group == null) return;

            var parameters = GetParameters()
                .Where(p => p.DataCollectionGroupId == group.Id)
                .OrderBy(p => p.DisplayOrder);

            // Un bloque por parámetro, como el real (Fase 6.5, entrega 8; captura "modal data
            // collection entry"): pregunta en negrita; debajo "Description: …" (si el catálogo
            // la tiene) con "Data Type: …" a su altura; el campo abajo y [Comments] a la
            // derecha. Cada parte es una celda de la grilla de .dc-param. Los valores viajan
            // por data-paramid → HfAnswers (collectDCAnswers): el orden de los controles no importa.
            foreach (var param in parameters)
            {
                var box = new Panel { CssClass = "dc-param" };

                box.Controls.Add(new LiteralControl(
                    "<label class=\"dc-param-label\"><span class=\"pod-star\">*</span> " +
                    Page.Server.HtmlEncode(QuestionLabel(param.Name)) + "</label>"));
                if (!string.IsNullOrWhiteSpace(param.Description))
                    box.Controls.Add(new LiteralControl(
                        "<div class=\"dc-param-desc\"><span class=\"dc-param-key\">Description:</span> " +
                        Page.Server.HtmlEncode(param.Description) + "</div>"));
                box.Controls.Add(new LiteralControl(
                    "<div class=\"dc-param-type\"><span class=\"dc-param-key\">Data Type:</span> " +
                    Page.Server.HtmlEncode(param.DataType) + "</div>" +
                    "<button type=\"button\" class=\"mes-btn dc-param-comments\">Comments</button>"));
                box.Controls.Add(BuildInput(param));

                PhParameters.Controls.Add(box);
            }
        }

        // El real termina cada pregunta con ":" (ej. "La limpieza de linea se ejecuto?:").
        private static string QuestionLabel(string name)
        {
            string text = (name ?? string.Empty).Trim();
            return text.EndsWith(":") ? text : text + ":";
        }

        private static Control BuildInput(DataCollectionParameter param)
        {
            if (param.DataType == "Boolean")
            {
                var ddl = new DropDownList { CssClass = "rs-select dc-param-input" };
                ddl.Attributes["data-paramid"] = param.Id.ToString();
                ddl.Items.Add(new ListItem("( Select )", ""));
                ddl.Items.Add(new ListItem("NO", "false"));
                ddl.Items.Add(new ListItem("Si", "true"));
                return ddl;
            }

            var txt = new TextBox { CssClass = "mes-input dc-param-input" };
            txt.Attributes["data-paramid"] = param.Id.ToString();
            if (param.DataType == "Numeric")
            {
                txt.Attributes["type"] = "number";
                // Sin step el navegador usa step=1 y rechaza decimales (ej. CO2 4.8–5.2 en LoFlo).
                txt.Attributes["step"] = "any";
            }
            else if (param.DataType == "Date")
                txt.Attributes["type"] = "date";
            return txt;
        }

        private IList<DataCollectionGroup> GetGroups()
        {
            if (ViewModel?.ExtraData?.ContainsKey("Groups") == true)
                return ViewModel.ExtraData["Groups"] as IList<DataCollectionGroup>
                    ?? new List<DataCollectionGroup>();
            return new List<DataCollectionGroup>();
        }

        private ICollection<int> GetSavedGroupIds()
        {
            if (ViewModel?.ExtraData?.ContainsKey("SavedGroupIds") == true)
                return ViewModel.ExtraData["SavedGroupIds"] as ICollection<int> ?? new HashSet<int>();
            return new HashSet<int>();
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
