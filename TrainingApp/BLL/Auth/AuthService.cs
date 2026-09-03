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

        public void ActivateAccount(int operatorId, string newPassword)
        {
            string hash = PasswordHelper.Hash(newPassword);
            _operatorRepo.UpdatePassword(operatorId, hash);
        }
    }
}
