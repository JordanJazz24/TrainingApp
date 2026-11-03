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
    // Pasos del dashboard: el ranking por % de sesiones con fallo. También lo usa el Resumen
    // para su top 5 (Rank). Lo llama DashboardService, que ya verificó CanViewReports.
    internal sealed class StepsQuery
    {
        private readonly IDashboardRepository _dashboard;
        private readonly Func<DateTime> _utcNow;

        public StepsQuery(IDashboardRepository dashboard, Func<DateTime> utcNow)
        {
            _dashboard = dashboard;
            _utcNow    = utcNow;
        }

        // Pasos (AC-17, AC-18): el ranking de pasos por % de sesiones con fallo del período y el
        // modo, de una guía o, con todas, un resumen con el mismo paso de cada guía junto, cada
        // paso con su cambio contra el período anterior (spec 023). Todo sale de los intentos con
        // StepRuns (D-146), la misma regla del detalle. Una guía que no está publicada vale
        // "todas"; un modo fuera de las opciones, el de inicio; un período inválido, Este mes con
        // el aviso (ReportPeriods.Resolve).
        public StepReport Get(StepFilter filter)
        {
            filter = filter ?? new StepFilter();

            var guides = _dashboard.GetGuides();
            int? guideId = PublishedOrAll(guides, filter.GuideId);
            var range = ReportPeriods.Resolve(filter.Period, filter.From, filter.To, _utcNow());
            var mode  = StepModes.Contains(filter.Mode) ? filter.Mode : SessionMode.Assessment;
            var ranking = Rank(guides, guideId, mode, range);

            return new StepReport
            {
                Applied    = new StepFilter
                {
                    Period = range.Period, From = CustomFrom(range), To = CustomTo(range), GuideId = guideId, Mode = mode
                },
                Range      = range,
                Guides     = guides,
                ShowsSkips = ModePolicy.For(mode).AllowsSkip,
                Rankings   = ranking.Count == 0
                    ? new List<GuideStepRanking>()
                    : new List<GuideStepRanking> { new GuideStepRanking { TrainingGuideId = guideId, Steps = ranking } }
            };
        }

        // El ranking de Pasos con los filtros ya validados (lo usan Pasos y el top 5 del Resumen,
        // con las mismas guías y el mismo período), cada paso con su cambio contra el anterior.
        // Una sola lectura cubre el período y el anterior; aquí se separan por el inicio de la
        // sesión (P-8).
        public IList<StepStats> Rank(IList<DashboardGuide> guides, int? guideId, SessionMode mode, ReportRange range)
        {
            var data     = _dashboard.GetStepAttempts(mode, range.ReadFromUtc, range.ToUtc, guideId);
            var guideIds = new HashSet<int>(guides.Select(g => g.Id));
            var attempts = data.Attempts.ToLookup(a => a.SessionId);
            // Solo las sesiones de las guías leídas: una guía publicada entre las dos lecturas no
            // tendría nombre.
            var sessions = data.Sessions.Where(s => guideIds.Contains(s.TrainingGuideId)).ToList();

            var names = StepNameMap(_dashboard.GetGuideSteps());
            Func<int, int, string> nameOf = (stepId, order) => names.TryGetValue(stepId, out var name) ? name : "Paso " + order;

            // Con una guía, sus pasos; con todas, el resumen por nombre de paso (usuario,
            // 2026-10-23), igual en los dos períodos.
            Func<Func<DateTime, bool>, IList<StepStats>> rank = inPeriod =>
            {
                var runs = sessions
                    .Where(s => inPeriod(s.StartedAt))
                    .Select(s => StepRuns.ForSession(s, attempts[s.SessionId]))
                    .ToList();
                if (!guideId.HasValue)
                    return StepStatistics.RankByName(runs, r => nameOf(r.StepId, r.StepOrder));

                var ranked = StepStatistics.Rank(runs);
                foreach (var s in ranked) s.StepName = nameOf(s.StepId, s.StepOrder);
                return ranked;
            };

            var ranking = rank(range.Contains);
            if (!range.Compares) return ranking;

            // El mismo renglón en el anterior: el paso con una guía, el nombre con todas.
            Func<StepStats, string> key = s => guideId.HasValue ? s.StepId.ToString() : s.StepName;
            var previous = rank(range.InPrevious).ToDictionary(key);
            foreach (var s in ranking)
            {
                previous.TryGetValue(key(s), out var before);
                // Un % con fallo mejora al bajar.
                s.FailureRateChange = ReportChange.Points(s.FailureRate, s.Sessions,
                    before?.FailureRate, before?.Sessions ?? 0, higherIsBetter: false);
            }
            return ranking;
        }

        // Los modos de Pasos (spec 018 → Filtros): Guided casi no tiene fallos.
        private static readonly SessionMode[] StepModes = { SessionMode.Assessment, SessionMode.FreePractice };
    }
}
