using System.Collections.Generic;

namespace TrainingApp.BLL.Training
{
    // Aviso de formato que decide la BLL: no cuenta como intento. La página muestra su texto.
    public enum PodNotice
    {
        None,
        ResourceSetupRequired,  // Start sin orden: Resource Setup pendiente
        SelectAllPieces,        // Serialized: el paso trabaja todas las piezas juntas (spec 008)
        StartWholeOrder,        // Serialized: Start sin la orden completa cargada (spec 008 y 011)
        SfcRequired,            // NonSerialized: Start sin SFC escaneado o elegido
        SfcNotInOrder,          // NonSerialized: Start con un SFC que no es el del lote ni la Shop Order (spec 011)
        SelectOneRow,           // NonSerialized: Assembly o DC sin la fila del lote seleccionada
        SelectRowToSplit,       // SFC Split sin una fila seleccionada
        SplitRequired,          // Close del SFC Split sin ningún split
        SplitNotActive,         // split pedido fuera del paso SFC Split
        InvalidSplit,           // fila o cantidad del split inválida
        NoPieceLeft             // Serialized: Log NC con todas las piezas (debe quedar una)
    }

    // Por qué una acción del operador fue un intento fallido sin pasar por el validador (la
    // página elige el texto del feedback).
    public enum PodAttemptReason
    {
        None,
        NoScrappableRow,        // Log NC sin una fila que se pueda scrapear (spec 007)
        NoRowToComplete         // Complete sin la selección que exige el paso (spec 007/008)
    }

    // Resultado de un comando del POD: un click fuera de secuencia, un aviso (sin intento), un
    // intento registrado, o lo propio del comando.
    public class PodCommandResult
    {
        // El botón no era el del paso actual. Step trae el intento fallido (Assessment) o es
        // null (Guided: el click se ignora).
        public bool OutOfSequence { get; set; }

        // Avance forzado por secuencia: la BLL dejó el POD como si el paso se hubiera hecho y
        // la página lo tiene que volver a pintar.
        public bool PodChanged { get; set; }

        public PodNotice Notice { get; set; }

        // Intento registrado; null si el comando no calificó el paso.
        public StepSubmitResult Step { get; set; }

        public PodAttemptReason FeedbackReason { get; set; }

        // Start en un área con checklist: la página abre la secuencia de modales.
        public bool OpenChecklist { get; set; }

        // SFC Split aplicado: SFC de la fila nueva y cantidad que quedó en la original.
        public string NewSfcCode { get; set; }
        public int RemainingQty { get; set; }

        // Log NC resuelto: filas que salieron de la orden (scrap).
        public IList<SimulatedOrderRow> Scrapped { get; set; }

        // Complete resuelto (spec 015, D-22): filas que salieron del POD (pasan a la siguiente
        // operación).
        public IList<SimulatedOrderRow> Completed { get; set; }

        public static PodCommandResult WithNotice(PodNotice notice)
            => new PodCommandResult { Notice = notice };
    }
}
