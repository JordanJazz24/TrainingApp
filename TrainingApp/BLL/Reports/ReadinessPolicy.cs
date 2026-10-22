using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Reports
{
    // Estado de un operador en una guía (spec 018, D-136). Sin iniciar es solo el estado general
    // de quien no tiene sesiones en ninguna guía publicada.
    public enum ReadinessState
    {
        NotStarted,
        InTraining,
        Failed,
        Passed
    }

    // Estado y puntajes de un operador en una guía.
    public class GuideReadiness
    {
        public int TrainingGuideId { get; set; }
        public ReadinessState State { get; set; }

        // De los Assessment que cuentan (CountsAsAssessment); null si no hay ninguno.
        public decimal? BestScore { get; set; }
        public decimal? LastScore { get; set; }

        // Inicio de su sesión más reciente de la guía, de cualquier modo y estado.
        public DateTime? LastActivity { get; set; }
    }

    // Único lugar con la regla de "listo para producción" (spec 018, D-136), como AccessPolicy
    // con los roles: el dashboard, el Excel y la ficha le preguntan y nunca repiten la regla
    // (AC-8). Usa todo el historial, sin período (DA-1).
    //   Aprobado: algún Assessment completado de la guía con FinalScore ≥ PassingScore (0.70).
    //   Reprobado: Assessment completados y ninguno aprobado.
    //   En entrenamiento: sesiones de la guía sin Assessment completados (solo Guided, Práctica
    //   libre, o Assessment en curso o abandonado).
    public static class ReadinessPolicy
    {
        // Un Assessment que entra en el estado, la tasa y el puntaje promedio: solo completado
        // (los abandonados o en curso no cuentan como reprobados, AC-6).
        public static bool CountsAsAssessment(DashboardSession session)
            => session.Mode == SessionMode.Assessment && session.Status == SessionStatus.Completed;

        public static bool IsPassed(DashboardSession session)
            => CountsAsAssessment(session) && ScoreCalculator.IsPassed(session.FinalScore);

        // Estado de un operador en una guía, con las sesiones de esa guía (de cualquier modo y
        // estado). Sin sesiones: NotStarted.
        public static GuideReadiness ForGuide(int trainingGuideId, IEnumerable<DashboardSession> sessions)
        {
            var mine = (sessions ?? Enumerable.Empty<DashboardSession>())
                .Where(s => s.TrainingGuideId == trainingGuideId)
                .ToList();
            var readiness = new GuideReadiness { TrainingGuideId = trainingGuideId };
            if (mine.Count == 0) return readiness;

            readiness.LastActivity = mine.Max(s => s.StartedAt);

            var assessments = mine.Where(CountsAsAssessment)
                .OrderBy(s => s.CompletedAt ?? s.StartedAt)
                .ToList();
            if (assessments.Count == 0)
            {
                readiness.State = ReadinessState.InTraining;
                return readiness;
            }

            readiness.BestScore = assessments.Max(s => s.FinalScore);
            readiness.LastScore = assessments[assessments.Count - 1].FinalScore;
            readiness.State = assessments.Any(IsPassed) ? ReadinessState.Passed : ReadinessState.Failed;
            return readiness;
        }
    }
}
