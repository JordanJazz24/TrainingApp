namespace TrainingApp.Domain
{
    // Área que un operador puede tener (spec 021, D-176): con al menos una guía publicada.
    // Con su producto, para el selector agrupado por producto.
    public class EligibleArea
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int AreaId { get; set; }
        public string AreaName { get; set; }
    }
}
