using System;
using System.Collections.Generic;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;
using static TrainingApp.BLL.Training.Pod.PodCore;
using static TrainingApp.BLL.Training.Pod.PodSelection;

namespace TrainingApp.BLL.Training.Pod
{
    // Log NC (AUD-74, E5): desecho rápido y reparaciones rápida y extendida.
    internal sealed class PodLogNcCommands
    {
        private readonly PodCore _core;
        private readonly PodOrderEffects _effects;
        private readonly PodSequence _sequence;

        public PodLogNcCommands(PodCore core, PodOrderEffects effects, PodSequence sequence)
        {
            _core = core;
            _effects = effects;
            _sequence = sequence;
        }

        // Abrir Log NC (el botón vale en cualquier paso de Log NC, spec 014 P-7). Serialized:
        // scrapear todas las piezas es un aviso (tiene que quedar una para Complete, spec 008).
        // Sin una fila scrapeable es un intento fallido del paso (spec 007): no hay gate en el
        // cliente, el click tiene que llegar acá para contar. La pieza no se califica al abrir
        // (P-3): abrir con otra pieza y salir con Return to POD no cuenta.
        public PodCommandResult OpenLogNC(PodContext ctx, ICollection<string> selectedRowIds)
        {
            var sequence = _sequence.CheckSequence(ctx, StepType.LogNC, AttemptActions.LogNC, selectedRowIds);
            if (sequence != null) return sequence;

            var finding = EnsureFinding(ctx);
            var scrappable = ScrappableRows(ctx, selectedRowIds, finding.Method, out bool noPieceLeft);
            if (noPieceLeft)
                return PodCommandResult.WithNotice(PodNotice.NoPieceLeft);
            if (scrappable.Count == 0)
            {
                var failed = _core.FailedAttempt(ctx, PodAttemptReason.NoScrappableRow, AttemptActions.LogNC);
                // Spec 017 (M-5): sin ninguna fila, además, el 12906 del MES.
                if (SelectedRows(ctx, selectedRowIds).Count == 0)
                    failed.MesErrors = new List<MesError> { MesMessage.Error(MesMessageId.SfcMissing) };
                // P-5: el paso forzado aplica la disposición del hallazgo.
                if (failed.Step.Outcome == StepSubmitOutcome.FailedForced)
                    ResolveFinding(ctx, finding, null, failed);
                return failed;
            }

            return new PodCommandResult();
        }

        // Add-Done de NC Data Entry (o un botón equivocado del modal). Spec 014: califica la
        // pieza y el código contra el hallazgo del paso (StepValidator). Resuelto (correcto o
        // forzado, P-5) se aplica la disposición del hallazgo.
        public PodCommandResult SubmitLogNC(PodContext ctx, string answer, ICollection<string> selectedRowIds)
        {
            EnsureInProgress(ctx);
            if (!StepTypes.IsLogNC(ctx.CurrentStep.StepType))
                throw new InvalidOperationException("El comando no corresponde al paso actual.");
            var finding = EnsureFinding(ctx);

            var selected = SelectedRows(ctx, selectedRowIds);
            var result = new PodCommandResult { Step = _core.SubmitStep(ctx, answer, selected) };
            if (result.Step.Outcome == StepSubmitOutcome.Incorrect) return result;

            ResolveFinding(ctx, finding, selected, result);
            return result;
        }

        // Paso de Log NC resuelto: aplica la disposición del hallazgo y deja en el resultado lo
        // que la página muestra. Desecho: las filas que salieron. Reparación rápida (D-47): la
        // pieza sigue. Extendida (D-54): la operación del banner; Return to any previous ya sacó
        // la pieza del POD.
        private void ResolveFinding(PodContext ctx, NcFinding finding, IList<SimulatedOrderRow> selected,
            PodCommandResult result)
        {
            var removed = _effects.ApplyFindingDisposition(ctx, finding, selected);
            switch (finding.Method)
            {
                case NCMethod.FastScrap:
                    result.Scrapped = removed;
                    break;
                case NCMethod.FastRepair:
                    result.Repaired = finding;
                    break;
                case NCMethod.ExtendedRepair:
                    result.Reworked        = finding;
                    result.ReworkOperation = finding.Disposition == NcDisposition.ReturnToPrevious
                        ? ctx.PreviousOperationName : ctx.CurrentOperationName;
                    break;
            }
        }

        // El hallazgo del paso actual de Log NC, con su pieza en el POD. Faltar es un error de
        // invariante (spec 014, Edge Cases): los hallazgos son de piezas distintas y Complete
        // va después. No es un intento.
        private static NcFinding EnsureFinding(PodContext ctx)
        {
            var finding = ctx.NcFindingFor(ctx.CurrentStep);
            if (finding == null)
                throw new InvalidOperationException("El paso de Log NC no tiene hallazgo.");
            if (finding.RowId != null && ctx.State.Order?.FindActiveRows(new[] { finding.RowId }).Count != 1)
                throw new InvalidOperationException("La pieza del hallazgo ya no está en el POD.");
            return finding;
        }

        // NonSerialized: exactamente una fila, creada por el SFC Split; la fila original del
        // lote no se scrapea (spec 007). Serialized: las filas seleccionadas, siempre que quede
        // al menos una pieza para Complete (spec 008); noPieceLeft avisa que no quedaría ninguna.
        // Spec 014: la regla de dejar una pieza para Complete es del desecho. Las reparaciones
        // califican exactamente la pieza del hallazgo, y aunque Return to any previous la saque
        // del POD, NcFindingGenerator deja siempre una pieza para Complete.
        private static IList<SimulatedOrderRow> ScrappableRows(PodContext ctx,
            ICollection<string> selectedRowIds, NCMethod method, out bool noPieceLeft)
        {
            var selected = SelectedRows(ctx, selectedRowIds);
            noPieceLeft  = false;

            if (ctx.Area.SerializationType == SerializationType.Serialized)
            {
                if (method != NCMethod.FastScrap)
                    return selected;
                var selection = PieceSelectionRule.Evaluate(StepType.LogNC, selected.Count, ActiveCount(ctx));
                noPieceLeft = selection == PieceSelectionResult.NoPieceLeft;
                return selection == PieceSelectionResult.Valid ? selected : new List<SimulatedOrderRow>();
            }

            return IsSingleRow(selected) && selected[0].IsFromSplit
                ? selected
                : new List<SimulatedOrderRow>();
        }
    }
}
