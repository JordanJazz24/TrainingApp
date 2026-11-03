using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public sealed partial class GuideScriptBuilder
    {
        // SFC Split (solo NonSerialized): la fila del lote, la cantidad, Split, su mensaje,
        // Close y el banner como cola.
        private static StepScript SfcSplit(PodContext ctx, TrainingStep step)
        {
            var s = new StepScript(step, "SFC Split") { OpenedName = "qty", IncorrectName = "qty", TailName = "banner" };
            s.ProgressByDetail["split"] = "created";

            // Enmienda 2 de la spec 014: el hallazgo de Log NC se presenta aquí (no se repite en
            // su Log NC) y dicta la cantidad del split, que no se califica (D-32).
            var finding = ctx.SplitFinding;
            if (finding != null) s.FreeOnAll.Add(Anchor("nc-finding"));
            if (finding != null)
                s.Add("finding", GuideMode.Info, Anchor("nc-finding"),
                    html: H("Calidad reportó un hallazgo: revise el defecto, la cantidad y la disposición. ",
                        "Primero separe esas unidades con SFC Split; después las registra en Log NC."));

            var lot = ctx.State.Order?.ActiveRows().FirstOrDefault(r => !r.IsFromSplit);
            AddRowSelection(s, "select", lot, ctx.State.Order?.SFCCode, null);
            s.Add("open", GuideMode.Click, Anchor("pod-sfc-split"), postback: true,
                html: H("Presione ", B("SFC Split"), " para dividir el lote."));
            // Enmienda 3: la cantidad se califica; la guía no deja seguir con otra (como D-25).
            s.Add("qty", GuideMode.Capture, Anchor("split-qty"), format: GuideFormat.Digits,
                expect: finding?.UnitCount.ToString(CultureInfo.InvariantCulture),
                html: finding != null
                    ? H("Ingrese la cantidad del hallazgo: ", B(finding.UnitCount.ToString(CultureInfo.InvariantCulture)), ".")
                    : H("Ingrese la cantidad a dividir, por ejemplo ",
                        B(PodDefaults.DefaultSplitQty.ToString(CultureInfo.InvariantCulture)), "."));
            s.Add("split", GuideMode.Click, Anchor("split-split"), postback: true,
                html: H("Presione ", B("Split"), "."));
            s.Add("created", GuideMode.Info, Anchor("split-created"),
                html: H("Verifique el mensaje ", B("New SFC … created with quantity …"), ": se creó un SFC nuevo con esa cantidad."));
            s.Add("close", GuideMode.Click, Anchor("split-close"), postback: true,
                html: H("Presione ", B("Close"), " para terminar el split."));
            s.Add("banner", GuideMode.Info, Anchor("pod-banner"), hide: true,
                html: H("Verifique el mensaje ", B("SFC Split completed successfully"), "."));
            return s;
        }
    }
}
