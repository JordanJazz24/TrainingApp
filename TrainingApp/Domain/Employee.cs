namespace TrainingApp.Domain
{
    // Read from external API (https://miappcr.com/api/basico/generalData/allUser/basicInfo).
    // Never stored in the local DB — used only during auth to validate code1.
    public class Employee
    {
        public string Code1 { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string FullName => $"{Nombre} {Apellidos}".Trim();
    }
}
