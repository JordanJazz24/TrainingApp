using System;
using System.Collections.Generic;
using System.Linq;

namespace TrainingApp.BLL.Scoring
{
    // Spec 015: un valor de Resource Setup es un equipo precargado si coincide con alguno por
    // valor, sin espacios. Una sola regla para StepValidator, PodService y el control; el JS
    // del control y de la guía quita el mismo conjunto ([ \t\r\n]: \s de JS y
    // char.IsWhiteSpace no coinciden).
    public static class PreloadedEquipmentRule
    {
        public static bool IsPreloaded(IEnumerable<string> preloadedValues, string value)
        {
            if (preloadedValues == null) return false;
            string normalized = Normalize(value);
            return preloadedValues.Any(v => string.Equals(Normalize(v), normalized, StringComparison.Ordinal));
        }

        public static string Normalize(string value)
            => new string((value ?? "").Where(c => c != ' ' && c != '\t' && c != '\r' && c != '\n').ToArray());
    }
}
