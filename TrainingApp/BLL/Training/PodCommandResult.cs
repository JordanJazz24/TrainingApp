namespace TrainingApp.BLL.Training
{
    // Aviso de formato que decide la BLL: no cuenta como intento. La página muestra su texto.
    public enum PodNotice
    {
        None,
        ResourceSetupRequired,  // Start sin orden: Resource Setup pendiente
        SelectAllPieces,        // Serialized: el paso trabaja todas las piezas juntas (spec 008)
        SfcRequired,            // NonSerialized: Start sin SFC escaneado o elegido
        SelectRowToSplit,       // SFC Split sin una fila seleccionada
        SplitRequired,          // Close del SFC Split sin ningún split
        SplitNotActive,         // split pedido fuera del paso SFC Split
        InvalidSplit            // fila o cantidad del split inválida
    }

    // Resultado de un comando del POD: un aviso (sin intento), un intento registrado, o lo
    // propio del comando.
    public class PodCommandResult
    {
        public PodNotice Notice { get; set; }

        // Intento registrado; null si el comando no calificó el paso.
        public StepSubmitResult Step { get; set; }

        // Start en un área con checklist: la página abre la secuencia de modales.
        public bool OpenChecklist { get; set; }

        // SFC Split aplicado: SFC de la fila nueva y cantidad que quedó en la original.
        public string NewSfcCode { get; set; }
        public int RemainingQty { get; set; }

        public static PodCommandResult WithNotice(PodNotice notice)
            => new PodCommandResult { Notice = notice };
    }
}
