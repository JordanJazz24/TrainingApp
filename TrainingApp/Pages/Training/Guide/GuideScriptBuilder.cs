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

        // Respuestas que la guía exige antes de avanzar (R10): las mismas que califica
        // StepValidator (Setup State, Line Clearance y checklist de Start). Tool Number es el
        // tipo de la segunda fila de Resource Setup (el control exige al menos una).
        private const string AnswerSetUp      = "Set Up";
        private const string AnswerToolNumber = "Tool Number";
        private const string AnswerYes        = "true";

        public GuideScript Build(PodContext ctx, GuideMoment moment)
        {
            var script = new GuideScript();

            // P-4: el paso se resolvió en este request y su confirmación sigue en pantalla. La
            // guía arranca en la cola de ese paso y sigue con el paso nuevo sin postback.
            if (moment.Kind == GuideMomentKind.StepResolved && moment.Step != null)
            {
                var resolved = ScriptFor(ctx, moment.Step);
                if (resolved?.TailKey != null)
                {
                    script.Substeps.AddRange(resolved.Substeps);
                    script.EntryKey = resolved.TailKey;
                }
            }

            var current = ctx.CurrentStepOrDefault;
            if (ctx.Session.Status == SessionStatus.InProgress && current != null)
            {
                var step = ScriptFor(ctx, current);
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
        private static StepScript ScriptFor(PodContext ctx, TrainingStep step)
        {
            switch (step.StepType)
            {
                case StepType.ResourceSetup: return ResourceSetup(step);
                case StepType.LineClearance: return LineClearance(step);
                case StepType.Search:        return Start(ctx, step);
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
            s.Add("toolType", GuideMode.Change, Anchor("rs-row-type"), pickLast: true, expect: AnswerToolNumber,
                html: H("Cambie el tipo de la fila nueva a ", B("Tool Number"), "."));
            s.Add("tool", GuideMode.Capture, Anchor("rs-row-value"), pickLast: true, format: GuideFormat.Required,
                html: H("Ingrese el número de la herramienta, por ejemplo ", B(ExampleToolNumber), "."));
            s.Add("table", GuideMode.Info, Anchor("rs-table"),
                html: H("En esta sección se agregan todos los recursos, herramientas y materiales asociados a su operación."));
            s.Add("setupState", GuideMode.Verify, Anchor("rs-setup-state"), expect: AnswerSetUp,
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

        // Pasos 2 y 6: el mismo recorrido en apertura y en cierre.
        private static StepScript LineClearance(TrainingStep step)
        {
            var s = new StepScript(step, "Line Clearance")
            {
                OpenedName    = "retrieve",
                IncorrectName = "answer",
                TailName      = "saved"
            };
            s.Add("activities", GuideMode.Click, Anchor("pod-activities"),
                html: H("Presione ", B("Activities..."), " para abrir el menú de actividades."));
            s.Add("standalone", GuideMode.Click, Anchor("pod-dc-standalone"), postback: true,
                html: H("Seleccione ", B("Data Collection Standalone"), " para registrar la limpieza de línea."));
            s.Add("retrieve", GuideMode.Click, Anchor("lc-retrieve"),
                html: H("Presione ", B("Retrieve"), " para cargar los grupos de datos del recurso."));
            s.Add("collect", GuideMode.Click, Anchor("lc-collect"),
                html: H("Presione ", B("Collect"), " en ", B("DC_LC_DONE"), " para responder la limpieza de línea."));
            s.Add("answer", GuideMode.Change, Anchor("lc-answer"), expect: AnswerYes,
                html: H("Seleccione ", B("Si"), ": la limpieza de línea se realizó."));
            s.Add("save", GuideMode.Click, Anchor("lc-save"), postback: true, retry: "answer",
                html: H("Presione ", B("Save"), " para guardar la respuesta."));
            s.Add("saved", GuideMode.Info, Anchor("lc-saved"), hide: true,
                html: H("Verifique que aparezca el mensaje ", B("1 DC Groups saved"), ": la respuesta quedó registrada."));
            s.Add("close", GuideMode.Click, Anchor("lc-close"),
                html: H("Cierre Data Collection Standalone con la ", B("X"), "."));
            return s;
        }

        // Search/Start: Browse Work List, selección y Start. Con el checklist de Start (hoy
        // Moldeo) siguen sus 4 modales; los formularios de varios campos van por bloque (R7).
        private static StepScript Start(PodContext ctx, TrainingStep step)
        {
            var s = new StepScript(step, "Start") { TailName = "banner" };

            s.Add("browse", GuideMode.Click, Anchor("pod-browse"),
                html: H("Presione el ícono ", B("Browse Work List"), " (⧉), junto a SFC, para buscar la orden."));
            if (ctx.Area.SerializationType == SerializationType.Serialized)
                s.Add("select", GuideMode.Click, Anchor("browse-select-all"),
                    html: H("Presione ", B("Select All"), " para seleccionar todas las piezas de la orden."));
            else
                s.Add("select", GuideMode.Click, Anchor("browse-row"),
                    html: SelectSfc(ctx.State.Order?.SFCCode));
            s.Add("ok", GuideMode.Click, Anchor("browse-ok"),
                html: H("Presione ", B("OK"), " para cargar la selección en el POD."));
            s.Add("start", GuideMode.Click, Anchor("pod-start"), postback: true,
                html: H("Presione ", B("Start"), " para iniciar la orden."));

            if (ctx.Area.RequiresStartChecklist)
            {
                // Start abre el modal 1; un intento incorrecto reabre el modal de la primera
                // respuesta "No" (verificaciones o final). Sin checklist no hay estas entradas.
                s.OpenedName    = "molde";
                s.IncorrectName = "verifs";
                s.IncorrectByDetail["modalSearch4"] = "final";

                s.Add("molde", GuideMode.Capture, Anchor("search-molde"), format: GuideFormat.Number,
                    html: H("Ingrese el ", B("ID del molde"), " según la MSS."));
                s.Add("save1", GuideMode.Click, Anchor("search-save1"),
                    html: H("Presione ", B("Save"), "."));
                s.Add("verifs", GuideMode.Capture, Anchor("search-verifs"), format: GuideFormat.Required, expect: AnswerYes,
                    html: H("Responda ", B("Sí"), " a las tres verificaciones de etiquetas."));
                s.Add("save2", GuideMode.Click, Anchor("search-save2"),
                    html: H("Presione ", B("Save"), "."));
                s.Add("mss", GuideMode.Capture, Anchor("search-mss"), format: GuideFormat.Number,
                    html: H("Ingrese los parámetros técnicos según la ", B("MSS D000281359"), "."));
                s.Add("save3", GuideMode.Click, Anchor("search-save3"),
                    html: H("Presione ", B("Save"), "."));
                s.Add("final", GuideMode.Change, Anchor("search-final"), expect: AnswerYes,
                    html: H("Seleccione ", B("Sí"), " en la verificación final."));
                s.Add("save4", GuideMode.Click, Anchor("search-save4"), postback: true, retry: "final",
                    html: H("Presione ", B("Save"), " para iniciar la orden."));
            }

            s.Add("banner", GuideMode.Info, Anchor("pod-banner"), hide: true,
                html: H("Verifique el mensaje ", B("Start process completed successfully"), ": la orden quedó activa."));
            return s;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static string Anchor(string guide) => "[data-guide=\"" + guide + "\"]";

        // "Seleccione el SFC {sfc}." (NonSerialized: la fila del lote).
        private static string SelectSfc(string sfc)
        {
            return string.IsNullOrEmpty(sfc)
                ? H("Seleccione el SFC de la orden.")
                : H("Seleccione el SFC ", B(sfc), ".");
        }

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

            // Si el control dice dónde reabrió (GuideMoment.Detail), el subpaso de ese lugar.
            public Dictionary<string, string> IncorrectByDetail { get; } = new Dictionary<string, string>();

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
                {
                    if (moment.Detail == null || !IncorrectByDetail.TryGetValue(moment.Detail, out name))
                        name = IncorrectName;
                }
                return name != null ? Key(name) : _items[0].Key;
            }

            public void Add(string name, string mode, string anchor, string html,
                bool pickLast = false, bool postback = false, string format = null,
                bool hide = false, string retry = null, string expect = null)
            {
                _items.Add(new GuideSubstep
                {
                    Key            = Key(name),
                    Anchor         = anchor,
                    PickLast       = pickLast,
                    Mode           = mode,
                    Postback       = postback,
                    Format         = format,
                    Expect         = expect,
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
