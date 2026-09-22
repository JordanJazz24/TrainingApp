namespace TrainingApp.Pages.Training.Models
{
    /// <summary>
    /// Una fila de cantidad dentro de una SimulatedOrder. Cuando se hace un
    /// SFC Split, la fila original reduce su Qty y se agrega una nueva fila
    /// con un RowId distinto — ambas comparten el mismo SFCCode/ShopOrder
    /// (que viven en la SimulatedOrder padre), por eso el RowId es lo único
    /// que distingue filas hermanas en el POD.
    /// </summary>
    public class SimulatedOrderRow
    {
        public string RowId { get; set; }
        public int    Qty   { get; set; }
    }
}
