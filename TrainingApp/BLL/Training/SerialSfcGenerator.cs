using System;
using System.Collections.Generic;

namespace TrainingApp.BLL.Training
{
    // Piezas de una Shop Order serializada simulada (spec 008): entre 4 y 5 SFCs (spec 014)
    // "{12NC}-{serial}", como en el MES real (W23, D-122): el 12NC del material y seriales
    // consecutivos. El inicio tiene de 1 a 6 dígitos sin ceros a la izquierda (D-126) y la
    // última pieza no pasa de 999999 (ej. 300000000003-7 … -11, 300000000003-46705 … -46709).
    public static class SerialSfcGenerator
    {
        public const int MinPieces       = 4;
        public const int MaxPieces       = 5;
        public const int MinSerialDigits = 1;
        public const int MaxSerialDigits = 6;
        public const int MaxSerial       = 999999;

        public static IList<string> Generate(string materialNumber, Random random)
        {
            if (string.IsNullOrWhiteSpace(materialNumber))
                throw new ArgumentException("El 12NC es obligatorio.", nameof(materialNumber));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            int count  = random.Next(MinPieces, MaxPieces + 1);
            int digits = random.Next(MinSerialDigits, MaxSerialDigits + 1);
            int min    = digits == 1 ? 1 : Pow10(digits - 1);
            int start  = Math.Min(random.Next(min, Pow10(digits)), MaxSerial - (count - 1));

            var result = new List<string>(count);
            for (int i = 0; i < count; i++)
                result.Add(materialNumber.Trim() + "-" + (start + i));
            return result;
        }

        private static int Pow10(int exponent)
        {
            int value = 1;
            for (int i = 0; i < exponent; i++)
                value *= 10;
            return value;
        }
    }
}
