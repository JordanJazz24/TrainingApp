using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Todo lo que un click del POD necesita, cargado una vez por request por
    // PodService.LoadContext (spec 009, AUD-25). Cuando PodService registra un intento,
    // actualiza el contexto (paso actual, estado, puntaje), así que la página no recarga la
    // sesión.
    public class PodContext
    {
        internal PodContext(int operatorId, TrainingSession session, TrainingGuide guide, Area area,
            IList<TrainingSessionStepAttempt> attempts, PodState state, decimal score)
        {
            OperatorId = operatorId;
            Session    = session;
            Guide      = guide;
            Area       = area;
            Attempts   = attempts;
            State      = state;
            Score      = score;
        }

        public int OperatorId { get; }
        public TrainingSession Session { get; }
        public TrainingGuide Guide { get; }
        public Area Area { get; }
        public PodState State { get; }

        // Puntaje actual de la sesión (0..1), con los intentos registrados hasta ahora.
        public decimal Score { get; internal set; }

        internal IList<TrainingSessionStepAttempt> Attempts { get; }

        public TrainingStep CurrentStep
            => Guide.Steps.First(s => s.Order == Session.CurrentStepOrder);
    }
}
