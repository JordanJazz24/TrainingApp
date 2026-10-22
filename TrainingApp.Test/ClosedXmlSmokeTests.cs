using System;
using System.IO;
using ClosedXML.Excel;
using NUnit.Framework;

namespace TrainingApp.Test
{
    // Spec 018, D-140: ClosedXML con packages.config en .NET Framework 4.7.2. Arma un .xlsx como
    // los del dashboard (encabezados en español, una fecha, un número y una tasa), lo guarda y lo
    // vuelve a leer. El archivo queda en la carpeta de trabajo del test para abrirlo en Excel.
    [TestFixture]
    public class ClosedXmlSmokeTests
    {
        [Test]
        public void Workbook_SavesAndReadsBack()
        {
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ClosedXmlSmoke.xlsx");
            var started = new DateTime(2026, 10, 22, 13, 54, 0);

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Sesiones");
                sheet.Cell(1, 1).Value = "Operador";
                sheet.Cell(1, 2).Value = "Inicio";
                sheet.Cell(1, 3).Value = "Puntaje";
                sheet.Cell(1, 4).Value = "Último acceso";
                sheet.Cell(2, 1).Value = "Ana Pérez";
                sheet.Cell(2, 2).Value = started;
                sheet.Cell(2, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                sheet.Cell(2, 3).Value = 0.85;
                sheet.Cell(2, 3).Style.NumberFormat.Format = "0%";
                sheet.Row(1).Style.Font.Bold = true;
                sheet.Columns().AdjustToContents();
                workbook.Worksheets.Add("Intentos por paso");
                workbook.SaveAs(path);
            }

            using (var workbook = new XLWorkbook(path))
            {
                var sheet = workbook.Worksheet("Sesiones");
                Assert.That(sheet.Cell(1, 4).GetString(), Is.EqualTo("Último acceso"));
                Assert.That(sheet.Cell(2, 1).GetString(), Is.EqualTo("Ana Pérez"));
                Assert.That(sheet.Cell(2, 2).GetDateTime(), Is.EqualTo(started));
                Assert.That(sheet.Cell(2, 3).GetDouble(), Is.EqualTo(0.85));
                Assert.That(workbook.Worksheets.Count, Is.EqualTo(2));
            }
        }
    }
}
