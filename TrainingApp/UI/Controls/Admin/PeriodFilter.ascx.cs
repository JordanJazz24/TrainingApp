using System;
using System.Globalization;
using System.Web;
using System.Web.UI;
using TrainingApp.BLL.Reports;
using TrainingApp.Pages.Admin;
using TrainingApp.Shared;

namespace TrainingApp.UI.Controls.Admin
{
    // El control de período de Pasos y del Resumen (spec 023, P-12). Solo UI: expone lo elegido
    // (la página lo pone en la URL) y pinta el período que aplicó la BLL; no calcula rangos.
    public partial class PeriodFilter : UserControl
    {
        // Lo elegido en el postback; un valor desconocido vale Este mes.
        public ReportPeriod Period
            => ReportFilters.PeriodValues.TryGetValue(DdlPeriod.SelectedValue ?? string.Empty, out var period)
                ? period : ReportPeriods.Default;

        // Inicio y Fin, solo con Personalizado; null si faltan o no son fechas.
        public DateTime? From => Period == ReportPeriod.Custom ? ReportFilters.Date(TxtFrom.Text) : null;
        public DateTime? To   => Period == ReportPeriod.Custom ? ReportFilters.Date(TxtTo.Text) : null;

        // false: la página pinta el rango aplicado en otro lugar (Pasos, brief 022, 4a).
        public bool ShowsRange { get; set; } = true;

        // El período aplicado (ya validado por la BLL), el rango en palabras y el anterior.
        public void Bind(ReportRange range)
        {
            DdlPeriod.SelectedValue = ReportFilters.ValueOf(range.Period) ?? ReportFilters.ValueOf(ReportPeriods.Default);

            bool custom = range.Period == ReportPeriod.Custom;
            TxtFrom.Text = custom ? Day(range.FromLocal) : string.Empty;
            TxtTo.Text   = custom ? Day(range.ToLocal) : string.Empty;

            // El selector no deja elegir días después de hoy en la planta (P-6, D-216); la BLL
            // igual lo valida.
            string today = PlantTime.Today(DateTime.UtcNow).ToString(ReportFilters.DateFormat, CultureInfo.InvariantCulture);
            TxtFrom.Attributes["max"] = today;
            TxtTo.Attributes["max"]   = today;
            if (!custom) PnlCustom.Attributes["hidden"] = "hidden";

            string text = ReportText.Range(range);
            if (range.Compares) text += " · se compara con " + ReportText.PreviousRange(range);
            LitRange.Text = HttpUtility.HtmlEncode(text);
            PhRange.Visible = ShowsRange;
        }

        private static string Day(DateTime? day)
            => day.HasValue ? day.Value.ToString(ReportFilters.DateFormat, CultureInfo.InvariantCulture) : string.Empty;
    }
}
