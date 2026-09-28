using System;
using System.Collections.Generic;
using System.Text;

namespace TrainingApp.BLL.Training
{
    // Piezas de una Shop Order serializada simulada (spec 008): entre 3 y 5 SFCs
    // "{ShopOrder}-{sufijo}", con un sufijo numérico aleatorio de 1 a 6 dígitos que no se
    // repite dentro de la orden (ej. 000123-7, 000123-430, 000123-048213).
    public static class SerialSfcGenerator
    {
        public const int MinPieces       = 3;
        public const int MaxPieces       = 5;
        public const int MinSuffixDigits = 1;
        public const int MaxSuffixDigits = 6;

        public static IList<string> Generate(string shopOrder, Random random)
        {
            if (string.IsNullOrWhiteSpace(shopOrder))
                throw new ArgumentException("La Shop Order es obligatoria.", nameof(shopOrder));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            int count    = random.Next(MinPieces, MaxPieces + 1);
            var suffixes = new HashSet<string>();
            var result   = new List<string>(count);

            while (result.Count < count)
            {
                string suffix = RandomDigits(random, random.Next(MinSuffixDigits, MaxSuffixDigits + 1));
                if (suffixes.Add(suffix))
                    result.Add(shopOrder.Trim() + "-" + suffix);
            }
            return result;
        }

        private static string RandomDigits(Random random, int length)
        {
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append((char)('0' + random.Next(0, 10)));
            return sb.ToString();
        }
    }
}
