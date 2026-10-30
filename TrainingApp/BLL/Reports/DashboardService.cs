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

        // null = todos los estados. Se evalúa en las guías de su área (spec 024, D-263).
        public ReadinessState? State { get; set; }

        // Área del operador (spec 024, D-262): null = todas; WithoutArea = los que no la eligieron
        // (y entonces AreaId no cuenta). Sin filtro de guía (D-264).
        public int? AreaId { get; set; }
        public bool WithoutArea { get; set; }

        // Desde 1.
        public int Page { get; set; } = 1;
    }

    // Un operador en la lista: su estado en las guías de su área (spec 024, D-261) y su última
    // actividad. Las guías de otras áreas no salen en la lista (D-266).
    public class OperatorRow
    {
        public int UserId { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }

        // "Producto · Área" o "Sin área" (spec 021, P-9).
        public string AreaText { get; set; }
        public OperatorAreaStatus AreaStatus { get; set; }

        // Una por guía publicada de su área (hoy una, D-268); vacía sin área o sin guía publicada.
        public IList<GuideReadiness> AreaGuides { get; set; }

        // Sin iniciar (D-263): ninguna sesión en las guías de su área (OperatorArea).
        public bool NotStarted => OperatorArea.IsNotStarted(AreaStatus, AreaGuides);

        // Inicio de su sesión más reciente en cualquier guía publicada (AC-13); null si nunca
        // entrenó.
        public DateTime? LastActivity { get; set; }

        // Días completos desde LastActivity (null sin actividad) y si lleva OperatorSummary.InactiveDays
        // o más sin practicar teniendo guía en su área y no siendo Sin iniciar (spec 022, brief 6;
        // métrica derivada). Los pone OperatorSummary.MarkActivity.
        public int? DaysSinceActivity { get; set; }
        public bool Inactive { get; set; }
    }

    public class OperatorPage
    {
        // Los filtros que se aplicaron, ya validados: la página los muestra y arma sus enlaces
        // con ellos (Page queda abajo).
        public OperatorFilter Applied { get; set; }

        // Las opciones del filtro Área: las que tienen guía publicada, por producto (D-262).
        public IList<ReportArea> Areas { get; set; }
        public IList<OperatorRow> Rows { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }       // desde 1
        public int PageCount { get; set; }

        // Operadores por estado y sin actividad reciente (spec 022, brief 6; métricas derivadas).
        public OperatorSummary Summary { get; set; }
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

        // "Producto · Área" de la guía de la sesión (spec 024, D-269).
        public string AreaText { get; set; }
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

        // "Producto · Área" o "Sin área" (spec 021).
        public string AreaText { get; set; }

        // Todas las guías publicadas: los nombres de las de abajo salen de aquí.
        public IList<DashboardGuide> Guides { get; set; }

        // Su área y las otras (spec 024, D-269; OperatorArea): las guías de su área, también Sin
        // iniciar, y las de otras áreas con sesiones (entrenamiento cruzado).
        public OperatorAreaStatus AreaStatus { get; set; }
        public IList<GuideReadiness> AreaGuides { get; set; }
        public IList<GuideReadiness> OtherGuides { get; set; }

        // De la más reciente a la más vieja, de todas las áreas.
        public IList<OperatorSessionRow> Sessions { get; set; }

        // Solo las guías con algún paso a reforzar: primero las de su área y después las otras
        // (D-269), cada grupo en el orden de Guides.
        public IList<GuideReinforcement> Reinforcement { get; set; }

        // La curva de aprendizaje de cada guía que entrenó (AC-15), con el mismo orden.
        public IList<GuideCurve> Curves { get; set; }
    }

    // Filtros de Pasos (spec 018 → Filtros; el período, spec 023). Los valida el servicio, como
    // OperatorFilter.
    public class StepFilter
    {
        public ReportPeriod Period { get; set; } = ReportPeriods.Default;

        // Inicio y Fin (días de la planta), solo con Personalizado.
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

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

        // El período aplicado, su anterior y si se pidió uno inválido (el aviso).
        public ReportRange Range { get; set; }

        public IList<DashboardGuide> Guides { get; set; }

        // Con una guía, su ranking; con todas, un solo resumen (TrainingGuideId null). Vacío si
        // no hay intentos en el período.
        public IList<GuideStepRanking> Rankings { get; set; }

        // Los saltos solo existen en el modo que permite saltar (Práctica libre, AC-18).
        public bool ShowsSkips { get; set; }
    }

    // Filtros del Resumen (spec 018 → Filtros; el período, spec 023). Los valida el servicio.
    public class SummaryFilter
    {
        public ReportPeriod Period { get; set; } = ReportPeriods.Default;

        // Inicio y Fin (días de la planta), solo con Personalizado.
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // null = todas las guías.
        public int? GuideId { get; set; }
    }

    // Resumen (spec 018, AC-9 a AC-12), el inicio del administrador.
    public class SummaryReport
    {
        // Los filtros que se aplicaron, ya validados.
        public SummaryFilter Applied { get; set; }

        // El período aplicado, su anterior y si se pidió uno inválido (el aviso).
        public ReportRange Range { get; set; }

        // Todas las guías publicadas, para el filtro.
        public IList<DashboardGuide> Guides { get; set; }

        // Una por guía mostrada (todas o la del filtro), en el orden de Guides.
        public IList<GuideSummary> GuideSummaries { get; set; }

        // Sesiones del período por día, semana o mes y por modo.
        public ActivityChart Activity { get; set; }

        // Sesiones del período, de todos los modos (P-11), y su cambio en %; null con Todo.
        public int Sessions { get; set; }
        public ReportChange SessionsChange { get; set; }

        // Top 5 de pasos por % de sesiones con fallo en Assessment del período: los primeros de
        // Pasos con los mismos filtros (D-147), con su cambio.
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
        public ReportRange Range { get; set; }
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

        // Llamar primero / Después y casos por regla (spec 022, brief 5; métricas derivadas).
        public FollowUpSummary Summary { get; set; }
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
        private readonly Func<DateTime> _utcNow;

        public DashboardService()
            : this(new DashboardRepository(), new UserRepository())
        {
        }

        // Para los tests: datos en memoria y, si hace falta, un "ahora" fijo (los períodos
        // dependen del día).
        public DashboardService(IDashboardRepository dashboard, IUserRepository users, Func<DateTime> utcNow = null)
        {
            _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
            _users     = users ?? throw new ArgumentNullException(nameof(users));
            _utcNow    = utcNow ?? (() => DateTime.UtcNow);
        }

        // Las guías publicadas, para los filtros de las páginas.
        public IList<DashboardGuide> GetGuides(int userId)
        {
            RequireReports(userId);
            return _dashboard.GetGuides();
        }

        // Una página de Operadores (AC-7, AC-13; spec 024), por nombre. Los filtros:
        //   · área: los operadores de esa área, o los que no tienen (D-262, D-267);
        //   · estado: en las guías de su área (OperatorArea.Matches; Sin iniciar = ninguna sesión
        //     en ellas, D-263). Sin área o sin guía publicada solo salen con todos.
        // Un área sin guía publicada o un estado fuera del enum valen "todas" o "todos" (P-4);
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
            var byId     = guides.ToDictionary(g => g.Id);
            var areaText = guides.ToDictionary(g => g.Id, g => AreaLabel.Of(g.ProductName, g.AreaName));

            // Su área primero y después las otras con sesiones (spec 024, D-269).
            var view  = OperatorArea.For(user.AreaId, guides, sessions);
            var shown = view.AreaGuides.Concat(view.OtherGuides).Select(r => byId[r.TrainingGuideId]).ToList();

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
                Sessions    = sessions
                    .OrderByDescending(s => s.StartedAt)
                    .Select(s => new OperatorSessionRow
                    {
                        Session        = s,
                        Duration       = StepRuns.SessionDuration(s),
                        FailedAttempts = runs[s.SessionId].Sum(r => r.FailedAttempts),
                        Score          = VisibleScore(s),
                        AreaText       = areaText[s.TrainingGuideId]
                    })
                    .ToList(),
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
                    .ToList()
            };
        }

        // Pasos (AC-17, AC-18): el ranking de pasos por % de sesiones con fallo del período y el
        // modo, de una guía o, con todas, un resumen con el mismo paso de cada guía junto, cada
        // paso con su cambio contra el período anterior (spec 023). Todo sale de los intentos con
        // StepRuns (D-146), la misma regla del detalle. Una guía que no está publicada vale
        // "todas"; un modo fuera de las opciones, el de inicio; un período inválido, Este mes con
        // el aviso (ReportPeriods.Resolve).
        public StepReport GetSteps(int userId, StepFilter filter)
        {
            RequireReports(userId);
            filter = filter ?? new StepFilter();

            var guides = _dashboard.GetGuides();
            int? guideId = PublishedOrAll(guides, filter.GuideId);
            var range = ReportPeriods.Resolve(filter.Period, filter.From, filter.To, _utcNow());
            var mode  = StepModes.Contains(filter.Mode) ? filter.Mode : SessionMode.Assessment;
            var ranking = RankSteps(guides, guideId, mode, range);

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
        private IList<StepStats> RankSteps(IList<DashboardGuide> guides, int? guideId, SessionMode mode, ReportRange range)
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

        public const int TopStepCount = 5;

        // El Resumen (AC-9 a AC-12): los estados con todo el historial (DA-1); las tasas, el
        // puntaje, la actividad y el top 5 del período. Con una guía, todo de esa guía. La tasa de
        // aprobación, las sesiones y el top 5, con su cambio contra el período anterior (spec 023,
        // D-212), de las mismas sesiones en memoria.
        public SummaryReport GetSummary(int userId, SummaryFilter filter)
        {
            RequireReports(userId);
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
                Activity       = SummaryRules.Activity(sessions, range, now),
                Sessions       = inPeriod,
                SessionsChange = range.Compares
                    ? ReportChange.Percent(inPeriod, sessions.Count(s => range.InPrevious(s.StartedAt)), higherIsBetter: true)
                    : null,
                // El principio de Pasos en Assessment con la misma guía y el mismo período, así sus
                // números coinciden al seguir el enlace (D-147). Solo pasos con algún fallo: un
                // 0 % no es un paso con fallos.
                TopSteps       = RankSteps(guides, guideId, SessionMode.Assessment, range)
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

        // Las sesiones del Resumen (AC-22), con los mismos filtros de período y guía: cada sesión
        // con su duración y sus intentos fallidos, y cada paso con su resultado (StepRuns, la
        // misma regla del detalle y de Pasos). Todos los modos.
        public SessionExport GetSessionExport(int userId, SummaryFilter filter)
        {
            RequireReports(userId);
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

        // Inicio y Fin del filtro aplicado, solo con Personalizado: los atajos se recalculan cada
        // día.
        private static DateTime? CustomFrom(ReportRange range)
            => range.Period == ReportPeriod.Custom ? range.FromLocal : null;

        private static DateTime? CustomTo(ReportRange range)
            => range.Period == ReportPeriod.Custom ? range.ToLocal : null;

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

        private void RequireReports(int userId)
        {
            var user = _users.GetById(userId);
            if (user == null || !AccessPolicy.CanViewReports(user.Role))
                throw new UnauthorizedAccessException("El usuario no puede ver los reportes.");
        }
    }
}
