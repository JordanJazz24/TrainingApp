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

        public TrainingSession Start(int userId, TrainingGuide guide, SessionMode mode)
        {
            if (guide == null) throw new ArgumentNullException(nameof(guide));

            // Una sola sesión activa por operador (spec 009, D-9): la que quedó a medias, de
            // esta u otra guía, se marca Abandoned y conserva sus intentos para el instructor
            // (auditoría AUD-01, decisión D-4).
            AbandonAllActive(userId);

            var session = NewSession(userId, guide, mode, DateTime.UtcNow);
            session.Id = _sessionRepo.Insert(session);
            return session;
        }

        // La sesión copia el tipo de Resource Setup de la guía al empezar (spec 020, D-188).
        public static TrainingSession NewSession(int userId, TrainingGuide guide, SessionMode mode, DateTime startedAt)
            => new TrainingSession
            {
                UserId            = userId,
                TrainingGuideId   = guide.Id,
                Mode              = mode,
                Status            = SessionStatus.InProgress,
                CurrentStepOrder  = 1,
                FinalScore        = 0m,
                StartedAt         = startedAt,
                ResourceSetupType = guide.ResourceSetupType
            };

        public void Abandon(int sessionId, int userId)
        {
            LoadOwnedSession(sessionId, userId);
            _sessionRepo.Abandon(sessionId);
        }

        // Logout (D-9): salir deja Abandoned las sesiones en curso del operador. Cerrar la
        // ventana no llega al servidor: esa sesión queda InProgress hasta el siguiente inicio.
        public void AbandonAllActive(int userId)
        {
            _sessionRepo.AbandonActiveByUser(userId);
        }

        // Dueño de la sesión (auditoría AUD-05): la página ya lo valida, pero la BLL no confía
        // en ella. Si no se cumple es una manipulación o un error de programación, no un
        // intento, así que no se registra nada.
        private TrainingSession LoadOwnedSession(int sessionId, int userId)
        {
            var session = _sessionRepo.GetById(sessionId);
            if (session == null || session.UserId != userId)
                throw new InvalidOperationException("La sesión no existe o no pertenece al operador.");
            return session;
        }
    }
}
