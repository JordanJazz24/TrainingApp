using System.Collections.Generic;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;
using static TrainingApp.BLL.Training.Pod.PodSelection;

namespace TrainingApp.BLL.Training.Pod
{
    // Complete, Logout, las acciones del MES que no son un paso y el Ingreso a MES con la
    // limpieza al salir (AUD-74, E5).
    internal sealed class PodCompletionCommands
    {
        private readonly PodCore _core;
        private readonly PodOrderEffects _effects;
        private readonly PodSequence _sequence;

        public PodCompletionCommands(PodCore core, PodOrderEffects effects, PodSequence sequence)
        {
            _core = core;
            _effects = effects;
            _sequence = sequence;
        }

        // Complete cierra las filas que quedan en el POD. Serialized (spec 008): algunas piezas
        // es un aviso, sin intento; ninguna es un intento fallido. NonSerialized: sin la fila
        // seleccionada es un intento fallido (spec 007).
        public PodCommandResult Complete(PodContext ctx, ICollection<string> selectedRowIds)
        {
            var sequence = _sequence.CheckSequence(ctx, StepType.Complete, AttemptActions.Complete, selectedRowIds);
            if (sequence != null) return sequence;

            var selected = SelectedRows(ctx, selectedRowIds);
            if (ctx.Area.SerializationType == SerializationType.Serialized)
            {
                var selection = PieceSelectionRule.Evaluate(StepType.Complete, selected.Count, ActiveCount(ctx));
                if (selection == PieceSelectionResult.MissingPieces)
                    return PodCommandResult.WithNotice(PodNotice.SelectAllPieces);
                if (PieceSelectionRule.CountsAsFailedAttempt(StepType.Complete, selection))
                    return FailedComplete(ctx, selected.Count == 0);
            }
            else if (!IsSingleRow(selected))
            {
                return FailedComplete(ctx, selected.Count == 0);
            }

            // Spec 015 (D-22): Complete ya no cierra la sesión (sigue Logout); las filas
            // completadas salen del POD, como el SFC que pasa a la siguiente operación.
            var result = new PodCommandResult { Step = _core.SubmitStep(ctx, StepAnswers.Complete) };
            if (result.Step.Outcome != StepSubmitOutcome.Incorrect)
                result.Completed = _effects.RemoveRows(ctx, selected);
            return result;
        }

        // Complete sin la selección del paso: intento fallido. Si avanza forzado, el POD queda
        // como tras un Complete hecho (D-22): sin filas. Sin ninguna fila, además, el 22506 del
        // MES (spec 017, M-4).
        private PodCommandResult FailedComplete(PodContext ctx, bool nothingSelected)
        {
            var result = _core.FailedAttempt(ctx, PodAttemptReason.NoRowToComplete, AttemptActions.Complete);
            if (nothingSelected)
                result.MesErrors = new List<MesError> { MesMessage.Error(MesMessageId.SfcMissingStart) };
            if (result.Step.Outcome == StepSubmitOutcome.FailedForced)
                result.Completed = _effects.RemoveRows(ctx, ctx.State.Order?.ActiveRows() ?? new List<SimulatedOrderRow>());
            return result;
        }

        // Ícono Logout del header (spec 015): antes del último paso es un botón más del POD (S2):
        // intento fallido del paso actual en Assessment, ignorado en Guided. En el último paso
        // ya no cierra la sesión (spec 016, D-65): empieza la limpieza ("You have logged off"),
        // sin intento; el paso lo cierra el script (SubmitMesAction). Un segundo clic no cambia
        // nada.
        public PodCommandResult ClickLogout(PodContext ctx)
        {
            var sequence = _sequence.CheckSequence(ctx, StepType.Logout, AttemptActions.Logout);
            if (sequence != null) return sequence;

            if (ctx.State.Logout == null)
            {
                ctx.State.Logout = new LogoutProgress();
                _core.SaveState(ctx);
            }
            return new PodCommandResult { MesScreen = ctx.State.Logout.ResumeScreen() };
        }

        // Work Instructions, Reports, Place Back in Queue, Tool List y las opciones de relleno
        // de Activities no tienen lógica: en Assessment el click es un intento fallido del paso
        // actual (S2); en Guided esos botones están deshabilitados. action: el botón (catálogo
        // de AttemptActions), para el registro de actividad.
        // En Práctica libre no hacen nada, sin aviso (spec 012, P-5).
        public PodCommandResult ClickNonStepAction(PodContext ctx, string action)
            => _sequence.OutOfSequence(ctx, action, ctx.Policy.NoLogicButtons == NoLogicButtonHandling.RecordAttempt
                ? OutOfSequenceHandling.RecordAttempt
                : OutOfSequenceHandling.Ignore);

        // Cada pantalla del Ingreso se califica al enviarla (D-68, como el Split de Moldeo):
        // MesLoginRule decide y aquí se aplica. Un fallo registra el intento y dice a qué
        // pantalla volver (P-4); el 4.º en Assessment fuerza el avance y aparece el POD (P-5).
        // Abrir el Repositorio antes del script es de secuencia: en Guided se ignora, como un
        // botón fuera de secuencia (S2). Un envío fuera del paso (doble envío tras Log On) se
        // ignora. Uno que no corresponde al avance guardado (la Session se perdió, otra pestaña
        // o un envío manipulado) no registra nada y vuelve a la pantalla del avance. El Code1
        // contra el que se califica es el del usuario (D-79).
        public PodCommandResult SubmitMesAction(PodContext ctx, MesAction action,
            string code1 = null, string resource = null, string password = null)
        {
            if (ctx.IsLogoutCleanup)
                return SubmitLogoutCleanup(ctx, action, code1);

            if (ctx.Session.Status != SessionStatus.InProgress ||
                ctx.CurrentStepOrDefault?.StepType != StepType.MesLogin)
                return new PodCommandResult();

            var progress = ctx.MesLogin;
            var outcome  = MesLoginRule.Evaluate(progress, action, ctx.Code1, code1, resource, password);

            switch (outcome.Kind)
            {
                case MesOutcomeKind.Ignored:
                case MesOutcomeKind.Invalid:
                    return new PodCommandResult { MesScreen = progress.ResumeScreen() };

                case MesOutcomeKind.Failed:
                    return MesFailure(ctx, outcome, action, AttemptActions.ForMesAction(action));
            }

            if (action == MesAction.LogOn)
                return new PodCommandResult
                {
                    Step = _core.RecordAttempt(ctx, ctx.CurrentStep, isCorrect: true, AttemptReason.None,
                        AttemptActions.ForMesAction(action))
                };

            progress.Apply(action);
            ctx.State.MesLogin = progress;
            _core.SaveState(ctx);
            return new PodCommandResult { MesScreen = outcome.Screen };
        }

        // Fallo de una pantalla del Ingreso o de la limpieza: uno de secuencia en Guided se
        // ignora sin intento, como un botón fuera de secuencia (S2). En Práctica libre solo el
        // Repositorio fuera de su turno es "otro paso" (aviso sin intento, P-4). El resto registra el intento y dice a qué pantalla
        // volver (P-4).
        private PodCommandResult MesFailure(PodContext ctx, MesOutcome outcome, MesAction mesAction, string action)
        {
            bool outOfSequence = outcome.Reason == AttemptReason.OutOfSequence;
            var  handling      = ctx.Policy.OutOfSequence;
            if (outOfSequence && (handling == OutOfSequenceHandling.Ignore ||
                                  handling == OutOfSequenceHandling.Notice && mesAction == MesAction.OpenRepository))
                return new PodCommandResult
                {
                    OutOfSequence        = true,
                    NotCurrentStepNotice = handling == OutOfSequenceHandling.Notice,
                    MesScreen            = outcome.Screen
                };

            return new PodCommandResult
            {
                OutOfSequence = outOfSequence,
                Step          = _core.RecordAttempt(ctx, ctx.CurrentStep, isCorrect: false, outcome.Reason, action),
                MesScreen     = outcome.Screen
            };
        }

        // Limpieza al salir (spec 016, P-6), dentro del paso Logout: cerrar Edge lleva al
        // escritorio; el script con el Code1 propio cierra el paso y la sesión (el resumen sale
        // al cerrar la consola, P-12). Abrir el Repositorio (en Guided se ignora, como en el
        // Ingreso) o un Code1 ajeno es intento fallido de Logout; el 4.º en Assessment cierra la
        // sesión con su resumen, como antes. Aplica a toda sesión, sea cual sea la versión de
        // su guía: es del código.
        private PodCommandResult SubmitLogoutCleanup(PodContext ctx, MesAction action, string code1)
        {
            var progress = ctx.State.Logout;
            var outcome  = LogoutCleanupRule.Evaluate(progress, action, ctx.Code1, code1);

            switch (outcome.Kind)
            {
                case MesOutcomeKind.Ignored:
                case MesOutcomeKind.Invalid:
                    return new PodCommandResult { MesScreen = progress.ResumeScreen() };

                case MesOutcomeKind.Failed:
                    return MesFailure(ctx, outcome, action, AttemptActions.ForLogoutCleanup(action));
            }

            if (action == MesAction.RunScript)
                return new PodCommandResult
                {
                    Step      = _core.RecordAttempt(ctx, ctx.CurrentStep, isCorrect: true, AttemptReason.None,
                        AttemptActions.ForLogoutCleanup(action)),
                    MesScreen = outcome.Screen
                };

            progress.Apply(action);
            _core.SaveState(ctx);
            return new PodCommandResult { MesScreen = outcome.Screen };
        }
    }
}
