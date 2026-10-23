using System;

namespace TrainingApp.Domain
{
    // Un intento de una sesión, para el dashboard (spec 018): el paso (con su orden), el
    // resultado, el motivo, la acción y la hora. Lo que es fallo, salto o paso sin acierto lo
    // decide la BLL (StepRuns).
    public class DashboardAttempt
    {
        public int SessionId { get; set; }
        public int StepId { get; set; }
        public int StepOrder { get; set; }
        public int AttemptNumber { get; set; }
        public bool IsCorrect { get; set; }
        public DateTime Timestamp { get; set; }
        public AttemptReason? Reason { get; set; }
        public string Action { get; set; }
    }
}
