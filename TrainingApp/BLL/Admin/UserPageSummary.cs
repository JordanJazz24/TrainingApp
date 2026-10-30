using System;
using System.Collections.Generic;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Admin
{
    // Cifras del directorio de Usuarios (spec 022, brief 8, D-205): cuenta solo las cuentas de la
    // página abierta, no toda la lista. Contar en todas las páginas pediría una consulta agregada.
    public class UserPageSummary
    {
        public int Locked { get; set; }
        public int PendingFirstLogin { get; set; }

        // Operadores que aún no eligieron su área.
        public int NoArea { get; set; }

        public static UserPageSummary Of(IEnumerable<UserSummary> users, DateTime nowUtc)
        {
            var summary = new UserPageSummary();
            foreach (var user in users)
            {
                var state = UserAdminService.StateOf(user, nowUtc);
                if (state == UserAccountState.Locked) summary.Locked++;
                else if (state == UserAccountState.PendingFirstLogin) summary.PendingFirstLogin++;
                if (user.Role == UserRole.Operator && user.AreaId == null) summary.NoArea++;
            }
            return summary;
        }
    }
}
