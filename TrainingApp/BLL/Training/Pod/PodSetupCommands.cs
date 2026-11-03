using System;
using System.Collections.Generic;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;
using static TrainingApp.BLL.Training.Pod.PodCore;

namespace TrainingApp.BLL.Training.Pod
{
    // Resource Setup, Line Clearance y Start (AUD-74, E5).
    internal sealed class PodSetupCommands
    {
        private readonly PodCore _core;
        private readonly PodOrderEffects _effects;
        private readonly PodSequence _sequence;

        public PodSetupCommands(PodCore core, PodOrderEffects effects, PodSequence sequence)
        {
            _core = core;
            _effects = effects;
            _sequence = sequence;
        }

        // Cada apertura genera otros equipos precargados (spec 015): Retrieve los muestra y el
        // Save no puede dejar ninguno. Viajan con el control, sin otro postback.
        public PodCommandResult OpenResourceSetup(PodContext ctx)
        {
            var sequence = _sequence.CheckSequence(ctx, StepType.ResourceSetup, AttemptActions.ResourceSetup);
            if (sequence != null) return sequence;

            // Custom RS parte vacío (spec 020, D-186).
            ctx.State.PreloadedEquipment = ctx.ResourceSetupType == ResourceSetupType.Custom
                ? null : PreloadedEquipmentGenerator.Generate(new Random());
            _core.SaveState(ctx);
            return new PodCommandResult();
        }

        // Line Clearance se abre desde Activities ("Data Collection Standalone").
        public PodCommandResult OpenLineClearance(PodContext ctx)
            => _sequence.CheckSequence(ctx, StepType.LineClearance, AttemptActions.DCStandalone) ?? new PodCommandResult();

        // Resource Setup: califica el Setup State y, ya registrado el intento (AUD-06), crea la
        // orden con la Shop Order capturada, también en un intento fallido ("Open"), como
        // antes (spec 005). Si el paso avanzó forzado, la orden sale de la Shop Order genérica
        // e ignora lo capturado (S2 / spec 008, E8).
        // Con un equipo precargado en las filas, el control no aplica el formato (D-26, spec
        // 015): la Shop Order puede faltar y el intento se registra igual. Sin una Shop Order
        // válida y propia (no la del lote anterior), la orden no se crea: el paso sigue pendiente.
        // shopOrderInput es solo de la estándar: Custom RS trae su Shop Order en el Save (spec 020).
        public StepSubmitResult SubmitResourceSetup(PodContext ctx, string answer, string shopOrderInput)
        {
            EnsureCurrentStep(ctx, StepType.ResourceSetup);

            var custom = CustomResourceSetupAnswer.RequireForSession(ctx.ResourceSetupType, answer,
                ctx.ResourceSetupType == ResourceSetupType.Custom ? ctx.CustomResourceSetup.ToolGroups : null);
            if (custom != null)
                return SubmitCustomResourceSetup(ctx, answer, custom);

            bool validShopOrder = SimulatedOrder.IsValidShopOrder(shopOrderInput) &&
                ctx.State.PreloadedEquipment?.Contains(shopOrderInput) != true;
            // El control ya lo avisa como formato; si llega mal es una manipulación o un error
            // de programación: no se registra nada (auditoría AUD-36).
            if (!validShopOrder && !BuildValidator(ctx, StepType.ResourceSetup).HasPreloadedValue(answer))
                throw new ArgumentException("La Shop Order debe ser numérica.", nameof(shopOrderInput));

            var result = _core.SubmitStep(ctx, answer, null);
            if (result.Outcome == StepSubmitOutcome.FailedForced)
                _effects.CreateOrder(ctx, PodDefaults.DefaultShopOrder);
            else if (validShopOrder)
                _effects.CreateOrder(ctx, shopOrderInput);
            return result;
        }

        // Custom RS (spec 020), ya validado por RequireForSession. Sin asociar es intento fallido
        // y no hay orden; con la orden asociada se crea también en "Open" (D-193). El avance
        // forzado usa la orden por defecto, como la estándar.
        private StepSubmitResult SubmitCustomResourceSetup(PodContext ctx, string answer,
            CustomResourceSetupAnswer parsed)
        {
            var result = _core.SubmitStep(ctx, answer, null);
            string shopOrder = CustomResourceSetupAnswer.ShopOrderToCreate(result.Outcome, parsed);
            if (shopOrder != null)
                _effects.CreateOrder(ctx, shopOrder);
            return result;
        }

        // Click en Start. Avisos de formato, sin intento: sin orden (Resource Setup pendiente o
        // Session perdida); Serialized sin todas las piezas elegidas en Browse Work List (spec
        // 008); NonSerialized sin SFC o con uno que no es del lote (spec 011). Con checklist (Moldeo) pide abrirlo; sin checklist, el
        // Start se califica solo por secuencia y las piezas pasan a Active.
        public PodCommandResult ClickStart(PodContext ctx, ICollection<string> browseRowIds, string scannedSfc)
        {
            var sequence = _sequence.CheckSequence(ctx, StepType.Search, AttemptActions.Start);
            if (sequence != null) return sequence;

            var order = ctx.State.Order;
            if (order == null)
                return PodCommandResult.WithNotice(PodNotice.ResourceSetupRequired);

            if (ctx.Area.SerializationType == SerializationType.Serialized)
            {
                if (!IsBrowseSelectionValid(order, browseRowIds))
                {
                    // Sin ninguna pieza, además del aviso del simulador, el 22506 del MES
                    // (corrección del usuario al probar la E2 de la spec 017).
                    var notice = PodCommandResult.WithNotice(PodNotice.StartWholeOrder);
                    if (browseRowIds == null || order.FindRows(browseRowIds).Count == 0)
                        notice.MesErrors = new List<MesError> { MesMessage.Error(MesMessageId.SfcMissingStart) };
                    return notice;
                }
            }
            else if (string.IsNullOrWhiteSpace(scannedSfc))
            {
                return PodCommandResult.WithMesError(MesMessage.Error(MesMessageId.SfcMissingStart));
            }
            else if (!order.IsLotReference(scannedSfc))
            {
                // Spec 011 (enmienda E-2): TxtSFC es editable; el cliente ya lo avisa, esto
                // cubre una manipulación. Ningún lote ajeno se inicia. Sin otras órdenes en el
                // simulador, todo SFC ajeno es inexistente para el MES (spec 017, D-106).
                return PodCommandResult.WithMesError(MesMessage.Error(MesMessageId.InvalidSfc));
            }

            if (ctx.Area.RequiresStartChecklist)
                return new PodCommandResult { OpenChecklist = true };

            return new PodCommandResult { Step = SubmitStart(ctx, StepAnswers.Start, browseRowIds) };
        }

        // Checklist de Start (Moldeo, 4 modales): califica las respuestas y, resuelto el paso,
        // inicia las piezas. Solo se llega desde ClickStart, que ya validó la orden y la
        // selección; sin checklist, sin orden o con otra selección es una manipulación, no un
        // intento. La selección se vuelve a validar porque es la que se inicia (AUD-37).
        public StepSubmitResult SubmitStartChecklist(PodContext ctx, string answerJson,
            ICollection<string> browseRowIds)
        {
            EnsureCurrentStep(ctx, StepType.Search);
            var order = ctx.State.Order;
            if (!ctx.Area.RequiresStartChecklist || order == null)
                throw new InvalidOperationException("El Start de esta área no tiene checklist pendiente.");
            if (ctx.Area.SerializationType == SerializationType.Serialized &&
                !IsBrowseSelectionValid(order, browseRowIds))
                throw new InvalidOperationException("La selección de piezas no es la que validó el Start.");

            return SubmitStart(ctx, answerJson, browseRowIds);
        }

        // Serialized (spec 008): el Start exige todas las piezas elegidas en Browse Work List.
        private static bool IsBrowseSelectionValid(SimulatedOrder order, ICollection<string> browseRowIds)
            => PieceSelectionRule.Evaluate(StepType.Search,
                   order.FindRows(browseRowIds).Count, order.Rows.Count) == PieceSelectionResult.Valid;

        private StepSubmitResult SubmitStart(PodContext ctx, string answer, ICollection<string> browseRowIds)
        {
            var result = _core.SubmitStep(ctx, answer);
            if (result.Outcome != StepSubmitOutcome.Incorrect)
                _effects.StartPieces(ctx, browseRowIds, startAll: result.Outcome == StepSubmitOutcome.FailedForced);
            return result;
        }
    }
}
