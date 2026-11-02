using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Lo que muestra el inicio del operador (spec 021, D-167/D-168): su área como texto y las
    // guías publicadas de esa área. Sin área: HasArea false, AreaText null y sin guías.
    public class OperatorStartOptions
    {
        public bool HasArea { get; set; }
        public string AreaText { get; set; }
        public IList<TrainingGuide> Guides { get; set; } = new List<TrainingGuide>();

        // Presentación (spec 022, E8): "1 guía" / "N guías" para la etiqueta del selector.
        public string GuideCountText =>
            Guides.Count == 1 ? "1 guía" : Guides.Count + " guías";

        // El Inicio con todo elegido (spec 025, P-9, D-337): ?mode=<0-2>&guide=<Id> de las acciones
        // del fin de sesión. Valida cada valor POR SEPARADO y los inválidos o ausentes se ignoran
        // sin aviso (como un filtro inválido): el modo, si es un SessionMode definido (B-4); la
        // guía, si es una de las iniciables del operador (publicada y de su área). Con un modo
        // válido y una guía inválida queda elegido solo el modo. Solo preselecciona: Iniciar sigue
        // siendo el único camino que crea la sesión, y PodService.StartSession vuelve a validar.
        public StartPreselection Preselect(string mode, string guide)
        {
            var result = new StartPreselection();
            if (int.TryParse(mode, NumberStyles.None, CultureInfo.InvariantCulture, out int modeValue)
                && Enum.IsDefined(typeof(SessionMode), modeValue))
                result.Mode = (SessionMode)modeValue;
            if (int.TryParse(guide, NumberStyles.None, CultureInfo.InvariantCulture, out int guideId)
                && Guides.Any(g => g.Id == guideId && g.IsPublished))
                result.GuideId = guideId;
            return result;
        }
    }

    // Lo que el Inicio deja elegido por la URL; cada valor es null si no valió.
    public class StartPreselection
    {
        public SessionMode? Mode { get; set; }
        public int? GuideId { get; set; }
    }
}
