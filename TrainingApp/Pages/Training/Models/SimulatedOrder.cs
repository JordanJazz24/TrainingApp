using System;
using System.Collections.Generic;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

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

        // SFC del lote (NonSerialized). En Serialized es null: cada pieza tiene el suyo.
        public string SFCCode              { get; set; }
        public string MaterialDescription  { get; set; }
        public string Status               { get; set; }
        public string ProcessLot           { get; set; }

        public List<SimulatedOrderRow> Rows { get; set; } = new List<SimulatedOrderRow>();

        // shopOrderInput ya viene validado como numérico por quien llama.
        //   NonSerialized: una sola fila con la cantidad inicial, ya Active (el lote).
        //   Serialized (spec 008): entre 3 y 5 piezas con SFC propio, en New hasta el Start.
        public static SimulatedOrder Create(string shopOrderInput, string materialDescription,
            int initialQty, SerializationType serializationType)
        {
            var order = new SimulatedOrder
            {
                ShopOrderInput      = shopOrderInput,
                MaterialDescription = materialDescription,
                Status              = SimulatedOrderRow.StatusActive,
                ProcessLot          = "LOT-" + DateTime.Today.ToString("yyyyMMdd")
            };

            if (serializationType == SerializationType.Serialized)
            {
                foreach (var sfc in SerialSfcGenerator.Generate(shopOrderInput, new Random()))
                    order.Rows.Add(new SimulatedOrderRow
                    {
                        RowId   = Guid.NewGuid().ToString(),
                        SFCCode = sfc,
                        Qty     = 1,
                        Status  = SimulatedOrderRow.StatusNew
                    });
            }
            else
            {
                order.SFCCode = DeriveSFCCode(shopOrderInput);
                order.Rows.Add(new SimulatedOrderRow
                {
                    RowId   = Guid.NewGuid().ToString(),
                    SFCCode = order.SFCCode,
                    Qty     = initialQty,
                    Status  = SimulatedOrderRow.StatusActive
                });
            }

            return order;
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
