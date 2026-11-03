using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Iniciar y abandonar una sesión, lo que PodService usa del ciclo de vida (AUD-74, D-15).
    // Interfaz para probarlo con datos en memoria; la implementa TrainingSessionService.
    public interface ITrainingSessionService
    {
        TrainingSession Start(int userId, TrainingGuide guide, SessionMode mode);
        void Abandon(int sessionId, int userId);
    }
}
