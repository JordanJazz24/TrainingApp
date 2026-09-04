using System.Collections.Generic;
using TrainingApp.BLL.Scoring;
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
        private readonly ProductRepository _productRepo;

        public TrainingGuideService()
        {
            _guideRepo   = new TrainingGuideRepository();
            _areaRepo    = new AreaRepository();
            _ncCodeRepo  = new NCCodeRepository();
            _dcParamRepo = new DataCollectionParameterRepository();
            _productRepo = new ProductRepository();
        }

        public IList<Product> GetAllProducts()
        {
            return _productRepo.GetAll();
        }

        public IList<Area> GetAreasByProduct(int productId)
        {
            return _areaRepo.GetByProduct(productId);
        }

        public TrainingGuide GetById(int id)
        {
            return _guideRepo.GetById(id);
        }

        public IList<TrainingGuide> GetByArea(int areaId)
        {
            return _guideRepo.GetByArea(areaId);
        }

        // Loads all catalog data for the guide's area and returns a ready-to-use
        // StepValidator. Call once per session, then pass the validator to each
        // step submission.
        public StepValidator BuildValidatorForGuide(TrainingGuide guide)
        {
            var area      = _areaRepo.GetById(guide.AreaId);
            var ncCodes   = _ncCodeRepo.GetByArea(guide.AreaId);
            var dcParams  = _dcParamRepo.GetByArea(guide.AreaId);

            // ExpectedComponentCount: not yet in DB schema (pending when Assembly areas are added).
            // For Moldeo (RequiresAssembly=false) the Assembly step is absent from the guide,
            // so this code path is never hit.
            var context = new StepValidationContext
            {
                Area                   = area,
                ValidNCCodes           = ncCodes,
                DataCollectionParams   = dcParams,
                ExpectedComponentCount = null
            };

            return new StepValidator(context);
        }
    }
}
