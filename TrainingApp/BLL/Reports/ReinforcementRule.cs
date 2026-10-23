using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Reports
{
    // Un paso a reforzar de un operador en una guía (spec 018, AC-14).
    public class ReinforcementStep
    {
        public int StepId { get; set; }
        public int StepOrder { get; set; }
        public string StepName { get; set; }

        // En sus Assessment de la guía: veces que el paso terminó sin acierto e intentos
        // fallidos en total.
        public int TimesWithoutSuccess { get; set; }
        public int FailedAttempts { get; set; }

        // El motivo más repetido entre esos intentos fallidos; null si no hay motivo registrado.
        public AttemptReason? MainReason { get; set; }
    }

    // Pasos a reforzar (spec 018, plan E2): en los Assessment de una guía, los pasos que
    // terminaron sin acierto o con MinFailedAttempts o más intentos fallidos en una sesión. Se
    // ordenan por veces sin acierto y después por intentos fallidos, hasta MaxPerGuide.
    public static class ReinforcementRule
    {
        public const int MinFailedAttempts = 2;
        public const int MaxPerGuide = 5;

        // runs: los pasos de cada Assessment del operador en la guía (StepRuns.ForSession).
        public static IList<ReinforcementStep> ForGuide(IEnumerable<IList<StepRun>> assessmentRuns)
        {
            var all = (assessmentRuns ?? Enumerable.Empty<IList<StepRun>>()).SelectMany(r => r).ToList();
            return all
                .GroupBy(r => new { r.StepId, r.StepOrder })
                .Where(g => g.Any(r => r.EndedWithoutSuccess || r.FailedAttempts >= MinFailedAttempts))
                .Select(g => new ReinforcementStep
                {
                    StepId              = g.Key.StepId,
                    StepOrder           = g.Key.StepOrder,
                    TimesWithoutSuccess = g.Count(r => r.EndedWithoutSuccess),
                    FailedAttempts      = g.Sum(r => r.FailedAttempts),
                    MainReason          = g.SelectMany(r => r.FailureReasons)
                        .GroupBy(reason => reason)
                        .OrderByDescending(x => x.Count()).ThenBy(x => x.Key)
                        .Select(x => (AttemptReason?)x.Key)
                        .FirstOrDefault()
                })
                .OrderByDescending(s => s.TimesWithoutSuccess)
                .ThenByDescending(s => s.FailedAttempts)
                .ThenBy(s => s.StepOrder)
                .Take(MaxPerGuide)
                .ToList();
        }
    }
}
