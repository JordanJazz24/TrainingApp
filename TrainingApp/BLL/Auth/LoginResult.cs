using System;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Auth
{
    public enum LoginStatus
    {
        Success,
        FirstLogin,
        InvalidCredentials,
        EmployeeNotFound,

        // Cuenta bloqueada por logins fallidos (spec 013), hasta LockedUntil. No dice si la
        // contraseña era correcta.
        Locked
    }

    public class LoginResult
    {
        public LoginStatus Status { get; set; }
        public User User { get; set; }

        // Locked: fin del bloqueo (UTC).
        public DateTime? LockedUntil { get; set; }

        // Success y FirstLogin: el último acceso ANTERIOR a este login (UTC); null = primer
        // acceso (spec 013, detección del reclamo de cuenta ajena).
        public DateTime? PreviousLoginAt { get; set; }

        public static LoginResult NotFound() =>
            new LoginResult { Status = LoginStatus.EmployeeNotFound };

        public static LoginResult Invalid() =>
            new LoginResult { Status = LoginStatus.InvalidCredentials };

        public static LoginResult Locked(DateTime lockedUntil) =>
            new LoginResult { Status = LoginStatus.Locked, LockedUntil = lockedUntil };

        public static LoginResult AsFirstLogin(User user, DateTime? previousLoginAt) =>
            new LoginResult { Status = LoginStatus.FirstLogin, User = user, PreviousLoginAt = previousLoginAt };

        public static LoginResult Ok(User user, DateTime? previousLoginAt) =>
            new LoginResult { Status = LoginStatus.Success, User = user, PreviousLoginAt = previousLoginAt };
    }
}
