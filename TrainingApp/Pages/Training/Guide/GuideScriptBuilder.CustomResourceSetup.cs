using System.Collections.Generic;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public sealed partial class GuideScriptBuilder
    {
        // Custom RS (spec 020, D-185): la secuencia de las capturas. Los avisos Information,
        // Confirmation y Success son capas del mismo modal, con sus anclas.
        private static StepScript CustomResourceSetup(PodContext ctx, TrainingStep step)
        {
            var s = new StepScript(step, "Custom Resource Setup")
            {
                OpenedName    = "retrieve",
                IncorrectName = "setupState",
                TailName      = "success"
            };
            s.Add("open", GuideMode.Click, Anchor("pod-resource-setup"), postback: true,
                html: H("Presione ", B("Resource Setup"), " para asociar la estación (recurso) a la orden de trabajo."),
                note: H("Resource Setup ", B("se repite cada vez que se sustituye un equipo"),
                    ": mantenimiento preventivo, correctivo o calibración."));
            s.Add("retrieve", GuideMode.Click, Anchor("crs-retrieve"),
                html: H("Presione ", B("Retrieve"), " para cargar el recurso y su operación."));
            s.Add("setupState", GuideMode.Verify, Anchor("crs-setup-state"), expect: AnswerSetUp,
                html: H("Verifique que ", B("Setup State"), " esté en ", B("Set Up"), ". Si no, cámbielo."));
            s.Add("shopOrder", GuideMode.Capture, Anchor("crs-shop-order"), format: GuideFormat.Digits,
                html: H("Ingrese la ", B("ShopOrder"), ", por ejemplo ", B(PodDefaults.DefaultShopOrder), "."));
            s.Add("associate", GuideMode.Click, Anchor("crs-associate"),
                html: H("Presione ", B("Associate New ShopOrder"), " para asociar la orden al recurso."));
            s.Add("associated", GuideMode.Click, Anchor("crs-info-ok"),
                html: H("El MES confirma que la orden quedó asociada y pide registrar las herramientas. Presione ", B("OK"), "."));
            s.Add("table", GuideMode.Info, Anchor("crs-table"),
                html: H("La tabla muestra los ", B("Tool Groups"), " que pide la operación: registre un ",
                    B("Tool Number"), " por cada herramienta que usa en su estación."),
                alert: H("Escanee las herramientas de su estación. No asuma que son las mismas del turno anterior; ",
                    "sin ese registro se pierde la trazabilidad de la operación."));
            // D-201: un Tool Number por cada Tool Group de la sesión (hoy 2, D-200), en el orden
            // de la tabla; cada subpaso resalta la fila de su grupo.
            var groups = ctx.CustomResourceSetup.ToolGroups;
            for (int i = 0; i < groups.Count; i++)
                s.Add("tool" + (i + 1), GuideMode.Capture,
                    Attr("data-group", groups[i].Code) + " " + Anchor("crs-tool-number"), format: GuideFormat.Required,
                    html: H("Ingrese el Tool Number de la herramienta ", B(groups[i].Code), " (", groups[i].Description,
                        "), por ejemplo ", B(i % 2 == 0 ? ExampleToolNumber : ExampleBarcode), "."));
            s.Add("save", GuideMode.Click, Anchor("crs-save"),
                html: H("Presione ", B("Save"), " para guardar."));
            // Un aviso de formato del servidor vuelve a la captura de la orden.
            s.Add("confirm", GuideMode.Click, Anchor("crs-confirm-yes"), postback: true, retry: "shopOrder",
                html: H("Confirme con ", B("Yes"), "."));
            // D-190: el OK del aviso cierra la ventana, como el MES real. En Guided no hay avance
            // forzado, así que el aviso siempre aparece cuando el paso se resuelve.
            s.Add("success", GuideMode.Click, Anchor("crs-success-ok"),
                html: H("Se muestra ", B("Resource setup saved successfully"), ": el cambio se aplicó. Presione ", B("OK"), "."));
            return s;
        }
    }
}
