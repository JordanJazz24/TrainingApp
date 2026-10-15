using System;
using System.Collections.Generic;
using System.Text;
using TrainingApp.BLL.Scoring;

namespace TrainingApp.BLL.Training
{
    // Equipos "del turno o lote anterior" que Retrieve muestra en Resource Setup (spec 015, A):
    // el operador tiene que quitarlos antes de guardar. Son datos de escenario, no catálogo.
    [Serializable]
    public class PreloadedEquipment
    {
        public string ShopOrder { get; set; }

        public List<string> ToolNumbers { get; set; } = new List<string>();

        public IList<string> AllValues()
        {
            var values = new List<string> { ShopOrder };
            values.AddRange(ToolNumbers);
            return values;
        }

        public bool Contains(string value) => PreloadedEquipmentRule.IsPreloaded(AllValues(), value);
    }

    // Como SerialSfcGenerator: una Shop Order de 9 dígitos y 2 herramientas de 8, aleatorias y
    // distintas de la orden por defecto y de los ejemplos de la guía (el operador que escribe
    // el ejemplo no debe dar con un precargado por casualidad).
    public static class PreloadedEquipmentGenerator
    {
        public const int ToolCount = 2;

        private static readonly string[] Reserved =
            { PodDefaults.DefaultShopOrder, "10033058", "10087734" };

        public static PreloadedEquipment Generate(Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var used = new HashSet<string>(Reserved);
            var equipment = new PreloadedEquipment
            {
                ShopOrder = Unique(used, () => "3" + RandomDigits(random, 8))
            };
            while (equipment.ToolNumbers.Count < ToolCount)
                equipment.ToolNumbers.Add(Unique(used, () => "100" + RandomDigits(random, 5)));
            return equipment;
        }

        private static string Unique(HashSet<string> used, Func<string> next)
        {
            string value;
            do { value = next(); } while (!used.Add(value));
            return value;
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
