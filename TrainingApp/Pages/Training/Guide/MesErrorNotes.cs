using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using TrainingApp.BLL.Training;

namespace TrainingApp.Pages.Training.Guide
{
    // Explicación en español de cada mensaje del MES (spec 017, D-102): la nota de la guía de
    // Guided y el "último fallo" de Ver pista en Práctica libre. Es presentación: Assessment no
    // la recibe. Textos literales de la tabla "Explicaciones en español" de la spec (D-114); lo
    // que va entre ** se muestra en negrita.
    public static class MesErrorNotes
    {
        private const string SfcStarted =
            "El MES necesita un **SFC iniciado**: seleccione o escanee el SFC y presione **Start** antes de esta acción.";
        private const string OutOfLimits =
            "El valor está **fuera de los límites** del MPI. La pantalla del MES no muestra los límites: revise la medición contra el MPI.";
        private const string SplitQty =
            "Revise la cantidad a separar. Al presionar **OK** el MES cierra la ventana: abra **SFC Split** otra vez.";

        private static readonly IDictionary<MesMessageId, string> Notes = new Dictionary<MesMessageId, string>
        {
            [MesMessageId.ResourceSetupMissing]   = "Falta el **Resource Setup** de la estación: hágalo antes de iniciar.",
            [MesMessageId.LineClearanceNotDone]   = "El MES no deja continuar porque falta el **Line Clearance** después del Resource Setup. Regístrelo antes de iniciar, ensamblar o recolectar datos.",
            [MesMessageId.LineClearanceStatusNo]  = "El último Line Clearance quedó en **NO**: el MES bloquea el proceso hasta que la línea esté liberada. Repita el Line Clearance.",
            [MesMessageId.SfcMissingStart]        = SfcStarted,
            [MesMessageId.SfcMissing]             = SfcStarted,
            [MesMessageId.InvalidSfc]             = "Ese SFC **no existe** en el MES: revise la etiqueta y escanee el SFC del lote.",
            [MesMessageId.MissingDataCollection]  = "Faltan parámetros de **Data Collection** obligatorios: recolecte todos los grupos antes de **Complete**.",
            [MesMessageId.BarcodeNotCompliant]    = "El barcode no tiene el formato del MES: escanee la etiqueta del componente.",
            [MesMessageId.RequiredValueMissing]   = "Falta un valor obligatorio. El MES avisa **un parámetro a la vez**: complete el que indica y guarde otra vez.",
            [MesMessageId.FieldMustBeNumber]      = "Ese parámetro es numérico: escriba solo números.",
            [MesMessageId.ValueAboveUpperLimit]   = OutOfLimits,
            [MesMessageId.ValueBelowLowerLimit]   = OutOfLimits,
            [MesMessageId.SplitQtyMissing]        = SplitQty,
            [MesMessageId.SplitQtyAboveAvailable] = SplitQty,
            [MesMessageId.SplitQtyNotPositive]    = SplitQty
        };

        private static readonly Regex Bold = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled);

        public static bool Has(MesMessageId id) => Notes.ContainsKey(id);

        // Para el globo de la guía: codificada, con <strong> en lo marcado.
        public static string Html(MesMessageId id)
            => Bold.Replace(WebUtility.HtmlEncode(Notes[id]), "<strong>$1</strong>");

        // Para Ver pista (texto plano).
        public static string Plain(MesMessageId id) => Notes[id].Replace("**", string.Empty);

        // Ancla de la superficie donde se ve el mensaje (spec 017, enmienda 1 del plan): el
        // banner del POD o la franja de su ventana.
        public static string AnchorName(MesMessageId id)
        {
            switch (id)
            {
                case MesMessageId.BarcodeNotCompliant:
                    return "asm-mes-error";
                case MesMessageId.RequiredValueMissing:
                case MesMessageId.FieldMustBeNumber:
                case MesMessageId.ValueAboveUpperLimit:
                case MesMessageId.ValueBelowLowerLimit:
                    return "dc-mes-error";
                case MesMessageId.SplitQtyMissing:
                case MesMessageId.SplitQtyAboveAvailable:
                case MesMessageId.SplitQtyNotPositive:
                    return "split-mes-error";
                default:
                    return "pod-mes-error";
            }
        }
    }
}
