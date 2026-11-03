using System.Collections.Generic;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Lo que PodService y PodContext piden al catálogo de guías (AUD-74, D-15): la guía, el
    // área y los catálogos del área. Interfaz para probarlos con datos en memoria; la
    // implementa TrainingGuideService.
    public interface IGuideCatalog
    {
        TrainingGuide GetById(int id);
        Area GetArea(int areaId);
        IList<Product> GetAllProducts();
        IList<DataCollectionGroup> GetDataCollectionGroups(int areaId);
        IList<DataCollectionParameter> GetDataCollectionParameters(int areaId);
        IList<AssemblyComponent> GetAssemblyComponents(int areaId);
        IList<NCCode> GetNCCodes(int areaId);
        IList<Operation> GetOperations(int areaId);
    }
}
