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
        public string StateIcon { get; set; }
        public string LastActivityText { get; set; }

        // Un chip por regla ("Reprobó 2 Assessment"); S-3, uno por paso.
        public IList<FollowUpChip> Chips { get; set; }
        public string DetailUrl { get; set; }
    }

    // Una regla de Seguimiento como chip (Fase 11, D-160). Title: la línea con su S-n.
    public class FollowUpChip
    {
        public string Text { get; set; }
        public string Title { get; set; }
        public string Css { get; set; }
        public string Icon { get; set; }
    }
}
