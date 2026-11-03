using System;
using System.Collections.Generic;
using TrainingApp.BLL.Auth;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Reports
{
    // Dashboard de Training (spec 018). Lee lo que los SPs agregan y aplica las reglas
    // (ReadinessPolicy). Cada método recibe el userId del usuario autenticado y verifica
    // CanViewReports sin confiar en la página: si no la tiene, es una excepción.
    public class DashboardService
    {
        public const int PageSize = OperatorsQuery.PageSize;
        public const int TopStepCount = SummaryQuery.TopStepCount;

        private readonly IDashboardRepository _dashboard;
        private readonly IUserRepository _users;
        private readonly OperatorsQuery _operators;
        private readonly StepsQuery _steps;
        private readonly SummaryQuery _summary;
        private readonly FollowUpQuery _followUp;
        private readonly ExportQuery _export;

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
            var now    = utcNow ?? (() => DateTime.UtcNow);

            _operators = new OperatorsQuery(_dashboard, _users, now);
            _steps     = new StepsQuery(_dashboard, now);
            _summary   = new SummaryQuery(_dashboard, _steps, now);
            _followUp  = new FollowUpQuery(_dashboard, now);
            _export    = new ExportQuery(_dashboard, now);
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
        {
            RequireReports(userId);
            return _operators.Find(filter, paged: true);
        }

        // La misma lista con los mismos filtros, sin paginar (el Excel de Operadores, DA-5): una
        // sola "página" con todas las filas.
        public OperatorPage ExportOperators(int userId, OperatorFilter filter)
        {
            RequireReports(userId);
            return _operators.Find(filter, paged: false);
        }

        // El detalle de un operador (AC-14); null si el id no existe o no es de un operador
        // (DA-9: un administrador no tiene detalle). Los intentos fallidos de cada sesión y los
        // pasos a reforzar salen de sus intentos (StepRuns), con la misma regla que Pasos.
        public OperatorDetail GetOperator(int userId, int operatorId)
        {
            RequireReports(userId);
            return _operators.Get(operatorId);
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
            return _steps.Get(filter);
        }

        // El Resumen (AC-9 a AC-12): los estados con todo el historial (DA-1); las tasas, el
        // puntaje, la actividad y el top 5 del período. Con una guía, todo de esa guía. La tasa de
        // aprobación, las sesiones y el top 5, con su cambio contra el período anterior (spec 023,
        // D-212), de las mismas sesiones en memoria.
        public SummaryReport GetSummary(int userId, SummaryFilter filter)
        {
            RequireReports(userId);
            return _summary.Get(filter);
        }

        // Seguimiento (AC-19, AC-20): los operadores que cumplen S-1, S-2 o S-3 en una guía, sin
        // período (DA-3a); quien aprobó la guía no aparece en ella. S-3 sale de los pasos de todos
        // sus Assessment (StepRuns, la misma regla que los pasos a reforzar).
        public FollowUpReport GetFollowUp(int userId, FollowUpFilter filter)
        {
            RequireReports(userId);
            return _followUp.Get(filter);
        }

        // Las sesiones del Resumen (AC-22), con los mismos filtros de período y guía: cada sesión
        // con su duración y sus intentos fallidos, y cada paso con su resultado (StepRuns, la
        // misma regla del detalle y de Pasos). Todos los modos.
        public SessionExport GetSessionExport(int userId, SummaryFilter filter)
        {
            RequireReports(userId);
            return _export.Get(filter);
        }

        private void RequireReports(int userId)
        {
            var user = _users.GetById(userId);
            if (user == null || !AccessPolicy.CanViewReports(user.Role))
                throw new UnauthorizedAccessException("El usuario no puede ver los reportes.");
        }
    }
}
