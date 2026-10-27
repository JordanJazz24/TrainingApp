using System;
using System.Collections.Generic;
using TrainingApp.BLL.Auth;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // El operador elige su propia área, una sola vez (spec 021, D-166/D-171). Después solo un
    // admin la cambia (UserAdminService.SetArea). No confía en la página: lo que no cumple es
    // un envío manipulado o una pestaña vieja, y es una excepción.
    public class OperatorAreaService
    {
        private readonly IUserRepository _users;
        private readonly Func<IList<EligibleArea>> _eligibleAreas;

        public OperatorAreaService()
            : this(new UserRepository(), () => new AreaRepository().GetEligible())
        {
        }

        // Para los tests: el repositorio de usuarios y las áreas elegibles se reemplazan.
        public OperatorAreaService(IUserRepository users, Func<IList<EligibleArea>> eligibleAreas)
        {
            _users         = users ?? throw new ArgumentNullException(nameof(users));
            _eligibleAreas = eligibleAreas ?? throw new ArgumentNullException(nameof(eligibleAreas));
        }

        // Elegir la propia es CanTrain (P-2): solo si aún no tiene área y el área es elegible
        // (P-4). El SP guarda solo si sigue sin área: si otra pestaña ya eligió, excepción.
        public void ChooseOwnArea(int userId, int areaId)
        {
            var user = _users.GetById(userId);
            if (user == null || !AccessPolicy.CanTrain(user.Role))
                throw new UnauthorizedAccessException("El usuario no puede entrenar.");
            if (user.AreaId != null)
                throw new InvalidOperationException("El operador ya tiene área; solo un administrador la cambia.");
            if (!TrainingAccessRule.IsEligible(areaId, _eligibleAreas()))
                throw new ArgumentException("El área no es elegible.", nameof(areaId));

            // 0 filas: otra pestaña eligió, o la cuenta se borró o dejó de ser Operator entretanto.
            if (!_users.ChooseArea(userId, areaId))
                throw new InvalidOperationException("No se guardó el área: ya tiene una o la cuenta cambió.");
        }
    }
}
