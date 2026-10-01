using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    // Assembly (spec 008): la Component List es el BOM de la operación. Cada componente se
    // ensambla con un escaneo que cubre toda su Qty y se aplica a todas las piezas
    // seleccionadas. El paso lo completa la BLL cuando todo el BOM quedó ensamblado.
    public partial class AssemblyControl : UserControl, IStepControl
    {
        public StepViewModel ViewModel { get; set; }

        // Parte del contrato IStepControl; Assembly nunca envía un Answer único (ver
        // OnComponentAssembled), así que el evento no guarda suscriptores.
        public event EventHandler<StepSubmitEventArgs> OnStepSubmit { add { } remove { } }

        // El operador escaneó el material de un componente y presionó Add.
        public event EventHandler<ComponentAssembledEventArgs> OnComponentAssembled;

        // El operador sale al POD sin terminar: el host cierra el control (el avance se conserva).
        public event EventHandler OnReturnToPod;

        // La confirmación del Add se ve solo en la respuesta de ese Add: Panel.Visible persiste
        // en ViewState, así que se apaga en cada request antes de los eventos.
        protected void Page_Load(object sender, EventArgs e)
        {
            PnlAddConfirm.Visible = false;
        }

        // Add con componentes pendientes (Fase 6.5): el modal queda abierto con la confirmación
        // arriba a la izquierda, como el MES real. Con el último componente el host vuelve al POD.
        public void ShowAddConfirmation(int componentId)
        {
            var component = GetComponents().FirstOrDefault(c => c.Id == componentId);
            string code = component == null ? string.Empty : component.PartNumber + "/" + component.Revision;
            LitAddConfirm.Text    = Page.Server.HtmlEncode("Component " + code + " assembled");
            PnlAddConfirm.Visible = true;
            Page.ClientScript.RegisterStartupScript(GetType(), "asmAddConfirm",
                "asmReopenAfterAdd('" + componentId + "');", true);
        }

        public void Initialize()
        {
            TxtBarcode.Text     = string.Empty;
            HfComponentId.Value = string.Empty;

            if (ViewModel == null) return;

            // "Apply to all SFCs" solo aparece cuando el escaneo cubre varias piezas.
            int selectedPieces = ViewModel.ExtraData.ContainsKey("SelectedPieceCount")
                ? Convert.ToInt32(ViewModel.ExtraData["SelectedPieceCount"]) : 1;
            PnlApplyAll.Visible = selectedPieces > 1;

            // Batch Number del modal = lote de las piezas (columna Process Lot del POD).
            HfProcessLot.Value = ViewModel.ExtraData.ContainsKey("ProcessLot")
                ? ViewModel.ExtraData["ProcessLot"]?.ToString() ?? string.Empty
                : string.Empty;

            BindComponents();
        }

        // Vuelve a pintar la Component List con el avance actual (Rem Qty y Assemble habilitado).
        public void BindComponents()
        {
            var assembled = GetAssembledIds();
            var rows = GetComponents().Select(c => new
            {
                c.Id,
                c.PartNumber,
                c.Revision,
                c.Description,
                Code        = c.PartNumber + "/" + c.Revision,
                Qty         = FormatQty(c.Qty),
                RemQty      = assembled.Contains(c.Id) ? "0" : FormatQty(c.Qty),
                IsAssembled = assembled.Contains(c.Id)
            }).ToList();

            RptComponents.DataSource = rows;
            RptComponents.DataBind();
            LitTotal.Text   = rows.Count.ToString();
            LitTotalOf.Text = rows.Count.ToString();
        }

        protected void BtnAdd_Click(object sender, EventArgs e)
        {
            // ASP.NET dispara el click aunque el control ya esté cerrado (spec 006, E12).
            if (!Visible) return;

            int componentId;
            if (!int.TryParse(HfComponentId.Value, out componentId)) return;

            // Formato (sin intento): el cliente ya lo valida en assembleValidate().
            string barcode = TxtBarcode.Text.Trim();
            if (!Regex.IsMatch(barcode, @"^\d+$"))
            {
                TxtBarcode.Text = string.Empty;
                Page.ClientScript.RegisterStartupScript(GetType(), "swalAsm",
                    "Swal.fire({icon:'warning',title:'Barcode inválido'," +
                    "text:'Escanea un barcode numérico antes de presionar Add.'});" +
                    "showModalSafe('modalAssemble');", true);
                return;
            }

            TxtBarcode.Text = string.Empty;
            OnComponentAssembled?.Invoke(this,
                new ComponentAssembledEventArgs { ComponentId = componentId, Barcode = barcode });
        }

        protected void BtnReturnPOD_Click(object sender, EventArgs e)
        {
            OnReturnToPod?.Invoke(this, EventArgs.Empty);
        }

        // Qty sin ceros de relleno: 1 → "1", 0.0001 → "0.0001" (spec 008).
        private static string FormatQty(decimal qty)
            => qty.ToString("0.######", CultureInfo.InvariantCulture);

        private IList<AssemblyComponent> GetComponents()
        {
            if (ViewModel?.ExtraData?.ContainsKey("Components") == true)
                return ViewModel.ExtraData["Components"] as IList<AssemblyComponent>
                    ?? new List<AssemblyComponent>();
            return new List<AssemblyComponent>();
        }

        private ICollection<int> GetAssembledIds()
        {
            if (ViewModel?.ExtraData?.ContainsKey("AssembledIds") == true)
                return ViewModel.ExtraData["AssembledIds"] as ICollection<int> ?? new HashSet<int>();
            return new HashSet<int>();
        }
    }
}
