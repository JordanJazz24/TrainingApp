using System.Collections.Generic;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training.Pod
{
    // Secuencia de los botones del POD (AUD-74, E5): un botón que no es el del paso actual es un
    // intento fallido en Assessment, un aviso en Práctica libre y se ignora en Guided (S2).
    internal sealed class PodSequence
    {
        private readonly PodCore _core;
        private readonly PodForcedAdvance _forced;

        public PodSequence(PodCore core, PodForcedAdvance forced)
        {
            _core = core;
            _forced = forced;
        }

        // null si el botón es el del paso actual. Si no: según ModePolicy, intento fallido del
        // paso actual (Assessment), aviso sin intento (Práctica libre) o se ignora (Guided).
        // action: el botón presionado, para el registro de actividad (spec 013, AC-20).
        // selectedRowIds: la selección del POD; sin ella el MES no tiene SFC (spec 017).
        internal PodCommandResult CheckSequence(PodContext ctx, StepType clickedStepType, string action,
            ICollection<string> selectedRowIds = null)
        {
            var step = ctx.CurrentStep;
            if (step.StepType == clickedStepType) return null;
            // Spec 014 (P-7): el botón [Log NC] abre cualquiera de los pasos de Log NC.
            if (StepTypes.IsLogNC(clickedStepType) && StepTypes.IsLogNC(step.StepType))
                return null;

            return OutOfSequence(ctx, action, ctx.Policy.OutOfSequence, clickedStepType, selectedRowIds);
        }

        // Click que no es el del paso actual. RecordAttempt: intento fallido del paso actual y,
        // si avanzó forzado, el POD queda como si el paso se hubiera hecho. Notice: el aviso sin
        // intento. Ignore (o con la sesión ya cerrada): el click se ignora. Spec 017: en
        // RecordAttempt y Notice, además, el mensaje que daría el MES (MesSequenceRule), con el
        // estado de antes del intento; no cambia la calificación (D-92).
        internal PodCommandResult OutOfSequence(PodContext ctx, string action, OutOfSequenceHandling handling,
            StepType? clickedStepType = null, ICollection<string> selectedRowIds = null)
        {
            var step   = ctx.CurrentStep;
            var result = new PodCommandResult { OutOfSequence = true };
            if (ctx.Session.Status != SessionStatus.InProgress || handling == OutOfSequenceHandling.Ignore)
                return result;

            if (clickedStepType.HasValue)
            {
                var errors = MesSequenceRule.For(clickedStepType.Value,
                    SequenceState(ctx, clickedStepType.Value, selectedRowIds));
                if (errors.Count > 0) result.MesErrors = errors;
            }

            if (handling == OutOfSequenceHandling.Notice)
            {
                // Práctica libre (D-101): el mensaje del MES reemplaza "Ese no es el paso actual.".
                result.NotCurrentStepNotice = result.MesErrors == null;
                return result;
            }

            result.Step = _core.RecordAttempt(ctx, step, isCorrect: false, AttemptReason.OutOfSequence, action);
            if (result.Step.Outcome == StepSubmitOutcome.FailedForced)
                result.PodChanged = _forced.RestoreAfterForcedAdvance(ctx, step);
            return result;
        }

        // Estado del POD que mira el MES para rechazar el botón (spec 017, C-2). Los catálogos de
        // DC solo se leen para Complete, el único que los usa (M-7).
        private static MesSequenceState SequenceState(PodContext ctx, StepType clickedStepType,
            ICollection<string> selectedRowIds)
        {
            bool forComplete = clickedStepType == StepType.Complete;
            return MesSequenceState.Build(ctx.Guide.Steps, ctx.Session.CurrentStepOrder, ctx.Attempts,
                ctx.State.Order, ctx.State.DataCollection,
                forComplete ? ctx.DataCollectionGroups : null,
                forComplete ? ctx.DataCollectionParameters : null,
                selectedRowIds);
        }
    }
}
