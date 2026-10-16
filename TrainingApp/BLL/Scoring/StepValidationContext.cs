using System.Collections.Generic;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Scoring
{
    public class StepValidationContext
    {
        public Area Area { get; set; }
        public IList<NCCode> ValidNCCodes { get; set; }
        public IList<DataCollectionParameter> DataCollectionParams { get; set; }

        // Equipos precargados de Resource Setup (spec 015): guardar con alguno es incorrecto.
        public IList<string> PreloadedValues { get; set; }

        // Log NC (spec 014): el hallazgo del paso y las filas del POD seleccionadas al enviar.
        public NcFinding NcFinding { get; set; }
        public IList<SimulatedOrderRow> SelectedRows { get; set; }
    }
}
