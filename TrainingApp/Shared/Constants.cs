namespace TrainingApp.Shared
{
    public static class Constants
    {
        public const string DEFAULT_PASSWORD = "Sanjose1!";

        public const string EmployeeApiUrl =
            "https://miappcr.com/api/basico/generalData/allUser/basicInfo";

        // Minutes before the employee list cache is refreshed from the API
        public const int EmployeeCacheMinutes = 30;

        // Espera máxima de una descarga de la API de empleados (hoy tarda 0,5-1,6 s).
        public const int EmployeeApiTimeoutSeconds = 10;

        // Si la API falló al refrescar, minutos hasta el siguiente intento (mientras tanto se
        // sigue sirviendo la última lista).
        public const int EmployeeApiRetryMinutes = 1;

        public static class SessionKeys
        {
            public const string UserId     = "UserId";
            public const string Code1      = "Code1";
            public const string Name       = "Name";

            // true mientras el operador no cambió la contraseña del primer acceso (AUD-08).
            public const string IsFirstLogin = "IsFirstLogin";

            // Estado del POD: orden simulada y avance de Assembly y DC (spec 009). Solo lo usa
            // SessionPodStateStore. Clave completa: PodStatePrefix + TrainingSession.Id
            public const string PodStatePrefix = "GDT_Pod_";
        }
    }
}
