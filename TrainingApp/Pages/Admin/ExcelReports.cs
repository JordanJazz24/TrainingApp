using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using TrainingApp.BLL.Reports;

namespace TrainingApp.Pages.Admin
{
    // Los .xlsx del dashboard (spec 018, AC-21 y AC-22). Solo presentación: los datos llegan del
    // mismo método de DashboardService que cada página (DA-5) y los textos son los de pantalla
    // (ReportText). Encabezados en español, fechas en la hora de la planta y duraciones como
    // tiempo de Excel. Con 0 filas, solo los encabezados.
    //
    // Sin AdjustToContents: medir el texto carga las fuentes del sistema (~20 s la primera vez,
    // plan 018 → E1); cada columna tiene su ancho.
    public static class ExcelReports
    {
        private const string DateFormat     = "dd/MM/yyyy HH:mm";
        private const string DurationFormat = "[h]:mm:ss";
        private const string PercentFormat  = "0.0%";

        // Una columna: encabezado, ancho en caracteres y formato de número (null = general).
        private sealed class Column
        {
            public string Header;
            public double Width;
            public string Format;
            public Column(string header, double width, string format = null)
            {
                Header = header; Width = width; Format = format;
            }
        }

        public static XLWorkbook Operators(OperatorPage page)
        {
            var columns = new List<Column> { new Column("Código", 12), new Column("Nombre", 32) };
            foreach (var g in page.Guides)
            {
                columns.Add(new Column(g.AreaName + " · Estado", 18));
                columns.Add(new Column(g.AreaName + " · Mejor puntaje", 14, PercentFormat));
                columns.Add(new Column(g.AreaName + " · Último puntaje", 14, PercentFormat));
            }
            columns.Add(new Column("Última actividad", 17, DateFormat));

            var rows = page.Rows.Select(r =>
            {
                var cells = new List<object> { r.Code1, r.Name };
                foreach (var g in r.Guides)
                {
                    cells.Add(ReportText.State(g.State));
                    cells.Add(g.BestScore);
                    cells.Add(g.LastScore);
                }
                cells.Add(Local(r.LastActivity));
                return cells.ToArray();
            });

            return Book("Operadores", columns, rows);
        }

        public static XLWorkbook Steps(StepReport report)
        {
            var guides = report.Guides.ToDictionary(g => g.Id);
            var columns = new List<Column>
            {
                new Column("Guía", 18), new Column("Paso", 30), new Column("Sesiones", 10),
                new Column("% con fallo", 11, PercentFormat), new Column("Intentos fallidos", 12),
                new Column("Veces sin acierto", 12)
            };
            if (report.ShowsSkips) columns.Add(new Column("Saltos", 9));
            columns.Add(new Column("Mediana", 10, DurationFormat));
            columns.Add(new Column("Motivos y acciones", 70));

            var rows = report.Rankings.SelectMany(r => r.Steps.Select(s =>
            {
                var cells = new List<object>
                {
                    r.TrainingGuideId.HasValue ? guides[r.TrainingGuideId.Value].AreaName : "Todas las guías",
                    s.StepName, s.Sessions, s.FailureRate, s.FailedAttempts, s.TimesWithoutSuccess
                };
                if (report.ShowsSkips) cells.Add(s.Skips);
                cells.Add(s.MedianDuration);
                cells.Add(string.Join("; ", s.Reasons.Select(ReportText.ReasonCount)));
                return cells.ToArray();
            }));

            return Book("Pasos", columns, rows);
        }

        public static XLWorkbook FollowUp(FollowUpReport report)
        {
            var guides = report.Guides.ToDictionary(g => g.Id);
            var columns = new List<Column>
            {
                new Column("Código", 12), new Column("Nombre", 32), new Column("Guía", 18), new Column("Estado", 18),
                new Column("Última sesión de la guía", 17, DateFormat),
                new Column("S-1 · Assessment reprobados", 14), new Column("S-2 · Días sin sesiones", 14),
                new Column("S-3 · Pasos sin acierto", 50), new Column("Reglas", 70)
            };

            var rows = report.Rows.Select(r =>
            {
                var s1 = r.Reasons.FirstOrDefault(x => x.Rule == FollowUpRule.RepeatedFailures);
                var s2 = r.Reasons.FirstOrDefault(x => x.Rule == FollowUpRule.Inactive);
                var s3 = r.Reasons.FirstOrDefault(x => x.Rule == FollowUpRule.StuckStep);
                return new object[]
                {
                    r.Code1, r.Name, guides[r.TrainingGuideId].AreaName, ReportText.State(r.State), Local(r.LastActivity),
                    s1?.FailedAssessments, s2?.DaysInactive,
                    s3 == null ? null : string.Join("; ", ReportText.FollowUpLines(s3)),
                    string.Join("; ", r.Reasons.SelectMany(ReportText.FollowUpLines))
                };
            });

            return Book("Seguimiento", columns, rows);
        }

        // Sesiones del Resumen (AC-22): una fila por sesión y una por sesión y paso.
        public static XLWorkbook Sessions(SessionExport export)
        {
            var guides = export.Guides.ToDictionary(g => g.Id);
            var workbook = new XLWorkbook();

            AddSheet(workbook, "Sesiones", new List<Column>
            {
                new Column("Sesión", 9), new Column("Código", 12), new Column("Nombre", 32), new Column("Guía", 18),
                new Column("Modo", 14), new Column("Estado", 12), new Column("Inicio", 17, DateFormat),
                new Column("Fin", 17, DateFormat), new Column("Duración", 10, DurationFormat),
                new Column("Puntaje", 9, PercentFormat), new Column("Intentos fallidos", 12)
            }, export.Rows.Select(r => new object[]
            {
                r.Session.SessionId, r.Code1, r.Name, guides[r.Session.TrainingGuideId].AreaName,
                ReportText.Mode(r.Session.Mode), ReportText.Status(r.Session.Status),
                Local(r.Session.StartedAt), Local(r.Session.CompletedAt), r.Duration, r.Score, r.FailedAttempts
            }));

            AddSheet(workbook, "Intentos por paso", new List<Column>
            {
                new Column("Sesión", 9), new Column("Código", 12), new Column("Guía", 18), new Column("Modo", 14),
                new Column("Orden", 7), new Column("Paso", 30), new Column("Resultado", 13),
                new Column("Intentos fallidos", 12), new Column("Duración", 10, DurationFormat),
                new Column("Último intento", 17, DateFormat)
            }, export.Rows.SelectMany(r => r.Steps.Select(s => new object[]
            {
                r.Session.SessionId, r.Code1, guides[r.Session.TrainingGuideId].AreaName, ReportText.Mode(r.Session.Mode),
                s.Run.StepOrder, s.StepName, ReportText.Outcome(s.Run.Outcome), s.Run.FailedAttempts,
                s.Run.Duration, Local(s.Run.LastAttempt)
            })));

            return workbook;
        }

        private static XLWorkbook Book(string sheet, IList<Column> columns, IEnumerable<object[]> rows)
        {
            var workbook = new XLWorkbook();
            AddSheet(workbook, sheet, columns, rows);
            return workbook;
        }

        private static void AddSheet(XLWorkbook workbook, string name, IList<Column> columns, IEnumerable<object[]> rows)
        {
            var sheet = workbook.Worksheets.Add(name);
            for (int c = 0; c < columns.Count; c++)
            {
                sheet.Cell(1, c + 1).Value = columns[c].Header;
                var column = sheet.Column(c + 1);
                column.Width = columns[c].Width;
                if (columns[c].Format != null) column.Style.NumberFormat.Format = columns[c].Format;
            }
            var header = sheet.Row(1);
            header.Style.Font.Bold = true;
            header.Style.NumberFormat.Format = "@";
            sheet.SheetView.FreezeRows(1);

            int r = 2;
            foreach (var row in rows)
            {
                for (int c = 0; c < row.Length; c++)
                    SetValue(sheet.Cell(r, c + 1), row[c]);
                r++;
            }
            if (r > 2) sheet.Range(1, 1, r - 1, columns.Count).SetAutoFilter();
        }

        private static void SetValue(IXLCell cell, object value)
        {
            switch (value)
            {
                case null:              return;
                case string s:          cell.Value = s; break;
                case int i:             cell.Value = i; break;
                case decimal d:         cell.Value = (double)d; break;
                case DateTime dt:       cell.Value = dt; break;
                case TimeSpan ts:       cell.Value = ts; break;
                default:                cell.Value = value.ToString(); break;
            }
        }

        // Fecha UTC de la BD en la hora de la planta (DA-8).
        private static DateTime? Local(DateTime? utc)
            => utc.HasValue ? SessionHelper.ToLocal(utc.Value) : (DateTime?)null;
    }
}
