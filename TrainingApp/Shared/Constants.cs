namespace TrainingApp.Shared
{
    public static class Constants
    {
        public const string DEFAULT_PASSWORD = "Sanjose1!";

        public const string EmployeeApiUrl =
            "https://miappcr.com/api/basico/generalData/allUser/basicInfo";

        // Minutes before the employee list cache is refreshed from the API
        public const int EmployeeCacheMinutes = 30;

        public static class SessionKeys
        {
            public const string OperatorId = "OperatorId";
            public const string Code1      = "Code1";
            public const string Name       = "Name";

            // Prefijo de clave de Session para la SimulatedOrder del POD (spec 005).
            // Clave completa: SimulatedOrderPrefix + TrainingSession.Id
            public const string SimulatedOrderPrefix = "GDT_SimOrder_";

            // Avance del paso Assembly (componentes del BOM ya ensamblados, spec 008).
            // Clave completa: AssemblyProgressPrefix + TrainingSession.Id
            public const string AssemblyProgressPrefix = "GDT_AsmProgress_";
        }
    }
}
