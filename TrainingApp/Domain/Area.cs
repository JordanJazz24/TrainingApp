namespace TrainingApp.Domain
{
    public enum SerializationType { Serialized, NonSerialized }

    public class Area
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; }
        public SerializationType SerializationType { get; set; }
        public bool RequiresAssembly { get; set; }
        public bool RequiresDataCollection { get; set; }
        public bool RequiresNCComponent { get; set; }

        // Start abre la secuencia de 4 modales (Moldeo); false = Start directo (spec 008).
        public bool RequiresStartChecklist { get; set; }
    }
}
