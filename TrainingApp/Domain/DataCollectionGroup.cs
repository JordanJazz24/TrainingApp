namespace TrainingApp.Domain
{
    // DC Group de un área (spec 008). Una pieza completa Data Collection cuando guardó
    // todos los grupos obligatorios; si el área no tiene ninguno, basta uno cualquiera.
    public class DataCollectionGroup
    {
        public int Id { get; set; }
        public int AreaId { get; set; }
        public int? OperationId { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string CollectDataAt { get; set; }   // 'Pre-Start' | 'Post-Start' | 'Anytime'
        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
    }
}
