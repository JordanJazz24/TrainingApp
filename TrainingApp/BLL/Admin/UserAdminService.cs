using System;
using System.Collections.Generic;
using TrainingApp.BLL.Auth;
using TrainingApp.BLL.Training;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.BLL.Admin
{
    // Resultado de una acción de la gestión de usuarios: lo que la página muestra como aviso.
    public enum UserAdminResult
    {
        Ok,
        UserNotFound,
        NotInEmployeeApi,   // Crear: el Code1 no está en la API de empleados
        AlreadyExists,      // Crear: el Code1 ya tiene cuenta
        OwnAccount          // nadie se borra a sí mismo
    }

    // Estado que muestra la lista (derivado, no se guarda): bloqueada si el bloqueo no venció;
    // si no, primer acceso pendiente mientras no cambió la contraseña; si no, activa.
    public enum UserAccountState
    {
        Active,
        PendingFirstLogin,
        Locked
    }

    public class UserSearchPage
    {
        public IList<UserSummary> Users { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }        // desde 1
        public int PageCount { get; set; }

        // Cifras de las cuentas de esta página (spec 022, brief 8).
        public UserPageSummary Summary { get; set; }
    }

    // Gestión de usuarios (spec 013). Cada método recibe el userId del admin autenticado y
    // verifica su capacidad (AccessPolicy) sin confiar en la página: si no la tiene, es una
    // excepción (un invariante, no un aviso).
    public class UserAdminService
    {
        public const int PageSize = 20;

        // Búsqueda: largo máximo del texto (la caja tiene MaxLength=100; el resto es manipulación).
        private const int MaxSearchLength = 100;

        private readonly IUserRepository _users;
        private readonly Func<string, Employee> _findEmployee;
        private readonly Func<IList<EligibleArea>> _eligibleAreas;

        public UserAdminService()
            : this(new UserRepository(), code1 => new EmployeeRepository().GetByCode1(code1))
        {
        }

        // Para los tests (AC-5): la API de empleados se reemplaza.
        public UserAdminService(IUserRepository users, Func<string, Employee> findEmployee)
            : this(users, findEmployee, () => new AreaRepository().GetEligible())
        {
        }

        // Para los tests de SetArea (spec 021): también las áreas elegibles.
        public UserAdminService(IUserRepository users, Func<string, Employee> findEmployee,
            Func<IList<EligibleArea>> eligibleAreas)
        {
            _users         = users ?? throw new ArgumentNullException(nameof(users));
            _findEmployee  = findEmployee ?? throw new ArgumentNullException(nameof(findEmployee));
            _eligibleAreas = eligibleAreas ?? throw new ArgumentNullException(nameof(eligibleAreas));
        }

        public static UserAccountState StateOf(User user, DateTime nowUtc)
        {
            if (LoginLockPolicy.IsLocked(user, nowUtc)) return UserAccountState.Locked;
            return user.IsFirstLogin ? UserAccountState.PendingFirstLogin : UserAccountState.Active;
        }

        // Una página de la lista (page desde 1; fuera de rango, la última que exista).
        public UserSearchPage Search(int adminUserId, string text, int page)
        {
            RequireAdmin(adminUserId);
            text = (text ?? string.Empty).Trim();
            if (text.Length > MaxSearchLength) text = text.Substring(0, MaxSearchLength);
            int pageNumber = Math.Max(1, page);
            var users = _users.Search(text, (pageNumber - 1) * PageSize, PageSize, out int total);

            int pageCount = Math.Max(1, (total + PageSize - 1) / PageSize);
            if (pageNumber > pageCount)
            {
                pageNumber = pageCount;
                users = _users.Search(text, (pageNumber - 1) * PageSize, PageSize, out total);
            }

            return new UserSearchPage
            {
                Users = users, Total = total, Page = pageNumber, PageCount = pageCount,
                Summary = UserPageSummary.Of(users, DateTime.UtcNow)
            };
        }

        // Crea la cuenta de un empleado de la API con la contraseña por defecto y el cambio
        // obligatorio (AC-8). Convive con el autorregistro.
        public UserAdminResult Create(int adminUserId, string code1, UserRole role)
        {
            RequireAdmin(adminUserId);
            RequireDefinedRole(role);
            string value = (code1 ?? string.Empty).Trim();

            var employee = value.Length == 0 ? null : _findEmployee(value);
            if (employee == null) return UserAdminResult.NotInEmployeeApi;
            if (_users.GetByCode1(employee.Code1) != null) return UserAdminResult.AlreadyExists;

            // Otro request (autorregistro u otro admin) pudo crearla entre la consulta y el alta.
            bool created = _users.TryInsert(new User
            {
                Code1        = employee.Code1,
                Name         = employee.FullName,
                PasswordHash = PasswordHelper.Hash(Constants.DEFAULT_PASSWORD),
                IsFirstLogin = true,
                CreatedAt    = DateTime.UtcNow,
                Role         = role
            });
            return created ? UserAdminResult.Ok : UserAdminResult.AlreadyExists;
        }

        // Contraseña por defecto con cambio obligatorio; también desbloquea (P-2). Conserva el
        // historial de entrenamiento (AC-10). La sesión abierta del usuario sigue hasta el
        // logout o el timeout (D-20).
        public UserAdminResult ResetPassword(int adminUserId, int userId)
        {
            RequireAdmin(adminUserId);
            if (_users.GetById(userId) == null) return UserAdminResult.UserNotFound;

            _users.ResetPassword(userId, PasswordHelper.Hash(Constants.DEFAULT_PASSWORD));
            return UserAdminResult.Ok;
        }

        // Desbloqueo antes de que venza (AC-11): contador a 0.
        public UserAdminResult Unlock(int adminUserId, int userId)
        {
            RequireAdmin(adminUserId);
            if (_users.GetById(userId) == null) return UserAdminResult.UserNotFound;

            _users.Unlock(userId);
            return UserAdminResult.Ok;
        }

        // Borra la cuenta y todo su historial (sesiones e intentos), sin recuperación (enmienda
        // 2026-10-13). Nadie se borra a sí mismo: así siempre queda quien borra y el sistema
        // nunca queda sin admins. El SP lo vuelve a verificar bajo su lock (dos admins que se
        // borran entre sí a la vez). La sesión abierta del borrado se cierra en su próximo
        // request: su cuenta ya no tiene rol.
        public UserAdminResult Delete(int adminUserId, int userId)
        {
            RequireAdmin(adminUserId);
            if (adminUserId == userId) return UserAdminResult.OwnAccount;

            var user = _users.GetById(userId);
            if (user == null) return UserAdminResult.UserNotFound;

            if (!_users.Delete(adminUserId, userId))
                throw new UnauthorizedAccessException("El usuario dejó de ser administrador.");

            RoleCache.Invalidate(user.Code1);
            return UserAdminResult.Ok;
        }

        // Cambia el área de un operador (spec 021, D-172, P-5). No la quita (enmienda 1, D-208):
        // null es una excepción. Solo un Operator tiene área (P-2): una cuenta de administrador
        // o un área no elegible es un envío manipulado, excepción. El SP vuelve a exigir
        // Operator: si la cuenta se borró entretanto, no la encuentra. Sin registro de actividad.
        public UserAdminResult SetArea(int adminUserId, int userId, int? areaId)
        {
            RequireAdmin(adminUserId);
            if (!areaId.HasValue)
                throw new ArgumentNullException(nameof(areaId), "El área no se quita, solo se cambia (D-208).");
            var user = _users.GetById(userId);
            if (user == null) return UserAdminResult.UserNotFound;
            if (!AccessPolicy.CanTrain(user.Role))
                throw new InvalidOperationException("Solo un operador tiene área.");
            if (!TrainingAccessRule.IsEligible(areaId.Value, _eligibleAreas()))
                throw new ArgumentException("El área no es elegible.", nameof(areaId));

            return _users.SetArea(userId, areaId) ? UserAdminResult.Ok : UserAdminResult.UserNotFound;
        }

        private void RequireAdmin(int adminUserId)
        {
            var admin = _users.GetById(adminUserId);
            if (admin == null || !AccessPolicy.CanManageUsers(admin.Role))
                throw new UnauthorizedAccessException("El usuario no puede administrar usuarios.");
        }

        // Un rol fuera del enum no tendría ninguna capacidad: es un error de quien llama.
        private static void RequireDefinedRole(UserRole role)
        {
            if (!Enum.IsDefined(typeof(UserRole), role))
                throw new ArgumentOutOfRangeException(nameof(role), role, "Rol inválido.");
        }
    }
}
