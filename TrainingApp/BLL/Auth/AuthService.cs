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

        public LoginResult Login(string code1, string password)
        {
            // Step 1: verify code1 exists in external company API
            var employee = _employeeRepo.GetByCode1(code1);
            if (employee == null)
                return LoginResult.NotFound();

            // Step 2: check for existing local User record
            var user = _userRepo.GetByCode1(code1);

            if (user == null)
            {
                // First access — no local record yet; validate against default password
                if (password != Constants.DEFAULT_PASSWORD)
                    return LoginResult.Invalid();

                user = new User
                {
                    Code1        = employee.Code1,
                    Name         = employee.FullName,
                    PasswordHash = PasswordHelper.Hash(Constants.DEFAULT_PASSWORD),
                    IsFirstLogin = true,
                    CreatedAt    = DateTime.UtcNow
                };
                user.Id = _userRepo.Insert(user);
                return LoginResult.AsFirstLogin(user);
            }

            // Step 3: existing account — verify stored hash
            if (!PasswordHelper.Verify(password, user.PasswordHash))
                return LoginResult.Invalid();

            return user.IsFirstLogin
                ? LoginResult.AsFirstLogin(user)
                : LoginResult.Ok(user);
        }

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
