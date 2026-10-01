namespace TrainingApp.Pages.Training.Models
{
    /// <summary>
    /// Una fila de una SimulatedOrder.
    /// NonSerialized: filas de cantidad del lote. Cuando se hace un SFC Split, la fila
    /// original reduce su Qty y se agrega otra con un SFC nuevo de la misma Shop Order
    /// (SplitSfcGenerator; enmienda 2026-10-01 de la spec 005). El RowId sigue siendo la
    /// clave de cada fila en el POD.
    /// Serialized (spec 008): una fila por pieza, cada una con su propio SFC y Qty 1.
    /// </summary>
    public class SimulatedOrderRow
    {
        public const string StatusNew    = "New";
        public const string StatusActive = "Active";

        public string RowId   { get; set; }
        public string SFCCode { get; set; }
        public int    Qty     { get; set; }

        // New: pieza en cola, todavía no iniciada (solo se ve en Browse Work List).
        // Active: iniciada con Start; aparece en el POD.
        public string Status  { get; set; }

        // true en las filas creadas por el SFC Split. En NonSerialized solo estas se
        // pueden scrapear en Log NC; la fila original del lote no (spec 007).
        public bool IsFromSplit { get; set; }
    }
}
