using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Reports
{
    // Piezas que comparten los reportes del dashboard (DashboardService y sus consultas). Sin
    // estado y sin acceso a datos.
    internal static class DashboardQueryKit
    {
        // El puntaje de una sesión se muestra si está completada y su modo lo muestra (D-90);
        // si no, null ("—" en pantalla, vacío en el Excel).
        internal static decimal? VisibleScore(DashboardSession session)
            => session.Status == SessionStatus.Completed && ModePolicy.For(session.Mode).ShowsScore
                ? session.FinalScore : (decimal?)null;

        // El nombre de cada paso por su id, con su variante dentro de su guía (StepNames).
        internal static Dictionary<int, string> StepNameMap(IList<TrainingStep> steps)
        {
            var byGuide = steps.ToLookup(st => st.TrainingGuideId);
            return steps.ToDictionary(st => st.Id, st => StepNames.Display(byGuide[st.TrainingGuideId].ToList(), st));
        }

        // Inicio y Fin del filtro aplicado, solo con Personalizado: los atajos se recalculan cada
        // día.
        internal static DateTime? CustomFrom(ReportRange range)
            => range.Period == ReportPeriod.Custom ? range.FromLocal : null;

        internal static DateTime? CustomTo(ReportRange range)
            => range.Period == ReportPeriod.Custom ? range.ToLocal : null;

        // El filtro de guía: una que no está publicada vale "todas" (null).
        internal static int? PublishedOrAll(IList<DashboardGuide> guides, int? guideId)
            => guideId.HasValue && guides.Any(g => g.Id == guideId.Value) ? guideId : null;

        internal static string StepName(IList<TrainingStep> guideSteps, int stepId, int stepOrder)
        {
            var step = guideSteps.FirstOrDefault(s => s.Id == stepId);
            return step != null ? StepNames.Display(guideSteps, step) : "Paso " + stepOrder;
        }
    }
}
