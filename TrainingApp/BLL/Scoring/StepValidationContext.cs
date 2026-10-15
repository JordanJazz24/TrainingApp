using System.Collections.Generic;
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
    }
}
