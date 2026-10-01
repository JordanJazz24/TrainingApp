using System;
using System.Collections.Generic;

namespace TrainingApp.BLL.Training
{
    // SFC de la fila que crea un SFC Split en una orden NonSerialized (enmienda 2026-10-01 de
    // la spec 005): como en el MES real, el split crea un SFC nuevo de la misma Shop Order.
    // Conserva el prefijo del SFC del lote y reemplaza los últimos dígitos por otros, sin
    // repetir ningún SFC de la orden (ej. CR9AP1700000239 → CR9AP1700000852).
    public static class SplitSfcGenerator
    {
        public const int SuffixDigits = 4;

        public static string Next(string lotSfc, ICollection<string> existingSfcs, Random random)
        {
            if (string.IsNullOrWhiteSpace(lotSfc))
                throw new ArgumentException("El SFC del lote es obligatorio.", nameof(lotSfc));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            string lot    = lotSfc.Trim();
            string prefix = lot.Length > SuffixDigits ? lot.Substring(0, lot.Length - SuffixDigits) : lot + "-";
            var taken     = new HashSet<string>(existingSfcs ?? new string[0], StringComparer.OrdinalIgnoreCase) { lot };

            // 10.000 sufijos posibles por prefijo: una orden simulada nunca se acerca a ese límite.
            while (true)
            {
                string candidate = prefix + random.Next(0, 10000).ToString("D" + SuffixDigits);
                if (taken.Add(candidate))
                    return candidate;
            }
        }
    }
}
