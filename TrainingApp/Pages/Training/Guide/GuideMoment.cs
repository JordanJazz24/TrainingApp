using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public enum GuideMomentKind
    {
        Load,             // GET, F5 o un postback que no cambió el paso: entra por lo pendiente
        Opened,           // se abrió el control (o el modal) del paso
        Incorrect,        // intento fallido: el control queda abierto en la decisión
        StepResolved,     // el paso se resolvió y su confirmación sigue en pantalla
        SessionCompleted  // la sesión terminó: globo final
    }

    // Qué dejó la página en pantalla en este request (spec 010, P-2). Lo anota GuidedTest en
    // los handlers y GuideScriptBuilder lo usa para elegir el subpaso de entrada.
    public sealed class GuideMoment
    {
        private GuideMoment(GuideMomentKind kind, TrainingStep step, string detail = null)
        {
            Kind   = kind;
            Step   = step;
            Detail = detail;
        }

        public GuideMomentKind Kind { get; }

        // Paso al que se refiere el momento; null en Load.
        public TrainingStep Step { get; }

        // Dato del control cuando el paso tiene más de un lugar posible (ej. el modal del
        // checklist de Start que reabre un intento incorrecto).
        public string Detail { get; }

        public static readonly GuideMoment Load = new GuideMoment(GuideMomentKind.Load, null);

        public static GuideMoment Opened(TrainingStep step)
            => new GuideMoment(GuideMomentKind.Opened, step);

        public static GuideMoment Incorrect(TrainingStep step, string detail = null)
            => new GuideMoment(GuideMomentKind.Incorrect, step, detail);

        public static GuideMoment StepResolved(TrainingStep step)
            => new GuideMoment(GuideMomentKind.StepResolved, step);

        public static GuideMoment SessionCompleted(TrainingStep step)
            => new GuideMoment(GuideMomentKind.SessionCompleted, step);
    }
}
