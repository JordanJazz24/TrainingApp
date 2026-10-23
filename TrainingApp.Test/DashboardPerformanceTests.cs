using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using ClosedXML.Excel;
using NUnit.Framework;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;
using TrainingApp.Pages.Admin;

namespace TrainingApp.Test
{
    // Spec 018, E7 (AC-24): mide cada vista y cada descarga del dashboard contra la BD de prueba
    // del generador (TrainingApp_DashboardTest, LocalDB). Explícita: no corre con el resto y
    // necesita esa BD. La cadena se inyecta en memoria (no se versiona un connectionStrings,
    // AUD-04). Ids de la BD del generador: admin y el operador con más sesiones, por variable de
    // entorno (MES_PERF_ADMIN, MES_PERF_OPERATOR).
    [TestFixture, Explicit("Medición de rendimiento contra TrainingApp_DashboardTest")]
    public class DashboardPerformanceTests
    {
        private const string ConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=TrainingApp_DashboardTest;Integrated Security=True;";

        private int _admin;
        private int _operator;
        private DashboardService _service;

        [OneTimeSetUp]
        public void UseTestDatabase()
        {
            var settings = ConfigurationManager.ConnectionStrings;
            typeof(ConfigurationElementCollection)
                .GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(settings, false);
            settings.Remove("Default");
            settings.Add(new ConnectionStringSettings("Default", ConnectionString));

            _admin    = int.Parse(Environment.GetEnvironmentVariable("MES_PERF_ADMIN") ?? "0");
            _operator = int.Parse(Environment.GetEnvironmentVariable("MES_PERF_OPERATOR") ?? "0");
            _service  = new DashboardService();
            AppDomain.MonitoringIsEnabled = true;
        }

        // Cada vista dos veces (la primera incluye compilar el plan y llenar la caché de SQL).
        [Test]
        public void Views()
        {
            foreach (var period in new[] { ReportPeriod.Last30Days, ReportPeriod.All })
            {
                Measure("Resumen " + period, () => _service.GetSummary(_admin, new SummaryFilter { Period = period }));
                Measure("Pasos Assessment " + period, () => _service.GetSteps(_admin, new StepFilter { Period = period }));
                Measure("Pasos Práctica " + period, () => _service.GetSteps(_admin, new StepFilter { Period = period, Mode = SessionMode.FreePractice }));
            }
            Measure("Operadores", () => _service.SearchOperators(_admin, new OperatorFilter()));
            Measure("Operador", () => _service.GetOperator(_admin, _operator));
            Measure("Seguimiento", () => _service.GetFollowUp(_admin, new FollowUpFilter()));
        }

        // Las descargas, con el armado y el guardado del .xlsx.
        [Test]
        public void Downloads()
        {
            Measure("Excel Operadores", () => Save(ExcelReports.Operators(_service.ExportOperators(_admin, new OperatorFilter()))));
            Measure("Excel Pasos (Todo)", () => Save(ExcelReports.Steps(_service.GetSteps(_admin, new StepFilter { Period = ReportPeriod.All }))));
            Measure("Excel Seguimiento", () => Save(ExcelReports.FollowUp(_service.GetFollowUp(_admin, new FollowUpFilter()))));
            Measure("Excel Sesiones (30 días)", () => Stream(_service.GetSessionExport(_admin, new SummaryFilter())));
        }

        // La descarga más pesada (D-148), sola para medir la memoria del proceso.
        [Test]
        public void SessionsAllPeriod()
        {
            Measure("Datos Sesiones (Todo)",
                () => _service.GetSessionExport(_admin, new SummaryFilter { Period = ReportPeriod.All }).Rows.Count,
                runs: 1);
            Measure("Excel Sesiones (Todo)",
                () => Stream(_service.GetSessionExport(_admin, new SummaryFilter { Period = ReportPeriod.All })),
                runs: 1);
        }

        // Como Export.ashx: a un temporal del disco que se borra al cerrarlo.
        private static long Stream(SessionExport export)
        {
            using (var file = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite,
                                             FileShare.None, 81920, FileOptions.DeleteOnClose))
            {
                ExcelReports.Sessions(export, file);
                return file.Length;
            }
        }

        private static long Save(XLWorkbook workbook)
        {
            using (workbook)
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return stream.Length;
            }
        }

        private static void Measure(string name, Func<object> action, int runs = 2)
        {
            for (int i = 1; i <= runs; i++)
            {
                GC.Collect();
                long allocatedBefore = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
                var process = Process.GetCurrentProcess();
                long peakBefore = process.PeakWorkingSet64;
                var watch = Stopwatch.StartNew();
                var result = action();
                watch.Stop();
                process.Refresh();
                long allocated = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - allocatedBefore;
                TestContext.Progress.WriteLine("{0,-28} vez {1}: {2,6:N2} s · asignado {3,6:N0} MB · pico del proceso {4,5:N0} MB (+{5:N0}){6}",
                    name, i, watch.Elapsed.TotalSeconds, allocated / 1048576.0,
                    process.PeakWorkingSet64 / 1048576.0, (process.PeakWorkingSet64 - peakBefore) / 1048576.0,
                    result is long bytes ? string.Format(" · {0:N1} MB", bytes / 1048576.0) : string.Empty);
            }
        }
    }
}
