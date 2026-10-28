using System;
using System.Collections.Specialized;
using NUnit.Framework;
using TrainingApp.BLL.Reports;
using TrainingApp.Pages.Admin;

namespace TrainingApp.Test
{
    // Spec 023: el período en la URL (P-7), los textos del rango (P-14) y cómo se muestra el
    // cambio (P-5, P-16). Solo presentación; los rangos los resuelve ReportPeriods.
    [TestFixture]
    public class ReportFiltersTests
    {
        // Miércoles 28 de octubre de 2026, 12:00 en la planta.
        private static readonly DateTime Now = new DateTime(2026, 10, 28, 18, 0, 0);

        private static NameValueCollection Query(string period = null, string from = null, string to = null)
        {
            var query = new NameValueCollection();
            if (period != null) query["period"] = period;
            if (from != null) query["from"] = from;
            if (to != null) query["to"] = to;
            return query;
        }

        [Test]
        public void Url_WithoutPeriod_IsThisMonth()
            => Assert.That(ReportFilters.Summary(Query()).Period, Is.EqualTo(ReportPeriod.ThisMonth));

        // period=all sigue funcionando; Personalizado trae sus fechas.
        [Test]
        public void Url_ReadsEachPeriod()
        {
            Assert.That(ReportFilters.Steps(Query("all")).Period, Is.EqualTo(ReportPeriod.All));
            Assert.That(ReportFilters.Steps(Query("week")).Period, Is.EqualTo(ReportPeriod.ThisWeek));

            var custom = ReportFilters.Summary(Query("custom", "2026-10-05", "2026-10-14"));
            Assert.That(custom.Period, Is.EqualTo(ReportPeriod.Custom));
            Assert.That(custom.From, Is.EqualTo(new DateTime(2026, 10, 5)));
            Assert.That(custom.To, Is.EqualTo(new DateTime(2026, 10, 14)));
        }

        // Un período desconocido o una fecha que no es fecha llegan a la BLL como inválidos:
        // Este mes con el aviso (P-6).
        [Test]
        public void Url_Manipulated_IsInvalidInTheBll()
        {
            var unknown = ReportFilters.Summary(Query("ayer"));
            Assert.That(ReportPeriods.Resolve(unknown.Period, unknown.From, unknown.To, Now).Invalid, Is.True);

            var badDate = ReportFilters.Summary(Query("custom", "05/10/2026", "2026-10-14"));
            var range = ReportPeriods.Resolve(badDate.Period, badDate.From, badDate.To, Now);
            Assert.That(range.Invalid, Is.True);
            Assert.That(range.Period, Is.EqualTo(ReportPeriod.ThisMonth));
        }

        // Ida y vuelta: Este mes no va en la URL; Personalizado lleva Inicio y Fin.
        [Test]
        public void WritePeriod_RoundTrips()
        {
            var month = new NameValueCollection();
            ReportFilters.WritePeriod(month, ReportPeriod.ThisMonth, null, null);
            Assert.That(month.Count, Is.EqualTo(0));

            var custom = new NameValueCollection();
            ReportFilters.WritePeriod(custom, ReportPeriod.Custom, new DateTime(2026, 10, 5), new DateTime(2026, 10, 14));
            Assert.That(custom["period"], Is.EqualTo("custom"));
            Assert.That(custom["from"], Is.EqualTo("2026-10-05"));
            Assert.That(ReportFilters.Steps(custom).To, Is.EqualTo(new DateTime(2026, 10, 14)));

            var prev = new NameValueCollection();
            ReportFilters.WritePeriod(prev, ReportPeriod.PrevMonth, new DateTime(2026, 10, 5), null);
            Assert.That(prev["period"], Is.EqualTo("prevmonth"));
            Assert.That(prev["from"], Is.Null, "las fechas solo van con Personalizado");
        }

        [Test]
        public void RangeTexts()
        {
            var month = ReportPeriods.Resolve(ReportPeriod.ThisMonth, null, null, Now);
            Assert.That(ReportText.Range(month), Is.EqualTo("Del 1 al 28 de oct. de 2026"));
            Assert.That(ReportText.PreviousRange(month), Is.EqualTo("1–28 sep."));
            Assert.That(ReportText.PeriodOf(month), Is.EqualTo("este mes"));

            var last30 = ReportPeriods.Resolve(ReportPeriod.Last30Days, null, null, Now);
            Assert.That(ReportText.Range(last30), Is.EqualTo("Del 29 de sep. al 28 de oct. de 2026"));
            Assert.That(ReportText.PreviousRange(last30), Is.EqualTo("30 ago.–28 sep."));

            var today = ReportPeriods.Resolve(ReportPeriod.Today, null, null, Now);
            Assert.That(ReportText.Range(today), Is.EqualTo("El 28 de oct. de 2026"));
            Assert.That(ReportText.PreviousRange(today), Is.EqualTo("27 oct."));

            var january = ReportPeriods.Resolve(ReportPeriod.ThisMonth, null, null, new DateTime(2027, 1, 15, 18, 0, 0));
            Assert.That(ReportText.PreviousRange(january), Is.EqualTo("1–15 dic. 2026"));

            var custom = ReportPeriods.Resolve(ReportPeriod.Custom, new DateTime(2025, 12, 30), new DateTime(2026, 1, 5), Now);
            Assert.That(ReportText.Range(custom), Is.EqualTo("Del 30 de dic. de 2025 al 5 de ene. de 2026"));
            Assert.That(ReportText.PeriodOf(custom), Is.EqualTo("del 30 de dic. de 2025 al 5 de ene. de 2026"));

            Assert.That(ReportText.Range(ReportPeriods.Resolve(ReportPeriod.All, null, null, Now)), Is.EqualTo("Todo el historial"));
        }

        // Flecha, valor, referencia y base; si es mejora, también en texto (nunca solo color).
        [Test]
        public void ChangeView_Texts()
        {
            var month = ReportPeriods.Resolve(ReportPeriod.ThisMonth, null, null, Now);

            var up = ChangeView.Of(ReportChange.Points(0.75m, 4, 0.5m, 2, higherIsBetter: true), month, "evaluado", "evaluados");
            Assert.That(up.Text, Is.EqualTo("▲ 25 pts"));
            Assert.That(up.Css, Is.EqualTo("mt-change-good"));
            Assert.That(up.Verdict, Is.EqualTo("mejora"));
            Assert.That(up.Reference, Is.EqualTo("vs. 1–28 sep."));
            Assert.That(up.Base, Is.EqualTo("4 vs. 2 evaluados"));

            var worse = ChangeView.Of(ReportChange.Points(0.5m, 1, 0.25m, 1, higherIsBetter: false), month, "sesión", "sesiones");
            Assert.That(worse.Text, Is.EqualTo("▲ 25 pts"));
            Assert.That(worse.Verdict, Is.EqualTo("empeora"));
            Assert.That(worse.Base, Is.EqualTo("1 vs. 1 sesión"));

            var none = ChangeView.Of(ReportChange.Percent(3, 0, higherIsBetter: true), month, "sesión", "sesiones");
            Assert.That(none.Text, Is.EqualTo("sin datos para comparar"));
            Assert.That(none.Css, Is.EqualTo("mt-change-none"));

            Assert.That(ChangeView.Of(null, month, "sesión", "sesiones").Visible, Is.False);
        }
    }
}
