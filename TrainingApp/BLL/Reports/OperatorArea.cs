using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Reports
{
    // Qué tiene que entrenar un operador según su área (spec 024).
    public enum OperatorAreaStatus
    {
        // Todavía no eligió su área (D-267).
        NoArea,

        // Su área no tiene ninguna guía publicada (P-5).
        NoPublishedGuide,

        // Tiene guías publicadas en su área (hoy una, D-268).
        HasGuides
    }

    // Un área del filtro de Operadores: las que tienen alguna guía publicada (D-262).
    public class ReportArea
    {
        public int AreaId { get; set; }
        public string ProductName { get; set; }
        public string AreaName { get; set; }
    }

    // El estado de un operador separado en su área y las demás (spec 024, D-261, D-263, D-269).
    public class OperatorAreaView
    {
        public OperatorAreaStatus Status { get; set; }

        // Una por guía publicada de su área, también las Sin iniciar, en el orden de las guías.
        public IList<GuideReadiness> AreaGuides { get; set; }

        // Las guías publicadas de otras áreas en las que tiene sesiones (entrenamiento cruzado),
        // en el orden de las guías.
        public IList<GuideReadiness> OtherGuides { get; set; }
    }

    // Único lugar con la regla de "su área" del dashboard (spec 024), como ReadinessPolicy con el
    // estado de una guía: la lista, el Excel y el detalle le preguntan. Su área es siempre la
    // actual, sin historial de cambios (P-1).
    public static class OperatorArea
    {
        // guides: las publicadas, en su orden; sessions: las del operador (de las guías
        // publicadas).
        public static OperatorAreaView For(int? areaId, IList<DashboardGuide> guides,
            IEnumerable<DashboardSession> sessions)
        {
            var mine = (sessions ?? Enumerable.Empty<DashboardSession>()).ToList();
            var trained = new HashSet<int>(mine.Select(s => s.TrainingGuideId));

            var inArea = areaId.HasValue
                ? guides.Where(g => g.AreaId == areaId.Value).ToList()
                : new List<DashboardGuide>();
            var others = guides.Where(g => (!areaId.HasValue || g.AreaId != areaId.Value) && trained.Contains(g.Id));

            return new OperatorAreaView
            {
                Status      = !areaId.HasValue ? OperatorAreaStatus.NoArea
                            : inArea.Count == 0 ? OperatorAreaStatus.NoPublishedGuide
                            : OperatorAreaStatus.HasGuides,
                AreaGuides  = inArea.Select(g => ReadinessPolicy.ForGuide(g.Id, mine)).ToList(),
                OtherGuides = others.Select(g => ReadinessPolicy.ForGuide(g.Id, mine)).ToList()
            };
        }

        // Sin iniciar (D-263): tiene área, su área tiene guía y no tiene sesiones en ninguna.
        // Sin área o sin guía publicada no es Sin iniciar (D-267, P-5).
        public static bool IsNotStarted(OperatorAreaStatus status, IList<GuideReadiness> areaGuides)
            => status == OperatorAreaStatus.HasGuides
               && areaGuides.All(g => g.State == ReadinessState.NotStarted);

        // El filtro Estado (D-268, P-6): null = todos; Sin iniciar con la regla de arriba; otro
        // estado, alguna guía de su área en ese estado. Sin área o sin guía publicada solo salen
        // con todos.
        public static bool Matches(OperatorAreaStatus status, IList<GuideReadiness> areaGuides,
            ReadinessState? state)
        {
            if (!state.HasValue) return true;
            if (state.Value == ReadinessState.NotStarted) return IsNotStarted(status, areaGuides);
            return status == OperatorAreaStatus.HasGuides && areaGuides.Any(g => g.State == state.Value);
        }

        // Las áreas con alguna guía publicada, en el orden de las guías (por producto y área).
        public static IList<ReportArea> AreasOf(IList<DashboardGuide> guides)
            => guides
                .GroupBy(g => g.AreaId)
                .Select(grp => new ReportArea
                {
                    AreaId      = grp.Key,
                    ProductName = grp.First().ProductName,
                    AreaName    = grp.First().AreaName
                })
                .ToList();
    }
}
