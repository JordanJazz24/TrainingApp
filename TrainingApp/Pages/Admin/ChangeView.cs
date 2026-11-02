using System;
using System.Globalization;
using TrainingApp.BLL.Reports;

namespace TrainingApp.Pages.Admin
{
    // El cambio de un número contra el período anterior, con los textos ya armados (spec 023,
    // P-5 y P-16): flecha, valor, referencia y la base de los dos períodos. Nunca solo color: la
    // flecha dice la dirección y Verdict (oculto a la vista, para lectores de pantalla) si es
    // mejora. Solo presentación: el cambio lo calcula la BLL (ReportChange).
    public class ChangeView
    {
        // Con Todo no hay comparación: no se muestra nada.
        public bool Visible { get; private set; }

        // mt-change-good, mt-change-bad, mt-change-same o mt-change-none.
        public string Css { get; private set; }

        // "▲ 6 pts", "▼ 12 %", "= 0 pts" o "sin datos para comparar".
        public string Text { get; private set; }

        // "mejora", "empeora" o vacío.
        public string Verdict { get; private set; }

        // "vs. 1–28 sep."
        public string Reference { get; private set; }

        // "1 vs. 2 evaluados"; vacío sin comparación.
        public string Base { get; private set; }

        // "Mejora", "Empeora", "Sin cambio" o vacío (sin datos para comparar o sin comparación):
        // la palabra del veredicto en el Resumen (E8b, D-252), siempre con el ícono, nunca solo color.
        public string VerdictLabel { get; private set; } = string.Empty;

        // El texto + " · " + la palabra; solo el texto si no hay veredicto.
        public string Full => string.IsNullOrEmpty(VerdictLabel) ? Text : Text + " · " + VerdictLabel;

        // La pastilla de veredicto del Resumen: "Mejora", "Empeora", "Sin cambio", "Sin datos" (hay
        // comparación pero no con qué) o "Sin comparación" (con Todo). Con su clase y su ícono.
        public string PillText { get; private set; }
        public string PillCss { get; private set; }
        public string PillIcon { get; private set; }

        private static readonly ChangeView Hidden = new ChangeView
        {
            Visible  = false,
            PillText = "Sin comparación",
            PillCss  = "mt-badge mt-badge-idle",
            PillIcon = "bi bi-slash-circle"
        };

        // baseSingular/basePlural: cómo se cuentan ("evaluado"/"evaluados", "sesión"/"sesiones").
        public static ChangeView Of(ReportChange change, ReportRange range, string baseSingular, string basePlural)
        {
            if (change == null) return Hidden;

            var view = new ChangeView
            {
                Visible   = true,
                Reference = "vs. " + ReportText.PreviousRange(range),
                Base      = change.CurrentBase + " vs. " + change.PreviousBase + " "
                            + (change.CurrentBase == 1 && change.PreviousBase == 1 ? baseSingular : basePlural)
            };
            if (!change.HasComparison)
            {
                view.Css     = "mt-change-none";
                view.Text    = "sin datos para comparar";
                view.Verdict = string.Empty;
                view.PillText = "Sin datos";
                view.PillCss  = "mt-badge mt-badge-idle";
                view.PillIcon = "bi bi-question-circle";
                return view;
            }

            string unit  = change.Unit == ChangeUnit.Points ? " pts" : " %";
            string value = Math.Abs(change.Change.Value).ToString("0.#", CultureInfo.CurrentCulture) + unit;
            switch (change.Direction)
            {
                case ChangeDirection.Up:   view.Text = "▲ " + value; break;
                case ChangeDirection.Down: view.Text = "▼ " + value; break;
                default:                   view.Text = "= " + value; break;
            }
            view.Css = change.IsImprovement == true ? "mt-change-good"
                     : change.IsImprovement == false ? "mt-change-bad" : "mt-change-same";
            view.Verdict = change.IsImprovement == true ? "mejora"
                         : change.IsImprovement == false ? "empeora" : string.Empty;
            if (change.IsImprovement == true)
            {
                view.VerdictLabel = view.PillText = "Mejora";
                view.PillCss  = "mt-badge mt-badge-ok";
                view.PillIcon = "bi bi-graph-up-arrow";
            }
            else if (change.IsImprovement == false)
            {
                view.VerdictLabel = view.PillText = "Empeora";
                view.PillCss  = "mt-badge mt-badge-bad";
                view.PillIcon = "bi bi-graph-down-arrow";
            }
            else
            {
                view.VerdictLabel = view.PillText = "Sin cambio";
                view.PillCss  = "mt-badge mt-badge-idle";
                view.PillIcon = "bi bi-dash-lg";
            }
            return view;
        }
    }
}
