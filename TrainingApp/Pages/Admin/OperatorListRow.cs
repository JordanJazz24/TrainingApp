using System.Collections.Generic;

namespace TrainingApp.Pages.Admin
{
    // Fila de la lista de Operadores (spec 018, AC-13), con los textos ya armados. Solo
    // presentación.
    public class OperatorListRow
    {
        public int UserId { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }

        // "Producto · Área" o "Sin área" (spec 021).
        public string AreaText { get; set; }
        public string LastActivityText { get; set; }
        public string DetailUrl { get; set; }

        // "Estado en su área" (spec 024, D-261): una insignia por guía de su área (hoy una, D-268)
        // o, si no tiene guías que entrenar, el texto atenuado ("Sin área", "Sin guía publicada").
        public IList<OperatorGuideCell> AreaGuides { get; set; }
        public string AreaNote { get; set; }
        public bool HasAreaNote => !string.IsNullOrEmpty(AreaNote);
    }

    public class OperatorGuideCell
    {
        public string StateText { get; set; }
        public string StateCss { get; set; }
        public string StateIcon { get; set; }

        // "Mejor 85.0% · Último 60.0%"; vacío sin Assessment completados.
        public string ScoresText { get; set; }
    }
}
