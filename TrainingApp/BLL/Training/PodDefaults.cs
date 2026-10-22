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

        // 12NC simulado del material del área (W23, D-121): "3000" + el Id del área con 8
        // dígitos. Es el prefijo de los SFC Serialized; se reemplaza por el real cuando negocio
        // lo dé. Cambia entre BDs si el Id del área cambia (D-125).
        public static string MaterialNumber(Area area) => "3000" + area.Id.ToString("D8");

        // As-Built Configuration de Remove component y Add Component (spec 014, P-11). El
        // material es el 12NC (prefijo de los SFC, como en el MES real; D-123); Routing y BOM
        // son simulados (SIM, D-128). La operación es la actual de las Operations del área
        // (D-58); SimOperation es el respaldo de un área sin Operations.
        public static string MaterialVersion(SimulatedOrder order)
            => string.IsNullOrEmpty(order?.MaterialNumber) ? string.Empty : order.MaterialNumber + "/A";

        public static string SimRouting(Area area) => "PR_" + Code(area) + "_SIM/A-1";

        public static string SimBom(SimulatedOrder order)
            => order == null ? string.Empty : "SIM-" + order.ShopOrderInput + "-1/A-1";

        public static string SimOperation(Area area) => "O_" + Code(area) + "_SIM";

        private static string Code(Area area) => (area.Name ?? string.Empty).Trim().ToUpperInvariant().Replace(' ', '_');
    }
}
