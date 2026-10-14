using System;
using System.Collections.Generic;
using System.Globalization;
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

        public GuideScript Build(PodContext ctx, GuideMoment moment)
        {
            var script = new GuideScript();

            // P-4: el paso se resolvió en este request y su confirmación sigue en pantalla. La
            // guía arranca en la cola de ese paso y sigue con el paso nuevo sin postback.
            if (moment.Kind == GuideMomentKind.StepResolved && moment.Step != null)
            {
                var resolved = ScriptFor(ctx, moment.Step, moment);
                if (resolved?.TailKey != null)
                {
                    script.Substeps.AddRange(resolved.Substeps);
                    script.EntryKey = resolved.TailKey;
                }
            }

            // Sesión completada (R6): un último globo sobre "Session Complete".
            if (moment.Kind == GuideMomentKind.SessionCompleted)
            {
                script.Substeps.Add(new GuideSubstep
                {
                    Key       = "final",
                    Anchor    = Anchor("pod-final"),
                    Mode      = GuideMode.Final,
                    StepLabel = "Guía completada",
                    Index     = 1,
                    Total     = 1,
                    Html      = H("Completó la guía. Cuando se sienta listo, intente el ", B("Assessment"), ".")
                });
                script.EntryKey = "final";
                return script;
            }

            var current = ctx.CurrentStepOrDefault;
            if (ctx.Session.Status == SessionStatus.InProgress && current != null)
            {
                var step = ScriptFor(ctx, current, moment);
                if (step != null && step.Substeps.Count > 0)
                {
                    script.Substeps.AddRange(step.Substeps);
                    if (script.EntryKey == null)
                        script.EntryKey = step.EntryFor(moment);
                }
            }

            return script;
        }

        // Guion de un paso, o null si su tipo todavía no tiene guion (la guía no se muestra).
        private static StepScript ScriptFor(PodContext ctx, TrainingStep step, GuideMoment moment)
        {
            switch (step.StepType)
            {
                case StepType.ResourceSetup:  return ResourceSetup(step);
                case StepType.LineClearance:  return LineClearance(ctx, step);
                case StepType.Search:         return Start(ctx, step);
                case StepType.Assembly:       return Assembly(ctx, step, moment);
                case StepType.DataCollection: return DataCollection(ctx, step, moment);
                case StepType.SFC_Split:      return SfcSplit(ctx, step);
                case StepType.LogNC:          return LogNC(ctx, step);
                case StepType.Complete:       return Complete(ctx, step);
                default:                      return null;
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
                html: H("Presione ", B("REMOVE ALL"), " para limpiar la tabla antes de agregar los recursos de esta orden."),
                alert: H("Si hay equipos registrados, haga ", B("Remove All"), " y escanee los de su estación. ",
                    "No asuma que son los mismos que tiene en su mesa; sin ese registro se pierde la trazabilidad ",
                    "de la operación, lo que puede conllevar acciones disciplinarias."));
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

        // Pasos 2 y 6: el mismo recorrido en apertura y en cierre. Cambian las notas de la
        // trainer (T7): la de apertura dice cuándo se hace; la de cierre, que no siempre aplica.
        private static StepScript LineClearance(PodContext ctx, TrainingStep step)
        {
            bool closing = ctx.Guide.Steps.Any(x => x.StepType == StepType.LineClearance && x.Order < step.Order);
            var s = new StepScript(step, "Line Clearance")
            {
                OpenedName    = "retrieve",
                IncorrectName = "answer",
                TailName      = "saved"
            };
            s.Add("activities", GuideMode.Click, Anchor("pod-activities"),
                html: H("Presione ", B("Activities..."), " para abrir el menú de actividades."),
                note: closing ? null : H("Siempre después de ", B("Resource Setup"),
                    " y en cada cambio de orden: la limpieza física y la del sistema."),
                alert: closing ? H("En algunas operaciones no aplica (depende de la operación).") : null);
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

        // Search/Start (spec 011): escanear la etiqueta de la orden (o tocar las barras, o
        // escribir la Shop Order en SFC) y Start; Browse Work List se menciona como camino
        // alternativo. Con el checklist de Start (hoy Moldeo) siguen sus 4 modales; los
        // formularios de varios campos van por bloque (R7).
        private static StepScript Start(PodContext ctx, TrainingStep step)
        {
            var s = new StepScript(step, "Start") { TailName = "banner" };

            // Avanza cuando la orden completa (todas las piezas o el lote) queda cargada en
            // SFC; una sola pieza no alcanza. Si ya estaba cargada, se salta.
            bool serialized = ctx.Area.SerializationType == SerializationType.Serialized;
            s.Add("scan", GuideMode.Scan, Anchor("pod-label"),
                free: new[] { Anchor("pod-sfc") },
                done: new[] { Anchor("pod-sfc") + Attr("data-loaded", "order") },
                html: H("Escanee la etiqueta de la orden con el lector o toque las barras: la ",
                    B("Shop Order"), serialized ? " carga todas las piezas. " : " carga el SFC del lote. ",
                    "También puede escribir el número en ", B("SFC"), " y presionar Enter."),
                alert: H("Asegúrese de trabajar solo las piezas de su orden y su estación."));
            s.Add("browse", GuideMode.Info, Anchor("pod-browse"),
                html: H("La orden también se puede cargar con ", B("Browse Work List"),
                    " (⧉), sobre todo cuando se trabaja con varias órdenes a la vez."));
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

        // Assembly: cada componente del BOM, en el orden de la Component List. Lo ensamblado
        // antes de este request no se muestra; del componente recién ensamblado (Add, sin ser
        // el último) quedan su confirmación y la X. El Add del último completa el paso.
        private static StepScript Assembly(PodContext ctx, TrainingStep step, GuideMoment moment)
        {
            var s = new StepScript(step, "Assembly") { TailName = "banner" };
            AddPodSelection(s, ctx, AllPiecesNote);
            s.Add("open", GuideMode.Click, Anchor("pod-assemble"), postback: true,
                html: H("Presione ", B("Assemble"), " para abrir la Component List."));

            var bom = ctx.AssemblyComponents;
            var assembled = ctx.State.Assembly?.AssembledComponentIds ?? new HashSet<int>();
            string justAssembled = s.DetailOf(moment, GuideMomentKind.Progress);

            for (int i = 0; i < bom.Count; i++)
            {
                var c = bom[i];
                string id = c.Id.ToString(CultureInfo.InvariantCulture);
                string p = "c" + id;
                bool last = i == bom.Count - 1;

                s.Add(p + ".row", GuideMode.Click, Anchor("asm-assemble") + Attr("data-component-id", id),
                    html: H("Presione ", B("Assemble"), " en el componente ", B(c.PartNumber + "/" + c.Revision), "."));
                s.Add(p + ".barcode", GuideMode.Scan, Anchor("asm-barcode"), format: GuideFormat.Digits,
                    done: new[] { Anchor("asm-add") },
                    html: H("Escanee el código de barras del componente, por ejemplo ", B(ExampleBarcode), "."));
                s.Add(p + ".add", GuideMode.Click, Anchor("asm-add"), postback: true, retry: p + ".barcode",
                    html: H("Presione ", B("Add"), " para registrar el componente."));
                if (!last)
                {
                    s.Add(p + ".confirm", GuideMode.Info, Anchor("asm-confirm"),
                        html: H("Verifique el mensaje de confirmación: el componente quedó ensamblado."));
                    s.Add(p + ".close", GuideMode.Click, Anchor("asm-close"),
                        html: H("Cierre el modal con la ", B("X"), " para seguir con el siguiente componente."));
                    s.ProgressByDetail[id] = p + ".confirm";
                }

                if (assembled.Contains(c.Id))
                {
                    s.Hide(p + ".row", p + ".barcode", p + ".add");
                    if (!last && id != justAssembled) s.Hide(p + ".confirm", p + ".close");
                }
                else if (s.OpenedName == null)
                {
                    s.OpenedName = p + ".row";
                }
            }

            s.Add("banner", GuideMode.Info, Anchor("pod-banner"), hide: true,
                html: H("Verifique el mensaje ", B("All components processed at this operation"), ": se ensambló todo el BOM."));
            return s;
        }

        // Data Collection: cada pieza (Serialized; NonSerialized, el lote) por cada grupo
        // obligatorio (R4: si el área no tiene, el primero). Lo guardado antes de este request
        // no se muestra; del grupo recién guardado queda su banner. El último Save completa el
        // paso sin mensaje, así que no hay cola.
        private static StepScript DataCollection(PodContext ctx, TrainingStep step, GuideMoment moment)
        {
            var s = new StepScript(step, "Data Collection");
            AddPodSelection(s, ctx, AllPiecesNote);
            s.Add("open", GuideMode.Click, Anchor("pod-dc-collect"), postback: true,
                html: H("Presione ", B("DC Collect"), " para abrir la Data Collection List."),
                note: H("La cantidad de datos depende de la operación."));

            var groups = ctx.DataCollectionGroups;
            var required = groups.Where(g => g.IsRequired).ToList();
            if (required.Count == 0 && groups.Count > 0) required.Add(groups[0]);

            var progress = ctx.State.DataCollection;
            var pieces = DataCollectionPieces(ctx);
            int currentPiece = progress?.CurrentIndex ?? 0;
            string justSaved = s.DetailOf(moment, GuideMomentKind.Progress);
            bool serialized = ctx.Area.SerializationType == SerializationType.Serialized;

            for (int pi = 0; pi < pieces.Count; pi++)
            {
                var piece = pieces[pi];
                for (int gi = 0; gi < required.Count; gi++)
                {
                    var g = required[gi];
                    string p = "dc." + piece.RowId + "." + g.Id.ToString(CultureInfo.InvariantCulture);
                    string detail = piece.RowId + "|" + g.Code;
                    bool lastPair = pi == pieces.Count - 1 && gi == required.Count - 1;

                    s.Add(p + ".collect", GuideMode.Click, Anchor("dc-collect") + Attr("data-group", g.Code), postback: true,
                        html: serialized
                            ? H("Presione ", B("Collect"), " en el grupo ", B(g.Code), " para la pieza ", B(piece.SFCCode), ".")
                            : H("Presione ", B("Collect"), " en el grupo ", B(g.Code), "."));
                    var block = DataCollectionBlock(ctx, g);
                    s.Add(p + ".block", GuideMode.Capture, Anchor("dc-params"), fields: block.Item1, html: block.Item2);
                    s.Add(p + ".save", GuideMode.Click, Anchor("dc-save"), postback: true, retry: p + ".block",
                        html: H("Presione ", B("Save"), " para guardar el grupo."));
                    if (!lastPair)
                    {
                        bool completesPiece = gi == required.Count - 1;
                        s.Add(p + ".banner", GuideMode.Info, Anchor("pod-banner"), hide: true,
                            html: completesPiece
                                ? H("Verifique el mensaje: la pieza ", B(piece.SFCCode), " completó Data Collection y sigue la siguiente.")
                                : H("Verifique el mensaje ", B("All data for DC group " + g.Code + " has been collected"), "."));
                        s.ProgressByDetail[detail] = p + ".banner";
                    }

                    bool saved = progress != null && (pi < currentPiece ||
                        (pi == currentPiece && progress.SavedGroupIds(piece.RowId).Contains(g.Id)));
                    if (saved)
                    {
                        s.Hide(p + ".collect", p + ".block", p + ".save");
                        if (!lastPair && detail != justSaved) s.Hide(p + ".banner");
                    }
                    else
                    {
                        if (s.OpenedName == null) s.OpenedName = p + ".collect";
                        // Collect abrió el grupo (o un intento incorrecto lo reabrió): su bloque.
                        if (pi == currentPiece)
                        {
                            s.OpenedByDetail[g.Code]    = p + ".block";
                            s.IncorrectByDetail[g.Code] = p + ".block";
                        }
                    }
                }
            }
            return s;
        }

        // SFC Split (solo NonSerialized): la fila del lote, la cantidad, Split, su mensaje,
        // Close y el banner como cola.
        private static StepScript SfcSplit(PodContext ctx, TrainingStep step)
        {
            var s = new StepScript(step, "SFC Split") { OpenedName = "qty", TailName = "banner" };
            s.ProgressByDetail["split"] = "created";

            var lot = ctx.State.Order?.ActiveRows().FirstOrDefault(r => !r.IsFromSplit);
            AddRowSelection(s, "select", lot, ctx.State.Order?.SFCCode, null);
            s.Add("open", GuideMode.Click, Anchor("pod-sfc-split"), postback: true,
                html: H("Presione ", B("SFC Split"), " para dividir el lote."));
            s.Add("qty", GuideMode.Capture, Anchor("split-qty"), format: GuideFormat.Digits,
                html: H("Ingrese la cantidad a dividir, por ejemplo ",
                    B(PodDefaults.DefaultSplitQty.ToString(CultureInfo.InvariantCulture)), "."));
            s.Add("split", GuideMode.Click, Anchor("split-split"), postback: true,
                html: H("Presione ", B("Split"), "."));
            s.Add("created", GuideMode.Info, Anchor("split-created"),
                html: H("Verifique el mensaje ", B("New SFC … created with quantity …"), ": se creó un SFC nuevo con esa cantidad."));
            s.Add("close", GuideMode.Click, Anchor("split-close"), postback: true,
                html: H("Presione ", B("Close"), " para terminar el split."));
            s.Add("banner", GuideMode.Info, Anchor("pod-banner"), hide: true,
                html: H("Verifique el mensaje ", B("SFC Split completed successfully"), "."));
            return s;
        }

        // Log NC: la fila a scrapear (NonSerialized: la del split; Serialized: una pieza), toda
        // la NC Selection (R6: cualquier código es válido), Component si el área lo pide y
        // Add-Done. El POD ya llega sin selección (GuidedTest la limpia tras Start, Assembly y
        // DC): un subpaso para quitarla se saltaba siempre y el contador empezaba en 2.
        private static StepScript LogNC(PodContext ctx, TrainingStep step)
        {
            bool needsComponent = ctx.Area.RequiresNCComponent;
            var s = new StepScript(step, "Log NC")
            {
                OpenedName    = "list",
                IncorrectName = needsComponent ? "component" : "addDone",
                TailName      = "banner"
            };
            s.OpenedByDetail["entry"] = needsComponent ? "component" : "addDone";

            var order = ctx.State.Order;
            if (ctx.Area.SerializationType == SerializationType.Serialized)
            {
                var piece = order?.ActiveRows().FirstOrDefault();
                AddRowSelection(s, "select", piece, null, "Seleccione una pieza para registrar el defecto, por ejemplo ");
            }
            else
            {
                var splitRow = order?.ActiveRows().FirstOrDefault(r => r.IsFromSplit);
                AddRowSelection(s, "select", splitRow, null, "Seleccione el SFC creado por el split, ");
            }

            s.Add("open", GuideMode.Click, Anchor("pod-lognc"), postback: true,
                html: H("Presione ", B("Log NC"), " para registrar un defecto."),
                note: H("Verifique que las piezas seleccionadas sean las defectuosas."));
            s.Add("list", GuideMode.Click, Anchor("nc-list"), postback: true,
                html: H("Presione ", B("Log NC"), " en el código del defecto. En el simulador cualquier código es válido."));
            if (needsComponent)
                s.Add("component", GuideMode.Capture, Anchor("nc-component"), format: GuideFormat.Required,
                    html: H("Ingrese el componente, por ejemplo ", B(ExampleBarcode), "."));
            s.Add("addDone", GuideMode.Click, Anchor("nc-add-done"), postback: true,
                html: H("Presione ", B("Add-Done"), " para registrar el NC."));
            s.Add("banner", GuideMode.Info, Anchor("pod-banner"), hide: true,
                html: H("Verifique el mensaje de ", B("scrap"), ": las unidades se dieron de baja."));
            return s;
        }

        // Complete: las filas que quedan (Serialized: todas con Select All; NonSerialized: el
        // lote) y Complete. Con él termina la sesión y aparece el globo final.
        private static StepScript Complete(PodContext ctx, TrainingStep step)
        {
            var s = new StepScript(step, "Complete");
            var order = ctx.State.Order;
            var rows = order?.ActiveRows() ?? new List<SimulatedOrderRow>();
            if (ctx.Area.SerializationType == SerializationType.Serialized)
                s.Add("select", GuideMode.Click, Anchor("pod-select-all"), done: AllSelected(rows),
                    html: H("Presione ", B("Select All"), " para seleccionar todas las piezas."));
            else
                AddRowSelection(s, "select", rows.FirstOrDefault(), order?.SFCCode, null);
            s.Add("complete", GuideMode.Click, Anchor("pod-complete"), postback: true,
                html: H("Presione ", B("Complete"), " para cerrar la orden."));
            return s;
        }

        // Seleccionar exactamente una fila del POD: avanza cuando esa fila (y ninguna otra)
        // queda seleccionada. prefix: texto antes del SFC; sin él, "Seleccione el SFC {sfc}."
        private static void AddRowSelection(StepScript s, string name, SimulatedOrderRow row,
            string fallbackSfc, string prefix)
        {
            string sfc = row?.SFCCode ?? fallbackSfc;
            string html = prefix == null
                ? SelectSfc(sfc)
                : H(prefix, B(sfc ?? string.Empty), ".");
            if (row == null)
            {
                s.Add(name, GuideMode.Click, Anchor("pod-row"), html: html);
                return;
            }
            s.Add(name, GuideMode.Click, Anchor("pod-row") + Attr("data-row-id", row.RowId),
                done: new[]
                {
                    SelectedRow(row.RowId),
                    "!" + Anchor("pod-row") + ".selected:not(" + Attr("data-row-id", row.RowId) + ")"
                },
                html: html);
        }

        // Piezas de Data Collection: las que fijó la BLL al abrirlo o, antes de abrirlo, las
        // filas activas de la orden (las mismas que tomará: todas en Serialized, el lote en
        // NonSerialized).
        private static IList<DataCollectionPiece> DataCollectionPieces(PodContext ctx)
        {
            var order = ctx.State.Order;
            if (ctx.State.DataCollection != null)
                return ctx.State.DataCollection.Pieces
                    .Select(p => new DataCollectionPiece { RowId = p.RowId, SFCCode = p.SFCCode ?? order?.SFCCode })
                    .ToList();
            if (order == null) return new List<DataCollectionPiece>();
            return order.ActiveRows()
                .Select(r => new DataCollectionPiece { RowId = r.RowId, SFCCode = r.SFCCode ?? order.SFCCode })
                .ToList();
        }

        // Bloque de un grupo de DC: las reglas de cada parámetro (las de StepValidator) y el
        // texto que las lista.
        private static Tuple<List<GuideField>, string> DataCollectionBlock(PodContext ctx, DataCollectionGroup group)
        {
            var fields = new List<GuideField>();
            var lines = new List<string> { H("Ingrese los valores:") };
            var parameters = ctx.DataCollectionParameters
                .Where(p => p.DataCollectionGroupId == group.Id)
                .OrderBy(p => p.DisplayOrder);

            foreach (var prm in parameters)
            {
                string selector = Attr("data-paramid", prm.Id.ToString(CultureInfo.InvariantCulture));
                string name = (prm.Name ?? string.Empty).Trim().TrimEnd(':');
                switch (prm.DataType)
                {
                    case "Boolean":
                        fields.Add(new GuideField { Selector = selector, Expect = AnswerYes });
                        lines.Add(H("• ", B(name), ": ", B("Si")));
                        break;
                    case "Date":
                        fields.Add(new GuideField { Selector = selector, Rule = "date" });
                        lines.Add(H("• ", B(name), ": una fecha válida, por ejemplo ",
                            B(DateTime.Today.AddYears(1).ToString("d 'de' MMMM 'de' yyyy", Spanish))));
                        break;
                    case "Numeric":
                        fields.Add(new GuideField { Selector = selector, Rule = "number", Min = prm.MinValue, Max = prm.MaxValue });
                        lines.Add(H("• ", B(name), ": ") + NumericRange(prm));
                        break;
                    default:
                        fields.Add(new GuideField { Selector = selector, Rule = "required" });
                        lines.Add(H("• ", B(name)));
                        break;
                }
            }
            return Tuple.Create(fields, string.Join("<br>", lines));
        }

        private static string NumericRange(DataCollectionParameter prm)
        {
            string unit = string.IsNullOrWhiteSpace(prm.Unit) ? string.Empty : " " + prm.Unit.Trim();
            if (prm.MinValue.HasValue && prm.MaxValue.HasValue)
                return H("entre ", B(Num(prm.MinValue.Value)), " y ", B(Num(prm.MaxValue.Value)), unit);
            if (prm.MinValue.HasValue)
                return H("mayor o igual a ", B(Num(prm.MinValue.Value)), unit);
            if (prm.MaxValue.HasValue)
                return H("menor o igual a ", B(Num(prm.MaxValue.Value)), unit);
            return H("un valor numérico");
        }

        private static string Num(decimal value) => value.ToString("0.######", CultureInfo.InvariantCulture);

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

                if (name != null && !_hidden.Contains(Key(name))) return Key(name);
                var first = _items.FirstOrDefault(item => !_hidden.Contains(item.Key));
                return first?.Key;
            }

            public void Add(string name, string mode, string anchor, string html,
                bool pickLast = false, bool postback = false, string format = null,
                bool hide = false, string retry = null, string expect = null,
                List<GuideField> fields = null, string[] done = null, string[] free = null,
                string note = null, string alert = null)
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
                    Fields         = fields,
                    Done           = done?.ToList(),
                    Free           = free?.ToList(),
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
