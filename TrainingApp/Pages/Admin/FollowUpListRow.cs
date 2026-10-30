using System.Collections.Generic;

namespace TrainingApp.Pages.Admin
{
    // Un caso de Seguimiento (spec 018; tarjeta o fila de la cola, spec 022 brief 5), con los
    // textos ya armados. Solo presentación.
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

        // "3 de 3 reglas".
        public string RuleCountText { get; set; }

        // Una frase por regla ("S-1 · Reprobó 2 Assessment sin aprobar ninguno"); S-3, una por paso.
        public IList<FollowUpLine> Lines { get; set; }
        public string DetailUrl { get; set; }
    }

    // Una regla de Seguimiento como frase con su etiqueta S-n (spec 022, brief 5). Css marca la
    // fila de S-1 (is-alert); TagCss, la etiqueta.
    public class FollowUpLine
    {
        public string Code { get; set; }
        public string Text { get; set; }
        public string Icon { get; set; }
        public string Css { get; set; }
        public string TagCss { get; set; }
    }
}
