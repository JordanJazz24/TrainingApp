using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Auth;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Quién puede iniciar qué guía y qué áreas puede tener un operador (spec 021). Pura, sin
    // repositorios, para probarla con datos en memoria (P-8): PodService y OperatorAreaService
    // le pasan lo que leyeron de la BD.
    public static class TrainingAccessRule
    {
        // Un inicio que no cumple es un envío manipulado o una lista vieja: excepción, no
        // intento (D-173, P-7). Solo decide qué se puede iniciar; una sesión en curso de otra
        // área se puede terminar (D-174). Un modo fuera del enum también (B-4): antes se
        // abandonaba la sesión en curso y quedaba una sesión que fallaba al cargar.
        public static void EnsureCanStart(User user, TrainingGuide guide, SessionMode mode)
        {
            var blocker = StartBlocker(user, guide, mode);
            if (blocker != null) throw blocker;
        }

        // Si el operador puede iniciar la guía hoy (publicada y de su área), sin lanzar: lo usa
        // el fin de sesión para decidir si ofrece Repetir y una acción de modo (spec 025, P-8).
        // Las mismas condiciones que EnsureCanStart: viven en StartBlocker, una sola vez.
        public static bool IsStartable(User user, TrainingGuide guide)
            => StartBlocker(user, guide, SessionMode.Guided) == null;

        // Por qué no se puede iniciar (la excepción que lanza EnsureCanStart), o null si se puede.
        private static Exception StartBlocker(User user, TrainingGuide guide, SessionMode mode)
        {
            if (user == null || !AccessPolicy.CanTrain(user.Role))
                return new UnauthorizedAccessException("El usuario no puede entrenar.");
            if (!Enum.IsDefined(typeof(SessionMode), mode))
                return new ArgumentOutOfRangeException(nameof(mode), mode, "Modo de sesión inválido.");
            if (guide == null)
                return new ArgumentException("La guía no existe.", nameof(guide));
            if (!guide.IsPublished)
                return new InvalidOperationException("La guía no está publicada.");
            if (user.AreaId == null)
                return new InvalidOperationException("El operador no tiene área.");
            if (guide.AreaId != user.AreaId.Value)
                return new InvalidOperationException("La guía es de otra área.");
            return null;
        }

        // Elegible = con al menos una guía publicada (D-176); la lista la calcula spAreaGetEligible.
        public static bool IsEligible(int areaId, IEnumerable<EligibleArea> eligible)
        {
            return eligible != null && eligible.Any(a => a.AreaId == areaId);
        }
    }
}
