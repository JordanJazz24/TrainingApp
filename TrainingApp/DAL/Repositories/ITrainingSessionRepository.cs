using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    // Lo que PodService lee de la sesión (AUD-74, D-15). Interfaz para que se pruebe con datos
    // en memoria; la implementa TrainingSessionRepository.
    public interface ITrainingSessionRepository
    {
        TrainingSession GetById(int id);
    }
}
