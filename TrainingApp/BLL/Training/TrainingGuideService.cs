using System.Collections.Generic;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;


namespace TrainingApp.BLL.Training
{
    public class TrainingGuideService
    {
        private readonly TrainingGuideRepository _guideRepo;
        private readonly AreaRepository _areaRepo;
        private readonly NCCodeRepository _ncCodeRepo;
        private readonly DataCollectionParameterRepository _dcParamRepo;
        private readonly DataCollectionGroupRepository _dcGroupRepo;
        private readonly AssemblyComponentRepository _componentRepo;
        private readonly ProductRepository _productRepo;

        public TrainingGuideService()
        {
            _guideRepo     = new TrainingGuideRepository();
            _areaRepo      = new AreaRepository();
            _ncCodeRepo    = new NCCodeRepository();
            _dcParamRepo   = new DataCollectionParameterRepository();
            _dcGroupRepo   = new DataCollectionGroupRepository();
            _componentRepo = new AssemblyComponentRepository();
            _productRepo   = new ProductRepository();
        }

        // DC Groups del área, en el orden de la Data Collection List (spec 008).
        public IList<DataCollectionGroup> GetDataCollectionGroups(int areaId)
        {
            return _dcGroupRepo.GetByArea(areaId);
        }

        // BOM de la operación (hoy por área) para la Component List de Assembly (spec 008).
        public IList<AssemblyComponent> GetAssemblyComponents(int areaId)
        {
            return _componentRepo.GetByArea(areaId);
        }

        public IList<Product> GetAllProducts()
        {
            return _productRepo.GetAll();
        }

        public IList<Area> GetAreasByProduct(int productId)
        {
            return _areaRepo.GetByProduct(productId);
        }

        // Área (flags de SerializationType y Requires*) y catálogos que lee el POD; la página
        // no llama a la DAL directamente (auditoría AUD-14).
        public Area GetArea(int areaId)
        {
            return _areaRepo.GetById(areaId);
        }

        public IList<NCCode> GetNCCodes(int areaId)
        {
            return _ncCodeRepo.GetByArea(areaId);
        }

        public IList<DataCollectionParameter> GetDataCollectionParameters(int areaId)
        {
            return _dcParamRepo.GetByArea(areaId);
        }

        public TrainingGuide GetById(int id)
        {
            return _guideRepo.GetById(id);
        }

        public IList<TrainingGuide> GetByArea(int areaId)
        {
            return _guideRepo.GetByArea(areaId);
        }
    }
}
