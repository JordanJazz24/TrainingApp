using System;
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

        // Orden recién creada: una sola fila con la cantidad inicial. shopOrderInput ya
        // viene validado como numérico por quien llama.
        public static SimulatedOrder Create(string shopOrderInput, string materialDescription, int initialQty)
        {
            return new SimulatedOrder
            {
                ShopOrderInput      = shopOrderInput,
                SFCCode             = DeriveSFCCode(shopOrderInput),
                MaterialDescription = materialDescription,
                Status              = "Active",
                ProcessLot          = "LOT-" + DateTime.Today.ToString("yyyyMMdd"),
                Rows = new List<SimulatedOrderRow>
                {
                    new SimulatedOrderRow { RowId = Guid.NewGuid().ToString(), Qty = initialQty }
                }
            };
        }

        // Deriva el SFC a partir de los dígitos del Shop Order: pad-izquierda a 8
        // si es más corto, o truncado a los primeros 8 si es más largo.
        private static string DeriveSFCCode(string shopOrderInput)
        {
            string digits = shopOrderInput.Length > 8 ? shopOrderInput.Substring(0, 8) : shopOrderInput.PadLeft(8, '0');
            return "CR9A" + digits;
        }
    }
}
