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
        Logout = 8,
        LogNCFastRepair = 9,   // Log NC · reparación rápida (spec 014, P-7); LogNC = desecho rápido
        LogNCExtendedRepair = 10, // Log NC · reparación extendida (spec 014, P-7)
        MesLogin = 11          // Ingreso a MES: script, Repositorio, Hora a Hora, Recursos MES y login (spec 016, D-75)
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
