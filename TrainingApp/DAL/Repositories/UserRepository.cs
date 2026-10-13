using System;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class UserRepository
    {
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

        private static User Map(SqlDataReader r) => new User
        {
            Id               = Convert.ToInt32(r["Id"]),
            Code1            = r["Code1"].ToString(),
            Name             = r["Name"].ToString(),
            PasswordHash     = r["PasswordHash"].ToString(),
            IsFirstLogin     = Convert.ToBoolean(r["IsFirstLogin"]),
            CreatedAt        = Convert.ToDateTime(r["CreatedAt"]),
            Role             = (UserRole)Convert.ToByte(r["Role"]),
            LastLoginAt      = NullableDate(r["LastLoginAt"]),
            FailedLoginCount = Convert.ToInt32(r["FailedLoginCount"]),
            LockedUntil      = NullableDate(r["LockedUntil"])
        };

        private static DateTime? NullableDate(object value)
            => value == null || value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
    }
}
