using System;

namespace TrainingApp.Domain
{
    public enum SessionMode { Guided, Assessment, FreePractice }
    public enum SessionStatus { InProgress, Completed, Abandoned }

    public class TrainingSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TrainingGuideId { get; set; }
        public SessionMode Mode { get; set; }
        public SessionStatus Status { get; set; }
        public int CurrentStepOrder { get; set; }
        public decimal FinalScore { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // El de la guía al empezar (spec 020, D-188): un cambio del admin no la afecta.
        public ResourceSetupType ResourceSetupType { get; set; }
    }
}
