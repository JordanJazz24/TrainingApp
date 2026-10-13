using TrainingApp.Domain;

namespace TrainingApp.BLL.Auth
{
    public enum LoginStatus
    {
        Success,
        FirstLogin,
        InvalidCredentials,
        EmployeeNotFound,

        // Contraseña correcta, pero el rol no entra por este login (spec 013): no cuenta como
        // fallo. La página muestra el camino correcto.
        WrongPortal
    }

    public class LoginResult
    {
        public LoginStatus Status { get; set; }
        public User User { get; set; }

        public static LoginResult NotFound() =>
            new LoginResult { Status = LoginStatus.EmployeeNotFound };

        public static LoginResult Invalid() =>
            new LoginResult { Status = LoginStatus.InvalidCredentials };

        public static LoginResult WrongPortal() =>
            new LoginResult { Status = LoginStatus.WrongPortal };

        public static LoginResult AsFirstLogin(User user) =>
            new LoginResult { Status = LoginStatus.FirstLogin, User = user };

        public static LoginResult Ok(User user) =>
            new LoginResult { Status = LoginStatus.Success, User = user };
    }
}
