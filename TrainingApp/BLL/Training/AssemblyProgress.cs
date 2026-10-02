using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Avance del paso Assembly de una sesión (spec 008): qué componentes del BOM ya se
    // ensamblaron. Cada escaneo se aplica a todas las piezas seleccionadas ("Apply to all
    // SFCs"), así que no hay avance por pieza. Vive en Session (servidor); la BLL decide
    // cuándo el paso queda completo.
    [Serializable]
    public class AssemblyProgress
    {
        public HashSet<int> AssembledComponentIds { get; } = new HashSet<int>();

        // El paso se completa cuando todo el BOM quedó ensamblado. Un BOM vacío es un error
        // de configuración del catálogo (E1) y nunca completa el paso.
        public bool Covers(IList<AssemblyComponent> bom)
        {
            return bom != null && bom.Count > 0
                && bom.All(c => AssembledComponentIds.Contains(c.Id));
        }

        // Si con este componente el BOM queda completo, sin modificar el avance: la BLL
        // registra el intento antes de guardar el último componente (auditoría AUD-06).
        public bool CoversWith(int componentId, IList<AssemblyComponent> bom)
        {
            return bom != null && bom.Count > 0
                && bom.All(c => c.Id == componentId || AssembledComponentIds.Contains(c.Id));
        }
    }

    public class AssemblySubmitResult
    {
        // El componente pertenece al BOM del área y quedó ensamblado.
        public bool ComponentAssembled { get; set; }

        // Resultado del paso al ensamblar el último componente; null mientras falten.
        public StepSubmitResult StepResult { get; set; }
    }
}
