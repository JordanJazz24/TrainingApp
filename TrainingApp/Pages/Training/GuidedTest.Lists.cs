using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Models;
using static TrainingApp.Pages.Training.GuidedTestHelpers;

namespace TrainingApp.Pages.Training
{
    // Página del POD (AUD-74, E6): Listas del POD (Work List y Browse) y avisos al operador.
    public partial class GuidedTest
    {
        // El POD solo muestra las filas iniciadas (Active). En NonSerialized el lote ya
        // nace Active; en Serialized cada pieza pasa a Active con el Start (spec 008).
        private void BindWorkList(Area area, TrainingSession session)
        {
            var rows = BuildPodWorkList(onlyActive: true);
            RptWorkList.DataSource = rows;
            RptWorkList.DataBind();
        }

        // Browse Work List muestra todas las filas de la orden (sin columna Status).
        private void BindBrowseList(Area area, TrainingSession session)
        {
            var rows = BuildPodWorkList(onlyActive: false);
            RptBrowseList.DataSource = rows;
            RptBrowseList.DataBind();
        }

        // Filas del POD que el operador seleccionó (HfSelectedRowIds, llenado en cliente por
        // selectWorkListRow). El cliente puede mandar cualquier cosa: solo cuentan los ids que
        // existen en la orden y están Active (spec 008).
        private IList<SimulatedOrderRow> FindSelectedOrderRows()
        {
            var order = Order;
            if (order == null) return new List<SimulatedOrderRow>();
            return order.FindActiveRows(ParseRowIds(HfSelectedRowIds.Value));
        }

        // Pasos de una sola fila (NonSerialized: DC, SFC Split, Log NC, Complete): la fila
        // seleccionada solo si hay exactamente una.
        private SimulatedOrderRow FindSelectedOrderRow()
        {
            var rows = FindSelectedOrderRows();
            return rows.Count == 1 ? rows[0] : null;
        }

        // Piezas elegidas en Browse Work List para el Start (HfBrowseRowIds).
        private IList<SimulatedOrderRow> FindBrowseSelectedRows(SimulatedOrder order)
        {
            return order.FindRows(ParseRowIds(HfBrowseRowIds.Value));
        }

        // Ids de filas que manda el cliente; la BLL descarta los que no son de la orden.
        private HashSet<string> SelectedIds() => ParseRowIds(HfSelectedRowIds.Value);

        private HashSet<string> BrowseSelectedIds() => ParseRowIds(HfBrowseRowIds.Value);

        private void ClearRowSelection()
        {
            HfSelectedRowIds.Value = string.Empty;
            HfSelectedSFC.Value    = string.Empty;
        }

        private IList<PodWorkListRow> BuildPodWorkList(bool onlyActive)
        {
            var order = Order;
            if (order == null) return new List<PodWorkListRow>();

            return order.Rows.Where(r => !onlyActive || r.IsActive).Select(r => new PodWorkListRow
            {
                RowId               = r.RowId,
                SFCCode             = r.SFCCode ?? order.SFCCode,
                MaterialDescription = order.MaterialDescription,
                Qty                 = r.Qty,
                Status              = r.Status ?? order.Status,
                ProcessLot          = order.ProcessLot,
                ShopOrder           = order.ShopOrderInput,
                IsSelected          = false,
                ScheduleStart       = DateTime.Today.ToString("MM/dd/yyyy"),
                PlannedCompletion   = DateTime.Today.AddDays(1).ToString("MM/dd/yyyy")
            }).ToList();
        }

        // ── Modal re-open & SweetAlert helpers ───────────────────────────

        // Mensajes del MES del banner del POD (spec 017, superficie B), elegidos por la BLL: una
        // fila por mensaje (varias solo con M-7, D-119). Los pinta podShowMesErrors como texto
        // plano; quedan hasta la acción siguiente (D-112).
        private void ShowMesErrors(IList<MesError> errors)
        {
            NoteMesError(errors[0]);
            var texts = errors.Select(e => e.Text).ToArray();
            // Varios mensajes son siempre del mismo id (M-7 por pieza): una sola explicación.
            var serializer = new JavaScriptSerializer();
            ClientScript.RegisterStartupScript(GetType(), "podMesError",
                "podShowMesErrors(" + serializer.Serialize(texts) + ", " +
                serializer.Serialize(MesHelp(errors[0])) + ");", true);
        }

        // Avisos de formato propios del simulador que decide la BLL (no cuentan como intento).
        private void ShowNotice(PodNotice notice)
        {
            var (title, text) = NoticeText(notice);
            ShowSweetAlert("warning", title, text);
        }

        protected void ShowSweetAlert(string type, string title, string text)
        {
            string script = string.Format(
                "Swal.fire({{title:'{0}',text:'{1}',icon:'{2}'}});",
                HttpUtility.JavaScriptStringEncode(title ?? string.Empty),
                HttpUtility.JavaScriptStringEncode(text ?? string.Empty), type);
            ClientScript.RegisterStartupScript(GetType(), "swal_" + type, script, true);
        }

        protected void ReopenModal(string modalId)
        {
            string script = string.Format("showModalSafe('{0}');", modalId);
            ClientScript.RegisterStartupScript(GetType(), "modal_" + modalId, script, true);
        }
    }
}
