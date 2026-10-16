namespace TrainingApp.Domain
{
    // Método de NC del código (spec 014, P-1; WI W20): qué hace con la pieza.
    public enum NCMethod
    {
        FastScrap      = 0,  // desecho rápido (F_FS…): desecha la unidad
        FastRepair     = 1,  // reparación rápida (F_FR…): reemplaza un componente
        ExtendedRepair = 2   // reparación rápida extendida (F_FX…): retrabajo o regreso
    }

    public class NCCode
    {
        public int Id { get; set; }
        public int AreaId { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public NCMethod Method { get; set; }

        // NC Data Entry exige Comments con este código (spec 014, P-13): formato, no se califica.
        public bool RequiresComments { get; set; }
    }
}
