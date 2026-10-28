using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using NUnit.Framework;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;
using TrainingApp.Pages.Admin;

namespace TrainingApp.Test
{
    // Spec 018, E6: los .xlsx (AC-21, AC-22) con los textos de pantalla, y el resultado de cada
    // paso (StepRun.Outcome) de la hoja Intentos por paso.
    [TestFixture]
    public class ExcelReportsTests
    {
        private static readonly IList<DashboardGuide> Guides = new List<DashboardGuide>
        {
            new DashboardGuide { Id = 10, AreaName = "LoFlo", Title = "LoFlo" }
        };

        [Test]
        public void Outcome_FromStepRun()
        {
            Assert.That(new StepRun { HasCorrectAttempt = true, Resolved = true }.Outcome, Is.EqualTo(StepOutcome.Correct));
            Assert.That(new StepRun { Resolved = true, FailedAttempts = 4 }.Outcome, Is.EqualTo(StepOutcome.WithoutSuccess));
            Assert.That(new StepRun { Resolved = true, Skipped = true }.Outcome, Is.EqualTo(StepOutcome.Skipped));
            Assert.That(new StepRun { Resolved = true, Skipped = true, FailedAttempts = 1 }.Outcome, Is.EqualTo(StepOutcome.WithoutSuccess));
            Assert.That(new StepRun { Resolved = false, FailedAttempts = 1 }.Outcome, Is.EqualTo(StepOutcome.Unresolved));
        }

        // AC-21: con 0 filas, el título y los encabezados en español (Fase 11, D-158).
        [Test]
        public void NoRows_TitleAndHeaders()
        {
            using (var book = ExcelReports.FollowUp(new FollowUpReport { Guides = Guides, Rows = new List<FollowUpRow>() }))
            {
                var sheet = book.Worksheet("Seguimiento");
                Assert.That(sheet.Cell(1, 1).GetString(), Does.StartWith("Seguimiento. Período: todo el historial. Guía: todas. Descargado el "));
                Assert.That(sheet.Cell(2, 1).GetString(), Is.EqualTo("Código"));
                Assert.That(sheet.LastRowUsed().RowNumber(), Is.EqualTo(2));
            }
        }

        // Fase 11 (D-158): el título con los filtros aplicados; el encabezado con el acento, fijo y
        // con autofiltro desde la fila 2; cada estado con su color.
        [Test]
        public void Operators_TitleHeaderAndStateColors()
        {
            var page = new OperatorPage
            {
                Guides = Guides,
                Applied = new OperatorFilter { Text = "ana", State = ReadinessState.Failed, GuideId = 10 },
                Rows = new List<OperatorRow>
                {
                    new OperatorRow
                    {
                        Code1 = "C1", Name = "Ana", AreaText = "Sensores · LoFlo",
                        Guides = new List<GuideReadiness> { new GuideReadiness { State = ReadinessState.Failed, BestScore = 0.6m } }
                    }
                }
            };

            using (var book = ExcelReports.Operators(page))
            {
                var sheet = book.Worksheet("Operadores");
                Assert.That(sheet.Cell(1, 1).GetString(), Does.StartWith("Operadores. Búsqueda: \"ana\". Estado: reprobado. Guía: LoFlo. Descargado el "));
                Assert.That(sheet.Cell(1, 1).Style.Font.Bold, Is.True);

                var header = sheet.Cell(2, 1).Style;
                Assert.That(header.Font.Bold, Is.True);
                Assert.That(header.Fill.BackgroundColor, Is.EqualTo(XLColor.FromArgb(unchecked((int)0xFF1F55A5))));
                Assert.That(header.Font.FontColor, Is.EqualTo(XLColor.FromArgb(unchecked((int)0xFFFFFFFF))));
                Assert.That(sheet.SheetView.SplitRow, Is.EqualTo(2));
                Assert.That(sheet.AutoFilter.Range.RangeAddress.FirstAddress.RowNumber, Is.EqualTo(2));

                // Spec 021 (D-175): el área después del nombre.
                Assert.That(sheet.Cell(2, 3).GetString(), Is.EqualTo("Área"));
                Assert.That(sheet.Cell(3, 3).GetString(), Is.EqualTo("Sensores · LoFlo"));

                var state = sheet.Cell(3, 4);
                Assert.That(state.GetString(), Is.EqualTo("Reprobado"));
                Assert.That(state.Style.Fill.BackgroundColor, Is.EqualTo(XLColor.FromArgb(unchecked((int)0xFFFBE5E3))));
                Assert.That(state.Style.Font.FontColor, Is.EqualTo(XLColor.FromArgb(unchecked((int)0xFFB93229))));
                Assert.That(sheet.Cell(3, 5).GetDouble(), Is.EqualTo(0.6));
            }
        }

        // Pasos con "Todas": la guía como "Todas las guías", la tasa como número y los motivos con
        // el texto de pantalla; sin saltos en Assessment.
        [Test]
        public void Steps_SameTextsAsTheScreen()
        {
            var report = new StepReport
            {
                Guides = Guides, ShowsSkips = false,
                Rankings = new List<GuideStepRanking>
                {
                    new GuideStepRanking
                    {
                        TrainingGuideId = null,
                        Steps = new List<StepStats>
                        {
                            new StepStats
                            {
                                StepName = "Resource Setup", Sessions = 4, SessionsWithFailure = 1, FailedAttempts = 2,
                                MedianDuration = TimeSpan.FromSeconds(75),
                                Reasons = new List<ReasonCount> { new ReasonCount { Reason = AttemptReason.WrongDecision, Action = "ResourceSetup.Save", Count = 2 } }
                            }
                        }
                    }
                }
            };

            using (var book = ExcelReports.Steps(report))
            {
                var sheet = book.Worksheet("Pasos");
                var headers = sheet.Row(2).CellsUsed().Select(c => c.GetString()).ToList();
                Assert.That(headers, Does.Not.Contain("Saltos"));
                Assert.That(sheet.Cell(3, 1).GetString(), Is.EqualTo("Todas las guías"));
                Assert.That(sheet.Cell(3, 4).GetDouble(), Is.EqualTo(0.25));
                Assert.That(sheet.Cell(3, headers.IndexOf("Motivos y acciones") + 1).GetString(),
                    Is.EqualTo("Decisión incorrecta · ResourceSetup.Save (2)"));
            }
        }

        // Spec 023 (AC-8, D-213): el título dice el rango y cada paso trae el % del período
        // anterior y el cambio en puntos; con Todo, las dos columnas van vacías.
        [Test]
        public void Steps_PreviousPeriodColumns()
        {
            var now = new DateTime(2026, 10, 28, 18, 0, 0);
            Func<ReportRange, StepReport> report = range => new StepReport
            {
                Guides = Guides, ShowsSkips = false, Range = range, Applied = new StepFilter { Period = range.Period },
                Rankings = new List<GuideStepRanking>
                {
                    new GuideStepRanking
                    {
                        Steps = new List<StepStats>
                        {
                            new StepStats
                            {
                                StepName = "Resource Setup", Sessions = 4, SessionsWithFailure = 2, Reasons = new List<ReasonCount>(),
                                FailureRateChange = ReportChange.Points(0.5m, 4, 0.25m, 4, higherIsBetter: false)
                            }
                        }
                    }
                }
            };

            using (var book = ExcelReports.Steps(report(ReportPeriods.Resolve(ReportPeriod.ThisMonth, null, null, now))))
            {
                var sheet = book.Worksheet("Pasos");
                var headers = sheet.Row(2).CellsUsed().Select(c => c.GetString()).ToList();
                Assert.That(sheet.Cell(1, 1).GetString(), Does.StartWith("Pasos. Período: del 1 al 28 de oct. de 2026. "));
                Assert.That(sheet.Cell(3, headers.IndexOf("% con fallo (anterior)") + 1).GetDouble(), Is.EqualTo(0.25));
                Assert.That(sheet.Cell(3, headers.IndexOf("Cambio (pts)") + 1).GetDouble(), Is.EqualTo(25));
            }

            var all = ReportPeriods.Resolve(ReportPeriod.All, null, null, now);
            using (var book = ExcelReports.Steps(report(all)))
            {
                var sheet = book.Worksheet("Pasos");
                var headers = sheet.Row(2).CellsUsed().Select(c => c.GetString()).ToList();
                Assert.That(sheet.Cell(1, 1).GetString(), Does.StartWith("Pasos. Período: todo el historial. "));
                Assert.That(sheet.Cell(3, headers.IndexOf("% con fallo (anterior)") + 1).IsEmpty(), Is.True);
                Assert.That(sheet.Cell(3, headers.IndexOf("Cambio (pts)") + 1).IsEmpty(), Is.True);
            }
        }

        // AC-22: dos hojas; el puntaje solo cuando el modo lo muestra; el resultado de cada paso.
        [Test]
        public void Sessions_TwoSheets()
        {
            var start = new DateTime(2026, 10, 20, 18, 0, 0);
            var export = new SessionExport
            {
                Guides = Guides,
                Rows = new List<SessionExportRow>
                {
                    new SessionExportRow
                    {
                        Session = new DashboardSession { SessionId = 7, TrainingGuideId = 10, Mode = SessionMode.FreePractice,
                                                         Status = SessionStatus.Completed, FinalScore = 0.9m, StartedAt = start, CompletedAt = start.AddMinutes(30) },
                        Code1 = "C1", Name = "Ana", Duration = TimeSpan.FromMinutes(30), FailedAttempts = 0,
                        Steps = new List<SessionStepRow>
                        {
                            new SessionStepRow { StepName = "Resource Setup", Run = new StepRun { StepOrder = 1, Resolved = true, Skipped = true, LastAttempt = start } }
                        }
                    }
                }
            };

            using (var stream = new MemoryStream())
            {
                ExcelReports.Sessions(export, stream);
                stream.Position = 0;
                using (var book = new XLWorkbook(stream))
                {
                Assert.That(book.Worksheets.Select(w => w.Name), Is.EqualTo(new[] { "Sesiones", "Intentos por paso" }));
                var sessions = book.Worksheet("Sesiones");
                Assert.That(sessions.Cell(1, 1).GetString(), Does.StartWith("Sesiones. Período: este mes. Guía: todas. Descargado el "));
                Assert.That(sessions.Cell(2, 1).GetString(), Is.EqualTo("Sesión"));
                Assert.That(sessions.Cell(2, 1).Style.Font.Bold, Is.True);
                Assert.That(sessions.Cell(2, 1).Style.Fill.BackgroundColor, Is.EqualTo(XLColor.FromArgb(unchecked((int)0xFF1F55A5))));
                Assert.That(sessions.SheetView.SplitRow, Is.EqualTo(2));
                Assert.That(sessions.Cell(3, 9).GetTimeSpan(), Is.EqualTo(TimeSpan.FromMinutes(30)));
                Assert.That(sessions.Cell(3, 5).GetString(), Is.EqualTo("Práctica libre"));
                Assert.That(sessions.Cell(3, 10).IsEmpty(), Is.True, "Práctica libre no muestra puntaje (D-90)");
                // Hora de la planta (UTC-6).
                Assert.That(sessions.Cell(3, 7).GetDateTime(), Is.EqualTo(new DateTime(2026, 10, 20, 12, 0, 0)));
                Assert.That(book.Worksheet("Intentos por paso").Cell(3, 7).GetString(), Is.EqualTo("Saltado"));
                }
            }
        }

        // El .xlsx escrito en streaming cumple el esquema de SpreadsheetML (Excel lo abre sin
        // pedir reparación).
        [Test]
        public void Sessions_ValidSpreadsheetMl()
        {
            var start = new DateTime(2026, 10, 20, 18, 0, 0);
            var export = new SessionExport
            {
                Guides = Guides,
                Rows = new List<SessionExportRow>
                {
                    new SessionExportRow
                    {
                        Session = new DashboardSession { SessionId = 1, TrainingGuideId = 10, Mode = SessionMode.Assessment,
                                                         Status = SessionStatus.Completed, FinalScore = 0.8m, StartedAt = start, CompletedAt = start.AddMinutes(9) },
                        Code1 = "C1", Name = "Ana", Duration = TimeSpan.FromMinutes(9), Score = 0.8m, FailedAttempts = 1,
                        Steps = new List<SessionStepRow>
                        {
                            new SessionStepRow { StepName = "Resource Setup", Run = new StepRun { StepOrder = 1, HasCorrectAttempt = true, Resolved = true, Duration = TimeSpan.FromMinutes(2), LastAttempt = start } }
                        }
                    }
                }
            };

            using (var stream = new MemoryStream())
            {
                ExcelReports.Sessions(export, stream);
                stream.Position = 0;
                using (var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(stream, false))
                {
                    var errors = new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(document)
                        .Select(e => e.Path?.XPath + ": " + e.Description).ToList();
                    Assert.That(errors, Is.Empty);
                }
            }
        }

        // Un carácter que XML no admite (de control) en un nombre no rompe la descarga: se quita.
        [Test]
        public void Sessions_InvalidXmlCharacter_IsRemoved()
        {
            var start = new DateTime(2026, 10, 20, 18, 0, 0);
            var export = new SessionExport
            {
                Guides = Guides,
                Rows = new List<SessionExportRow>
                {
                    new SessionExportRow
                    {
                        Session = new DashboardSession { SessionId = 1, TrainingGuideId = 10, Mode = SessionMode.Guided,
                                                         Status = SessionStatus.InProgress, StartedAt = start },
                        Code1 = "C1", Name = "Ana\u0001 Pérez", Steps = new List<SessionStepRow>()
                    }
                }
            };

            using (var stream = new MemoryStream())
            {
                ExcelReports.Sessions(export, stream);
                stream.Position = 0;
                using (var book = new XLWorkbook(stream))
                    Assert.That(book.Worksheet("Sesiones").Cell(3, 3).GetString(), Is.EqualTo("Ana Pérez"));
            }
        }

        // Sin sesiones: las dos hojas con su título y sus encabezados (AC-21).
        [Test]
        public void Sessions_NoRows_TitleAndHeaders()
        {
            using (var stream = new MemoryStream())
            {
                ExcelReports.Sessions(new SessionExport { Guides = Guides, Rows = new List<SessionExportRow>() }, stream);
                stream.Position = 0;
                using (var book = new XLWorkbook(stream))
                {
                    Assert.That(book.Worksheet("Sesiones").LastRowUsed().RowNumber(), Is.EqualTo(2));
                    Assert.That(book.Worksheet("Intentos por paso").Cell(2, 6).GetString(), Is.EqualTo("Paso"));
                }
            }
        }

        // Medición aproximada para el plan (AC-24): ~22 000 sesiones y ~250 000 filas de pasos,
        // como "Todo" con el generador. Explícita: no corre con el resto.
        [Test, Explicit("Medición de rendimiento")]
        public void Sessions_LargeVolumeTiming()
        {
            var start = new DateTime(2026, 1, 1);
            var rows = Enumerable.Range(1, 22000).Select(i => new SessionExportRow
            {
                Session = new DashboardSession { SessionId = i, TrainingGuideId = 10, Mode = SessionMode.Assessment,
                                                 Status = SessionStatus.Completed, FinalScore = 0.8m, StartedAt = start, CompletedAt = start.AddMinutes(25) },
                Code1 = "C" + i, Name = "Operador " + i, Duration = TimeSpan.FromMinutes(25), FailedAttempts = 3,
                Steps = Enumerable.Range(1, 12).Select(o => new SessionStepRow
                {
                    StepName = "Paso " + o, Run = new StepRun { StepOrder = o, HasCorrectAttempt = true, Resolved = true, Duration = TimeSpan.FromSeconds(90), LastAttempt = start }
                }).ToList()
            }).ToList();

            var watch = Stopwatch.StartNew();
            using (var stream = new MemoryStream())
            {
                ExcelReports.Sessions(new SessionExport { Guides = Guides, Rows = rows }, stream);
                TestContext.Progress.WriteLine("Streaming: {0:N1} s · {1:N1} MB", watch.Elapsed.TotalSeconds, stream.Length / 1048576.0);
            }
        }
    }
}
