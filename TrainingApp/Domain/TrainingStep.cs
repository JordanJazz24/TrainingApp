namespace TrainingApp.Domain
{
    public enum StepType
    {
        ResourceSetup,
        LineClearance,
        Search,
        Assembly,
        DataCollection,
        LogNC,
        Complete
    }

    public class TrainingStep
    {
        public int Id { get; set; }
        public int TrainingGuideId { get; set; }
        public StepType StepType { get; set; }
        public int Order { get; set; }
        public decimal Weight { get; set; }
    }
}
