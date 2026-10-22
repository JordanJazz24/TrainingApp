using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Auth;
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
            int? guideId = filter.GuideId.HasValue && guides.Any(g => g.Id == filter.GuideId.Value)
                ? filter.GuideId : null;
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
