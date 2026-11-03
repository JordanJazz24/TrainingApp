using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public sealed partial class GuideScriptBuilder
    {
    // Piezas compartidas por los guiones de cada paso (AUD-74, E2): los textos de ejemplo y las
    // respuestas esperadas, la selección de filas del POD, el script de limpieza, los helpers de
    // HTML y selectores y el StepScript que arma los subpasos de un paso.
        // Valores de ejemplo realistas de los campos libres (R6). La Shop Order de ejemplo es
        // la de la orden por defecto del simulador.
        private const string ExampleToolNumber = "10033058";
        private const string ExampleBarcode    = "10087734";

        // Respuestas que la guía exige antes de avanzar (R10): las mismas que califica
        // StepValidator (Setup State, Line Clearance, checklist de Start y Boolean de DC).
        // Tool Number es el tipo de la segunda fila de Resource Setup (el control exige una).
        private const string AnswerSetUp      = "Set Up";
        private const string AnswerToolNumber = "Tool Number";
        private const string AnswerYes        = "true";

        private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

        // Script de limpieza (spec 016): doble clic en el ícono, el Code1 del operador + Enter y
        // cualquier tecla para cerrar la consola. last: la limpieza al salir ("para terminar").
        private static void AddScript(StepScript s, string code1, bool last)
        {
            // Doble clic: avanza cuando se ve la consola.
            s.Add("script", GuideMode.Click, Anchor("mes-icon-script"),
                done: new[] { Anchor("mes-console") },
                html: last
                    ? H("Haga doble clic en ", B("MES Limpiar historial y cache"), ".")
                    : H("Haga doble clic en ", B("MES Limpiar historial y cache"), " para limpiar el navegador."));
            // Enter lo procesa la consola (envía la página); la guía solo lo deja pasar con el
            // Code1 del operador.
            s.Add("scriptCode1", GuideMode.Scan, Anchor("mes-console-input"), expect: code1,
                html: H("Escriba su Code1: ", B(code1), " y presione Enter."));
            AddCloseConsole(s, last);
        }

        // "Press any key to continue": avanza cuando la consola se cierra.
        private static void AddCloseConsole(StepScript s, bool last)
        {
            s.Add("closeConsole", GuideMode.Scan, Anchor("mes-console"),
                done: new[] { "!" + Anchor("mes-console") },
                html: last
                    ? H("Presione cualquier tecla para terminar.")
                    : H("Presione cualquier tecla para cerrar la ventana."));
        }

        // Seleccionar exactamente una fila del POD: avanza cuando esa fila (y ninguna otra)
        // queda seleccionada. prefix: texto antes del SFC; sin él, "Seleccione el SFC {sfc}."
        // html: texto propio en lugar del armado con prefix; alert: alerta de la trainer (T7).
        private static void AddRowSelection(StepScript s, string name, SimulatedOrderRow row,
            string fallbackSfc, string prefix, string alert = null, string html = null)
        {
            string sfc = row?.SFCCode ?? fallbackSfc;
            if (html == null)
                html = prefix == null
                    ? SelectSfc(sfc)
                    : H(prefix, B(sfc ?? string.Empty), ".");
            if (row == null)
            {
                s.Add(name, GuideMode.Click, Anchor("pod-row"), html: html, alert: alert);
                return;
            }
            s.Add(name, GuideMode.Click, Anchor("pod-row") + Attr("data-row-id", row.RowId),
                done: new[]
                {
                    SelectedRow(row.RowId),
                    "!" + Anchor("pod-row") + ".selected:not(" + Attr("data-row-id", row.RowId) + ")"
                },
                html: html, alert: alert);
        }

        // Nota de la selección de Assembly y DC en Serialized (T7): el simulador exige todas las
        // piezas; el MES real, no.
        private static readonly string AllPiecesNote = H("Para fines de entrenamiento se trabaja con todas las piezas de la orden; ",
            "en el proceso real puede trabajar con cualquier pieza seleccionada.");

        // Selección de filas del POD antes de Assembly o DC (las exige la BLL: todas las piezas
        // en Serialized, el lote en NonSerialized). Se salta si ya están seleccionadas.
        // serializedNote: nota del Select All de Serialized.
        private static void AddPodSelection(StepScript s, PodContext ctx, string serializedNote)
        {
            var order = ctx.State.Order;
            var rows = order?.ActiveRows() ?? new List<SimulatedOrderRow>();
            if (ctx.Area.SerializationType == SerializationType.Serialized)
            {
                s.Add("select", GuideMode.Click, Anchor("pod-select-all"), done: AllSelected(rows),
                    html: H("Presione ", B("Select All"), " para seleccionar todas las piezas."),
                    note: serializedNote);
                return;
            }

            AddRowSelection(s, "select", rows.FirstOrDefault(), order?.SFCCode, null);
        }

        private static string SelectedRow(string rowId) => Anchor("pod-row") + ".selected" + Attr("data-row-id", rowId);

        // "Todas las filas seleccionadas". Sin filas no hay condición de hecho (una lista vacía
        // daría el subpaso por hecho sin que el operador haga nada).
        private static string[] AllSelected(IList<SimulatedOrderRow> rows)
            => rows.Count == 0 ? null : rows.Select(r => SelectedRow(r.RowId)).ToArray();

        // ── Helpers ─────────────────────────────────────────────────────────

        private static string Anchor(string guide) => Attr("data-guide", guide);

        // Selector de atributo con el valor escapado (códigos de grupo, ids de fila).
        private static string Attr(string name, string value)
        {
            string escaped = (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
            return "[" + name + "=\"" + escaped + "\"]";
        }

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
        // momento del request. Una entrada sin nombre usa el primer subpaso visible.
        private sealed class StepScript
        {
            private readonly TrainingStep _step;
            private readonly string _label;
            private readonly List<GuideSubstep> _items = new List<GuideSubstep>();
            private readonly HashSet<string> _hidden = new HashSet<string>();

            public StepScript(TrainingStep step, string name)
            {
                _step  = step;
                _label = "Paso " + step.Order + " · " + name;
            }

            // Primer subpaso dentro del control recién abierto.
            public string OpenedName { get; set; }

            // Entrada sin un momento que la decida (Load): la que corresponde al avance del paso
            // (spec 016: el Ingreso y la limpieza retoman donde iban). null: el primer subpaso.
            public string ResumeName { get; set; }

            // Elementos libres de todos los subpasos del paso (spec 014: la nota de hallazgo
            // queda resaltada mientras dura el paso). Se fija antes de agregar los subpasos.
            public List<string> FreeOnAll { get; } = new List<string>();

            // Subpaso de la decisión que el servidor calificó como incorrecta.
            public string IncorrectName { get; set; }

            // Si el control dice dónde abrió, reabrió o avanzó (GuideMoment.Detail), el subpaso
            // de ese lugar.
            public Dictionary<string, string> OpenedByDetail { get; } = new Dictionary<string, string>();
            public Dictionary<string, string> IncorrectByDetail { get; } = new Dictionary<string, string>();
            public Dictionary<string, string> ProgressByDetail { get; } = new Dictionary<string, string>();

            // Cola del paso resuelto: el subpaso de su mensaje de confirmación. Sin cola, la
            // guía no retoma este paso después de resolverlo.
            public string TailName { get; set; }

            public string TailKey => TailName != null && !_hidden.Contains(Key(TailName)) ? Key(TailName) : null;

            // Lo ya hecho no se muestra, pero cuenta en el contador: "Subpaso N de M" no cambia
            // entre requests.
            public IList<GuideSubstep> Substeps
            {
                get
                {
                    for (int i = 0; i < _items.Count; i++)
                    {
                        _items[i].Index = i + 1;
                        _items[i].Total = _items.Count;
                    }
                    return _items.Where(item => !_hidden.Contains(item.Key)).ToList();
                }
            }

            public void Hide(params string[] names)
            {
                foreach (var name in names) _hidden.Add(Key(name));
            }

            // Detalle del momento si es de este paso y de ese tipo.
            public string DetailOf(GuideMoment moment, GuideMomentKind kind)
            {
                return moment.Kind == kind && moment.Step != null && moment.Step.Order == _step.Order
                    ? moment.Detail : null;
            }

            public string EntryFor(GuideMoment moment)
            {
                bool sameStep = moment.Step != null && moment.Step.Order == _step.Order;
                string name = null;
                if (sameStep)
                {
                    switch (moment.Kind)
                    {
                        case GuideMomentKind.Opened:
                            if (moment.Detail == null || !OpenedByDetail.TryGetValue(moment.Detail, out name))
                                name = OpenedName;
                            break;
                        case GuideMomentKind.Incorrect:
                            if (moment.Detail == null || !IncorrectByDetail.TryGetValue(moment.Detail, out name))
                                name = IncorrectName;
                            break;
                        case GuideMomentKind.Progress:
                            if (moment.Detail != null) ProgressByDetail.TryGetValue(moment.Detail, out name);
                            break;
                    }
                }

                if (name == null) name = ResumeName;
                if (name != null && !_hidden.Contains(Key(name))) return Key(name);
                var first = _items.FirstOrDefault(item => !_hidden.Contains(item.Key));
                return first?.Key;
            }

            public void Add(string name, string mode, string anchor, string html,
                bool pickLast = false, bool postback = false, string format = null,
                bool hide = false, string retry = null, string expect = null,
                List<GuideField> fields = null, string[] done = null, string[] free = null,
                string note = null, string alert = null, IList<string> reject = null)
            {
                var freeAll = (free ?? new string[0]).Concat(FreeOnAll).ToList();
                _items.Add(new GuideSubstep
                {
                    Key            = Key(name),
                    Anchor         = anchor,
                    PickLast       = pickLast,
                    Mode           = mode,
                    Postback       = postback,
                    Format         = format,
                    Expect         = expect,
                    Reject         = reject?.ToList(),
                    Fields         = fields,
                    Done           = done?.ToList(),
                    Free           = freeAll.Count > 0 ? freeAll : null,
                    HideOnContinue = hide,
                    RetryKey       = retry != null ? Key(retry) : null,
                    StepLabel      = _label,
                    Html           = html,
                    // Nota de la trainer (T7), normal o alerta, solo en este subpaso.
                    NoteHtml       = alert ?? note,
                    NoteIsAlert    = alert != null
                });
            }

            // Claves únicas en la lista: Line Clearance aparece dos veces en la guía.
            private string Key(string name) => _step.Order + ":" + name;
        }
    }
}
