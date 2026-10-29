using System;
using System.Linq;

namespace TrainingApp.UI.Master
{
    /// <summary>
    /// Textos de la barra superior que comparten Admin.Master y Site.Master (spec 022, brief 0).
    /// Solo presentación: no lee la Session ni la BD.
    /// </summary>
    internal static class TopBar
    {
        /// <summary>
        /// Iniciales de la cuenta: la primera letra de las dos primeras palabras del nombre
        /// ("Mariela Rojas Quesada" → "MR"), o los dos primeros caracteres si es una sola
        /// palabra (el Code1 cuando no hay nombre).
        /// </summary>
        public static string Initials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            var words = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1)
                return words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpperInvariant();

            return string.Concat(words.Take(2).Select(w => char.ToUpperInvariant(w[0])));
        }

        /// <summary>
        /// El title de la cuenta: repite lo que la barra oculta en pantallas angostas.
        /// </summary>
        public static string AccountTitle(string name, string detail)
        {
            return string.IsNullOrEmpty(detail) ? name : name + " · " + detail;
        }
    }
}
