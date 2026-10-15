using System;

namespace TrainingApp.Domain
{
    // Roles excluyentes (spec 013): el operador entrena; el administrador administra y no
    // entrena. Qué puede hacer cada uno lo decide AccessPolicy en la BLL.
    public enum UserRole
    {
        Operator      = 1,
        Administrator = 2
    }

    // Cuenta local de un Employee de la API (primer acceso completado o en curso). Antes se
    // llamaba Operator; "operador" queda como término del MES y como rol (D-17).
    public class User
    {
        public int Id { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public bool IsFirstLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserRole Role { get; set; } = UserRole.Operator;

        // Último login correcto (UTC); null = nunca entró.
        public DateTime? LastLoginAt { get; set; }

        // Logins fallidos seguidos y fin del bloqueo (UTC; null o en el pasado = no bloqueada).
        public int FailedLoginCount { get; set; }
        public DateTime? LockedUntil { get; set; }
    }
}
