using System.Collections.Generic;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    // Guion de la guía visual de Guided (spec 010): los subpasos de la UI de cada tipo de paso,
    // con los textos del anexo de docs/specs/010-plan.md. Es presentación (P-3): describe la
    // pantalla del paso que el servidor ya tiene como actual y no decide nada del flujo. Sin
    // System.Web, para poder probarlo sin la página.
    public sealed partial class GuideScriptBuilder
    {
        public GuideScript Build(PodContext ctx, GuideMoment moment)
        {
            var script = new GuideScript();

            // P-4: el paso se resolvió en este request y su confirmación sigue en pantalla. La
            // guía arranca en la cola de ese paso y sigue con el paso nuevo sin postback.
            if (moment.Kind == GuideMomentKind.StepResolved && moment.Step != null)
            {
                var resolved = ScriptFor(ctx, moment.Step, moment);
                if (resolved?.TailKey != null)
                {
                    script.Substeps.AddRange(resolved.Substeps);
                    script.EntryKey = resolved.TailKey;
                }
            }

            // Sesión completada: el resultado tiene su propia página (spec 025, P-11) y la guía no
            // la acompaña. Solo queda cerrar la consola de la limpieza (spec 016, P-12), que muestra
            // la salida del script y al cerrarla lleva al resultado. En cualquier otro cierre la
            // página redirige en el mismo request y no hay guion.
            if (moment.Kind == GuideMomentKind.SessionCompleted)
            {
                if (moment.Detail == GuideMoment.CleanupDetail && moment.Step != null)
                {
                    var cleanup = new StepScript(moment.Step, "Logout");
                    AddCloseConsole(cleanup, last: true);
                    script.Substeps.AddRange(cleanup.Substeps);
                    script.EntryKey = script.Substeps[0].Key;
                }
                return script;
            }

            var current = ctx.CurrentStepOrDefault;
            if (ctx.Session.Status == SessionStatus.InProgress && current != null)
            {
                var step = ScriptFor(ctx, current, moment);
                if (step != null && step.Substeps.Count > 0)
                {
                    script.Substeps.AddRange(step.Substeps);
                    if (script.EntryKey == null)
                        script.EntryKey = step.EntryFor(moment);
                }
            }

            AddMesErrorNote(script, moment);
            return script;
        }

        // Spec 017 (D-102): con un mensaje del MES en pantalla, la nota en español sobre su
        // superficie tiene prioridad en este request. Va justo antes de la entrada que el paso
        // ya eligió: "Continuar" sigue por ahí. Es una nota sobre ese subpaso: lleva su número y
        // sus elementos libres. El mensaje de SFC Split se cierra con su OK (cierra la ventana),
        // así que esa nota espera el clic en OK. Sin guion, sin entrada o con un paso recién
        // resuelto (su cola de confirmación), no hay nota.
        public static void AddMesErrorNote(GuideScript script, GuideMoment moment)
        {
            if (!moment.MesError.HasValue || !MesErrorNotes.Has(moment.MesError.Value)) return;
            if (moment.Kind == GuideMomentKind.StepResolved || moment.Kind == GuideMomentKind.SessionCompleted) return;
            int at = script.Substeps.FindIndex(s => s.Key == script.EntryKey);
            if (at < 0) return;

            var id    = moment.MesError.Value;
            var entry = script.Substeps[at];
            bool split = MesMessage.Get(id).Surface == MesSurface.SplitForm;
            var note = new GuideSubstep
            {
                Key       = entry.Key + ":mes-error",
                Anchor    = Anchor(split ? "split-mes-ok" : MesErrorNotes.AnchorName(id)),
                Mode      = split ? GuideMode.Click : GuideMode.Info,
                Free      = split ? new List<string> { Anchor(MesErrorNotes.AnchorName(id)) } : entry.Free,
                StepLabel = entry.StepLabel,
                Index     = entry.Index,
                Total     = entry.Total,
                Html      = MesErrorNotes.Html(id) +
                    (split ? " " + H("Presione ", B("OK"), ".") : string.Empty)
            };
            script.Substeps.Insert(at, note);
            script.EntryKey = note.Key;
        }

        // Guion de un paso, o null si su tipo todavía no tiene guion (la guía no se muestra).
        private static StepScript ScriptFor(PodContext ctx, TrainingStep step, GuideMoment moment)
        {
            switch (step.StepType)
            {
                case StepType.ResourceSetup:
                    return ctx.ResourceSetupType == ResourceSetupType.Custom
                        ? CustomResourceSetup(ctx, step) : ResourceSetup(ctx, step);
                case StepType.LineClearance:  return LineClearance(ctx, step);
                case StepType.Search:         return Start(ctx, step);
                case StepType.Assembly:       return Assembly(ctx, step, moment);
                case StepType.DataCollection: return DataCollection(ctx, step, moment);
                case StepType.SFC_Split:      return SfcSplit(ctx, step);
                case StepType.Complete:       return Complete(ctx, step);
                case StepType.Logout:         return Logout(ctx, step);
                case StepType.MesLogin:       return MesLogin(ctx, step);
                default:                      return StepTypes.IsLogNC(step.StepType) ? LogNC(ctx, step) : null;
            }
        }
    }
}
