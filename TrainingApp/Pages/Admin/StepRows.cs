using System.Collections.Generic;

namespace TrainingApp.Pages.Admin
{
    // Filas de Pasos (spec 018, AC-17 y AC-18), con los textos ya armados. Solo presentación.
    public class StepGuideGroup
    {
        public string GuideName { get; set; }
        public string GuideTitle { get; set; }
        public IList<StepListRow> Rows { get; set; }
    }

    public class StepListRow
    {
        public int Rank { get; set; }
        public string StepName { get; set; }
        public string FailureRateText { get; set; }

        // Barra de % (Fase 11, D-160) y su detalle ("3 de 8 sesiones").
        public string FailureWidth { get; set; }
        public string SessionsText { get; set; }

        // El cambio del % con fallo contra el período anterior (spec 023).
        public ChangeView FailureRateChange { get; set; }
        public int FailedAttempts { get; set; }
        public int TimesWithoutSuccess { get; set; }
        public int Skips { get; set; }
        public string MedianText { get; set; }

        // "Motivo · acción (veces)", del más repetido al menos: los 2 primeros a la vista y el
        // resto detrás de "Ver todos", en el cliente.
        public IList<string> TopReasons { get; set; }
        public IList<string> MoreReasons { get; set; }
        public bool HasMoreReasons => MoreReasons.Count > 0;
    }
}
