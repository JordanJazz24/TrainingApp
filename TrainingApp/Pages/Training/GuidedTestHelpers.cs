using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training
{
    // Texto y formato del POD que no dependen de la página (AUD-74, E6): sin controles ni Session.
    internal static class GuidedTestHelpers
    {
        // Modal-overlay: el POD queda visible detrás del modal. El resto reemplaza el POD Work List.
        internal static bool IsModalOverlay(StepType stepType) =>
            stepType == StepType.ResourceSetup || stepType == StepType.LineClearance ||
            stepType == StepType.Search        || stepType == StepType.SFC_Split;

        // Fin de sesión (spec 025, D-338): el resultado tiene su propia página; el POD ya no lo muestra.
        internal static string ResultUrl(TrainingSession session) => SessionResultService.ResultUrl(session.Id);

        // Disposición del hallazgo como la dictaría el MPI o el NCR (D-28).
        internal static string FindingDisposition(NcFinding finding)
        {
            switch (finding.Method)
            {
                case NCMethod.FastScrap:
                    return finding.RowId != null ? "desechar la pieza (scrap)" : "desechar las unidades (scrap)";
                case NCMethod.FastRepair:
                    return "reemplazar el componente " + finding.ComponentPartNumber + " (se desecha)";
                case NCMethod.ExtendedRepair:
                    // P-13: con un código que exige Comments, el MPI que se anota.
                    string disposition = finding.Disposition == NcDisposition.ReturnToPrevious
                        ? "regresar la pieza a una operación anterior (Return to any previous)"
                        : "retrabajar la pieza en esta operación (Local Rework)";
                    return string.IsNullOrEmpty(finding.Mpi) ? disposition : disposition + ", según " + finding.Mpi;
                default:
                    return string.Empty;
            }
        }

        internal static string MesErrorJs(MesMessageId id)
            => HttpUtility.JavaScriptStringEncode(MesMessage.Get(id).Format());

        // Texto del intento fallido que decidió la BLL sin pasar por el validador (Guided lo
        // muestra en lugar del hint; Assessment muestra los intentos restantes).
        internal static string AttemptFeedback(PodAttemptReason reason, Area area)
        {
            bool serialized = area.SerializationType == SerializationType.Serialized;
            switch (reason)
            {
                // En español y de usted, como los hints de Guided (spec 010, P-6 y P-8).
                case PodAttemptReason.NoScrappableRow:
                    return serialized
                        ? "Seleccione una fila para scrapear"
                        : "Seleccione una fila creada por el split para scrapear";
                case PodAttemptReason.NoRowToComplete:
                    return serialized ? "Seleccione todas las filas para completar" : "Seleccione 1 fila para completar";
                default:
                    return null;
            }
        }

        // Guided: hint del paso para que el operador aprenda. Assessment: no se revela la
        // respuesta, solo cuántos intentos con puntaje le quedan antes del avance forzado.
        internal static string BuildIncorrectFeedback(PodContext ctx, StepType stepType,
            int remainingScoredAttempts, string incorrectFeedback)
        {
            var policy = ctx.Policy;
            if (policy.FailureFeedback == FailureFeedbackKind.RemainingAttempts)
            {
                return remainingScoredAttempts > 0
                    ? string.Format("Respuesta incorrecta. Te quedan {0} intento(s) con puntaje.", remainingScoredAttempts)
                    : "Respuesta incorrecta. Si vuelves a fallar, el paso se marca fallido y avanza.";
            }

            // Práctica libre (P-6): sin intentos restantes; la pista se pide con Ver pista.
            if (policy.FailureFeedback == FailureFeedbackKind.Incorrect)
                return "Respuesta incorrecta.";

            if (!string.IsNullOrEmpty(incorrectFeedback)) return incorrectFeedback;
            string hint = StepHints.For(ctx, stepType);
            return !string.IsNullOrEmpty(hint) ? hint : "Intente de nuevo.";
        }

        // "000123-430 (2/3)": pieza actual y posición dentro de las piezas del paso.
        internal static string DescribeCurrentPiece(DataCollectionProgress progress)
        {
            if (progress == null || progress.CurrentPiece == null) return "—";
            return string.Format("{0} ({1}/{2})",
                progress.CurrentPiece.SFCCode, progress.CurrentIndex + 1, progress.Pieces.Count);
        }

        internal static HashSet<string> ParseRowIds(string value)
        {
            return new HashSet<string>((value ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim()));
        }

        // Texto de TxtSFC para una selección (spec 008): el SFC si es una fila, "[N SELECTED]"
        // si son varias (igual que el POD real). selectWorkListRow hace lo mismo en el cliente.
        internal static string DescribeSelection(IList<SimulatedOrderRow> rows)
        {
            if (rows.Count == 0) return string.Empty;
            return rows.Count == 1 ? rows[0].SFCCode : "[" + rows.Count + " SELECTED]";
        }

        // Nombre del modo en el header y la barra del simulador, con su badge.
        internal static (string Label, string BadgeClass) ModeLabel(ModePolicy policy)
        {
            switch (policy.Mode)
            {
                case SessionMode.Guided:     return ("Guided", "badge badge-guided");
                case SessionMode.Assessment: return ("Assessment", "badge badge-assessment");
                case SessionMode.FreePractice: return ("Práctica libre", "badge badge-practice");
                default: throw new ArgumentOutOfRangeException(nameof(policy), policy.Mode, "Modo sin etiqueta.");
            }
        }

        internal static (string Title, string Text) NoticeText(PodNotice notice)
        {
            switch (notice)
            {
                case PodNotice.ResourceSetupRequired:
                    return ("Resource Setup requerido", "Debes completar Resource Setup antes de iniciar.");
                case PodNotice.SelectAllPieces:
                    return ("Selecciona todas las piezas",
                        "En el simulador se trabajan todas las piezas juntas: selecciona todas las filas.");
                // Spec 011, punto 6: un solo aviso para el escaneo y Browse Work List.
                case PodNotice.StartWholeOrder:
                    return ("Inicia la orden completa",
                        "En esta guía se trabaja la orden completa: escanea la Shop Order o selecciona todas las piezas en Browse Work List.");
                case PodNotice.SplitRequired:
                    return ("Split requerido", "Debes completar al menos un split antes de cerrar el paso.");
                case PodNotice.SplitNotActive:
                    return ("SFC Split", "SFC Split no es el paso activo.");
                case PodNotice.InvalidSplit:
                    return ("SFC Split", "La fila seleccionada ya no es válida.");
                case PodNotice.InvalidSplitQty:
                    return ("SFC Split", "La cantidad a dividir debe ser un número entero menor que la Qty disponible.");
                case PodNotice.NoPieceLeft:
                    return ("Selección inválida", "Debe quedar al menos una pieza para Complete.");
                case PodNotice.SplitAlreadyDone:
                    return ("SFC Split", "Ya se separó la cantidad del hallazgo.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(notice), notice, null);
            }
        }

        internal static string FormatScore(decimal score)
            => (score * 100m).ToString("F1") + "%";
    }
}
