using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Disposición de la reparación extendida (spec 014, D-37b): la dicta el hallazgo.
    public enum NcDisposition
    {
        LocalRework,       // se retrabaja en la operación actual; la pieza sigue en el POD
        ReturnToPrevious   // vuelve a una operación anterior; la pieza sale del POD (D-54)
    }

    // Hallazgo de un paso de Log NC (spec 014, D-28): qué pieza está defectuosa, con qué
    // defecto (un código exacto del catálogo, D-31) y qué hacer con ella. Es dato de escenario:
    // vive en PodState y lo genera NcFindingGenerator al dar Start.
    [Serializable]
    public class NcFinding
    {
        // Paso de Log NC al que pertenece (TrainingStep.Order).
        public int StepOrder { get; set; }

        // Posición entre los hallazgos de la orden ("Hallazgo n de N", D-49).
        public int Index { get; set; }
        public int Total { get; set; }

        public NCMethod Method { get; set; }

        // Serialized: la pieza defectuosa. NonSerialized: null (la pieza correcta es el SFC
        // creado por el split, P-9).
        public string RowId   { get; set; }
        public string SFCCode { get; set; }

        // NonSerialized: "N unidades" del lote, la cantidad del split (enmiendas 2 y 3).
        // 0 en Serialized.
        public int UnitCount { get; set; }

        // El código exacto del hallazgo y el defecto que muestra la nota. Desecho: la descripción
        // del código. Reparaciones (enmienda 5): el código es el general del método y el defecto,
        // el de un código de desecho del área.
        public string Code        { get; set; }
        public string Description { get; set; }

        // Reparación rápida: el componente ensamblado que se reemplaza (D-39, AC-16).
        public int    ComponentId          { get; set; }
        public string ComponentPartNumber  { get; set; }
        public string ComponentDescription { get; set; }

        // Reparación extendida: la disposición (D-37b) y, si el código exige Comments, el MPI
        // simulado que se anota (P-13; no se califica, D-38). null en los otros métodos.
        public NcDisposition? Disposition { get; set; }
        public string Mpi { get; set; }
    }

    // Genera un hallazgo por cada paso de Log NC de la guía (spec 014, Scope punto 3).
    public static class NcFindingGenerator
    {
        // steps: los pasos de Log NC de la guía, en orden. pieces: en Serialized, las piezas de
        // la orden (cada hallazgo, una distinta al azar); en NonSerialized se ignora.
        // components: el BOM ensamblado; la reparación rápida reemplaza uno al azar.
        public static List<NcFinding> Generate(IList<TrainingStep> steps, IList<SimulatedOrderRow> pieces,
            SerializationType serializationType, IList<NCCode> codes, IList<AssemblyComponent> components,
            int lotQty, Random random)
        {
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            if (random == null) throw new ArgumentNullException(nameof(random));

            var logNcSteps = steps.Where(s => StepTypes.IsLogNC(s.StepType)).OrderBy(s => s.Order).ToList();
            var result = new List<NcFinding>(logNcSteps.Count);
            if (logNcSteps.Count == 0) return result;

            bool serialized = serializationType == SerializationType.Serialized;
            if (!serialized && lotQty < 2)
                throw new ArgumentOutOfRangeException(nameof(lotQty), "El lote necesita al menos 2 unidades para un split.");
            var available = serialized ? new List<SimulatedOrderRow>(pieces ?? new List<SimulatedOrderRow>()) : null;
            // Siempre queda una pieza para Complete: con 4-5 piezas y hasta 3 hallazgos alcanza
            // (el desecho y Return to any previous sacan cada uno su pieza del POD).
            if (serialized && available.Count <= logNcSteps.Count)
                throw new InvalidOperationException("La orden no tiene piezas suficientes para los hallazgos de Log NC.");

            // Defectos ya usados en la orden: cada hallazgo muestra otro mientras el catálogo alcance
            // (usuario, 2026-10-19: con elecciones independientes se repetían seguido).
            var usedDefects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var scrapCodes  = (codes ?? new List<NCCode>()).Where(c => c.Method == NCMethod.FastScrap).ToList();

            for (int i = 0; i < logNcSteps.Count; i++)
            {
                var method = StepTypes.MethodOf(logNcSteps[i].StepType).Value;
                var candidates = (codes ?? new List<NCCode>()).Where(c => c.Method == method).ToList();
                if (candidates.Count == 0)
                    throw new InvalidOperationException("El catálogo del área no tiene códigos NC del método " + method + ".");

                // Desecho: el código es el defecto. Reparaciones: el código es el general y el
                // defecto, el de un código de desecho del área (o el propio, si no hay ninguno).
                bool scrap = method == NCMethod.FastScrap;
                var code = scrap ? PickUnused(candidates, usedDefects, random) : candidates[random.Next(candidates.Count)];
                string defect = scrap || scrapCodes.Count == 0
                    ? code.Description
                    : PickUnused(scrapCodes, usedDefects, random).Description;
                usedDefects.Add(defect);

                var finding = new NcFinding
                {
                    StepOrder   = logNcSteps[i].Order,
                    Index       = i + 1,
                    Total       = logNcSteps.Count,
                    Method      = method,
                    Code        = code.Code,
                    Description = defect
                };

                if (method == NCMethod.FastRepair)
                {
                    if (components == null || components.Count == 0)
                        throw new InvalidOperationException("La reparación rápida necesita componentes ensamblados (BOM del área).");
                    var component = components[random.Next(components.Count)];
                    finding.ComponentId          = component.Id;
                    finding.ComponentPartNumber  = component.PartNumber;
                    finding.ComponentDescription = component.Description;
                }
                else if (method == NCMethod.ExtendedRepair)
                {
                    finding.Disposition = random.Next(2) == 0 ? NcDisposition.LocalRework : NcDisposition.ReturnToPrevious;
                    if (code.RequiresComments)
                        finding.Mpi = SimulatedMpi(random);
                }

                if (serialized)
                {
                    var piece = available[random.Next(available.Count)];
                    available.Remove(piece);
                    finding.RowId   = piece.RowId;
                    finding.SFCCode = piece.SFCCode;
                }
                else
                {
                    // De 1 a lote − 1: nunca el lote completo, que el split no puede dividir
                    // (enmienda 3 de la spec 014).
                    finding.UnitCount = random.Next(1, lotQty);
                }
                result.Add(finding);
            }
            return result;
        }

        // Un código al azar cuya descripción todavía no salió en la orden; si ya salieron todas,
        // cualquiera.
        private static NCCode PickUnused(IList<NCCode> codes, ISet<string> usedDefects, Random random)
        {
            var pool = codes.Where(c => !usedDefects.Contains(c.Description ?? string.Empty)).ToList();
            if (pool.Count == 0) pool = codes.ToList();
            return pool[random.Next(pool.Count)];
        }

        // MPI y revisión simulados (P-13), con el prefijo SIM como el resto del catálogo simulado.
        private static string SimulatedMpi(Random random)
            => string.Format(CultureInfo.InvariantCulture, "MPI-SIM-{0:D4} Rev {1}",
                random.Next(1000, 10000), (char)('A' + random.Next(4)));
    }
}
