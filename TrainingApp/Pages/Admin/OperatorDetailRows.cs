using System.Collections.Generic;

namespace TrainingApp.Pages.Admin
{
    // Filas del detalle del Operador (spec 018, AC-14; spec 022, brief 7, alternativa H "Tres
    // preguntas con historial por guía"), con los textos ya armados. Solo presentación.

    // "¿Está listo en su área?": una por guía de su área, con su línea de qué hacer
    // (ReportText.StateHint, la misma de Operadores).
    public class OperatorGuideCard
    {
        public string GuideName { get; set; }
        public string GuideTitle { get; set; }
        public string StateText { get; set; }
        public string StateCss { get; set; }
        public string StateIcon { get; set; }
        public string BestText { get; set; }
        public string LastText { get; set; }
        public string LastActivityText { get; set; }

        // "hace 3 días" y "5.5 puntos menos que su mejor" (métricas derivadas, OperatorActivity).
        public string AgoText { get; set; }
        public string GapText { get; set; }

        public string HintTitle { get; set; }
        public string HintText { get; set; }
        public string HintIcon { get; set; }
        public string HintTone { get; set; }
    }

    // "¿Qué le falla?": los pasos a reforzar de una guía. Los de otras áreas van ocultos hasta
    // "Ver también otras áreas" (cliente), salvo que no haya de su área.
    public class OperatorReinforcementGroup
    {
        public string GuideName { get; set; }
        public string Tag { get; set; }
        public string TagCss { get; set; }

        // Grupo de otra área que arranca oculto en pantalla (la ficha los imprime todos).
        public bool Collapsed { get; set; }
        public IList<OperatorReinforcementRow> Steps { get; set; }
    }

    public class OperatorReinforcementRow
    {
        public string StepName { get; set; }
        public int TimesWithoutSuccess { get; set; }
        public int FailedAttempts { get; set; }
        public string MainReasonText { get; set; }
    }

    // "Historial por guía": un bloque plegable por guía con su curva (Chart.js, canvas
    // curve-<CurveIndex>), su lectura y sus sesiones.
    public class OperatorHistoryBlock
    {
        public int CurveIndex { get; set; }
        public string AreaText { get; set; }
        public string GuideTitle { get; set; }
        public string Tag { get; set; }
        public string TagCss { get; set; }

        // Abierto: su área, o todas si no tiene área (P-8).
        public bool Open { get; set; }

        public string StateText { get; set; }
        public string StateCss { get; set; }
        public string StateIcon { get; set; }
        public string BestText { get; set; }
        public string LastText { get; set; }
        public int Sessions { get; set; }
        public string LastActivityText { get; set; }
        public string AgoText { get; set; }

        // La lectura junto a la curva.
        public bool HasPoints { get; set; }
        public string PracticeText { get; set; }
        public string ScoresText { get; set; }
        public int Assessments { get; set; }
        public int PassedAssessments { get; set; }
        public int FailedAttempts { get; set; }

        public IList<OperatorSessionListRow> Rows { get; set; }
    }

    // Una sesión: en la tabla de su bloque (sin Área) y en la tabla única de la ficha impresa.
    public class OperatorSessionListRow
    {
        public string StartedText { get; set; }
        public string DayText { get; set; }
        public string TimeText { get; set; }

        // "Producto · Área" de la guía (spec 024, D-269); el título de la guía, al pasar el mouse.
        public string AreaText { get; set; }
        public string GuideTitle { get; set; }
        public string ModeText { get; set; }
        public string ModeCss { get; set; }
        public string StatusText { get; set; }
        public string StatusIcon { get; set; }

        // Puntaje: el texto, el ancho del medidor (0 a 100, con punto decimal), si aprobó y si es
        // el mejor de su guía. Sin puntaje visible, solo "—".
        public bool HasScore { get; set; }
        public string ScoreText { get; set; }
        public string ScoreWidth { get; set; }
        public bool Passed { get; set; }
        public bool IsBest { get; set; }

        public string DurationText { get; set; }
        public int FailedAttempts { get; set; }

        // Ancho en px de la barrita de intentos fallidos (relativa a la sesión con más).
        public int FailWidth { get; set; }
    }
}
