using System;
using System.Linq;
using System.Web.SessionState;
using TrainingApp.BLL.Training;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Training
{
    // Estado del POD en la Session del operador (spec 009, D-10): una clave por sesión de
    // entrenamiento. Solo PodService lo lee y lo escribe.
    public class SessionPodStateStore : IPodStateStore
    {
        private readonly HttpSessionState _session;

        public SessionPodStateStore(HttpSessionState session)
        {
            _session = session;
        }

        public PodState Load(int trainingSessionId) => _session[Key(trainingSessionId)] as PodState;

        public void Save(int trainingSessionId, PodState state) => _session[Key(trainingSessionId)] = state;

        public void Remove(int trainingSessionId) => _session.Remove(Key(trainingSessionId));

        public void RemoveAll()
        {
            var keys = _session.Keys.Cast<string>()
                .Where(k => k.StartsWith(Constants.SessionKeys.PodStatePrefix, StringComparison.Ordinal))
                .ToList();
            foreach (var key in keys)
                _session.Remove(key);
        }

        private static string Key(int trainingSessionId)
            => Constants.SessionKeys.PodStatePrefix + trainingSessionId;
    }
}
