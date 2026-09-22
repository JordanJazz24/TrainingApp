namespace TrainingApp.Domain
{
    public class DataCollectionParameter
    {
        public int Id { get; set; }
        public int AreaId { get; set; }
        public int? OperationId { get; set; }
        public string Name { get; set; }
        public string DataType { get; set; }   // 'Numeric' | 'Boolean' | 'Date'
        public string Unit { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int DisplayOrder { get; set; }
        public string DCGroup { get; set; }
        public string GroupDescription { get; set; }
        public string CollectDataAt { get; set; }
    }
}
