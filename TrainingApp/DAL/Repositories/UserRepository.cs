using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        // THROW de spUserDelete: quien borra ya no es admin o se borra a sí mismo.
        private const int CallerNotAllowedError = 50011;

        // Violación de UQ_Users_Code1 (índice único / restricción única).
        private const int UniqueIndexError      = 2601;
        private const int UniqueConstraintError = 2627;

        public User GetById(int userId)
        {
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spUserGetById",
                cmd => cmd.Parameters.AddWithValue("@UserId", userId)))
            {
                return reader.Read() ? Map(reader) : null;
            }
        }

        public IList<UserSummary> Search(string text, int offset, int pageSize, out int total)
        {
            var users = new List<UserSummary>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spUserSearch", cmd =>
            {
                cmd.Parameters.AddWithValue("@Text",     (object)text ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Offset",   offset);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
            }))
            {
                total = reader.Read() ? Convert.ToInt32(reader["Total"]) : 0;
                if (reader.NextResult())
                    while (reader.Read())
                    {
                        var summary = new UserSummary { SessionCount = Convert.ToInt32(reader["SessionCount"]) };
                        Fill(summary, reader);
                        users.Add(summary);
                    }
            }
            return users;
        }

        public void ResetPassword(int userId, string passwordHash)
        {
            using (var db = DatabaseFactory.Create())
                db.ExecuteNonQuery("spUserResetPassword", cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserId",       userId);
                    cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                });
        }

        public void Unlock(int userId)
        {
            using (var db = DatabaseFactory.Create())
                db.ExecuteNonQuery("spUserUnlock",
                    cmd => cmd.Parameters.AddWithValue("@UserId", userId));
        }

        public bool Delete(int callerId, int userId)
        {
            try
            {
                using (var db = DatabaseFactory.Create())
                    db.ExecuteNonQuery("spUserDelete", cmd =>
                    {
                        cmd.Parameters.AddWithValue("@CallerId", callerId);
                        cmd.Parameters.AddWithValue("@UserId",   userId);
                    });
                return true;
            }
            catch (SqlException ex) when (ex.Number == CallerNotAllowedError)
            {
                return false;
            }
        }

        public bool TryInsert(User user)
        {
            try
            {
                user.Id = Insert(user);
                return true;
            }
            catch (SqlException ex) when (ex.Number == UniqueIndexError || ex.Number == UniqueConstraintError)
            {
                return false;
            }
        }

        public User GetByCode1(string code1)
        {
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spUserGetByCode1",
                cmd => cmd.Parameters.AddWithValue("@Code1", code1)))
            {
                if (reader.Read())
                    return Map(reader);
                return null;
            }
        }

        public int Insert(User user)
        {
            using (var db = DatabaseFactory.Create())
            {
                var id = db.ExecuteScalar("spUserInsert", cmd =>
                {
                    cmd.Parameters.AddWithValue("@Code1",        user.Code1);
                    cmd.Parameters.AddWithValue("@Name",         user.Name);
                    cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                    cmd.Parameters.AddWithValue("@IsFirstLogin", user.IsFirstLogin);
                    cmd.Parameters.AddWithValue("@CreatedAt",    user.CreatedAt);
                    cmd.Parameters.AddWithValue("@Role",         (byte)user.Role);
                    cmd.Parameters.AddWithValue("@LastLoginAt",  (object)user.LastLoginAt ?? DBNull.Value);
                });
                return Convert.ToInt32(id);
            }
        }

        public void UpdatePassword(int userId, string passwordHash)
        {
            using (var db = DatabaseFactory.Create())
                db.ExecuteNonQuery("spUserUpdatePassword", cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserId",       userId);
                    cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                });
        }

        // Login fallido (spec 013): suma el fallo y, al llegar al umbral, bloquea hasta
        // lockedUntil; con la cuenta ya bloqueada no suma. Devuelve el fin del bloqueo vigente
        // (null si nunca tuvo uno).
        public DateTime? RecordLoginFailure(int userId, int lockThreshold, DateTime lockedUntil,
            DateTime now)
        {
            using (var db = DatabaseFactory.Create())
            {
                var value = db.ExecuteScalar("spUserLoginFailed", cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserId",        userId);
                    cmd.Parameters.AddWithValue("@LockThreshold", lockThreshold);
                    cmd.Parameters.AddWithValue("@LockedUntil",   lockedUntil);
                    cmd.Parameters.AddWithValue("@Now",           now);
                });
                return NullableDate(value);
            }
        }

        // Login correcto (spec 013): contador a 0, sin bloqueo y último acceso. false si otro
        // request bloqueó la cuenta mientras tanto (no se actualiza nada).
        public bool RecordLoginSuccess(int userId, DateTime lastLoginAt)
        {
            using (var db = DatabaseFactory.Create())
            {
                var rows = db.ExecuteScalar("spUserLoginSucceeded", cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserId",      userId);
                    cmd.Parameters.AddWithValue("@LastLoginAt", lastLoginAt);
                });
                return Convert.ToInt32(rows) > 0;
            }
        }

        // El operador elige su propia área (spec 021, P-4): solo si es Operator y aún no
        // tiene. false si no se guardó (ya tenía área, no es Operator o no existe).
        public bool ChooseArea(int userId, int areaId)
        {
            using (var db = DatabaseFactory.Create())
            {
                var rows = db.ExecuteScalar("spUserChooseArea", cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@AreaId", areaId);
                });
                return Convert.ToInt32(rows) > 0;
            }
        }

        // Un admin cambia o quita (null) el área de un Operator (spec 021, P-5). false si la
        // cuenta no es Operator o no existe.
        public bool SetArea(int userId, int? areaId)
        {
            using (var db = DatabaseFactory.Create())
            {
                var rows = db.ExecuteScalar("spUserSetArea", cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@AreaId", (object)areaId ?? DBNull.Value);
                });
                return Convert.ToInt32(rows) > 0;
            }
        }

        private static User Map(SqlDataReader r)
        {
            var user = new User();
            Fill(user, r);
            return user;
        }

        private static void Fill(User user, SqlDataReader r)
        {
            user.Id               = Convert.ToInt32(r["Id"]);
            user.Code1            = r["Code1"].ToString();
            user.Name             = r["Name"].ToString();
            user.PasswordHash     = r["PasswordHash"].ToString();
            user.IsFirstLogin     = Convert.ToBoolean(r["IsFirstLogin"]);
            user.CreatedAt        = Convert.ToDateTime(r["CreatedAt"]);
            user.Role             = (UserRole)Convert.ToByte(r["Role"]);
            user.LastLoginAt      = NullableDate(r["LastLoginAt"]);
            user.FailedLoginCount = Convert.ToInt32(r["FailedLoginCount"]);
            user.LockedUntil      = NullableDate(r["LockedUntil"]);
            user.AreaId           = r["AreaId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["AreaId"]);
            user.ProductName      = r["ProductName"] as string;
            user.AreaName         = r["AreaName"] as string;
        }

        private static DateTime? NullableDate(object value)
            => value == null || value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
    }
}
