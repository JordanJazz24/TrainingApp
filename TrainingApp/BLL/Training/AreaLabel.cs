namespace TrainingApp.BLL.Training
{
    // Texto del área del operador, igual en todas partes (spec 021, P-9): inicio, Usuarios,
    // dashboard y Excel.
    public static class AreaLabel
    {
        public const string NoArea = "Sin área";

        // "Producto · Área"; null si no hay área.
        public static string Of(string productName, string areaName)
        {
            return string.IsNullOrEmpty(areaName) ? null : productName + " · " + areaName;
        }
    }
}
