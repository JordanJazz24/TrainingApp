using System.Collections.Generic;

namespace TrainingApp.Pages.Admin
{
    // Una fila de Seguimiento (spec 018), con los textos ya armados. Solo presentación.
    public class FollowUpListRow
    {
        public string Code1 { get; set; }
        public string Name { get; set; }
        public string GuideName { get; set; }
        public string GuideTitle { get; set; }
        public string StateText { get; set; }
        public string StateCss { get; set; }
        public string LastActivityText { get; set; }

        // Una línea por regla ("S-2 · 21 días sin sesiones de esta guía").
        public IList<string> Reasons { get; set; }
        public string DetailUrl { get; set; }
    }
}
