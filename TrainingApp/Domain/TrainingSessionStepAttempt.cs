using System;

namespace TrainingApp.Domain
{
    public class TrainingSessionStepAttempt
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int StepId { get; set; }
        public int AttemptNumber { get; set; }
        public bool IsCorrect { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
