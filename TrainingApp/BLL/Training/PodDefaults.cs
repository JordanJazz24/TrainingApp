using System;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Datos de simulación del POD, iguales para todas las áreas (spec 009, D-11). No son
    // catálogo: si un área llega a necesitar valores propios, pasan a la BD.
    public static class PodDefaults
    {
        // Shop Order genérica de la orden por defecto (Resource Setup forzado, S2 / spec 008 E8).
        public const string DefaultShopOrder = "303406950";

        // Fila que deja un SFC Split forzado (auditoría D-1).
        public const int DefaultSplitQty = 100;

        public const int NonSerializedLotQty = 250;
        public const int SerializedPieceQty  = 1;

        public const string Site = "CR9A";

        // SFC del lote NonSerialized: prefijo + 8 dígitos de la Shop Order.
        public const string SfcPrefix = "CR9A";

        // La pregunta de Line Clearance es la misma en todas las operaciones (auditoría D-6).
        public const string LineClearanceQuestion = "¿La limpieza de linea se ejecuto?";

        public static string ResourceName(Area area) => area.Name + "-01";

        public static string ProcessLot(DateTime day) => "LOT-" + day.ToString("yyyyMMdd");

        public static string MaterialDescription(Product product, Area area)
            => (product?.Name ?? "") + " / " + area.Name;
    }
}
