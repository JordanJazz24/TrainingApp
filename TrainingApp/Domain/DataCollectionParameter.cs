namespace TrainingApp.Domain
{
    public class DataCollectionParameter
    {
        public int Id { get; set; }
        public int AreaId { get; set; }
        public int? OperationId { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string MinValue { get; set; }
        public string MaxValue { get; set; }
    }
}
