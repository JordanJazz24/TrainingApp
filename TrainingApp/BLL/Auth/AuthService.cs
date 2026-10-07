using System;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.BLL.Auth
{
    public class AuthService
    {
        private readonly EmployeeRepository _employeeRepo;
        private readonly OperatorRepository _operatorRepo;

        public AuthService()
        {
            _employeeRepo = new EmployeeRepository();
            _operatorRepo = new OperatorRepository();
        }

        public LoginResult Login(string code1, string password)
        {
            // Step 1: verify code1 exists in external company API
            var employee = _employeeRepo.GetByCode1(code1);
            if (employee == null)
                return LoginResult.NotFound();

            // Step 2: check for existing local Operator record
            var op = _operatorRepo.GetByCode1(code1);

            if (op == null)
            {
                // First access — no local record yet; validate against default password
                if (password != Constants.DEFAULT_PASSWORD)
                    return LoginResult.Invalid();

                op = new Operator
                {
                    Code1        = employee.Code1,
                    Name         = employee.FullName,
                    PasswordHash = PasswordHelper.Hash(Constants.DEFAULT_PASSWORD),
                    IsFirstLogin = true,
                    CreatedAt    = DateTime.UtcNow
                };
                op.Id = _operatorRepo.Insert(op);
                return LoginResult.AsFirstLogin(op);
            }

            // Step 3: existing account — verify stored hash
            if (!PasswordHelper.Verify(password, op.PasswordHash))
                return LoginResult.Invalid();

            return op.IsFirstLogin
                ? LoginResult.AsFirstLogin(op)
                : LoginResult.Ok(op);
        }

        // Precarga la lista de empleados en segundo plano al arrancar la aplicación
        // (Global.Application_Start): el primer login no espera a la API.
        public void WarmUpEmployees()
        {
            _employeeRepo.WarmUp();
        }

        // Operador local por code1, para re-hidratar la Session cuando se pierde con el login
        // vigente (SessionHelper; auditoría AUD-15).
        public Operator GetOperatorByCode1(string code1)
        {
            return _operatorRepo.GetByCode1(code1);
        }

        // Cambio de contraseña (auditoría AUD-09, decisión D-3): siempre pide la actual; en el
        // primer acceso la actual es la por defecto. Guardar también activa la cuenta
        // (spOperatorUpdatePassword pone IsFirstLogin = 0).
        public ChangePasswordStatus ChangePassword(string code1, string currentPassword, string newPassword)
        {
            var op = _operatorRepo.GetByCode1(code1);
            if (op == null)
                return ChangePasswordStatus.OperatorNotFound;

            if (!PasswordHelper.Verify(currentPassword ?? string.Empty, op.PasswordHash))
                return ChangePasswordStatus.InvalidCurrentPassword;

            if (newPassword == Constants.DEFAULT_PASSWORD)
                return ChangePasswordStatus.NewPasswordIsDefault;

            _operatorRepo.UpdatePassword(op.Id, PasswordHelper.Hash(newPassword));
            return ChangePasswordStatus.Success;
        }
    }
}
