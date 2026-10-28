using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using TrainingApp.BLL.Reports;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

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
    // Diseño de la Fase 11 (D-158, mockup v1 → "Excel"), igual en los cuatro: fila 1, el título
    // con la vista, los filtros y la fecha de descarga; fila 2, el encabezado con el acento, fijo
    // y con autofiltro; los estados del operador con su color del tema claro.
    //
    // Sin AdjustToContents: medir el texto carga las fuentes del sistema (~20 s la primera vez,
    // plan 018 → E1); cada columna tiene su ancho.
    public static class ExcelReports
    {
        private const int TitleRow  = 1;
        private const int HeaderRow = 2;

        // Fondo y texto de cada estado (P-2), los de admin.css en claro.
        private static readonly Dictionary<string, (string Fill, string Text)> StateColors = new Dictionary<string, (string, string)>
        {
            { ReportText.State(ReadinessState.Passed),     ("FFE1F3E8", "FF1B7A43") },
            { ReportText.State(ReadinessState.Failed),     ("FFFBE5E3", "FFB93229") },
            { ReportText.State(ReadinessState.InTraining), ("FFFDF0D5", "FF8A5A00") },
            { ReportText.State(ReadinessState.NotStarted), ("FFE6E9ED", "FF5D6673") }
        };

        public static XLWorkbook Operators(OperatorPage page)
        {
            var applied = page.Applied ?? new OperatorFilter();
            var filters = new List<string>();
            if (!string.IsNullOrEmpty(applied.Text)) filters.Add("Búsqueda: \"" + applied.Text + "\"");
            filters.Add("Área: " + AreaText(page.Areas, applied));
            filters.Add("Estado: " + (applied.State.HasValue ? ReportText.State(applied.State.Value).ToLowerInvariant() : "todos"));

            // Como la pantalla (spec 024, P-7): el estado en la guía de su área, sin columnas por
            // guía. Sin guías que entrenar, "Sin área" o "Sin guía publicada" en lugar del estado.
            // Con varias guías en su área (P-6, futuro), los estados unidos y sin puntajes.
            var columns = new List<XlsxColumn>
            {
                new XlsxColumn("Código", 12), new XlsxColumn("Nombre", 32), new XlsxColumn("Área", 24),
                new XlsxColumn("Estado en su área", 18, states: true),
                new XlsxColumn("Mejor puntaje", 14, XlsxFormat.Percent),
                new XlsxColumn("Último puntaje", 14, XlsxFormat.Percent),
                new XlsxColumn("Última actividad", 17, XlsxFormat.Date)
            };

            var rows = page.Rows.Select(r =>
            {
                var single = r.AreaGuides.Count == 1 ? r.AreaGuides[0] : null;
                var state  = ReportText.AreaNote(r.AreaStatus)
                             ?? string.Join("; ", r.AreaGuides.Select(g => ReportText.State(g.State)));
                return new object[]
                {
                    r.Code1, r.Name, r.AreaText, state, single?.BestScore, single?.LastScore, Local(r.LastActivity)
                };
            });

            return Book("Operadores", Title("Operadores", filters), columns, rows);
        }

        public static XLWorkbook Steps(StepReport report)
        {
            var applied = report.Applied ?? new StepFilter();
            // Con un período anterior, dos columnas más por paso (spec 023, D-213, P-10); con
            // Todo, vacías.
            bool compares = report.Range != null && report.Range.Compares;
            string title = Title("Pasos", new[]
            {
                // El rango exacto (P-14).
                "Período: " + (report.Range != null ? LowerFirst(ReportText.Range(report.Range))
                                                    : ReportText.Period(applied.Period).ToLowerInvariant()),
                "Guía: " + GuideText(report.Guides, applied.GuideId),
                "Modo: " + ReportText.Mode(applied.Mode)
            });
            var guides = report.Guides.ToDictionary(g => g.Id);
            var columns = new List<XlsxColumn>
            {
                new XlsxColumn("Guía", 18), new XlsxColumn("Paso", 30), new XlsxColumn("Sesiones", 10),
                new XlsxColumn("% con fallo", 11, XlsxFormat.Percent),
                new XlsxColumn("% con fallo (anterior)", 13, XlsxFormat.Percent), new XlsxColumn("Cambio (pts)", 10),
                new XlsxColumn("Intentos fallidos", 12),
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
                    s.StepName, s.Sessions, s.FailureRate,
                    compares ? s.FailureRateChange?.Previous : null,
                    compares ? s.FailureRateChange?.Change : null,
                    s.FailedAttempts, s.TimesWithoutSuccess
                };
                if (report.ShowsSkips) cells.Add(s.Skips);
                cells.Add(s.MedianDuration);
                cells.Add(string.Join("; ", s.Reasons.Select(ReportText.ReasonCount)));
                return cells.ToArray();
            }));

            return Book("Pasos", title, columns, rows);
        }

        public static XLWorkbook FollowUp(FollowUpReport report)
        {
            // Seguimiento ignora el período: siempre todo el historial.
            string title = Title("Seguimiento", new[]
            {
                "Período: " + ReportText.Period(ReportPeriod.All).ToLowerInvariant(),
                "Guía: " + GuideText(report.Guides, report.Applied?.GuideId)
            });
            var guides = report.Guides.ToDictionary(g => g.Id);
            var columns = new List<XlsxColumn>
            {
                new XlsxColumn("Código", 12), new XlsxColumn("Nombre", 32), new XlsxColumn("Guía", 18), new XlsxColumn("Estado", 18, states: true),
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

            return Book("Seguimiento", title, columns, rows);
        }

        // Sesiones del Resumen (AC-22): una fila por sesión y una por sesión y paso, escritas en
        // streaming a output (E7, D-148).
        public static void Sessions(SessionExport export, Stream output)
        {
            var applied = export.Applied ?? new SummaryFilter();
            var filters = new[]
            {
                // El rango exacto (spec 023, P-10 y P-14), sin comparación.
                "Período: " + (export.Range != null ? LowerFirst(ReportText.Range(export.Range))
                                                    : ReportText.Period(applied.Period).ToLowerInvariant()),
                "Guía: " + GuideText(export.Guides, applied.GuideId)
            };
            var guides = export.Guides.ToDictionary(g => g.Id);
            using (var writer = new XlsxStreamWriter(output))
            {
                writer.WriteSheet("Sesiones", Title("Sesiones", filters), new List<XlsxColumn>
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

                writer.WriteSheet("Intentos por paso", Title("Intentos por paso", filters), new List<XlsxColumn>
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

        // "Pasos. Período: últimos 30 días. Guía: todas. Modo: Assessment. Descargado el
        // 26/10/2026 10:30", la fecha en la hora de la planta (DA-8).
        private static string Title(string view, IEnumerable<string> filters)
            => view + ". " + string.Join(". ", filters) + ". Descargado el "
               + ReportText.Date(DateTime.UtcNow) + ".";

        private static string LowerFirst(string text)
            => string.IsNullOrEmpty(text) ? text : char.ToLowerInvariant(text[0]) + text.Substring(1);

        // "Sensores · LoFlo", "sin área" o "todas" (spec 024).
        private static string AreaText(IEnumerable<ReportArea> areas, OperatorFilter applied)
        {
            if (applied.WithoutArea) return "sin área";
            var area = applied.AreaId.HasValue ? areas?.FirstOrDefault(a => a.AreaId == applied.AreaId.Value) : null;
            return area != null ? AreaLabel.Of(area.ProductName, area.AreaName) : "todas";
        }

        private static string GuideText(IEnumerable<DashboardGuide> guides, int? guideId)
            => guideId.HasValue ? guides.FirstOrDefault(g => g.Id == guideId.Value)?.AreaName ?? "todas" : "todas";

        private static XLWorkbook Book(string sheet, string title, IList<XlsxColumn> columns, IEnumerable<object[]> rows)
        {
            var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheet);

            var titleCell = ws.Cell(TitleRow, 1);
            titleCell.Value = title;
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 14;

            for (int c = 0; c < columns.Count; c++)
            {
                ws.Cell(HeaderRow, c + 1).Value = columns[c].Header;
                var column = ws.Column(c + 1);
                column.Width = columns[c].Width;
                var format = XlsxStreamWriter.FormatCode(columns[c].Format);
                if (format != null) column.Style.NumberFormat.Format = format;
            }
            var header = ws.Range(HeaderRow, 1, HeaderRow, columns.Count).Style;
            header.Font.Bold = true;
            header.Font.FontColor = Color(XlsxColors.OnAccent);
            header.Fill.BackgroundColor = Color(XlsxColors.Accent);
            header.Border.OutsideBorder = XLBorderStyleValues.Thin;
            header.Border.InsideBorder = XLBorderStyleValues.Thin;
            header.Border.OutsideBorderColor = Color(XlsxColors.AccentBorder);
            header.Border.InsideBorderColor = Color(XlsxColors.AccentBorder);
            header.NumberFormat.Format = "@";
            ws.SheetView.FreezeRows(HeaderRow);

            int r = HeaderRow + 1;
            foreach (var row in rows)
            {
                for (int c = 0; c < row.Length; c++)
                {
                    var cell = ws.Cell(r, c + 1);
                    SetValue(cell, row[c]);
                    if (columns[c].States && row[c] is string state && StateColors.TryGetValue(state, out var colors))
                    {
                        cell.Style.Fill.BackgroundColor = Color(colors.Fill);
                        cell.Style.Font.FontColor = Color(colors.Text);
                        cell.Style.Font.Bold = true;
                    }
                }
                r++;
            }
            // Autofiltro sobre el encabezado y los datos, no sobre el título (también sin filas).
            ws.Range(HeaderRow, 1, r - 1, columns.Count).SetAutoFilter();
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

        // Un color ARGB de XlsxColors o StateColors ("FF1F55A5").
        private static XLColor Color(string argb)
            => XLColor.FromArgb(Convert.ToInt32(argb, 16));
    }
}
