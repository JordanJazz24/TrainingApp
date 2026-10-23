using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Reports
{
    // Las reglas de Seguimiento (spec 018 → "Lista de seguimiento").
    public enum FollowUpRule
    {
        // S-1: reprobó 2 o más Assessment de la guía sin aprobar ninguno.
        RepeatedFailures = 1,

        // S-2: no aprobó la guía y lleva 14 días sin sesiones de esa guía, aunque practique otras.
        Inactive = 2,

        // S-3: el mismo paso de la guía terminó sin acierto en 2 o más Assessment.
        StuckStep = 3
    }

    // Un paso de la guía que terminó sin acierto en varios Assessment (S-3).
    public class StuckStep
    {
        public int StepId { get; set; }
        public int StepOrder { get; set; }
        public string StepName { get; set; }

        // Assessment en los que terminó sin acierto.
        public int Times { get; set; }
    }

    // Una regla que cumple un operador en una guía, con su dato.
    public class FollowUpReason
    {
        public FollowUpRule Rule { get; set; }

        // S-1: Assessment completados y reprobados.
        public int FailedAssessments { get; set; }

        // S-2: días completos desde el inicio de su última sesión de la guía.
        public int DaysInactive { get; set; }

        // S-3: los pasos, del que más veces terminó sin acierto al que menos. Sin nombre: lo pone
        // el servicio.
        public IList<StuckStep> Steps { get; set; } = new List<StuckStep>();
    }

    // Seguimiento (spec 018, D-141): reglas S-1, S-2 y S-3 por operador y guía, sin período
    // (DA-3a). Quien aprobó la guía sale con todas sus reglas. Funciones puras: lo que es
    // aprobado lo decide ReadinessPolicy y lo que es "sin acierto", StepRuns.
    public static class FollowUpRules
    {
        public const int MinFailedAssessments = 2;          // S-1
        public const int InactiveDays = 14;                 // S-2
        public const int MinAssessmentsWithoutSuccess = 2;  // S-3

        // Las reglas que cumple un operador en una guía; vacío si no requiere atención.
        //   sessions: sus sesiones de la guía (de cualquier modo y estado, todo el historial);
        //   assessmentRuns: los pasos de cada uno de sus Assessment de esa guía (StepRuns.ForSession),
        //   abandonados y en curso incluidos.
        public static IList<FollowUpReason> ForGuide(int trainingGuideId, IEnumerable<DashboardSession> sessions,
            IEnumerable<IList<StepRun>> assessmentRuns, DateTime nowUtc)
        {
            var mine = (sessions ?? Enumerable.Empty<DashboardSession>())
                .Where(s => s.TrainingGuideId == trainingGuideId)
                .ToList();
            return ForGuide(ReadinessPolicy.ForGuide(trainingGuideId, mine), mine, assessmentRuns, nowUtc);
        }

        // Igual, con el estado ya calculado de esas sesiones (el servicio lo usa también para la
        // fila, así hay una sola fuente para el estado y la última actividad).
        public static IList<FollowUpReason> ForGuide(GuideReadiness readiness, IList<DashboardSession> guideSessions,
            IEnumerable<IList<StepRun>> assessmentRuns, DateTime nowUtc)
        {
            var reasons = new List<FollowUpReason>();
            // Sin sesiones no hay seguimiento; al aprobar la guía sale con todas sus reglas.
            if (!readiness.LastActivity.HasValue || readiness.State == ReadinessState.Passed) return reasons;

            int failed = guideSessions.Count(ReadinessPolicy.CountsAsAssessment);
            if (failed >= MinFailedAssessments)
                reasons.Add(new FollowUpReason { Rule = FollowUpRule.RepeatedFailures, FailedAssessments = failed });

            // Desde el inicio de su última sesión de la guía, aunque siga en curso (Edge Cases).
            int days = (int)Math.Floor((nowUtc - readiness.LastActivity.Value).TotalDays);
            if (days >= InactiveDays)
                reasons.Add(new FollowUpReason { Rule = FollowUpRule.Inactive, DaysInactive = days });

            // Por paso de la guía (StepId), no por nombre (DA-3b): cada Assessment cuenta una vez
            // por paso.
            var stuck = (assessmentRuns ?? Enumerable.Empty<IList<StepRun>>())
                .SelectMany(r => r)
                .Where(r => r.EndedWithoutSuccess)
                .GroupBy(r => r.StepId)
                .Select(g => new StuckStep { StepId = g.Key, StepOrder = g.Min(r => r.StepOrder), Times = g.Count() })
                .Where(s => s.Times >= MinAssessmentsWithoutSuccess)
                .OrderByDescending(s => s.Times).ThenBy(s => s.StepOrder)
                .ToList();
            if (stuck.Count > 0)
                reasons.Add(new FollowUpReason { Rule = FollowUpRule.StuckStep, Steps = stuck });

            return reasons;
        }
    }
}
