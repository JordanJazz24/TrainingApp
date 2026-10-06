using System;

namespace TrainingApp.BLL.Training
{
    // Estado del POD de una sesión de entrenamiento (spec 009): vive en Session (D-10) y solo
    // lo modifica PodService. Un solo objeto: la orden y el avance se pierden o se conservan
    // juntos, así que un avance nunca apunta a filas de otra orden.
    [Serializable]
    public class PodState
    {
        // null hasta Resource Setup.
        public SimulatedOrder Order { get; set; }

        // null hasta el primer componente, o cuando el paso ya se resolvió.
        public AssemblyProgress Assembly { get; set; }

        // null hasta la primera apertura de DC (que fija las piezas), o cuando el paso ya se resolvió.
        public DataCollectionProgress DataCollection { get; set; }
    }

    // Dónde guarda la BLL el estado del POD. La BLL no depende de System.Web: la página le
    // pasa una implementación sobre la Session (Pages/Training/SessionPodStateStore).
    public interface IPodStateStore
    {
        // null si la sesión todavía no tiene estado (o se perdió la Session).
        PodState Load(int trainingSessionId);

        void Save(int trainingSessionId, PodState state);

        void Remove(int trainingSessionId);

        // Estado de todas las sesiones de este operador (D-9 deja una sola en curso).
        void RemoveAll();
    }
}
