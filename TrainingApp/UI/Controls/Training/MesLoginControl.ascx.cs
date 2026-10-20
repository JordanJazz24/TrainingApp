using System;
using System.Web.Script.Serialization;
using System.Web.UI;
using TrainingApp.BLL.Training;
using TrainingApp.Pages.Training.Models;

namespace TrainingApp.UI.Controls.Training
{
    // Ingreso a MES (spec 016, D-76): escritorio, consola del script, Edge con el Repositorio,
    // Hora a Hora 3.0, Recursos MES y el login de MES. Las pantallas y las pestañas se manejan
    // en el cliente; cada acción calificable o que cambia el avance es un postback (D-77) que
    // la página manda a PodService. No es un IStepControl: se ve mientras el paso actual es
    // Ingreso a MES y la página le pasa en View lo que tiene que pintar.
    public partial class MesLoginControl : UserControl
    {
        // Pantalla de arranque (MesLoginView.Screen): los nombres que usa el script del .ascx.
        public const string ScreenDesktop     = "desktop";
        public const string ScreenConsole     = "console";
        public const string ScreenConsoleDone = "consoleDone";
        public const string ScreenRepository  = "repository";
        public const string ScreenHoraHora    = "horahora";
        public const string ScreenRecursos    = "recursos";
        public const string ScreenMesLogin    = "meslogin";
        public const string ScreenLoggedOff   = "loggedoff";

        // MesLoginView.Mode: el Ingreso a MES o la limpieza al salir (P-6).
        public const string ModeLogin  = "login";
        public const string ModeLogout = "logout";

        public event EventHandler<MesActionEventArgs> OnMesAction;

        public MesLoginView View { get; set; }

        // Estado inicial del script del .ascx. JavaScriptSerializer escapa < > &: no puede
        // cerrar el <script>.
        protected string StateJson => new JavaScriptSerializer().Serialize(View ?? new MesLoginView());

        protected string ResourceName => View?.ResourceName ?? string.Empty;

        protected void BtnMesSubmit_Click(object sender, EventArgs e)
        {
            var args = new MesActionEventArgs
            {
                Code1    = HfMesCode1.Value,
                Resource = HfMesResource.Value,
                Password = HfMesPassword.Value
            };
            bool valid = Enum.TryParse(HfMesAction.Value, out MesAction action) &&
                         Enum.IsDefined(typeof(MesAction), action);
            ClearFields();
            if (!valid) return;

            args.Action = action;
            OnMesAction?.Invoke(this, args);
        }

        // Los campos ocultos viajan una sola vez: no vuelven en el HTML de la respuesta.
        private void ClearFields()
        {
            HfMesAction.Value   = string.Empty;
            HfMesCode1.Value    = string.Empty;
            HfMesResource.Value = string.Empty;
            HfMesPassword.Value = string.Empty;
        }

        // Pantalla de arranque para una pantalla de la BLL.
        public static string ScreenFor(MesScreen screen)
        {
            switch (screen)
            {
                case MesScreen.Console:      return ScreenConsole;
                case MesScreen.ScriptOutput: return ScreenConsoleDone;
                case MesScreen.Repository: return ScreenRepository;
                case MesScreen.HoraHora:   return ScreenHoraHora;
                case MesScreen.Recursos:   return ScreenRecursos;
                case MesScreen.MesLogin:   return ScreenMesLogin;
                case MesScreen.LoggedOff:  return ScreenLoggedOff;
                default:                   return ScreenDesktop;
            }
        }
    }
}
