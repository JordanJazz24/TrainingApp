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
        public bool NotStarted { get; set; }
        public string LastActivityText { get; set; }
        public string DetailUrl { get; set; }

        // Una celda por guía publicada, en el orden de las columnas.
        public IList<OperatorGuideCell> Guides { get; set; }
    }

    public class OperatorGuideCell
    {
        public bool HasSessions { get; set; }
        public string StateText { get; set; }
        public string StateCss { get; set; }

        // "Mejor 85.0% · Último 60.0%"; vacío sin Assessment completados.
        public string ScoresText { get; set; }
    }
}
