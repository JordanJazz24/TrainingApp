namespace TrainingApp.Domain
{
    // Una guía publicada, para el dashboard (spec 018): la unidad del estado, la tasa y los
    // pasos. Las no publicadas no aparecen nunca (AC-12).
    public class DashboardGuide
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int AreaId { get; set; }
        public string AreaName { get; set; }
        public string ProductName { get; set; }
    }
}
