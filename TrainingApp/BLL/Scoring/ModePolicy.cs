using System;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Scoring
{
    // Botón de otro paso: se ignora, es intento fallido del paso actual o muestra un aviso.
    public enum OutOfSequenceHandling { Ignore, RecordAttempt, Notice }

    // Botones del MES sin lógica (Work Instructions, Reports…).
    public enum NoLogicButtonHandling { Disabled, RecordAttempt, Inert }

    // Mensaje de un intento fallido.
    public enum FailureFeedbackKind { Hint, RemainingAttempts, Incorrect }

    // Qué muestra el resumen al cerrar la sesión.
    public enum SummaryKind { Score, ScoreAndResult, AttemptsPerStep }

    // Mapa modo → capacidades (spec 012, P-12), en un solo lugar como AccessPolicy con los
    // roles: la página, PodService y ScoreCalculator preguntan por la capacidad, nunca por el
    // SessionMode. Un modo nuevo es una instancia más, no ramas nuevas.
    public sealed class ModePolicy
    {
        // Último intento con puntaje cuando el modo penaliza; el fallo siguiente fuerza el avance.
        public const int MaxScoredAttempts = 3;

        public static readonly ModePolicy Guided = new ModePolicy(SessionMode.Guided,
            showsGuide: true, enablesAllButtons: false,
            outOfSequence: OutOfSequenceHandling.Ignore, noLogicButtons: NoLogicButtonHandling.Disabled,
            penalizesRetries: false, forcesAdvance: false, failureFeedback: FailureFeedbackKind.Hint,
            hintOnDemand: false, allowsSkip: false, showsScore: true, summary: SummaryKind.Score);

        public static readonly ModePolicy Assessment = new ModePolicy(SessionMode.Assessment,
            showsGuide: false, enablesAllButtons: true,
            outOfSequence: OutOfSequenceHandling.RecordAttempt, noLogicButtons: NoLogicButtonHandling.RecordAttempt,
            penalizesRetries: true, forcesAdvance: true, failureFeedback: FailureFeedbackKind.RemainingAttempts,
            hintOnDemand: false, allowsSkip: false, showsScore: true, summary: SummaryKind.ScoreAndResult);

        public static readonly ModePolicy FreePractice = new ModePolicy(SessionMode.FreePractice,
            showsGuide: false, enablesAllButtons: true,
            outOfSequence: OutOfSequenceHandling.Notice, noLogicButtons: NoLogicButtonHandling.Inert,
            penalizesRetries: false, forcesAdvance: false, failureFeedback: FailureFeedbackKind.Incorrect,
            hintOnDemand: true, allowsSkip: true, showsScore: false, summary: SummaryKind.AttemptsPerStep);

        public static ModePolicy For(SessionMode mode)
        {
            switch (mode)
            {
                case SessionMode.Guided:       return Guided;
                case SessionMode.Assessment:   return Assessment;
                case SessionMode.FreePractice: return FreePractice;
                default: throw new ArgumentOutOfRangeException(nameof(mode), mode, "Modo de sesión desconocido.");
            }
        }

        private ModePolicy(SessionMode mode, bool showsGuide, bool enablesAllButtons,
            OutOfSequenceHandling outOfSequence, NoLogicButtonHandling noLogicButtons,
            bool penalizesRetries, bool forcesAdvance, FailureFeedbackKind failureFeedback,
            bool hintOnDemand, bool allowsSkip, bool showsScore, SummaryKind summary)
        {
            ShowsScore        = showsScore;
            Mode              = mode;
            ShowsGuide        = showsGuide;
            EnablesAllButtons = enablesAllButtons;
            OutOfSequence     = outOfSequence;
            NoLogicButtons    = noLogicButtons;
            PenalizesRetries  = penalizesRetries;
            ForcesAdvance     = forcesAdvance;
            FailureFeedback   = failureFeedback;
            HintOnDemand      = hintOnDemand;
            AllowsSkip        = allowsSkip;
            Summary           = summary;
        }

        public SessionMode Mode { get; }

        // Guía visual y lo que solo ella recibe: el código del hallazgo en NC Selection y el
        // panel del hallazgo revelado por la guía.
        public bool ShowsGuide { get; }
        public bool EnablesAllButtons { get; }
        public OutOfSequenceHandling OutOfSequence { get; }
        public NoLogicButtonHandling NoLogicButtons { get; }
        public bool PenalizesRetries { get; }
        public bool ForcesAdvance { get; }
        public FailureFeedbackKind FailureFeedback { get; }
        public bool HintOnDemand { get; }
        public bool AllowsSkip { get; }
        public SummaryKind Summary { get; }

        // Puntaje en el header y en el resumen (D-90): no en Práctica libre.
        public bool ShowsScore { get; }

        // Factor del peso del paso según el intento en que acertó.
        public decimal PenaltyFactor(int attemptNumber)
        {
            if (!PenalizesRetries) return 1.00m;

            switch (attemptNumber)
            {
                case 1: return 1.00m;
                case 2: return 0.80m;
                case 3: return 0.60m;
                default: return 0.00m;
            }
        }

        // El fallo siguiente al último intento con puntaje resuelve el paso (avance forzado).
        public bool ForcesAdvanceAt(int attemptNumber)
            => ForcesAdvance && attemptNumber > MaxScoredAttempts;
    }
}
