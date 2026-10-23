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

        // Null si no se muestra (DashboardService.VisibleScore).
        public decimal? Score { get; set; }
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

        // La curva de aprendizaje de cada guía que entrenó, en el orden de Guides (AC-15).
        public IList<GuideCurve> Curves { get; set; }
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

    // Filtros del Resumen (spec 018 → Filtros). Los valida el servicio.
    public class SummaryFilter
    {
        public ReportPeriod Period { get; set; } = ReportPeriod.Last30Days;

        // null = todas las guías.
        public int? GuideId { get; set; }
    }

    // Resumen (spec 018, AC-9 a AC-12), el inicio del administrador.
    public class SummaryReport
    {
        // Los filtros que se aplicaron, ya validados.
        public SummaryFilter Applied { get; set; }

        // Todas las guías publicadas, para el filtro.
        public IList<DashboardGuide> Guides { get; set; }

        // Una por guía mostrada (todas o la del filtro), en el orden de Guides.
        public IList<GuideSummary> GuideSummaries { get; set; }

        // Sesiones por semana y modo del período; vacío sin sesiones.
        public IList<WeekActivity> Weeks { get; set; }

        // Top 5 de pasos por % de sesiones con fallo en Assessment del período: los primeros de
        // Pasos con los mismos filtros (D-147).
        public IList<StepStats> TopSteps { get; set; }
    }

    // Un paso de una sesión en la descarga de sesiones, con su nombre (StepNames).
    public class SessionStepRow
    {
        public string StepName { get; set; }
        public StepRun Run { get; set; }
    }

    // Una sesión en la descarga de sesiones del Resumen (AC-22).
    public class SessionExportRow
    {
        public DashboardSession Session { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }

        // Null si sigue en curso.
        public TimeSpan? Duration { get; set; }
        public int FailedAttempts { get; set; }

        // Null si no se muestra (DashboardService.VisibleScore).
        public decimal? Score { get; set; }

        // En orden de la guía.
        public IList<SessionStepRow> Steps { get; set; }
    }

    // Descarga de sesiones del Resumen (AC-22): las hojas Sesiones e Intentos por paso.
    public class SessionExport
    {
        public SummaryFilter Applied { get; set; }
        public IList<DashboardGuide> Guides { get; set; }

        // De la más reciente a la más vieja.
        public IList<SessionExportRow> Rows { get; set; }
    }

    // Filtros de Seguimiento: solo la guía; ignora el período (DA-3a).
    public class FollowUpFilter
    {
        // null = todas las guías.
        public int? GuideId { get; set; }
    }

    // Un operador que requiere atención en una guía, con todas las reglas que cumple.
    public class FollowUpRow
    {
        public int UserId { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }
        public int TrainingGuideId { get; set; }
        public ReadinessState State { get; set; }

        // Inicio de su última sesión de la guía.
        public DateTime? LastActivity { get; set; }

        public IList<FollowUpReason> Reasons { get; set; }
    }

    // Seguimiento (spec 018, AC-19 y AC-20).
    public class FollowUpReport
    {
        public FollowUpFilter Applied { get; set; }
        public IList<DashboardGuide> Guides { get; set; }

        // Por guía (en el orden de Guides) y después por nombre.
        public IList<FollowUpRow> Rows { get; set; }
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
            => FindOperators(userId, filter, paged: true);

        // La misma lista con los mismos filtros, sin paginar (el Excel de Operadores, DA-5): una
        // sola "página" con todas las filas.
        public OperatorPage ExportOperators(int userId, OperatorFilter filter)
            => FindOperators(userId, filter, paged: false);

        private OperatorPage FindOperators(int userId, OperatorFilter filter, bool paged)
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

            if (!paged)
                return new OperatorPage
                {
                    Applied   = new OperatorFilter { Text = text, State = state, GuideId = guideId, Page = 1 },
                    Guides    = guides,
                    Rows      = rows,
                    Total     = rows.Count,
                    Page      = 1,
                    PageCount = 1
                };

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
                        FailedAttempts = runs[s.SessionId].Sum(r => r.FailedAttempts),
                        Score          = VisibleScore(s)
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
                    .ToList(),
                // Las guías que entrenó; los intentos fallidos y el puntaje, como en las sesiones.
                Curves = guides
                    .Where(g => sessions.Any(s => s.TrainingGuideId == g.Id))
                    .Select(g => LearningCurve.ForGuide(g.Id, sessions,
                        s => runs[s.SessionId].Sum(r => r.FailedAttempts), VisibleScore))
                    .ToList()
            };
        }

        // Pasos (AC-17, AC-18): el ranking de pasos por % de sesiones con fallo del período y el
        // modo, de una guía o, con todas, un resumen con el mismo paso de cada guía junto. Todo
        // sale de los intentos con StepRuns (D-146), la misma regla del detalle. Una guía que no
        // está publicada vale "todas"; un período o un modo fuera de las opciones, el de inicio.
        public StepReport GetSteps(int userId, StepFilter filter)
        {
            RequireReports(userId);
            filter = filter ?? new StepFilter();

            var guides = _dashboard.GetGuides();
            int? guideId = PublishedOrAll(guides, filter.GuideId);
            var period = ValidPeriod(filter.Period);
            var mode   = StepModes.Contains(filter.Mode) ? filter.Mode : SessionMode.Assessment;
            var ranking = RankSteps(guides, guideId, mode, ReportPeriods.FromUtc(period, DateTime.UtcNow));

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

        // El ranking de Pasos con los filtros ya validados (lo usan Pasos y el top 5 del Resumen,
        // con las mismas guías y el mismo período).
        private IList<StepStats> RankSteps(IList<DashboardGuide> guides, int? guideId, SessionMode mode, DateTime? fromUtc)
        {
            var data     = _dashboard.GetStepAttempts(mode, fromUtc, guideId);
            var guideIds = new HashSet<int>(guides.Select(g => g.Id));
            var attempts = data.Attempts.ToLookup(a => a.SessionId);
            // Solo las sesiones de las guías leídas: una guía publicada entre las dos lecturas no
            // tendría nombre.
            var runs = data.Sessions
                .Where(s => guideIds.Contains(s.TrainingGuideId))
                .Select(s => StepRuns.ForSession(s, attempts[s.SessionId]))
                .ToList();

            var names = StepNameMap(_dashboard.GetGuideSteps());
            Func<int, int, string> nameOf = (stepId, order) => names.TryGetValue(stepId, out var name) ? name : "Paso " + order;

            // Con una guía, sus pasos; con todas, el resumen por nombre de paso (usuario,
            // 2026-10-23).
            if (!guideId.HasValue)
                return StepStatistics.RankByName(runs, r => nameOf(r.StepId, r.StepOrder));

            var ranking = StepStatistics.Rank(runs);
            foreach (var s in ranking) s.StepName = nameOf(s.StepId, s.StepOrder);
            return ranking;
        }

        public const int TopStepCount = 5;

        // El Resumen (AC-9 a AC-12): los estados con todo el historial (DA-1); las tasas, el
        // puntaje, la actividad y el top 5 del período. Con una guía, todo de esa guía.
        public SummaryReport GetSummary(int userId, SummaryFilter filter)
        {
            RequireReports(userId);
            filter = filter ?? new SummaryFilter();

            var guides = _dashboard.GetGuides();
            int? guideId = PublishedOrAll(guides, filter.GuideId);
            var period = ValidPeriod(filter.Period);
            var now  = DateTime.UtcNow;
            var from = ReportPeriods.FromUtc(period, now);

            var shown    = guides.Where(g => !guideId.HasValue || g.Id == guideId.Value).ToList();
            var shownIds = new HashSet<int>(shown.Select(g => g.Id));
            var sessions = _dashboard.GetSessions(null, false).Where(s => shownIds.Contains(s.TrainingGuideId)).ToList();

            return new SummaryReport
            {
                Applied        = new SummaryFilter { Period = period, GuideId = guideId },
                Guides         = guides,
                GuideSummaries = shown.Select(g => SummaryRules.ForGuide(g.Id, sessions, from)).ToList(),
                Weeks          = SummaryRules.Weeks(sessions, from, now),
                // El principio de Pasos en Assessment con la misma guía y el mismo período, así sus
                // números coinciden al seguir el enlace (D-147). Solo pasos con algún fallo: un
                // 0 % no es un paso con fallos.
                TopSteps       = RankSteps(guides, guideId, SessionMode.Assessment, from)
                    .Where(s => s.SessionsWithFailure > 0)
                    .Take(TopStepCount)
                    .ToList()
            };
        }

        // Seguimiento (AC-19, AC-20): los operadores que cumplen S-1, S-2 o S-3 en una guía, sin
        // período (DA-3a); quien aprobó la guía no aparece en ella. S-3 sale de los pasos de todos
        // sus Assessment (StepRuns, la misma regla que los pasos a reforzar).
        public FollowUpReport GetFollowUp(int userId, FollowUpFilter filter)
        {
            RequireReports(userId);
            filter = filter ?? new FollowUpFilter();

            var guides  = _dashboard.GetGuides();
            int? guideId = PublishedOrAll(guides, filter.GuideId);
            var shown   = guides.Where(g => !guideId.HasValue || g.Id == guideId.Value).ToList();
            var shownIds = new HashSet<int>(shown.Select(g => g.Id));
            var now     = DateTime.UtcNow;

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
            var assessments = _dashboard.GetStepAttempts(SessionMode.Assessment, null, guideId);
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

            var order = shown.Select((g, i) => new { g.Id, i }).ToDictionary(x => x.Id, x => x.i);
            return new FollowUpReport
            {
                Applied = new FollowUpFilter { GuideId = guideId },
                Guides  = guides,
                Rows    = rows.OrderBy(r => order[r.TrainingGuideId]).ThenBy(r => r.Name, StringComparer.CurrentCulture).ToList()
            };
        }

        // Las sesiones del Resumen (AC-22), con los mismos filtros de período y guía: cada sesión
        // con su duración y sus intentos fallidos, y cada paso con su resultado (StepRuns, la
        // misma regla del detalle y de Pasos). Todos los modos.
        public SessionExport GetSessionExport(int userId, SummaryFilter filter)
        {
            RequireReports(userId);
            filter = filter ?? new SummaryFilter();

            var guides = _dashboard.GetGuides();
            int? guideId = PublishedOrAll(guides, filter.GuideId);
            var period = ValidPeriod(filter.Period);
            var from = ReportPeriods.FromUtc(period, DateTime.UtcNow);
            var guideIds = new HashSet<int>(guides.Select(g => g.Id));

            // Una lectura de intentos (todos los modos) y una de sesiones, las dos con el período y
            // la guía (E7). Las sesiones salen de GetSessions: traen el puntaje y el cierre, y
            // también las que no tienen intentos.
            var bySession = _dashboard.GetStepAttempts(null, from, guideId).Attempts.ToLookup(a => a.SessionId);
            var all = _dashboard.GetSessions(null, false, from, guideId)
                .Where(s => guideIds.Contains(s.TrainingGuideId))
                .ToList();

            var operators = _dashboard.GetOperators(string.Empty).ToDictionary(o => o.UserId);
            var names = StepNameMap(_dashboard.GetGuideSteps());

            return new SessionExport
            {
                Applied = new SummaryFilter { Period = period, GuideId = guideId },
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

        // El puntaje de una sesión se muestra si está completada y su modo lo muestra (D-90);
        // si no, null ("—" en pantalla, vacío en el Excel).
        private static decimal? VisibleScore(DashboardSession session)
            => session.Status == SessionStatus.Completed && ModePolicy.For(session.Mode).ShowsScore
                ? session.FinalScore : (decimal?)null;

        // El nombre de cada paso por su id, con su variante dentro de su guía (StepNames).
        private static Dictionary<int, string> StepNameMap(IList<TrainingStep> steps)
        {
            var byGuide = steps.ToLookup(st => st.TrainingGuideId);
            return steps.ToDictionary(st => st.Id, st => StepNames.Display(byGuide[st.TrainingGuideId].ToList(), st));
        }

        private static ReportPeriod ValidPeriod(ReportPeriod period)
            => Enum.IsDefined(typeof(ReportPeriod), period) ? period : ReportPeriod.Last30Days;

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
