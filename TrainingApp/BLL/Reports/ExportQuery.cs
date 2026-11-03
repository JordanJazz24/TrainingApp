using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;
using static TrainingApp.BLL.Reports.DashboardQueryKit;

namespace TrainingApp.BLL.Reports
{
    // Export de sesiones del Resumen (el Excel). Lo llama DashboardService, que ya verificó
    // CanViewReports.
    internal sealed class ExportQuery
    {
        private readonly IDashboardRepository _dashboard;
        private readonly Func<DateTime> _utcNow;

        public ExportQuery(IDashboardRepository dashboard, Func<DateTime> utcNow)
        {
            _dashboard = dashboard;
            _utcNow    = utcNow;
        }

        public SessionExport Get(SummaryFilter filter)
        {
            filter = filter ?? new SummaryFilter();

            var guides = _dashboard.GetGuides();
            int? guideId = PublishedOrAll(guides, filter.GuideId);
            var range = ReportPeriods.Resolve(filter.Period, filter.From, filter.To, _utcNow());
            var guideIds = new HashSet<int>(guides.Select(g => g.Id));

            // Una lectura de intentos (todos los modos) y una de sesiones, las dos con el período y
            // la guía (E7). Las sesiones salen de GetSessions: traen el puntaje y el cierre, y
            // también las que no tienen intentos.
            var bySession = _dashboard.GetStepAttempts(null, range.FromUtc, range.ToUtc, guideId).Attempts.ToLookup(a => a.SessionId);
            var all = _dashboard.GetSessions(null, false, range.FromUtc, range.ToUtc, guideId)
                .Where(s => guideIds.Contains(s.TrainingGuideId))
                .ToList();

            var operators = _dashboard.GetOperators(string.Empty).ToDictionary(o => o.UserId);
            var names = StepNameMap(_dashboard.GetGuideSteps());

            return new SessionExport
            {
                Applied = new SummaryFilter { Period = range.Period, From = CustomFrom(range), To = CustomTo(range), GuideId = guideId },
                Range   = range,
                Guides  = guides,
                Rows    = all
                    .Where(s => operators.ContainsKey(s.UserId))
                    .OrderByDescending(s => s.StartedAt)
                    .Select(s =>
                    {
                        var runs = StepRuns.ForSession(s, bySession[s.SessionId]);
                        return new SessionExportRow
                        {
                            Session        = s,
                            Code1          = operators[s.UserId].Code1,
                            Name           = operators[s.UserId].Name,
                            Duration       = StepRuns.SessionDuration(s),
                            FailedAttempts = runs.Sum(r => r.FailedAttempts),
                            Score          = VisibleScore(s),
                            Steps          = runs.Select(r => new SessionStepRow
                            {
                                StepName = names.TryGetValue(r.StepId, out var name) ? name : "Paso " + r.StepOrder,
                                Run      = r
                            }).ToList()
                        };
                    })
                    .ToList()
            };
        }
    }
}
