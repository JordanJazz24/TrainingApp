using System.Collections.Generic;

namespace TrainingApp.Pages.Training.Models
{
    /// <summary>
    /// Orden mock creada dinámicamente por el operador en Resource Setup
    /// (spec 005). Vive únicamente en ASP.NET Session bajo la clave
    /// Constants.SessionKeys.SimulatedOrderPrefix + TrainingSession.Id —
    /// no hay persistencia en base de datos.
    /// </summary>
    public class SimulatedOrder
    {
        public string ShopOrderInput       { get; set; }
        public string SFCCode              { get; set; }
        public string MaterialDescription  { get; set; }
        public string Status               { get; set; }
        public string ProcessLot           { get; set; }

        public List<SimulatedOrderRow> Rows { get; set; } = new List<SimulatedOrderRow>();
    }
}
