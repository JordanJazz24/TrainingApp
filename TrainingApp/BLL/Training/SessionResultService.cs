using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Auth;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // De dónde lee el servicio. Una interfaz para probarlo con datos en memoria (spec 025, P-14):
    // PodService no admite repositorios inyectados (D-15), este servicio sí.
    public interface ISessionResultSource
    {
        TrainingSession GetSession(int sessionId);
        IList<TrainingSessionStepAttempt> GetAttempts(int sessionId);

        // La guía con sus pasos (el repositorio de guías los puebla).
        TrainingGuide GetGuide(int guideId);

        // "Producto · Área" del área de una guía.
        string AreaText(int areaId);
        IList<CompletedSessionItem> GetRecentCompleted(int userId, int top);
    }

    public class RepositorySessionResultSource : ISessionResultSource
    {
        private readonly TrainingSessionRepository _sessions = new TrainingSessionRepository();
        private readonly StepAttemptRepository _attempts = new StepAttemptRepository();
        private readonly TrainingGuideRepository _guides = new TrainingGuideRepository();
        private readonly AreaRepository _areas = new AreaRepository();
        private readonly ProductRepository _products = new ProductRepository();

        public TrainingSession GetSession(int sessionId) => _sessions.GetById(sessionId);
        public IList<TrainingSessionStepAttempt> GetAttempts(int sessionId) => _attempts.GetBySessionId(sessionId);
        public TrainingGuide GetGuide(int guideId) => _guides.GetById(guideId);

        public string AreaText(int areaId)
        {
            var area = _areas.GetById(areaId);
            if (area == null) return null;
            var product = _products.GetAll().FirstOrDefault(p => p.Id == area.ProductId);
            return AreaLabel.Of(product?.Name, area.Name);
        }

        public IList<CompletedSessionItem> GetRecentCompleted(int userId, int top)
            => _sessions.GetRecentCompleted(userId, top);
    }

    // Una acción del fin de sesión: lleva al Inicio, con el modo y la guía ya elegidos o sin
    // nada. Nunca inicia la sesión (D-337): Iniciar sigue siendo el único camino que la crea.
    public class SessionAction
    {
        // null = Ir al Inicio sin preselección.
        public SessionMode? Mode { get; set; }
        public string Url { get; set; }
        public bool IsHome => !Mode.HasValue;
    }

    // El resultado de una sesión completada con lo que hace falta para pintarlo (spec 025).
    public class SessionResult
    {
        public SessionResultDetail Detail { get; set; }
        public int GuideId { get; set; }
        public string GuideTitle { get; set; }

        // "Producto · Área" de la guía de la sesión.
        public string GuideText { get; set; }

        // La acción principal (D-332): el modo sugerido, o Ir al Inicio si el modo no sugiere
        // ninguno o la guía ya no se puede iniciar.
        public SessionAction Primary { get; set; }

        // Repetir (misma guía y modo); null si la guía ya no se puede iniciar o coincide con la
        // principal (D-342).
        public SessionAction Repeat { get; set; }

        // Ir al Inicio (la secundaria que siempre está).
        public SessionAction Home { get; set; }
    }

    // Una fila de "Últimas sesiones" del Inicio (spec 025, D-335).
    public class RecentSession
    {
        public int SessionId { get; set; }
        public SessionMode Mode { get; set; }

        // Cierre en UTC; la página lo pasa a la hora de la planta (P-12).
        public DateTime CompletedAt { get; set; }

        // "Producto · Área" solo si la sesión es de OTRA área que la actual del operador (P-5);
        // null si es la misma.
        public string AreaText { get; set; }
    }

    // El fin de sesión (spec 025, P-2, P-3, P-5, P-8). Protege el invariante sin confiar en la
    // página: el resultado de una sesión solo sale para su dueño y completada; el sessionId de
    // la URL nunca se usa sin esa validación.
    public class SessionResultService
    {
        public const string StartUrl = "~/Pages/Training/StartSession.aspx";

        // Cuántas sesiones muestra el Inicio (D-335).
        public const int RecentCount = 5;

        private readonly IUserRepository _users;
        private readonly ISessionResultSource _source;

        public SessionResultService() : this(new UserRepository(), new RepositorySessionResultSource())
        {
        }

        public SessionResultService(IUserRepository users, ISessionResultSource source)
        {
            _users  = users ?? throw new ArgumentNullException(nameof(users));
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        // El resultado, o null si la sesión no existe, no es del usuario o no está completada
        // (otra persona, en curso, abandonada o inexistente: la página va al Inicio sin decir por
        // qué, P-2). Quien no puede entrenar (un admin) es una excepción: no es un intento.
        public SessionResult Get(int userId, int sessionId)
        {
            var user = RequireTrainer(userId);

            var session = _source.GetSession(sessionId);
            if (session == null || session.UserId != userId || session.Status != SessionStatus.Completed)
                return null;

            var guide = _source.GetGuide(session.TrainingGuideId);
            if (guide?.Steps == null) return null;

            var detail = SessionResultRule.Build(session, guide.Steps, _source.GetAttempts(sessionId));
            var home = new SessionAction { Mode = null, Url = StartUrl };
            var result = new SessionResult
            {
                Detail     = detail,
                GuideId    = guide.Id,
                GuideTitle = guide.Title,
                GuideText  = _source.AreaText(guide.AreaId),
                Primary    = home,
                Home       = home
            };

            // Si la guía ya no se puede iniciar (no publicada o de otra área, D-173), solo Ir al
            // Inicio: ni acción de modo ni Repetir (P-8).
            if (!TrainingAccessRule.IsStartable(user, guide)) return result;

            var suggested = detail.Policy.SuggestedNextMode(detail.WentWell);
            if (suggested.HasValue)
                result.Primary = ActionFor(suggested.Value, guide.Id);

            // Repetir no aparece cuando coincide con la principal (D-342).
            if (!(result.Primary.Mode == session.Mode))
                result.Repeat = ActionFor(session.Mode, guide.Id);
            return result;
        }

        // Las últimas 5 completadas del operador, de la más reciente (D-335). Vacía = el bloque
        // no aparece. El área solo si es otra que la actual (P-5).
        public IList<RecentSession> GetRecent(int userId)
        {
            var user = RequireTrainer(userId);
            return _source.GetRecentCompleted(userId, RecentCount)
                .OrderByDescending(i => i.CompletedAt).ThenByDescending(i => i.SessionId)
                .Take(RecentCount)
                .Select(i => new RecentSession
                {
                    SessionId   = i.SessionId,
                    Mode        = i.Mode,
                    CompletedAt = i.CompletedAt,
                    AreaText    = user.AreaId.HasValue && i.AreaId == user.AreaId.Value
                        ? null : AreaLabel.Of(i.ProductName, i.AreaName)
                })
                .ToList();
        }

        // El Inicio con el modo y la guía elegidos (D-337). La BLL del Inicio los valida otra vez
        // (OperatorStartOptions.Preselect).
        public static SessionAction ActionFor(SessionMode mode, int guideId)
            => new SessionAction { Mode = mode, Url = StartUrl + "?mode=" + (int)mode + "&guide=" + guideId };

        private User RequireTrainer(int userId)
        {
            var user = _users.GetById(userId);
            if (user == null || !AccessPolicy.CanTrain(user.Role))
                throw new UnauthorizedAccessException("El usuario no puede entrenar.");
            return user;
        }
    }
}
