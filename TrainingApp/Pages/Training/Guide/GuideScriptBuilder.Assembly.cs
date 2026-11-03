using System.Collections.Generic;
using System.Globalization;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public sealed partial class GuideScriptBuilder
    {
        // Assembly: cada componente del BOM, en el orden de la Component List. Lo ensamblado
        // antes de este request no se muestra; del componente recién ensamblado (Add, sin ser
        // el último) quedan su confirmación y la X. El Add del último completa el paso.
        private static StepScript Assembly(PodContext ctx, TrainingStep step, GuideMoment moment)
        {
            var s = new StepScript(step, "Assembly") { TailName = "banner" };
            AddPodSelection(s, ctx, AllPiecesNote);
            s.Add("open", GuideMode.Click, Anchor("pod-assemble"), postback: true,
                html: H("Presione ", B("Assemble"), " para abrir la Component List."),
                note: H(B("Consuma los componentes antes"), " de empezar la instrucción del MPI."));

            var bom = ctx.AssemblyComponents;
            var assembled = ctx.State.Assembly?.AssembledComponentIds ?? new HashSet<int>();
            string justAssembled = s.DetailOf(moment, GuideMomentKind.Progress);

            for (int i = 0; i < bom.Count; i++)
            {
                var c = bom[i];
                string id = c.Id.ToString(CultureInfo.InvariantCulture);
                string p = "c" + id;
                bool last = i == bom.Count - 1;

                s.Add(p + ".row", GuideMode.Click, Anchor("asm-assemble") + Attr("data-component-id", id),
                    html: H("Presione ", B("Assemble"), " en el componente ", B(c.PartNumber + "/" + c.Revision), "."));
                s.Add(p + ".barcode", GuideMode.Scan, Anchor("asm-barcode"), format: GuideFormat.Digits,
                    done: new[] { Anchor("asm-add") },
                    html: H("Escanee el código de barras del componente, por ejemplo ", B(ExampleBarcode), "."));
                s.Add(p + ".add", GuideMode.Click, Anchor("asm-add"), postback: true, retry: p + ".barcode",
                    html: H("Presione ", B("Add"), " para registrar el componente."));
                // Spec 017 (M-8): el Add que la BLL rechazó vuelve al escaneo del componente.
                s.IncorrectByDetail[id] = p + ".barcode";
                if (!last)
                {
                    s.Add(p + ".confirm", GuideMode.Info, Anchor("asm-confirm"),
                        html: H("Verifique el mensaje de confirmación: el componente quedó ensamblado."));
                    s.Add(p + ".close", GuideMode.Click, Anchor("asm-close"),
                        html: H("Cierre el modal con la ", B("X"), " para seguir con el siguiente componente."));
                    s.ProgressByDetail[id] = p + ".confirm";
                }

                if (assembled.Contains(c.Id))
                {
                    s.Hide(p + ".row", p + ".barcode", p + ".add");
                    if (!last && id != justAssembled) s.Hide(p + ".confirm", p + ".close");
                }
                else if (s.OpenedName == null)
                {
                    s.OpenedName = p + ".row";
                }
            }

            s.Add("banner", GuideMode.Info, Anchor("pod-banner"), hide: true,
                html: H("Verifique el mensaje ", B("All components processed at this operation"), ": se ensambló todo el BOM."));
            return s;
        }
    }
}
