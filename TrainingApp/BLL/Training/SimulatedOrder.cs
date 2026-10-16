using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    /// <summary>
    /// Orden mock que el operador crea en Resource Setup (spec 005). Es parte del estado del
    /// POD (PodState): vive en Session y solo la modifica PodService (spec 009).
    /// </summary>
    [Serializable]
    public class SimulatedOrder
    {
        public string ShopOrderInput       { get; set; }

        // SFC del lote (NonSerialized). En Serialized es null: cada pieza tiene el suyo.
        public string SFCCode              { get; set; }
        public string MaterialDescription  { get; set; }
        public string Status               { get; set; }
        public string ProcessLot           { get; set; }

        public List<SimulatedOrderRow> Rows { get; set; } = new List<SimulatedOrderRow>();

        // Hubo al menos un SFC Split (el paso solo existe en NonSerialized).
        public bool HasSplit => Rows.Any(r => r.IsFromSplit);

        // Formato de la Shop Order capturada en Resource Setup (spec 005): solo dígitos. El
        // control lo usa como aviso de formato y PodService lo exige (auditoría AUD-36).
        public static bool IsValidShopOrder(string shopOrderInput)
            => !string.IsNullOrEmpty(shopOrderInput) && shopOrderInput.All(char.IsDigit);

        // shopOrderInput ya viene validado con IsValidShopOrder por quien llama.
        //   NonSerialized: una sola fila con la cantidad del lote, ya Active.
        //   Serialized (spec 008): entre 4 y 5 piezas con SFC propio (spec 014), en New hasta el Start.
        public static SimulatedOrder Create(string shopOrderInput, string materialDescription,
            SerializationType serializationType)
        {
            var order = new SimulatedOrder
            {
                ShopOrderInput      = shopOrderInput,
                MaterialDescription = materialDescription,
                Status              = SimulatedOrderRow.StatusActive,
                ProcessLot          = PodDefaults.ProcessLot(DateTime.Today)
            };

            if (serializationType == SerializationType.Serialized)
            {
                foreach (var sfc in SerialSfcGenerator.Generate(shopOrderInput, new Random()))
                    order.Rows.Add(new SimulatedOrderRow
                    {
                        RowId   = Guid.NewGuid().ToString(),
                        SFCCode = sfc,
                        Qty     = PodDefaults.SerializedPieceQty,
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
                    Qty     = PodDefaults.NonSerializedLotQty,
                    Status  = SimulatedOrderRow.StatusActive
                });
            }

            return order;
        }

        // NonSerialized (spec 011): lo escaneado o escrito en el campo SFC nombra el lote si es
        // su SFC o la Shop Order (sin espacios al inicio ni al final; el resto, exacto).
        public bool IsLotReference(string scanned)
        {
            string value = (scanned ?? string.Empty).Trim();
            return value.Length > 0 && (value == SFCCode || value == ShopOrderInput);
        }

        // Filas iniciadas: las que muestra el POD.
        public IList<SimulatedOrderRow> ActiveRows()
            => Rows.Where(r => r.IsActive).ToList();

        // Los ids llegan del cliente: solo cuentan los que existen en la orden y están Active.
        public IList<SimulatedOrderRow> FindActiveRows(ICollection<string> rowIds)
            => Rows.Where(r => r.IsActive && rowIds.Contains(r.RowId)).ToList();

        // Piezas elegidas en Browse Work List, en cualquier estado.
        public IList<SimulatedOrderRow> FindRows(ICollection<string> rowIds)
            => Rows.Where(r => rowIds.Contains(r.RowId)).ToList();

        public void Activate(IEnumerable<SimulatedOrderRow> rows)
        {
            foreach (var row in rows)
                row.Status = SimulatedOrderRow.StatusActive;
        }

        // Como el MES real, el split crea un SFC nuevo de la misma Shop Order (enmienda
        // 2026-10-01 de la spec 005): mismo prefijo que el lote, sufijo que no se repite.
        public string Split(SimulatedOrderRow row, int qty)
        {
            string newSfc = SplitSfcGenerator.Next(SFCCode,
                Rows.Select(r => r.SFCCode).ToList(), new Random());

            row.Qty -= qty;
            Rows.Add(new SimulatedOrderRow
            {
                RowId       = Guid.NewGuid().ToString(),
                SFCCode     = newSfc,
                Qty         = qty,
                Status      = SimulatedOrderRow.StatusActive,
                IsFromSplit = true
            });
            return newSfc;
        }

        // Scrap de Log NC: las filas salen de la orden.
        public void Remove(IEnumerable<SimulatedOrderRow> rows)
        {
            foreach (var row in rows.ToList())
                Rows.Remove(row);
        }

        // SFC del lote a partir de los dígitos del Shop Order: pad-izquierda a 8 si es más
        // corto, o los primeros 8 si es más largo.
        private static string DeriveSFCCode(string shopOrderInput)
        {
            string digits = shopOrderInput.Length > 8 ? shopOrderInput.Substring(0, 8) : shopOrderInput.PadLeft(8, '0');
            return PodDefaults.SfcPrefix + digits;
        }
    }
}
