using System.Collections.Generic;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    // Lo que PodService usa de los intentos (AUD-74, D-15). Interfaz para que se pruebe con
    // datos en memoria; la implementa StepAttemptRepository.
    public interface IStepAttemptRepository
    {
        // Guarda el intento y, en la misma transacción, avanza la sesión a nextStepOrder o la
        // cierra con finalScore (auditoría AUD-06). Ambos null = solo el intento. stepOrder es
        // el paso en el que la BLL tomó la decisión: el SP falla si la sesión ya no está ahí.
        // Contrato para un fake: con finalScore, la sesión queda Completed con ese puntaje y
        // CompletedAt, y ITrainingSessionRepository.GetById la devuelve así (PodService la lee
        // de vuelta para mostrar el puntaje guardado).
        void Record(TrainingSessionStepAttempt attempt, int stepOrder, int? nextStepOrder,
            decimal? finalScore);

        // La lista devuelta tiene que poder modificarse: PodService le agrega el intento nuevo
        // (ctx.Attempts.Add). Un arreglo o una colección de solo lectura no sirve.
        IList<TrainingSessionStepAttempt> GetBySessionId(int sessionId);
    }
}
