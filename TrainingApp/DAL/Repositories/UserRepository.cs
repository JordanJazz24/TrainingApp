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

        private static User Map(SqlDataReader r) => new User
        {
            Id           = Convert.ToInt32(r["Id"]),
            Code1        = r["Code1"].ToString(),
            Name         = r["Name"].ToString(),
            PasswordHash = r["PasswordHash"].ToString(),
            IsFirstLogin = Convert.ToBoolean(r["IsFirstLogin"]),
            CreatedAt    = Convert.ToDateTime(r["CreatedAt"])
        };
    }
}
