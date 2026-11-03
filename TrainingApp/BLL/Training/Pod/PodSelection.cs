using System.Collections.Generic;
using System.Text.RegularExpressions;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training.Pod
{
    // La selección de filas del POD que manda el cliente y las reglas de formato que la usan
    // (AUD-74, E5). Sin estado.
    internal static class PodSelection
    {
        // Filas del POD que mandó el cliente: solo cuentan las que existen y están Active.
        internal static IList<SimulatedOrderRow> SelectedRows(PodContext ctx, ICollection<string> rowIds)
            => ctx.State.Order?.FindActiveRows(rowIds) ?? new List<SimulatedOrderRow>();

        internal static int ActiveCount(PodContext ctx) => ctx.State.Order?.ActiveRows().Count ?? 0;

        // Pasos de una sola fila (NonSerialized): exactamente una fila seleccionada.
        internal static bool IsSingleRow(IList<SimulatedOrderRow> selected) => selected.Count == 1;

        // Barcode de Assembly: numérico, como lo que imprime la etiqueta del componente.
        internal static bool IsCompliantBarcode(string barcode)
            => !string.IsNullOrWhiteSpace(barcode) && Regex.IsMatch(barcode.Trim(), @"^[0-9]+$");

        // Assembly y DC trabajan la selección del POD (spec 008, punto 6): en Serialized todas
        // las piezas juntas; en NonSerialized, la fila del lote. Sin ninguna fila es el 12906 del
        // MES (spec 017); la selección parcial de Serialized, el aviso del simulador. null si la
        // selección es válida.
        internal static PodCommandResult CheckPodSelection(PodContext ctx, StepType stepType,
            ICollection<string> selectedRowIds)
        {
            var selected = SelectedRows(ctx, selectedRowIds);
            if (ctx.Area.SerializationType == SerializationType.Serialized)
            {
                switch (PieceSelectionRule.Evaluate(stepType, selected.Count, ActiveCount(ctx)))
                {
                    case PieceSelectionResult.Valid:
                        return null;
                    case PieceSelectionResult.NothingSelected:
                        return PodCommandResult.WithMesError(MesMessage.Error(MesMessageId.SfcMissing));
                    default:
                        return PodCommandResult.WithNotice(PodNotice.SelectAllPieces);
                }
            }

            return IsSingleRow(selected) ? null : PodCommandResult.WithMesError(MesMessage.Error(MesMessageId.SfcMissing));
        }
    }
}
