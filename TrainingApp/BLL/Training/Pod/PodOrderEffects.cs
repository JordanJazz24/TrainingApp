using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training.Pod
{
    // Efectos sobre la orden simulada del POD que comparten el avance forzado, la reconstrucción
    // y los comandos (AUD-74, E5): crear la orden, iniciar piezas, el split por defecto, la
    // disposición del hallazgo de Log NC y sacar filas. El estado del POD solo lo cambian
    // PodService y sus colaboradores de BLL/Training/Pod/; PodCore.SaveState es el único que lo
    // persiste.
    internal sealed class PodOrderEffects
    {
        private readonly PodCore _core;

        public PodOrderEffects(PodCore core)
        {
            _core = core;
        }

        internal static bool HasLogNC(TrainingGuide guide)
            => guide.Steps.Any(s => StepTypes.IsLogNC(s.StepType));

        // Orden nueva de la sesión, que reemplaza a la anterior (spec 005). El avance de
        // Assembly y DC apuntaba a filas de la orden vieja: se descarta con ella.
        internal void CreateOrder(PodContext ctx, string shopOrderInput)
        {
            ctx.State.Order = SimulatedOrder.Create(shopOrderInput, PodDefaults.MaterialNumber(ctx.Area),
                PodDefaults.MaterialDescription(ctx.Product, ctx.Area), ctx.Area.SerializationType);
            ctx.State.Assembly       = null;
            ctx.State.DataCollection = null;
            ctx.State.NcFindings     = null;
            ctx.State.AssembledComponents = null;
            _core.SaveState(ctx);
        }

        // Piezas iniciadas con el Start. Serialized: las elegidas en Browse Work List (todas si
        // el paso avanzó forzado, E9 de la spec 008). NonSerialized: el lote ya nace Active.
        // Spec 014: con el Start se generan los hallazgos de los pasos de Log NC.
        internal void StartPieces(PodContext ctx, ICollection<string> browseRowIds, bool startAll)
        {
            var order = ctx.State.Order;
            if (order == null) return;

            bool serialized = ctx.Area.SerializationType == SerializationType.Serialized;
            bool logNC = HasLogNC(ctx.Guide);
            if (!serialized && !logNC) return;

            if (serialized)
                order.Activate(startAll || browseRowIds == null ? order.Rows : order.FindRows(browseRowIds));

            if (logNC)
            {
                ctx.State.NcFindings = NcFindingGenerator.Generate(ctx.Guide.Steps, order.ActiveRows(),
                    ctx.Area.SerializationType, ctx.NCCodes,
                    ctx.Guide.Steps.Any(s => s.StepType == StepType.LogNCFastRepair) ? ctx.AssemblyComponents : null,
                    PodDefaults.NonSerializedLotQty, new Random());
                if (!serialized)
                    foreach (var finding in ctx.State.NcFindings)
                        finding.SFCCode = order.SFCCode;
            }
            _core.SaveState(ctx);
        }

        // Disposición del hallazgo (spec 014, P-4): el desecho rápido y Return to any previous
        // sacan su pieza del POD (D-54); la reparación rápida y el Local Rework la dejan (D-47).
        // La pieza es la enviada si es la del hallazgo (StepValidator.IsFindingPiece); si no
        // (avance forzado y reconstrucción), la del hallazgo o, en NonSerialized, la primera fila
        // del split. Devuelve las filas que salieron.
        internal IList<SimulatedOrderRow> ApplyFindingDisposition(PodContext ctx, NcFinding finding,
            IList<SimulatedOrderRow> selected)
        {
            var order = ctx.State.Order;
            if (finding == null || order == null || !RemovesPiece(finding))
                return new List<SimulatedOrderRow>();

            var piece = StepValidator.IsFindingPiece(finding, selected)
                ? selected[0]
                : finding.RowId != null
                    ? order.FindActiveRows(new[] { finding.RowId }).FirstOrDefault()
                    : order.ActiveRows().FirstOrDefault(r => r.IsFromSplit);

            return piece == null
                ? new List<SimulatedOrderRow>()
                : RemoveRows(ctx, new List<SimulatedOrderRow> { piece });
        }

        private static bool RemovesPiece(NcFinding finding)
            => finding.Method == NCMethod.FastScrap ||
               (finding.Method == NCMethod.ExtendedRepair && finding.Disposition == NcDisposition.ReturnToPrevious);

        internal IList<SimulatedOrderRow> RemoveRows(PodContext ctx, IList<SimulatedOrderRow> rows)
        {
            if (rows.Count == 0) return rows;
            ctx.State.Order.Remove(rows);
            _core.SaveState(ctx);
            return rows;
        }

        // SFC Split forzado sin ningún split hecho (auditoría AUD-03, decisión D-1): queda una
        // fila de split por defecto, porque en NonSerialized Log NC exige scrapear una.
        // Enmienda 2 de la spec 014: con hallazgo, la cantidad es la suya (también al reconstruir).
        internal bool ApplyDefaultSplit(PodContext ctx)
        {
            var order = ctx.State.Order;
            if (order == null || order.HasSplit) return false;

            int qty = ctx.SplitFinding?.UnitCount > 0 ? ctx.SplitFinding.UnitCount : PodDefaults.DefaultSplitQty;
            var lot = order.Rows.FirstOrDefault(r => !r.IsFromSplit && r.Qty > qty);
            if (lot == null) return false;

            order.Split(lot, qty);
            _core.SaveState(ctx);
            return true;
        }
    }
}
