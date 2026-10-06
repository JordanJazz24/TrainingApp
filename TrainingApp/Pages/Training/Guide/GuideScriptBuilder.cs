using System.Collections.Generic;
using System.Linq;
using System.Net;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    // Guion de la guía visual de Guided (spec 010): los subpasos de la UI de cada tipo de paso,
    // con los textos del anexo de docs/specs/010-plan.md. Es presentación (P-3): describe la
    // pantalla del paso que el servidor ya tiene como actual y no decide nada del flujo. Sin
    // System.Web, para poder probarlo sin la página.
    public sealed class GuideScriptBuilder
    {
        // Valor de ejemplo realista de la herramienta en Resource Setup (R6). La Shop Order de
        // ejemplo es la de la orden por defecto del simulador.
        private const string ExampleToolNumber = "10033058";

        public GuideScript Build(PodContext ctx, GuideMoment moment)
        {
            var script = new GuideScript();

            // P-4: el paso se resolvió en este request y su confirmación sigue en pantalla. La
            // guía arranca en la cola de ese paso y sigue con el paso nuevo sin postback.
            if (moment.Kind == GuideMomentKind.StepResolved && moment.Step != null)
            {
                var resolved = ScriptFor(moment.Step);
                if (resolved?.TailKey != null)
                {
                    script.Substeps.AddRange(resolved.Substeps);
                    script.EntryKey = resolved.TailKey;
                }
            }

            var current = ctx.CurrentStepOrDefault;
            if (ctx.Session.Status == SessionStatus.InProgress && current != null)
            {
                var step = ScriptFor(current);
                if (step != null)
                {
                    script.Substeps.AddRange(step.Substeps);
                    if (script.EntryKey == null)
                        script.EntryKey = step.EntryFor(moment);
                }
            }

            return script;
        }

        // Guion de un paso, o null si su tipo todavía no tiene guion (la guía no se muestra).
        private static StepScript ScriptFor(TrainingStep step)
        {
            switch (step.StepType)
            {
                case StepType.ResourceSetup: return ResourceSetup(step);
                default:                     return null;
            }
        }

        // ── Guion por tipo de paso ──────────────────────────────────────────

        private static StepScript ResourceSetup(TrainingStep step)
        {
            var s = new StepScript(step, "Resource Setup")
            {
                OpenedName    = "retrieve",
                IncorrectName = "setupState",
                TailName      = "recordChanged"
            };
            s.Add("open", GuideMode.Click, Anchor("pod-resource-setup"), postback: true,
                html: H("Presione ", B("Resource Setup"), " para asociar la estación (recurso) a la orden de trabajo."));
            s.Add("retrieve", GuideMode.Click, Anchor("rs-retrieve"),
                html: H("Presione ", B("Retrieve"), " para cargar la configuración del recurso."));
            s.Add("removeAll", GuideMode.Click, Anchor("rs-remove-all"),
                html: H("Presione ", B("REMOVE ALL"), " para limpiar la tabla antes de agregar los recursos de esta orden."));
            s.Add("insertShopOrder", GuideMode.Click, Anchor("rs-insert-new"),
                html: H("Presione ", B("INSERT NEW"), " para agregar una fila."));
            s.Add("shopOrder", GuideMode.Capture, Anchor("rs-row-value"), pickLast: true, format: GuideFormat.Digits,
                html: H("La fila nueva es de tipo ", B("Shop Order"), ". Ingrese el número de la orden, por ejemplo ",
                    B(PodDefaults.DefaultShopOrder), "."));
            s.Add("insertTool", GuideMode.Click, Anchor("rs-insert-new"),
                html: H("Presione ", B("INSERT NEW"), " para agregar otra fila."));
            s.Add("toolType", GuideMode.Change, Anchor("rs-row-type"), pickLast: true,
                html: H("Cambie el tipo de la fila nueva a ", B("Tool Number"), "."));
            s.Add("tool", GuideMode.Capture, Anchor("rs-row-value"), pickLast: true, format: GuideFormat.Required,
                html: H("Ingrese el número de la herramienta, por ejemplo ", B(ExampleToolNumber), "."));
            s.Add("table", GuideMode.Info, Anchor("rs-table"),
                html: H("En esta sección se agregan todos los recursos, herramientas y materiales asociados a su operación."));
            s.Add("setupState", GuideMode.Verify, Anchor("rs-setup-state"),
                html: H("Verifique que ", B("Setup State"), " esté en ", B("Set Up"), ". Si no, cámbielo."));
            // Un aviso de formato del control (filas incompletas) vuelve a armar las filas.
            s.Add("save", GuideMode.Click, Anchor("rs-save"), postback: true, retry: "removeAll",
                html: H("Presione ", B("Save"), " para guardar."));
            s.Add("recordChanged", GuideMode.Info, Anchor("rs-record-changed"), hide: true,
                html: H("Se muestra el mensaje ", B("Record changed"), ": indica que se aplicó el cambio."));
            s.Add("close", GuideMode.Click, Anchor("rs-close"),
                html: H("Cierre Resource Setup con la ", B("X"), "."));
            return s;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static string Anchor(string guide) => "[data-guide=\"" + guide + "\"]";

        // Texto de la instrucción: todo se codifica y los valores van en <strong>.
        private static string H(params object[] parts)
        {
            return string.Concat(parts.Select(p =>
            {
                var bold = p as Bold;
                return bold != null
                    ? "<strong>" + WebUtility.HtmlEncode(bold.Text) + "</strong>"
                    : WebUtility.HtmlEncode(p?.ToString() ?? string.Empty);
            }));
        }

        private static Bold B(string text) => new Bold(text);

        private sealed class Bold
        {
            public Bold(string text) { Text = text; }
            public string Text { get; }
        }

        // Todo el guion de un paso en un solo lugar: sus subpasos (con la etiqueta "Paso 1 ·
        // Resource Setup" y el contador "Subpaso 3 de 13") y dónde entra la guía según el
        // momento del request. Una entrada sin nombre usa el primer subpaso.
        private sealed class StepScript
        {
            private readonly TrainingStep _step;
            private readonly string _label;
            private readonly List<GuideSubstep> _items = new List<GuideSubstep>();

            public StepScript(TrainingStep step, string name)
            {
                _step  = step;
                _label = "Paso " + step.Order + " · " + name;
            }

            // Primer subpaso dentro del control recién abierto.
            public string OpenedName { get; set; }

            // Subpaso de la decisión que el servidor calificó como incorrecta.
            public string IncorrectName { get; set; }

            // Cola del paso resuelto: el subpaso de su mensaje de confirmación. Sin cola, la
            // guía no retoma este paso después de resolverlo.
            public string TailName { get; set; }

            public string TailKey => TailName != null ? Key(TailName) : null;

            public IList<GuideSubstep> Substeps
            {
                get
                {
                    for (int i = 0; i < _items.Count; i++)
                    {
                        _items[i].Index = i + 1;
                        _items[i].Total = _items.Count;
                    }
                    return _items;
                }
            }

            public string EntryFor(GuideMoment moment)
            {
                bool sameStep = moment.Step != null && moment.Step.Order == _step.Order;
                string name = null;
                if (sameStep && moment.Kind == GuideMomentKind.Opened)
                    name = OpenedName;
                else if (sameStep && moment.Kind == GuideMomentKind.Incorrect)
                    name = IncorrectName;
                return name != null ? Key(name) : _items[0].Key;
            }

            public void Add(string name, string mode, string anchor, string html,
                bool pickLast = false, bool postback = false, string format = null,
                bool hide = false, string retry = null)
            {
                _items.Add(new GuideSubstep
                {
                    Key            = Key(name),
                    Anchor         = anchor,
                    PickLast       = pickLast,
                    Mode           = mode,
                    Postback       = postback,
                    Format         = format,
                    HideOnContinue = hide,
                    RetryKey       = retry != null ? Key(retry) : null,
                    StepLabel      = _label,
                    Html           = html
                });
            }

            // Claves únicas en la lista: Line Clearance aparece dos veces en la guía.
            private string Key(string name) => _step.Order + ":" + name;
        }
    }
}
