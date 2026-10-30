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

        // "hace 26 días" bajo la fecha; Inactive lo pinta en ámbar con reloj (spec 022, brief 6).
        public string AgoText { get; set; }
        public bool Inactive { get; set; }
        public bool HasAgo => !string.IsNullOrEmpty(AgoText);

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

    // Un tramo de la barra por estado del encabezado de Operadores (spec 022, brief 6): un enlace
    // con los mismos filtros de la URL; su ancho es cuántos hay.
    public class OperatorMeterSegment
    {
        public string Label { get; set; }
        public string Icon { get; set; }
        public string Css { get; set; }
        public int Count { get; set; }
        public int Grow { get; set; }
        public string ShareText { get; set; }
        public string Url { get; set; }
        public bool Selected { get; set; }
    }

    // La línea de "qué hacer" del grupo elegido (ReportText.StateHint).
    public class OperatorHint
    {
        public string Title { get; set; }
        public string Text { get; set; }
        public string Icon { get; set; }
        public string Tone { get; set; }
    }
}
