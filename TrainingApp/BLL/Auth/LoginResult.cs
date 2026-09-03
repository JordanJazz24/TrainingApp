using TrainingApp.Domain;

namespace TrainingApp.BLL.Auth
{
    public enum LoginStatus
    {
        Success,
        FirstLogin,
        InvalidCredentials,
        EmployeeNotFound
    }

    public class LoginResult
    {
        public LoginStatus Status { get; set; }
        public Operator Operator { get; set; }

        public static LoginResult NotFound() =>
            new LoginResult { Status = LoginStatus.EmployeeNotFound };

        public static LoginResult Invalid() =>
            new LoginResult { Status = LoginStatus.InvalidCredentials };

        public static LoginResult AsFirstLogin(Operator op) =>
            new LoginResult { Status = LoginStatus.FirstLogin, Operator = op };

        public static LoginResult Ok(Operator op) =>
            new LoginResult { Status = LoginStatus.Success, Operator = op };
    }
}
