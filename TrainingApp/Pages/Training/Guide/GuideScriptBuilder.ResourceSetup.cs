using System.Collections.Generic;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public sealed partial class GuideScriptBuilder
    {
        private static StepScript ResourceSetup(PodContext ctx, TrainingStep step)
        {
            // D-25 (spec 015): la Shop Order y la herramienta no pueden ser equipos precargados.
            var preloaded = ctx.State.PreloadedEquipment?.AllValues();
            var s = new StepScript(step, "Resource Setup")
            {
                OpenedName    = "retrieve",
                IncorrectName = "setupState",
                TailName      = "recordChanged"
            };
            s.Add("open", GuideMode.Click, Anchor("pod-resource-setup"), postback: true,
                html: H("Presione ", B("Resource Setup"), " para asociar la estación (recurso) a la orden de trabajo."),
                note: H("Resource Setup ", B("se repite cada vez que se sustituye un equipo"),
                    ": mantenimiento preventivo, correctivo o calibración."));
            s.Add("retrieve", GuideMode.Click, Anchor("rs-retrieve"),
                html: H("Presione ", B("Retrieve"), " para cargar la configuración del recurso."));
            s.Add("removeAll", GuideMode.Click, Anchor("rs-remove-all"),
                html: H("Presione ", B("REMOVE ALL"), " para limpiar la tabla antes de agregar los recursos de esta orden."),
                alert: H("Si hay equipos registrados, haga ", B("Remove All"), " y escanee los de su estación. ",
                    "No asuma que son los mismos que tiene en su mesa; sin ese registro se pierde la trazabilidad ",
                    "de la operación."));
            s.Add("insertShopOrder", GuideMode.Click, Anchor("rs-insert-new"),
                html: H("Presione ", B("INSERT NEW"), " para agregar una fila."));
            s.Add("shopOrder", GuideMode.Capture, Anchor("rs-row-value"), pickLast: true, format: GuideFormat.Digits,
                reject: preloaded,
                html: H("La fila nueva es de tipo ", B("Shop Order"), ". Ingrese el número de la orden, por ejemplo ",
                    B(PodDefaults.DefaultShopOrder), "."));
            s.Add("insertTool", GuideMode.Click, Anchor("rs-insert-new"),
                html: H("Presione ", B("INSERT NEW"), " para agregar otra fila."));
            s.Add("toolType", GuideMode.Change, Anchor("rs-row-type"), pickLast: true, expect: AnswerToolNumber,
                html: H("Cambie el tipo de la fila nueva a ", B("Tool Number"), "."));
            s.Add("tool", GuideMode.Capture, Anchor("rs-row-value"), pickLast: true, format: GuideFormat.Required,
                reject: preloaded,
                html: H("Ingrese el número de la herramienta, por ejemplo ", B(ExampleToolNumber), "."));
            s.Add("table", GuideMode.Info, Anchor("rs-table"),
                html: H("En esta sección se agregan todos los recursos, herramientas y materiales asociados a su operación."));
            s.Add("setupState", GuideMode.Verify, Anchor("rs-setup-state"), expect: AnswerSetUp,
                html: H("Verifique que ", B("Setup State"), " esté en ", B("Set Up"), ". Si no, cámbielo."));
            // Un aviso de formato del control (filas incompletas) vuelve a armar las filas.
            s.Add("save", GuideMode.Click, Anchor("rs-save"), postback: true, retry: "removeAll",
                html: H("Presione ", B("Save"), " para guardar."));
            s.Add("recordChanged", GuideMode.Info, Anchor("rs-record-changed"), hide: true,
                html: H("Se muestra el mensaje ", B("Record changed"), ": indica que se aplicó el cambio."));
            s.Add("close", GuideMode.Click, Anchor("rs-close"),
                html: H("Cierre Resource Setup con la ", B("X"), "."));
            return s;
        }
    }
}
