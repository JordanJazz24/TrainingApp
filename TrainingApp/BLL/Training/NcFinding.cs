using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
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

        // NonSerialized: "N unidades" del lote, informativo (D-32). 0 en Serialized.
        public int UnitCount { get; set; }

        // El código exacto del hallazgo y su descripción (el defecto que muestra la nota).
        public string Code        { get; set; }
        public string Description { get; set; }
    }

    // Genera un hallazgo por cada paso de Log NC de la guía (spec 014, Scope punto 3).
    public static class NcFindingGenerator
    {
        // steps: los pasos de Log NC de la guía, en orden. pieces: en Serialized, las piezas de
        // la orden (cada hallazgo, una distinta al azar); en NonSerialized se ignora.
        public static List<NcFinding> Generate(IList<TrainingStep> steps, IList<SimulatedOrderRow> pieces,
            SerializationType serializationType, IList<NCCode> codes, int lotQty, Random random)
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
            // Siempre queda una pieza para Complete: con 4-5 piezas y hasta 3 hallazgos alcanza.
            if (serialized && available.Count <= logNcSteps.Count)
                throw new InvalidOperationException("La orden no tiene piezas suficientes para los hallazgos de Log NC.");

            for (int i = 0; i < logNcSteps.Count; i++)
            {
                var method = StepTypes.MethodOf(logNcSteps[i].StepType).Value;
                var candidates = (codes ?? new List<NCCode>()).Where(c => c.Method == method).ToList();
                if (candidates.Count == 0)
                    throw new InvalidOperationException("El catálogo del área no tiene códigos NC del método " + method + ".");
                var code = candidates[random.Next(candidates.Count)];

                var finding = new NcFinding
                {
                    StepOrder   = logNcSteps[i].Order,
                    Index       = i + 1,
                    Total       = logNcSteps.Count,
                    Method      = method,
                    Code        = code.Code,
                    Description = code.Description
                };

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
    }
}
