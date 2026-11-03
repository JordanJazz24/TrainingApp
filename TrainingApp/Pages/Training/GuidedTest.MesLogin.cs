using System.Web;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Guide;
using TrainingApp.Pages.Training.Models;
using TrainingApp.UI.Controls.Training;
using static TrainingApp.Pages.Training.GuidedTestHelpers;

namespace TrainingApp.Pages.Training
{
    // Página del POD (AUD-74, E6): Pantalla del Ingreso a MES y la limpieza al salir (spec 016).
    public partial class GuidedTest
    {
        // Ingreso a MES (spec 016, P-1): mientras es el paso actual, el escritorio simulado en
        // pantalla completa en lugar del POD. Se decide después de los handlers (Log On o el
        // avance forzado de este request lo cierran); Visible persiste en ViewState: se asigna
        // siempre.
        private void ApplyMesLoginScreen()
        {
            bool login   = _ctx != null && _ctx.Session.Status == SessionStatus.InProgress &&
                           _ctx.CurrentStepOrDefault?.StepType == StepType.MesLogin;
            bool cleanup = _ctx != null && (_ctx.IsLogoutCleanup || _cleanupCompleted);
            MesScreenActive = login || cleanup;
            PnlMesScreen.Visible = MesScreenActive;
            if (!MesScreenActive) return;

            if (login)
            {
                var progress = _ctx.MesLogin;
                var screen   = _mesScreen ?? progress.ResumeScreen();
                CtrlMesLogin.View = new MesLoginView
                {
                    Mode         = MesLoginControl.ModeLogin,
                    ScriptDone   = progress.ScriptDone,
                    EdgeOpen     = progress.EdgeOpen,
                    RecursosDone = progress.RecursosDone,
                    Screen       = MesLoginControl.ScreenFor(screen),
                    ResourceName = _ctx.ResourceName,
                    ScriptCode1  = screen == MesScreen.ScriptOutput ? _ctx.Code1 : null
                };
            }
            else
            {
                // Limpieza al salir (P-6): "You have logged off" hasta cerrar Edge; después el
                // escritorio y el script. Si el script cerró la sesión en este request, la
                // consola muestra su salida y al cerrarla el cliente va al resultado (P-12, D-338).
                var screen = _cleanupCompleted ? MesScreen.ScriptOutput
                    : _mesScreen ?? _ctx.State.Logout.ResumeScreen();
                CtrlMesLogin.View = new MesLoginView
                {
                    Mode           = MesLoginControl.ModeLogout,
                    EdgeOpen       = !_cleanupCompleted && !_ctx.State.Logout.EdgeClosed,
                    Screen         = MesLoginControl.ScreenFor(screen),
                    ResourceName   = _ctx.ResourceName,
                    ScriptCode1    = screen == MesScreen.ScriptOutput ? _ctx.Code1 : null,
                    CloseUrl       = _cleanupCompleted ? ResolveUrl(ResultUrl(_ctx.Session)) : null
                };
            }
            LitMesArea.Text = HttpUtility.HtmlEncode(_ctx.Area.Name);
            LitMesMode.Text = ModeLabel(_ctx.Policy).Label;
            BtnMesAbandon.Attributes["onclick"] = "return confirmAbandon('" + BtnMesAbandon.UniqueID + "');";
        }

        // Una pantalla del Ingreso enviada por MesLoginControl (D-77): la califica la BLL y dice
        // qué pantalla sigue o a cuál vuelve un fallo (P-4). Log On correcto o el avance forzado
        // cierran el paso y aparece el POD (P-5).
        protected void HandleMesAction(object sender, MesActionEventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            bool cleanup = _ctx.IsLogoutCleanup;
            var result = Pod.SubmitMesAction(_ctx, e.Action, e.Code1, e.Resource, e.Password);
            _mesScreen = result.MesScreen;
            if (result.Step != null)
            {
                // Limpieza terminada con el script (no forzada): primero la salida de la consola.
                // Se decide antes de aplicar el resultado: el cierre no redirige en ese caso.
                _cleanupCompleted = cleanup && result.Step.Outcome == StepSubmitOutcome.SessionCompleted &&
                                    result.MesScreen == MesScreen.ScriptOutput;
                ApplySubmitResult(result.Step, session, guide, area, step, incorrectFeedback: null);
                // La guía retoma en la pantalla que falló (spec 016, P-4).
                if (result.Step.Outcome == StepSubmitOutcome.Incorrect)
                    _guideMoment = GuideMoment.Incorrect(step, result.MesScreen?.ToString());
                if (_cleanupCompleted)
                    _guideMoment = GuideMoment.SessionCompleted(step, GuideMoment.CleanupDetail);
                return;
            }

            // Guided: abrir el Repositorio fuera de su turno (antes del script del Ingreso o en
            // la limpieza) se ignora, sin intento, como un botón fuera de secuencia; la guía (E4)
            // no lo deja. Mientras tanto, un aviso que no parece un error calificado.
            if (result.NotCurrentStepNotice)
                ShowNotCurrentStepNotice();
            else if (result.OutOfSequence)
                ShowSweetAlert("info", cleanup ? "Limpieza al salir" : "Antes del Repositorio",
                    cleanup
                        ? "Después del Logout no se abre el Repositorio: ejecute el script de limpieza (MES Limpiar historial y cache) con su Code1."
                        : "Primero ejecute el script de limpieza (MES Limpiar historial y cache) con su Code1.");
        }
    }
}
