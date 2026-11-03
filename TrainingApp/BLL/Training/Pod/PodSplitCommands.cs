using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;
using static TrainingApp.BLL.Training.Pod.PodCore;
using static TrainingApp.BLL.Training.Pod.PodSelection;

namespace TrainingApp.BLL.Training.Pod
{
    // SFC Split (AUD-74, E5).
    internal sealed class PodSplitCommands
    {
        private readonly PodCore _core;
        private readonly PodOrderEffects _effects;
        private readonly PodSequence _sequence;

        public PodSplitCommands(PodCore core, PodOrderEffects effects, PodSequence sequence)
        {
            _core = core;
            _effects = effects;
            _sequence = sequence;
        }

        // Abrir SFC Split exige una fila seleccionada del POD (formato, sin intento): sin ella,
        // el 12906 del MES (spec 017).
        public PodCommandResult OpenSplit(PodContext ctx, ICollection<string> selectedRowIds)
        {
            return _sequence.CheckSequence(ctx, StepType.SFC_Split, AttemptActions.SFCSplit, selectedRowIds)
                ?? (IsSingleRow(SelectedRows(ctx, selectedRowIds))
                    ? new PodCommandResult()
                    : PodCommandResult.WithMesError(MesMessage.Error(MesMessageId.SfcMissing)));
        }

        // Divide una fila; la cantidad tiene que dejar algo en la original. Crea un SFC nuevo de
        // la misma Shop Order (enmienda 2026-10-01 de la spec 005). El paso se resuelve al
        // cerrar. Con hallazgo (enmienda 3 de la spec 014), la cantidad es una decisión: otra que
        // no sea la del hallazgo es intento fallido y el split no se aplica (forzado: el split
        // de N); un segundo split es un aviso. qty null: el campo vacío o sin un número; llega
        // como decimal para que el MES juzgue también una cantidad enorme o con decimales.
        public PodCommandResult Split(PodContext ctx, string rowId, decimal? qty)
        {
            if (ctx.CurrentStep.StepType != StepType.SFC_Split)
                return PodCommandResult.WithNotice(PodNotice.SplitNotActive);

            // Solo una fila del POD (Active): el id llega del cliente.
            var order = ctx.State.Order;
            var row   = order?.FindActiveRows(new[] { rowId }).FirstOrDefault();
            if (row == null)
                return PodCommandResult.WithNotice(PodNotice.InvalidSplit);

            // Formato con el mensaje del MES (spec 017, M-14 a M-16), sin intento.
            var qtyError = SplitQtyError(qty, row.Qty);
            if (qtyError != null)
                return PodCommandResult.WithMesError(qtyError);

            // La fila entera no deja nada en la original y las piezas no se parten: aviso del
            // simulador, sin intento.
            if (qty.Value == row.Qty || qty.Value != decimal.Truncate(qty.Value))
                return PodCommandResult.WithNotice(PodNotice.InvalidSplitQty);
            int splitQty = (int)qty.Value;

            var finding = ctx.SplitFinding;
            if (finding != null)
            {
                if (order.HasSplit)
                    return PodCommandResult.WithNotice(PodNotice.SplitAlreadyDone);
                if (splitQty != finding.UnitCount)
                {
                    EnsureInProgress(ctx);
                    var result = new PodCommandResult
                    {
                        Step = _core.RecordAttempt(ctx, ctx.CurrentStep, isCorrect: false,
                            AttemptReason.WrongDecision, AttemptActions.SFCSplitSplit)
                    };
                    if (result.Step.Outcome == StepSubmitOutcome.FailedForced)
                        result.PodChanged = _effects.ApplyDefaultSplit(ctx);
                    return result;
                }
            }

            string newSfc = order.Split(row, splitQty);
            _core.SaveState(ctx);
            return new PodCommandResult { NewSfcCode = newSfc, RemainingQty = row.Qty };
        }

        // Cantidad del split que el MES rechaza, en su orden: vacía, no positiva, mayor a la
        // disponible. null si el MES la acepta.
        private static MesError SplitQtyError(decimal? qty, int availableQty)
        {
            if (!qty.HasValue)
                return MesMessage.Error(MesMessageId.SplitQtyMissing);
            if (qty.Value <= 0)
                return MesMessage.Error(MesMessageId.SplitQtyNotPositive);
            if (qty.Value > availableQty)
                return MesMessage.Error(MesMessageId.SplitQtyAboveAvailable,
                    ("n", qty.Value.ToString("0.######", CultureInfo.InvariantCulture)),
                    ("m", availableQty.ToString(CultureInfo.InvariantCulture)));
            return null;
        }

        // Close del SFC Split: hace falta al menos un split; sin él es un aviso, sin intento.
        public PodCommandResult CloseSplit(PodContext ctx, string answer)
        {
            EnsureCurrentStep(ctx, StepType.SFC_Split);
            if (ctx.State.Order == null || !ctx.State.Order.HasSplit)
                return PodCommandResult.WithNotice(PodNotice.SplitRequired);

            return new PodCommandResult { Step = _core.SubmitStep(ctx, answer) };
        }
    }
}
