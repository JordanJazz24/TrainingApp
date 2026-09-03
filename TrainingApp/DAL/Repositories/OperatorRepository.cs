using System;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class OperatorRepository
    {
        public Operator GetByCode1(string code1)
        {
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spOperatorGetByCode1",
                cmd => cmd.Parameters.AddWithValue("@Code1", code1)))
            {
                if (reader.Read())
                    return Map(reader);
                return null;
            }
        }

        public int Insert(Operator op)
        {
            using (var db = DatabaseFactory.Create())
            {
                var id = db.ExecuteScalar("spOperatorInsert", cmd =>
                {
                    cmd.Parameters.AddWithValue("@Code1",        op.Code1);
                    cmd.Parameters.AddWithValue("@Name",         op.Name);
                    cmd.Parameters.AddWithValue("@PasswordHash", op.PasswordHash);
                    cmd.Parameters.AddWithValue("@IsFirstLogin", op.IsFirstLogin);
                    cmd.Parameters.AddWithValue("@CreatedAt",    op.CreatedAt);
                });
                return Convert.ToInt32(id);
            }
        }

        public void UpdatePassword(int operatorId, string passwordHash)
        {
            using (var db = DatabaseFactory.Create())
                db.ExecuteNonQuery("spOperatorUpdatePassword", cmd =>
                {
                    cmd.Parameters.AddWithValue("@OperatorId",   operatorId);
                    cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                });
        }

        private static Operator Map(SqlDataReader r) => new Operator
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
