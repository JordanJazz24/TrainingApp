using System.Collections.Generic;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Lo que muestra el inicio del operador (spec 021, D-167/D-168): su área como texto y las
    // guías publicadas de esa área. Sin área: HasArea false, AreaText null y sin guías.
    public class OperatorStartOptions
    {
        public bool HasArea { get; set; }
        public string AreaText { get; set; }
        public IList<TrainingGuide> Guides { get; set; } = new List<TrainingGuide>();
    }
}
