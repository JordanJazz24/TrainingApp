using System.Collections.Generic;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public sealed partial class GuideScriptBuilder
    {
        // Search/Start (spec 011): escanear la etiqueta de la orden (o tocar las barras, o
        // escribir la Shop Order en SFC) y Start; Browse Work List se menciona como camino
        // alternativo. Con el checklist de Start (hoy Moldeo) siguen sus 4 modales; los
        // formularios de varios campos van por bloque (R7).
        private static StepScript Start(PodContext ctx, TrainingStep step)
        {
            var s = new StepScript(step, "Start") { TailName = "banner" };

            // Avanza cuando la orden completa (todas las piezas o el lote) queda cargada en
            // SFC; una sola pieza no alcanza. Si ya estaba cargada, se salta.
            bool serialized = ctx.Area.SerializationType == SerializationType.Serialized;
            s.Add("scan", GuideMode.Scan, Anchor("pod-label"),
                free: new[] { Anchor("pod-sfc") },
                done: new[] { Anchor("pod-sfc") + Attr("data-loaded", "order") },
                html: H("Escanee la etiqueta de la orden con el lector o toque las barras: la ",
                    B("Shop Order"), serialized ? " carga todas las piezas. " : " carga el SFC del lote. ",
                    "También puede escribir el número en ", B("SFC"), " y presionar Enter."),
                alert: H("Asegúrese de trabajar solo las piezas de su orden y su estación."));
            s.Add("browse", GuideMode.Info, Anchor("pod-browse"),
                html: H("La orden también se puede cargar con ", B("Browse Work List"),
                    " (⧉), sobre todo cuando se trabaja con varias órdenes a la vez."));
            s.Add("start", GuideMode.Click, Anchor("pod-start"), postback: true,
                html: H("Presione ", B("Start"), " para iniciar la orden."));

            if (ctx.Area.RequiresStartChecklist)
            {
                // Start abre el modal 1; un intento incorrecto reabre el modal de la primera
                // respuesta "No" (verificaciones o final). Sin checklist no hay estas entradas.
                s.OpenedName    = "molde";
                s.IncorrectName = "verifs";
                s.IncorrectByDetail["modalSearch4"] = "final";

                s.Add("molde", GuideMode.Capture, Anchor("search-molde"), format: GuideFormat.Number,
                    html: H("Ingrese el ", B("ID del molde"), " según la MSS."));
                s.Add("save1", GuideMode.Click, Anchor("search-save1"),
                    html: H("Presione ", B("Save"), "."));
                s.Add("verifs", GuideMode.Capture, Anchor("search-verifs"), format: GuideFormat.Required, expect: AnswerYes,
                    html: H("Responda ", B("Sí"), " a las tres verificaciones de etiquetas."));
                s.Add("save2", GuideMode.Click, Anchor("search-save2"),
                    html: H("Presione ", B("Save"), "."));
                s.Add("mss", GuideMode.Capture, Anchor("search-mss"), format: GuideFormat.Number,
                    html: H("Ingrese los parámetros técnicos según la ", B("MSS D000281359"), "."));
                s.Add("save3", GuideMode.Click, Anchor("search-save3"),
                    html: H("Presione ", B("Save"), "."));
                s.Add("final", GuideMode.Change, Anchor("search-final"), expect: AnswerYes,
                    html: H("Seleccione ", B("Sí"), " en la verificación final."));
                s.Add("save4", GuideMode.Click, Anchor("search-save4"), postback: true, retry: "final",
                    html: H("Presione ", B("Save"), " para iniciar la orden."));
            }

            s.Add("banner", GuideMode.Info, Anchor("pod-banner"), hide: true,
                html: H("Verifique el mensaje ", B("Start process completed successfully"), ": la orden quedó activa."));
            return s;
        }
    }
}
