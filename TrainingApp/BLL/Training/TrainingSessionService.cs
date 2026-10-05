using System;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Ciclo de vida de una TrainingSession: iniciar y abandonar. Los pasos, el registro de
    // intentos y el estado del POD son de PodService (spec 009).
    public class TrainingSessionService
    {
        private readonly TrainingSessionRepository _sessionRepo = new TrainingSessionRepository();

        public TrainingSession Start(int operatorId, int guideId, SessionMode mode)
        {
            // La sesión que quedó a medias en esta guía se marca Abandoned y conserva sus
            // intentos para el instructor (auditoría AUD-01, decisión D-4).
            _sessionRepo.AbandonActiveByOperator(operatorId, guideId);

            var session = new TrainingSession
            {
                OperatorId       = operatorId,
                TrainingGuideId  = guideId,
                Mode             = mode,
                Status           = SessionStatus.InProgress,
                CurrentStepOrder = 1,
                FinalScore       = 0m,
                StartedAt        = DateTime.UtcNow
            };
            session.Id = _sessionRepo.Insert(session);
            return session;
        }

        public void Abandon(int sessionId, int operatorId)
        {
            LoadOwnedSession(sessionId, operatorId);
            _sessionRepo.Abandon(sessionId);
        }

        // Dueño de la sesión (auditoría AUD-05): la página ya lo valida, pero la BLL no confía
        // en ella. Si no se cumple es una manipulación o un error de programación, no un
        // intento, así que no se registra nada.
        private TrainingSession LoadOwnedSession(int sessionId, int operatorId)
        {
            var session = _sessionRepo.GetById(sessionId);
            if (session == null || session.OperatorId != operatorId)
                throw new InvalidOperationException("La sesión no existe o no pertenece al operador.");
            return session;
        }
    }
}
