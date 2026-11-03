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
    // Seguimiento del dashboard: los operadores que cumplen S-1, S-2 o S-3 en una guía. Lo llama
    // DashboardService, que ya verificó CanViewReports.
    internal sealed class FollowUpQuery
    {
        private readonly IDashboardRepository _dashboard;
        private readonly Func<DateTime> _utcNow;

        public FollowUpQuery(IDashboardRepository dashboard, Func<DateTime> utcNow)
        {
            _dashboard = dashboard;
            _utcNow    = utcNow;
        }

        public FollowUpReport Get(FollowUpFilter filter)
        {
            filter = filter ?? new FollowUpFilter();

            var guides  = _dashboard.GetGuides();
            int? guideId = PublishedOrAll(guides, filter.GuideId);
            var shown   = guides.Where(g => !guideId.HasValue || g.Id == guideId.Value).ToList();
            var shownIds = new HashSet<int>(shown.Select(g => g.Id));
            var now     = _utcNow();

            // Las sesiones y el estado de cada operador en cada guía que entrenó.
            var byPair = _dashboard.GetSessions(null, false)
                .Where(s => shownIds.Contains(s.TrainingGuideId))
                .GroupBy(s => new { s.UserId, s.TrainingGuideId })
                .ToDictionary(g => g.Key, g =>
                {
                    var list = g.ToList();
                    return new { Sessions = list, Readiness = ReadinessPolicy.ForGuide(g.Key.TrainingGuideId, list) };
                });

            // Los pasos de los Assessment (abandonados y en curso incluidos), solo de quien no
            // aprobó esa guía: quien la aprobó sale de Seguimiento con todas sus reglas.
            var assessments = _dashboard.GetStepAttempts(SessionMode.Assessment, null, null, guideId);
            var attempts    = assessments.Attempts.ToLookup(a => a.SessionId);
            var runs = assessments.Sessions
                .Where(s => byPair.TryGetValue(new { s.UserId, s.TrainingGuideId }, out var pair)
                            && pair.Readiness.State != ReadinessState.Passed)
                .ToLookup(s => new { s.UserId, s.TrainingGuideId }, s => StepRuns.ForSession(s, attempts[s.SessionId]));

            var stepsByGuide = _dashboard.GetGuideSteps().ToLookup(st => st.TrainingGuideId);
            var rows = new List<FollowUpRow>();
            foreach (var op in _dashboard.GetOperators(string.Empty))
            {
                foreach (var g in shown)
                {
                    var key = new { op.UserId, TrainingGuideId = g.Id };
                    if (!byPair.TryGetValue(key, out var pair)) continue;

                    var readiness = pair.Readiness;
                    var reasons = FollowUpRules.ForGuide(readiness, pair.Sessions, runs[key], now);
                    if (reasons.Count == 0) continue;

                    var guideSteps = stepsByGuide[g.Id].ToList();
                    foreach (var s in reasons.SelectMany(r => r.Steps))
                        s.StepName = StepName(guideSteps, s.StepId, s.StepOrder);

                    rows.Add(new FollowUpRow
                    {
                        UserId          = op.UserId,
                        Code1           = op.Code1,
                        Name            = op.Name,
                        TrainingGuideId = g.Id,
                        State           = readiness.State,
                        LastActivity    = readiness.LastActivity,
                        Reasons         = reasons
                    });
                }
            }

            // Primero quien cumple más reglas (Fase 11, D-160); a igual número, por guía y nombre.
            // Solo orden: qué reglas aplican lo decide FollowUpRules.
            var order = shown.Select((g, i) => new { g.Id, i }).ToDictionary(x => x.Id, x => x.i);
            var ordered = rows.OrderByDescending(r => r.Reasons.Count)
                              .ThenBy(r => order[r.TrainingGuideId])
                              .ThenBy(r => r.Name, StringComparer.CurrentCulture).ToList();
            return new FollowUpReport
            {
                Applied = new FollowUpFilter { GuideId = guideId },
                Guides  = guides,
                Rows    = ordered,
                Summary = FollowUpSummary.For(ordered)
            };
        }
    }
}
