using System.Collections.Generic;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public sealed partial class GuideScriptBuilder
    {
        // Logout (spec 015): el ícono del header, con la nota de la trainer (resumen del punto 9
        // de la spec). Al cerrar la sesión sigue el globo final.
        // Logout (spec 015) con la limpieza al salir (spec 016, P-6): "You have logged off",
        // cerrar Edge y el script con el Code1. Retoma donde va la limpieza.
        private static StepScript Logout(PodContext ctx, TrainingStep step)
        {
            var cleanup = ctx.State.Logout;
            var s = new StepScript(step, "Logout")
            {
                ResumeName = cleanup == null ? "logout" : cleanup.EdgeClosed ? "script" : "closeEdge"
            };
            s.IncorrectByDetail[MesScreen.Console.ToString()] = "scriptCode1";
            s.Add("logout", GuideMode.Click, Anchor("pod-logout"), postback: true,
                html: H("Presione el ícono ", B("Logout"), " para cerrar su sesión en MES."),
                note: string.Join("<br>",
                    H("Cierre sesión ", B("siempre que deje de usar el sistema"), ", por ejemplo:"),
                    H("• En descansos o al terminar el turno."),
                    H("• Al cambiar de estación, actividad, producto, orden o línea."),
                    H("• Al entregar la estación a otro operador."),
                    H("Nunca deje su sesión abierta: lo que otra persona haga en ella queda registrado con su usuario.")));
            // Sin Info en "You have logged off": la nota del subpaso anterior ya dice cuándo se
            // cierra sesión (usuario, 2026-10-19, al probar la E4).
            s.Add("closeEdge", GuideMode.Click, Anchor("mes-edge-close"), postback: true,
                html: H("Cierre el navegador con la ", B("✕"), "."));
            AddScript(s, ctx.Code1, last: true);
            return s;
        }
    }
}
