using System.Collections.Generic;
using TrainingApp.BLL.Training;

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

        // Lo escaneado tal cual: el formato lo decide la BLL (spec 017, M-8).
        public string Barcode { get; set; }
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

        // El Ingreso a MES o la limpieza al salir (MesLoginControl.Mode*, spec 016, P-6).
        public string Mode { get; set; }

        // La limpieza terminó en este request: al cerrar la consola se ve el POD con el resumen
        // (P-12).
        public bool CloseToSummary { get; set; }
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

        // null: el campo vacío o sin un número (la BLL decide el mensaje, spec 017).
        public decimal? Qty { get; set; }
        public bool IsApplied { get; set; }
        public int RemainingQty { get; set; }

        // Aviso del simulador (SweetAlert) o mensaje del MES, que reemplaza el formulario con
        // [OK] (spec 017, superficie S).
        public string ErrorMessage { get; set; }
        public MesError MesError { get; set; }

        // SFC de la fila creada, para el mensaje "New SFC … created with quantity …".
        public string NewSfcCode { get; set; }

        // La página ya mostró el resultado de un intento (cantidad distinta de la del hallazgo,
        // spec 014): el control no muestra su aviso. StepResolved: el paso avanzó forzado y el
        // control no se vuelve a abrir.
        public bool Handled { get; set; }
        public bool StepResolved { get; set; }
    }
}
