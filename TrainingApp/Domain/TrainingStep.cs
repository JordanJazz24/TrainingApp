namespace TrainingApp.Domain
{
    public enum StepType
    {
        ResourceSetup = 0,
        LineClearance = 1,
        Search = 2,
        Assembly = 3,
        DataCollection = 4,
        SFC_Split = 5,
        LogNC = 6,
        Complete = 7,
        Logout = 8
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
