namespace TrainingApp.Domain
{
    // Componente del BOM de la operación (hoy por área, a futuro por Operation — spec 008).
    // La Component List de Assembly lo muestra como PartNumber/Revision.
    public class AssemblyComponent
    {
        public int Id { get; set; }
        public int AreaId { get; set; }
        public int? OperationId { get; set; }
        public string PartNumber { get; set; }
        public string Revision { get; set; }
        public string Description { get; set; }
        public decimal Qty { get; set; }   // por unidad; puede ser fraccionaria (ej. 0.0001)
        public int DisplayOrder { get; set; }
    }
}
