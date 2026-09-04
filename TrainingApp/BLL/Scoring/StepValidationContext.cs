using System.Collections.Generic;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Scoring
{
    public class StepValidationContext
    {
        public Area Area { get; set; }
        public IList<NCCode> ValidNCCodes { get; set; }
        public IList<DataCollectionParameter> DataCollectionParams { get; set; }
        public int? ExpectedComponentCount { get; set; }
    }
}
