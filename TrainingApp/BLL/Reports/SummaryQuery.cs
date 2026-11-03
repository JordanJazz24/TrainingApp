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
    // Resumen del dashboard: estados, tasas, puntaje, actividad y top 5 del período. Lo llama
    // DashboardService, que ya verificó CanViewReports.
    internal sealed class SummaryQuery
    {
        public const int TopStepCount = 5;

        private readonly IDashboardRepository _dashboard;
        private readonly StepsQuery _steps;
        private readonly Func<DateTime> _utcNow;

        public SummaryQuery(IDashboardRepository dashboard, StepsQuery steps, Func<DateTime> utcNow)
        {
            _dashboard = dashboard;
            _steps     = steps;
            _utcNow    = utcNow;
        }

        public SummaryReport Get(SummaryFilter filter)
        {
            filter = filter ?? new SummaryFilter();

            var guides = _dashboard.GetGuides();
            int? guideId = PublishedOrAll(guides, filter.GuideId);
            var now   = _utcNow();
            var range = ReportPeriods.Resolve(filter.Period, filter.From, filter.To, now);

            var shown    = guides.Where(g => !guideId.HasValue || g.Id == guideId.Value).ToList();
            var shownIds = new HashSet<int>(shown.Select(g => g.Id));
            var sessions = _dashboard.GetSessions(null, false).Where(s => shownIds.Contains(s.TrainingGuideId)).ToList();

            var summaries = shown.Select(g =>
            {
                var summary = SummaryRules.ForGuide(g.Id, sessions, range.FromUtc, range.ToUtc);
                if (range.Compares)
                {
                    var before = SummaryRules.ForGuide(g.Id, sessions, range.PrevFromUtc, range.PrevToUtc);
                    summary.PassRateChange = ReportChange.Points(summary.PassRate, summary.AssessedOperators,
                        before.PassRate, before.AssessedOperators, higherIsBetter: true);
                }
                return summary;
            }).ToList();
            int inPeriod = sessions.Count(s => range.Contains(s.StartedAt));

            return new SummaryReport
            {
                Applied        = new SummaryFilter { Period = range.Period, From = CustomFrom(range), To = CustomTo(range), GuideId = guideId },
                Range          = range,
                Guides         = guides,
                GuideSummaries = summaries,
                Totals         = SummaryTotals.Of(summaries, range.Compares),
                Activity       = SummaryRules.Activity(sessions, range, now),
                Sessions       = inPeriod,
                SessionsChange = range.Compares
                    ? ReportChange.Percent(inPeriod, sessions.Count(s => range.InPrevious(s.StartedAt)), higherIsBetter: true)
                    : null,
                // El principio de Pasos en Assessment con la misma guía y el mismo período, así sus
                // números coinciden al seguir el enlace (D-147). Solo pasos con algún fallo: un
                // 0 % no es un paso con fallos.
                TopSteps       = _steps.Rank(guides, guideId, SessionMode.Assessment, range)
                    .Where(s => s.SessionsWithFailure > 0)
                    .Take(TopStepCount)
                    .ToList()
            };
        }
    }
}
