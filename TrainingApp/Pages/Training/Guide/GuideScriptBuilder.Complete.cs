using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public sealed partial class GuideScriptBuilder
    {
        // Complete: las filas que quedan (Serialized: todas con Select All; NonSerialized: el
        // lote) y Complete. Sigue Logout (spec 015): el banner de Complete es la cola del paso.
        private static StepScript Complete(PodContext ctx, TrainingStep step)
        {
            // Cola: el banner de Complete (spec 015, D-22) antes de seguir con Logout.
            var s = new StepScript(step, "Complete") { TailName = "banner" };
            var order = ctx.State.Order;
            var rows = order?.ActiveRows() ?? new List<SimulatedOrderRow>();
            if (ctx.Area.SerializationType == SerializationType.Serialized)
                s.Add("select", GuideMode.Click, Anchor("pod-select-all"), done: AllSelected(rows),
                    html: H("Presione ", B("Select All"), " para seleccionar todas las piezas."));
            else
                AddRowSelection(s, "select", rows.FirstOrDefault(), order?.SFCCode, null);
            s.Add("complete", GuideMode.Click, Anchor("pod-complete"), postback: true,
                html: H("Presione ", B("Complete"), " para cerrar la orden."));
            s.Add("banner", GuideMode.Info, Anchor("pod-banner"), hide: true,
                html: H("Verifique el mensaje: las piezas completadas salen del POD y pasan a la siguiente operación."));
            return s;
        }
    }
}
