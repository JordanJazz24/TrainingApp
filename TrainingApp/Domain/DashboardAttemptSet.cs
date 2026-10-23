using System.Collections.Generic;

namespace TrainingApp.Domain
{
    // Los intentos de un período, una guía y un modo, con sus sesiones (spec 018, Pasos; D-146).
    // Las sesiones traen lo que necesita StepRuns (guía, modo, estado e inicio) y su dueño.
    public class DashboardAttemptSet
    {
        public IList<DashboardSession> Sessions { get; set; }
        public IList<DashboardAttempt> Attempts { get; set; }
    }
}
