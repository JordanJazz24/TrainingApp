using System;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Auth
{
    // Bloqueo por logins fallidos (spec 013): 5 fallos seguidos bloquean la cuenta 15 minutos.
    // Se desbloquea sola al vencer, antes por un admin, o con un reset de contraseña. El
    // contador vuelve a 0 con un login correcto o al bloquear (P-1: al vencer hay otros 5
    // intentos). El SP solo aplica estos valores.
    public static class LoginLockPolicy
    {
        public const int MaxFailedLogins = 5;

        public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

        public static bool IsLocked(User user, DateTime nowUtc)
            => user.LockedUntil.HasValue && user.LockedUntil.Value > nowUtc;
    }
}
