using System;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.BLL.Auth
{
    public class AuthService
    {
        private readonly EmployeeRepository _employeeRepo;
        private readonly UserRepository _userRepo;

        public AuthService()
        {
            _employeeRepo = new EmployeeRepository();
            _userRepo = new UserRepository();
        }

        // Login único de la app para los dos roles (spec 016, D-62): la página manda a cada uno
        // a su inicio. Con cuenta: el bloqueo se revisa antes que la contraseña
        // (LoginLockPolicy) y cada fallo suma; el login correcto reinicia el contador y
        // devuelve el último acceso anterior.
        public LoginResult Login(string code1, string password)
        {
            // Step 1: verify code1 exists in external company API
            var employee = _employeeRepo.GetByCode1(code1);
            if (employee == null)
            {
                // Enmascarado: puede ser la contraseña escrita en el campo del Code1.
                AppLog.Write("Login fallido", "Code1 " + AppLog.Mask(code1) + ": no registrado en la API de empleados");
                return LoginResult.NotFound();
            }

            // Después de la API (puede tardar con la caché fría): el bloqueo se mide desde aquí.
            var now = DateTime.UtcNow;

            // Step 2: check for existing local User record
            var user = _userRepo.GetByCode1(code1);

            if (user == null)
            {
                // First access — no local record yet; validate against default password
                if (password != Constants.DEFAULT_PASSWORD)
                {
                    LogFailure(code1, "contraseña incorrecta (primer acceso)");
                    return LoginResult.Invalid();
                }

                // El autorregistro crea siempre un Operator (spec 013, P-3): los admins los
                // crea otro admin o Database/Ops/CreateAdmin.sql.
                user = new User
                {
                    Code1        = employee.Code1,
                    Name         = employee.FullName,
                    PasswordHash = PasswordHelper.Hash(Constants.DEFAULT_PASSWORD),
                    IsFirstLogin = true,
                    CreatedAt    = now,
                    Role         = UserRole.Operator,
                    LastLoginAt  = now
                };
                user.Id = _userRepo.Insert(user);
                return LoginResult.AsFirstLogin(user, previousLoginAt: null);
            }

            // Step 3: existing account. Bloqueada: no se mira la contraseña (no revela si era
            // correcta) y no suma otro fallo.
            if (LoginLockPolicy.IsLocked(user, now))
            {
                LogFailure(code1, "cuenta bloqueada hasta " + user.LockedUntil.Value.ToString("HH:mm") + "Z");
                return LoginResult.Locked(user.LockedUntil.Value);
            }

            if (!PasswordHelper.Verify(password, user.PasswordHash))
            {
                var lockedUntil = _userRepo.RecordLoginFailure(user.Id,
                    LoginLockPolicy.MaxFailedLogins, now.Add(LoginLockPolicy.LockDuration), now);
                if (lockedUntil.HasValue && lockedUntil.Value > now)
                {
                    AppLog.Write("Bloqueo", "Code1 " + AppLog.Clean(code1) + ": " + LoginLockPolicy.MaxFailedLogins +
                        " fallos seguidos, bloqueada hasta " + lockedUntil.Value.ToString("HH:mm") + "Z");
                    return LoginResult.Locked(lockedUntil.Value);
                }
                LogFailure(code1, "contraseña incorrecta");
                return LoginResult.Invalid();
            }

            // El último acceso que ve el usuario es el anterior a este login (AC-17).
            var previousLoginAt = user.LastLoginAt;
            // Otro request pudo bloquearla mientras se revisaba la contraseña: el bloqueo gana.
            if (!_userRepo.RecordLoginSuccess(user.Id, now))
            {
                var current = _userRepo.GetByCode1(code1);
                LogFailure(code1, "cuenta bloqueada mientras se revisaba la contraseña");
                return LoginResult.Locked(current?.LockedUntil ?? now.Add(LoginLockPolicy.LockDuration));
            }
            user.LastLoginAt      = now;
            user.FailedLoginCount = 0;
            user.LockedUntil      = null;

            // La página manda al inicio del rol de la BD: los requests siguientes lo leen de
            // RoleCache, que no debe quedar con un rol viejo (ej. un Operator recién promovido
            // por CreateAdmin.sql).
            RoleCache.Invalidate(user.Code1);

            return user.IsFirstLogin
                ? LoginResult.AsFirstLogin(user, previousLoginAt)
                : LoginResult.Ok(user, previousLoginAt);
        }

        // Login fallido en App_Data/Logs (AUD-28): el Code1 que se escribió, el motivo y la hora;
        // nunca la contraseña.
        private static void LogFailure(string code1, string reason)
            => AppLog.Write("Login fallido", "Code1 " + AppLog.Clean(code1) + ": " + reason);

        // Precarga la lista de empleados en segundo plano al arrancar la aplicación
        // (Global.Application_Start): el primer login no espera a la API.
        public void WarmUpEmployees()
        {
            _employeeRepo.WarmUp();
        }

        // Operador local por code1, para re-hidratar la Session cuando se pierde con el login
        // vigente (SessionHelper; auditoría AUD-15).
        public User GetUserByCode1(string code1)
        {
            return _userRepo.GetByCode1(code1);
        }

        // Cambio de contraseña (auditoría AUD-09, decisión D-3): siempre pide la actual; en el
        // primer acceso la actual es la por defecto. Guardar también activa la cuenta
        // (spUserUpdatePassword pone IsFirstLogin = 0).
        public ChangePasswordStatus ChangePassword(string code1, string currentPassword, string newPassword)
        {
            var user = _userRepo.GetByCode1(code1);
            if (user == null)
                return ChangePasswordStatus.UserNotFound;

            if (!PasswordHelper.Verify(currentPassword ?? string.Empty, user.PasswordHash))
                return ChangePasswordStatus.InvalidCurrentPassword;

            if (newPassword == Constants.DEFAULT_PASSWORD)
                return ChangePasswordStatus.NewPasswordIsDefault;

            _userRepo.UpdatePassword(user.Id, PasswordHelper.Hash(newPassword));
            return ChangePasswordStatus.Success;
        }
    }
}
