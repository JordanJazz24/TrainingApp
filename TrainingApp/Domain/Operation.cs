namespace TrainingApp.Domain
{
    // Future level between Area and TrainingGuide.
    // OperationId is a nullable FK on TrainingGuide — not used today.
    // Each Area will have 3-10 Operations when this level is activated.
    public class Operation
    {
        public int Id { get; set; }
        public int AreaId { get; set; }
        public string Name { get; set; }
    }
}
