namespace TrainingApp.Domain
{
    // Grupo de herramientas de Custom Resource Setup (spec 020): el operador registra un Tool
    // Number por grupo. Hoy simulado; con las Operations reales vendrá de la BD.
    public class ToolGroup
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }
}
