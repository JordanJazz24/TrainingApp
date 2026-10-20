using System.Collections.Generic;

namespace TrainingApp.Pages.Training.Models
{
    // Datos que el host le pasa a cada control de paso; los controles solo leen ExtraData.
    public class StepViewModel
    {
        public Dictionary<string, object> ExtraData { get; set; }

        public StepViewModel()
        {
            ExtraData = new Dictionary<string, object>();
        }
    }

    public class StepSubmitEventArgs : System.EventArgs
    {
        public string Answer { get; set; }

        // Texto opcional que el host muestra en lugar del hint genérico si la BLL
        // evalúa el Answer como incorrecto (ej. "Not the correct button").
        public string IncorrectFeedback { get; set; }

        // Resource Setup: Shop Order capturada, ya validada como numérica por el control. Con
        // ella la BLL crea la orden simulada (spec 005, spec 009).
        public string ShopOrder { get; set; }
    }

    // Data Collection (spec 008): Save de un DC Group para la pieza actual.
    public class DataCollectionGroupSubmitEventArgs : System.EventArgs
    {
        public string GroupCode { get; set; }
        public string AnswersJson { get; set; }
    }

    // Assembly (spec 008): el operador escaneó el material de un componente del BOM.
    public class ComponentAssembledEventArgs : System.EventArgs
    {
        public int ComponentId { get; set; }
    }

    // Ingreso a MES (spec 016): lo que MesLoginControl pinta en este request. Screen es la
    // pantalla de arranque (MesLoginControl.Screen*): la que falló (P-4), la que sigue a lo
    // aceptado o la que reconstruye el avance tras F5 (D-78).
    public class MesLoginView
    {
        public bool ScriptDone   { get; set; }
        public bool EdgeOpen     { get; set; }
        public bool HoraHoraDone { get; set; }
        public bool RecursosDone { get; set; }
        public string Screen     { get; set; }

        // Recurso del área para la etiqueta de la estación (D-70).
        public string ResourceName { get; set; }

        // Code1 del operador para la salida del script ("Script ejecutado por …"); solo con
        // la consola terminada.
        public string ScriptCode1 { get; set; }
    }

    // Ingreso a MES (spec 016, D-77): una pantalla enviada al servidor. Password solo dice si
    // el campo tenía algo (D-73: no se compara con nada), así la contraseña que escriba el
    // operador nunca viaja ni vuelve en el HTML.
    public class MesActionEventArgs : System.EventArgs
    {
        public TrainingApp.BLL.Training.MesAction Action { get; set; }
        public string Code1 { get; set; }
        public string Resource { get; set; }
        public string Password { get; set; }
    }

    public class SFCSplitRequestedEventArgs : System.EventArgs
    {
        public string RowId { get; set; }
        public int Qty { get; set; }
        public bool IsApplied { get; set; }
        public int RemainingQty { get; set; }
        public string ErrorMessage { get; set; }

        // SFC de la fila creada, para el mensaje "New SFC … created with quantity …".
        public string NewSfcCode { get; set; }

        // La página ya mostró el resultado de un intento (cantidad distinta de la del hallazgo,
        // spec 014): el control no muestra su aviso. StepResolved: el paso avanzó forzado y el
        // control no se vuelve a abrir.
        public bool Handled { get; set; }
        public bool StepResolved { get; set; }
    }
}
