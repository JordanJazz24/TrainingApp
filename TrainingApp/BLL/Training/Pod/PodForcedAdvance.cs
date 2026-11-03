using System;
using System.Collections.Generic;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;
using static TrainingApp.BLL.Training.Pod.PodCore;

namespace TrainingApp.BLL.Training.Pod
{
    // Saltar paso (Práctica libre) y avance forzado (AUD-74, E5): un paso forzado nunca abrió su
    // control, así que el POD queda como si se hubiera hecho.
    internal sealed class PodForcedAdvance
    {
        private readonly PodCore _core;
        private readonly PodOrderEffects _effects;

        public PodForcedAdvance(PodCore core, PodOrderEffects effects)
        {
            _core = core;
            _effects = effects;
        }

        // ── Saltar paso e Ir a… (Práctica libre) ─────────────────────────

        // Spec 012 (P-9, D-83): adelanta cada paso desde el actual hasta el anterior a
        // targetOrder, uno por uno y cada uno en su transacción, como un avance forzado:
        // intento no correcto con Reason = Skipped y el POD como si el paso se hubiera hecho.
        // targetOrder = el último + 1 cierra la sesión (saltar Logout). Un destino que ya no
        // está adelante (doble clic, F5) se ignora; uno fuera de la guía o un modo sin salto
        // es un invariante violado, no un intento (enmienda 1).
        public PodCommandResult Skip(PodContext ctx, int targetOrder)
        {
            EnsureInProgress(ctx);
            if (!ctx.Policy.AllowsSkip)
                throw new InvalidOperationException("El modo de la sesión no permite saltar pasos.");
            if (targetOrder < 1 || targetOrder > ctx.Guide.Steps.Count + 1)
                throw new ArgumentOutOfRangeException(nameof(targetOrder), targetOrder, "El destino no es un paso de la guía.");

            var result = new PodCommandResult();
            while (ctx.Session.Status == SessionStatus.InProgress && ctx.Session.CurrentStepOrder < targetOrder)
            {
                var step = ctx.CurrentStep;
                result.Step    = _core.RecordAttempt(ctx, step, isCorrect: false, AttemptReason.Skipped,
                    AttemptActions.Skip, resolve: true);
                RestoreAfterForcedAdvance(ctx, step);
                result.Skipped = true;
            }
            return result;
        }

        // Un paso forzado por errores de secuencia nunca abrió su control: el POD queda como si
        // el paso se hubiera hecho, para que el resto de la evaluación se pueda completar.
        // Assembly y DC no necesitan nada: RecordAttempt ya descartó su avance parcial.
        // Devuelve true si la página tiene que volver a pintar el POD (Resource Setup y Start
        // siempre; SFC Split solo si se aplicó el split por defecto; Complete si quitó filas).
        internal bool RestoreAfterForcedAdvance(PodContext ctx, TrainingStep forced)
        {
            // Spec 014 (P-5): Log NC forzado aplica la disposición de su hallazgo.
            if (StepTypes.IsLogNC(forced.StepType))
                return _effects.ApplyFindingDisposition(ctx, ctx.NcFindingFor(forced), null).Count > 0;

            switch (forced.StepType)
            {
                case StepType.ResourceSetup:
                    // La orden sale de la Shop Order genérica (S2 / spec 008, E8).
                    _effects.CreateOrder(ctx, PodDefaults.DefaultShopOrder);
                    return true;

                case StepType.Search:
                    // E9 (spec 008): Start forzado inicia todas las piezas.
                    _effects.StartPieces(ctx, null, startAll: true);
                    return true;

                case StepType.SFC_Split:
                    return _effects.ApplyDefaultSplit(ctx);

                case StepType.Complete:
                    // Spec 015 (D-22): como un Complete hecho, las filas salen del POD.
                    var active = ctx.State.Order?.ActiveRows() ?? new List<SimulatedOrderRow>();
                    return _effects.RemoveRows(ctx, active).Count > 0;

                default:
                    return false;
            }
        }
    }
}
