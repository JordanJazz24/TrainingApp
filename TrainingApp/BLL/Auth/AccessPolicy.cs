using TrainingApp.Domain;

namespace TrainingApp.BLL.Auth
{
    // Por qué login entra el usuario (spec 013): el de SAP NetWeaver es parte de la
    // simulación y es para entrenar; el administrativo, para administrar.
    public enum LoginPortal
    {
        Training,
        Admin
    }

    // Mapa rol → capacidades (spec 013), en un solo lugar: las páginas y los servicios
    // preguntan por la capacidad, nunca por el nombre del rol. Los roles son excluyentes: el
    // administrador no entrena.
    public static class AccessPolicy
    {
        public static bool CanTrain(UserRole role) => role == UserRole.Operator;

        public static bool CanManageUsers(UserRole role) => role == UserRole.Administrator;

        // Cada login acepta solo a quien tiene la capacidad de ese portal.
        public static bool CanUsePortal(UserRole role, LoginPortal portal)
            => portal == LoginPortal.Admin ? CanManageUsers(role) : CanTrain(role);
    }
}
