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
        public User User { get; set; }

        public static LoginResult NotFound() =>
            new LoginResult { Status = LoginStatus.EmployeeNotFound };

        public static LoginResult Invalid() =>
            new LoginResult { Status = LoginStatus.InvalidCredentials };

        public static LoginResult AsFirstLogin(User user) =>
            new LoginResult { Status = LoginStatus.FirstLogin, User = user };

        public static LoginResult Ok(User user) =>
            new LoginResult { Status = LoginStatus.Success, User = user };
    }
}
