using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public sealed partial class GuideScriptBuilder
    {
        // Alerta del subpaso 1 de Log NC (spec 014, D-43, texto aprobado; sin la mención
        // disciplinaria, D-152).
        private static readonly string WrongUnitAlert = H(
            "Revise que solo esté seleccionada la pieza/SFC defectuosa. ",
            "Scrapear la unidad equivocada es CRÍTICO: MES no lo detecta.");

        // Log NC (spec 014): la pieza del hallazgo (NonSerialized: la fila del split), el código
        // del hallazgo en NC Selection, el componente en la reparación rápida, Comments en la
        // extendida si el código lo exige, y Add-Done. El desecho no pide Component en ningún
        // área (enmienda 4). La guía señala la pieza, el código y la disposición, así que no deja
        // enviar otros (como D-25). El POD ya llega sin selección (GuidedTest la limpia tras
        // Start, Assembly y DC).
        private static StepScript LogNC(PodContext ctx, TrainingStep step)
        {
            bool repair   = step.StepType == StepType.LogNCFastRepair;
            bool extended = step.StepType == StepType.LogNCExtendedRepair;
            var finding = ctx.NcFindingFor(step);
            bool comments = extended && finding != null &&
                ctx.NCCodes.Any(c => c.RequiresComments && string.Equals(c.Code, finding.Code, StringComparison.OrdinalIgnoreCase));
            string entry = repair ? "component" : comments ? "commentsInfo" : "addDone";

            var s = new StepScript(step, repair ? "Log NC · reparación rápida" : extended ? "Log NC · reparación extendida" : "Log NC")
            {
                OpenedName    = "list",
                IncorrectName = entry,
                TailName      = "banner"
            };
            s.OpenedByDetail["entry"] = entry;

            AddFindingAndPiece(s, ctx, finding);
            AddNcList(s, finding, repair, extended);

            if (repair)
            {
                AddFastRepair(s, finding);
                return s;
            }

            if (extended)
            {
                AddExtendedRepair(s, ctx, finding, comments);
                return s;
            }

            AddScrap(s);
            return s;
        }

        // Primera pantalla: el hallazgo (Serialized) y la pieza del POD que hay que seleccionar
        // (la del hallazgo o, en NonSerialized, la fila del split).
        private static void AddFindingAndPiece(StepScript s, PodContext ctx, NcFinding finding)
        {
            var order = ctx.State.Order;

            if (finding != null) s.FreeOnAll.Add(Anchor("nc-finding"));
            if (ctx.Area.SerializationType == SerializationType.Serialized)
            {
                // Enmienda 2 de la spec 014: el primer subpaso presenta el hallazgo (en Moldeo se
                // presenta en el SFC Split).
                s.Add("finding", GuideMode.Info, Anchor("nc-finding"),
                    html: H("Calidad reportó un hallazgo: revise el SFC, el defecto y la disposición. ",
                        "En este paso lo registra en el MES."));
                var piece = finding?.RowId == null ? null
                    : order?.ActiveRows().FirstOrDefault(r => r.RowId == finding.RowId);
                AddRowSelection(s, "select", piece, null, "Seleccione la pieza del hallazgo: ", alert: WrongUnitAlert);
            }
            else
            {
                var splitRow = order?.ActiveRows().FirstOrDefault(r => r.IsFromSplit);
                AddRowSelection(s, "select", splitRow, null, null, alert: WrongUnitAlert,
                    html: H("Seleccione el SFC creado por el split."));
            }
        }

        // Abrir Log NC y elegir el código en NC Selection.
        private static void AddNcList(StepScript s, NcFinding finding, bool repair, bool extended)
        {
            s.Add("open", GuideMode.Click, Anchor("pod-lognc"), postback: true,
                html: H("Presione ", B("Log NC"), " para registrar el defecto."));
            s.Add("list", GuideMode.Click,
                finding == null ? Anchor("nc-list") : Anchor("nc-code") + Attr("data-nc-code", finding.Code),
                postback: true,
                // Enmienda 5: las reparaciones usan el código general de su método; el desecho, el
                // del defecto.
                html: repair || extended
                    ? H("Presione ", B("Log NC"),
                        repair ? " en el código general de reparación rápida: " : " en el código general de reparación extendida: ",
                        B(finding?.Code ?? string.Empty), ".")
                    : H("Presione ", B("Log NC"), " en el código del defecto del hallazgo: ",
                        B(finding?.Description ?? string.Empty), "."));
        }

        // Reparación rápida: el componente, su Browse, Add-Done, las confirmaciones de Remove y
        // Add Component y el banner.
        private static void AddFastRepair(StepScript s, NcFinding finding)
        {
            // Spec 014 (punto 10): Browse con el componente del hallazgo y la secuencia de
            // Remove component y Add Component, que se envía con Add (un solo postback).
            string component = finding?.ComponentPartNumber ?? string.Empty;
            string picked = Anchor("nc-component") + Attr("data-picked", component);
            s.Add("component", GuideMode.Click, Anchor("nc-component-browse"),
                html: H("Presione ", B("⧉"), " en Component para buscar el componente a reemplazar."));
            // Un subpaso con el resaltado en dos tiempos: las filas hasta que la del hallazgo
            // queda seleccionada, después OK (el motor vuelve a ubicar el ancla en cada tick).
            string rowPicked = ":has(" + Anchor("nc-browse-row") + ".selected" + Attr("data-component", component) + ")";
            string browse = Anchor("nc-browse");
            s.Add("browse", GuideMode.Click,
                browse + ":not(" + rowPicked + ") " + Anchor("nc-browse-list") + ", " +
                browse + rowPicked + " " + Anchor("nc-browse-ok"),
                done: new[] { picked },
                html: H("Seleccione el componente a reemplazar y dele ", B("OK"), "."));
            s.Add("addDone", GuideMode.Click, Anchor("nc-add-done"),
                html: H("Presione ", B("Add-Done"), " para registrar el NC."));
            s.Add("removeYes", GuideMode.Click, Anchor("nc-remove-yes"),
                html: H("Presione ", B("Yes"), " para quitar el componente de la pieza."));
            s.Add("removeScrap", GuideMode.Click, Anchor("nc-remove-scrap"),
                html: H("Presione ", B("Scrap"), ": el componente dañado se desecha."),
                note: H("Return aplica solo a ciertos componentes y según la operación (lo indica el MPI); ",
                    "no es común. En la mayoría de los casos se usa Scrap."));
            s.Add("addYes", GuideMode.Click, Anchor("nc-add-yes"),
                html: H("Presione ", B("Yes"), " para agregar el componente nuevo."),
                note: string.Join("<br>",
                    H(B("Yes"), ": agrega ahora el componente nuevo."),
                    H(B("No"), ": la pieza queda sin el componente y hay que volver a ensamblarlo; lo mismo pasa con Return.")));
            s.Add("addComponent", GuideMode.Click, Anchor("nc-addcomp-add"), postback: true,
                free: new[] { Anchor("nc-addcomp-batch") }, retry: "addComponent",
                html: H("Complete el ", B("Batch Number"), " del componente nuevo (o los datos que pida su material) y presione ",
                    B("Add"), "."));
            s.Add("banner", GuideMode.Info, Anchor("pod-banner"), hide: true,
                html: H("Verifique el mensaje: el componente se reemplazó y la pieza sigue en el POD."));
        }

        // Reparación extendida: Comments si el código lo exige, Add-Done, la disposición y el
        // banner.
        private static void AddExtendedRepair(StepScript s, PodContext ctx, NcFinding finding, bool comments)
        {
            // Spec 014 (punto 14): Comments si el código lo exige (P-13; formato), Add-Done
            // abre en el cliente Select Disposition Function y Select envía la disposición.
            if (comments)
            {
                // Por qué el código exige Comments antes de Add-Done (manual del MES; usuario,
                // 2026-10-19).
                s.Add("commentsInfo", GuideMode.Info, Anchor("nc-comments"),
                    html: H("El código ", B(finding.Code), " exige ", B("Comments"), " antes de Add-Done. ",
                        "Se usa en operaciones cuyo procedimiento permite el retrabajo: anote el número de ",
                        B("MPI"), " y la revisión a la que se refiere. Si el producto está afectado por un ",
                        B("NCR"), ", anote el número de NCR como referencia."));
                s.Add("comments", GuideMode.Capture, Anchor("nc-comments"), format: GuideFormat.Required,
                    html: string.IsNullOrEmpty(finding.Mpi)
                        ? H("Anote el MPI y su revisión, o el número de NCR.")
                        : H("Anote el MPI y su revisión, o el número de NCR: ", B(finding.Mpi), "."));
            }
            s.Add("addDone", GuideMode.Click, Anchor("nc-add-done"),
                html: H("Presione ", B("Add-Done"), " para registrar el NC."));
            bool returnToPrevious = finding?.Disposition == NcDisposition.ReturnToPrevious;
            string disposition = returnToPrevious ? "Return to any previous" : "Local Rework";
            s.Add("disposition", GuideMode.Click,
                Anchor("nc-disposition") + Attr("data-disposition", returnToPrevious
                    ? LogNCAnswer.DispositionReturnToPrevious : LogNCAnswer.DispositionLocalRework),
                postback: true,
                html: H("Presione ", B("Select"), " en ", B(disposition), ", la disposición del hallazgo."));
            s.Add("banner", GuideMode.Info, Anchor("pod-banner"), hide: true,
                html: returnToPrevious
                    ? H("Verifique el mensaje: la pieza se procesará en ", B(ctx.PreviousOperationName), " y salió del POD.")
                    : H("Verifique el mensaje: la pieza se retrabaja en esta operación y sigue en el POD."));
        }

        // Desecho (scrap): Add-Done envía y el banner confirma la baja de las unidades.
        private static void AddScrap(StepScript s)
        {
            s.Add("addDone", GuideMode.Click, Anchor("nc-add-done"), postback: true,
                html: H("Presione ", B("Add-Done"), " para registrar el NC."));
            s.Add("banner", GuideMode.Info, Anchor("pod-banner"), hide: true,
                html: H("Verifique el mensaje de ", B("scrap"), ": las unidades se dieron de baja."));
        }
    }
}
