using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Auth;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;
using static TrainingApp.BLL.Reports.DashboardQueryKit;

namespace TrainingApp.BLL.Reports
{
    // Operadores del dashboard: la lista con sus filtros y el detalle de un operador. Lo llama
    // DashboardService, que ya verificó CanViewReports.
    internal sealed class OperatorsQuery
    {
        public const int PageSize = 50;

        // Búsqueda: largo máximo del texto (la caja tiene MaxLength=100; el resto es manipulación).
        private const int MaxSearchLength = 100;

        private readonly IDashboardRepository _dashboard;
        private readonly IUserRepository _users;
        private readonly Func<DateTime> _utcNow;

        public OperatorsQuery(IDashboardRepository dashboard, IUserRepository users, Func<DateTime> utcNow)
        {
            _dashboard = dashboard;
            _users     = users;
            _utcNow    = utcNow;
        }

        public OperatorPage Find(OperatorFilter filter, bool paged)
        {
            filter = filter ?? new OperatorFilter();

            var guides = _dashboard.GetGuides();
            var areas  = OperatorArea.AreasOf(guides);
            bool withoutArea = filter.WithoutArea;
            int? areaId = !withoutArea && filter.AreaId.HasValue && areas.Any(a => a.AreaId == filter.AreaId.Value)
                ? filter.AreaId : null;
            ReadinessState? state = filter.State.HasValue && Enum.IsDefined(typeof(ReadinessState), filter.State.Value)
                ? filter.State : null;

            var text = (filter.Text ?? string.Empty).Trim();
            if (text.Length > MaxSearchLength) text = text.Substring(0, MaxSearchLength);

            // Solo las sesiones de las guías leídas, como en el detalle: una guía publicada entre
            // las dos lecturas no cuenta.
            var guideIds = new HashSet<int>(guides.Select(g => g.Id));
            var sessionsByUser = _dashboard.GetSessions(null, false)
                .Where(s => guideIds.Contains(s.TrainingGuideId))
                .ToLookup(s => s.UserId);
            // Todos los que coinciden con la búsqueda, de cualquier área: el resumen por estado del
            // encabezado (spec 022, brief 6) cuenta sobre ellos; la lista aplica después el área y
            // el estado.
            var now = _utcNow();
            var matched = _dashboard.GetOperators(text)
                .Select(o =>
                {
                    var mine = sessionsByUser[o.UserId].ToList();
                    var view = OperatorArea.For(o.AreaId, guides, mine);
                    return new
                    {
                        o.AreaId,
                        Row = new OperatorRow
                        {
                            UserId       = o.UserId,
                            Code1        = o.Code1,
                            Name         = o.Name,
                            AreaText     = AreaLabel.Of(o.ProductName, o.AreaName) ?? AreaLabel.NoArea,
                            AreaStatus   = view.Status,
                            AreaGuides   = view.AreaGuides,
                            LastActivity = mine.Count > 0 ? mine.Max(s => s.StartedAt) : (DateTime?)null
                        }
                    };
                })
                .ToList();
            foreach (var m in matched)
                OperatorSummary.MarkActivity(m.Row, now);

            var inArea = matched
                .Where(m => withoutArea ? !m.AreaId.HasValue : !areaId.HasValue || m.AreaId == areaId)
                .Select(m => m.Row)
                .ToList();
            var rows = inArea.Where(r => OperatorArea.Matches(r.AreaStatus, r.AreaGuides, state)).ToList();

            // Con "Sin área" elegida, los estados se cuentan en todas las áreas: elegir uno vuelve
            // a Todas (un operador sin área no tiene estado, D-267).
            var summary = OperatorSummary.For(withoutArea ? matched.Select(m => m.Row).ToList() : inArea,
                matched.Count(m => !m.AreaId.HasValue), rows);

            var applied = new OperatorFilter { Text = text, State = state, AreaId = areaId, WithoutArea = withoutArea, Page = 1 };
            if (!paged)
                return new OperatorPage
                {
                    Applied   = applied,
                    Areas     = areas,
                    Rows      = rows,
                    Total     = rows.Count,
                    Page      = 1,
                    PageCount = 1,
                    Summary   = summary
                };

            int pageCount = Math.Max(1, (rows.Count + PageSize - 1) / PageSize);
            int page = Math.Min(Math.Max(1, filter.Page), pageCount);

            applied.Page = page;
            return new OperatorPage
            {
                Applied   = applied,
                Areas     = areas,
                Rows      = rows.Skip((page - 1) * PageSize).Take(PageSize).ToList(),
                Total     = rows.Count,
                Page      = page,
                PageCount = pageCount,
                Summary   = summary
            };
        }

        public OperatorDetail Get(int operatorId)
        {
            var user = _users.GetById(operatorId);
            if (user == null || !AccessPolicy.CanTrain(user.Role)) return null;

            var guides   = _dashboard.GetGuides();
            // Solo las sesiones de las guías leídas: una guía publicada entre las dos lecturas no
            // tendría nombre.
            var guideIds = new HashSet<int>(guides.Select(g => g.Id));
            var sessions = _dashboard.GetSessions(operatorId, false).Where(s => guideIds.Contains(s.TrainingGuideId)).ToList();
            var attempts = _dashboard.GetOperatorAttempts(operatorId).ToLookup(a => a.SessionId);
            var steps    = _dashboard.GetGuideSteps();
            var runs     = sessions.ToDictionary(s => s.SessionId, s => StepRuns.ForSession(s, attempts[s.SessionId]));
            var byId     = guides.ToDictionary(g => g.Id);
            var areaText = guides.ToDictionary(g => g.Id, g => AreaLabel.Of(g.ProductName, g.AreaName));

            // Su área primero y después las otras con sesiones (spec 024, D-269).
            var view  = OperatorArea.For(user.AreaId, guides, sessions);
            var shown = view.AreaGuides.Concat(view.OtherGuides).Select(r => byId[r.TrainingGuideId]).ToList();

            var rows = sessions
                .OrderByDescending(s => s.StartedAt)
                .Select(s => new OperatorSessionRow
                {
                    Session        = s,
                    Duration       = StepRuns.SessionDuration(s),
                    FailedAttempts = runs[s.SessionId].Sum(r => r.FailedAttempts),
                    Score          = VisibleScore(s),
                    AreaText       = areaText[s.TrainingGuideId],
                    Passed         = ReadinessPolicy.IsPassed(s)
                })
                .ToList();

            return new OperatorDetail
            {
                UserId      = user.Id,
                Code1       = user.Code1,
                Name        = user.Name,
                AreaText    = AreaLabel.Of(user.ProductName, user.AreaName) ?? AreaLabel.NoArea,
                Guides      = guides,
                AreaStatus  = view.Status,
                AreaGuides  = view.AreaGuides,
                OtherGuides = view.OtherGuides,
                Sessions    = rows,
                Reinforcement = shown
                    .Select(g => new GuideReinforcement
                    {
                        TrainingGuideId = g.Id,
                        Steps = Named(ReinforcementRule.ForGuide(sessions
                                .Where(s => s.TrainingGuideId == g.Id && s.Mode == SessionMode.Assessment)
                                .Select(s => runs[s.SessionId])),
                            steps.Where(st => st.TrainingGuideId == g.Id).ToList())
                    })
                    .Where(r => r.Steps.Count > 0)
                    .ToList(),
                // Las guías que entrenó; los intentos fallidos y el puntaje, como en las sesiones.
                Curves = shown
                    .Where(g => sessions.Any(s => s.TrainingGuideId == g.Id))
                    .Select(g => LearningCurve.ForGuide(g.Id, sessions,
                        s => runs[s.SessionId].Sum(r => r.FailedAttempts), VisibleScore))
                    .ToList(),
                Activity = OperatorActivity.For(rows, view.AreaGuides.Concat(view.OtherGuides).ToList(), _utcNow())
            };
        }

        // Nombre de cada paso como en el resumen final (StepNames).
        private static IList<ReinforcementStep> Named(IList<ReinforcementStep> reinforce, IList<TrainingStep> guideSteps)
        {
            foreach (var r in reinforce)
                r.StepName = StepName(guideSteps, r.StepId, r.StepOrder);
            return reinforce;
        }
    }
}
