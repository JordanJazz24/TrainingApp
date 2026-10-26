using TrainingApp.Domain;

namespace TrainingApp.BLL.Auth
{
    // Mapa rol → capacidades (spec 013), en un solo lugar: las páginas y los servicios
    // preguntan por la capacidad, nunca por el nombre del rol. Los roles son excluyentes: el
    // administrador no entrena. Los dos entran por el mismo login (spec 016, D-62).
    public static class AccessPolicy
    {
        public static bool CanTrain(UserRole role) => role == UserRole.Operator;

        public static bool CanManageUsers(UserRole role) => role == UserRole.Administrator;

        // Dashboard de Training (spec 018): hoy solo el administrador. Un rol Trainer futuro
        // tendría esta capacidad sin CanManageUsers.
        public static bool CanViewReports(UserRole role) => role == UserRole.Administrator;

        // Configurar las guías, hoy su tipo de Resource Setup (spec 020).
        public static bool CanManageGuides(UserRole role) => role == UserRole.Administrator;
    }
}
