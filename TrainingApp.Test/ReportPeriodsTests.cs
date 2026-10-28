using System;
using NUnit.Framework;
using TrainingApp.BLL.Reports;

namespace TrainingApp.Test
{
    // Spec 023 (AC-11): el rango de cada atajo y su período anterior (P-4), los bordes de mes y de
    // semana, la medianoche de la planta (UTC-6, P-2) y la validación de Personalizado (P-6).
    [TestFixture]
    public class ReportPeriodsTests
    {
        // Miércoles 28 de octubre de 2026, 12:00 en la planta.
        private static readonly DateTime Now = new DateTime(2026, 10, 28, 18, 0, 0);

        // Mediodía de un día de la planta, en UTC.
        private static DateTime NoonUtc(int year, int month, int day) => new DateTime(year, month, day, 18, 0, 0);

        private static ReportRange Resolve(ReportPeriod period, DateTime nowUtc, DateTime? from = null, DateTime? to = null)
            => ReportPeriods.Resolve(period, from, to, nowUtc);

        private static void AssertDays(ReportRange range, DateTime from, DateTime to, DateTime prevFrom, DateTime prevTo)
        {
            Assert.That(range.FromLocal, Is.EqualTo(from), "Inicio");
            Assert.That(range.ToLocal, Is.EqualTo(to), "Fin");
            Assert.That(range.PrevFromLocal, Is.EqualTo(prevFrom), "Inicio del anterior");
            Assert.That(range.PrevToLocal, Is.EqualTo(prevTo), "Fin del anterior");
            Assert.That(range.Invalid, Is.False);
        }

        [Test]
        public void Today_ComparesWithYesterday()
        {
            var range = Resolve(ReportPeriod.Today, Now);

            AssertDays(range, new DateTime(2026, 10, 28), new DateTime(2026, 10, 28), new DateTime(2026, 10, 27), new DateTime(2026, 10, 27));
            // Medianoche de la planta = 06:00 UTC; el fin, la del día siguiente (excluida).
            Assert.That(range.FromUtc, Is.EqualTo(new DateTime(2026, 10, 28, 6, 0, 0)));
            Assert.That(range.ToUtc, Is.EqualTo(new DateTime(2026, 10, 29, 6, 0, 0)));
            Assert.That(range.PrevToUtc, Is.EqualTo(range.FromUtc));
            Assert.That(range.Days, Is.EqualTo(1));
        }

        // Una sesión a las 23:30 de la planta cae en ese día aunque en UTC ya sea el siguiente; a
        // las 00:30, en el día nuevo.
        [Test]
        public void PlantMidnight_DecidesTheDay()
        {
            var range = Resolve(ReportPeriod.Today, Now);
            var lateYesterday = new DateTime(2026, 10, 28, 5, 30, 0);   // 27 oct., 23:30 en la planta
            var earlyToday    = new DateTime(2026, 10, 28, 6, 30, 0);   // 28 oct., 00:30

            Assert.That(range.Contains(lateYesterday), Is.False);
            Assert.That(range.InPrevious(lateYesterday), Is.True);
            Assert.That(range.Contains(earlyToday), Is.True);
            Assert.That(range.InPrevious(earlyToday), Is.False);
            Assert.That(range.Contains(new DateTime(2026, 10, 29, 6, 0, 0)), Is.False, "el fin es exclusivo");
        }

        // El "hoy" es el de la planta: a las 03:00 UTC del 29 todavía es el 28 en la planta.
        [Test]
        public void Today_IsThePlantDay()
        {
            var range = Resolve(ReportPeriod.Today, new DateTime(2026, 10, 29, 3, 0, 0));

            Assert.That(range.FromLocal, Is.EqualTo(new DateTime(2026, 10, 28)));
        }

        [Test]
        public void ThisWeek_SameDaysOfLastWeek()
        {
            AssertDays(Resolve(ReportPeriod.ThisWeek, Now),
                new DateTime(2026, 10, 26), new DateTime(2026, 10, 28), new DateTime(2026, 10, 19), new DateTime(2026, 10, 21));
        }

        [Test]
        public void ThisWeek_OnMondayAndSunday()
        {
            AssertDays(Resolve(ReportPeriod.ThisWeek, NoonUtc(2026, 10, 26)),
                new DateTime(2026, 10, 26), new DateTime(2026, 10, 26), new DateTime(2026, 10, 19), new DateTime(2026, 10, 19));
            AssertDays(Resolve(ReportPeriod.ThisWeek, NoonUtc(2026, 11, 1)),
                new DateTime(2026, 10, 26), new DateTime(2026, 11, 1), new DateTime(2026, 10, 19), new DateTime(2026, 10, 25));
        }

        [Test]
        public void ThisMonth_SameDaysOfLastMonth()
        {
            var range = Resolve(ReportPeriod.ThisMonth, Now);

            AssertDays(range, new DateTime(2026, 10, 1), new DateTime(2026, 10, 28), new DateTime(2026, 9, 1), new DateTime(2026, 9, 28));
            Assert.That(range.Period, Is.EqualTo(ReportPeriod.ThisMonth));
            Assert.That(range.Days, Is.EqualTo(28));
        }

        // Con un mes anterior más corto, hasta su último día.
        [Test]
        public void ThisMonth_ShorterPreviousMonth()
        {
            AssertDays(Resolve(ReportPeriod.ThisMonth, NoonUtc(2026, 10, 31)),
                new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));
            AssertDays(Resolve(ReportPeriod.ThisMonth, NoonUtc(2027, 3, 31)),
                new DateTime(2027, 3, 1), new DateTime(2027, 3, 31), new DateTime(2027, 2, 1), new DateTime(2027, 2, 28));
            AssertDays(Resolve(ReportPeriod.ThisMonth, NoonUtc(2028, 3, 31)),
                new DateTime(2028, 3, 1), new DateTime(2028, 3, 31), new DateTime(2028, 2, 1), new DateTime(2028, 2, 29));
        }

        [Test]
        public void ThisMonth_InJanuary_ComparesWithDecember()
        {
            AssertDays(Resolve(ReportPeriod.ThisMonth, NoonUtc(2027, 1, 15)),
                new DateTime(2027, 1, 1), new DateTime(2027, 1, 15), new DateTime(2026, 12, 1), new DateTime(2026, 12, 15));
        }

        [Test]
        public void PrevMonth_ComparesWithTheMonthBefore()
        {
            AssertDays(Resolve(ReportPeriod.PrevMonth, Now),
                new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), new DateTime(2026, 8, 1), new DateTime(2026, 8, 31));
            AssertDays(Resolve(ReportPeriod.PrevMonth, NoonUtc(2027, 1, 15)),
                new DateTime(2026, 12, 1), new DateTime(2026, 12, 31), new DateTime(2026, 11, 1), new DateTime(2026, 11, 30));
        }

        // Hoy y los 29 días anteriores, completos; el anterior, los 30 días previos.
        [Test]
        public void Last30Days_IncludesToday()
        {
            var range = Resolve(ReportPeriod.Last30Days, Now);

            AssertDays(range, new DateTime(2026, 9, 29), new DateTime(2026, 10, 28), new DateTime(2026, 8, 30), new DateTime(2026, 9, 28));
            Assert.That(range.Days, Is.EqualTo(30));
            Assert.That(range.PrevToUtc, Is.EqualTo(range.FromUtc));
        }

        [Test]
        public void All_HasNoLimitsNorPrevious()
        {
            var range = Resolve(ReportPeriod.All, Now);

            Assert.That(range.FromUtc, Is.Null);
            Assert.That(range.ToUtc, Is.Null);
            Assert.That(range.Compares, Is.False);
            Assert.That(range.ReadFromUtc, Is.Null);
            Assert.That(range.Days, Is.Null);
            Assert.That(range.Contains(new DateTime(2020, 1, 1)), Is.True);
            Assert.That(range.InPrevious(new DateTime(2020, 1, 1)), Is.False);
        }

        // Los mismos días justo antes; el día de Fin entra completo.
        [Test]
        public void Custom_SameDaysJustBefore()
        {
            var range = Resolve(ReportPeriod.Custom, Now, new DateTime(2026, 10, 5), new DateTime(2026, 10, 14, 9, 0, 0));

            AssertDays(range, new DateTime(2026, 10, 5), new DateTime(2026, 10, 14), new DateTime(2026, 9, 25), new DateTime(2026, 10, 4));
            Assert.That(range.Period, Is.EqualTo(ReportPeriod.Custom));
            Assert.That(range.ToUtc, Is.EqualTo(new DateTime(2026, 10, 15, 6, 0, 0)));
            Assert.That(range.PrevToUtc, Is.EqualTo(range.FromUtc));
            Assert.That(range.ReadFromUtc, Is.EqualTo(range.PrevFromUtc));
        }

        [Test]
        public void Custom_EndingToday_IsValid()
        {
            var range = Resolve(ReportPeriod.Custom, Now, new DateTime(2026, 10, 28), new DateTime(2026, 10, 28));

            Assert.That(range.Period, Is.EqualTo(ReportPeriod.Custom));
            Assert.That(range.Invalid, Is.False);
        }

        // P-6: sin Inicio o Fin, Fin antes de Inicio o Fin después de hoy: Este mes con el aviso.
        [TestCase(null, "2026-10-14")]
        [TestCase("2026-10-05", null)]
        [TestCase("2026-10-14", "2026-10-05")]
        [TestCase("2026-10-05", "2026-10-29")]
        [TestCase("0001-01-02", "2026-10-28")]
        [TestCase("1999-12-31", "2026-10-28")]
        public void Custom_Invalid_FallsBackToThisMonth(string from, string to)
        {
            var range = Resolve(ReportPeriod.Custom, Now,
                from == null ? (DateTime?)null : DateTime.Parse(from), to == null ? (DateTime?)null : DateTime.Parse(to));

            Assert.That(range.Period, Is.EqualTo(ReportPeriod.ThisMonth));
            Assert.That(range.Invalid, Is.True);
            Assert.That(range.FromLocal, Is.EqualTo(new DateTime(2026, 10, 1)));
        }

        // Un valor fuera del enum (manipulado) también vale Este mes con el aviso; el valor por
        // defecto del enum es Este mes, sin aviso.
        [Test]
        public void UndefinedPeriod_FallsBackToThisMonth()
        {
            Assert.That(Resolve(default(ReportPeriod), Now).Period, Is.EqualTo(ReportPeriod.ThisMonth));
            Assert.That(Resolve(default(ReportPeriod), Now).Invalid, Is.False);

            var range = Resolve((ReportPeriod)42, Now);

            Assert.That(range.Period, Is.EqualTo(ReportPeriod.ThisMonth));
            Assert.That(range.Invalid, Is.True);
        }

        // ReportChange: puntos con un decimal, la dirección y si es mejora; sin anterior, nada.
        [Test]
        public void Change_PointsAndPercent()
        {
            var pass = ReportChange.Points(0.75m, 4, 0.5m, 2, higherIsBetter: true);
            Assert.That(pass.Change, Is.EqualTo(25m));
            Assert.That(pass.Direction, Is.EqualTo(ChangeDirection.Up));
            Assert.That(pass.IsImprovement, Is.True);
            Assert.That((pass.CurrentBase, pass.PreviousBase), Is.EqualTo((4, 2)));

            var failure = ReportChange.Points(0.5m, 2, 0.25m, 4, higherIsBetter: false);
            Assert.That(failure.Direction, Is.EqualTo(ChangeDirection.Up));
            Assert.That(failure.IsImprovement, Is.False);

            var thirds = ReportChange.Points(2m / 3m, 3, 0.5m, 2, higherIsBetter: true);
            Assert.That(thirds.Change, Is.EqualTo(16.7m));

            var sessions = ReportChange.Percent(3, 4, higherIsBetter: true);
            Assert.That(sessions.Change, Is.EqualTo(-25m));
            Assert.That(sessions.IsImprovement, Is.False);

            var same = ReportChange.Points(0.5m, 2, 0.5m, 4, higherIsBetter: true);
            Assert.That(same.Direction, Is.EqualTo(ChangeDirection.Same));
            Assert.That(same.IsImprovement, Is.Null);
        }

        [Test]
        public void Change_WithoutPreviousData_HasNoComparison()
        {
            var rate = ReportChange.Points(0.5m, 2, null, 0, higherIsBetter: true);
            Assert.That(rate.HasComparison, Is.False);
            Assert.That(rate.Direction, Is.EqualTo(ChangeDirection.Same));
            Assert.That(rate.IsImprovement, Is.Null);

            Assert.That(ReportChange.Percent(5, 0, higherIsBetter: true).HasComparison, Is.False);
        }
    }
}
