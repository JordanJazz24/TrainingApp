using System.Collections.Generic;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Models
{
    public class StepViewModel
    {
        public int      StepId         { get; set; }
        public StepType StepType       { get; set; }
        public int      StepOrder      { get; set; }
        public string   Hint           { get; set; }
        public bool?    IsCorrect      { get; set; }
        public int      AttemptCount   { get; set; }
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
        public string Barcode { get; set; }
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
    }
}
