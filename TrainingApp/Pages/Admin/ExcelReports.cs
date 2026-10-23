using System;
using System.Collections.Generic;
using System.IO;
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
    // Operadores, Pasos y Seguimiento con ClosedXML (D-140), pocas filas. Las sesiones, que con
    // todo el historial son cientos de miles de filas, en streaming (XlsxStreamWriter, E7,
    // D-148), con el mismo aspecto y los mismos formatos (XlsxStreamWriter.FormatCode).
    //
    // Sin AdjustToContents: medir el texto carga las fuentes del sistema (~20 s la primera vez,
    // plan 018 → E1); cada columna tiene su ancho.
    public static class ExcelReports
    {
        public static XLWorkbook Operators(OperatorPage page)
        {
            var columns = new List<XlsxColumn> { new XlsxColumn("Código", 12), new XlsxColumn("Nombre", 32) };
            foreach (var g in page.Guides)
            {
                columns.Add(new XlsxColumn(g.AreaName + " · Estado", 18));
                columns.Add(new XlsxColumn(g.AreaName + " · Mejor puntaje", 14, XlsxFormat.Percent));
                columns.Add(new XlsxColumn(g.AreaName + " · Último puntaje", 14, XlsxFormat.Percent));
            }
            columns.Add(new XlsxColumn("Última actividad", 17, XlsxFormat.Date));

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
            var columns = new List<XlsxColumn>
            {
                new XlsxColumn("Guía", 18), new XlsxColumn("Paso", 30), new XlsxColumn("Sesiones", 10),
                new XlsxColumn("% con fallo", 11, XlsxFormat.Percent), new XlsxColumn("Intentos fallidos", 12),
                new XlsxColumn("Veces sin acierto", 12)
            };
            if (report.ShowsSkips) columns.Add(new XlsxColumn("Saltos", 9));
            columns.Add(new XlsxColumn("Mediana", 10, XlsxFormat.Time));
            columns.Add(new XlsxColumn("Motivos y acciones", 70));

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
            var columns = new List<XlsxColumn>
            {
                new XlsxColumn("Código", 12), new XlsxColumn("Nombre", 32), new XlsxColumn("Guía", 18), new XlsxColumn("Estado", 18),
                new XlsxColumn("Última sesión de la guía", 17, XlsxFormat.Date),
                new XlsxColumn("S-1 · Assessment reprobados", 14), new XlsxColumn("S-2 · Días sin sesiones", 14),
                new XlsxColumn("S-3 · Pasos sin acierto", 50), new XlsxColumn("Reglas", 70)
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

        // Sesiones del Resumen (AC-22): una fila por sesión y una por sesión y paso, escritas en
        // streaming a output (E7, D-148).
        public static void Sessions(SessionExport export, Stream output)
        {
            var guides = export.Guides.ToDictionary(g => g.Id);
            using (var writer = new XlsxStreamWriter(output))
            {
                writer.WriteSheet("Sesiones", new List<XlsxColumn>
                {
                    new XlsxColumn("Sesión", 9), new XlsxColumn("Código", 12), new XlsxColumn("Nombre", 32), new XlsxColumn("Guía", 18),
                    new XlsxColumn("Modo", 14), new XlsxColumn("Estado", 12), new XlsxColumn("Inicio", 17, XlsxFormat.Date),
                    new XlsxColumn("Fin", 17, XlsxFormat.Date), new XlsxColumn("Duración", 10, XlsxFormat.Time),
                    new XlsxColumn("Puntaje", 9, XlsxFormat.Percent), new XlsxColumn("Intentos fallidos", 12)
                }, export.Rows.Select(r => new object[]
                {
                    r.Session.SessionId, r.Code1, r.Name, guides[r.Session.TrainingGuideId].AreaName,
                    ReportText.Mode(r.Session.Mode), ReportText.Status(r.Session.Status),
                    Local(r.Session.StartedAt), Local(r.Session.CompletedAt), r.Duration, r.Score, r.FailedAttempts
                }));

                writer.WriteSheet("Intentos por paso", new List<XlsxColumn>
                {
                    new XlsxColumn("Sesión", 9), new XlsxColumn("Código", 12), new XlsxColumn("Guía", 18), new XlsxColumn("Modo", 14),
                    new XlsxColumn("Orden", 7), new XlsxColumn("Paso", 30), new XlsxColumn("Resultado", 13),
                    new XlsxColumn("Intentos fallidos", 12), new XlsxColumn("Duración", 10, XlsxFormat.Time),
                    new XlsxColumn("Último intento", 17, XlsxFormat.Date)
                }, export.Rows.SelectMany(r => r.Steps.Select(s => new object[]
                {
                    r.Session.SessionId, r.Code1, guides[r.Session.TrainingGuideId].AreaName, ReportText.Mode(r.Session.Mode),
                    s.Run.StepOrder, s.StepName, ReportText.Outcome(s.Run.Outcome), s.Run.FailedAttempts,
                    s.Run.Duration, Local(s.Run.LastAttempt)
                })));
            }
        }

        private static XLWorkbook Book(string sheet, IList<XlsxColumn> columns, IEnumerable<object[]> rows)
        {
            var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheet);
            for (int c = 0; c < columns.Count; c++)
            {
                ws.Cell(1, c + 1).Value = columns[c].Header;
                var column = ws.Column(c + 1);
                column.Width = columns[c].Width;
                var format = XlsxStreamWriter.FormatCode(columns[c].Format);
                if (format != null) column.Style.NumberFormat.Format = format;
            }
            var header = ws.Row(1);
            header.Style.Font.Bold = true;
            header.Style.NumberFormat.Format = "@";
            ws.SheetView.FreezeRows(1);

            int r = 2;
            foreach (var row in rows)
            {
                for (int c = 0; c < row.Length; c++)
                    SetValue(ws.Cell(r, c + 1), row[c]);
                r++;
            }
            if (r > 2) ws.Range(1, 1, r - 1, columns.Count).SetAutoFilter();
            return workbook;
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
