using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public sealed partial class GuideScriptBuilder
    {
        // Pasos 2 y 6: el mismo recorrido en apertura y en cierre. Cambian las notas de la
        // trainer (T7): la de apertura dice cuándo se hace; la de cierre, que no siempre aplica.
        private static StepScript LineClearance(PodContext ctx, TrainingStep step)
        {
            bool closing = ctx.Guide.Steps.Any(x => x.StepType == StepType.LineClearance && x.Order < step.Order);
            var s = new StepScript(step, "Line Clearance")
            {
                OpenedName    = "retrieve",
                IncorrectName = "answer",
                TailName      = "saved"
            };
            s.Add("activities", GuideMode.Click, Anchor("pod-activities"),
                html: H("Presione ", B("Activities..."), " para abrir el menú de actividades."),
                note: closing ? null : string.Join("<br>",
                    H("Siempre después de ", B("Resource Setup"), ": la limpieza física y la del sistema."),
                    H("Se registra de nuevo cuando:"),
                    H("• cambia el usuario o la Shop Order,"),
                    H("• queda un SFC activo,"),
                    H("• vence el tiempo configurado.")),
                alert: closing ? H("En algunas operaciones no aplica (depende de la operación).") : null);
            s.Add("standalone", GuideMode.Click, Anchor("pod-dc-standalone"), postback: true,
                html: H("Seleccione ", B("Data Collection Standalone"), " para registrar la limpieza de línea."));
            s.Add("retrieve", GuideMode.Click, Anchor("lc-retrieve"),
                html: H("Presione ", B("Retrieve"), " para cargar los grupos de datos del recurso."));
            s.Add("collect", GuideMode.Click, Anchor("lc-collect"),
                html: H("Presione ", B("Collect"), " en ", B("DC_LC_DONE"), " para responder la limpieza de línea."));
            s.Add("answer", GuideMode.Change, Anchor("lc-answer"), expect: AnswerYes,
                html: H("Seleccione ", B("Si"), ": la limpieza de línea se realizó."));
            s.Add("save", GuideMode.Click, Anchor("lc-save"), postback: true, retry: "answer",
                html: H("Presione ", B("Save"), " para guardar la respuesta."));
            s.Add("saved", GuideMode.Info, Anchor("lc-saved"), hide: true,
                html: H("Verifique que aparezca el mensaje ", B("1 DC Groups saved"), ": la respuesta quedó registrada."));
            s.Add("close", GuideMode.Click, Anchor("lc-close"),
                html: H("Cierre Data Collection Standalone con la ", B("X"), "."));
            return s;
        }
    }
}
