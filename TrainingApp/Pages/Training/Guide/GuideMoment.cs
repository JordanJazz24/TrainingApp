using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public enum GuideMomentKind
    {
        Load,             // GET, F5 o un postback que no cambió el paso: entra por lo pendiente
        Opened,           // se abrió el control (o el modal) del paso
        Incorrect,        // intento fallido: el control queda abierto en la decisión
        Progress,         // avance dentro del paso con su mensaje (Add de Assembly, grupo de DC)
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

        // Dato del control cuando el paso tiene más de un lugar posible: el modal del checklist
        // que reabre un intento incorrecto, el grupo de DC que abrió Collect, el componente
        // recién ensamblado o la pieza y el grupo de DC recién guardados.
        public string Detail { get; }

        public static readonly GuideMoment Load = new GuideMoment(GuideMomentKind.Load, null);

        public static GuideMoment Opened(TrainingStep step, string detail = null)
            => new GuideMoment(GuideMomentKind.Opened, step, detail);

        public static GuideMoment Progress(TrainingStep step, string detail)
            => new GuideMoment(GuideMomentKind.Progress, step, detail);

        public static GuideMoment Incorrect(TrainingStep step, string detail = null)
            => new GuideMoment(GuideMomentKind.Incorrect, step, detail);

        public static GuideMoment StepResolved(TrainingStep step)
            => new GuideMoment(GuideMomentKind.StepResolved, step);

        // detail: "cleanup" si la cerró el script de la limpieza (spec 016): la consola con su
        // salida sigue en pantalla antes del resumen.
        public static GuideMoment SessionCompleted(TrainingStep step, string detail = null)
            => new GuideMoment(GuideMomentKind.SessionCompleted, step, detail);

        public const string CleanupDetail = "cleanup";
    }
}
