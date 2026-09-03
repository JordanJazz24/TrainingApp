using System.Collections.Generic;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    public class TrainingGuideService
    {
        private readonly TrainingGuideRepository _guideRepo;

        public TrainingGuideService()
        {
            _guideRepo = new TrainingGuideRepository();
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
