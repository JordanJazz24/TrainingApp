using System.Collections.Generic;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    // Lo que usa la gestión de usuarios (spec 013). Interfaz para que UserAdminService se
    // pruebe con datos en memoria (AC-5); la implementa UserRepository.
    public interface IUserRepository
    {
        User GetById(int userId);
        User GetByCode1(string code1);
        int Insert(User user);

        // Alta que no falla si otro request creó el mismo Code1 a la vez (autorregistro u
        // otro admin): false en ese caso. Si la crea, user.Id queda con el Id nuevo.
        bool TryInsert(User user);

        // Una página ordenada por nombre, con las sesiones de cada uno; total = usuarios que
        // cumplen el filtro.
        IList<UserSummary> Search(string text, int offset, int pageSize, out int total);

        void ResetPassword(int userId, string passwordHash);
        void Unlock(int userId);

        // Borra la cuenta y su historial. Lo pide callerId; el SP vuelve a verificar, bajo su
        // lock, que siga siendo admin y que no se borre a sí mismo: false si no.
        bool Delete(int callerId, int userId);

        // Área del operador (spec 021). ChooseArea: la propia, solo si es Operator y aún no
        // tiene (false si no). SetArea: la cambia o la quita (null) un admin; false si la
        // cuenta no es Operator. Que el área sea elegible lo verifica la BLL.
        bool ChooseArea(int userId, int areaId);
        bool SetArea(int userId, int? areaId);
    }
}
