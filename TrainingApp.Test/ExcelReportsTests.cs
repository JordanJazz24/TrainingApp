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

        // AC-21: con 0 filas, solo los encabezados en español.
        [Test]
        public void NoRows_OnlyHeaders()
        {
            using (var book = ExcelReports.FollowUp(new FollowUpReport { Guides = Guides, Rows = new List<FollowUpRow>() }))
            {
                var sheet = book.Worksheet("Seguimiento");
                Assert.That(sheet.Cell(1, 1).GetString(), Is.EqualTo("Código"));
                Assert.That(sheet.LastRowUsed().RowNumber(), Is.EqualTo(1));
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
                var headers = sheet.Row(1).CellsUsed().Select(c => c.GetString()).ToList();
                Assert.That(headers, Does.Not.Contain("Saltos"));
                Assert.That(sheet.Cell(2, 1).GetString(), Is.EqualTo("Todas las guías"));
                Assert.That(sheet.Cell(2, 4).GetDouble(), Is.EqualTo(0.25));
                Assert.That(sheet.Cell(2, headers.IndexOf("Motivos y acciones") + 1).GetString(),
                    Is.EqualTo("Decisión incorrecta · ResourceSetup.Save (2)"));
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

            using (var book = ExcelReports.Sessions(export))
            {
                Assert.That(book.Worksheets.Select(w => w.Name), Is.EqualTo(new[] { "Sesiones", "Intentos por paso" }));
                var sessions = book.Worksheet("Sesiones");
                Assert.That(sessions.Cell(2, 5).GetString(), Is.EqualTo("Práctica libre"));
                Assert.That(sessions.Cell(2, 10).IsEmpty(), Is.True, "Práctica libre no muestra puntaje (D-90)");
                // Hora de la planta (UTC-6).
                Assert.That(sessions.Cell(2, 7).GetDateTime(), Is.EqualTo(new DateTime(2026, 10, 20, 12, 0, 0)));
                Assert.That(book.Worksheet("Intentos por paso").Cell(2, 7).GetString(), Is.EqualTo("Saltado"));
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
            using (var book = ExcelReports.Sessions(new SessionExport { Guides = Guides, Rows = rows }))
            using (var stream = new MemoryStream())
            {
                var built = watch.Elapsed;
                book.SaveAs(stream);
                TestContext.Progress.WriteLine("Armar: {0:N1} s · Guardar: {1:N1} s · Total: {2:N1} s · {3:N1} MB",
                    built.TotalSeconds, (watch.Elapsed - built).TotalSeconds, watch.Elapsed.TotalSeconds, stream.Length / 1048576.0);
            }
        }
    }
}
