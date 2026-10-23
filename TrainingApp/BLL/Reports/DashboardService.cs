using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Auth;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Reports
{
    // Filtros de la lista de Operadores (spec 018, AC-13). Los valida el servicio: lo que llega
    // de la URL puede venir manipulado.
    public class OperatorFilter
    {
        public string Text { get; set; }

        // null = todos los estados.
        public ReadinessState? State { get; set; }

        // null = todas las guías.
        public int? GuideId { get; set; }

        // Desde 1.
        public int Page { get; set; } = 1;
    }

    // Un operador en la lista: su estado en cada guía publicada (en el orden de Guides de la
    // página) y su última actividad.
    public class OperatorRow
    {
        public int UserId { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }
        public IList<GuideReadiness> Guides { get; set; }

        // Sin iniciar: ninguna sesión en ninguna guía publicada.
        public bool NotStarted => Guides.All(g => g.State == ReadinessState.NotStarted);

        // Inicio de su sesión más reciente (AC-13); null si nunca entrenó.
        public DateTime? LastActivity => Guides.Max(g => g.LastActivity);
    }

    public class OperatorPage
    {
        // Los filtros que se aplicaron, ya validados: la página los muestra y arma sus enlaces
        // con ellos (Page queda abajo).
        public OperatorFilter Applied { get; set; }

        public IList<DashboardGuide> Guides { get; set; }
        public IList<OperatorRow> Rows { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }       // desde 1
        public int PageCount { get; set; }
    }

    // Una sesión en el detalle del Operador (AC-14).
    public class OperatorSessionRow
    {
        public DashboardSession Session { get; set; }

        // Null si sigue en curso ("—").
        public TimeSpan? Duration { get; set; }
        public int FailedAttempts { get; set; }
    }

    // Los pasos a reforzar de una guía.
    public class GuideReinforcement
    {
        public int TrainingGuideId { get; set; }
        public IList<ReinforcementStep> Steps { get; set; }
    }

    // Detalle de un Operador (spec 018, AC-14).
    public class OperatorDetail
    {
        public int UserId { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }
        public IList<DashboardGuide> Guides { get; set; }

        // Una por guía publicada, en el orden de Guides.
        public IList<GuideReadiness> Readiness { get; set; }

        // De la más reciente a la más vieja.
        public IList<OperatorSessionRow> Sessions { get; set; }

        // Solo las guías con algún paso a reforzar, en el orden de Guides.
        public IList<GuideReinforcement> Reinforcement { get; set; }
    }

    // Filtros de Pasos (spec 018 → Filtros). Los valida el servicio, como OperatorFilter.
    public class StepFilter
    {
        public ReportPeriod Period { get; set; } = ReportPeriod.Last30Days;

        // null = todas las guías.
        public int? GuideId { get; set; }

        // Assessment (al entrar) o Práctica libre; Guided no se ofrece.
        public SessionMode Mode { get; set; } = SessionMode.Assessment;
    }

    // El ranking de pasos de una guía, o el resumen de todas.
    public class GuideStepRanking
    {
        // null = todas las guías, con el mismo paso de cada una en un renglón.
        public int? TrainingGuideId { get; set; }
        public IList<StepStats> Steps { get; set; }
    }

    // Pasos (spec 018, AC-17 y AC-18).
    public class StepReport
    {
        // Los filtros que se aplicaron, ya validados.
        public StepFilter Applied { get; set; }

        public IList<DashboardGuide> Guides { get; set; }

        // Con una guía, su ranking; con todas, un solo resumen (TrainingGuideId null). Vacío si
        // no hay intentos en el período.
        public IList<GuideStepRanking> Rankings { get; set; }

        // Los saltos solo existen en el modo que permite saltar (Práctica libre, AC-18).
        public bool ShowsSkips { get; set; }
    }

    // Dashboard de Training (spec 018). Lee lo que los SPs agregan y aplica las reglas
    // (ReadinessPolicy). Cada método recibe el userId del usuario autenticado y verifica
    // CanViewReports sin confiar en la página: si no la tiene, es una excepción.
    public class DashboardService
    {
        public const int PageSize = 50;

        // Búsqueda: largo máximo del texto (la caja tiene MaxLength=100; el resto es manipulación).
        private const int MaxSearchLength = 100;

        private readonly IDashboardRepository _dashboard;
        private readonly IUserRepository _users;

        public DashboardService()
            : this(new DashboardRepository(), new UserRepository())
        {
        }

        // Para los tests: datos en memoria.
        public DashboardService(IDashboardRepository dashboard, IUserRepository users)
        {
            _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
            _users     = users ?? throw new ArgumentNullException(nameof(users));
        }

        // Las guías publicadas, para los filtros de las páginas.
        public IList<DashboardGuide> GetGuides(int userId)
        {
            RequireReports(userId);
            return _dashboard.GetGuides();
        }

        // Una página de Operadores (AC-7, AC-13), por nombre. Los filtros:
        //   · estado solo: el operador tiene ese estado en alguna guía; Sin iniciar = ninguna
        //     sesión en ninguna guía;
        //   · guía sola: el operador entrenó esa guía;
        //   · estado y guía: su estado en esa guía (Sin iniciar = nunca entrenó esa guía).
        // Una guía que no está publicada o un estado fuera del enum valen "todas" o "todos";
        // una página fuera de rango, la última que exista.
        public OperatorPage SearchOperators(int userId, OperatorFilter filter)
        {
            RequireReports(userId);
            filter = filter ?? new OperatorFilter();

            var guides = _dashboard.GetGuides();
            int? guideId = PublishedOrAll(guides, filter.GuideId);
            ReadinessState? state = filter.State.HasValue && Enum.IsDefined(typeof(ReadinessState), filter.State.Value)
                ? filter.State : null;

            var text = (filter.Text ?? string.Empty).Trim();
            if (text.Length > MaxSearchLength) text = text.Substring(0, MaxSearchLength);

            var sessionsByUser = _dashboard.GetSessions(null, false).ToLookup(s => s.UserId);
            var rows = _dashboard.GetOperators(text)
                .Select(o => new OperatorRow
                {
                    UserId = o.UserId,
                    Code1  = o.Code1,
                    Name   = o.Name,
                    Guides = guides.Select(g => ReadinessPolicy.ForGuide(g.Id, sessionsByUser[o.UserId])).ToList()
                })
                .Where(r => Matches(r, state, guideId))
                .ToList();

            int pageCount = Math.Max(1, (rows.Count + PageSize - 1) / PageSize);
            int page = Math.Min(Math.Max(1, filter.Page), pageCount);

            return new OperatorPage
            {
                Applied   = new OperatorFilter { Text = text, State = state, GuideId = guideId, Page = page },
                Guides    = guides,
                Rows      = rows.Skip((page - 1) * PageSize).Take(PageSize).ToList(),
                Total     = rows.Count,
                Page      = page,
                PageCount = pageCount
            };
        }

        // El detalle de un operador (AC-14); null si el id no existe o no es de un operador
        // (DA-9: un administrador no tiene detalle). Los intentos fallidos de cada sesión y los
        // pasos a reforzar salen de sus intentos (StepRuns), con la misma regla que Pasos.
        public OperatorDetail GetOperator(int userId, int operatorId)
        {
            RequireReports(userId);

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

            return new OperatorDetail
            {
                UserId    = user.Id,
                Code1     = user.Code1,
                Name      = user.Name,
                Guides    = guides,
                Readiness = guides.Select(g => ReadinessPolicy.ForGuide(g.Id, sessions)).ToList(),
                Sessions  = sessions
                    .OrderByDescending(s => s.StartedAt)
                    .Select(s => new OperatorSessionRow
                    {
                        Session        = s,
                        Duration       = StepRuns.SessionDuration(s),
                        FailedAttempts = runs[s.SessionId].Sum(r => r.FailedAttempts)
                    })
                    .ToList(),
                Reinforcement = guides
                    .Select(g => new GuideReinforcement
                    {
                        TrainingGuideId = g.Id,
                        Steps = Named(ReinforcementRule.ForGuide(sessions
                                .Where(s => s.TrainingGuideId == g.Id && s.Mode == SessionMode.Assessment)
                                .Select(s => runs[s.SessionId])),
                            steps.Where(st => st.TrainingGuideId == g.Id).ToList())
                    })
                    .Where(r => r.Steps.Count > 0)
                    .ToList()
            };
        }

        // Pasos (AC-17, AC-18): el ranking de pasos por % de sesiones con fallo del período y el
        // modo, de una guía o, con todas, un resumen con el mismo paso de cada guía junto. Todo sale de los intentos con StepRuns (D-146), la misma regla del
        // detalle. Una guía que no está publicada vale "todas"; un período o un modo fuera de
        // las opciones, el de inicio.
        public StepReport GetSteps(int userId, StepFilter filter)
        {
            RequireReports(userId);
            filter = filter ?? new StepFilter();

            var guides = _dashboard.GetGuides();
            int? guideId = PublishedOrAll(guides, filter.GuideId);
            var period = Enum.IsDefined(typeof(ReportPeriod), filter.Period) ? filter.Period : ReportPeriod.Last30Days;
            var mode   = StepModes.Contains(filter.Mode) ? filter.Mode : SessionMode.Assessment;

            var data     = _dashboard.GetStepAttempts(mode, ReportPeriods.FromUtc(period, DateTime.UtcNow), guideId);
            var guideIds = new HashSet<int>(guides.Select(g => g.Id));
            var attempts = data.Attempts.ToLookup(a => a.SessionId);
            // Solo las sesiones de las guías leídas: una guía publicada entre las dos lecturas no
            // tendría nombre.
            var runs = data.Sessions
                .Where(s => guideIds.Contains(s.TrainingGuideId))
                .Select(s => StepRuns.ForSession(s, attempts[s.SessionId]))
                .ToList();

            // El nombre de cada paso, con su variante dentro de su guía (StepNames).
            var steps = _dashboard.GetGuideSteps();
            var names = steps.ToDictionary(st => st.Id,
                st => StepNames.Display(steps.Where(x => x.TrainingGuideId == st.TrainingGuideId).ToList(), st));
            Func<int, int, string> nameOf = (stepId, order) => names.TryGetValue(stepId, out var name) ? name : "Paso " + order;

            // Con una guía, sus pasos; con todas, el resumen por nombre de paso (usuario,
            // 2026-10-23).
            IList<StepStats> ranking;
            if (guideId.HasValue)
            {
                ranking = StepStatistics.Rank(runs);
                foreach (var s in ranking) s.StepName = nameOf(s.StepId, s.StepOrder);
            }
            else
                ranking = StepStatistics.RankByName(runs, r => nameOf(r.StepId, r.StepOrder));

            return new StepReport
            {
                Applied    = new StepFilter { Period = period, GuideId = guideId, Mode = mode },
                Guides     = guides,
                ShowsSkips = ModePolicy.For(mode).AllowsSkip,
                Rankings   = ranking.Count == 0
                    ? new List<GuideStepRanking>()
                    : new List<GuideStepRanking> { new GuideStepRanking { TrainingGuideId = guideId, Steps = ranking } }
            };
        }

        // El filtro de guía: una que no está publicada vale "todas" (null).
        private static int? PublishedOrAll(IList<DashboardGuide> guides, int? guideId)
            => guideId.HasValue && guides.Any(g => g.Id == guideId.Value) ? guideId : null;

        // Los modos de Pasos (spec 018 → Filtros): Guided casi no tiene fallos.
        private static readonly SessionMode[] StepModes = { SessionMode.Assessment, SessionMode.FreePractice };

        // Nombre de cada paso como en el resumen final (StepNames).
        private static IList<ReinforcementStep> Named(IList<ReinforcementStep> reinforce, IList<TrainingStep> guideSteps)
        {
            foreach (var r in reinforce)
                r.StepName = StepName(guideSteps, r.StepId, r.StepOrder);
            return reinforce;
        }

        private static string StepName(IList<TrainingStep> guideSteps, int stepId, int stepOrder)
        {
            var step = guideSteps.FirstOrDefault(s => s.Id == stepId);
            return step != null ? StepNames.Display(guideSteps, step) : "Paso " + stepOrder;
        }

        private static bool Matches(OperatorRow row, ReadinessState? state, int? guideId)
        {
            if (guideId.HasValue)
            {
                var inGuide = row.Guides.First(g => g.TrainingGuideId == guideId.Value).State;
                return state.HasValue ? inGuide == state.Value : inGuide != ReadinessState.NotStarted;
            }
            if (!state.HasValue) return true;
            return state.Value == ReadinessState.NotStarted
                ? row.NotStarted
                : row.Guides.Any(g => g.State == state.Value);
        }

        private void RequireReports(int userId)
        {
            var user = _users.GetById(userId);
            if (user == null || !AccessPolicy.CanViewReports(user.Role))
                throw new UnauthorizedAccessException("El usuario no puede ver los reportes.");
        }
    }
}
