namespace TrainingApp.Domain
{
    // Guía publicada con su producto y área, para la página Guías de Admin (spec 020).
    public class PublishedGuide
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string AreaName { get; set; }
        public string Title { get; set; }
        public bool IsPublished { get; set; }
        public ResourceSetupType ResourceSetupType { get; set; }
    }
}
