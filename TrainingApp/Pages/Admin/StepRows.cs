using System.Collections.Generic;

namespace TrainingApp.Pages.Admin
{
    // Filas de Pasos (spec 018, AC-17 y AC-18; brief 022, 4a), con los textos ya armados. Solo
    // presentación.
    public class StepListRow
    {
        public int Rank { get; set; }
        public string StepName { get; set; }
        public string FailureRateText { get; set; }

        // Alto de la columna y ancho de las barras de % (de 0 a 100), y su detalle ("3 de 8
        // sesiones").
        public string FailureWidth { get; set; }
        public string SessionsText { get; set; }

        // El cambio del % con fallo contra el período anterior (spec 023); en el paso elegido,
        // "Mejora" o "Empeora" a la vista y el % del anterior junto al de este período.
        public ChangeView FailureRateChange { get; set; }
        public string ChangeVerdict { get; set; }
        public bool HasPrevious { get; set; }
        public string PreviousRateText { get; set; }
        public string PreviousWidth { get; set; }

        public int FailedAttempts { get; set; }
        public int TimesWithoutSuccess { get; set; }
        public int Skips { get; set; }
        public string MedianText { get; set; }

        // Los 2 motivos más repetidos, con su parte del total; el resto ("Motivo · acción
        // (veces)") detrás de "Ver todos", en el cliente. En el ranking completo, el primero.
        public IList<StepReasonView> TopReasons { get; set; }
        public IList<string> MoreReasons { get; set; }
        public bool HasMoreReasons => MoreReasons.Count > 0;
        public string FirstReason { get; set; }
    }

    // Un motivo del paso elegido: el motivo, su acción, cuántas veces ("6 veces"), su parte de
    // todos los registrados en el paso (métrica derivada, brief 022) y el ancho de su barra
    // contra el motivo más repetido.
    public class StepReasonView
    {
        public string Reason { get; set; }
        public string Action { get; set; }
        public string CountText { get; set; }
        public string ShareText { get; set; }
        public string Width { get; set; }
    }
}
