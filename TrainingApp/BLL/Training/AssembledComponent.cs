using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Lo ensamblado en Assembly, por componente (spec 014): el Browse de Component y Remove
    // component lo leen después del paso. Cada escaneo se aplica a todas las piezas ("Apply to
    // all SFCs"), así que es uno por orden. Vive en PodState.
    [Serializable]
    public class AssembledComponent
    {
        public int     ComponentId  { get; set; }
        public string  PartNumber   { get; set; }
        public string  Revision     { get; set; }
        public string  Description  { get; set; }
        public decimal Qty          { get; set; }

        // Assembly Data registrada (hoy: Batch Number = Process Lot de la orden; W13 en E4).
        public string  BatchNumber  { get; set; }
        public string  InventorySfc { get; set; }

        public static AssembledComponent From(AssemblyComponent c, string batchNumber)
            => new AssembledComponent
            {
                ComponentId = c.Id,
                PartNumber  = c.PartNumber,
                Revision    = c.Revision,
                Description = c.Description,
                Qty         = c.Qty,
                BatchNumber = batchNumber
            };

        // Agrega los componentes del BOM que falten, en su orden, con la Assembly Data por
        // defecto (paso resuelto forzado o estado reconstruido).
        public static List<AssembledComponent> Complete(List<AssembledComponent> recorded,
            IList<AssemblyComponent> bom, string batchNumber)
        {
            var result = recorded ?? new List<AssembledComponent>();
            foreach (var c in bom ?? new List<AssemblyComponent>())
                if (!result.Any(r => r.ComponentId == c.Id))
                    result.Add(From(c, batchNumber));
            return result;
        }
    }
}
