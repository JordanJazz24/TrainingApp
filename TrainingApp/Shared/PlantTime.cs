using System;

namespace TrainingApp.Shared
{
    // Hora de planta (Costa Rica, UTC-6 sin horario de verano), sin depender de la zona horaria
    // configurada en el servidor. La BD guarda UTC; la usan las páginas para mostrar fechas y el
    // dashboard para armar las semanas de la planta (spec 018).
    public static class PlantTime
    {
        private static readonly TimeZoneInfo PlantTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");

        public static DateTime ToLocal(DateTime utc)
            => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), PlantTimeZone);
    }
}
