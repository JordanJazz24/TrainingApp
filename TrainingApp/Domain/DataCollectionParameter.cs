namespace TrainingApp.Domain
{
    public class DataCollectionParameter
    {
        public int Id { get; set; }
        public int AreaId { get; set; }
        public int? OperationId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; } // "Description: …" bajo la pregunta en DC Entry; null = no se muestra
        public string DataType { get; set; }   // 'Numeric' | 'Boolean' | 'Date'
        public string Unit { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int DisplayOrder { get; set; }
        public int DataCollectionGroupId { get; set; }
    }
}
