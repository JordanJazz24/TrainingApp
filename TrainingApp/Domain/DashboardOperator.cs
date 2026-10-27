using System;

namespace TrainingApp.Domain
{
    // Un usuario con rol Operator, para el dashboard (spec 018, DA-9): también los que nunca
    // entrenaron.
    public class DashboardOperator
    {
        public int UserId { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }

        // Su área, solo para mostrarla (spec 021, D-175); null si no tiene.
        public string ProductName { get; set; }
        public string AreaName { get; set; }
    }
}
