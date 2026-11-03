using System.Collections.Generic;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public sealed partial class GuideScriptBuilder
    {
        // Ingreso a MES (spec 016, P-8): el script, el Repositorio, Recursos MES y el login de
        // MES (sin Hora a Hora, D-151); al final, el usuario en el header (P-10). Retoma según
        // el avance guardado; tras un intento fallido, en la pantalla que falló. Las capturas de Code1 solo dejan seguir con el del operador (como D-25).
        private static StepScript MesLogin(PodContext ctx, TrainingStep step)
        {
            var progress = ctx.MesLogin;
            string code1 = ctx.Code1;
            var s = new StepScript(step, "Ingreso a MES")
            {
                TailName   = "userInfo",
                ResumeName = !progress.ScriptDone ? "info"
                    : !progress.EdgeOpen ? "closeConsole"
                    : !progress.RecursosDone ? "recursos"
                    : "loginUser"
            };
            s.IncorrectByDetail[MesScreen.Console.ToString()]    = "scriptCode1";
            s.IncorrectByDetail[MesScreen.Recursos.ToString()]   = "recResource";
            s.IncorrectByDetail[MesScreen.MesLogin.ToString()]   = "loginUser";

            s.Add("info", GuideMode.Info, Anchor("mes-desktop"),
                html: H("Antes de entrar a SAP MES se prepara la estación. Esto se hace al inicio del turno ",
                    "y cada vez que cambia de estación, antes de procesar unidades."));
            AddScript(s, code1, last: false);
            // Doble clic: avanza cuando se ve Edge (la página se envía).
            s.Add("repositorio", GuideMode.Click, Anchor("mes-icon-repositorio"),
                done: new[] { Anchor("mes-edge") },
                html: H("Haga doble clic en ", B("Repositorio"), "."));
            s.Add("recursos", GuideMode.Click, Anchor("mes-card-recursos"),
                html: H("Seleccione ", B("Recursos MES"), "."));
            // Como la etiqueta de la orden (spec 011): se resalta la etiqueta y el campo queda
            // libre; avanza cuando la estación quedó escaneada (barras o lector + Enter).
            s.Add("recResource", GuideMode.Scan, Anchor("mes-station-label"),
                free: new[] { Anchor("mes-rec-resource") },
                done: new[] { Anchor("mes-rec-resource") + Attr("data-scanned", "1") },
                html: H("Escanee la etiqueta de su estación en ", B("Ingresar recurso"), "."));
            s.Add("recCode1", GuideMode.Capture, Anchor("mes-rec-code1"), format: GuideFormat.Required, expect: code1,
                html: H("Escriba su Code1: ", B(code1), "."));
            s.Add("recSearch", GuideMode.Click, Anchor("mes-rec-search"), postback: true,
                html: H("Presione ", B("Buscar"), "."));
            s.Add("loginUser", GuideMode.Capture, Anchor("mes-login-user"), format: GuideFormat.Required, expect: code1,
                html: H("Ingrese su usuario (", B(code1), ")."));
            s.Add("loginPassword", GuideMode.Capture, Anchor("mes-login-password"), format: GuideFormat.Required,
                html: H("Ingrese su contraseña."));
            s.Add("logon", GuideMode.Click, Anchor("mes-login-logon"), postback: true,
                html: H("Presione ", B("Log On"), "."));
            s.Add("userInfo", GuideMode.Info, Anchor("pod-user"),
                html: H("Verifique que su usuario aparezca correctamente."));
            return s;
        }
    }
}
