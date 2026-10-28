using System;
using System.Collections.Generic;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    // Lecturas del dashboard (spec 018): los SPs agregan sin reglas (D-139). Interfaz para que
    // DashboardService se pruebe con datos en memoria; la implementa DashboardRepository.
    public interface IDashboardRepository
    {
        // Las guías publicadas.
        IList<DashboardGuide> GetGuides();

        // Los operadores cuyo nombre o Code1 contiene text (vacío = todos), por nombre. Sin
        // paginar: la BLL filtra por estado y después pagina.
        IList<DashboardOperator> GetOperators(string text);

        // Las sesiones de las guías publicadas de los operadores (de uno solo con userId; desde
        // fromUtc, antes de toUtc y de guideId si vienen). Con includeFailedAttempts, cada una con
        // su conteo de intentos fallidos; si no, null.
        IList<DashboardSession> GetSessions(int? userId, bool includeFailedAttempts,
            DateTime? fromUtc = null, DateTime? toUtc = null, int? guideId = null);

        // Los pasos de las guías publicadas, en orden.
        IList<TrainingStep> GetGuideSteps();

        // Los intentos de las sesiones de un operador en las guías publicadas, en orden.
        IList<DashboardAttempt> GetOperatorAttempts(int userId);

        // Los intentos de las sesiones de un modo (null = todos) que empezaron desde fromUtc y
        // antes de toUtc (null = sin límite), en las guías publicadas (o en guideId), con sus
        // sesiones.
        DashboardAttemptSet GetStepAttempts(SessionMode? mode, DateTime? fromUtc, DateTime? toUtc, int? guideId);
    }
}
