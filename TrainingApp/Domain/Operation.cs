namespace TrainingApp.Domain
{
    // Future level between Area and TrainingGuide.
    // OperationId is a nullable FK on TrainingGuide — not used today.
    // Each Area will have 3-10 Operations when this level is activated.
    // Spec 014 (D-58): today only the simulated (SIM) operations named by the
    // extended-repair banner.
    public class Operation
    {
        public int Id { get; set; }
        public int AreaId { get; set; }
        public string Name { get; set; }

        // Número de la operación en la ruta (10, 20…): la actual es la de mayor Sequence.
        public int? Sequence { get; set; }

        // Texto del MES, ej. "Ensamble (SIM)".
        public string Description { get; set; }
    }
}
