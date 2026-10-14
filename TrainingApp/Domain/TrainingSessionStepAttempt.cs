using System;

namespace TrainingApp.Domain
{
    // Por qué un intento falló (spec 013, registro de actividad). Propio del registro: no es
    // PodAttemptReason, que solo elige el mensaje de la UI. Se guarda como TINYINT: los
    // valores no se reordenan.
    public enum AttemptReason
    {
        None               = 0,   // intento correcto
        OutOfSequence      = 1,   // botón que no es el del paso actual (Assessment, S2)
        WrongDecision      = 2,   // decisión incorrecta (Setup State, Sí/No, código NC, valores de DC…)
        InvalidAction      = 3,   // botón equivocado dentro de un paso (ej. Add o Done en vez de Add-Done)
        MissingSelection   = 4,   // Log NC o Complete sin la fila que exige el paso
        EmptyRequiredField = 5    // Data Collection con un valor vacío
    }

    public class TrainingSessionStepAttempt
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int StepId { get; set; }
        public int AttemptNumber { get; set; }
        public bool IsCorrect { get; set; }
        public DateTime Timestamp { get; set; }

        // Registro de actividad (spec 013): motivo y acción del operador. Null en los intentos
        // anteriores a la spec ("sin dato" para el dashboard de la Fase 10).
        public AttemptReason? Reason { get; set; }
        public string Action { get; set; }
    }
}
