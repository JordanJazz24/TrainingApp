using System;

namespace TrainingApp.Shared
{
    // Hora de planta (Costa Rica, UTC-6 sin horario de verano), sin depender de la zona horaria
    // configurada en el servidor. La BD guarda UTC; la usan las páginas para mostrar fechas y el
    // dashboard para armar los períodos y las semanas de la planta (specs 018 y 023).
    public static class PlantTime
    {
        private static readonly TimeZoneInfo PlantTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");

        public static DateTime ToLocal(DateTime utc)
            => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), PlantTimeZone);

        // Una hora de la planta en UTC (la medianoche de un día, para cortar los períodos).
        public static DateTime FromLocal(DateTime local)
            => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), PlantTimeZone);

        // El día de hoy en la planta.
        public static DateTime Today(DateTime nowUtc) => ToLocal(nowUtc).Date;
    }
}
